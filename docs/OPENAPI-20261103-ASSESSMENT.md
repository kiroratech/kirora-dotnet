# OpenAPI compatibility assessment — 3 November 2026

Assessment completed before specification replacement or code generation.

- SDK main reviewed: `630160cba3f1403ea5a34ba7e76f9f02f3caa83c` (fresh origin/main).
- Previous specification: `30043317efd68ffceeab882019431e57b7463018`.
- Target official specification: `80060d50bfa80f3c82b89a363a229c5b1c39506a`.
- Both declare API 1.0.0 and OpenAPI 3.1.0; version strings alone do not identify compatibility.
- Routes: 184 before and after; no added, removed or method/path-changed operations. 58 operations are affected through structural schema references; one further operation has documentation/example changes only. Direct operation-level structural changes: none.
- Schemas: 319 → 334; 17 added, two removed, 41 modified (37 structural, four annotations only).
- Production origin, Bearer authentication, security schemes, scopes, pagination parameters, retry/idempotency annotations and non-schema components have no structural changes. Upstream MIT license is byte-identical. No deployment/live contract was tested.

## Breaking contract changes

1. Transactional sends replace `deliver` with `fallbackOrder` (email/push string array, at least one). The two delivery union schemas disappear. Omit fallbackOrder to use the documented default rather than sending obsolete deliver values. Existing `SendTransactionalInput.Deliver` and delivery types require migration; an alias that emits deliver would misrepresent the contract.
2. Templates, versions, snapshots, creation, content rendering and test-send input become design/HTML discriminated unions. Design generally permits omitted kind for existing block payloads; HTML requires kind=html and html. Responses now require handling both variants. Template list items require kind. Existing design object initializers and direct property access cannot represent both variants unchanged.
3. RenderTemplateInput.version becomes a positive integer only; old draft/published strings become draft=true or omission, respectively. New context enum marketing/transactional governs HTML rendering. String variants cannot be silently retained on the wire.
4. Segment contact/attribute conditions split scalar string `value` from nonempty string-array `values` (1–100, strings max 255). Numeric/boolean/array value variants are removed. Event filters, journey triggers/steps/exits, conversion goals and funnels also narrow scalar filter values to strings. Operators are not invented or automatically coerced.
5. IdentifyContactInput.topics entries become TopicChoiceInput objects with optional subscribed boolean and channels map. SetContactTopicInput.subscribed is now an optional boolean; channels is separate. Old boolean/channel-map union payloads must change.
6. Forms hidden values become explicit source=fixed/value or source=link/param variants; the old bare string/fromParam forms no longer apply. SubmitFormInput.fields becomes string-only and multi-valued choices moves to choices arrays (max 50 items, strings max 100). Form input width becomes fit/full/fixed plus widthPx (200–720); old numeric width must migrate.
7. UpdateTemplateInput adds html; new variants introduce kind discriminators and additional required members. All changed nullability, required sets, enums, limits and nested schemas are preserved in the exact before/after report.

## Migration policy

Retain resource method names/signatures wherever the operation contract permits. Regenerate typed models from this exact target. Preserve unchanged public members and existing non-affected APIs; do not add legacy fields or coercions that send invalid JSON. Document unavoidable model/union/source changes and required-member changes explicitly. Keep prior public API snapshots unchanged and enforce exact migration deltas plus a new baseline. Any compatibility adapter must translate only a proven equivalent contract; no blanket suppression or aliases to obsolete payloads.

[Endpoint/schema report](upstream-2026-11-03/CHANGE-REPORT.md) and [machine-readable exact changes](upstream-2026-11-03/comparison.json) enumerate every affected operation and schema. This structural assessment is conservative and does not establish deployed staging parity.
