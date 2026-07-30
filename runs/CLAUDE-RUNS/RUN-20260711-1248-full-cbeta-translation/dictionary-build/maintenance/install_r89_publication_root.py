#!/usr/bin/env python3
import hashlib,json,os,shutil,subprocess,sys
from pathlib import Path
if len(sys.argv)!=4:raise SystemExit("usage: install_r89_publication_root.py STAGE PUBLIC BACKUP")
STAGE,PUBLIC,BACKUP=map(Path,sys.argv[1:])
PRODUCTS={"t_1e41b014d80e":"d203e6446035f420e2edd7efecc20574583fa694fe151fa8d586740c61061a17","t_1f3653f30389":"527dd956e5d008187b3e08b0d8875c328846b4dd2c1f0fd96064a45a894774a4","t_1fe4eac13d6e":"d7c398269a62a6f6ba114475ce39bf57354f6cf6d3bd6c8a7ba65131a525d4a2"}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x):return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
r=json.loads((STAGE/"merge-receipt.json").read_text());expected=r["outputSha256"]
for rel,d in expected.items():
 if sha(STAGE/rel)!=d:raise SystemExit(f"stage drift {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in expected:
 p=BACKUP/rel;p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(PUBLIC/rel,p)
try:
 for rel in expected:
  target=PUBLIC/rel;tmp=target.with_name(f".{target.name}.r89.tmp");shutil.copy2(STAGE/rel,tmp);os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text());legacy=json.loads((PUBLIC/"termbase.json").read_text());index=json.loads((PUBLIC/"termbase.index.json").read_text());shards=[]
 for p in (PUBLIC/"termbase").glob("*.json"):shards.extend(json.loads(p.read_text())["Entries"])
 by={e["Id"]:e for e in rich["Entries"]}
 if not(len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shards)==4714 and all(canon(by[i])==d for i,d in PRODUCTS.items()) and all(sha(PUBLIC/rel)==d for rel,d in expected.items())):raise RuntimeError("parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in expected:os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4714,"replacementParity":"3/3","files":expected}))
