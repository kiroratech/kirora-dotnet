# Phase 4 — Release hardening and public alpha preparation

## Audit findings and fixes

- **Callback failures could retransmit HTTP requests.** ResponseReceived exceptions that resembled transport/cancellation errors were wrapped and retried. They now propagate as the original exception without any retry, with regression cases for HTTP/IO/cancellation/application exceptions.
- **Required response nulls escaped type safety.** JSON required-presence metadata accepted explicit null reference fields, allowing later null-reference failures. Success envelopes are now checked against the pinned contract before DTO use, while allowing unknown response fields and string enums.
- **Pagination errors lost correlation.** Repeated-cursor errors now retain the actual last response metadata/request ID rather than an invented blank response.
- **Configured retry delays could exceed the key replay window.** A single call now stops keyed retries before 24 hours from its first attempt using monotonic/UTC elapsed time, preserving its last failure. Applications remain responsible for the original age of keys reused across calls. A regression checks a server-requested 24-hour delay against a two-day configured delay budget.
- **Retry header parsing had an overflow boundary and incomplete HTTP-date support.** Untrusted extreme delays saturate safely, fractional ticks round upward and all three HTTP-date forms are recognized. Refusals are never retried earlier merely to fit the delay budget.
- **Non-JSON proxy errors had inaccurate fallback codes.** 405/413/415 now use METHOD_NOT_ALLOWED/PAYLOAD_TOO_LARGE/UNSUPPORTED_MEDIA_TYPE while preserving status/request metadata.
- **Factory logging could expose identifier-bearing URIs.** Default HttpClientFactory logging is removed and header logging is redacted. A regression test checks that credentials/contact paths are not emitted by default.
- **The directory inherited an unrelated ancestor Git repository.** Ordinary builds disable repository/SourceLink inference; the release procedure requires a dedicated root and an explicitly approved matching remote. No Git repository/remote was created or modified.
- **Optional converters depended on runtime generic construction.** Generated SDK types now have statically rooted converters and JsonTypeInfo-based transport/union serialization. Consumer-defined Optional types retain a guarded JIT fallback; unsupported consumer types must register metadata/converters for AOT.
- **Two high-severity advisories appeared in the old test-only dependency graph.** Updated xUnit/runner/Test SDK remove the obsolete System.Net.Http/Regex transitive packages. The follow-up solution NuGet scan reports no vulnerable direct/transitive dependencies.
- **Packaging/documentation gaps.** Explicit package ID/tags, portable symbol output, lock files, stable paths, dynamic build-version User-Agent and strict local/release validators are prepared. Non-library projects are not packaged. Generated property documentation now uses schema descriptions and public docs compile without CS1591 suppression.
- **Generation pin checks could rely on Python assertions.** Critical source/version/coverage checks now fail explicitly even with optimized Python; unsupported future schema constructs fail instead of silently becoming an untyped model.

## Scope preserved

No API endpoints were added. Coverage remains 183/183 against unchanged spec/openapi.json/source.json. Existing public API type/member signatures are protected by both Phase 2 and Phase 3 baselines; no baseline was regenerated from the hardened assembly to hide a removal. Existing async names, resource properties, DTO shapes and the net8.0 target remain. Nullable metadata/semantic behavior still need human review in addition to signature checks.

## HTTP/lifetime/cancellation review

The client snapshots options, keeps caller-owned HttpClient alive, applies request-local authentication/tenant/key headers, disposes owned request/response/body objects and leaves CSV destinations open. Each logical request snapshots its body once and reuses keys/bytes across retries; batches preserve ordered results and entry keys without mutating caller DTOs. Cancellation covers sends, reads, delays and iterators; configured timeouts bound attempts. Partial CSV output is not replayed. Caller-provided mutable models/streams and custom transports remain the caller responsibility.

## CI and publication files

