using System.Net;
using System.Text.Json.Nodes;
using Kirora;
using Kirora.Exceptions;
using Kirora.Models;
using Kirora.Serialization;
using Microsoft.Extensions.DependencyInjection;

if (args.Contains("--require-native") && System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
    throw new InvalidOperationException("Expected a native executable without dynamic code support.");
if (System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault)
    throw new InvalidOperationException("The smoke must run with reflection JSON serialization disabled.");
var fixtures = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "core-examples.json")))!.AsArray();
var services = new ServiceCollection();
services.AddKirora(o => { o.ApiKey = "fake-aot-smoke-credential"; o.MaxRetries = 0; })
    .ConfigurePrimaryHttpMessageHandler(() => new OfflineHandler(fixtures));
using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<KiroraClient>();
var caller = await client.GetMeAsync();
if (caller.Workspace.Id == Guid.Empty) throw new InvalidOperationException("Required caller data missing.");
var contact = await client.Contacts.IdentifyAsync(new IdentifyContactInput { ExternalId = "aot-smoke", FirstName = new Optional<string?>(null) });
if (contact.Data.Id == Guid.Empty) throw new InvalidOperationException("Required contact data missing.");
await client.Events.TrackAsync(new TrackEventInputByExternalId { ExternalId = "aot-smoke", Name = "SDK Smoke" });
SendTransactionalInputEmail email = new SendTransactionalInputEmailStoredTemplate { Template = "order-receipt" };
await client.Transactional.SendAsync(new SendTransactionalInput { To = new TransactionalRecipientInputByExternalId { ExternalId = "aot-smoke" }, Email = email });
await client.Reports.GetRfmSummaryAsync();
await client.Templates.GetAsync("0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d");
await client.Campaigns.GetAsync("0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d");
await client.Segments.GetAsync("0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d");
var itemCount = 0;
await foreach (var _ in client.Contacts.ListAllAsync()) itemCount++;
if (itemCount == 0) throw new InvalidOperationException("Pagination did not decode items.");
using var csv = new MemoryStream();
await client.Messages.DownloadExportToAsync("0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d", csv);
if (csv.Length == 0) throw new InvalidOperationException("CSV missing.");
await client.Assets.UploadAsync(new UploadAssetInput { File = new byte[] { 137, 80, 78, 71 }, Name = "Smoke" }, "smoke.png", "image/png");
try { await client.Messages.GetAsync("00000000-0000-0000-0000-000000000000"); throw new InvalidOperationException("Expected typed refusal."); }
catch (KiroraNotFoundException error) when (error.Code == "NOT_FOUND" && error.Details!.Value.GetProperty("source").GetString() == "smoke") { }
Console.WriteLine("Offline AOT smoke passed: DI, optional fields, request/response unions, recursive content, errors, pagination, multipart and CSV. No live HTTP.");

sealed class OfflineHandler(JsonArray fixtures) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path == "/v1/messages/00000000-0000-0000-0000-000000000000")
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"error":{"code":"NOT_FOUND","message":"Missing","details":{"source":"smoke"}}}""") });
        var operation = path switch
        {
            "/v1/me" => "getMe", "/v1/contacts/identify" => "identifyContact", "/v1/contacts" => "listContacts",
            "/v1/events/track" => "trackEvent", "/v1/transactional/messages" => "sendTransactionalMessage",
            "/v1/reports/rfm" => "getRfmSummary", "/v1/assets" => "uploadAsset",
            _ when path.StartsWith("/v1/templates/", StringComparison.Ordinal) => "getTemplate",
            _ when path.StartsWith("/v1/campaigns/", StringComparison.Ordinal) => "getCampaign",
            _ when path.StartsWith("/v1/segments/", StringComparison.Ordinal) => "getSegment",
            _ when path.EndsWith("/download", StringComparison.Ordinal) => "downloadMessageExport",
            _ => throw new InvalidOperationException("No fake route configured.")
        };
        var fixture = fixtures.First(f => f!["operationId"]!.GetValue<string>() == operation)!;
        var text = operation == "downloadMessageExport" ? fixture["response"]!.GetValue<string>() : fixture["response"]!.ToJsonString();
        return Task.FromResult(new HttpResponseMessage((HttpStatusCode)fixture["status"]!.GetValue<int>()) { Content = new StringContent(text) });
    }
}
