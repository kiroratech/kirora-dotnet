# OpenAPI migration to 30043317 — review record

## Provenance and isolation

- Original pin: `d4e38cb942645976d01e3e109ed586c8d8783feb`; original implementation preserved in local baseline commit `659dad6`.
- Target: [official immutable commit 30043317efd68ffceeab882019431e57b7463018](https://github.com/kiroratech/openapi/commit/30043317efd68ffceeab882019431e57b7463018).
- SHA-256: `b6b5b3a071f06b91b9d6ca271e10f4baca1e3dcb32d75303d83b67b1b7547653`. API version remains 1.0.0 and OpenAPI 3.1.0.
- Branch: `chore/update-kirora-openapi`. The SDK had no dedicated repository and inherited an unrelated home repository. A nested SDK repository now preserves its baseline on main; migration edits are exclusively on the requested branch. No remote was configured, no push, merge, publication or live API call was performed.
- Upstream MIT license retained unchanged; THIRD-PARTY-NOTICES.txt includes its full text and the new pin. SDK licensing remains a separate maintainer decision.

## Coverage and intentional pre-release changes

184/184 operations have typed resource methods, across 25 tags. There are 1,090 generated models/unions and 204 official-example contract fixtures.

- Added `Templates.GetBrandKitAsync()` for `GET /v1/brand-kit`.
- Added `Journeys.GetActivationCheckAsync(id)` for `GET /v1/journeys/{id}/activation-check`.
- Removed `Templates.GetBrandThemeAsync()`, its response DTO, and the obsolete `/v1/brand-theme` route. No compatibility wrapper sends to the removed route or pretends that the new response has the old shape.
- `AssetUsage.BrandTheme` and `TemplateVersion.BrandTheme` become `BrandKit`; the latter is a structured snapshot, not just EmailTheme.
- `SendTransactionalInput.Deliver` becomes `Optional<TransactionalDeliverInput>`. Only `"all"` remains a valid string. Replace bare `"first"` with an explicitly ordered `TransactionalDeliverFirstInput`. The previous verification prose incorrectly implied both old strings remained accepted; this migration corrects that statement.
- `SocialBlock.Links` and `SocialBlockInput.Links` become optional. Check `IsSet` before reading `.Value`; omitted links use brand-kit defaults.
- Reports use shared DeliverabilityHealth/DeliverabilityLimits model types instead of the previous inline classes. Nested generated type changes and removals are enumerated below.
- Newly required properties break source callers constructing response DTOs even though their added wire fields are usually additive. Nullable required fields still must appear, including TransactionalMessage.FallbackFor and nullable brand-kit fields. Their complete member changes are enumerated below.

## Usage migration

```csharp
var brand = await kirora.Templates.GetBrandKitAsync(cancellationToken: cancellationToken);
var check = await kirora.Journeys.GetActivationCheckAsync(journeyId, cancellationToken: cancellationToken);
TransactionalDeliverInput priority = new TransactionalDeliverFirstInput
{
    First = ["push", "email"]
};
// Assign Deliver = priority in SendTransactionalInput; use (TransactionalDeliverInput)"all" for every channel.
```

## Public API review

Historical Phase 2 and Phase 3 snapshots are retained unchanged. The new full baseline has 7,203 signatures. The machine-readable [migration manifest](../tests/Kirora.Tests/Fixtures/migration-api-changes.json) records exact historical removals, new signatures, and model-member type/required changes. Unit tests and tools/check-public-api.py require the exact new baseline and exact historical deltas; unexpected changes fail. No compatibility failure is silently ignored.

Phase 2: 1 missing/changed signatures. Phase 3: 30 missing/changed signatures; 174 added signatures. A type change produces a removed old signature and an added replacement. These counts describe signature deltas, not independent conceptual defects.

### Every removed or replaced Phase 3 signature

- `C:Kirora.Models.GetBrandThemeResponse:Void .ctor()`
- `C:Kirora.Models.OverviewDeliverabilityHealth:Void .ctor()`
- `C:Kirora.Models.OverviewDeliverabilityHealthLimits:Void .ctor()`
- `C:Kirora.Models.OverviewDeliverabilityHealthLimitsCritical:Void .ctor()`
- `C:Kirora.Models.OverviewDeliverabilityHealthLimitsWatch:Void .ctor()`
- `M:Kirora.Resources.TemplatesResource:System.Threading.Tasks.Task`1[Kirora.Models.GetBrandThemeResponse] GetBrandThemeAsync(Kirora.KiroraRequestOptions, System.Threading.CancellationToken)`
- `P:Kirora.Models.AssetUsage:System.Boolean:BrandTheme:get=True:set=True`
- `P:Kirora.Models.GetBrandThemeResponse:Kirora.Models.EmailTheme:Data:get=True:set=True`
- `P:Kirora.Models.Overview:Kirora.Models.OverviewDeliverabilityHealth:DeliverabilityHealth:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:Kirora.Models.OverviewDeliverabilityHealthLimits:Limits:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:System.Decimal:MinimumFeedbackCoverage:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:System.Decimal:MinimumSampleSize:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:System.Int64:SampleSize:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:System.Nullable`1[System.Decimal]:FeedbackCoverage:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealth:System.String:Band:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimits:Kirora.Models.OverviewDeliverabilityHealthLimitsCritical:Critical:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimits:Kirora.Models.OverviewDeliverabilityHealthLimitsWatch:Watch:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimitsCritical:System.Decimal:BounceRatio:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimitsCritical:System.Decimal:ComplaintRatio:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimitsWatch:System.Decimal:BounceRatio:get=True:set=True`
- `P:Kirora.Models.OverviewDeliverabilityHealthLimitsWatch:System.Decimal:ComplaintRatio:get=True:set=True`
- `P:Kirora.Models.SendTransactionalInput:Kirora.Serialization.Optional`1[System.String]:Deliver:get=True:set=True`
- `P:Kirora.Models.SocialBlock:System.Collections.Generic.IReadOnlyList`1[Kirora.Models.SocialBlockLinksItem]:Links:get=True:set=True`
- `P:Kirora.Models.SocialBlockInput:System.Collections.Generic.IReadOnlyList`1[Kirora.Models.SocialBlockInputLinksItem]:Links:get=True:set=True`
- `P:Kirora.Models.TemplateVersion:Kirora.Models.EmailTheme:BrandTheme:get=True:set=True`
- `T:Kirora.Models.GetBrandThemeResponse:System.Object:True:False`
- `T:Kirora.Models.OverviewDeliverabilityHealth:System.Object:True:False`
- `T:Kirora.Models.OverviewDeliverabilityHealthLimits:System.Object:True:False`
- `T:Kirora.Models.OverviewDeliverabilityHealthLimitsCritical:System.Object:True:False`
- `T:Kirora.Models.OverviewDeliverabilityHealthLimitsWatch:System.Object:True:False`

### Every changed member in an existing generated model

Each entry records type and C# required status; newly introduced classes are covered by the new signature baseline and upstream schema report.
- `AssetUsage.BrandKit`: `null` → `{"required": true, "type": "bool"}`.
- `AssetUsage.BrandTheme`: `{"required": true, "type": "bool"}` → `null`.
- `CampaignEligibility.AbTestSplit`: `null` → `{"required": true, "type": "CampaignEligibilityAbTestSplit?"}`.
- `CampaignMessageCounts.Simulated`: `null` → `{"required": true, "type": "long"}`.
- `CampaignMetrics.DeliverabilityHealth`: `null` → `{"required": true, "type": "DeliverabilityHealth?"}`.
- `CreateFormInput.NotifyUserIds`: `null` → `{"required": false, "type": "Optional<IReadOnlyList<Guid>>"}`.
- `EmailBlockStyle.MobilePadding`: `null` → `{"required": false, "type": "Optional<EmailBlockStyleMobilePadding>"}`.
- `EmailBlockStyleInput.MobilePadding`: `null` → `{"required": false, "type": "Optional<EmailBlockStyleInputMobilePadding>"}`.
- `EmailTheme.BaseFontSize`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EmailTheme.ButtonTextColor`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EmailTheme.HeadingColor`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EmailThemeInput.BaseFontSize`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EmailThemeInput.ButtonTextColor`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EmailThemeInput.HeadingColor`: `null` → `{"required": false, "type": "Optional<string>"}`.
- `EventAccepted.ExpiresAt`: `null` → `{"required": true, "type": "DateTimeOffset"}`.
- `Form.NotifyUserIds`: `null` → `{"required": true, "type": "IReadOnlyList<Guid>"}`.
- `FormListItem.NotifyUserIds`: `null` → `{"required": true, "type": "IReadOnlyList<Guid>"}`.
- `JourneyEmailCounts.Simulated`: `null` → `{"required": true, "type": "long"}`.
- `JourneySimulationPathItem.Issues`: `null` → `{"required": false, "type": "Optional<IReadOnlyList<JourneyActivationIssue>>"}`.
- `ListUsage.Goals`: `null` → `{"required": true, "type": "IReadOnlyList<ListUsageGoalsItem>"}`.
- `ListUsage.SharedBlocks`: `null` → `{"required": true, "type": "IReadOnlyList<ListUsageSharedBlocksItem>"}`.
- `ListUsage.Templates`: `null` → `{"required": true, "type": "IReadOnlyList<ListUsageTemplatesItem>"}`.
- `Overview.DeliverabilityHealth`: `{"required": true, "type": "OverviewDeliverabilityHealth"}` → `{"required": true, "type": "DeliverabilityHealth"}`.
- `OverviewCounts.FailedBeforeSending`: `null` → `{"required": true, "type": "long"}`.
- `OverviewCounts.Simulated`: `null` → `{"required": true, "type": "long"}`.
- `SendTransactionalInput.Deliver`: `{"required": false, "type": "Optional<string>"}` → `{"required": false, "type": "Optional<TransactionalDeliverInput>"}`.
- `SendTransactionalMessagesBatchResultResultsItemAccepted.SkippedChannels`: `null` → `{"required": true, "type": "IReadOnlyList<TransactionalSkippedChannel>"}`.
- `SharedBlockUsage.BrandKit`: `null` → `{"required": true, "type": "bool"}`.
- `SocialBlock.Links`: `{"required": true, "type": "IReadOnlyList<SocialBlockLinksItem>"}` → `{"required": false, "type": "Optional<IReadOnlyList<SocialBlockLinksItem>>"}`.
- `SocialBlockInput.Links`: `{"required": true, "type": "IReadOnlyList<SocialBlockInputLinksItem>"}` → `{"required": false, "type": "Optional<IReadOnlyList<SocialBlockInputLinksItem>>"}`.
- `TemplateVersion.BrandKit`: `null` → `{"required": true, "type": "TemplateVersionBrandKit"}`.
- `TemplateVersion.BrandTheme`: `{"required": true, "type": "EmailTheme"}` → `null`.
- `Topic.Counts`: `null` → `{"required": true, "type": "TopicCounts"}`.
- `TrackedEvent.ExpiresAt`: `null` → `{"required": true, "type": "DateTimeOffset"}`.
- `TransactionalMessage.FallbackFor`: `null` → `{"required": true, "type": "Guid?"}`.
- `TransactionalSendResult.SkippedChannels`: `null` → `{"required": true, "type": "IReadOnlyList<TransactionalSkippedChannel>"}`.
- `UpdateFormInput.NotifyUserIds`: `null` → `{"required": false, "type": "Optional<IReadOnlyList<Guid>>"}`.

## Scope and remaining concerns

All 37 structurally changed schemas were regenerated from the immutable contract. Parameterized tests compare their generated required/optional declarations with the embedded specification. Endpoint fixtures cover all operations, authorization, serialization, success responses and standard errors; dedicated tests cover delivery priorities, rejection of the obsolete first string, optional social links, required nullable fields, new endpoint cancellation and removal of the obsolete public method/route.

The [previous endpoint/schema comparison](upstream-2026-11-02/CHANGE-REPORT.md) and its exact JSON before/after report remain historical artifacts. Deployment aliases and live compatibility remain unverified. This migration intentionally changes an unreleased API; consumers must adapt before rebuilding. Historical signature snapshots do not capture all C# required/nullability semantics, so explicit member changes above and schema tests supplement them. Native AOT should be revalidated after regeneration before renewing platform claims.

## Changed files

- `README.md`
- `.gitignore`
- `THIRD-PARTY-NOTICES.txt`
- `tools/generate-endpoint-invoker.py`
- `tools/generate-coverage.py`
- `tools/check-public-api.py`
- `spec/core-unions.json`
- `spec/source.json`
- `spec/core-operations.json`
- `spec/openapi.json`
- `docs/UPSTREAM-VERIFICATION.md`
- `docs/OPENAPI-MIGRATION.md`
- `docs/API-COVERAGE.md`
- `docs/api-coverage.csv`
- `tests/Kirora.Tests/OpenApiMigrationTests.cs`
- `tests/Kirora.Tests/Kirora.Tests.csproj`
- `tests/Kirora.Tests/ExtendedEndpointInvoker.g.cs`
- `tests/Kirora.Tests/PublicApiTests.cs`
- `tests/Kirora.Tests/Fixtures/public-api-upstream-30043317.txt`
- `tests/Kirora.Tests/Fixtures/migration-api-changes.json`
- `tests/Kirora.Tests/Fixtures/core-examples.json`
- `examples/Kirora.Examples/CoreResourceExamples.cs`
- `src/Kirora/Serialization/OptionalConverters.g.cs`
- `src/Kirora/Resources/JourneysResource.g.cs`
- `src/Kirora/Resources/TemplatesResource.g.cs`
- `src/Kirora/Models/CoreModels.g.cs`
- `src/Kirora/Http/Operations.g.cs`
- `src/Kirora/Http/CoreJsonContext.g.cs`

## Final validation

- `dotnet restore`: passed.
- Release build: passed, zero warnings and errors.
- Release tests: 1,638 SDK tests plus six integration-gate tests passed (1,644 total); four live tests skipped with KIRORA_RUN_INTEGRATION=0. No live API calls.
- Seven Python drift-comparator tests passed.
- All five generation/coverage checks passed: 184/184 operations, 1,090 models/unions, 204 fixtures. The CSV current-coverage column replaces the historical phase3 label.
- Public API check passed against the exact new 7,203-signature baseline and documented historical deltas. Original snapshot files remain unchanged.
- Local NuGet and portable symbols packages created; package validation passed, including upstream MIT notice inclusion. SDK license/repository/ownership decisions remain release prerequisites.
- `git diff --check`: passed.
- Staging validation initially needed restore and caught an xUnit analyzer error in a new test; both were corrected before the final successful run.
- Validation logs are local ignored artifacts under artifacts/openapi-migration/. Native AOT publication was not repeated for this migration; no new AOT support claim is made.
