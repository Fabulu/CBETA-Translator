#!/usr/bin/env python3
import hashlib,json,os,shutil,subprocess
from pathlib import Path
STAGE=Path("/mnt/c/temp/r80-publication-urt3tX/out");PUBLIC=Path("/mnt/c/programmieren/CbetaZenTranslations");BACKUP=Path("/mnt/c/temp/r80-publication-urt3tX/backup")
EXPECTED={
"termbase.index.json":"17b8328733977aeaf01e7477bc572bd7ac34080672f7e87aa55150b696cd4368",
"termbase.json":"417df31c2f337b7fba8c5779563828cb22600026bf0dabf20500f87aa8266a66",
"termbase.v2.json":"0bbfec4e1b4fc79cab3a9c1e1ad399f80c082659a30c6cb43075e2cd4435c06d",
"termbase/082.json":"4aa057ce6ffb24cc1836b2c6b750867e883b96eb5fd0450cf25d660057c76b8f",
"termbase/191.json":"27ad9d63da55311a6b1965994afbd52269e2221142d1410d621f1b7def47ae3c",
"termbase/222.json":"f1bb2de17474302d1b8faa832888577e8cdb0fc22c1080d402ccc63c8c21efa0"}
PRODUCTS={"t_1a9ab2ab3675":"3ed6020e6826c519e44e1cf508ee52a1a24af0bf192009b089fed4a393080bef","t_1b056c5af929":"eec67e2bd341d10e7caeb7543032e55fa3c2fe28cfcae333db9c3715492f5879"}
REMOVED="t_1a86ee3d406f"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def canon(x):return hashlib.sha256((json.dumps(x,ensure_ascii=False,indent=2)+"\n").encode()).hexdigest()
for rel,d in EXPECTED.items():
 if sha(STAGE/rel)!=d:raise SystemExit(f"stage drift {rel}")
BACKUP.mkdir(parents=True,exist_ok=False)
for rel in EXPECTED:
 p=BACKUP/rel;p.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(PUBLIC/rel,p)
try:
 for rel in EXPECTED:
  target=PUBLIC/rel;tmp=target.with_name(f".{target.name}.r80.tmp");shutil.copy2(STAGE/rel,tmp);os.replace(tmp,target)
 rich=json.loads((PUBLIC/"termbase.v2.json").read_text());legacy=json.loads((PUBLIC/"termbase.json").read_text());index=json.loads((PUBLIC/"termbase.index.json").read_text())
 shards=[]
 for p in (PUBLIC/"termbase").glob("*.json"):shards.extend(json.loads(p.read_text())["Entries"])
 by={e["Id"]:e for e in rich["Entries"]}
 if not(len(rich["Entries"])==len(legacy)==len(index["Terms"])==len(shards)==4714 and REMOVED not in by and all(canon(by[i])==d for i,d in PRODUCTS.items()) and all(sha(PUBLIC/r)==d for r,d in EXPECTED.items())):raise RuntimeError("parity failed")
 subprocess.run(["python3","scripts/audit-dictionary-integrity.py"],cwd=PUBLIC,check=True)
except Exception:
 for rel in EXPECTED:os.replace(BACKUP/rel,PUBLIC/rel)
 raise
print(json.dumps({"count":4714,"replacementParity":"2/2","removalParity":"1/1","files":EXPECTED}))
