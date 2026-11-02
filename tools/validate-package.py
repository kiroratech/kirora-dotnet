#!/usr/bin/env python3
"""Validate local alpha artifacts; --release fails closed on unresolved maintainer metadata."""
import argparse,json,re,sys,xml.etree.ElementTree as E
from pathlib import Path
from zipfile import ZipFile
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('package',nargs='?');p.add_argument('--release',action='store_true');a=p.parse_args();config=json.loads((root/'eng/release.json').read_text());package=Path(a.package) if a.package else root/('src/Kirora/bin/Release/'+config['packageId']+'.'+config['version']+'.nupkg');issues=[]
with ZipFile(package) as z:
 names=z.namelist();nuspec=E.fromstring(z.read(next(n for n in names if n.endswith('.nuspec'))))
 def node(name):return nuspec.find('.//{*}metadata/{*}'+name)
 def text(name):
  n=node(name);return n.text if n is not None else None
 if text('id')!=config['packageId']:issues.append('Package ID does not match release configuration')
 if text('version')!=config['version']:issues.append('Package version does not match release configuration')
 if text('authors')!=','.join(config['authors']):issues.append('Package authors do not match release configuration')
 for field in ['description','tags','readme']:
  if not text(field):issues.append('Missing '+field)
 if 'THIRD-PARTY-NOTICES.txt' not in names:issues.append('Missing upstream specification attribution')
 elif (root/'spec/LICENSE').read_text() not in z.read('THIRD-PARTY-NOTICES.txt').decode():issues.append('Upstream MIT license text is missing')
 if text('readme') not in names:issues.append('README is not included')
 if 'lib/net8.0/Kirora.dll' not in names or 'lib/net8.0/Kirora.xml' not in names:issues.append('Missing net8.0 library/XML documentation')
 if any(n.startswith('lib/') and not n.startswith('lib/net8.0/') for n in names):issues.append('Unexpected target framework')
 if any('Tests' in n or n.endswith('.cs') for n in names):issues.append('Unexpected test or source content')
 icon=text('icon')
 if icon and (icon not in names or not icon.lower().endswith(('.png','.jpg','.jpeg')) or len(z.read(icon))>1024*1024):issues.append('Icon must be included as PNG/JPEG at most 1 MiB')
 repo=node('repository')
 if repo is not None and repo.get('url')!=config.get('repositoryUrl'):issues.append('Unapproved repository URL was embedded')
 if a.release:
  source_link=root/'src/Kirora/obj/Release/net8.0/Kirora.sourcelink.json'
  if not source_link.exists():issues.append('SourceLink mapping was not generated from the approved repository')
  else:
   mapping=json.loads(source_link.read_text()).get('documents',{})
   expected_prefix='https://raw.githubusercontent.com/'+(config.get('repositoryUrl') or '').removeprefix('https://github.com/')+'/'
   if not mapping or not all(value.startswith(expected_prefix) for value in mapping.values()):issues.append('SourceLink mapping does not point at the approved repository')
  if not config.get('maintainerApproved'):issues.append('Maintainer approval is not recorded')
  if not config.get('licenseExpression') or text('license')!=config['licenseExpression']:issues.append('Approved license expression is missing/mismatched')
  if not config.get('repositoryUrl') or repo is None or repo.get('url')!=config['repositoryUrl'] or not repo.get('commit'):issues.append('Approved repository/commit metadata is missing')
  if not re.fullmatch(r'\d+\.\d+\.\d+-alpha\.\d+',config['version']):issues.append('Only explicit alpha versions are allowed by this workflow')
symbols=package.with_suffix('.snupkg')
if not symbols.exists():issues.append('Missing symbols package')
else:
 with ZipFile(symbols) as z:
  if 'lib/net8.0/Kirora.pdb' not in z.namelist():issues.append('Symbols package is missing portable PDB')
if issues:
 for issue in issues:print(issue,file=sys.stderr)
 raise SystemExit(1)
print('Package and portable symbols validated'+(' for approved release.' if a.release else '; license/repository/ownership remain maintainer decisions.'))
