#!/usr/bin/env python3
import hashlib, json, os, shutil, subprocess
from pathlib import Path
STAGE=Path("/mnt/c/temp/r70-publication-8kUSbP/out")
PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations")
BACKUP=Path("/mnt/c/temp/r70-publication-8kUSbP/backup")
EXPECTED={
 "termbase.index.json":"6a75bdde95031d2bb61a6763aadceceef62f80a5f7a27f178aed92f6d232b15b",
 "termbase.json":"755ce09c54d435f1e4047f5fb00b5a7ce8a10bb3f0782587c81a17d3f6bfe410",
 "termbase.v2.json":"7e1fc0140aff5a9fd9d4bc35a630282b7ad9855a484cf5d05feab05d8f9654e4",
 "termbase/072.json":"a63512e0c76161dc6dbed0b0bde21a6418d2e09e417fe6bd937471361e088b30",
 "termbase/171.json":"3c8e48e18a71255ef316683395793e1d292f7be6a7a382641e66f0398c20f487",
 "termbase/230.json":"7ec24ae889d3ac9c7c9e666e68ad4b2829633599e135e7658053b91c6a071875",
}
PRODUCTS={
 "t_17c1d8b4f105":"8b092b60342d50f2bb68cc2ef32363100468095c115c66eadb988fa627756b7b",
 "t_1820fe9e6a50":"adb8cfddd4a9c472a2d17decaa16760101cb126de012b46f3b9192310ccae409",
 "t_1901868691a8":"083c60adb7c4c71fd24af8ff7ea1d2ac776a9dbe2c361fd09b953e03b8322bb3",
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
  target=PUBLIC/rel; tmp=target.with_name(f".{target.name}.r70.tmp")
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
