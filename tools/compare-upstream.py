#!/usr/bin/env python3
"""Read-only official OpenAPI drift report. Never changes pins, generated SDK files or the public API."""
import argparse,hashlib,json,os,sys,urllib.request,urllib.parse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ANNOTATIONS={'description','summary','title','example','examples','externalDocs'}
MAPS={'properties','schemas','parameters','responses','headers','content','paths','webhooks','patternProperties','mapping','encoding','securitySchemes'}

def structural(value, parent=''):
    if isinstance(value,dict):
        return {key:structural(item,key) for key,item in value.items() if parent in MAPS or key not in ANNOTATIONS}
    if isinstance(value,list):return [structural(item) for item in value]
    return value

def changes(before,after,pointer=''):
    if type(before) is not type(after):return [{'pointer':pointer,'before':before,'after':after}]
    if isinstance(before,dict):
        result=[]
        for key in sorted(before.keys()|after.keys()):
            path=pointer+'/'+key.replace('~','~0').replace('/','~1')
            if key not in before:result.append({'pointer':path,'kind':'added','after':after[key]})
            elif key not in after:result.append({'pointer':path,'kind':'removed','before':before[key]})
            else:result.extend(changes(before[key],after[key],path))
        return result
    if isinstance(before,list) and len(before)==len(after):
        return [change for index,(a,b) in enumerate(zip(before,after)) for change in changes(a,b,pointer+'/'+str(index))]
    return [] if before==after else [{'pointer':pointer,'before':before,'after':after}]

def operations(spec):
    return {op['operationId']:{'method':method.upper(),'path':path,'operation':op} for path,item in spec['paths'].items() for method,op in item.items() if isinstance(op,dict) and 'operationId' in op}

def refs(node):
    if isinstance(node,dict):
        if node.get('$ref','').startswith('#/components/schemas/'):yield node['$ref'].split('/')[-1]
        for value in node.values():yield from refs(value)
    elif isinstance(node,list):
        for value in node:yield from refs(value)

def reachable(operation,spec):
    found=set();pending=list(refs(operation))
    while pending:
        name=pending.pop()
        if name in found:continue
        found.add(name);pending.extend(refs(spec['components']['schemas'].get(name,{})))
    return found

def fetch(url):
    headers={'User-Agent':'Kirora-dotnet-upstream-drift-check','Accept':'application/vnd.github+json'}
    token=os.environ.get('GITHUB_TOKEN')
    if token and urllib.parse.urlparse(url).hostname=='api.github.com':headers['Authorization']='Bearer '+token
    with urllib.request.urlopen(urllib.request.Request(url,headers=headers),timeout=60) as response:
        data=response.read(16*1024*1024+1)
    if len(data)>16*1024*1024:raise ValueError('Upstream input exceeds the drift checker size limit')
    return data

def compare(local_bytes,remote_bytes,local_license,remote_license,commit):
    old=json.loads(local_bytes);new=json.loads(remote_bytes);a=operations(old);b=operations(new)
    old_schemas=old['components']['schemas'];new_schemas=new['components']['schemas']
    schema_changes=[]
    for name in sorted(old_schemas.keys()&new_schemas.keys()):
        full=changes(old_schemas[name],new_schemas[name])
        if full:schema_changes.append({'name':name,'structural':changes(structural(old_schemas[name]),structural(new_schemas[name])),'allChanges':full})
    changed_schema_names={x['name'] for x in schema_changes if x['structural']}|set(old_schemas.keys()^new_schemas.keys())
    endpoint_changes=[]
    for oid in sorted(a.keys()&b.keys()):
        direct=changes(a[oid],b[oid]);contract=changes(structural(a[oid]),structural(b[oid]));affected=sorted((reachable(a[oid],old)|reachable(b[oid],new))&changed_schema_names)
        if direct or affected:endpoint_changes.append({'operationId':oid,'method':b[oid]['method'],'path':b[oid]['path'],'directStructuralChanges':contract,'referencedChangedSchemas':affected,'documentationOrExampleOnly':bool(direct) and not contract and not affected,'allDirectChanges':direct})
    local_hash=hashlib.sha256(local_bytes).hexdigest();remote_hash=hashlib.sha256(remote_bytes).hexdigest()
    routes_old={(x['method'],x['path']) for x in a.values()};routes_new={(x['method'],x['path']) for x in b.values()}
    return {'officialRepository':'https://github.com/kiroratech/openapi','upstreamCommit':commit,'upstreamUrl':f'https://raw.githubusercontent.com/kiroratech/openapi/{commit}/openapi.json','classification':'byte-identical' if local_bytes==remote_bytes else 'JSON-equivalent (formatting/key order only)' if old==new else 'different','localSha256':local_hash,'upstreamSha256':remote_hash,'localApiVersion':old['info']['version'],'upstreamApiVersion':new['info']['version'],'localOpenapiVersion':old['openapi'],'upstreamOpenapiVersion':new['openapi'],'localOperationCount':len(a),'upstreamOperationCount':len(b),'sharedMethodPathCount':len(routes_old&routes_new),'operationsAdded':[{**b[key],'operationId':key} for key in sorted(b.keys()-a.keys())],'operationsRemoved':[{**a[key],'operationId':key} for key in sorted(a.keys()-b.keys())],'endpointsChanged':endpoint_changes,'localSchemaCount':len(old_schemas),'upstreamSchemaCount':len(new_schemas),'schemasAdded':{key:new_schemas[key] for key in sorted(new_schemas.keys()-old_schemas.keys())},'schemasRemoved':{key:old_schemas[key] for key in sorted(old_schemas.keys()-new_schemas.keys())},'schemasChanged':schema_changes,'otherStructuralChanges':changes(structural({k:v for k,v in old.items() if k not in {'paths','components'}}),structural({k:v for k,v in new.items() if k not in {'paths','components'}})),'otherComponentStructuralChanges':changes(structural({k:v for k,v in old['components'].items() if k!='schemas'}),structural({k:v for k,v in new['components'].items() if k!='schemas'})),'licenseByteIdentical':local_license==remote_license,'localLicenseSha256':hashlib.sha256(local_license).hexdigest(),'upstreamLicenseSha256':hashlib.sha256(remote_license).hexdigest()}

