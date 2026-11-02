using System.Net;
using System.Text.Json;
using Kirora.Http;
using Kirora.Models;

namespace Kirora.Exceptions;

/// <summary>An API refusal. Unknown server error codes are preserved.</summary>
public class KiroraApiException : Exception
{
    /// <summary>Provides the documented configuration or response value.</summary>
    public string Code { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public HttpStatusCode StatusCode => Metadata.StatusCode;
    /// <summary>Provides the documented configuration or response value.</summary>
    public string? RequestId => Metadata.RequestId;
    /// <summary>Provides the documented configuration or response value.</summary>
    public KiroraResponseMetadata Metadata { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public IReadOnlyList<FieldError> Errors { get; }
    /// <summary>Provides the documented configuration or response value.</summary>
    public JsonElement? Details { get; }

    internal KiroraApiException(string code, string message, KiroraResponseMetadata metadata,
        IReadOnlyList<FieldError> errors, JsonElement? details) : base(message)
    {
        Code = code; Metadata = metadata; Errors = errors; Details = details;
    }

    internal static KiroraApiException Create(string code, string message, KiroraResponseMetadata metadata,
        IReadOnlyList<FieldError> errors, JsonElement? details) => code is "VALIDATION_ERROR" or "DRAFT_INCOMPLETE"
        ? new KiroraValidationException(code, message, metadata, errors, details)
        : metadata.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new KiroraAuthenticationException(code, message, metadata, errors, details),
            HttpStatusCode.Forbidden => new KiroraForbiddenException(code, message, metadata, errors, details),
            HttpStatusCode.NotFound => new KiroraNotFoundException(code, message, metadata, errors, details),
            HttpStatusCode.Conflict => new KiroraConflictException(code, message, metadata, errors, details),
            HttpStatusCode.TooManyRequests => new KiroraRateLimitException(code, message, metadata, errors, details),
            _ => new KiroraApiException(code, message, metadata, errors, details)
        };
}

/// <summary>KiroraAuthenticationException Kirora API contract.</summary>
public sealed class KiroraAuthenticationException : KiroraApiException
{
    internal KiroraAuthenticationException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
/// <summary>KiroraForbiddenException Kirora API contract.</summary>
public sealed class KiroraForbiddenException : KiroraApiException
{
    internal KiroraForbiddenException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
/// <summary>KiroraNotFoundException Kirora API contract.</summary>
public sealed class KiroraNotFoundException : KiroraApiException
{
    internal KiroraNotFoundException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
/// <summary>KiroraConflictException Kirora API contract.</summary>
public sealed class KiroraConflictException : KiroraApiException
{
    internal KiroraConflictException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
/// <summary>KiroraRateLimitException Kirora API contract.</summary>
public sealed class KiroraRateLimitException : KiroraApiException
{
    internal KiroraRateLimitException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
/// <summary>KiroraValidationException Kirora API contract.</summary>
public sealed class KiroraValidationException : KiroraApiException
{
    internal KiroraValidationException(string c, string m, KiroraResponseMetadata r, IReadOnlyList<FieldError> e, JsonElement? d) : base(c, m, r, e, d) { }
}
