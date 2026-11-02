#!/usr/bin/env python3
"""Fail on vulnerable packages or incomplete NuGet audit results (CLI exit zero alone is insufficient)."""
import argparse,json,subprocess,sys
p=argparse.ArgumentParser();p.add_argument('--input');p.add_argument('--output',default='artifacts/dependency-audit.json');a=p.parse_args()
if a.input:
 data=json.load(open(a.input))
else:
 result=subprocess.run(['dotnet','list','Kirora.sln','package','--vulnerable','--include-transitive','--format','json'],capture_output=True,text=True)
 if result.returncode:sys.stderr.write(result.stderr);raise SystemExit(result.returncode)
 try:data=json.loads(result.stdout)
 except ValueError:raise SystemExit('NuGet audit did not return valid JSON')
 from pathlib import Path
 path=Path(a.output);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,indent=2)+'\n')
failures=[]
for log in data.get('logs',[]):
 if str(log.get('level','')).lower() in {'error','warning'}:failures.append('NuGet audit warning/error: '+log.get('message',''))
projects=data.get('projects',[])
if not projects:failures.append('No projects were audited')
for project in projects:
 for framework in project.get('frameworks',[]):
  for key in ['topLevelPackages','transitivePackages']:
   for package in framework.get(key,[]):
    if package.get('vulnerabilities'):failures.append(package['id']+' '+package.get('resolvedVersion','')+' has reported vulnerabilities')
for failure in failures:print(failure,file=sys.stderr)
if failures:raise SystemExit(1)
print(f'NuGet vulnerability audit passed for {len(projects)} projects; no reported vulnerable direct/transitive packages.')
