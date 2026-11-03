#!/usr/bin/env python3
"""Extract official examples and schema-defined required query values for wire tests."""
import json,sys,re
from pathlib import Path
root=Path(__file__).resolve().parents[1];spec=json.loads((root/'spec/openapi.json').read_text());ops=json.loads((root/'spec/core-operations.json').read_text());fixtures=[]
for op in ops:
 o=spec['paths'][op['path']][op['method'].lower()]
 requestContent=o.get('requestBody',{}).get('content',{})
 req=requestContent.get('application/json',{}).get('examples',{})
 if op['multipart']:
  # A minimal PNG test payload: file/name are exactly the documented multipart fields.
  examples={'image':{'value':{'file':'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jM2kAAAAASUVORK5CYII=','name':'SDK fixture'}}}
 elif req:examples=req
 elif o.get('requestBody',{}).get('required'):raise ValueError('No official request examples for '+op['id'])
 else:examples={'noBody':{'value':None}}
 query={}
 for p in o.get('parameters',[]):
  if p.get('in')!='query' or not p.get('required'):continue
  shape=p['schema'];name=p['name']
  query[name]=p.get('example',shape.get('default',shape.get('enum',[None])[0]))
  if query[name] is None:query[name]='2026-10-01' if shape.get('format')=='date' or name=='day' else 'SDK Example'
 pathValues={p:('1' if p=='version' else 'SDK Example' if p=='name' else 'plan' if p=='key' else 'tenant-1' if p=='externalId' else '0192f3a1-7c2e-7d4b-9a10-3e5f6a7b8c9d') for p in op['paths']}
 success={k:v for k,v in o['responses'].items() if k.startswith('2')}
 # Read operations can document multiple response variants (for example design/HTML templates).
 # Exercise every official success example instead of silently covering only the first one.
 if examples=={'noBody':{'value':None}}:
  status,response=next(iter(success.items()))
  content=response.get('content',{}).get('text/csv' if op['csv'] else 'application/json',{})
  samples=content.get('examples',{})
  if samples:examples={name:{'value':None} for name in samples}
 for example,data in examples.items():
  status,response=next(iter(success.items()));content=response.get('content',{}).get('text/csv' if op['csv'] else 'application/json',{});samples=content.get('examples',{})
  output=samples.get(example,next(iter(samples.values()),{})).get('value')
  if output is None and content.get('schema',{}).get('examples'):output=content['schema']['examples'][0]
  fixtures.append({'operationId':op['id'],'example':example,'method':op['method'],'path':op['path'],'pathValues':pathValues,'query':query,'acceptsKey':op['key'],'multipart':op['multipart'],'csv':op['csv'],'request':data['value'],'status':int(status),'response':output})
text=json.dumps(fixtures,indent=2)+'\n';p=root/'tests/Kirora.Tests/Fixtures/core-examples.json'
if '--check' in sys.argv:
 if not p.exists() or p.read_text()!=text:raise SystemExit('Core contract fixtures are stale')
else:p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text)
print(f'{len(fixtures)} contract fixtures verified for {len(ops)} operations (official examples; schema-based query values; one multipart PNG).')
