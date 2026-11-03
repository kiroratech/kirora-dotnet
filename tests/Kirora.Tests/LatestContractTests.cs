using System.Text.Json;
using System.Text.Json.Nodes;
using Kirora.Models;
using static Kirora.Tests.CoreContractTests;

namespace Kirora.Tests;

public sealed class LatestContractTests
{
    [Fact]
    public async Task HtmlTemplateCreationUsesNewDiscriminatorAndTypedModel()
    {
        using var handler = new Handler(async (request, token) =>
        {
            Assert.Equal("/v1/templates", request.RequestUri!.AbsolutePath);
            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(token))!;
            Assert.Equal("html", body["kind"]!.GetValue<string>());
            Assert.Equal("<p>Hello</p>", body["html"]!.GetValue<string>());
            Assert.False(body.AsObject().ContainsKey("blocks"));
            return Success(Fixture("createTemplate"));
        });
        using var http = new HttpClient(handler);
        CreateTemplateInput body = new CreateHtmlTemplateInput { Kind = "html", Name = "SDK HTML", Html = "<p>Hello</p>" };
        await new KiroraClient(http, Options()).Templates.CreateAsync(body);
    }

    [Fact]
    public void LegacyDesignWireShapeStillSelectsDesignVariant()
    {
        var input = Fixture("createTemplate", "welcome")["request"]!.DeepClone();
        input.AsObject().Remove("kind");
        var union = input.Deserialize<CreateTemplateInput>(Json)!;
        Assert.IsType<CreateDesignTemplateInput>(union.Value);
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(JsonSerializer.Serialize(union, Json))));
    }

    [Fact]
    public void TemplateResponsesCanRepresentHtmlAndDesign()
    {
        var data = Fixture("getTemplate")["response"]!["data"]!.DeepClone();
        var design = data.Deserialize<Template>(Json)!;
        Assert.IsType<DesignTemplate>(design.Value);
        // Build a response using exactly the required HTML properties from the embedded official schema.
        using var stream = typeof(KiroraClient).Assembly.GetManifestResourceStream("Kirora.openapi.json")!;
        using var spec = JsonDocument.Parse(stream);
        var required = spec.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("HtmlTemplate").GetProperty("required");
        var html = new JsonObject();
        foreach (var name in required.EnumerateArray().Select(v => v.GetString()!))
            html[name] = name == "kind" ? JsonValue.Create("html") : name == "html" ? JsonValue.Create("<p>HTML</p>") : data[name]?.DeepClone();
        var typed = html.Deserialize<Template>(Json)!;
        Assert.IsType<HtmlTemplate>(typed.Value);
        AssertEquivalent(html, JsonNode.Parse(JsonSerializer.Serialize(typed, Json)));
    }

    [Theory]
    [InlineData("{\"kind\":\"html\",\"name\":\"Test\"}")]
    [InlineData("{\"kind\":\"unknown\",\"name\":\"Test\",\"html\":\"x\"}")]
    public void InvalidTemplateUnionFailsDeserialization(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateTemplateInput>(json, Json));

    [Theory]
    [InlineData("{\"source\":\"fixed\",\"value\":\"campaign\"}")]
    [InlineData("{\"source\":\"link\",\"param\":\"utm_campaign\"}")]
    public void HiddenFieldVariantsRoundTrip(string json)
    {
        var value = JsonSerializer.Deserialize<FormHiddenValueInput>(json, Json)!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(JsonSerializer.Serialize(value, Json))));
    }

    [Theory]
    [InlineData("\"constant\"")]
    [InlineData("{\"fromParam\":\"utm_campaign\"}")]
    public void RemovedHiddenFieldShapesAreRejected(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FormHiddenValueInput>(json, Json));

    [Fact]
    public void TopicsUseObjectsAndScalarSubscriptionIsOptional()
    {
        var choice = new TopicChoiceInput { Channels = new Dictionary<string, bool> { ["email"] = false } };
        var contact = new IdentifyContactInput { ExternalId = "sdk-contract", Topics = new Dictionary<string, TopicChoiceInput> { [Id] = choice } };
        var node = JsonSerializer.SerializeToNode(contact, Json)!;
        Assert.False(node["topics"]![Id]!.AsObject().ContainsKey("subscribed"));
        Assert.False(node["topics"]![Id]!["channels"]!["email"]!.GetValue<bool>());
        var update = new SetContactTopicInput { Subscribed = true, Channels = new Dictionary<string, bool> { ["push"] = false } };
        var body = JsonSerializer.SerializeToNode(update, Json)!;
        Assert.True(body["subscribed"]!.GetValue<bool>());
        Assert.False(body["channels"]!["push"]!.GetValue<bool>());
    }

    [Fact]
    public void StringFilterValuesAndListValuesRemainDistinct()
    {
        var filter = new AttributeConditionInput { Type = "attribute", Field = "attributes.tier", Operator = "in", Values = new[] { "gold", "silver" } };
        var node = JsonSerializer.SerializeToNode(filter, Json)!;
        Assert.False(node.AsObject().ContainsKey("value"));
        Assert.Equal(2, node["values"]!.AsArray().Count);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AttributeConditionInput>("{\"type\":\"attribute\",\"field\":\"attributes.count\",\"operator\":\"equals\",\"value\":3}", Json));
    }

    [Fact]
    public void DraftVersionAndFormValuesUseNewFields()
    {
        var render = new RenderTemplateInput { Draft = true, Context = "marketing", Version = 2 };
        var node = JsonSerializer.SerializeToNode(render, Json)!;
        Assert.True(node["draft"]!.GetValue<bool>()); Assert.Equal(2, node["version"]!.GetValue<int>());
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RenderTemplateInput>("{\"version\":\"draft\"}", Json));
        var form = new SubmitFormInput { Fields = new Dictionary<string, string> { ["name"] = "SDK" }, Choices = new Dictionary<string, IReadOnlyList<string>> { ["interests"] = new[] { "sdk" } } };
        var serialized = JsonSerializer.SerializeToNode(form, Json)!;
        Assert.Equal("sdk", serialized["choices"]!["interests"]![0]!.GetValue<string>());
    }

    [Fact]
    public void FixedFormWidthUsesSeparatePixelField()
    {
        var theme = JsonSerializer.Deserialize<FormThemeInput>("{\"inputs\":{\"width\":\"fixed\",\"widthPx\":320}}", Json)!;
        Assert.Equal("fixed", theme.Inputs.Value.Width.Value);
        Assert.Equal(320, theme.Inputs.Value.WidthPx.Value);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FormThemeInput>("{\"inputs\":{\"width\":320}}", Json));
    }

    [Theory]
    [InlineData(199)]
    [InlineData(721)]
    public async Task PixelWidthRangeIsValidatedBeforeHttp(int width)
    {
        var node = Fixture("createForm")["request"]!.DeepClone();
        node["theme"] = JsonNode.Parse("{\"inputs\":{\"width\":\"fixed\",\"widthPx\":" + width + "}}");
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Must not send"));
        using var http = new HttpClient(handler);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new KiroraClient(http, Options()).Forms.CreateAsync(node.Deserialize<CreateFormInput>(Json)!));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void RemovedDeliverTypesAreNotPretendedToBeSupported()
    {
        Assert.Null(typeof(SendTransactionalInput).GetProperty("Deliver"));
        Assert.Null(typeof(KiroraClient).Assembly.GetType("Kirora.Models.TransactionalDeliverInput"));
        Assert.NotNull(typeof(SendTransactionalInput).GetProperty("FallbackOrder"));
    }
}
