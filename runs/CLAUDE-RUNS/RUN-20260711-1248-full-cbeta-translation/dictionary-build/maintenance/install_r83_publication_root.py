#!/usr/bin/env python3
import hashlib,json,os,shutil,subprocess,sys
from pathlib import Path
if len(sys.argv)!=4:raise SystemExit("usage: install_r83_publication_root.py STAGE PUBLIC BACKUP")
STAGE,PUBLIC,BACKUP=map(Path,sys.argv[1:])
PRODUCTS={"t_1c2e34e1abb7":"1b7aab4ff6a3a6ec04e80a5a00175950301a342260e87f98fd5fe0be7ee19164","t_1c3869bb802d":"61ddda8521b35c4bd3083e41d96bff71c60b0314cccbb610f2737e8c5e5d5484","t_1c7d25824f85":"856bc57ea516076cd7e3c91a5687bf5e04dc921f7c522532f32c3ac1ab9da861"}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x):return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
r=json.loads((STAGE/"merge-receipt.json").read_text()); expected=r["outputSha256"]
for rel,d in expected.items():
 if sha(STAGE/rel)!=d:raise SystemExit(f"stage drift {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in expected:
 p=BACKUP/rel;p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(PUBLIC/rel,p)
try:
 for rel in expected:
  target=PUBLIC/rel;tmp=target.with_name(f".{target.name}.r83.tmp");shutil.copy2(STAGE/rel,tmp);os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text());legacy=json.loads((PUBLIC/"termbase.json").read_text());index=json.loads((PUBLIC/"termbase.index.json").read_text());shards=[]
 for p in (PUBLIC/"termbase").glob("*.json"):shards.extend(json.loads(p.read_text())["Entries"])
 by={e["Id"]:e for e in rich["Entries"]}
 if not(len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shards)==4714 and all(canon(by[i])==d for i,d in PRODUCTS.items()) and all(sha(PUBLIC/rel)==d for rel,d in expected.items())):raise RuntimeError("parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in expected:os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4714,"replacementParity":"3/3","files":expected}))
