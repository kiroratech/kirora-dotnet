#!/usr/bin/env python3
"""Preflight runs without publication secrets and fails closed if GitHub reviewers are not configured."""
import json,os,urllib.request
repository=os.environ.get('GITHUB_REPOSITORY','');token=os.environ.get('GH_TOKEN','')
if not repository or not token:raise SystemExit('GitHub repository/read token is required for environment protection inspection')
url='https://api.github.com/repos/'+repository+'/environments/nuget-alpha'
request=urllib.request.Request(url,headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'})
try:
 with urllib.request.urlopen(request,timeout=30) as response:environment=json.load(response)
except Exception:raise SystemExit('Cannot verify nuget-alpha environment protection; publication is blocked')
reviewers=[rule for rule in environment.get('protection_rules',[]) if rule.get('type')=='required_reviewers' and rule.get('reviewers') and rule.get('prevent_self_review')]
if not reviewers:raise SystemExit('Configure required reviewers and prevent self-review on nuget-alpha before publication')
print('nuget-alpha has required reviewers and prevents self-review. The environment job must still receive approval.')
