# Troubleshooting

## Restore, build and package

Run from the directory containing Kirora.sln. MSB1003 means the current directory does not contain the solution/project. global.json targets an installed .NET 8 SDK compatible with 8.0.404; install an appropriate SDK if selection fails. All library targets remain net8.0.

NuGet restore needs access to api.nuget.org or a configured package mirror. Package creation is local and does not publish. An unpublished alpha cannot be installed from nuget.org; use a project reference or the local nupkg/feed described in README.md.

Run the offline consistency checks if generated files drift. Update spec/source.json and spec/openapi.json only through an explicitly approved contract update; do not edit generated DTOs or make a normal build download mutable main.

## Authentication and refusals

Use server-side configuration: Kirora__ApiKey maps to Kirora:ApiKey in ASP.NET Core/worker examples. The console example reads KIRORA_API_KEY. DI validates configuration on startup. Never include a key in a support ticket or exception log.

A 401 indicates missing/invalid/revoked/expired credentials. A 403 can indicate a missing scope, an agent-key restriction, plan availability, consent, suppression or workspace state. Inspect KiroraApiException.Code, Details and RequestId; do not assume every 403 is a bad key. A cross-workspace resource normally appears as 404. Partner tenancy uses ForWorkspace(externalId), not a mutable global HttpClient header.

## Invalid requests and models

ArgumentException before HTTP means a request/query failed local validation (for example a required analytics interval, invalid enum, or an excessive batch limit). Server-only business rules still produce structured API exceptions. Optional<T> distinguishes omission from null; setting an explicit null is only valid where the schema permits it. Use an intermediate named union variable when assigning a variant to an Optional<Union> property.

New resource results retain Data and any documented Meta. The original KiroraClient.GetMeAsync returns ApiCaller directly. An HTTP 2xx batch can still contain entry errors; inspect each typed result variant. Missing required response fields or an unknown union shape produce KiroraInvalidResponseException; report the request ID and a sanitized schema discrepancy, not the raw body.

## Timeouts, retries and rate limits

Use a caller CancellationToken for an overall deadline. RequestTimeout bounds each attempt, including body reads. A supplied HttpClient.Timeout can shorten it. Set MaxRetries=0 to disable automatic retries; avoid stacking a second write retry policy around the SDK.

Reuse a business-operation idempotency key only for exactly the same operation/body. Do not replace a key and resend after IDEMPOTENCY_KEY_OUTCOME_UNKNOWN. IDEMPOTENCY_KEY_REUSED is a permanent refusal. Retry-After is never shortened to fit a configured delay budget. RateLimit-Reset is seconds until refill, not a Unix timestamp.

CSV downloads leave the destination open and do not replay partially written output. Discard partial output and deliberately restart into a fresh destination if appropriate. Multipart uploads support documented PNG/JPEG/GIF types, at most 2 MB and 25 megapixels server-side; use a basename and valid MIME type. The SDK checks byte size/type but leaves image decoding/pixel checks to the API.

## Live tests and contract differences

Four live workflows are intentionally skipped by default. Follow docs/INTEGRATION-TESTS.md only after explicit authorization; a skipped workflow is not a successful live verification. No API key is required for offline tests.

The reference website advertises 169 endpoints while the pinned specification has 183. Groups/Lists and contact erasure paths differ. This SDK follows the pinned contract. Endpoint coverage is not a guarantee that a deployment matches it; request official reconciliation rather than adding guessed routes.


## Phase 4 hardening notes

Default DI HTTP logging is removed to avoid exposing contact identifiers in URIs. Re-enabling a custom logger is an explicit application choice. ResponseReceived callback failures propagate as the original exception without retransmitting the request. Required non-null response values are validated before DTO use; malformed payloads retain HTTP metadata in KiroraInvalidResponseException.

Ordinary builds disable automatic SourceLink/repository inference because this SDK directory currently inherits an unrelated ancestor Git root. This is intentional. Supply approved metadata only through the dedicated release procedure; do not adopt the ancestor remote to silence missing-repository checks. A failed release preflight with maintainerApproved=false is expected.

NuGet audit failures block CI, including feed failures. The native AOT smoke is offline and distinct from the explicitly gated live Kirora tests. See NATIVE-AOT.md for the local framework/linker warnings rather than treating them as SDK support for every platform.
