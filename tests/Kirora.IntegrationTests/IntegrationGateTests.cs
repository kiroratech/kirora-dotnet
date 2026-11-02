namespace Kirora.IntegrationTests;

public sealed class IntegrationGateTests
{
    private static Dictionary<string, string?> Configuration() => new()
    {
        ["KIRORA_RUN_INTEGRATION"] = "1",
        ["KIRORA_INTEGRATION_AUTHORIZATION"] = "I_AUTHORIZE_LIVE_TESTS",
        ["KIRORA_API_KEY"] = "fake-unit-test-credential",
        ["KIRORA_INTEGRATION_WORKSPACE_ID"] = "0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d",
        ["KIRORA_TEST_FROM"] = "sender@example.com"
    };

    [Theory]
    [InlineData("KIRORA_RUN_INTEGRATION")]
    [InlineData("KIRORA_INTEGRATION_AUTHORIZATION")]
    [InlineData("KIRORA_API_KEY")]
    [InlineData("KIRORA_INTEGRATION_WORKSPACE_ID")]
    public void MissingRequirementAlwaysSkips(string missing)
    {
        var configuration = Configuration(); configuration.Remove(missing);
        Assert.NotNull(LiveGate.Reason(key => configuration.GetValueOrDefault(key)));
    }

    [Fact]
    public void EmailNeedsSenderEvenWhenOtherRequirementsArePresent()
    {
        var configuration = Configuration(); configuration.Remove("KIRORA_TEST_FROM");
        Assert.Null(LiveGate.Reason(key => configuration.GetValueOrDefault(key)));
        Assert.NotNull(LiveGate.Reason(key => configuration.GetValueOrDefault(key), requireSender: true));
    }

    [Fact]
    public void FullyConfiguredGatePassesWithoutMakingAnyNetworkCall()
    {
        var configuration = Configuration();
        Assert.Null(LiveGate.Reason(key => configuration.GetValueOrDefault(key), requireSender: true));
    }
}
