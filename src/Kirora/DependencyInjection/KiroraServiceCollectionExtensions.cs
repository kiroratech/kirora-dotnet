using Kirora;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>KiroraServiceCollectionExtensions Kirora API contract.</summary>
public static class KiroraServiceCollectionExtensions
{
    /// <summary>Registers a factory-managed typed client and validates configuration on host startup.</summary>
    public static IHttpClientBuilder AddKirora(this IServiceCollection services, Action<KiroraOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<KiroraOptions>().Configure(configure)
            .Validate(options =>
            {
                try { options.Snapshot(); return true; }
                catch (ArgumentException) { return false; }
            }, "Kirora configuration is invalid. Check credentials, origin and limits.")
            .ValidateOnStart();
        return services.AddHttpClient("Kirora", (provider, http) => http.Timeout = Timeout.InfiniteTimeSpan)
            // Factory logging includes request URIs, which can contain contact identifiers.
            // Applications can deliberately add a redacting logger through the returned builder.
            .RemoveAllLoggers()
            .RedactLoggedHeaders(_ => true)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false, UseCookies = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddTypedClient<KiroraClient>((http, provider) => new KiroraClient(http, provider.GetRequiredService<IOptions<KiroraOptions>>().Value));
    }
}
