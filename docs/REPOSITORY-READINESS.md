# Official repository and release-readiness review

## Git preparation

The official repository is https://github.com/kiroratech/kirora-dotnet. Read-only GitHub metadata reports a public, accessible, unarchived repository, size 0, default branch main, and no license. `git ls-remote --symref` returned no refs. Read access is verified; write permission is not established by those public checks. GitHub CLI is not installed; verification used Git and the public GitHub REST API.

Local main preserves baseline 659dad6, and chore/update-kirora-openapi preserves migration 9f07299. Those commits and their topology are retained. origin is configured to the official HTTPS URL. No push, force-push, history rewrite, merge, GitHub release or NuGet publication is authorized or performed. main remains the intended default branch; publishing both refs would allow review of migration as a pull request.

## Repository audit and decisions

The audit covers every reachable Git blob across all local refs and the current tracked paths. The original two commits contained two Finder metadata files: src/.DS_Store and src/Kirora/.DS_Store. They are removed from the current migration branch and ignored, but remain in the preserved baseline main tree and history. No build outputs, package archives, environment files or private-key files were found in that original history. Redacted heuristic audit results are stored under ignored artifacts/.

Historical Finder metadata is an unresolved publication decision: preserving the exact baseline/migration commit IDs retains these blobs. Removing them from unpublished history requires separate approval for a targeted history rewrite, changes commit IDs, and requires rebuilding SourceLink packages. Nothing rewrites them automatically. Do not push until the maintainer accepts retained metadata or authorizes its removal.

Heuristic and automated scanners do not prove absence of secrets. Reports never print raw matches; findings must be resolved before publication. No real Kirora credentials were supplied or used for validation. The SDK's MIT specification attribution remains included; an SDK license has not been selected, and the upstream spec license does not license the whole SDK.

## Repository metadata and CI

eng/release.json and MSBuild now use the confirmed official repository URL. MaintainerApproved remains false and SDK licenseExpression remains unset. Package ownership/license and required GitHub nuget-alpha reviewers with prevention of self-review still require maintainer configuration before any alpha publication. The existing manual publication workflow fails closed on those missing decisions.

Dedicated checkouts enable the .NET 8 SDK's built-in GitHub SourceLink provider; isolated source copies without .git disable it. tools/check-sourcelink.py extracts SourceLink from the actual portable PDB, compares its mapping with generated metadata, and verifies that it matches the NuGet repository commit. Mapping correctness can be checked locally; raw GitHub source URLs cannot resolve until an approved push makes that commit available remotely.

The normal CI retains pinned actions, read-only permissions, three-OS restore/build/test/pack, model consistency, explicit API delta checks, dependency audit and Linux AOT. It now also checks PDB SourceLink and an independent NuGet consumer. Both live run flags are set to 0 in validation/publication jobs; live test gates independently refuse known CI runners. No GitHub workflow has been uploaded or run during this task.

## Live validation readiness

See INTEGRATION-TESTS.md for exact environment variables, scopes and separate commands for read-only smoke, contact/list/event mutation tests and separately approved email testing. The four existing mutation workflows now fail closed unless the expected workspace has no journeys and no campaigns (including archived), with an additional explicit exclusive-workspace attestation. Checks cannot lock remote configuration: exclusive operator control is required to prevent changes between preflight and event ingestion.

Email is restricted in code to unique sdk-integration-*@example.com recipients and requires a separate approval token for that namespace and a verified sender. Fixed failure text/HTTP status prevents server-provided codes, IDs, messages and body data from entering integration reports. Offline tests cover malicious credential echoes. The SDK itself preserves structured exceptions; consumers must not log raw headers, exception data or external diagnostics containing secrets.

## Technical validation

Final results and any toolchain limitations are appended after local validation. No live Kirora API calls were made. The Native AOT smoke and independent package consumer both use fake HTTP and exercise the new brand-kit and journey activation-check models and transactional delivery union.

## Exact next Git commands — approval required

Resolve the historical Finder metadata decision first. If the same history is retained and the remote is still empty, after explicit push approval:

```sh
cd /Users/Osigbemes/Kirora-dotnet
git status --short
git ls-remote --symref origin
git push --dry-run --atomic origin main chore/update-kirora-openapi
git push --atomic --set-upstream origin main chore/update-kirora-openapi
```

Stop and reconcile if remote refs appear; do not force-push or overwrite remote history. These commands are prepared instructions, not an executed or authorized push. They do not merge the migration. Review it through a pull request afterward; NuGet publication remains separately approval-gated.

## Exact offline validation commands

