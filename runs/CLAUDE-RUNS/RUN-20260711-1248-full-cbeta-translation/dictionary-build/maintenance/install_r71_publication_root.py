#!/usr/bin/env python3
import hashlib, json, os, shutil, subprocess
from pathlib import Path
STAGE=Path("/mnt/c/temp/r71-publication-7K4GwF/out")
PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations")
BACKUP=Path("/mnt/c/temp/r71-publication-7K4GwF/backup")
EXPECTED={
 "termbase.index.json":"d736d65705966093f3325fb34aa8d6845322648cc5f44a5d40849104535e7c53",
 "termbase.json":"dd1c17dffbbb51682db07240f29922726f4939f47b79fe9ff6e062af3cd8bf61",
 "termbase.v2.json":"06f3ffcb29b1cfcb68d326749b96278792834406b7fb3f0d558e63108a3eb865",
 "termbase/009.json":"bf477bfb538765b7c3cf6da8c3d4191b148157431d94fb93d9f0641d9caf35ad",
 "termbase/016.json":"70fc009c18d5568da1d6a2c8d9aa5b3e6b8ed20818e1d9e86c4e4752ad5361a1",
 "termbase/049.json":"38dddf5d288d59c2c2dba4cc56ef8a56385154a68901696d8ecb7c60436c01f1",
}
PRODUCTS={
 "t_19025ed20021":"5d584e6f58f939a8f6680ef98efb7613a1e017e4d1a6cea055b90a7bbb18966c",
 "t_192801178305":"20d221a57ec6cb1a3886b008edb5d0912452499e0a87d0adc0e2052d9addd41e",
 "t_192de5925365":"e4565c20e7c2f63b65d86357fcd420b92799ceff66a59f34f4f8d6a1c29b687f",
}
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x): return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
for rel,digest in EXPECTED.items():
 if sha(STAGE/rel)!=digest: raise SystemExit(f"staged authority drift: {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in EXPECTED:
 saved=BACKUP/rel; saved.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(PUBLIC/rel,saved)
try:
 for rel in EXPECTED:
  target=PUBLIC/rel; tmp=target.with_name(f".{target.name}.r71.tmp")
  shutil.copy2(STAGE/rel,tmp); os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text())
 legacy=json.loads((PUBLIC/"termbase.json").read_text())
 index=json.loads((PUBLIC/"termbase.index.json").read_text())
 shard_entries=[]
 for path in (PUBLIC/"termbase").glob("*.json"): shard_entries.extend(json.loads(path.read_text())["Entries"])
 by_id={entry["Id"]:entry for entry in rich["Entries"]}
 if not (len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shard_entries)==4717
  and all(canon(by_id[ident])==digest for ident,digest in PRODUCTS.items())
  and all(sha(PUBLIC/rel)==digest for rel,digest in EXPECTED.items())):
  raise RuntimeError("post-install parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in EXPECTED: os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4717,"exactProductParity":"3/3","files":EXPECTED}))
