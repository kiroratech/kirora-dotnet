#!/usr/bin/env python3
"""Verify embedded PDB SourceLink mappings and package repository commit, without fetching remote source."""
import json,re,subprocess,xml.etree.ElementTree as E
from pathlib import Path
from zipfile import ZipFile
root=Path(__file__).resolve().parents[1];config=json.loads((root/'eng/release.json').read_text())
url=config['repositoryUrl'];assert url=='https://github.com/kiroratech/kirora-dotnet'
out=root/'artifacts/pdb-sourcelink.json';out.parent.mkdir(exist_ok=True);documents=root/'artifacts/pdb-documents.json'
subprocess.run(['dotnet','run','--project',str(root/'tools/Kirora.ApiCompatibility'),'--configuration','Release','--','--source-link',str(root/'src/Kirora/bin/Release/net8.0/Kirora.pdb'),str(out),str(documents)],cwd=root,check=True)
embedded=json.loads(out.read_text());generated=json.loads((root/'src/Kirora/obj/Release/net8.0/Kirora.sourcelink.json').read_text())
if embedded!=generated:raise SystemExit('PDB SourceLink differs from the generated mapping')
with ZipFile(root/'src/Kirora/bin/Release'/f"{config['packageId']}.{config['version']}.nupkg") as z:
 spec=E.fromstring(z.read(next(n for n in z.namelist() if n.endswith('.nuspec'))));repo=spec.find('.//{*}repository')
 if repo is None or repo.get('url')!=url:raise SystemExit('Package repository metadata is missing or incorrect')
 commit=repo.get('commit','')
 if not re.fullmatch('[a-f0-9]{40}',commit):raise SystemExit('Package repository commit is missing')
 prefix='https://raw.githubusercontent.com/kiroratech/kirora-dotnet/'+commit+'/'
 mappings=embedded.get('documents',{})
 if not mappings or not all(value.startswith(prefix) for value in mappings.values()):raise SystemExit('SourceLink mappings do not match the package commit')
for document in json.loads(documents.read_text()):
 name=document['name'];key=next((key for key in mappings if name.startswith(key.removesuffix('*'))),None)
 if key is None:raise SystemExit('PDB document is not covered by a SourceLink mapping')
 if not document['embedded']:
  relative=name[len(key.removesuffix('*')):]
  exists=subprocess.run(['git','cat-file','-e',commit+':'+relative],cwd=root,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
  if exists.returncode:raise SystemExit('A PDB document is neither embedded nor present at the SourceLink commit')
subprocess.run(['git','cat-file','-e',commit+'^{commit}'],cwd=root,check=True)
print('Portable PDB and NuGet SourceLink metadata match official repository commit '+commit+'. Remote source availability requires the approved push.')