```sh
KIRORA_RUN_INTEGRATION=0 KIRORA_RUN_SMOKE=0 dotnet restore --locked-mode
dotnet build --configuration Release
KIRORA_RUN_INTEGRATION=0 KIRORA_RUN_SMOKE=0 dotnet test --configuration Release
sh tools/check-generated.sh
python3 tools/check-public-api.py
python3 -m unittest discover -s tests/upstream -v
dotnet pack --configuration Release
python3 tools/validate-package.py
python3 tools/check-sourcelink.py
python3 tools/check-package-consumer.py
python3 tools/check-reproducible.py
python3 tools/audit-repository.py
```

Dependency advisory lookup is separate from strictly offline checks: `python3 tools/check-dependencies.py` queries NuGet data and makes no Kirora calls. Native AOT reproduction on the verified macOS Intel toolchain:

```sh
dotnet publish examples/Kirora.NativeAotSmoke --configuration Release --runtime osx-x64 -p:PublishAot=true -p:TrimmerSingleWarn=false -p:EnableTrimAnalyzer=true -p:EnableAotAnalyzer=true --output artifacts/aot-readiness
artifacts/aot-readiness/Kirora.NativeAotSmoke --require-native
```

## Final evidence and readiness

- Release restore/build succeeded; ordinary build has zero warnings/errors. The final offline suite passes 1,638 SDK tests and 27 integration-gate/safety tests (1,665 total); five live tests skipped. Seven Python drift-checker tests passed.
- Generation/API checks passed unchanged: 184/184 pinned operations, 1,090 models/unions, 204 fixtures and the exact migrated 7,203-signature API baseline. No SDK public API changes were introduced during preparation.
- NuGet and symbols packages were created and validated. A separate .NET 8 application installed the exact local package into an isolated fresh cache, with no project references, then exercised brand-kit, journey activation-check, the delivery union and optional social links through fake HTTP.
- Reproducibility passed: DLL and portable PDB were identical across two isolated build paths under SDK 8.0.404.
- Actual PDB SourceLink validation passed after a confirmed defect was fixed: a manually set PathMap had normalized document paths to /_/ while SourceLink retained physical keys, leaving all 1,644 documents unmapped. Dedicated checkouts now use SDK deterministic SourceRoot mapping consistently. All 1,644 documents are covered: 1,600 embedded and 44 Git-backed. The PDB mapping and package repository commit agree (validated at ca56a7574fcd1c2892eb8fb770f251729aee9734). URLs remain unavailable remotely until an approved push. A history rewrite requires rebuilding these artifacts.
- Native AOT osx-x64 publish and --require-native execution passed with reflection JSON disabled and dynamic-code support unavailable. The expanded fake-HTTP smoke covers the new endpoints and structured delivery input. The known framework System.Net.Quic IL3000 warning and Apple -ld_classic deprecation remain; no SDK serialization/trimming/AOT warnings were reported. This does not establish live API interoperability, QUIC behavior, or Linux/Windows AOT support.
- NuGet advisory audit passed for all seven projects, with no reported vulnerable direct/transitive packages. All three GitHub workflow files passed actionlint; GitHub-hosted execution and environment protection configuration remain unverified.
- Secret audit: checksum-verified official Gitleaks 8.30.1 scanned all initial/preparation history and raised six generic-key candidates. They are three exact public upstream response examples (two webhook secrets and one embed-session token), duplicated in the spec and generated fixtures. The whole-blob heuristic found those twelve occurrences across two spec/fixture revisions plus one intentional invalid-URL fixture. Exact digests, permitted paths and official JSON pointers are recorded in eng/secret-audit-examples.json; the audit verifies them against the pinned spec and reports them separately. Nothing blankets out files/rules, and Gitleaks findings remain in the redacted raw report. No unexplained credential candidates remain; live validity of upstream examples was not tested. Neither scanner proves absence of all possible secrets.
- The two Finder metadata blobs remain in the preserved baseline main tree and history. Current migration-branch tracked files contain no .DS_Store, credentials/environment files, build outputs, packages, test reports or cache directories. No history was rewritten.
- Release-only validators intentionally remain blocked: maintainer approval and SDK SPDX license are unset. Repository URL/SourceLink now validate; package ownership and GitHub publication reviewer protections still require maintainer confirmation/configuration. No publication gate was bypassed.

Readiness: suitable for maintainer review and separately authorized local smoke testing. Not approved for a first push or alpha publication until the historical metadata decision and the applicable approval/configuration steps are resolved. Validation logs and redacted scan reports are ignored local artifacts under artifacts/; no Kirora live tests, GitHub push, branch merge, GitHub release or NuGet publication was executed.
