#!/usr/bin/env python3
"""Redacted heuristic audit of every reachable Git blob and current tracked path. Not a complete secret detector."""
import argparse,json,re,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--output',default='artifacts/repository-audit.json');a=p.parse_args()
def git(*args):return subprocess.check_output(['git',*args],cwd=root)
tracked=git('ls-files','-z').decode().strip('\0').split('\0')
objects=git('rev-list','--objects','--all').decode().splitlines();findings=[];reviewed=[];blobs=0;seen=set();historical_artifacts=[]
import hashlib
examples=json.loads((root/'eng/secret-audit-examples.json').read_text())
pinned=json.loads((root/'spec/openapi.json').read_text())
for example in examples:
 if 'upstreamPointer' not in example:continue
 value=pinned
 for segment in example['upstreamPointer'].split('/')[1:]:
  key=segment.replace('~1','/').replace('~0','~');value=value[int(key)] if isinstance(value,list) else value[key]
 if not isinstance(value,str) or hashlib.sha256(value.encode()).hexdigest()!=example['sha256']:raise SystemExit('Reviewed example digest no longer matches the pinned official example')
for entry in objects:
 oid,_,path=entry.partition(' ')
 if oid in seen or git('cat-file','-t',oid).strip()!=b'blob':continue
 seen.add(oid);blobs+=1
 if re.search(r'(^|/)(bin|obj|artifacts|TestResults|__pycache__)(/|$)|(^|/)\.env($|\.)|\.(pfx|p12|pem|key|nupkg|snupkg|dll|pdb)$|(^|/)\.DS_Store$',path,re.I):historical_artifacts.append({'blob':oid,'path':path})
 raw=git('cat-file','blob',oid)
 patterns={
  'private-key':rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----',
  'github-token':rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{60,})\b',
  'aws-access-key':rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',
  'jwt':rb'\beyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{20,}\b',
  'credential-in-url':rb'https?://[^\s/<>:@]{2,}:[^\s/<>@]{6,}@',
  'high-entropy-key-assignment':rb'(?i)(?:api[_-]?key|secret|password|token)\s*["\x27]?\s*[:=]\s*["\x27]([A-Za-z0-9_+/=-]{32,})["\x27]'
 }
 for rule,pattern in patterns.items():
  for match in re.finditer(pattern,raw):
   value=match.group(1) if match.lastindex else match.group()
   # Assignments made only of a repeated placeholder are not high-entropy credentials.
   if rule=='high-entropy-key-assignment' and len(set(value))<10:continue
   finding={'blob':oid,'path':path,'line':raw[:match.start()].count(b'\n')+1,'rule':rule}
   digest=hashlib.sha256(value).hexdigest()
   known=next((e for e in examples if e['sha256']==digest and e['rule']==rule and path in e['paths']),None)
   if known:reviewed.append(dict(finding,sha256=digest,reason=known['reason']))
   else:findings.append(finding)
current_artifacts=[path for path in tracked if re.search(r'(^|/)(bin|obj|artifacts|TestResults|__pycache__)(/|$)|(^|/)\.env($|\.)|\.(pfx|p12|pem|key|nupkg|snupkg|dll|pdb)$|(^|/)\.DS_Store$',path,re.I)]
report={'scope':'all refs and current tracked paths','commits':len(git('rev-list','--all').splitlines()),'trackedFiles':len(tracked),'reachableBlobs':blobs,'candidateSecrets':findings,'reviewedExampleMatches':reviewed,'historicalArtifacts':historical_artifacts,'currentArtifacts':current_artifacts,'limitations':'Heuristic rules do not prove absence of secrets. No raw matches are printed. Historical artifacts are reported, never removed or rewritten automatically.'}
out=root/a.output;out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n')
print(f"Audited {report['commits']} commits, {blobs} reachable blobs, {len(tracked)} tracked files; {len(findings)} unexplained secret candidates, {len(reviewed)} exact documented example matches, {len(current_artifacts)} current artifacts, {len(historical_artifacts)} historical artifacts. Redacted report: {a.output}")
if findings or current_artifacts:raise SystemExit(1)
