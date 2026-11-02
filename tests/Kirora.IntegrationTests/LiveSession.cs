using Kirora.Exceptions;
using Kirora.Models;
using Xunit.Sdk;

namespace Kirora.IntegrationTests;

internal static class LiveGate
{
    internal static bool IsCi(Func<string, string?> read) =>
        new[] { "CI", "GITHUB_ACTIONS", "TF_BUILD", "GITLAB_CI", "JENKINS_URL", "BUILD_BUILDID" }
        .Any(name => read(name) is { Length: > 0 } value && value != "0" && !value.Equals("false", StringComparison.OrdinalIgnoreCase));

    internal static string? Reason(Func<string, string?> read, bool requireSender = false)
    {
        if (IsCi(read)) return "Live API tests are disabled on CI runners.";
        if (read("KIRORA_RUN_INTEGRATION") != "1" ||
            read("KIRORA_INTEGRATION_AUTHORIZATION") != "I_AUTHORIZE_LIVE_TESTS" ||
            read("KIRORA_INTEGRATION_ISOLATION") != "I_CONFIRM_EXCLUSIVE_EMPTY_WORKSPACE" ||
            string.IsNullOrWhiteSpace(read("KIRORA_API_KEY")) ||
            !Guid.TryParse(read("KIRORA_INTEGRATION_WORKSPACE_ID"), out var id) || id == Guid.Empty)
            return "Live tests require explicit authorization, exclusive workspace isolation, credentials and a workspace ID.";
        if (requireSender && (string.IsNullOrWhiteSpace(read("KIRORA_TEST_FROM")) ||
            read("KIRORA_TEST_RECIPIENT_APPROVAL") != "SDK_GENERATED_EXAMPLE_COM_ONLY"))
            return "Email testing requires an approved sender and explicit approval for generated example.com recipients.";
        return null;
    }

    internal static string? SmokeReason(Func<string, string?> read)
    {
        if (IsCi(read)) return "Live API tests are disabled on CI runners.";
        if (read("KIRORA_RUN_SMOKE") != "1" ||
            read("KIRORA_SMOKE_AUTHORIZATION") != "I_AUTHORIZE_READ_ONLY_SMOKE" ||
            string.IsNullOrWhiteSpace(read("KIRORA_API_KEY")) ||
            !Guid.TryParse(read("KIRORA_INTEGRATION_WORKSPACE_ID"), out var id) || id == Guid.Empty)
            return "Read-only smoke requires its own authorization, credentials and expected workspace ID.";
        return null;
    }
}

internal static class LiveSession
{
    internal static async Task Run(Func<string, string?> read, Func<KiroraClient, CancellationToken, Task> test,
        bool mutations, bool requireSender = false, HttpMessageHandler? handler = null)
    {
        // Snapshot only needed values. Neither credentials nor server-provided strings appear in failures.
        var names = new[] { "CI", "GITHUB_ACTIONS", "TF_BUILD", "GITLAB_CI", "JENKINS_URL", "BUILD_BUILDID",
            "KIRORA_RUN_INTEGRATION", "KIRORA_INTEGRATION_AUTHORIZATION", "KIRORA_INTEGRATION_ISOLATION",
            "KIRORA_API_KEY", "KIRORA_INTEGRATION_WORKSPACE_ID", "KIRORA_TEST_FROM", "KIRORA_TEST_RECIPIENT_APPROVAL",
            "KIRORA_RUN_SMOKE", "KIRORA_SMOKE_AUTHORIZATION" };
        var values = names.ToDictionary(name => name, read);
        string? Configuration(string name) => values.GetValueOrDefault(name);
        var refusal = mutations ? LiveGate.Reason(Configuration, requireSender) : LiveGate.SmokeReason(Configuration);
        if (refusal is not null) throw new XunitException(refusal);
        try
        {
            using var http = new HttpClient(handler ?? new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = Timeout.InfiniteTimeSpan };
            var client = new KiroraClient(http, new KiroraOptions { ApiKey = Configuration("KIRORA_API_KEY")!, MaxRetries = 0 });
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var caller = await client.GetMeAsync(timeout.Token);
            if (caller.Workspace.Id != Guid.Parse(Configuration("KIRORA_INTEGRATION_WORKSPACE_ID")!))
                throw new XunitException("The authenticated workspace does not match the approved workspace.");
            if (mutations) await VerifyNoAutomations(client, timeout.Token);
            await test(client, timeout.Token);
        }
        catch (KiroraApiException error)
        {
            throw new XunitException($"Live API failure: HTTP {(int)error.StatusCode}. Server diagnostic fields are omitted.");
        }
        catch (Exception)
        {
            throw new XunitException("Live validation failed. Check workspace isolation, configuration and cleanup state; sensitive diagnostics are omitted.");
        }
    }

    private static async Task VerifyNoAutomations(KiroraClient client, CancellationToken token)
    {
        // Even draft/paused/archived automations are refused. Do not alter existing automations to make a test pass.
        await foreach (var _ in client.Journeys.ListAllAsync(cancellationToken: token))
            throw new XunitException("Mutation tests require a workspace without any journeys.");
        foreach (var archived in new[] { "false", "true" })
            await foreach (var _ in client.Campaigns.ListAllAsync(new ListCampaignsQuery { Archived = archived }, cancellationToken: token))
                throw new XunitException("Mutation tests require a workspace without any campaigns.");
    }
}

public sealed class LiveSmokeFactAttribute : FactAttribute
{
    public LiveSmokeFactAttribute() => Skip = LiveGate.SmokeReason(Environment.GetEnvironmentVariable);
}

[Trait("Category", "LiveSmoke")]
public sealed class LiveSmokeTests
{
    [LiveSmokeFact]
    public Task AuthenticationAndWorkspaceConnectivity() =>
        LiveSession.Run(Environment.GetEnvironmentVariable, (_, _) => Task.CompletedTask, mutations: false);
}
