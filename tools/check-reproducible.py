#!/usr/bin/env python3
"""Compare deterministic library DLL/PDB output from two isolated paths; never publishes packages."""
from pathlib import Path
import hashlib,shutil,subprocess,tempfile,json
root=Path(__file__).resolve().parents[1];digests=[]
with tempfile.TemporaryDirectory(prefix='kirora-repro-') as temporary:
 for name in ['first','second']:
  checkout=Path(temporary)/name
  for folder in ['src','spec']:
   shutil.copytree(root/folder,checkout/folder,ignore=shutil.ignore_patterns('bin','obj','__pycache__'))
  for filename in ['global.json','Directory.Build.props','README.md']:shutil.copyfile(root/filename,checkout/filename)
  subprocess.run(['dotnet','build','src/Kirora/Kirora.csproj','--configuration','Release','--verbosity','quiet'],cwd=checkout,check=True)
  output=checkout/'src/Kirora/bin/Release/net8.0'
  digests.append({extension:hashlib.sha256((output/('Kirora.'+extension)).read_bytes()).hexdigest() for extension in ['dll','pdb']})
if digests[0]!=digests[1]:raise SystemExit('Library DLL/PDB differed between isolated build paths')
path=root/'artifacts/reproducible-builds.json';path.parent.mkdir(exist_ok=True);path.write_text(json.dumps({'sameInputsTwoPaths':True,'sha256':digests[0]},indent=2)+'\n')
print('Library DLL and portable PDB are byte-identical across two isolated build paths.')
