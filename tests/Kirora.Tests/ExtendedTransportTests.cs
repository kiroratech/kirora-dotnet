using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora.Exceptions;
using Kirora.Models;
using static Kirora.Tests.CoreContractTests;

namespace Kirora.Tests;

public sealed class ExtendedTransportTests
{
    [Fact]
    public async Task CsvExportStreamsWithoutJsonLimitAndKeepsDestinationOpen()
    {
        var csv = new string('x', 32768) + "\r\n";
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("text/csv", request.Headers.Accept.Single().MediaType);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(csv, Encoding.UTF8, "text/csv") });
        });
        using var http = new HttpClient(handler); var options = Options(); options.MaxResponseBytes = 16;
        using var destination = new MemoryStream();
        await new KiroraClient(http, options).Messages.DownloadExportToAsync(Id, destination);
        Assert.True(destination.CanWrite); Assert.Equal(csv, Encoding.UTF8.GetString(destination.ToArray())); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task PartiallyWrittenCsvIsNotRetried()
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("id\r\nrecord\r\n") }));
        using var http = new HttpClient(handler); using var destination = new FailingDestination();
        await Assert.ThrowsAsync<KiroraTransportException>(() => new KiroraClient(http, Options(3)).Messages.DownloadExportToAsync(Id, destination));
        Assert.Equal(1, handler.Calls); Assert.True(destination.CanWrite);
    }

    [Fact]
    public async Task CsvCancellationIsPropagatedWithoutDisposingDestination()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new Handler((_, _) => { cancellation.Cancel(); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("header") }); });
        using var http = new HttpClient(handler); using var destination = new MemoryStream();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new KiroraClient(http, Options(3)).Messages.DownloadExportToAsync(Id, destination, cancellationToken: cancellation.Token));
        Assert.Equal(1, handler.Calls); Assert.True(destination.CanWrite);
    }

    [Fact]
    public async Task MultipartReplayKeepsBoundaryBytesAndIdempotencyStable()
    {
        var fixture = Fixture("uploadAsset"); var bodies = new List<byte[]>(); var keys = new List<string>();
        using var handler = new Handler(async (request, token) =>
        {
            bodies.Add(await request.Content!.ReadAsByteArrayAsync(token)); keys.Add(request.Headers.GetValues("Idempotency-Key").Single());
            return bodies.Count == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") } : Success(fixture);
        });
        using var http = new HttpClient(handler);
        var input = fixture["request"]!.Deserialize<UploadAssetInput>(Json)!; var copy = input.File.ToArray();
        await new KiroraClient(http, Options(1)).Assets.UploadAsync(input, "pixel.png", "image/png", new() { IdempotencyKey = "asset-test-call" });
        Assert.Equal(2, bodies.Count); Assert.Equal(bodies[0], bodies[1]); Assert.Equal(keys[0], keys[1]); Assert.Equal(copy, input.File);
    }

    [Theory]
    [InlineData("application/octet-stream", "image.png", 1)]
    [InlineData("image/png", "../image.png", 1)]
    [InlineData("image/png", "image.png", 2097153)]
    public async Task MultipartRejectsUnsupportedMimeFilenameAndOversizeBeforeHttp(string mime, string filename, int length)
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException()); using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => new KiroraClient(http, Options()).Assets.UploadAsync(new UploadAssetInput { File = new byte[length] }, filename, mime));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task OptionalBodyOperationCanSendNoBody()
    {
        using var handler = new Handler((request, _) => { Assert.Null(request.Content); return Task.FromResult(Success(Fixture("duplicateCampaign"))); });
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options()).Campaigns.DuplicateAsync(Id);
    }

    [Fact]
    public async Task RequiredAnalyticsQueryIsValidatedBeforeHttp()
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException()); using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => new KiroraClient(http, Options()).Events.GetSeriesAsync(null!));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task AnalyticsQuerySerializesRequiredNamesAndIntervals()
    {
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("?name=Order%20Completed&interval=day", request.RequestUri!.Query);
            return Task.FromResult(Success(Fixture("getEventSeries")));
        });
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options()).Events.GetSeriesAsync(new GetEventSeriesQuery { Name = "Order Completed", Interval = "day" });
    }

    [Fact]
    public async Task UnsafeWritesNeverChangeKeyAfterAmbiguousFailure()
    {
        var keys = new List<string>();
        using var handler = new Handler((request, _) => { keys.Add(request.Headers.GetValues("Idempotency-Key").Single()); throw new HttpRequestException("Response lost"); });
        using var http = new HttpClient(handler);
        var input = Fixture("createTemplate")["request"]!.Deserialize<CreateTemplateInput>(Json)!;
        await Assert.ThrowsAsync<KiroraTransportException>(() => new KiroraClient(http, Options(1)).Templates.CreateAsync(input));
        Assert.Equal(2, keys.Count); Assert.Equal(keys[0], keys[1]);
    }

    [Fact]
    public void EveryResourceAsyncMethodHasCancellationToken()
    {
        foreach (var type in typeof(KiroraClient).Assembly.GetExportedTypes().Where(t => t.Namespace == "Kirora.Resources"))
            foreach (var method in type.GetMethods().Where(m => m.Name.EndsWith("Async", StringComparison.Ordinal)))
                Assert.Equal(typeof(CancellationToken), method.GetParameters().Last().ParameterType);
    }

    private sealed class FailingDestination : MemoryStream
    {
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new IOException("Partial write");
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("Partial write");
    }
}
