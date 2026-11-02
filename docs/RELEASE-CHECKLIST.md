# Public alpha release checklist

No publication or GitHub release is authorized by completing this checklist. The supplied publication workflow is manual, approval-gated and currently fails closed because maintainer decisions are unresolved.

## Maintainer decisions (blocking publication)

- [ ] Confirm ownership/permission to publish NuGet package ID Kirora. Its presence in a csproj is not proof of ownership or name availability.
- [ ] Approve version 0.1.0-alpha.1 and authors. These are existing draft metadata, not an assigned package reservation.
- [ ] Choose and approve the SDK license. The MIT license under spec/LICENSE covers the upstream specification; it is not automatically the SDK's license. No SDK licensing terms were invented.
- [ ] Establish the SDK's own Git repository and confirm its canonical GitHub URL. The current directory inherits an unrelated ancestor repository. Do not use or modify that ancestor for release provenance.
- [ ] Confirm an optional approved PNG/JPEG package icon (at most 1 MiB; NuGet recommends a square icon). No logo/brand asset was fabricated. A package icon is recommended, not a prerequisite for the local package.
- [ ] Record approved metadata in eng/release.json, update any corresponding project metadata, and set maintainerApproved only after review.

## Local validation

- [ ] Restore (including --locked-mode), Release build and all offline tests pass with KIRORA_RUN_INTEGRATION=0.
- [ ] All five generation/coverage checks pass; the pinned OpenAPI hash and source record remain unchanged.
- [ ] Both Phase 2 and Phase 3 public API baselines pass. Review intentional semantic fixes and nullable annotations separately from signature availability.
- [ ] NuGet dependency audit reports no vulnerable direct/transitive packages and no incomplete-feed warning. This is not an OS/runtime security scan.
- [ ] dotnet pack --configuration Release produces only Kirora's nupkg and snupkg; validate both with tools/validate-package.py.
- [ ] Confirm README, net8.0 DLL, public XML docs, tags, description, authors, version and portable PDB contents.
- [ ] Compare deterministic library DLL/PDB output with tools/check-reproducible.py. Cross-SDK/OS/package-ZIP identity is not promised.
- [ ] Validate the candidate with the Native AOT smoke. Local verification covers macOS x64/.NET 8 and exercised paths only; run the prepared Linux CI smoke before claiming Linux support. Use a currently serviced SDK/runtime for any distributed native executable.

## Integration authorization

- [ ] Review docs/INTEGRATION-TESTS.md and isolate a Kirora test workspace with no production journeys or recipients.
- [ ] Explicitly authorize the four live workflows; securely configure environment credentials, expected workspace UUID, and a verified sender for the fixed example.com email recipient.
- [ ] If authorized, run the workflows and record actual results, including cleanup failures. Skipped workflows are not successful live verification.
- [ ] If live verification is deferred for an alpha, explicitly record that limitation and resolve website/spec discrepancies before claiming deployed parity.

## GitHub and publication setup

- [ ] Push reviewed source into the dedicated, approved SDK repository. No workflow was uploaded or executed on GitHub in this implementation session.
- [ ] Review immutable action pins in eng/action-pins.json and .github/workflows; CI has read-only permissions and no Kirora/NuGet secrets.
- [ ] Configure the nuget-alpha environment with required reviewers and prevent-self-review, plus appropriate branch restrictions. The preflight inspects these protections before the publication job can access secrets.
- [ ] Add a scoped NUGET_API_KEY only to that protected environment; confirm NuGet ownership separately. Prefer short-lived/trusted-publishing credentials when the maintainer configures the account integration.
- [ ] Run CI on the approved commit. The publish workflow reruns validation and checks approved metadata, dedicated Git root, origin URL, repository commit and SourceLink mapping.
- [ ] Inspect portable PDB/SourceLink mappings from the real repository. Local SourceLink is intentionally disabled, so it has not been verified against an approved remote.
- [ ] Review the exact package/symbol artifacts and release notes, then separately authorize manual publication. The workflow requires PUBLISH KIRORA ALPHA on the default branch and environment approval.

Neither workflow creates a GitHub release. No workflow automatically publishes on push, tag creation, pull requests or completion of CI.
