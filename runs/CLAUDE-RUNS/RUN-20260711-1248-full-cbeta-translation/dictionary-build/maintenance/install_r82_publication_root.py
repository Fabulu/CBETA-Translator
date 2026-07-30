#!/usr/bin/env python3
import hashlib,json,os,shutil,subprocess,sys
from pathlib import Path
if len(sys.argv)!=4: raise SystemExit("usage: install_r82_publication_root.py STAGE PUBLIC BACKUP")
STAGE,PUBLIC,BACKUP=map(Path,sys.argv[1:])
PRODUCTS={
"t_1b2b5d1e63c9":"c3c00f288a14949ba03ac532f75de036016580ba4a063b6d656695625c0ab093",
"t_1b3195ce4368":"d370508ad817d9273491b86d8357f7e0b1d450580c281ee0408b6071cb748077",
"t_1b6cbdc8d52e":"8b3e4469b00e7886f4dc2ca1113c5dc80d84904482309967165f85df64774e49"}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x):return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
receipt=json.loads((STAGE/"merge-receipt.json").read_text())
EXPECTED=receipt["outputSha256"]
if set(EXPECTED)!=set(receipt["changedFiles"]):raise SystemExit("receipt file mismatch")
for rel,d in EXPECTED.items():
 if sha(STAGE/rel)!=d:raise SystemExit(f"stage drift {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in EXPECTED:
 p=BACKUP/rel;p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(PUBLIC/rel,p)
try:
 for rel in EXPECTED:
  target=PUBLIC/rel;tmp=target.with_name(f".{target.name}.r82.tmp");shutil.copy2(STAGE/rel,tmp);os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text());legacy=json.loads((PUBLIC/"termbase.json").read_text());index=json.loads((PUBLIC/"termbase.index.json").read_text())
 shards=[]
 for p in (PUBLIC/"termbase").glob("*.json"):shards.extend(json.loads(p.read_text())["Entries"])
 by={e["Id"]:e for e in rich["Entries"]}
 if not(len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shards)==4714 and all(canon(by[i])==d for i,d in PRODUCTS.items()) and all(sha(PUBLIC/r)==d for r,d in EXPECTED.items())):raise RuntimeError("parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in EXPECTED:os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4714,"replacementParity":"3/3","files":EXPECTED}))
