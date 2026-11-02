using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora.Exceptions;
using Kirora.Models;

namespace Kirora.Tests;

public sealed class CoreContractTests
{
    internal const string Id = "0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d";
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal static readonly JsonArray Fixtures = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/core-examples.json")))!.AsArray();
    public static IEnumerable<object[]> Examples() => Fixtures.Select(x => new object[] { x!["operationId"]!.GetValue<string>(), x["example"]!.GetValue<string>() });
    public static IEnumerable<object[]> Errors() => Fixtures.Select(x => x!["operationId"]!.GetValue<string>()).Distinct()
        .SelectMany(id => new[] { 400, 401, 403, 404, 409, 429, 500 }.Select(status => new object[] { id, status }));
    internal static JsonObject Fixture(string id, string? example = null) => Fixtures.First(x => x!["operationId"]!.GetValue<string>() == id && (example is null || x["example"]!.GetValue<string>() == example))!.AsObject();
    internal static KiroraOptions Options(int retries = 0) => new() { ApiKey = "unit-test-placeholder", MaxRetries = retries, MaxRetryDelay = TimeSpan.Zero };
    internal static HttpResponseMessage Success(JsonObject fixture) => new((HttpStatusCode)fixture["status"]!.GetValue<int>())
    { Content = new StringContent(fixture["csv"]?.GetValue<bool>() == true ? fixture["response"]!.GetValue<string>() : fixture["response"]?.ToJsonString() ?? "", Encoding.UTF8, fixture["csv"]?.GetValue<bool>() == true ? "text/csv" : "application/json") };

    [Theory]
    [MemberData(nameof(Examples))]
    public async Task OfficialExamplesMatchMethodPathHeadersAndTypedContracts(string operation, string example)
    {
        var fixture = Fixture(operation, example);
        using var handler = new Handler(async (request, token) =>
        {
            Assert.Equal(fixture["method"]!.GetValue<string>(), request.Method.Method);
            var expected = fixture["path"]!.GetValue<string>();
            foreach (var pair in fixture["pathValues"]!.AsObject()) expected = expected.Replace("{" + pair.Key + "}", Uri.EscapeDataString(pair.Value!.GetValue<string>()));
            Assert.Equal(expected, request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer unit-test-placeholder", request.Headers.Authorization!.ToString());
            Assert.Equal("tenant-1", request.Headers.GetValues("Kirora-Workspace").Single());
            Assert.Equal(fixture["acceptsKey"]!.GetValue<bool>(), request.Headers.Contains("Idempotency-Key"));
            if (fixture["multipart"]!.GetValue<bool>())
            {
                Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);
                var content = await request.Content.ReadAsStringAsync(token);
                Assert.Contains("name=file", content.Replace("\"", ""));
                Assert.Contains("fixture.png", content);
                Assert.Contains("SDK fixture", content);
            }
            else if (fixture["request"] is { } expectedBody)
            {
                Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
                AssertEquivalent(expectedBody, JsonNode.Parse(await request.Content.ReadAsStringAsync(token))!);
            }
            else Assert.Null(request.Content);
            return Success(fixture);
        });
        using var http = new HttpClient(handler);
        var result = await Invoke(new KiroraClient(http, Options()).ForWorkspace("tenant-1"), operation, fixture["request"]);
        if (fixture["response"] is { } expectedResponse)
            AssertEquivalent(expectedResponse, JsonSerializer.SerializeToNode(result, result!.GetType(), Json)!);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [MemberData(nameof(Errors))]
    public async Task EveryEndpointPreservesStructuredApiErrors(string operation, int status)
    {
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent("""{"error":{"code":"FUTURE_ERROR","message":"refused","details":{"reason":"business-rule"},"errors":[{"field":"input","message":"bad value"}]}}""") };
            response.Headers.Add("X-Request-Id", "req-test");
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAnyAsync<KiroraApiException>(() => Invoke(new KiroraClient(http, Options()), operation, Fixture(operation)["request"]));
        Assert.Equal(status, (int)error.StatusCode);
        Assert.Equal("FUTURE_ERROR", error.Code);
        Assert.Equal("req-test", error.RequestId);
        Assert.Equal("business-rule", error.Details!.Value.GetProperty("reason").GetString());
        Assert.Equal("input", Assert.Single(error.Errors).Field);
        Assert.Equal(1, handler.Calls);
    }

