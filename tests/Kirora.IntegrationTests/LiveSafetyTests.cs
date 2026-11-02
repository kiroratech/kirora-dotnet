using System.Net;
using System.Text.Json.Nodes;
using Xunit.Sdk;

namespace Kirora.IntegrationTests;

public sealed class LiveSafetyTests
{
    private const string Credential = "fake-offline-sensitive-credential";
    private static readonly JsonArray Fixtures = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "core-examples.json")))!.AsArray();
    private static JsonObject Fixture(string operation) => Fixtures.First(f => f!["operationId"]!.GetValue<string>() == operation)!.DeepClone().AsObject();
    private static Dictionary<string, string?> Configuration() => new()
    {
        ["KIRORA_API_KEY"] = Credential,
        ["KIRORA_INTEGRATION_WORKSPACE_ID"] = Fixture("getMe")["response"]!["data"]!["workspace"]!["id"]!.GetValue<string>(),
        ["KIRORA_RUN_INTEGRATION"] = "1",
        ["KIRORA_INTEGRATION_AUTHORIZATION"] = "I_AUTHORIZE_LIVE_TESTS",
        ["KIRORA_INTEGRATION_ISOLATION"] = "I_CONFIRM_EXCLUSIVE_EMPTY_WORKSPACE",
        ["KIRORA_TEST_FROM"] = "sender@example.com",
        ["KIRORA_TEST_RECIPIENT_APPROVAL"] = "SDK_GENERATED_EXAMPLE_COM_ONLY",
        ["KIRORA_RUN_SMOKE"] = "1",
        ["KIRORA_SMOKE_AUTHORIZATION"] = "I_AUTHORIZE_READ_ONLY_SMOKE"
    };

    [Theory]
    [InlineData("CI")]
    [InlineData("GITHUB_ACTIONS")]
    [InlineData("TF_BUILD")]
    [InlineData("GITLAB_CI")]
    [InlineData("JENKINS_URL")]
    [InlineData("BUILD_BUILDID")]
    public void AllOptInsStillRefuseCi(string marker)
    {
        var config = Configuration(); config[marker] = "true";
        Assert.NotNull(LiveGate.Reason(config.GetValueOrDefault, requireSender: true));
        Assert.NotNull(LiveGate.SmokeReason(config.GetValueOrDefault));
    }

    [Theory]
    [InlineData("KIRORA_RUN_SMOKE")]
    [InlineData("KIRORA_SMOKE_AUTHORIZATION")]
    [InlineData("KIRORA_API_KEY")]
    [InlineData("KIRORA_INTEGRATION_WORKSPACE_ID")]
    public void SmokeRequiresEachOfItsOwnGates(string missing)
    {
        var config = Configuration(); config.Remove(missing);
        Assert.NotNull(LiveGate.SmokeReason(config.GetValueOrDefault));
    }

    [Fact]
    public void MutationAuthorizationDoesNotAuthorizeSmoke()
    {
        var config = Configuration(); config.Remove("KIRORA_RUN_SMOKE"); config.Remove("KIRORA_SMOKE_AUTHORIZATION");
        Assert.Null(LiveGate.Reason(config.GetValueOrDefault));
        Assert.NotNull(LiveGate.SmokeReason(config.GetValueOrDefault));
    }

    [Fact]
    public void EmailRequiresExplicitGeneratedRecipientApproval()
    {
        var config = Configuration(); config.Remove("KIRORA_TEST_RECIPIENT_APPROVAL");
        Assert.Null(LiveGate.Reason(config.GetValueOrDefault));
        Assert.NotNull(LiveGate.Reason(config.GetValueOrDefault, requireSender: true));
    }

    [Fact]
    public async Task ReadOnlySmokeSendsOnlyOneGetMeRequest()
    {
        var config = Configuration(); var paths = new List<string>();
        using var handler = new Handler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Null(request.Content);
            paths.Add(request.RequestUri!.AbsolutePath); return Response(Fixture("getMe"));
        });
        await LiveSession.Run(config.GetValueOrDefault, (_, _) => Task.CompletedTask, mutations: false, handler: handler);
        Assert.Equal(new[] { "/v1/me" }, paths);
    }

    [Theory]
    [InlineData("journey")]
    [InlineData("campaign")]
    [InlineData("archivedCampaign")]
    public async Task AnyAutomationPreventsTheMutationCallback(string present)
    {
        var config = Configuration(); var invoked = false;
        using var handler = new Handler(request => PreflightResponse(request, present));
        await Assert.ThrowsAsync<XunitException>(() => LiveSession.Run(config.GetValueOrDefault,
            (_, _) => { invoked = true; return Task.CompletedTask; }, mutations: true, handler: handler));
        Assert.False(invoked);
    }

    [Fact]
    public async Task EmptyWorkspaceChecksBothCampaignArchivesBeforeMutation()
    {
        var config = Configuration(); var paths = new List<string>(); var invoked = false;
        using var handler = new Handler(request => { paths.Add(request.RequestUri!.PathAndQuery); return PreflightResponse(request, "none"); });
        await LiveSession.Run(config.GetValueOrDefault, (_, _) => { invoked = true; return Task.CompletedTask; }, mutations: true, handler: handler);
        Assert.True(invoked); Assert.Equal(4, paths.Count);
        Assert.Contains(paths, path => path.Contains("archived=true", StringComparison.Ordinal));
        Assert.Contains(paths, path => path.Contains("archived=false", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WorkspaceMismatchPreventsMutation()
    {
        var config = Configuration(); config["KIRORA_INTEGRATION_WORKSPACE_ID"] = Guid.NewGuid().ToString(); var invoked = false;
        using var handler = new Handler(_ => Response(Fixture("getMe")));
        await Assert.ThrowsAsync<XunitException>(() => LiveSession.Run(config.GetValueOrDefault,
            (_, _) => { invoked = true; return Task.CompletedTask; }, mutations: true, handler: handler));
        Assert.False(invoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailureOutputDoesNotEchoCredentialsOrServerFields(bool server)
    {
        var config = Configuration();
        using var handler = new Handler(_ => server
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(new JsonObject { ["error"] = new JsonObject { ["code"] = Credential, ["message"] = Credential, ["details"] = new JsonObject { ["secret"] = Credential } } }.ToJsonString()) }
            : Response(Fixture("getMe")));
        var failure = await Assert.ThrowsAsync<XunitException>(() => LiveSession.Run(config.GetValueOrDefault,
            (_, _) => throw new InvalidOperationException(Credential), mutations: false, handler: handler));
        Assert.DoesNotContain(Credential, failure.ToString(), StringComparison.Ordinal);
        Assert.Null(failure.InnerException);
    }

    private static HttpResponseMessage PreflightResponse(HttpRequestMessage request, string present)
    {
        Assert.Equal(HttpMethod.Get, request.Method); Assert.Null(request.Content);
        var path = request.RequestUri!.AbsolutePath;
        if (path == "/v1/me") return Response(Fixture("getMe"));
        var journey = path == "/v1/journeys";
        Assert.True(journey || path == "/v1/campaigns");
        var fixture = Fixture(journey ? "listJourneys" : "listCampaigns");
        var keep = journey ? present == "journey" : present == "campaign" || present == "archivedCampaign" && request.RequestUri.Query.Contains("archived=true", StringComparison.Ordinal);
        if (!keep) fixture["response"]!["data"] = new JsonArray();
        fixture["response"]!["meta"]!["pagination"]!["nextCursor"] = null;
        return Response(fixture);
    }
    private static HttpResponseMessage Response(JsonObject fixture) => new((HttpStatusCode)fixture["status"]!.GetValue<int>()) { Content = new StringContent(fixture["response"]!.ToJsonString()) };
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
