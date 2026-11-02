# Phase 3 — Extended coverage and integration readiness

## Result

183/183 unique pinned-spec operations have typed resource methods across all 25 tags. The baseline was 36 (35 Phase 2 operations plus GetMeAsync); Phase 3 adds 147 operations. The pinned OpenAPI JSON and source pin are unchanged.

The full before/after endpoint matrix, Node/Go comparison, scopes and retry policies are in [API-COVERAGE.md](API-COVERAGE.md) and [api-coverage.csv](api-coverage.csv).

## Modules

Extended Events and Messages; added Templates, Campaigns, Journeys, Segments, Reports (including RFM), ConversionGoals, Topics, Attributes, Tags, SharedBlocks, Assets, Forms, Suppressions, SendingDomains, Senders, PushProjects, Webhooks, Workspaces, EmbedSessions and a Me resource alias. Existing GetMeAsync remains compatible.

## Compatibility and transport

- A snapshot of 1,309 public Phase 2 signatures is compared with the current assembly in an automated regression test. This checks public type/member availability; it does not prove unchanged semantics in every case.
- Every resource class and operation has packaged XML documentation. Tests verify the XML entries and cancellation-token presence.
- Requests/responses, query contracts, union variants and operation retry policies are generated from the pinned spec. Optional field omission/null behavior remains intact.
- CSV downloads stream to caller-owned destinations without replaying partial writes. Multipart uploads use the documented file/name fields, accepted MIME types, bounded file bytes and a stable serialized payload/key across retries.
- The generator supports the additional recursive unions and primitive-array variants used by templates, rules, journeys and forms; new Attribute schema maps to ContactAttribute to avoid a System.Attribute collision. No existing DTO type was renamed.

## Integration readiness

Four live workflows are prepared: contact creation/retrieval, list/membership lifecycle, event tracking and transactional email to a documented nondelivering example.com test address. They require explicit environment authorization plus credentials and an expected isolated workspace ID; email also needs a configured sender. Six ordinary tests validate the gates. **No live API tests were executed.** See [integration instructions](INTEGRATION-TESTS.md).

The solution includes console, ASP.NET Core and background-worker examples. No example is started by a build/test. Troubleshooting guidance covers configuration, server errors, idempotency, pagination, uploads/downloads and contract discrepancies.

## New and modified files

