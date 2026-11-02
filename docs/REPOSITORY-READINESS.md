# Official repository and release-readiness review

## Git preparation

The official repository is https://github.com/kiroratech/kirora-dotnet. Read-only GitHub metadata reports a public, accessible, unarchived repository, size 0, default branch main, and no license. `git ls-remote --symref` returned no refs. Read access is verified; write permission is not established by those public checks. GitHub CLI is not installed; verification used Git and the public GitHub REST API.

Local main preserves baseline 659dad6, and chore/update-kirora-openapi preserves migration 9f07299. Those commits and their topology are retained. origin is configured to the official HTTPS URL. No push, force-push, history rewrite, merge, GitHub release or NuGet publication is authorized or performed. main remains the intended default branch; publishing both refs would allow review of migration as a pull request.

## Repository audit and decisions

The audit covers every reachable Git blob across all local refs and the current tracked paths. The original two commits contained two Finder metadata files: src/.DS_Store and src/Kirora/.DS_Store. They are removed from the current branch and ignored, but remain in preserved history. No build outputs, package archives, environment files or private-key files were found in that original history. Redacted heuristic audit results are stored under ignored artifacts/.

Historical Finder metadata is an unresolved publication decision: preserving the exact baseline/migration commit IDs retains these blobs. Removing them from unpublished history requires separate approval for a targeted history rewrite, changes commit IDs, and requires rebuilding SourceLink packages. Nothing rewrites them automatically. Do not push until the maintainer accepts retained metadata or authorizes its removal.

Heuristic and automated scanners do not prove absence of secrets. Reports never print raw matches; findings must be resolved before publication. No real Kirora credentials were supplied or read for validation. The SDK's MIT specification attribution remains included; an SDK license has not been selected, and the upstream spec license does not license the whole SDK.

## Repository metadata and CI

eng/release.json and MSBuild now use the confirmed official repository URL. MaintainerApproved remains false and SDK licenseExpression remains unset. Package ownership/license and required GitHub nuget-alpha reviewers with prevention of self-review still require maintainer configuration before any alpha publication. The existing manual publication workflow fails closed on those missing decisions.

Dedicated checkouts enable the .NET 8 SDK's built-in GitHub SourceLink provider; isolated source copies without .git disable it. tools/check-sourcelink.py extracts SourceLink from the actual portable PDB, compares its mapping with generated metadata, and verifies that it matches the NuGet repository commit. Mapping correctness can be checked locally; raw GitHub source URLs cannot resolve until an approved push makes that commit available remotely.

The normal CI retains pinned actions, read-only permissions, three-OS restore/build/test/pack, model consistency, explicit API delta checks, dependency audit and Linux AOT. It now also checks PDB SourceLink and an independent NuGet consumer. Both live run flags are set to 0 in validation/publication jobs; live test gates independently refuse known CI runners. No GitHub workflow has been uploaded or run during this task.

## Live validation readiness

See INTEGRATION-TESTS.md for exact environment variables, scopes and separate commands for read-only smoke, contact/list/event mutation tests and separately approved email testing. The four existing mutation workflows now fail closed unless the expected workspace has no journeys and no campaigns (including archived), with an additional explicit exclusive-workspace attestation. Checks cannot lock remote configuration: exclusive operator control is required to prevent changes between preflight and event ingestion.

Email is restricted in code to unique sdk-integration-*@example.com recipients and requires a separate approval token for that namespace and a verified sender. Fixed failure text/HTTP status prevents server-provided codes, IDs, messages and body data from entering integration reports. Offline tests cover malicious credential echoes. The SDK itself preserves structured exceptions; consumers must not log raw headers, exception data or external diagnostics containing secrets.

## Technical validation

Final results and any toolchain limitations are appended after local validation. No live API calls will be made. The Native AOT smoke and independent package consumer both use fake HTTP and exercise the new brand-kit and journey activation-check models and transactional delivery union.

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
