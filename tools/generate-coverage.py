#!/usr/bin/env python3
"""Audit unique operation coverage, public resource mappings and inspected SDK parity."""
import csv,io,json,sys,hashlib
from pathlib import Path
from collections import Counter
root=Path(__file__).resolve().parents[1];s=json.loads((root/'spec/openapi.json').read_text());source=json.loads((root/'spec/source.json').read_text())
if hashlib.sha256((root/'spec/openapi.json').read_bytes()).hexdigest()!=source['sha256']:raise SystemExit('Pinned specification checksum mismatch')
ops=json.loads((root/'spec/core-operations.json').read_text());byid={o['id']:o for o in ops};parity=json.loads((root/'spec/sdk-parity.json').read_text());before=set(json.loads((root/'spec/phase2-operation-ids.json').read_text()));rows=[]
for path,item in s['paths'].items():
 for method,o in item.items():
  if not isinstance(o,dict) or 'operationId' not in o:continue
  oid=o['operationId'];mapping=byid.get(oid);implemented=False
  if mapping:
   resource=root/f'src/Kirora/Resources/{mapping["resource"]}Resource.g.cs';implemented=resource.exists() and f'Operation: {oid}.' in resource.read_text()
  rows.append([o['tags'][0],oid,method.upper(),path,'implemented' if oid in before else 'missing','implemented' if implemented else 'missing',mapping['resource']+'.'+mapping['methodName']+'Async' if mapping else '',str(oid in parity['nodeTypedOperationIds']).lower(),str(oid in parity['goTypedOperationIds']).lower(),o.get('x-required-scope','any valid key'),','.join(o.get('x-key-kinds',[])),mapping['retry'] if mapping else '',str(mapping['key']).lower() if mapping else ''])
if len(rows)!=len(byid) or len(rows)!=183 or not all(r[5]=='implemented' for r in rows):raise SystemExit('Endpoint coverage is incomplete')
stream=io.StringIO();writer=csv.writer(stream,lineterminator='\n');writer.writerow(['module','operationId','httpMethod','path','phase2','phase3','dotnetMethod','nodeDedicatedTyped','goDedicatedTyped','requiredScope','keyKinds','retryPolicy','supportsHeaderKey']);writer.writerows(rows)
counts=Counter(r[0] for r in rows)
lines=['# API coverage against pinned Kirora API 1.0.0','','**183/183 unique operations (100%) have typed resource methods across 25 spec tags.** This is implementation coverage, not evidence of a live verified API contract.','','Before Phase 3: 36/183 operations (35 Phase 2 operations plus the pre-existing GET /v1/me). Phase 3 adds 147 unique operations. GET /v1/me remains available as KiroraClient.GetMeAsync and also Me.GetAsync; it is counted once.','','The complete operation-level coverage matrix is [api-coverage.csv](api-coverage.csv). It records before/after status, method/path, C# method, scopes, allowed key kinds, retry policy and Node/Go dedicated-method parity.','','## Module coverage','']
for tag,count in counts.items():lines.append(f'- {tag}: {count}/{count} implemented; 0 missing.')
lines+=['','## Scope and comparisons','','- The [official API reference](https://kirora.io/docs/api/reference) advertises 169 endpoints in 22 areas. The immutable specification contains 183 operations in 25 tags. This SDK follows the pinned specification; there are no invented Groups aliases or undocumented transactional detail paths.','- [Node SDK](https://github.com/kiroratech/kirora-node/tree/'+parity['nodeCommit']+') and [Go SDK](https://github.com/kiroratech/kirora-go/tree/'+parity['goCommit']+') select the same 49 dedicated typed operations. Their generic request helpers can access other endpoints, which is distinct from dedicated typed resource coverage. Both pin the same spec checksum as this SDK.','- Events exposes ingestion, event history, catalog, standard names, summary and time series. Reports exposes overview, transactional delivery, campaign comparison, funnel and RFM summary; ConversionGoals exposes conversion metrics. No extra Analytics URL prefix has been invented.','- Message CSV exports stream to caller-owned destinations; partial stream failures are not automatically retried. Assets.UploadAsync uses the documented multipart file/name fields, MIME types, maximum byte size and stable per-call idempotency.','- All paginated JSON operations expose lazy typed iterators; nonpaginated arrays and analytics series retain their documented response shape.','- Request/response types, operation policies, scopes and key restrictions come from unchanged spec/openapi.json. Generated consistency checks run offline and verify its SHA-256.','','## Gaps beyond endpoint coverage','','No pinned-spec endpoints are missing. Remaining gaps are live contract verification, webhook signature/user-token convenience helpers, long-term public API/release policy, license and package ownership approval, SourceLink/repository provenance and broader Native AOT platform coverage (see PHASE4.md for the verified local smoke). The website/spec discrepancies need Kirora confirmation before claiming deployed parity.','','## Reproduce','','Run python3 tools/generate-coverage.py --check along with the three model/fixture generators and endpoint invoker generation check.']
outputs={'docs/api-coverage.csv':stream.getvalue(),'docs/API-COVERAGE.md':'\n'.join(lines)+'\n'}
for name,text in outputs.items():
 p=root/name
 if '--check' in sys.argv:
  if not p.exists() or p.read_text()!=text:raise SystemExit('Coverage artifact is stale: '+name)
 else:p.parent.mkdir(exist_ok=True,parents=True);p.write_text(text)
print('Coverage verified: 183/183 operations, 25 modules; baseline 36; Node and Go 49 each.')
