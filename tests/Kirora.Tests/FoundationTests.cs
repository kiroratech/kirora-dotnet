using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Kirora.Exceptions;
using Kirora.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kirora.Tests;

public sealed class FoundationTests
{
    private const string ValidResponse = """
        {"data":{"workspace":{"id":"0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d","name":"Test workspace"},"apiKey":{"id":"0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9e","name":"Test key","kind":"integration","source":null,"scopes":["contacts:read"]}}}
        """;

    private static KiroraOptions Options() => new() { ApiKey = "test-only-placeholder", MaxRetries = 0 };
    private static HttpResponseMessage Response(string body = ValidResponse, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SendsDocumentedGetWithBearerAndUnwrapsStronglyTypedData()
    {
        using var handler = new StubHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.kirora.io/v1/me", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("test-only-placeholder", request.Headers.Authorization.Parameter);
            Assert.Contains(request.Headers.Accept, h => h.MediaType == "application/json");
            Assert.Contains("kirora-dotnet/", request.Headers.UserAgent.ToString());
            return Task.FromResult(Response());
        });
        using var http = new HttpClient(handler);
        var caller = await new KiroraClient(http, Options()).GetMeAsync();
        Assert.Equal("Test workspace", caller.Workspace.Name);
        Assert.Equal("integration", caller.ApiKey.Kind);
        Assert.Null(caller.ApiKey.Source);
        Assert.Equal(new[] { "contacts:read" }, caller.ApiKey.Scopes);
    }

    [Fact]
    public async Task SnapshotsOptionsAndWorkspaceViewsDoNotLeakBetweenCalls()
    {
        var seen = new List<(string Key, string? Workspace)>();
        using var handler = new StubHandler((request, _) =>
        {
            lock (seen) seen.Add((request.Headers.Authorization!.Parameter!,
                request.Headers.TryGetValues("Kirora-Workspace", out var v) ? v.Single() : null));
            return Task.FromResult(Response());
        });
        using var http = new HttpClient(handler);
        var options = Options();
        var client = new KiroraClient(http, options);
        options.ApiKey = "changed";
        var first = client.ForWorkspace("tenant-1");
        var second = client.ForWorkspace("tenant-2");
        await Task.WhenAll(first.GetMeAsync(), second.GetMeAsync(), client.GetMeAsync());
        Assert.All(seen, x => Assert.Equal("test-only-placeholder", x.Key));
        Assert.Contains(seen, x => x.Workspace == "tenant-1");
        Assert.Contains(seen, x => x.Workspace == "tenant-2");
        Assert.Contains(seen, x => x.Workspace is null);
        Assert.False(http.DefaultRequestHeaders.Contains("Kirora-Workspace"));
        Assert.Null(http.DefaultRequestHeaders.Authorization);
    }

    [Theory]
    [InlineData(401, typeof(KiroraAuthenticationException))]
    [InlineData(403, typeof(KiroraForbiddenException))]
    [InlineData(404, typeof(KiroraNotFoundException))]
    [InlineData(409, typeof(KiroraConflictException))]
    [InlineData(429, typeof(KiroraRateLimitException))]
    [InlineData(503, typeof(KiroraApiException))]
    public async Task MapsStatusesAndPreservesUnknownErrorCodes(int status, Type expected)
    {
        using var handler = new StubHandler((_, _) =>
        {
            var r = Response("""{"error":{"code":"FUTURE_CODE","message":"Refused","details":{"reason":42},"requestId":"body-id"}}""", (HttpStatusCode)status);
            r.Headers.Add("X-Request-Id", "header-id");
            r.Headers.Add("Retry-After", "12");
            r.Headers.Add("RateLimit-Limit", "60");
            r.Headers.Add("RateLimit-Remaining", "0");
            r.Headers.Add("RateLimit-Reset", "20");
            return Task.FromResult(r);
        });
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAnyAsync<KiroraApiException>(() => new KiroraClient(http, Options()).GetMeAsync());
        Assert.Equal(expected, error.GetType());
        Assert.Equal("FUTURE_CODE", error.Code);
        Assert.Equal("header-id", error.RequestId);
        Assert.Equal(TimeSpan.FromSeconds(12), error.Metadata.RetryAfter);
        Assert.Equal(20, error.Metadata.RateLimit!.ResetSeconds);
        Assert.Equal(42, error.Details!.Value.GetProperty("reason").GetInt32());
    }

    [Fact]
    public async Task PreservesValidFieldErrorsAndUsesBodyRequestIdFallback()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Response("""
            {"error":{"code":"VALIDATION_ERROR","message":"Invalid","requestId":"body-id","errors":[{"field":"email","message":"Invalid email","in":"body","code":"custom"},{"field":42},null]}}
            """, HttpStatusCode.BadRequest)));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraValidationException>(() => new KiroraClient(http, Options()).GetMeAsync());
        Assert.Equal("body-id", error.RequestId);
        Assert.Equal("email", Assert.Single(error.Errors).Field);
    }

    [Fact]
    public async Task NonJsonProxyErrorRetainsStatusWithoutIncludingRawBody()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Response("<html>proxy internals</html>", HttpStatusCode.BadGateway)));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<KiroraApiException>(() => new KiroraClient(http, Options()).GetMeAsync());
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.DoesNotContain("proxy internals", error.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"data\":null}")]
    [InlineData("{\"data\":{}}")]
    [InlineData("[]")]
    public async Task RejectsMalformedOrMissingSuccessPayload(string body)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Response(body)));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => new KiroraClient(http, Options()).GetMeAsync());
    }

    [Fact]
    public async Task ResponseEnumsAllowUnknownFutureStrings()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Response(ValidResponse.Replace("integration", "future-kind"))));
        using var http = new HttpClient(handler);
        Assert.Equal("future-kind", (await new KiroraClient(http, Options()).GetMeAsync()).ApiKey.Kind);
    }

    [Fact]
    public async Task RetriesReadFailuresWithinConfiguredBudget()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Response()));
        handler.Callback = (_, _) => Task.FromResult(handler.Calls < 3 ? Response("", HttpStatusCode.ServiceUnavailable) : Response());
        using var http = new HttpClient(handler);
        var options = Options(); options.MaxRetries = 2; options.MaxRetryDelay = TimeSpan.Zero;
        await new KiroraClient(http, options).GetMeAsync();
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task DoesNotShortenServerRetryAfterBeyondWaitBudget()
    {
        using var handler = new StubHandler((_, _) =>
        {
            var r = Response("", HttpStatusCode.TooManyRequests); r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return Task.FromResult(r);
        });
        using var http = new HttpClient(handler);
        var options = Options(); options.MaxRetries = 3;
        await Assert.ThrowsAsync<KiroraRateLimitException>(() => new KiroraClient(http, options).GetMeAsync());
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RetriesTransientNetworkFailureForRead()
    {
        using var handler = new StubHandler((_, _) => throw new HttpRequestException("offline"));
        using var http = new HttpClient(handler);
        var options = Options(); options.MaxRetries = 1; options.MaxRetryDelay = TimeSpan.Zero;
        await Assert.ThrowsAsync<KiroraTransportException>(() => new KiroraClient(http, options).GetMeAsync());
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task CallerCancellationIsNotRetriedOrWrapped()
    {
        using var handler = new StubHandler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Response(); });
        using var http = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        var task = new KiroraClient(http, Options()).GetMeAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task PerAttemptTimeoutProducesTypedTimeout()
    {
        using var handler = new StubHandler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return Response(); });
        using var http = new HttpClient(handler);
        var options = Options(); options.RequestTimeout = TimeSpan.FromMilliseconds(30);
        await Assert.ThrowsAsync<KiroraTimeoutException>(() => new KiroraClient(http, options).GetMeAsync());
    }

    [Fact]
    public async Task CancellationDuringBodyReadIsPropagated()
    {
        var content = new WaitingContent();
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        using var http = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        var task = new KiroraClient(http, Options()).GetMeAsync(cancellation.Token);
        await content.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task EnforcesBodySizeAndDisposesResponses()
    {
        var content = new TrackedContent(ValidResponse);
        using var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content }));
        using var http = new HttpClient(handler);
        var options = Options(); options.MaxResponseBytes = 10;
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => new KiroraClient(http, options).GetMeAsync());
        Assert.True(content.Disposed);
        Assert.False(handler.Disposed);
    }

    [Fact]
    public async Task DependencyInjectionCreatesUsableClientAndReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddKirora(o => { o.ApiKey = "test-only-placeholder"; o.MaxRetries = 0; });
        builder.ConfigurePrimaryHttpMessageHandler(() => new StubHandler((_, _) => Task.FromResult(Response())));
        using var provider = services.BuildServiceProvider();
        Assert.Equal("Test workspace", (await provider.GetRequiredService<KiroraClient>().GetMeAsync()).Workspace.Name);
    }

    [Fact]
    public void DependencyInjectionRejectsInvalidOptions()
    {
        var services = new ServiceCollection(); services.AddKirora(_ => { });
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<KiroraClient>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad\r\nkey")]
    [InlineData("contains space")]
    public void RejectsInvalidCredentials(string key)
    {
        using var http = new HttpClient();
        var options = Options(); options.ApiKey = key;
        Assert.Throws<ArgumentException>(() => new KiroraClient(http, options));
    }

    [Theory]
    [InlineData("http://api.kirora.io/")]
    [InlineData("https://api.kirora.io/v1/")]
    [InlineData("https://user:password@api.kirora.io/")]
    public void RejectsInsecureOrAmbiguousOrigins(string origin)
    {
        using var http = new HttpClient();
        var options = Options(); options.BaseAddress = new Uri(origin);
        Assert.Throws<ArgumentException>(() => new KiroraClient(http, options));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Callback { get; set; } = callback;
        public int Calls { get; private set; }
        public bool Disposed { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; return Callback(request, cancellationToken); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class TrackedContent(string body) : StringContent(body)
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class WaitingContent : HttpContent
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new NotSupportedException();
        protected override async Task<Stream> CreateContentReadStreamAsync() { await Task.CompletedTask; return new WaitingStream(Started); }
        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) => CreateContentReadStreamAsync();
    }

    private sealed class WaitingStream(TaskCompletionSource started) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { started.TrySetResult(); await Task.Delay(Timeout.Infinite, cancellationToken); return 0; }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