def render(report):
    r=report;structural_schemas=[x for x in r['schemasChanged'] if x['structural']];docs_schemas=[x for x in r['schemasChanged'] if not x['structural']]
    lines=['# Official upstream OpenAPI comparison','','Source: '+r['officialRepository'], 'Upstream immutable commit: `'+r['upstreamCommit']+'`.','',f'Classification: **{r["classification"]}**. Operation counts: {r["localOperationCount"]} pinned vs {r["upstreamOperationCount"]} upstream. Shared method/path pairs: {r["sharedMethodPathCount"]}.',f'API version: `{r["localApiVersion"]}` → `{r["upstreamApiVersion"]}`; OpenAPI: `{r["localOpenapiVersion"]}` → `{r["upstreamOpenapiVersion"]}`.','',f'Pinned SHA-256: `{r["localSha256"]}`',f'Upstream SHA-256: `{r["upstreamSha256"]}`','',f'License byte-identical: {r["licenseByteIdentical"]}.','','## Endpoint-level changes','']
    for item in r['operationsAdded']:lines.append(f'- Added `{item["method"]} {item["path"]}` (`{item["operationId"]}`).')
    for item in r['operationsRemoved']:lines.append(f'- Removed `{item["method"]} {item["path"]}` (`{item["operationId"]}`).')
    lines+=['','Shared operations below include direct changes and transitive referenced-schema changes. Structural filtering excludes descriptions/titles/examples, but preserves schema properties named description/summary. It is a conservative structural comparison, not a general JSON Schema equivalence or automatic breaking-change proof.','']
    for item in r['endpointsChanged']:
        status='prose/examples only' if item['documentationOrExampleOnly'] else 'direct structural and/or referenced schema changes'
        lines.append(f'- `{item["method"]} {item["path"]}` (`{item["operationId"]}`): {status}.')
        if item['referencedChangedSchemas']:lines.append('  Referenced schemas: '+', '.join('`'+x+'`' for x in item['referencedChangedSchemas'])+'.')
        for c in item['directStructuralChanges']:lines.append('  Direct contract pointer: `'+c['pointer']+'`.')
    lines+=['','## Schema-level changes','',f'{r["localSchemaCount"]} → {r["upstreamSchemaCount"]} components; {len(r["schemasAdded"])} added, {len(r["schemasRemoved"])} removed, {len(structural_schemas)} structurally modified, {len(docs_schemas)} modified only in prose/examples. Exact before/after values are in comparison.json.','']
    for name in r['schemasAdded']:lines.append('- Added `'+name+'`.')
    for name in r['schemasRemoved']:lines.append('- Removed `'+name+'`.')
    for item in structural_schemas:
        lines+=['','### '+item['name'],'']
        for c in item['structural']:lines.append('- `'+c['pointer']+'` ('+c.get('kind','changed')+').')
    lines+=['','## Other structural changes','','See otherStructuralChanges and otherComponentStructuralChanges in comparison.json. Documentation changes can still carry behavioral guidance and should be reviewed; this tool does not prove deployed server behavior.']
    return '\n'.join(lines)+'\n'

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--upstream');p.add_argument('--upstream-license');p.add_argument('--commit');p.add_argument('--fetch',action='store_true');p.add_argument('--output',default='artifacts/upstream-drift');p.add_argument('--fail-on-drift',action='store_true');a=p.parse_args()
    if a.fetch:
        repository=json.loads(fetch('https://api.github.com/repos/kiroratech/openapi'));branch=repository['default_branch']
        metadata=json.loads(fetch('https://api.github.com/repos/kiroratech/openapi/commits/'+urllib.parse.quote(branch,safe='')));commit=metadata['sha']
        remote=fetch(f'https://raw.githubusercontent.com/kiroratech/openapi/{commit}/openapi.json');license_bytes=fetch(f'https://raw.githubusercontent.com/kiroratech/openapi/{commit}/LICENSE')
    else:
        if not all([a.upstream,a.upstream_license,a.commit]):p.error('Supply --fetch or --upstream, --upstream-license and --commit')
        remote=Path(a.upstream).read_bytes();license_bytes=Path(a.upstream_license).read_bytes();commit=a.commit
    report=compare((ROOT/'spec/openapi.json').read_bytes(),remote,(ROOT/'spec/LICENSE').read_bytes(),license_bytes,commit)
    pin=json.loads((ROOT/'spec/source.json').read_text())
    if report['localSha256']!=pin['sha256']:raise ValueError('The local specification no longer matches its pin')
    report['pinnedUrl']=pin['url'];out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
    (out/'comparison.json').write_text(json.dumps(report,indent=2)+'\n');(out/'CHANGE-REPORT.md').write_text(render(report))
    print(f"{report['classification']}: {report['localOperationCount']} → {report['upstreamOperationCount']} operations; {len(report['schemasAdded'])} new schemas; license identical={report['licenseByteIdentical']}")
    if a.fail_on_drift and (report['classification']!='byte-identical' or not report['licenseByteIdentical']):return 2
    return 0
if __name__=='__main__':
    try:sys.exit(main())
    except (OSError,ValueError,KeyError) as error:sys.exit('Comparison failed (not an identical result): '+str(error))
