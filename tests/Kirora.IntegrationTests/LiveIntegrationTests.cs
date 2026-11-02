using Kirora.Exceptions;
using Kirora.Models;
using Xunit.Sdk;

namespace Kirora.IntegrationTests;

/// <summary>Discovered but skipped unless credentials, a dedicated workspace and explicit authorization are configured.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        Skip = LiveGate.Reason(Environment.GetEnvironmentVariable);
    }
}

public sealed class LiveEmailFactAttribute : FactAttribute
{
    public LiveEmailFactAttribute()
    {
        Skip = LiveGate.Reason(Environment.GetEnvironmentVariable, requireSender: true);
    }
}

[Trait("Category", "LiveIntegration")]
public sealed class LiveIntegrationTests
{
    [LiveFact]
    public Task ContactCreationAndRetrieval() => Run(async (client, token) =>
    {
        var external = "sdk-integration-" + Guid.NewGuid().ToString("N");
        Guid? contactId = null;
        try
        {
            var result = await client.Contacts.IdentifyAsync(new IdentifyContactInput { ExternalId = external, Email = external + "@example.com" }, cancellationToken: token);
            contactId = result.Data.Id;
            var read = await client.Contacts.GetAsync(external, new GetContactQuery { IdType = "externalId" }, cancellationToken: token);
            Assert.Equal(contactId, read.Data.Id);
        }
        finally { if (contactId is { } id) await CleanupContact(client, id); }
    });

    [LiveFact]
    public Task ListCreationAndMembershipManagement() => Run(async (client, token) =>
    {
        var external = "sdk-integration-" + Guid.NewGuid().ToString("N");
        Guid? contactId = null; Guid? listId = null;
        try
        {
            var contact = await client.Contacts.IdentifyAsync(new IdentifyContactInput { ExternalId = external }, cancellationToken: token);
            contactId = contact.Data.Id;
            var list = await client.Lists.CreateAsync(new CreateListInput { Name = external }, new() { IdempotencyKey = external + "-list" }, token);
            listId = list.Data.Id;
            await client.Lists.AddContactsAsync(listId.Value.ToString(), new ListContactsInputByContactIds { ContactIds = new[] { contactId.Value } }, cancellationToken: token);
            var members = await client.Contacts.ListListsAsync(contactId.Value.ToString(), cancellationToken: token);
            Assert.Contains(members.Data, member => member.Id == listId);
            await client.Lists.RemoveContactsAsync(listId.Value.ToString(), new ListContactsInputByContactIds { ContactIds = new[] { contactId.Value } }, cancellationToken: token);
            members = await client.Contacts.ListListsAsync(contactId.Value.ToString(), cancellationToken: token);
            Assert.DoesNotContain(members.Data, member => member.Id == listId);
        }
        finally
        {
            // Never delete resources found by shared names; only IDs created in this test are eligible.
            try { if (listId is { } id) await CleanupList(client, id); }
            finally { if (contactId is { } id) await CleanupContact(client, id); }
        }
    });

    [LiveFact]
    public Task EventTracking() => Run(async (client, token) =>
    {
        var external = "sdk-integration-" + Guid.NewGuid().ToString("N");
        var eventName = "SDK Integration " + Guid.NewGuid().ToString("N");
        Guid? contactId = null; var trackAttempted = false;
        try
        {
            var contact = await client.Contacts.IdentifyAsync(new IdentifyContactInput { ExternalId = external }, cancellationToken: token);
            contactId = contact.Data.Id; trackAttempted = true;
            var result = await client.Events.TrackAsync(new TrackEventInputByContactId { ContactId = contactId.Value, Name = eventName }, new() { IdempotencyKey = external + "-event" }, token);
            Assert.NotEqual(Guid.Empty, result.Data.Id);
        }
        finally
        {
            try
            {
                if (trackAttempted)
                {
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try { await client.Events.DeleteNameAsync(eventName, cancellationToken: cleanup.Token); }
                    catch (KiroraNotFoundException) { }
                }
            }
            finally { if (contactId is { } id) await CleanupContact(client, id); }
        }
    });

