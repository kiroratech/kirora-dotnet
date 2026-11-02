using System.Globalization;
using System.Net;

namespace Kirora.Http;

/// <summary>The effective API key/workspace request budget and seconds until refill.</summary>
public sealed record KiroraRateLimit(long Limit, long Remaining, long ResetSeconds);

/// <summary>Immutable status, correlation, retry and rate-limit metadata from an HTTP response.</summary>
public sealed record KiroraResponseMetadata(
    HttpStatusCode StatusCode, string? RequestId, TimeSpan? RetryAfter,
    KiroraRateLimit? RateLimit, bool IdempotencyReplay)
{
    internal static KiroraResponseMetadata From(HttpResponseMessage response)
    {
        string? Header(string name) => response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
        static bool Number(string? value, out long result) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) && result >= 0;
        KiroraRateLimit? rate = null;
        if (Number(Header("RateLimit-Limit"), out var limit) && Number(Header("RateLimit-Remaining"), out var remaining) && Number(Header("RateLimit-Reset"), out var reset))
            rate = new(limit, remaining, reset);
        TimeSpan? retry = null;
        var raw = Header("Retry-After");
        if (decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            // Saturate untrusted extreme values: callers will return the refusal rather than wait too early.
            const decimal maxWholeSeconds = 922337203685m;
            retry = seconds >= maxWholeSeconds ? TimeSpan.MaxValue : TimeSpan.FromTicks(decimal.ToInt64(decimal.Ceiling(seconds * TimeSpan.TicksPerSecond)));
        }
        else if (DateTimeOffset.TryParseExact(raw,
            ["r", "dddd, dd-MMM-yy HH:mm:ss 'GMT'", "ddd MMM d HH:mm:ss yyyy", "ddd MMM  d HH:mm:ss yyyy"],
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
        {
            var now = DateTimeOffset.UtcNow;
            retry = date > now ? date - now : TimeSpan.Zero;
        }
        return new(response.StatusCode, Header("X-Request-Id"), retry, rate,
            string.Equals(Header("Idempotency-Replay"), "true", StringComparison.OrdinalIgnoreCase));
    }
}