- .github/workflows/ci.yml: three-OS restore/build/test/package, all generation checks, both public API baselines, vulnerability audit, artifact validation and a separate Linux native-AOT smoke.
- .github/workflows/publish-alpha.yml: workflow_dispatch only on the default branch, exact confirmation, approved checked-in metadata, required-reviewer/prevent-self-review environment preflight, rerun validation, strict package/SourceLink checks and protected NuGet secret access. No GitHub release step exists.
- Actions are pinned to official immutable SHAs recorded in eng/action-pins.json. Default permissions are read-only; no pull_request_target workflow or PR secret access is used.
- Both workflows were linted locally with actionlint v1.7.7. They were not uploaded, executed or configured on GitHub during this task. Remote environment protections must still be created/verified by the maintainer.

## Native AOT and live validation

The .NET 8 osx-x64 native binary published and ran with dynamic code and reflection JSON disabled, using fake HTTP only. It exercised DI, optional fields, unions/recursive content, errors, pagination, multipart and CSV. Framework IL3000 (System.Net.Quic Assembly.Location) and Apple -ld_classic deprecation warnings remain disclosed in NATIVE-AOT.md. This is not blanket certification of all endpoints/platforms; no Linux/Windows native run was performed locally. The SDK does not declare IsAotCompatible based on this single smoke.

Four live API workflows remain explicitly gated and were not executed. The six offline authorization-gate tests continue to run. Credentials, dedicated workspace UUID, explicit dual opt-in and a verified sender for fixed example.com recipients are documented in INTEGRATION-TESTS.md.

## Decisions blocking public publication

- Confirm NuGet ID ownership, alpha version and authors.
- Select/approve an SDK license; spec/LICENSE does not assign SDK terms.
- Establish the SDK Git root and canonical URL; verify SourceLink against that actual remote/commit.
- Decide whether to supply an approved optional package icon. No brand asset was invented.
- Configure GitHub environment reviewers/prevent-self-review and a scoped publication credential; review the exact artifact and separately authorize publication.
- Record live-validation deferral or authorize a dedicated workspace run; resolve website/pinned-spec differences before claiming deployed parity.

The artifact is a technically tested alpha candidate, not a published or maintainer-approved release. RELEASE-CHECKLIST.md contains the actionable final checklist.

## New and modified files

