# Kirora .NET SDK

A .NET 8 SDK with strongly typed coverage of all **184 operations** in the pinned official Kirora OpenAPI specification. This is an **unpublished public-alpha candidate**, not a claim of live verified API parity. No API key is included in the SDK.

## Installation

Before publication, use a project reference to `src/Kirora/Kirora.csproj`, or add the locally packed `src/Kirora/bin/Release` directory as a NuGet source alongside your usual dependency feed.

After a maintainer approves and publishes the version, installation will be:

```sh
dotnet add package Kirora --version 0.1.0-alpha.1
```

The package is not published by running restore, build, tests or pack. See [package readiness](docs/PACKAGE-READINESS.md) and the [release checklist](docs/RELEASE-CHECKLIST.md) for unresolved ownership/license/repository decisions.

## Console quickstart

Read the key from a server-side environment variable or secret store. Reuse HttpClient; its owner disposes it. This first-use example only checks the key/workspace and never sends a message.

```csharp
using Kirora;

var key = Environment.GetEnvironmentVariable("KIRORA_API_KEY")
    ?? throw new InvalidOperationException("Configure KIRORA_API_KEY.");
using var handler = new SocketsHttpHandler {
    AllowAutoRedirect = false, UseCookies = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
};
using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
var kirora = new KiroraClient(http, new KiroraOptions { ApiKey = key });
using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var caller = await kirora.GetMeAsync(deadline.Token);
Console.WriteLine(caller.Workspace.Name);
```

Run the existing read-only console application with `dotnet run --project examples/Kirora.Examples`. Its resource helpers are compile-checked examples and are not automatically executed.

## ASP.NET Core dependency injection

```csharp
using Kirora;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKirora(options => options.ApiKey =
    builder.Configuration["Kirora:ApiKey"]
    ?? throw new InvalidOperationException("Configure Kirora:ApiKey."));
var app = builder.Build();
app.MapGet("/health/kirora", async (KiroraClient kirora, CancellationToken ct) => {
    await kirora.GetMeAsync(ct);
    return Results.Ok(new { connected = true });
});
app.Run();
```

`Kirora__ApiKey` maps to `Kirora:ApiKey` through .NET configuration. AddKirora returns IHttpClientBuilder for deliberate transport customization; it configures handler lifetimes, cookies/redirects, timeout handling and validated options. Factory HTTP logging is disabled by default because request URIs may contain contact identifiers. Any custom logger must redact credentials, headers and identifiers.

See `examples/Kirora.AspNetCore` and `examples/Kirora.Worker`. A singleton background worker should create factory-managed clients per operation rather than capture a transient typed client forever. The worker demonstrates cancellation-aware periodic reads without logging keys or message bodies.

## Core operations

The following snippets assume an injected `kirora` client and a caller-provided `CancellationToken ct`. New resource methods return typed `Data` and, where documented, `Meta`; the original GetMeAsync continues returning ApiCaller directly.

### Identify and find a contact

```csharp
using Kirora.Models;
var contact = await kirora.Contacts.IdentifyAsync(new IdentifyContactInput {
    ExternalId = "user-123", Email = "ada@example.com", FirstName = "Ada"
}, cancellationToken: ct);
var read = await kirora.Contacts.GetAsync("user-123",
    new GetContactQuery { IdType = "externalId" }, cancellationToken: ct);
```

Generated `Optional<T>` fields distinguish omission from explicit null. For example, `LastName = new Kirora.Serialization.Optional<string?>(null)` clears the name where allowed; leaving it unset omits it. Free-form schema fields use dictionaries of JsonElement. Structured objects remain generated DTOs.

### Track an event

```csharp
await kirora.Events.TrackAsync(new TrackEventInputByExternalId {
    ExternalId = "user-123", Name = "Order Completed"
}, new KiroraRequestOptions { IdempotencyKey = "order-1001-completed" }, ct);
```

Keep event names stable and use one business-operation key for exactly the same event payload. The SDK also exposes event catalog/history, standard names, summaries and time series.

