#!/usr/bin/env python3
import hashlib, json, os, shutil, subprocess
from pathlib import Path

STAGE=Path("/mnt/c/temp/r75-publication-uQkBPg/out")
PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations")
BACKUP=Path("/mnt/c/temp/r75-publication-uQkBPg/backup")
EXPECTED={
 "termbase.index.json":"e28b426f406322257f58b8910a318088503c562fdfae4eeec8dddf44c2c27a34",
 "termbase.json":"f9021e764a30718297277edb1fdcf517c0351e03c57342a23d6d9ebb587c8d65",
 "termbase.v2.json":"3b56a47d05fb9347da5f9b42f822175434d75bd6f13651c84d5f2ef69250ab7c",
 "termbase/083.json":"6f52a83436c4d666fadc21c6706bf6919766ae0554546d47ee8c23f82352c4bc",
 "termbase/166.json":"f623c979efc4e2005a33802f4558063e21f97e301d4015aa170bdb1bcb039dd2",
 "termbase/209.json":"202e852de54b8fba9770507d5805378f0bad7e4b705e3f07cf474e7409c071b2",
}
PRODUCTS={
 "t_19c58dc2d3be":"37c02f14d84819820d9691270c9d54d178d4c409f2ff1ea6bcae02b5778b0060",
 "t_1a0dbf72d9b7":"c997bd050b0cebf05e5a21586546ecebe2c55172dea81db51bdbb788dc0ea112",
}
REMOVED="t_19b90a49b420"
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def canon(value): return hashlib.sha256((json.dumps(value,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
for rel,digest in EXPECTED.items():
 if sha(STAGE/rel)!=digest: raise SystemExit(f"staged authority drift: {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in EXPECTED:
 saved=BACKUP/rel; saved.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(PUBLIC/rel,saved)
try:
 for rel in EXPECTED:
  target=PUBLIC/rel; temporary=target.with_name(f".{target.name}.r75.tmp")
  shutil.copy2(STAGE/rel,temporary); os.replace(temporary,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text())
 legacy=json.loads((PUBLIC/"termbase.json").read_text())
 index=json.loads((PUBLIC/"termbase.index.json").read_text())
 shard_entries=[]
 for shard in (PUBLIC/"termbase").glob("*.json"):
  shard_entries.extend(json.loads(shard.read_text())["Entries"])
 by_id={entry["Id"]:entry for entry in rich["Entries"]}
 if not (len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shard_entries)==4715
  and REMOVED not in by_id
  and all(canon(by_id[ident])==digest for ident,digest in PRODUCTS.items())
  and all(sha(PUBLIC/rel)==digest for rel,digest in EXPECTED.items())):
  raise RuntimeError("post-install parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in EXPECTED: os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4715,"replacementParity":"2/2","removalParity":"1/1","files":EXPECTED}))
