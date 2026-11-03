# Kirora .NET SDK

A typed, asynchronous SDK for Kirora's contacts, lists, events, messaging and engagement APIs. Targets **.NET 8 (`net8.0`)** and uses `HttpClient` and `System.Text.Json`. All **184 operations** in the [pinned official specification](spec/source.json) have typed methods and cancellation support.

This repository is an **unpublished alpha candidate**. Package licensing/ownership and release approval remain open; local validation does not mean a version is available on NuGet.org.

## Install

**Before publication:** clone this repository, build the local package, and configure its output folder as an application-local NuGet feed alongside nuget.org. See [local installation](docs/GETTING-STARTED.md#local-package-installation).

```sh
dotnet pack src/Kirora/Kirora.csproj --configuration Release
```

**After an official release is published:** install the approved version in your application:

```sh
dotnet add package Kirora --version 0.1.0-alpha.1
```

## Configure and connect

Provide `KIRORA_API_KEY` through your process environment or a secret store. Never put it in source or log it. For the currently approved staging environment:

```sh
export KIRORA_BASE_URL='https://kirora-staging-backend.brimble.app'
```

The examples apply this optional variable through `KiroraOptions.BaseAddress`; the SDK does not read environment variables itself. Use an HTTPS origin only, without `/v1`. Unset it to retain the SDK's production default, `https://api.kirora.io/`. Kirora has confirmed that production DNS is not configured yet; use the staging backend until that changes. Use credentials for the intended workspace.

A complete console example (only a read-only authentication request):

<!-- example: console -->
```csharp
using Kirora;

var key = Environment.GetEnvironmentVariable("KIRORA_API_KEY");
if (string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("Configure KIRORA_API_KEY in this process.");
    return;
}
var options = new KiroraOptions { ApiKey = key };
var baseUrl = Environment.GetEnvironmentVariable("KIRORA_BASE_URL");
if (!string.IsNullOrEmpty(baseUrl)) options.BaseAddress = new Uri(baseUrl);
using var handler = new SocketsHttpHandler
{
    AllowAutoRedirect = false, UseCookies = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
};
using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
var kirora = new KiroraClient(http, options);
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var caller = await kirora.GetMeAsync(deadline.Token);
Console.WriteLine($"Connected to workspace {caller.Workspace.Id}.");
```

Run it with `dotnet run --project examples/Kirora.Examples` only when a live read is authorized. An exported terminal variable is inherited by `dotnet run`; VS Code debugging uses its own process configuration. See [environment setup and troubleshooting](docs/GETTING-STARTED.md).

## ASP.NET Core

`AddKirora` registers a typed client, validates options on startup, and manages connection pooling through `IHttpClientFactory`. Inject `KiroraClient` into your services or endpoints.

<!-- example: aspnet -->
```csharp
using Kirora;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKirora(options =>
{
    options.ApiKey = builder.Configuration["KIRORA_API_KEY"]
        ?? throw new InvalidOperationException("Configure KIRORA_API_KEY.");
    var baseUrl = builder.Configuration["KIRORA_BASE_URL"];
    if (!string.IsNullOrEmpty(baseUrl)) options.BaseAddress = new Uri(baseUrl);
});
var app = builder.Build();
app.MapGet("/kirora/check", async (KiroraClient kirora, CancellationToken token) =>
{
    await kirora.GetMeAsync(token);
    return Results.Ok(new { connected = true });
});
app.Run();
```

## Common operations

With a configured `kirora` client and `CancellationToken token`, the following examples **create data and send a message**. Use an authorized test workspace, an approved test recipient, and a verified sender supplied by `KIRORA_TEST_FROM`.

<!-- example: resources -->
```csharp
using Kirora;
using Kirora.Models;

var contact = await kirora.Contacts.IdentifyAsync(new IdentifyContactInput
{
    ExternalId = "sdk-demo-user", Email = "sdk-demo@example.com"
}, cancellationToken: token);
var list = await kirora.Lists.CreateAsync(new CreateListInput { Name = "SDK demo" },
    new KiroraRequestOptions { IdempotencyKey = "sdk-demo-list-v1" }, token);
await kirora.Lists.AddContactsAsync(list.Data.Id.ToString(),
    new ListContactsInputByContactIds { ContactIds = new[] { contact.Data.Id } },
    cancellationToken: token);
await kirora.Events.TrackAsync(new TrackEventInputByContactId
{
    ContactId = contact.Data.Id, Name = "SDK Demo Completed"
}, new KiroraRequestOptions { IdempotencyKey = "sdk-demo-event-v1" }, token);
SendTransactionalInputEmail email = new SendTransactionalInputEmailInlineContent
{
    From = Environment.GetEnvironmentVariable("KIRORA_TEST_FROM")
        ?? throw new InvalidOperationException("Configure a verified sender."),
    Subject = "SDK test", Html = "<p>Your explicitly approved SDK test.</p>"
};
await kirora.Transactional.SendAsync(new SendTransactionalInput
{
    To = new TransactionalRecipientInputByContactId { ContactId = contact.Data.Id },
    Email = email
}, new KiroraRequestOptions { IdempotencyKey = "sdk-demo-email-v1" }, token);
```

The example.com recipient is a documented nondelivering test address; normal send checks still apply. Replace demo identifiers only deliberately, and never reuse an idempotency key with a different body. More examples include [push and stored templates](examples/Kirora.Examples/CoreResourceExamples.cs), [extended resources](examples/Kirora.Examples/ExtendedResourceExamples.cs), and a [background worker](examples/Kirora.Worker).

## Errors, retries and pagination

API refusals throw typed `KiroraApiException` subclasses. Network errors use `KiroraTransportException`, deadlines use `KiroraTimeoutException`, and invalid response contracts use `KiroraInvalidResponseException`. Structured codes/details remain available; avoid logging raw server messages, headers or bodies.

<!-- example: errors -->
```csharp
using Kirora.Exceptions;

try
{
    var count = 0;
    await foreach (var contact in kirora.Contacts.ListAllAsync(cancellationToken: token))
        count++;
    Console.WriteLine($"Enumerated {count} contacts.");
}
catch (KiroraApiException error)
{
    Console.Error.WriteLine($"Kirora returned HTTP {(int)error.StatusCode}.");
}
catch (KiroraTransportException)
{
    Console.Error.WriteLine("Check connectivity; inspect write outcomes before retrying.");
}
```

`ListAsync` returns one page plus cursor metadata; `ListAllAsync` lazily follows documented cursors. Retries follow each operation's safety policy. Supported writes retain a stable key across attempts; application retries must reuse that key with the same payload within the documented window. Unsafe writes and partially copied exports are not blindly replayed. See [HTTP and model guidance](docs/SDK-USAGE.md).

## More

- [Official API reference](https://kirora.io/docs/api/reference) · [SDK repository](https://github.com/kiroratech/kirora-dotnet)
- [Coverage matrix](docs/API-COVERAGE.md) · [current breaking-change assessment](docs/OPENAPI-20261103-ASSESSMENT.md) · [migration details](docs/OPENAPI-20261103-MIGRATION.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md) · [opt-in live tests](docs/INTEGRATION-TESTS.md) · [release checklist](docs/RELEASE-CHECKLIST.md)

The OpenAPI pin is `80060d50bfa80f3c82b89a363a229c5b1c39506a`. Its MIT attribution is retained in `spec/LICENSE` and `THIRD-PARTY-NOTICES.txt`; that does not assign a license to the SDK itself. No examples or tests are authorization to send live traffic.
