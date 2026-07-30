#!/usr/bin/env python3
import hashlib, json, os, shutil, subprocess
from pathlib import Path

STAGE=Path("/mnt/c/temp/r73-publication-AxLZ66/out")
PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations")
BACKUP=Path("/mnt/c/temp/r73-publication-AxLZ66/backup")
EXPECTED={
 "termbase.index.json":"6958be3d84872d5b62a7c163747468daab65c3290e0fddc7ab77d5a1c654379a",
 "termbase.json":"16646285879538b359dc05ce0f9783e238a125863a993bba32116fbcb245fab9",
 "termbase.v2.json":"69ddcfe3b31ff1a288e3ee911c2d65a82ba3e6a6e218f7079ae534eaa3d350f9",
 "termbase/140.json":"03eea3d02f682f279075d14cfe826cd85251df855ad1525ca65db91e16a17528",
 "termbase/158.json":"d28c6c49a42de3605bb6d6f97daf341eb50533446a58c84c5ba901aa06562ac7",
 "termbase/190.json":"0cd3489464760b7ded7f3f7312bf969a8b36da78455cfc8eb14ab54e683659f4",
}
PRODUCTS={
 "t_193535d6b929":"55205fc3d73fb71b8f769e419745e121f78e68e16b86a32f22211b9e1aaea06c",
 "t_195a2b5b63d4":"3d3bd469d4d92a5029463b44d61fd6b4ece9d545fc868f9cbe08f609fdb1cf53",
}
REMOVED="t_19784084ccb4"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x): return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
for rel,digest in EXPECTED.items():
 if sha(STAGE/rel)!=digest: raise SystemExit(f"staged authority drift: {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in EXPECTED:
 saved=BACKUP/rel; saved.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(PUBLIC/rel,saved)
try:
 for rel in EXPECTED:
  target=PUBLIC/rel; tmp=target.with_name(f".{target.name}.r73.tmp")
  shutil.copy2(STAGE/rel,tmp); os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text())
 legacy=json.loads((PUBLIC/"termbase.json").read_text())
 index=json.loads((PUBLIC/"termbase.index.json").read_text())
 shard_entries=[]
 for path in (PUBLIC/"termbase").glob("*.json"):
  shard_entries.extend(json.loads(path.read_text())["Entries"])
 by_id={entry["Id"]:entry for entry in rich["Entries"]}
 if not (len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shard_entries)==4716
  and REMOVED not in by_id
  and all(canon(by_id[ident])==digest for ident,digest in PRODUCTS.items())
  and all(sha(PUBLIC/rel)==digest for rel,digest in EXPECTED.items())):
  raise RuntimeError("post-install parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in EXPECTED: os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4716,"replacementParity":"2/2","removalParity":"1/1","files":EXPECTED}))
