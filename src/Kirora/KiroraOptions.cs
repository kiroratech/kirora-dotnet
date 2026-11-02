namespace Kirora;

/// <summary>Configuration copied and validated when a client is constructed.</summary>
public sealed class KiroraOptions
{
    /// <summary>The server API key, supplied by configuration or a secret store. Never log this value.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>The HTTPS API origin; loopback HTTP origins are allowed for local tests.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.kirora.io/");
    /// <summary>The partner tenant external ID sent in Kirora-Workspace, or null for the key workspace.</summary>
    public string? WorkspaceExternalId { get; set; }
    /// <summary>Maximum duration of each HTTP attempt, including response body reads.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Maximum retries after the initial attempt, from zero to ten; defaults to three.</summary>
    public int MaxRetries { get; set; } = 3;
    /// <summary>Largest permitted individual retry delay. Longer server delays are surfaced as failures.</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Maximum buffered JSON or error response bytes; streaming CSV success bodies are not buffered.</summary>
    public int MaxResponseBytes { get; set; } = 4 * 1024 * 1024;

    internal KiroraOptions Snapshot()
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey.Any(char.IsWhiteSpace) || ApiKey.Any(char.IsControl))
            throw new ArgumentException("A nonempty API key without whitespace is required.", nameof(ApiKey));
        if (BaseAddress is null || !BaseAddress.IsAbsoluteUri ||
            !(BaseAddress.Scheme == Uri.UriSchemeHttps || BaseAddress.Scheme == Uri.UriSchemeHttp && BaseAddress.IsLoopback) ||
            !string.IsNullOrEmpty(BaseAddress.UserInfo) || !string.IsNullOrEmpty(BaseAddress.Query) ||
            !string.IsNullOrEmpty(BaseAddress.Fragment) || BaseAddress.AbsolutePath != "/")
            throw new ArgumentException("BaseAddress must be an HTTPS origin, or a loopback HTTP origin for testing.", nameof(BaseAddress));
        if (WorkspaceExternalId is not null && (WorkspaceExternalId.Length is < 1 or > 128 ||
            WorkspaceExternalId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not ':' and not '-')))
            throw new ArgumentException("WorkspaceExternalId must match the documented external ID format.", nameof(WorkspaceExternalId));
        if (RequestTimeout <= TimeSpan.Zero || RequestTimeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(RequestTimeout));
        if (MaxRetries is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(MaxRetries));
        if (MaxRetryDelay < TimeSpan.Zero || MaxRetryDelay.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(MaxRetryDelay));
        if (MaxResponseBytes is < 1 or > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(MaxResponseBytes));
        return new KiroraOptions
        {
            ApiKey = ApiKey, BaseAddress = BaseAddress, WorkspaceExternalId = WorkspaceExternalId,
            RequestTimeout = RequestTimeout, MaxRetries = MaxRetries,
            MaxRetryDelay = MaxRetryDelay, MaxResponseBytes = MaxResponseBytes
        };
    }
}
