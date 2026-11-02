using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Kirora.Serialization;

/// <summary>Validates the JSON Schema constructs used by core request contracts; never rewrites the spec.</summary>
internal static class ContractValidator
{
    private static readonly JsonDocument Spec = Load("Kirora.openapi.json");
    private static readonly JsonDocument Unions = Load("Kirora.core-unions.json");
    private static JsonDocument Load(string name)
    {
        using var stream = typeof(ContractValidator).Assembly.GetManifestResourceStream(name)!;
        return JsonDocument.Parse(stream);
    }

    private static JsonElement Resolve(JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference)) return schema;
        var current = Spec.RootElement;
        foreach (var part in reference.GetString()!.Split('/').Skip(1)) current = current.GetProperty(part.Replace("~1", "/").Replace("~0", "~"));
        return current;
    }

    private static JsonElement Operation(string operationId) => Spec.RootElement.GetProperty("paths").EnumerateObject()
        .SelectMany(path => path.Value.EnumerateObject()).Select(method => method.Value)
        .First(operation => operation.TryGetProperty("operationId", out var id) && id.GetString() == operationId);

    private static JsonElement Element(JsonNode node)
    {
        using var document = JsonDocument.Parse(node.ToJsonString());
        return document.RootElement.Clone();
    }

    internal static void ValidateResponse(string operationId, int status, byte[] bytes)
    {
        var responses = Operation(operationId).GetProperty("responses");
        if (!responses.TryGetProperty(status.ToString(System.Globalization.CultureInfo.InvariantCulture), out var response))
            throw new JsonException("The status is not a documented success response.");
        if (!response.TryGetProperty("content", out var content) || !content.TryGetProperty("application/json", out var json)) return;
        using var document = JsonDocument.Parse(bytes);
        if (!Matches(document.RootElement, json.GetProperty("schema"), false))
            throw new JsonException("The response is missing a required value or has an invalid structure.");
    }

    internal static bool MatchesUnionVariant(string name, int index, JsonElement value) =>
        Matches(value, Unions.RootElement.GetProperty(name)[index], false);

    internal static void Validate(string operationId, JsonNode? body, JsonObject? query)
    {
        var operation = Operation(operationId);
        if (operation.TryGetProperty("requestBody", out var request) &&
            (body is not null || request.TryGetProperty("required", out var bodyRequired) && bodyRequired.GetBoolean()))
        {
            if (body is null) throw new ArgumentException("This operation requires a request body.", nameof(body));
            var value = Element(body);
            if (!Matches(value, request.GetProperty("content").GetProperty("application/json").GetProperty("schema"), true))
                throw new ArgumentException($"The request body does not match the pinned contract for {operationId}.", nameof(body));
        }
        if (operation.TryGetProperty("parameters", out var parameters))
            foreach (var parameter in parameters.EnumerateArray())
            {
                // Shared parameters are headers, never query fields in the pinned core contracts.
                if (parameter.TryGetProperty("$ref", out _) || parameter.GetProperty("in").GetString() != "query") continue;
                var name = parameter.GetProperty("name").GetString()!;
                if (parameter.TryGetProperty("required", out var required) && required.GetBoolean() && (query is null || !query.ContainsKey(name)))
                    throw new ArgumentException($"Query field {name} is required.", nameof(query));
                if (query is not null && query.TryGetPropertyValue(name, out var node) && node is not null &&
                    !Matches(Element(node), parameter.GetProperty("schema"), true))
                    throw new ArgumentException($"Query field {name} does not match the pinned contract.", nameof(query));
            }
    }

    private static bool Matches(JsonElement value, JsonElement schema, bool strict)
    {
        schema = Resolve(schema);
        if (schema.TryGetProperty("type", out var type))
        {
            bool IsType(string? kind) => kind switch
            {
                "null" => value.ValueKind == JsonValueKind.Null,
                "object" => value.ValueKind == JsonValueKind.Object,
                "array" => value.ValueKind == JsonValueKind.Array,
                "string" => value.ValueKind == JsonValueKind.String,
                "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var integer) && decimal.Truncate(integer) == integer,
                "number" => value.ValueKind == JsonValueKind.Number,
                _ => throw new InvalidOperationException("Unsupported schema type.")
            };
            if (type.ValueKind == JsonValueKind.Array ? !type.EnumerateArray().Any(t => IsType(t.GetString())) : !IsType(type.GetString())) return false;
        }
        if (schema.TryGetProperty("const", out var constant) && value.GetRawText() != constant.GetRawText()) return false;
        if (strict && schema.TryGetProperty("enum", out var choices) && !choices.EnumerateArray().Any(x => x.ToString() == value.ToString())) return false;
        foreach (var keyword in new[] { "anyOf", "oneOf", "allOf" })
            if (schema.TryGetProperty(keyword, out var branches))
            {
                var matched = branches.EnumerateArray().Count(branch => Matches(value, branch, strict));
                if (keyword == "anyOf" && matched == 0 || keyword == "oneOf" && matched != 1 || keyword == "allOf" && matched != branches.GetArrayLength()) return false;
            }
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("required", out var required) && required.EnumerateArray().Any(p => !value.TryGetProperty(p.GetString()!, out _))) return false;
            var hasProperties = schema.TryGetProperty("properties", out var properties);
            foreach (var field in value.EnumerateObject())
            {
                if (schema.TryGetProperty("propertyNames", out var names) && strict && !Matches(Element(JsonValue.Create(field.Name)!), names, strict)) return false;
                if (hasProperties && properties.TryGetProperty(field.Name, out var property))
                { if (!Matches(field.Value, property, strict)) return false; }
                else if (schema.TryGetProperty("additionalProperties", out var additional))
                {
                    if (strict && additional.ValueKind == JsonValueKind.False) return false;
                    if (additional.ValueKind == JsonValueKind.Object && !Matches(field.Value, additional, strict)) return false;
                }
            }
            if (strict && schema.TryGetProperty("maxProperties", out var maxp) && value.EnumerateObject().Count() > maxp.GetInt32()) return false;
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (strict && schema.TryGetProperty("minItems", out var min) && value.GetArrayLength() < min.GetInt32()) return false;
            if (strict && schema.TryGetProperty("maxItems", out var max) && value.GetArrayLength() > max.GetInt32()) return false;
            if (schema.TryGetProperty("items", out var items) && value.EnumerateArray().Any(item => !Matches(item, items, strict))) return false;
        }
        if (strict && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString()!;
            var length = text.EnumerateRunes().Count();
            if (schema.TryGetProperty("minLength", out var min) && length < min.GetInt32()) return false;
            if (schema.TryGetProperty("maxLength", out var max) && length > max.GetInt32()) return false;
            if (schema.TryGetProperty("pattern", out var pattern) && !Regex.IsMatch(text, pattern.GetString()!, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))) return false;
            if (schema.TryGetProperty("format", out var format))
            {
                if (format.GetString() == "uuid" && !Guid.TryParseExact(text, "D", out _)) return false;
                if (format.GetString() == "date-time" && !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
                if (format.GetString() == "date" && !DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return false;
            }
        }
        if (strict && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            if (schema.TryGetProperty("minimum", out var min) && number < min.GetDecimal()) return false;
            if (schema.TryGetProperty("maximum", out var max) && number > max.GetDecimal()) return false;
            if (schema.TryGetProperty("exclusiveMinimum", out var emin) && number <= emin.GetDecimal()) return false;
            if (schema.TryGetProperty("exclusiveMaximum", out var emax) && number >= emax.GetDecimal()) return false;
        }
        return true;
    }
}