### Transactional email and push

Configure a published template and verified sender in Kirora first. This request model supports email, push, or both according to the API contract:

```csharp
SendTransactionalInputEmail email =
    new SendTransactionalInputEmailStoredTemplate { Template = "order-receipt" };
var result = await kirora.Transactional.SendAsync(new SendTransactionalInput {
    To = new TransactionalRecipientInputByExternalId { ExternalId = "user-123" },
    Email = email,
    Push = new TransactionalPushInput {
        Title = "Order received", Body = "Thank you for your order."
    },
    Deliver = (TransactionalDeliverInput)new TransactionalDeliverFirstInput
    {
        First = ["email", "push"]
    }
}, new KiroraRequestOptions { IdempotencyKey = "order-1001-receipt" }, ct);
```

Use SendTransactionalInputEmailInlineContent for inline subject/from/html instead of a stored template. Batch sends retain ordered typed success/error results; HTTP success does not imply every entry succeeded. Message details and retained content are under `kirora.Messages`, matching the spec rather than guessed transactional read URLs. See compile-checked `CoreResourceExamples.cs` for more examples.

### Lists and memberships

```csharp
var list = await kirora.Lists.CreateAsync(new CreateListInput { Name = "Newsletter" },
    new KiroraRequestOptions { IdempotencyKey = "newsletter-create-1001" }, ct);
await kirora.Lists.AddContactsAsync(list.Data.Id.ToString(),
    new ListContactsInputByExternalIds { ExternalIds = new[] { "user-123" } },
    cancellationToken: ct);
var memberships = await kirora.Contacts.ListListsAsync("user-123",
    new ListContactListsQuery { IdType = "externalId" }, cancellationToken: ct);
await kirora.Lists.RemoveContactsAsync(list.Data.Id.ToString(),
    new ListContactsInputByExternalIds { ExternalIds = new[] { "user-123" } },
    cancellationToken: ct);
```

The spec has no separate membership-check endpoint. Use contact memberships or filter Contacts.ListAsync by ListId.

### Pagination, reporting and downloads

```csharp
await foreach (var contact in kirora.Contacts.ListAllAsync(
    new ListContactsQuery { Query = "Ada", Limit = 20 }, cancellationToken: ct))
    Console.WriteLine(contact.Id);
var rfm = await kirora.Reports.GetRfmSummaryAsync(cancellationToken: ct);
await kirora.Messages.DownloadExportToAsync(readyExportId, destinationStream,
    cancellationToken: ct);
```

Paginated JSON methods have lazy `...AllAsync` iterators that preserve filters, follow opaque cursors until null, continue across empty intermediate pages and reject cycles. Nonpaginated arrays do not gain a fabricated cursor API. CSV downloads leave the destination open and never replay partially written output. Multipart image uploads use UploadAssetInput.File and documented filename/MIME metadata, buffer at most the allowed 2 MB file, and reuse identical content/key across retries.

## Errors, cancellation and retries

```csharp
using Kirora.Exceptions;
try {
    var page = await kirora.Contacts.ListAsync(cancellationToken: ct);
} catch (KiroraApiException error) {
    // Log selected metadata, not credentials, raw bodies or personal information.
    Console.Error.WriteLine($"Kirora {error.Code}; HTTP {(int)error.StatusCode}; request {error.RequestId}");
} catch (OperationCanceledException) when (ct.IsCancellationRequested) {
    // Caller cancellation remains cancellation, rather than an API failure.
}
```

API exceptions preserve status, unknown future string codes, structured field errors/details, request ID, rate-limit and replay metadata. Malformed/missing/explicit-null required success values produce KiroraInvalidResponseException. Per-call ResponseReceived receives HTTP metadata; exceptions thrown by that application callback propagate unchanged and never cause a request retry.

RequestTimeout bounds each attempt, including body reads. Use CancellationToken for an overall deadline. Default MaxRetries is three; zero disables retries. Retry-After is honored or the refusal is returned if it exceeds MaxRetryDelay. RateLimit-Reset is seconds until refill, not an epoch timestamp. Do not stack a second unsafe write retry policy around the SDK.

