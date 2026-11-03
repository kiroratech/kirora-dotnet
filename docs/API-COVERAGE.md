# API coverage against pinned Kirora API 1.0.0

**184/184 unique operations (100%) have typed resource methods across 25 spec tags.** This is implementation coverage, not evidence of a live verified API contract.

The original contract implemented 183 operations. The 30043317 migration yielded 184; the 80060d50 migration changes contracts while retaining all 184 routes. GET /v1/me remains available as KiroraClient.GetMeAsync and also Me.GetAsync; it is counted once.

The complete operation-level coverage matrix is [api-coverage.csv](api-coverage.csv). It records before/after status, method/path, C# method, scopes, allowed key kinds, retry policy and Node/Go dedicated-method parity.

## Module coverage

- contacts: 20/20 implemented; 0 missing.
- events: 9/9 implemented; 0 missing.
- lists: 8/8 implemented; 0 missing.
- segments: 10/10 implemented; 0 missing.
- topics: 5/5 implemented; 0 missing.
- attributes: 3/3 implemented; 0 missing.
- tags: 5/5 implemented; 0 missing.
- transactional: 3/3 implemented; 0 missing.
- campaigns: 20/20 implemented; 0 missing.
- journeys: 13/13 implemented; 0 missing.
- conversion-goals: 5/5 implemented; 0 missing.
- templates: 15/15 implemented; 0 missing.
- shared-blocks: 6/6 implemented; 0 missing.
- assets: 6/6 implemented; 0 missing.
- forms: 12/12 implemented; 0 missing.
- messages: 7/7 implemented; 0 missing.
- suppressions: 3/3 implemented; 0 missing.
- sending-domains: 6/6 implemented; 0 missing.
- senders: 1/1 implemented; 0 missing.
- push-projects: 1/1 implemented; 0 missing.
- webhooks: 11/11 implemented; 0 missing.
- reports: 5/5 implemented; 0 missing.
- me: 1/1 implemented; 0 missing.
- workspaces: 8/8 implemented; 0 missing.
- embed-sessions: 1/1 implemented; 0 missing.

## Scope and comparisons

- The [official API reference](https://kirora.io/docs/api/reference) describes the API; this coverage audit uses the immutable specification, containing 184 operations in 25 tags. This SDK follows the pinned specification; there are no invented Groups aliases or undocumented transactional detail paths.
- [Node SDK](https://github.com/kiroratech/kirora-node/tree/d67052f245ce9e75225dc9784fb3da6b06a51819) and [Go SDK](https://github.com/kiroratech/kirora-go/tree/1ddaa306374060360d146a60aedda175118e4141) select the same 49 dedicated typed operations. Their generic request helpers can access other endpoints, which is distinct from dedicated typed resource coverage. These inspected SDK revisions pin the previous specification, so their parity columns are historical.
- Events exposes ingestion, event history, catalog, standard names, summary and time series. Reports exposes overview, transactional delivery, campaign comparison, funnel and RFM summary; ConversionGoals exposes conversion metrics. No extra Analytics URL prefix has been invented.
- Message CSV exports stream to caller-owned destinations; partial stream failures are not automatically retried. Assets.UploadAsync uses the documented multipart file/name fields, MIME types, maximum byte size and stable per-call idempotency.
- All paginated JSON operations expose lazy typed iterators; nonpaginated arrays and analytics series retain their documented response shape.
- Request/response types, operation policies, scopes and key restrictions come from the checksum-pinned spec/openapi.json. Generated consistency checks run offline and verify its SHA-256.

## Gaps beyond endpoint coverage

No pinned-spec endpoints are missing. Remaining gaps are live contract verification, webhook signature/user-token convenience helpers, long-term public API/release policy, license and package ownership approval, SourceLink/repository provenance and broader Native AOT platform coverage (see PHASE4.md for the verified local smoke). The website/spec discrepancies need Kirora confirmation before claiming deployed parity.

## Reproduce

Run python3 tools/generate-coverage.py --check along with the three model/fixture generators and endpoint invoker generation check.
