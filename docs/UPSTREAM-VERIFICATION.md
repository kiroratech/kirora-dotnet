# Official upstream verification — 2026-11-02

The authoritative source is https://github.com/kiroratech/openapi, an automatically published OpenAPI 3.1 repository. This review fetched its current default-branch commit and compared parsed JSON, bytes, operations, transitive schema references, and license text. No live API requests were made.

## Provenance and result

- Retained SDK pin: `d4e38cb942645976d01e3e109ed586c8d8783feb`, recorded in `spec/source.json`.
- Reviewed official upstream: `30043317efd68ffceeab882019431e57b7463018`.
- Result: different at contract level; neither byte-identical nor semantically equivalent.
- Pinned SHA-256: `4b31b357845dc09f89018879db8f238bb742df6a4c5f023b3210dded9919c8fa`.
- Upstream SHA-256: `b6b5b3a071f06b91b9d6ca271e10f4baca1e3dcb32d75303d83b67b1b7547653`.
- Both still declare API version `1.0.0` and OpenAPI `3.1.0`; version-string monitoring alone misses these changes.
- 183 pinned operations versus 184 upstream; 182 method/path pairs remain common.
- 309 versus 319 component schemas: 10 added, none removed, 51 modified (including documentation changes).

See the [endpoint and schema report](upstream-2026-11-02/CHANGE-REPORT.md) and [exact before/after JSON diff](upstream-2026-11-02/comparison.json). The comparison conservatively filters annotations; it is not a general JSON Schema equivalence solver. Transitive impact indicates review scope, not proof that every referenced operation breaks.

## Breaking changes and SDK impact

- `GET /v1/brand-theme` was removed; `GET /v1/brand-kit` and `GET /v1/journeys/{id}/activation-check` were added. Removing the existing BrandTheme resource during regeneration would break SDK callers. Whether the deployed service preserves an alias is unverified.
- `AssetUsage.brandTheme` and `TemplateVersion.brandTheme` were replaced by required `brandKit` fields. The current strict response contract expects the old fields; newly conforming responses can fail deserialization/validation. Regeneration would remove existing public properties.
- `SocialBlock` and `SocialBlockInput` no longer require `links`. The old required response contract can reject newly valid responses; regeneration changes required/optional public model members.
- `SendTransactionalInput.deliver` changes from a string enum to a union permitting structured delivery/fallback input. Existing strings remain accepted by the new schema, but the current public string property cannot express the new object form. Regeneration changes its public type.
- `Overview.deliverabilityHealth` changes from an inline model to a shared schema reference. Regeneration can change the public CLR property type even where wire representations overlap.
- Other additions include event retention timestamps, skipped transactional channels, fallback linkage, campaign deliverability health/simulation counts, form notification users, topic counts and segmentation conditions, journey activation issues, list usage references, responsive email styles, and additional condition operators. New response fields alone are generally additive; new union alternatives and formerly required fields becoming optional need reader compatibility review.
- The `custom_events` plan-feature annotation was removed from three event operations. Authentication/security and non-schema component changes are recorded in the exact report; no deployment behavior is inferred from annotations.

## Recommendation and attribution

Retain the existing pin for this reviewed alpha. Upgrade to the reviewed immutable commit only in an explicitly approved migration: preserve existing SDK members where possible, decide compatibility aliases and model adapters, regenerate in isolation, review API signatures, and add fixtures for every changed contract. The current 183/183 claim applies only to the retained pin; common routes alone do not establish compatibility with current upstream schemas.

The upstream LICENSE and `spec/LICENSE` are byte-identical MIT text, copyright 2026 Kirora. `THIRD-PARTY-NOTICES.txt` now includes the complete text and provenance and is included in NuGet packages, fixing its previous omission from distributed artifacts. This upstream license does not determine the SDK's own license; that remains a maintainer decision. The pinned specification, source metadata and public SDK API were not changed.

## Proposed drift monitoring

`.github/workflows/upstream-spec.yml` is a prepared, read-only weekly/manual workflow. It resolves the official default branch to an immutable commit, checks both specification and license, uploads a report, and fails on drift or fetch errors. It cannot update pins, generate SDK changes, open pull requests, or publish packages. Maintainers review its artifact and authorize upgrades separately. Nothing has been uploaded or activated on GitHub.

Reproduce offline with `python3 tools/compare-upstream.py --upstream /path/to/openapi.json --upstream-license /path/to/LICENSE --commit <immutable-commit> --output artifacts/upstream-drift`; use `--fetch --fail-on-drift` for monitoring. Exit 2 signals detected drift; fetch/parse/pin errors fail separately. All comparison outputs are review artifacts.
