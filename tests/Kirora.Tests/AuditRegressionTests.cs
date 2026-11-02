using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora.Exceptions;
using Kirora.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Kirora.Tests.CoreContractTests;

namespace Kirora.Tests;

public sealed class AuditRegressionTests
{
    [Theory]
    [InlineData("http")]
    [InlineData("io")]
    [InlineData("cancel")]
    [InlineData("api")]
    public async Task CallbackFailuresArePreservedWithoutRetries(string kind)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Success(Fixture("trackEvent"))));
        using var http = new HttpClient(handler);
        Exception original = kind switch
        {
            "http" => new HttpRequestException("Callback failed"),
            "io" => new IOException("Callback failed"),
            "cancel" => new OperationCanceledException("Callback canceled"),
            _ => new InvalidOperationException("Callback failed")
        };
        var actual = await Assert.ThrowsAnyAsync<Exception>(() => Invoke(new KiroraClient(http, Options(3)), "trackEvent", Fixture("trackEvent")["request"],
            new() { ResponseReceived = _ => throw original }));
        Assert.Same(original, actual); Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("listContacts", "data")]
    [InlineData("listContacts", "meta")]
    [InlineData("getContact", "data")]
    public async Task ExplicitNullRequiredResponseValuesProduceTypedResponseError(string operation, string field)
    {
        var body = Fixture(operation)["response"]!.DeepClone(); body[field] = null;
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) };
            response.Headers.Add("X-Request-Id", "req-invalid");return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => Invoke(new KiroraClient(http, Options()), operation, null));
        Assert.Equal("req-invalid", error.Metadata.RequestId);
    }

    [Fact]
    public async Task NestedRequiredNullAndLegacyCallerNullAreRejected()
    {
        var body = Fixture("getContact")["response"]!.DeepClone(); body["data"]!["consent"] = null;
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) }));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => new KiroraClient(http, Options()).Contacts.GetAsync(Id));
        body = Fixture("getMe")["response"]!.DeepClone(); body["data"]!["workspace"] = null;
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => new KiroraClient(http, Options()).GetMeAsync());
    }

    [Fact]
    public async Task CursorLoopRetainsLastActualRequestId()
    {
        var body = Fixture("listLists")["response"]!.DeepClone();body["data"] = new JsonArray();body["meta"]!["pagination"]!["nextCursor"] = "same";
        var count = 0;
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) };
            response.Headers.Add("X-Request-Id", "request-" + ++count);return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraInvalidResponseException>(async () =>
        { await foreach (var _ in new KiroraClient(http, Options()).Lists.ListAllAsync()) { } });
        Assert.Equal("request-2", error.Metadata.RequestId);
    }

    [Theory]
    [InlineData(405, "METHOD_NOT_ALLOWED")]
    [InlineData(413, "PAYLOAD_TOO_LARGE")]
    [InlineData(415, "UNSUPPORTED_MEDIA_TYPE")]
    public async Task ProxyStatusFallbackUsesDocumentedErrorCode(int status, string expected)
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("proxy") }));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraApiException>(() => new KiroraClient(http, Options()).GetMeAsync());
        Assert.Equal(expected, error.Code);
    }

    [Fact]
    public async Task HugeRetryAfterIsSaturatedAndNeverRetriedEarly()
    {
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{}") };
            response.Headers.TryAddWithoutValidation("Retry-After", "79228162514264337593543950335");return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraRateLimitException>(() => new KiroraClient(http, Options(3)).GetMeAsync());
        Assert.Equal(TimeSpan.MaxValue, error.Metadata.RetryAfter);Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("r")]
    [InlineData("dddd, dd-MMM-yy HH:mm:ss 'GMT'")]
    [InlineData("ddd MMM d HH:mm:ss yyyy")]
    public async Task AllHttpDateRetryAfterFormatsAreRecognized(string format)
    {
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{}") };
            response.Headers.TryAddWithoutValidation("Retry-After", DateTimeOffset.UtcNow.AddMinutes(5).ToString(format, System.Globalization.CultureInfo.InvariantCulture));return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraRateLimitException>(() => new KiroraClient(http, Options(1)).GetMeAsync());
        Assert.True(error.Metadata.RetryAfter > TimeSpan.FromMinutes(3));Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public void ConsumerDefinedOptionalRetainsJitSerializationBehavior()
    {
        var value = new Optional<int>(123);
        var json = JsonSerializer.Serialize(value);
        Assert.Equal("123", json);Assert.Equal(123,JsonSerializer.Deserialize<Optional<int>>(json).Value);
    }

    [Fact]
    public async Task DefaultDiLoggingNeverEmitsCredentialsOrContactUris()
    {
        var provider = new CaptureLogs(); var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(provider));
        services.AddKirora(o => { o.ApiKey = "credential-must-not-appear"; o.MaxRetries = 0; })
            .ConfigurePrimaryHttpMessageHandler(() => new Handler((_, _) => Task.FromResult(Success(Fixture("getContact")))));
        using var container = services.BuildServiceProvider();
        await container.GetRequiredService<KiroraClient>().Contacts.GetAsync("private-contact@example.com", new Kirora.Models.GetContactQuery { IdType = "email" });
        Assert.DoesNotContain(provider.Messages, message => message.Contains("credential-must-not-appear",StringComparison.Ordinal) || message.Contains("private-contact",StringComparison.Ordinal));
    }

    [Fact]
    public async Task RetryAfterCannotOutliveDocumentedIdempotencyWindow()
    {
        using var handler = new Handler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") };
            response.Headers.TryAddWithoutValidation("Retry-After", "86400");return Task.FromResult(response);
        });
        using var http = new HttpClient(handler); var options = Options(1); options.MaxRetryDelay = TimeSpan.FromDays(2);
        // The deadline makes a regression fail promptly instead of sleeping for a day.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<KiroraApiException>(() => Invoke(new KiroraClient(http, options), "trackEvent", Fixture("trackEvent")["request"], cancellationToken: timeout.Token));
        Assert.Equal(1,handler.Calls);
    }

    private sealed class CaptureLogs : ILoggerProvider
    {
        public List<string> Messages { get; } = [];
        public ILogger CreateLogger(string categoryName) => new Capture(Messages);
        public void Dispose() { }
        private sealed class Capture(List<string> messages) : ILogger
        {
            public bool IsEnabled(LogLevel logLevel) => true;
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Add(formatter(state,exception));
        }
    }
}
