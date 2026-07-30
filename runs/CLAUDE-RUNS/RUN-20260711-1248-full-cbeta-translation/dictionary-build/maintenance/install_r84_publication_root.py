#!/usr/bin/env python3
import hashlib,json,os,shutil,subprocess,sys
from pathlib import Path
if len(sys.argv)!=4:raise SystemExit("usage: install_r84_publication_root.py STAGE PUBLIC BACKUP")
STAGE,PUBLIC,BACKUP=map(Path,sys.argv[1:])
PRODUCTS={"t_1cec9c4c3c40":"0ecbac9499c7d8e90d67bdb8e0ff92651486296f5f37919f50cf99d0fa9d71e6","t_1cfa8b8aa2a3":"e5880063bb97d6dcc4d52b9dc51ea527ef30642c009ace4218b1f6b1ee374ee3","t_1d0056511f4d":"ad5a701713658c213dd52f8cefa1adc916d50b3d90cdbf9ef575c7bfe4124923"}
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
  target=PUBLIC/rel;tmp=target.with_name(f".{target.name}.r84.tmp");shutil.copy2(STAGE/rel,tmp);os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text());legacy=json.loads((PUBLIC/"termbase.json").read_text());index=json.loads((PUBLIC/"termbase.index.json").read_text());shards=[]
 for p in (PUBLIC/"termbase").glob("*.json"):shards.extend(json.loads(p.read_text())["Entries"])
 by={e["Id"]:e for e in rich["Entries"]}
 if not(len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shards)==4714 and all(canon(by[i])==d for i,d in PRODUCTS.items()) and all(sha(PUBLIC/rel)==d for rel,d in expected.items())):raise RuntimeError("parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in expected:os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4714,"replacementParity":"3/3","files":expected}))
