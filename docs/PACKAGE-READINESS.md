# Package readiness audit

The local package remains **Kirora 0.1.0-alpha.1**, targeting **net8.0**. Existing authors metadata is Kirora. Description, discoverability tags and README are included. Only the SDK is packable; test, console, web, worker and AOT projects are explicitly excluded from solution packaging.

Portable symbols are emitted as Kirora.0.1.0-alpha.1.snupkg alongside the nupkg. tools/validate-package.py checks the DLL, XML docs, README, metadata, absence of test/source payloads and symbol PDB. It validates approved icon content if one is later supplied. No SDK license or icon was invented.

## Decisions still required

NuGet ID ownership/reservation, final author identity, SDK license, canonical repository and optional branding need maintainer confirmation. eng/release.json records these as unapproved/null rather than silently assigning terms. The strict --release package check and publication preflight intentionally fail until these are settled.

The SDK folder currently inherits an unrelated parent Git checkout. Automatic source-control queries and SourceLink are disabled for ordinary/local builds, so the ancestor URL/commit is not stamped into a public artifact. After a dedicated root and matching approved origin exist, release-properties.py emits opt-in MSBuild properties. .NET 8's built-in GitHub SourceLink provider then supplies commit mappings, and the strict package validator checks the approved URL/commit and generated mapping. This configuration is prepared but has not been validated against an approved SDK remote.

## Reproducibility and dependencies

Deterministic compilation and stable path mapping are enabled. Library DLL and portable PDB were byte-identical when built from two isolated temporary paths under the same SDK/environment. This is not a claim of bit-identical nupkg ZIPs or cross-SDK builds. Dependency lock files are included for repeatable resolution; runtime-specific AOT restore locks live under obj so they do not overwrite the portable solution graph.

The initial NuGet scan found high-severity advisories for **System.Net.Http 4.3.0** (GHSA-7jgj-8wvc-jh57) and **System.Text.RegularExpressions 4.3.0** (GHSA-cmhx-cq75-c4mj) in the older xUnit test-only graph. The shipped SDK dependency graph had no reported vulnerability. Updating xunit to 2.9.3, xunit.runner.visualstudio to 2.8.2 and Microsoft.NET.Test.Sdk to 17.11.1 removed those obsolete transitive packages. The follow-up solution audit reports no vulnerable direct/transitive NuGet dependencies. This reports advisory matches in resolved packages, not exploitability of the net8.0 runtime, and does not audit the installed SDK, operating system or bundled native runtime.

xUnit v2 is in security-maintenance mode; a future v3 migration can be reviewed independently. The SDK's Microsoft.Extensions.Http 8.0.1 dependency and .NET 8 target are preserved. API signature tests protect both Phase 2 and Phase 3 baselines; nullable metadata and behavior still require human review.

The public alpha checklist is in RELEASE-CHECKLIST.md. Local technical validation does not itself authorize publication.
