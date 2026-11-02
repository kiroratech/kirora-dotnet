#!/usr/bin/env python3
"""Compare a current public API signature snapshot to both committed compatibility baselines."""
import argparse,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--assembly',default=str(root/'src/Kirora/bin/Release/net8.0/Kirora.dll'));a=p.parse_args()
with tempfile.TemporaryDirectory(prefix='kirora-api-') as directory:
 snapshot=Path(directory)/'current.txt'
 subprocess.run(['dotnet','run','--project',str(root/'tools/Kirora.ApiCompatibility'),'--configuration','Release','--',a.assembly,str(snapshot)],check=True,cwd=root)
 current=set(snapshot.read_text().splitlines())
 for name in ['public-api-phase2.txt','public-api-phase3.txt']:
  expected=set((root/'tests/Kirora.Tests/Fixtures'/name).read_text().splitlines());missing=expected-current
  if missing:raise SystemExit('Removed public signatures in '+name+':\n'+'\n'.join(sorted(missing)))
 print('Phase 2 and Phase 3 public API type/member signatures are preserved.')
