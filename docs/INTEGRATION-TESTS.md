# Local-only live integration validation

No live Kirora calls were executed during preparation. All normal validation uses fake handlers and explicitly disables both live opt-ins. Five live tests are discovered: one read-only smoke and four mutation workflows. They are skipped by default, recheck gates at execution, and refuse CI/GITHUB_ACTIONS/TF_BUILD/GITLAB_CI/JENKINS_URL/BUILD_BUILDID runners even with all opt-ins present. The checked-in GitHub workflows set both live run flags to 0 and never load Kirora credentials.

## Secure common configuration

Only after explicit approval for the selected run, securely export KIRORA_API_KEY in the local terminal without putting its value into chat, shell history, command arguments, test settings files or source control. Disable shell tracing and HTTP/debug collectors. KIRORA_INTEGRATION_WORKSPACE_ID must be the approved workspace UUID. Never use environment-printing or diagnostic test loggers around credentials. None of the commands below supplies or prints a key.

## Read-only authentication/connectivity smoke

The smoke sends only GET /v1/me and confirms the authenticated workspace ID. It creates no contact, list, event or message and does not require mutation/isolation approval. Required variables: KIRORA_API_KEY, KIRORA_INTEGRATION_WORKSPACE_ID, plus its own KIRORA_RUN_SMOKE=1 and KIRORA_SMOKE_AUTHORIZATION=I_AUTHORIZE_READ_ONLY_SMOKE. Mutation flags do not authorize it.

After approval and secure credential setup:

```sh
KIRORA_RUN_INTEGRATION=0 KIRORA_RUN_SMOKE=1 KIRORA_SMOKE_AUTHORIZATION=I_AUTHORIZE_READ_ONLY_SMOKE dotnet test tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj --configuration Release --filter 'Category=LiveSmoke'
```

## State-changing workflows

Every mutation run additionally requires KIRORA_RUN_INTEGRATION=1, KIRORA_INTEGRATION_AUTHORIZATION=I_AUTHORIZE_LIVE_TESTS and KIRORA_INTEGRATION_ISOLATION=I_CONFIRM_EXCLUSIVE_EMPTY_WORKSPACE. Use an exclusively controlled disposable workspace with no journeys or campaigns, including drafts, paused/ended and archived records. Preflight checks GET /v1/me and enumerates all journeys and nonarchived/archived campaigns before every workflow. Any record, authorization failure or malformed response blocks the mutation callback. Additional read scopes: journeys:read and campaigns:read.

These read checks cannot lock server state. Exclusive operator control is necessary: no person, scheduled task or other integration may create/activate automations during the run. A unique event name alone does not establish safety because broad or segment-based triggers may exist. The tests never pause/delete other automations or claim an undocumented server sandbox flag. If exclusivity cannot be guaranteed, do not run mutation tests.

Prepared workflows and exact FullyQualifiedName suffixes:

- ContactCreationAndRetrieval: creates a unique sdk-integration contact and retrieves it by external ID; deletes its returned ID in finally.
- ListCreationAndMembershipManagement: creates a unique contact/list, adds/checks/removes membership; deletes returned list/contact IDs in nested finally blocks.
- EventTracking: creates a unique contact, tracks a unique SDK Integration event with a stable per-call idempotency key, deletes that event name and contact in finally.
- TransactionalEmailToDocumentedTestAddress: creates a unique sdk-integration-<guid>@example.com contact, sends inline email, checks message details, and deletes the contact. Queued messages are not deleted or claimed cancelled; the retained message record is expected.

For the email test only, also configure KIRORA_TEST_FROM on an approved verified sending domain and KIRORA_TEST_RECIPIENT_APPROVAL=SDK_GENERATED_EXAMPLE_COM_ONLY. This explicitly approves only the generated sdk-integration-*@example.com namespace. Recipient addresses are constructed in code; no real address override exists. Kirora's quickstart/staging guidance designates example.com addresses as nondelivering test recipients. Approval of read-only smoke or contact/event testing does not approve email sending.

Required resource scopes by workflow: contacts:read/write/delete; lists:write for list creation, membership changes and deletion; events:write/delete for event testing; transactional:send and messages:read for email. Consult the pinned operation scopes for the selected workflow; the server enforces key permissions.

After separate mutation approval and secure common configuration, contact/list/event tests without email:

```sh
KIRORA_RUN_SMOKE=0 KIRORA_RUN_INTEGRATION=1 KIRORA_INTEGRATION_AUTHORIZATION=I_AUTHORIZE_LIVE_TESTS KIRORA_INTEGRATION_ISOLATION=I_CONFIRM_EXCLUSIVE_EMPTY_WORKSPACE dotnet test tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj --configuration Release --filter 'Category=LiveIntegration&FullyQualifiedName!~TransactionalEmailToDocumentedTestAddress'
```

After separate email approval and secure KIRORA_TEST_FROM setup:

```sh
KIRORA_RUN_SMOKE=0 KIRORA_RUN_INTEGRATION=1 KIRORA_INTEGRATION_AUTHORIZATION=I_AUTHORIZE_LIVE_TESTS KIRORA_INTEGRATION_ISOLATION=I_CONFIRM_EXCLUSIVE_EMPTY_WORKSPACE KIRORA_TEST_RECIPIENT_APPROVAL=SDK_GENERATED_EXAMPLE_COM_ONLY dotnet test tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj --configuration Release --filter 'Category=LiveIntegration&FullyQualifiedName~TransactionalEmailToDocumentedTestAddress'
```

For a single workflow, use `--filter 'FullyQualifiedName=Kirora.IntegrationTests.LiveIntegrationTests.EventTracking'` (or another exact suffix above) with the same required authorization variables. To run all four, use `--filter 'Category=LiveIntegration'` with email approval configured. A skipped test is not a successful live validation; check actual passed/skipped counts.

## Cleanup, failure output and offline checks

Cleanup uses separate bounded 30-second cancellation tokens and only returned resource IDs or the unique event name. A 404 is treated as already gone. Interrupted/ambiguous creations can leave orphan data; event deletion may be asynchronous and active sends may block contact deletion. Cleanup failures fail the run; inspect isolated records rather than broadening deletion. No cleanup can undo an already processed event or message.

Integration failure output includes only fixed text and HTTP status, never server error codes/messages/request IDs, response bodies, exception objects, credentials or request content. Tests verify that even a malicious server echoing a fake credential cannot leak it through test exceptions. The SDK preserves structured exceptions for application use; consumers must avoid logging raw exception data, headers or diagnostics. No universal promise is made about external profilers or user-configured logging.

All gates, workspace mismatches, automation refusal, empty-workspace checks and redaction are tested with fake HTTP. Run them safely with:

```sh
KIRORA_RUN_INTEGRATION=0 KIRORA_RUN_SMOKE=0 dotnet test tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj --configuration Release
```

Unset live opt-ins and remove temporary credentials from the local environment after an approved run. Do not enable these tests on CI.
