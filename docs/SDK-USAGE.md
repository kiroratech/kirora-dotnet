# HTTP, model and resource guidance

## Ownership, DI and cancellation

A caller-created HttpClient remains caller-owned. Reuse it and disable automatic redirects so credentials are not forwarded to unexpected origins. The DI extension creates factory-managed handlers with a five-minute pooled connection lifetime, redirects/cookies disabled, and header/URI logging removed. It returns IHttpClientBuilder for explicit application customization. SDK options are snapshotted on client creation; ForWorkspace creates an immutable partner-tenant view sharing transport.

All async operations take CancellationToken. SDK RequestTimeout defaults to 30 seconds per attempt; an outer cancellation token can bound the whole workflow. Do not infer a write failed merely because a connection or deadline failed before you received its response.

## Optional values and unions

Generated Optional<T> represents omitted versus explicitly supplied values. Default Optional means omitted; assign an explicit nullable value only where the schema allows null. New required nullable response members still must appear in JSON. Do not deserialize a legacy request payload into newer DTOs expecting a compatibility translator: normal System.Text.Json behavior may ignore unknown fields. Rebuild typed requests for the selected contract.

Discriminated template and snapshot unions expose Value and As<T>(); choose DesignTemplate/HtmlTemplate and the corresponding input types. HTML requires its documented kind discriminator. Some design inputs retain an optional kind, allowing a valid legacy design wire shape even though the generated CLR API has changed. Do not add fields from the other variant. Read OPENAPI-20261103-MIGRATION.md for exact source changes, filter/value conversion, topics, forms and fallbackOrder.

Request serialization uses generated JSON metadata and statically rooted Optional converters. DTO generation is checksum-pinned and fails on unsupported constructs rather than guessing endpoints/fields. Unmodeled response attributes are retained only where the schema permits extension data.

## Refusals, retries and idempotency

KiroraApiException preserves HTTP metadata, structured error code/details and field errors, including unknown future codes. Authentication/forbidden/not-found/conflict/rate-limit subclasses permit focused handling. KiroraTransportException retains its inner exception for application-controlled safe diagnostics; KiroraTimeoutException is its timeout subclass. KiroraInvalidResponseException carries response metadata for JSON/schema failures. Logging raw server messages, request IDs, details or exceptions can expose data; log only approved/sanitized fields.

MaxRetries defaults to three. The generated operation policy determines whether a retry is safe; merely being a POST does not grant retry safety. Supported idempotent writes retain a single key across attempts, and explicit caller keys are respected. Reuse a key only for the same operation and identical payload, within the documented 24-hour window. Per-call retry scheduling is capped before that window; tracking a key's age across separate application calls is the caller's responsibility. Batch entry keys are snapshotted and retained. Ambiguous idempotency outcomes and unsafe lookup-changing contact updates are not blindly resent with a new key. Retry-After is honored within the configured delay budget.

CSV exports stream to a caller-owned destination. A partially copied stream is never silently replayed into the same destination. Multipart upload validates the documented constraints and preserves replay bytes/key within a safe call. Response callbacks are caller code; callback failures are not API retry signals.

## Pagination and resources

ListAsync returns one typed page and its documented cursor metadata. ListAllAsync follows nextCursor lazily, preserving filters, cancellation and request options; it rejects repeated cursors. Not every API array is paginated, and the SDK does not invent cursors on analytics series or nonpaginated arrays. Inspect API-COVERAGE.md/csv for every operation's C# mapping, scopes, key restrictions and retry policy.

Core resource usage is in the README and examples/Kirora.Examples. The console and ASP.NET examples are built directly from README code blocks; additional extended-resource helpers demonstrate templates, journeys, campaigns, exports, assets and analytics. No example is automatically authorized to modify an actual workspace.
