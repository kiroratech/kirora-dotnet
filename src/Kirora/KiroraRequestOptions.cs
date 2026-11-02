using Kirora.Http;

namespace Kirora;

/// <summary>Per-call controls. Idempotency keys apply only to documented supporting operations.</summary>
public sealed class KiroraRequestOptions
{
    /// <summary>Provides the documented configuration or response value.</summary>
    public string? IdempotencyKey { get; init; }
    /// <summary>Receives immutable response metadata for each HTTP attempt, including API refusals.</summary>
    public Action<KiroraResponseMetadata>? ResponseReceived { get; init; }
}