    [LiveEmailFact]
    public Task TransactionalEmailToDocumentedTestAddress() => Run(async (client, token) =>
    {
        var external = "sdk-integration-" + Guid.NewGuid().ToString("N");
        // Kirora's quickstart/staging docs explicitly designate example.com as nondelivering test addresses.
        var recipient = external + "@example.com";
        Guid? contactId = null;
        try
        {
            var contact = await client.Contacts.IdentifyAsync(new IdentifyContactInput { ExternalId = external, Email = recipient }, cancellationToken: token);
            contactId = contact.Data.Id;
            SendTransactionalInputEmail email = new SendTransactionalInputEmailInlineContent
            {
                From = Environment.GetEnvironmentVariable("KIRORA_TEST_FROM")!,
                Subject = "Kirora SDK integration", Html = "<p>SDK integration test.</p>"
            };
            var result = await client.Transactional.SendAsync(new SendTransactionalInput
            { To = new TransactionalRecipientInputByContactId { ContactId = contactId.Value }, Email = email }, new() { IdempotencyKey = external + "-mail" }, token);
            Assert.NotEmpty(result.Data.Messages);
            foreach (var message in result.Data.Messages)
            {
                var read = await client.Messages.GetAsync(message.Id.ToString(), cancellationToken: token);
                Assert.Equal("transactional", read.Data.MessageType);
                Assert.Equal(recipient, read.Data.RecipientEmail);
            }
        }
        finally { if (contactId is { } id) await CleanupContact(client, id); }
    });

    private static async Task Run(Func<KiroraClient, CancellationToken, Task> test)
    {
        // Recheck at execution time as well as discovery. No network operation precedes this gate.
        if (new LiveFactAttribute().Skip is not null) throw new XunitException("Live authorization is no longer present.");
        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false };
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var client = new KiroraClient(http, new KiroraOptions { ApiKey = Environment.GetEnvironmentVariable("KIRORA_API_KEY")!, MaxRetries = 1 });
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            var caller = await client.GetMeAsync(timeout.Token);
            Assert.Equal(Guid.Parse(Environment.GetEnvironmentVariable("KIRORA_INTEGRATION_WORKSPACE_ID")!), caller.Workspace.Id);
            await test(client, timeout.Token);
        }
        catch (KiroraApiException error)
        {
            // Do not include server messages, credentials, request bodies or exception objects in test output.
            throw new XunitException($"Live API failure: HTTP {(int)error.StatusCode}; code {error.Code}; request {error.RequestId}.");
        }
        catch (Exception) { throw new XunitException("Live integration failed. Check workspace configuration and cleanup state; sensitive diagnostics are omitted."); }
    }

    private static async Task CleanupContact(KiroraClient client, Guid id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await client.Contacts.DeleteAsync(id.ToString(), cancellationToken: timeout.Token); }
        catch (KiroraNotFoundException) { }
    }
    private static async Task CleanupList(KiroraClient client, Guid id)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await client.Lists.DeleteAsync(id.ToString(), cancellationToken: timeout.Token); }
        catch (KiroraNotFoundException) { }
    }
}

internal static class LiveGate
{
    internal static string? Reason(Func<string, string?> read, bool requireSender = false)
    {
        if (read("KIRORA_RUN_INTEGRATION") != "1" ||
            read("KIRORA_INTEGRATION_AUTHORIZATION") != "I_AUTHORIZE_LIVE_TESTS" ||
            string.IsNullOrWhiteSpace(read("KIRORA_API_KEY")) ||
            !Guid.TryParse(read("KIRORA_INTEGRATION_WORKSPACE_ID"), out _))
            return "Live tests require explicit authorization, credentials and a dedicated workspace ID.";
        if (requireSender && string.IsNullOrWhiteSpace(read("KIRORA_TEST_FROM")))
            return "Transactional integration needs KIRORA_TEST_FROM on a verified sending domain.";
        return null;
    }
}