Automatic write retries follow generated operation semantics and documented idempotency support. A supported call gets one key retained across attempts; caller-supplied keys also protect application-level retries within the API's 24-hour window. Reuse a key only for the same operation and body. A single call never schedules keyed retries 24 hours or more after its first attempt, even with a larger delay budget. Applications must track the original age of keys reused across separate calls. Batches generate absent per-entry keys on a private snapshot and preserve supplied keys. IDEMPOTENCY_KEY_REUSED and IDEMPOTENCY_KEY_OUTCOME_UNKNOWN surface rather than triggering a new-key resend. Contact updates that change their lookup email/externalId are not retried after ambiguous failures. Partner tenant views use immutable ForWorkspace(externalId), never mutable default headers.

## Coverage, generation and validation

[API coverage](docs/API-COVERAGE.md) and the [operation CSV matrix](docs/api-coverage.csv) document all 184 endpoints, scopes, retry policies and the inspected Node/Go comparison. Reports, Events and ConversionGoals expose analytics; no speculative Analytics URL prefix is added. The website/spec discrepancies remain documented.

`spec/openapi.json` and its source pin are unchanged: API 1.0.0 at commit 30043317efd68ffceeab882019431e57b7463018, SHA-256 4b31b357845dc09f89018879db8f238bb742df6a4c5f023b3210dded9919c8fa. Generation is deterministic, offline and fail-closed on pin mismatches; never edit generated DTOs or fetch mutable main during a normal build.

```sh
sh tools/check-generated.sh
dotnet restore --locked-mode
dotnet build --configuration Release
KIRORA_RUN_INTEGRATION=0 dotnet test --configuration Release
python3 tools/check-public-api.py
python3 tools/check-dependencies.py
dotnet pack --configuration Release
python3 tools/validate-package.py
```

All default tests and the Native AOT smoke use fake HTTP. Four live workflows require separate explicit authorization, credentials and an expected test workspace ID; they were not executed. See [integration instructions](docs/INTEGRATION-TESTS.md).

[Native AOT verification](docs/NATIVE-AOT.md) records a successful .NET 8 macOS-x64 smoke, its exercised paths and remaining framework/toolchain warnings. Broader platform support and consumer-defined optional types are not implied. [Troubleshooting](docs/TROUBLESHOOTING.md), [Phase 4 audit](docs/PHASE4.md) and [release checklist](docs/RELEASE-CHECKLIST.md) describe remaining decisions. CI/publication workflow files are prepared; they do not run or publish merely because they exist.

## Official OpenAPI provenance

The authoritative specification repository is [kiroratech/openapi](https://github.com/kiroratech/openapi). This SDK pins commit [`30043317efd68ffceeab882019431e57b7463018`](https://github.com/kiroratech/openapi/commit/30043317efd68ffceeab882019431e57b7463018), with its checksum recorded in `spec/source.json`. Coverage claims refer to that immutable pin. See [upstream verification](docs/UPSTREAM-VERIFICATION.md) for the newer official contract differences and migration recommendation. The upstream MIT license is retained in `spec/LICENSE` and distributed in `THIRD-PARTY-NOTICES.txt`; SDK licensing remains a separate maintainer decision.

The pre-release contract migration intentionally changes generated model types and removes the obsolete brand-theme route. See [migration guidance and exact API changes](docs/OPENAPI-MIGRATION.md).

## Official repository and validation

Source: [kiroratech/kirora-dotnet](https://github.com/kiroratech/kirora-dotnet). `main` is the intended default branch; the OpenAPI migration is reviewed on `chore/update-kirora-openapi`. Native AOT and standalone package checks use fake HTTP only. [Live validation instructions](docs/INTEGRATION-TESTS.md) separate read-only authentication smoke from state-changing tests and require local opt-in; CI refuses both. See [release readiness](docs/REPOSITORY-READINESS.md) for validation evidence, historical artifact findings, SourceLink status and the approval-dependent push commands.