- New: `.github/workflows/ci.yml`
- New: `.github/workflows/publish-alpha.yml`
- Modified: `Directory.Build.props`
- Modified: `Kirora.sln`
- Modified: `README.md`
- Modified: `docs/API-COVERAGE.md`
- Modified: `docs/INTEGRATION-TESTS.md`
- New: `docs/NATIVE-AOT.md`
- New: `docs/PACKAGE-READINESS.md`
- New: `docs/RELEASE-CHECKLIST.md`
- Modified: `docs/TROUBLESHOOTING.md`
- New: `eng/action-pins.json`
- New: `eng/release.json`
- Modified: `examples/Kirora.AspNetCore/Kirora.AspNetCore.csproj`
- New: `examples/Kirora.AspNetCore/packages.lock.json`
- New: `examples/Kirora.Examples/packages.lock.json`
- New: `examples/Kirora.NativeAotSmoke/Kirora.NativeAotSmoke.csproj`
- New: `examples/Kirora.NativeAotSmoke/Program.cs`
- New: `examples/Kirora.NativeAotSmoke/packages.lock.json`
- New: `examples/Kirora.Worker/packages.lock.json`
- Modified: `src/Kirora/DependencyInjection/KiroraServiceCollectionExtensions.cs`
- Modified: `src/Kirora/Http/KiroraHttpTransport.cs`
- Modified: `src/Kirora/Http/KiroraResponseMetadata.cs`
- Modified: `src/Kirora/Kirora.csproj`
- Modified: `src/Kirora/Models/CoreModels.g.cs`
- Modified: `src/Kirora/Resources/AssetsResource.g.cs`
- Modified: `src/Kirora/Resources/AttributesResource.g.cs`
- Modified: `src/Kirora/Resources/CampaignsResource.g.cs`
- Modified: `src/Kirora/Resources/ContactsResource.g.cs`
- Modified: `src/Kirora/Resources/ConversionGoalsResource.g.cs`
- Modified: `src/Kirora/Resources/EmbedSessionsResource.g.cs`
- Modified: `src/Kirora/Resources/EventsResource.g.cs`
- Modified: `src/Kirora/Resources/FormsResource.g.cs`
- Modified: `src/Kirora/Resources/JourneysResource.g.cs`
- Modified: `src/Kirora/Resources/ListsResource.g.cs`
- Modified: `src/Kirora/Resources/MeResource.g.cs`
- Modified: `src/Kirora/Resources/MessagesResource.g.cs`
- Modified: `src/Kirora/Resources/PushProjectsResource.g.cs`
- Modified: `src/Kirora/Resources/ReportsResource.g.cs`
- Modified: `src/Kirora/Resources/SegmentsResource.g.cs`
- Modified: `src/Kirora/Resources/SendersResource.g.cs`
- Modified: `src/Kirora/Resources/SendingDomainsResource.g.cs`
- Modified: `src/Kirora/Resources/SharedBlocksResource.g.cs`
- Modified: `src/Kirora/Resources/SuppressionsResource.g.cs`
- Modified: `src/Kirora/Resources/TagsResource.g.cs`
- Modified: `src/Kirora/Resources/TemplatesResource.g.cs`
- Modified: `src/Kirora/Resources/TopicsResource.g.cs`
- Modified: `src/Kirora/Resources/TransactionalResource.g.cs`
- Modified: `src/Kirora/Resources/WebhooksResource.g.cs`
- Modified: `src/Kirora/Resources/WorkspacesResource.g.cs`
- Modified: `src/Kirora/Serialization/ContractValidator.cs`
- Modified: `src/Kirora/Serialization/Optional.cs`
- New: `src/Kirora/Serialization/OptionalConverters.g.cs`
- New: `src/Kirora/packages.lock.json`
- Modified: `tests/Kirora.IntegrationTests/Kirora.IntegrationTests.csproj`
- New: `tests/Kirora.IntegrationTests/packages.lock.json`
- New: `tests/Kirora.Tests/AuditRegressionTests.cs`
- New: `tests/Kirora.Tests/Fixtures/public-api-phase3.txt`
- Modified: `tests/Kirora.Tests/Kirora.Tests.csproj`
- Modified: `tests/Kirora.Tests/PublicApiTests.cs`
- New: `tests/Kirora.Tests/packages.lock.json`
- New: `tools/Kirora.ApiCompatibility/packages.lock.json`
- New: `tools/README.md`
- New: `tools/check-dependencies.py`
- New: `tools/check-public-api.py`
- New: `tools/check-publication-gate.py`
- New: `tools/check-reproducible.py`
- Modified: `tools/generate-core.py`
- Modified: `tools/generate-coverage.py`
- New: `tools/release-properties.py`
- New: `tools/validate-package.py`
- New: `docs/PHASE4.md` (this report).

## Final validation results

- Restore, locked restore and Release solution build passed: zero build warnings/errors.
- 1580 tests passed, 0 failed; four explicitly gated live workflows skipped. Live Kirora API requests executed: zero.
- All five generation/coverage checks and both public API baselines passed.
- Release solution pack emitted only Kirora.0.1.0-alpha.1.nupkg and its portable-symbol snupkg; local validation passed.
- NuGet audit passed for seven solution projects with no reported vulnerable direct/transitive packages.
- DLL/PDB reproducibility comparison across two isolated paths passed: {'dll': '6744a09cdc94feebd3662f7c852ebf45e7826440d45c156b47bbc819fc80fb78', 'pdb': '3352b019d8086c2cef3f017877a771f696b75b8606cc96711db59a5a6e9c96be'}.
- Both GitHub workflow files pass actionlint v1.7.7 (shellcheck/pyflakes integrations were disabled). Workflows were not executed on GitHub.
- The final .NET 8 osx-x64 Native AOT binary published and ran successfully with reflection JSON/dynamic code disabled. The documented framework IL3000 and Apple linker deprecation warnings remained; no SDK-origin trimming/AOT warnings were emitted.
- Release preflight/strict release checks intentionally remain blocked on unapproved maintainer metadata and SourceLink provenance. No publication or GitHub release occurred.
