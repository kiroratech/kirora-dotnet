# Opt-in live integration tests

The default suite uses fake HTTP handlers. The separate Kirora.IntegrationTests project discovers four live workflows but skips them unless every authorization requirement is present. Validation for this phase explicitly sets KIRORA_RUN_INTEGRATION=0; **no live API tests were executed**.

## Authorization and configuration

Use a dedicated staging workspace, never a production workspace with real engagement automations. The test runner first checks GET /v1/me and compares the returned workspace ID to KIRORA_INTEGRATION_WORKSPACE_ID before creating any resource.

Explicit requirements:

- KIRORA_RUN_INTEGRATION=1.
- KIRORA_INTEGRATION_AUTHORIZATION=I_AUTHORIZE_LIVE_TESTS.
- KIRORA_API_KEY supplied through the environment or the CI secret store.
- KIRORA_INTEGRATION_WORKSPACE_ID containing the expected staging workspace UUID.
- For the email workflow only, KIRORA_TEST_FROM containing an address on your verified sending domain.

These are instructions for a future explicitly authorized run, not authorization granted by creating the suite. The implementation session did not set these values or run live calls. Do not print environment variables in CI, enable shell tracing around secrets, or pass keys as command-line arguments.

After a maintainer authorizes a live run and securely configures those variables, use:

```sh
dotnet test tests/Kirora.IntegrationTests --configuration Release --filter Category=LiveIntegration
```

To verify the project and skip behavior without contacting Kirora:

```sh
KIRORA_RUN_INTEGRATION=0 dotnet test tests/Kirora.IntegrationTests --configuration Release
```

Six ordinary tests verify the gates with fake in-memory configuration. They never make HTTP calls and are not live integration results.

## Prepared workflows

1. Contact identification and retrieval by external ID, followed by contact deletion.
2. List creation, contact membership add/read/remove, followed by list/contact deletion.
3. Event tracking with a stable key, followed by deletion of the uniquely named test event and contact.
4. Inline transactional email to a unique address at example.com, followed by message-detail retrieval and contact cleanup.

The email recipient domain is fixed in code and cannot be replaced by a real recipient through configuration. Kirora's [quickstart](https://kirora.io/docs/api/quickstart) and [staging guidance](https://kirora.io/docs/api/staging) document example.com/example.net/example.org as nondelivering test addresses. A verified sender and other normal send checks still apply. These tests assert API acceptance and a documented message read; they do not assert inbox delivery or invent a server test-mode flag. Server state transitions are not assumed to be instantaneous.

Use a staging key with contacts:read/write/delete, lists:write, events:write/delete, transactional:send and messages:read scopes as required by the selected workflows. The server determines authorization; an invalid/revoked key fails an explicitly opted-in run rather than being reported as passing.

## Isolation and cleanup

Every workflow creates unique sdk-integration-prefixed external IDs, names and idempotency keys. Cleanup targets only resources whose IDs were returned by that workflow, plus its uniquely named event. It runs in finally blocks with separate bounded cancellation tokens and treats a cleanup 404 as already gone. It never deletes resources found under a shared business name.

A process crash or an ambiguous response before an ID was returned can leave an isolated resource behind. A contact may temporarily refuse deletion while a send is active. Such cleanup failures fail the workflow and need inspection of the staging workspace; do not broaden deletion to unrelated data. Event-name deletion can be asynchronous. Keep the staging workspace separate because even test events can trigger journeys configured there.

API failures are reported using status, error code and request ID. Raw server messages, credentials and request bodies are omitted from integration output. No HTTP body/header logging is configured by the suite.

## Phase 4 operator checklist

- Confirm explicit authorization for this run and a dedicated workspace with production automations disabled.
- Confirm the key scopes and expected workspace UUID; the suite checks `/v1/me` before mutations.
- Supply credentials through environment/secret storage, never command-line arguments or log output.
- Review the four workflow bodies and fixed nondelivering `example.com` recipient. The verified sender remains separately configured.
- Set both opt-in flags only after approval. Run the integration project with the LiveIntegration trait filter.
- Record four actual outcomes separately from the six offline authorization-gate tests. Check finally-block cleanup and any isolated orphan records after ambiguous failures.
- Remove live opt-in flags/temporary credentials from the runner afterward. No live workflow was executed during Phase 4 validation.