- Modified: `.gitignore`
- Modified: `Kirora.sln`
- Modified: `README.md`
- New: `docs/API-COVERAGE.md`
- New: `docs/INTEGRATION-TESTS.md`
- New: `docs/TROUBLESHOOTING.md`
- New: `docs/api-coverage.csv`
- New: `examples/Kirora.AspNetCore/Kirora.AspNetCore.csproj`
- New: `examples/Kirora.AspNetCore/Program.cs`
- New: `examples/Kirora.Examples/ExtendedResourceExamples.cs`
- New: `examples/Kirora.Worker/Kirora.Worker.csproj`
- New: `examples/Kirora.Worker/Program.cs`
- Modified: `spec/core-operations.json`
- Modified: `spec/core-unions.json`
- New: `spec/phase2-operation-ids.json`
- New: `spec/sdk-parity.json`
- Modified: `src/Kirora/DependencyInjection/KiroraServiceCollectionExtensions.cs`
- Modified: `src/Kirora/Exceptions/KiroraApiException.cs`
- Modified: `src/Kirora/Exceptions/KiroraTransportException.cs`
- Modified: `src/Kirora/Http/CoreJsonContext.g.cs`
- Modified: `src/Kirora/Http/KiroraHttpTransport.cs`
- Modified: `src/Kirora/Http/KiroraResponseMetadata.cs`
- Modified: `src/Kirora/Http/Operations.g.cs`
- Modified: `src/Kirora/Kirora.csproj`
- Modified: `src/Kirora/KiroraClient.cs`
- Modified: `src/Kirora/KiroraOptions.cs`
- Modified: `src/Kirora/KiroraRequestOptions.cs`
- Modified: `src/Kirora/Models/CoreModels.g.cs`
- Modified: `src/Kirora/Models/FoundationModels.g.cs`
- New: `src/Kirora/Resources/AssetsResource.g.cs`
- New: `src/Kirora/Resources/AttributesResource.g.cs`
- New: `src/Kirora/Resources/CampaignsResource.g.cs`
- Modified: `src/Kirora/Resources/ContactsResource.g.cs`
- New: `src/Kirora/Resources/ConversionGoalsResource.g.cs`
- New: `src/Kirora/Resources/EmbedSessionsResource.g.cs`
- Modified: `src/Kirora/Resources/EventsResource.g.cs`
- New: `src/Kirora/Resources/FormsResource.g.cs`
- New: `src/Kirora/Resources/JourneysResource.g.cs`
- Modified: `src/Kirora/Resources/ListsResource.g.cs`
- New: `src/Kirora/Resources/MeResource.g.cs`
- Modified: `src/Kirora/Resources/MessagesResource.g.cs`
- New: `src/Kirora/Resources/PushProjectsResource.g.cs`
- New: `src/Kirora/Resources/ReportsResource.g.cs`
- New: `src/Kirora/Resources/SegmentsResource.g.cs`
- New: `src/Kirora/Resources/SendersResource.g.cs`
- New: `src/Kirora/Resources/SendingDomainsResource.g.cs`
- New: `src/Kirora/Resources/SharedBlocksResource.g.cs`
- New: `src/Kirora/Resources/SuppressionsResource.g.cs`
- New: `src/Kirora/Resources/TagsResource.g.cs`
- New: `src/Kirora/Resources/TemplatesResource.g.cs`
- New: `src/Kirora/Resources/TopicsResource.g.cs`
- Modified: `src/Kirora/Resources/TransactionalResource.g.cs`
- New: `src/Kirora/Resources/WebhooksResource.g.cs`
- New: `src/Kirora/Resources/WorkspacesResource.g.cs`
- Modified: `src/Kirora/Serialization/ContractValidator.cs`
- Modified: `src/Kirora/Serialization/Optional.cs`
- New: `tests/Kirora.IntegrationTests/IntegrationGateTests.cs`
- New: `tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj`
- New: `tests/Kirora.IntegrationTests/LiveIntegrationTests.cs`
- Modified: `tests/Kirora.Tests/CoreContractTests.cs`
- New: `tests/Kirora.Tests/ExtendedEndpointInvoker.g.cs`
- New: `tests/Kirora.Tests/ExtendedTransportTests.cs`
- Modified: `tests/Kirora.Tests/Fixtures/core-examples.json`
- New: `tests/Kirora.Tests/Fixtures/public-api-phase2.txt`
- Modified: `tests/Kirora.Tests/Kirora.Tests.csproj`
- New: `tests/Kirora.Tests/PublicApiTests.cs`
- New: `tools/Kirora.ApiCompatibility/Kirora.ApiCompatibility.csproj`
- New: `tools/Kirora.ApiCompatibility/Program.cs`
- New: `tools/check-generated.sh`
- New: `tools/codegen_xml.py`
- Modified: `tools/generate-core-fixtures.py`
- Modified: `tools/generate-core.py`
- New: `tools/generate-coverage.py`
- New: `tools/generate-endpoint-invoker.py`
- Modified: `tools/generate-foundation-models.py`
- New: `docs/PHASE3.md` (this report).

## Alpha release assessment

The SDK is a technical alpha candidate, subject to final validation and local-package consumption. There are no missing pinned-spec endpoints. Publication is not authorized and has not occurred. Before public release, the maintainer must confirm SDK licensing/package ownership, repository/source provenance and SourceLink, support/release policy and website-versus-spec disagreements. Live verification remains deliberately unexecuted; endpoint coverage and fake HTTP tests are not deployed parity. Native AOT and convenience webhook verification/user-token helpers remain outside this phase.

## Final validation

- Restore and Release solution build passed with zero warnings/errors.
- 1560 tests passed; zero failed; 4 explicitly gated live workflows skipped. Live API tests executed: zero.
- The Phase 2 public API availability regression and resource XML documentation checks passed.
- All five offline generation/fixture/coverage consistency checks passed.
- Release package creation passed; the nupkg contains the net8.0 library, XML documentation and README, without tests or source files.
- A separate consumer restored the locally packed artifact and exercised DI, an extended resource and typed RFM decoding with fake HTTP.
- Package: src/Kirora/bin/Release/Kirora.0.1.0-alpha.1.nupkg (1679330 bytes). Nothing was published.
