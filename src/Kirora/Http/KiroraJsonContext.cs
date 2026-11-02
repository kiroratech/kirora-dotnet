using System.Text.Json.Serialization;
using Kirora.Models;

namespace Kirora.Http;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiCaller))]
[JsonSerializable(typeof(FieldError))]
internal partial class KiroraJsonContext : JsonSerializerContext;
