using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora.Exceptions;
using Kirora.Models;
using Kirora.Serialization;
using static Kirora.Tests.CoreContractTests;

namespace Kirora.Tests;

public sealed class CoreBehaviorTests
{
    [Theory]
    [InlineData("trackEvent")]
    [InlineData("sendTransactionalMessage")]
    [InlineData("createList")]
    [InlineData("identifyContact")]
    public async Task HeaderKeysAndPayloadAreStableAcrossWriteRetries(string operation)
    {
        var fixture = Fixture(operation);
        var keys = new List<string>(); var bodies = new List<string>();
        using var handler = new Handler(async (r, ct) =>
        {
            keys.Add(r.Headers.GetValues("Idempotency-Key").Single());
            bodies.Add(await r.Content!.ReadAsStringAsync(ct));
            return keys.Count == 1 ? new(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") } : Success(fixture);
        });
        using var http = new HttpClient(handler);
        await Invoke(new KiroraClient(http, Options(1)), operation, fixture["request"], new() { IdempotencyKey = "caller-key-123" });
        Assert.Equal(2, handler.Calls); Assert.All(keys, k => Assert.Equal("caller-key-123", k)); Assert.Equal(bodies[0], bodies[1]);
    }

    [Theory]
    [InlineData("trackEventsBatch", "events")]
    [InlineData("sendTransactionalMessagesBatch", "messages")]
    public async Task BatchKeysAreSnapshotOnlyAndStableAcrossRetries(string operation, string property)
    {
        var fixture = Fixture(operation); var body = fixture["request"]!.DeepClone();
        var entries = body[property]!.AsArray(); entries[0]!.AsObject().Remove("idempotencyKey");
        entries[1]!["idempotencyKey"] = "supplied-entry-key";
        var original = body.ToJsonString(); var sent = new List<JsonNode>();
        using var handler = new Handler(async (r, ct) =>
        {
            Assert.False(r.Headers.Contains("Idempotency-Key"));
            sent.Add(JsonNode.Parse(await r.Content!.ReadAsStringAsync(ct))!);
            return sent.Count == 1 ? new(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") } : Success(fixture);
        });
        using var http = new HttpClient(handler);
        await Invoke(new KiroraClient(http, Options(1)), operation, body);
        Assert.Equal(2, sent.Count); Assert.True(JsonNode.DeepEquals(sent[0], sent[1]));
        Assert.True(Guid.TryParse(sent[0][property]![0]!["idempotencyKey"]!.GetValue<string>(), out _));
        Assert.Equal("supplied-entry-key", sent[0][property]![1]!["idempotencyKey"]!.GetValue<string>());
        Assert.Equal(original, body.ToJsonString());
    }

    [Fact]
    public async Task DifferentLogicalCallsGetDifferentGeneratedKeys()
    {
        var keys = new List<string>(); var fixture = Fixture("trackEvent");
        using var handler = new Handler((r, _) => { keys.Add(r.Headers.GetValues("Idempotency-Key").Single()); return Task.FromResult(Success(fixture)); });
        using var http = new HttpClient(handler); var client = new KiroraClient(http, Options());
        await Invoke(client, "trackEvent", fixture["request"]); await Invoke(client, "trackEvent", fixture["request"]);
        Assert.NotEqual(keys[0], keys[1]);
    }

    [Theory]
    [InlineData("IDEMPOTENCY_KEY_REUSED")]
    [InlineData("IDEMPOTENCY_KEY_OUTCOME_UNKNOWN")]
    public async Task DoesNotRetryPermanentOrAmbiguousIdempotencyRefusals(string code)
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
        { Content = new StringContent($"{{\"error\":{{\"code\":\"{code}\",\"message\":\"refused\"}}}}") }));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraConflictException>(() => Invoke(new KiroraClient(http, Options(3)), "trackEvent", Fixture("trackEvent")["request"]));
        Assert.Equal(code, error.Code); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task KeyInUseRetriesWithOriginalKey()
    {
        var keys = new List<string>(); var fixture = Fixture("trackEvent");
        using var handler = new Handler((r, _) =>
        {
            keys.Add(r.Headers.GetValues("Idempotency-Key").Single());
            return Task.FromResult(keys.Count == 1 ? new HttpResponseMessage(HttpStatusCode.Conflict)
            { Content = new StringContent("""{"error":{"code":"IDEMPOTENCY_KEY_IN_USE","message":"busy"}}""") } : Success(fixture));
        });
        using var http = new HttpClient(handler);
        await Invoke(new KiroraClient(http, Options(1)), "trackEvent", fixture["request"]);
        Assert.Equal(2, keys.Count); Assert.Equal(keys[0], keys[1]);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("has\r\nheader-injection")]
    public async Task RejectsInvalidKeysBeforeHttp(string key)
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException("No HTTP expected"));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => Invoke(new KiroraClient(http, Options()), "trackEvent", Fixture("trackEvent")["request"], new() { IdempotencyKey = key }));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task UnsupportedHeaderKeyIsNotSilentlyAppliedToBatch()
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException());
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => Invoke(new KiroraClient(http, Options()), "trackEventsBatch", Fixture("trackEventsBatch")["request"], new() { IdempotencyKey = "unsupported-header" }));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(0)]
    public async Task ContactLookupChangingWritesAreNotRetried(int status)
    {
        using var handler = new Handler((_, _) => status == 0 ? throw new HttpRequestException("Lost response") :
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("{}") }));
        using var http = new HttpClient(handler);
        var task = new KiroraClient(http, Options(3)).Contacts.UpdateAsync("old@example.com", new UpdateContactInput { Email = "new@example.com" }, new UpdateContactQuery { IdType = "email" });
        await Assert.ThrowsAnyAsync<Exception>(() => task); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ContactLookupUnchangedWritesCanRetry()
    {
        var count = 0; var fixture = Fixture("updateContact");
        using var handler = new Handler((_, _) => Task.FromResult(++count == 1 ? new(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") } : Success(fixture)));
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options(1)).Contacts.UpdateAsync("  OLD@example.com  ", new UpdateContactInput { Email = "old@example.com" }, new UpdateContactQuery { IdType = "email" });
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task NullablePatchPreservesExplicitNullAndOmitsAbsentFields()
    {
        using var handler = new Handler(async (r, ct) =>
        {
            var body = JsonNode.Parse(await r.Content!.ReadAsStringAsync(ct))!.AsObject();
            Assert.True(body.ContainsKey("firstName")); Assert.Null(body["firstName"]);
            Assert.False(body.ContainsKey("lastName")); Assert.False(body.ContainsKey("email"));
            Assert.Null(body["attributes"]!["obsolete"]);
            return Success(Fixture("updateContact"));
        });
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options()).Contacts.UpdateAsync(Id, new UpdateContactInput
        {
            FirstName = new Optional<string?>(null),
            Attributes = new Dictionary<string, JsonElement> { ["obsolete"] = JsonSerializer.SerializeToElement<object?>(null) }
        });
    }

    [Fact]
    public async Task PaginationKeepsFiltersContinuesEmptyPagesAndDoesNotMutateQuery()
    {
        var fixture = Fixture("listContacts"); var urls = new List<string>();
        using var handler = new Handler((r, _) =>
        {
            urls.Add(r.RequestUri!.Query);
            var body = fixture["response"]!.DeepClone();
            body["meta"]!["pagination"]!["nextCursor"] = urls.Count == 1 ? "opaque +/=?" : null;
            if (urls.Count == 1) body["data"] = new JsonArray();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) });
        });
        using var http = new HttpClient(handler); var query = new ListContactsQuery { Query = "Ada +&", Limit = 1 };
        var items = new List<ContactDetail>();
        await foreach (var item in new KiroraClient(http, Options()).Contacts.ListAllAsync(query)) items.Add(item);
        Assert.Single(items); Assert.Equal(2, urls.Count);
        Assert.All(urls, u => Assert.Contains("query=Ada%20%2B%26", u));
        Assert.Contains("cursor=opaque%20%2B%2F%3D%3F", urls[1]); Assert.False(query.Cursor.IsSet);
    }

    [Fact]
    public async Task RepeatedCursorIsRejected()
    {
        var body = Fixture("listLists")["response"]!.DeepClone(); body["data"] = new JsonArray(); body["meta"]!["pagination"]!["nextCursor"] = "loop";
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(async () => { await foreach (var _ in new KiroraClient(http, Options()).Lists.ListAllAsync()) { } });
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task IteratorCancellationStopsBeforeNextItemOrPage()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Success(Fixture("listContacts"))));
        using var http = new HttpClient(handler); using var cancellation = new CancellationTokenSource();
        await using var iterator = new KiroraClient(http, Options()).Contacts.ListAllAsync(cancellationToken: cancellation.Token).GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync()); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => iterator.MoveNextAsync().AsTask()); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task CallerCancellationInterruptsRetryAfterDelay()
    {
        using var cancellation = new CancellationTokenSource(); var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new Handler((_, _) =>
        {
            received.TrySetResult(); var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{}") };
            response.Headers.Add("Retry-After", "20"); return Task.FromResult(response);
        });
        using var http = new HttpClient(handler); var options = Options(3); options.MaxRetryDelay = TimeSpan.FromSeconds(30);
        var task = Invoke(new KiroraClient(http, options), "trackEvent", Fixture("trackEvent")["request"], cancellationToken: cancellation.Token);
        await received.Task; cancellation.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task PreCanceledCallsNeverReachHandler()
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException()); using var http = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Invoke(new KiroraClient(http, Options()), "trackEvent", Fixture("trackEvent")["request"], cancellationToken: new CancellationToken(true)));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task DeleteOnlyTreatsNotFoundAsDoneAfterAmbiguousAttempt()
    {
        var count = 0;
        using var handler = new Handler((_, _) => ++count == 1 ? throw new HttpRequestException("Lost response") : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") }));
        using var http = new HttpClient(handler); await new KiroraClient(http, Options(1)).Lists.DeleteAsync(Id); Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ValidatesSpecLimitsAndRequiredIdentityBeforeSending()
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException()); using var http = new HttpClient(handler);
        var client = new KiroraClient(http, Options());
        await Assert.ThrowsAsync<ArgumentException>(() => client.Contacts.IdentifyAsync(new IdentifyContactInput()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Contacts.ListAsync(new ListContactsQuery { Limit = 101 }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.Events.TrackBatchAsync(new TrackEventsBatchInput { Events = Array.Empty<TrackEventsBatchInputEventsItem>() }));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ContactLookupEmailIsEscapedAndIdTypeSent()
    {
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("/v1/contacts/a%2Bb%40example.com", request.RequestUri!.AbsolutePath);
            Assert.Equal("?idType=email", request.RequestUri.Query); return Task.FromResult(Success(Fixture("getContact")));
        });
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options()).Contacts.GetAsync("a+b@example.com", new GetContactQuery { IdType = "email" });
    }

    [Fact]
    public async Task ResponseMetadataCallbackReportsReplayAndRateLimit()
    {
        using var handler = new Handler((_, _) =>
        {
            var response = Success(Fixture("trackEvent")); response.Headers.Add("Idempotency-Replay", "true"); response.Headers.Add("X-Request-Id", "req-replay"); return Task.FromResult(response);
        });
        using var http = new HttpClient(handler); Kirora.Http.KiroraResponseMetadata? metadata = null;
        await Invoke(new KiroraClient(http, Options()), "trackEvent", Fixture("trackEvent")["request"], new() { ResponseReceived = value => metadata = value });
        Assert.True(metadata!.IdempotencyReplay); Assert.Equal("req-replay", metadata.RequestId);
    }
}
