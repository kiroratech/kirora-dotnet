#!/usr/bin/env python3
"""Validate approved release metadata and write MSBuild properties without inventing license/ownership."""
import json,re,subprocess,sys,xml.etree.ElementTree as E
from pathlib import Path
root=Path(__file__).resolve().parents[1];config=json.loads((root/'eng/release.json').read_text())
if not config.get('maintainerApproved'):raise SystemExit('Maintainer approval is required in eng/release.json')
url=config.get('repositoryUrl') or ''
if not re.fullmatch(r'https://github\.com/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+',url):raise SystemExit('Confirm the dedicated SDK GitHub repository URL')
if not config.get('licenseExpression') or not re.fullmatch(r'[A-Za-z0-9.()+ -]+',config['licenseExpression']):raise SystemExit('Confirm an SPDX license expression')
if not re.fullmatch(r'\d+\.\d+\.\d+-alpha\.\d+',config['version']):raise SystemExit('An alpha version must be explicitly approved')
if config['packageId']!='Kirora' or not config.get('authors'):raise SystemExit('Confirm package ID/author decisions')
actual=subprocess.check_output(['git','rev-parse','--show-toplevel'],cwd=root,text=True).strip()
if Path(actual).resolve()!=root.resolve():raise SystemExit('The SDK must have its own dedicated Git root; do not use an unrelated ancestor repository')
remote=subprocess.check_output(['git','remote','get-url','origin'],cwd=root,text=True).strip().removesuffix('.git')
if remote!=url and remote!='git@github.com:'+url.removeprefix('https://github.com/'):
 raise SystemExit('Approved SDK URL does not match origin')
project=E.Element('Project');props=E.SubElement(project,'PropertyGroup')
values={'KiroraRepositoryUrl':url,'KiroraPackageLicenseExpression':config['licenseExpression'],'KiroraEnableSourceLink':'true','KiroraReleaseVersion':config['version'],'ContinuousIntegrationBuild':'true'}
icon=config.get('packageIcon')
if icon:
 path=(root/icon).resolve()
 if not path.is_relative_to(root) or not path.is_file():raise SystemExit('Approved icon must exist inside the SDK repository')
 values['KiroraPackageIcon']=icon
for key,value in values.items():E.SubElement(props,key).text=value
output=root/'artifacts/approved-release.props';output.parent.mkdir(exist_ok=True,parents=True);E.indent(project);E.ElementTree(project).write(output,encoding='unicode')
print(output)
