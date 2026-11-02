using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Kirora.Serialization;

/// <summary>A field that can be omitted or explicitly supplied, including null where the API permits it.</summary>
/// <typeparam name="T">The documented field value type.</typeparam>
[JsonConverter(typeof(OptionalConverterFactory))]
public readonly struct Optional<T>
{
    /// <summary>Whether this field was supplied. False means it is omitted from generated request JSON.</summary>
    public bool IsSet { get; }
    /// <summary>The supplied value. Throws InvalidOperationException when the field was omitted.</summary>
    public T Value => IsSet ? _value! : throw new InvalidOperationException("The field was omitted.");
    private readonly T? _value;
    /// <summary>Creates a supplied field with the given value, including explicit null.</summary>
    public Optional(T value) { IsSet = true; _value = value; }
    /// <summary>Converts a value to a supplied field.</summary>
    public static implicit operator Optional<T>(T value) => new(value);
}

internal sealed class OptionalConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The reflection fallback is guarded by RuntimeFeature.IsDynamicCodeSupported. Native AOT uses the statically generated SDK converter registry and rejects unregistered consumer types.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only the JIT-only fallback for consumer-defined Optional types uses reflection. SDK types have generated converter roots.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = GeneratedOptionalConverters.Create(typeToConvert);
        if (converter is not null) return converter;
        if (RuntimeFeature.IsDynamicCodeSupported) return CreateJitConverter(typeToConvert);
        throw new NotSupportedException("This consumer-defined Optional type needs an explicitly registered converter in Native AOT.");
    }

    [RequiresDynamicCode("Consumer-defined Optional types use a JIT-only reflection fallback.")]
    [RequiresUnreferencedCode("Consumer-defined Optional types need their JSON metadata preserved by the consumer.")]
    private static JsonConverter CreateJitConverter(Type type) =>
        (JsonConverter)Activator.CreateInstance(typeof(OptionalJsonConverter<>).MakeGenericType(type.GetGenericArguments()))!;
}

internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public OptionalJsonConverter() { }
    public override bool HandleNull => true;
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)))!);
    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
}
