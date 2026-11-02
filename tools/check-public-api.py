#!/usr/bin/env python3
"""Enforce the migrated public API and exact documented deltas from preserved historical baselines."""
import argparse,json,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--assembly',default=str(root/'src/Kirora/bin/Release/net8.0/Kirora.dll'));a=p.parse_args()
fixtures=root/'tests/Kirora.Tests/Fixtures'
migration=json.loads((fixtures/'migration-api-changes.json').read_text())
with tempfile.TemporaryDirectory(prefix='kirora-api-') as directory:
 snapshot=Path(directory)/'current.txt'
 subprocess.run(['dotnet','run','--project',str(root/'tools/Kirora.ApiCompatibility'),'--configuration','Release','--',a.assembly,str(snapshot)],check=True,cwd=root)
 current=set(snapshot.read_text().splitlines())
 target=set((fixtures/'public-api-upstream-30043317.txt').read_text().splitlines())
 if current!=target:raise SystemExit('Unexpected public API changes. Missing:\n'+'\n'.join(sorted(target-current))+'\nAdded:\n'+'\n'.join(sorted(current-target)))
 for name,documented in migration['removedSignatures'].items():
  expected=set((fixtures/name).read_text().splitlines());missing=expected-current
  if missing!=set(documented):raise SystemExit('Historical API delta differs from the migration report: '+name+'\n'+'\n'.join(sorted(missing^set(documented))))
  print(f'{name}: {len(missing)} explicitly documented removed/changed signatures (intentional pre-release contract migration).')
 print('Migrated API baseline matches exactly; historical deltas verified, not suppressed.')
