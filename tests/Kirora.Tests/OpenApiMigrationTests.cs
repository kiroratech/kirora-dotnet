using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Kirora.Exceptions;
using Kirora.Models;
using Kirora.Serialization;
using static Kirora.Tests.CoreContractTests;

namespace Kirora.Tests;

public sealed class OpenApiMigrationTests
{
    public static IEnumerable<object[]> ChangedSchemas()
    {
        var report = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/migration-api-changes.json")))!;
        return report["structurallyChangedSchemas"]!.AsArray().Select(n => new object[] { n!.GetValue<string>() });
    }

    [Theory]
    [MemberData(nameof(ChangedSchemas))]
    public void ChangedSchemaRequiredMembersMatchTheNewSpecification(string name)
    {
        using var stream = typeof(KiroraClient).Assembly.GetManifestResourceStream("Kirora.openapi.json")!;
        using var document = JsonDocument.Parse(stream);
        var schema = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(name);
        var type = typeof(KiroraClient).Assembly.GetType("Kirora.Models." + name, throwOnError: true)!;
        if (!schema.TryGetProperty("properties", out var properties))
        {
            Assert.NotNull(type.GetCustomAttribute<JsonConverterAttribute>());
            return;
        }
        var required = schema.TryGetProperty("required", out var fields) ? fields.EnumerateArray().Select(f => f.GetString()).ToHashSet() : [];
        foreach (var field in properties.EnumerateObject())
        {
            var property = Assert.Single(type.GetProperties(), p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == field.Name);
            Assert.Equal(required.Contains(field.Name), property.GetCustomAttribute<RequiredMemberAttribute>() is not null);
            Assert.Equal(!required.Contains(field.Name), property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Optional<>));
        }
    }

    [Theory]
    [InlineData("\"all\"")]
    [InlineData("{\"first\":[\"email\",\"push\"]}")]
    [InlineData("{\"first\":[\"push\",\"email\"]}")]
    public void DeliveryUnionRoundTripsTheDocumentedShapes(string json)
    {
        var value = JsonSerializer.Deserialize<TransactionalDeliverInput>(json, Json)!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(JsonSerializer.Serialize(value, Json))));
    }

    [Theory]
    [InlineData("\"first\"")]
    [InlineData("{\"first\":[]}")]
    [InlineData("{\"first\":[\"sms\"]}")]
    [InlineData("null")]
    public async Task InvalidDeliveryIsRejectedBeforeSending(string json)
    {
        var node = Fixture("sendTransactionalMessage")["request"]!.DeepClone();
        node["deliver"] = JsonNode.Parse(json);
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Must not send"));
        using var http = new HttpClient(handler);
        // Invalid union values fail deserialization, or required/null validation before HTTP.
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            SendTransactionalInput body;
            try { body = node.Deserialize<SendTransactionalInput>(Json)!; }
            catch (JsonException e) { throw new ArgumentException("Invalid union", e); }
            await new KiroraClient(http, Options()).Transactional.SendAsync(body);
        });
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task TypedDeliveryPriorityPreservesItsOrderOnTheWire()
    {
        var node = Fixture("sendTransactionalMessage")["request"]!.DeepClone();
        node["deliver"] = new JsonObject { ["first"] = new JsonArray("push", "email") };
        using var handler = new Handler(async (request, token) =>
        {
            var sent = JsonNode.Parse(await request.Content!.ReadAsStringAsync(token))!;
            Assert.True(JsonNode.DeepEquals(node["deliver"], sent["deliver"]));
            return Success(Fixture("sendTransactionalMessage"));
        });
        using var http = new HttpClient(handler);
        await new KiroraClient(http, Options()).Transactional.SendAsync(node.Deserialize<SendTransactionalInput>(Json)!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SocialLinksCanBeOmittedOrExplicitlyEmpty(bool empty)
    {
        var node = JsonNode.Parse("{\"id\":\"social\",\"type\":\"social\"}")!;
        if (empty) node["links"] = new JsonArray();
        var response = node.Deserialize<SocialBlock>(Json)!;
        var input = node.Deserialize<SocialBlockInput>(Json)!;
        Assert.Equal(empty, response.Links.IsSet);
        Assert.Equal(empty, input.Links.IsSet);
        Assert.True(JsonNode.DeepEquals(node, JsonNode.Parse(JsonSerializer.Serialize(input, Json))));
    }

    [Fact]
    public void ObsoleteBrandThemeRouteAndPublicMethodAreRemoved()
    {
        Assert.Null(typeof(Kirora.Resources.TemplatesResource).GetMethod("GetBrandThemeAsync"));
        using var stream = typeof(KiroraClient).Assembly.GetManifestResourceStream("Kirora.openapi.json")!;
        using var document = JsonDocument.Parse(stream);
        Assert.False(document.RootElement.GetProperty("paths").TryGetProperty("/v1/brand-theme", out _));
        Assert.NotNull(typeof(Kirora.Resources.TemplatesResource).GetMethod("GetBrandKitAsync"));
    }

    [Theory]
    [InlineData("getBrandKit")]
    [InlineData("getJourneyActivationCheck")]
    public async Task NewReadsHonorCancellationWithoutSending(string operation)
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Must not send"));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Invoke(new KiroraClient(http, Options()), operation, null, cancellationToken: cts.Token));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task BrandKitRequiredNullableFieldsMayBeNullButNotMissing()
    {
        var fixture = (JsonObject)Fixture("getBrandKit").DeepClone();
        foreach (var field in new[] { "name", "logo", "defaultHeaderId", "defaultFooterId", "updatedAt" }) fixture["response"]!["data"]![field] = null;
        using var handler = new Handler((_, _) => Task.FromResult(Success(fixture)));
        using var http = new HttpClient(handler); var client = new KiroraClient(http, Options());
        var response = await client.Templates.GetBrandKitAsync();
        Assert.Null(response.Data.Name); Assert.Null(response.Data.UpdatedAt); Assert.Null(response.Data.Logo);
        fixture["response"]!["data"]!.AsObject().Remove("updatedAt");
        await Assert.ThrowsAsync<KiroraInvalidResponseException>(() => client.Templates.GetBrandKitAsync());
    }

    [Fact]
    public async Task ReadyJourneyHasNoActivationIssues()
    {
        var fixture = (JsonObject)Fixture("getJourneyActivationCheck").DeepClone();
        fixture["response"]!["data"]!["issues"] = new JsonArray();
        using var handler = new Handler((_, _) => Task.FromResult(Success(fixture)));
        using var http = new HttpClient(handler);
        var result = await new KiroraClient(http, Options()).Journeys.GetActivationCheckAsync(Id);
        Assert.Empty(result.Data.Issues);
    }

    [Theory]
    [InlineData("getBrandKit")]
    [InlineData("getJourneyActivationCheck")]
    public async Task NewReadsUseTheExistingSafeRetryPolicy(string operation)
    {
        var fixture = Fixture(operation); var attempts = 0;
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.False(request.Headers.Contains("Idempotency-Key"));
            return Task.FromResult(++attempts == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") } : Success(fixture));
        });
        using var http = new HttpClient(handler);
        await Invoke(new KiroraClient(http, Options(1)), operation, null);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void TransactionalFallbackLinkIsRequiredAndNullable()
    {
        var fixture = Fixture("sendTransactionalMessage")["response"]!["data"]!["messages"]![0]!;
        var node = fixture.DeepClone();
        node["fallbackFor"] = null;
        var result = node.Deserialize<TransactionalMessage>(Json)!;
        Assert.Null(result.FallbackFor);
        node.AsObject().Remove("fallbackFor");
        Assert.Throws<JsonException>(() => node.Deserialize<TransactionalMessage>(Json));
    }
}
