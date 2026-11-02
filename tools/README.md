# Build and release tools

Normal builds never download or rewrite the pinned OpenAPI specification. `sh tools/check-generated.sh` runs the five offline model, metadata, fixture, invocation and coverage checks. Checksum/version mismatches fail explicitly, including when Python optimization is enabled.

`python tools/check-public-api.py` compiles the signature inspection tool and compares the current assembly to committed Phase 2/3 type/member baselines. This protects availability/signatures, not every semantic detail or nullable attribute. The unit suite also checks both baselines. Do not regenerate a baseline merely to make a breaking change pass review.

`python tools/check-dependencies.py` queries NuGet vulnerability data and fails on findings or an incomplete audit. It writes artifacts/dependency-audit.json. Lock files pin dependency resolution. Runtime-specific AOT restore locks go under obj to avoid replacing the portable, checked-in solution restore graph.

`python tools/validate-package.py` checks the local nupkg and snupkg. `--release` also requires approved license/repository/commit metadata and SourceLink mappings. Missing approval is an intentional blocker, not a validation check to bypass.

`python tools/check-reproducible.py` builds the library twice from separate temporary paths and compares DLL/PDB hashes. It checks one build environment; cross-SDK/OS byte identity is not promised.

`python tools/release-properties.py` fails unless eng/release.json contains maintainer-approved decisions and the SDK has its own Git root and matching remote. It never initializes Git or adopts an ancestor repository. It writes an early-imported MSBuild properties file under artifacts. SourceLink is provided by the .NET 8 SDK's built-in GitHub provider; it is disabled for unapproved/local builds. GitHub release jobs opt in only after validating the dedicated repository.

`tools/check-publication-gate.py` inspects required reviewers/prevent-self-review on the nuget-alpha GitHub environment before any publication secret is available. It has not been run against or configured in a GitHub repository during this task.
