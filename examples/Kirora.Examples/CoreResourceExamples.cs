using Kirora;
using Kirora.Models;
using Kirora.Serialization;

namespace Kirora.Examples;

/// <summary>Compile-checked examples. These methods are not invoked by the default read-only console entry point.</summary>
public static class CoreResourceExamples
{
    public static async Task ContactsAsync(KiroraClient kirora, CancellationToken cancellationToken)
    {
        var contact = await kirora.Contacts.IdentifyAsync(new IdentifyContactInput
        {
            ExternalId = "example-user-123", Email = "ada@example.com", FirstName = "Ada"
        }, cancellationToken: cancellationToken);
        Console.WriteLine(contact.Data.Id);
        await kirora.Contacts.UpdateAsync(contact.Data.Id.ToString(), new UpdateContactInput
        {
            FirstName = "Ada", LastName = new Optional<string?>(null)
        }, cancellationToken: cancellationToken);
        await foreach (var person in kirora.Contacts.ListAllAsync(new ListContactsQuery { Query = "Ada", Limit = 20 }, cancellationToken: cancellationToken))
            Console.WriteLine(person.Id);
    }

    public static async Task ListsAsync(KiroraClient kirora, CancellationToken cancellationToken)
    {
        var list = await kirora.Lists.CreateAsync(new CreateListInput { Name = "SDK example" },
            new KiroraRequestOptions { IdempotencyKey = "sdk-example-list-create-1" }, cancellationToken);
        await kirora.Lists.AddContactsAsync(list.Data.Id.ToString(), new ListContactsInputByExternalIds { ExternalIds = new[] { "example-user-123" } }, cancellationToken: cancellationToken);
        var memberships = await kirora.Contacts.ListListsAsync("example-user-123", new ListContactListsQuery { IdType = "externalId" }, cancellationToken: cancellationToken);
        Console.WriteLine(memberships.Data.Count);
        var usage = await kirora.Lists.GetUsageAsync(list.Data.Id.ToString(), cancellationToken: cancellationToken);
        Console.WriteLine(usage.Data.HasMore);
    }

    public static async Task EventsAsync(KiroraClient kirora, string orderId, CancellationToken cancellationToken)
    {
        // Reuse this business-event key only for exactly the same operation and payload.
        var tracked = await kirora.Events.TrackAsync(new TrackEventInputByExternalId
        {
            ExternalId = "example-user-123", Name = "Order Completed"
        }, new KiroraRequestOptions { IdempotencyKey = $"order-{orderId}-completed" }, cancellationToken);
        Console.WriteLine(tracked.Data.Id);
    }

    public static async Task TransactionalAsync(KiroraClient kirora, string orderId, CancellationToken cancellationToken)
    {
        SendTransactionalInputEmail email = new SendTransactionalInputEmailStoredTemplate { Template = "order-receipt" };
        var result = await kirora.Transactional.SendAsync(new SendTransactionalInput
        {
            To = new TransactionalRecipientInputByExternalId { ExternalId = "example-user-123" },
            Email = email,
            Push = new TransactionalPushInput { Title = "Order received", Body = "Thank you for your order." },
            Deliver = "first"
        }, new KiroraRequestOptions { IdempotencyKey = $"order-{orderId}-receipt" }, cancellationToken);
        Console.WriteLine(result.Data.Messages.Count);
        // Read delivery details/content through kirora.Messages.GetAsync(id) and GetContentAsync(id).
    }
}