    // Deliberately calls the public methods: tests do not bypass resources through their internal descriptors.
    internal static async Task<object?> Invoke(KiroraClient client, string operation, JsonNode? body, KiroraRequestOptions? options = null, CancellationToken cancellationToken = default)
    {
        T Read<T>() => body!.Deserialize<T>(Json)!;
        switch (operation)
        {
            case "listContacts": return await client.Contacts.ListAsync(options: options, cancellationToken: cancellationToken);
            case "identifyContact": return await client.Contacts.IdentifyAsync(Read<IdentifyContactInput>(), options, cancellationToken);
            case "identifyContactsBatch": return await client.Contacts.IdentifyBatchAsync(Read<IdentifyContactsBatchInput>(), options, cancellationToken);
            case "getContact": return await client.Contacts.GetAsync(Id, options: options, cancellationToken: cancellationToken);
            case "updateContact": return await client.Contacts.UpdateAsync(Id, Read<UpdateContactInput>(), options: options, cancellationToken: cancellationToken);
            case "deleteContact": await client.Contacts.DeleteAsync(Id, options: options, cancellationToken: cancellationToken); return null;
            case "deleteContacts": return await client.Contacts.DeleteBatchAsync(Read<DeleteContactsInput>(), options, cancellationToken);
            case "mergeContacts": return await client.Contacts.MergeAsync(Id, Read<MergeContactInput>(), options: options, cancellationToken: cancellationToken);
            case "listContactConsentChanges": return await client.Contacts.ListConsentChangesAsync(Id, options: options, cancellationToken: cancellationToken);
            case "getContactExport": return await client.Contacts.ExportAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listContactLists": return await client.Contacts.ListListsAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listContactSegments": return await client.Contacts.ListSegmentsAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listContactTopics": return await client.Contacts.ListTopicsAsync(Id, options: options, cancellationToken: cancellationToken);
            case "setContactTopic": return await client.Contacts.SetTopicAsync(Id, Id, Read<SetContactTopicInput>(), options: options, cancellationToken: cancellationToken);
            case "listContactEvents": return await client.Contacts.ListEventsAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listContactJourneys": return await client.Contacts.ListJourneysAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listContactMessages": return await client.Contacts.ListMessagesAsync(Id, options: options, cancellationToken: cancellationToken);
            case "listPushInstallations": return await client.Contacts.ListInstallationsAsync(Id, options: options, cancellationToken: cancellationToken);
            case "registerPushInstallation": return await client.Contacts.RegisterInstallationAsync(Id, Read<RegisterPushInstallationInput>(), options: options, cancellationToken: cancellationToken);
            case "deletePushInstallation": await client.Contacts.DeleteInstallationAsync(Id, Id, options: options, cancellationToken: cancellationToken); return null;
            case "listLists": return await client.Lists.ListAsync(options: options, cancellationToken: cancellationToken);
            case "createList": return await client.Lists.CreateAsync(Read<CreateListInput>(), options, cancellationToken);
            case "getList": return await client.Lists.GetAsync(Id, options, cancellationToken);
            case "updateList": return await client.Lists.UpdateAsync(Id, Read<UpdateListInput>(), options, cancellationToken);
            case "deleteList": await client.Lists.DeleteAsync(Id, options, cancellationToken); return null;
            case "addListContacts": return await client.Lists.AddContactsAsync(Id, Read<ListContactsInput>(), options, cancellationToken);
            case "deleteListContacts": return await client.Lists.RemoveContactsAsync(Id, Read<ListContactsInput>(), options, cancellationToken);
            case "getListUsage": return await client.Lists.GetUsageAsync(Id, options, cancellationToken);
            case "trackEvent": return await client.Events.TrackAsync(Read<TrackEventInput>(), options, cancellationToken);
            case "trackEventsBatch": return await client.Events.TrackBatchAsync(Read<TrackEventsBatchInput>(), options, cancellationToken);
            case "sendTransactionalMessage": return await client.Transactional.SendAsync(Read<SendTransactionalInput>(), options, cancellationToken);
            case "sendTransactionalMessagesBatch": return await client.Transactional.SendBatchAsync(Read<SendTransactionalMessagesBatchInput>(), options, cancellationToken);
            case "cancelScheduledTransactionalMessage": return await client.Transactional.CancelAsync(Id, options, cancellationToken);
            case "getMessage": return await client.Messages.GetAsync(Id, options, cancellationToken);
            case "getMessageContent": return await client.Messages.GetContentAsync(Id, options, cancellationToken);
            default: return await ExtendedEndpointInvoker.Invoke(client, operation, body, options, cancellationToken);
        }
    }

    internal static void AssertEquivalent(JsonNode? expected, JsonNode? actual)
    {
        if (expected is JsonObject obj)
        {
            var other = Assert.IsType<JsonObject>(actual);
            Assert.Equal(obj.Select(x => x.Key).OrderBy(x => x), other.Select(x => x.Key).OrderBy(x => x));
            foreach (var pair in obj) AssertEquivalent(pair.Value, other[pair.Key]);
        }
        else if (expected is JsonArray array)
        {
            var other = Assert.IsType<JsonArray>(actual); Assert.Equal(array.Count, other.Count);
            for (var i = 0; i < array.Count; i++) AssertEquivalent(array[i], other[i]);
        }
        else if (expected is JsonValue value && value.TryGetValue<string>(out var first) && actual is JsonValue otherValue && otherValue.TryGetValue<string>(out var second) &&
            DateTimeOffset.TryParse(first, out var a) && DateTimeOffset.TryParse(second, out var b)) Assert.Equal(a, b);
        else Assert.True(JsonNode.DeepEquals(expected, actual), $"Expected {expected?.ToJsonString() ?? "null"}, actual {actual?.ToJsonString() ?? "null"}");
    }

    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; return callback(request, cancellationToken); }
    }
}
