#!/usr/bin/env python3
"""Select the next exact authoritative row for R94 replacement2."""
from __future__ import annotations
import hashlib, json, os, time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]; M=ROOT/"maintenance"
GATE=M/"non-iriya-v7-depth-regeneration-r94-replacement2-timegate-root.json"
SELECTOR=M/"last1500-public-depth/final-scope/full-regeneration-selector.json"
UNION=M/"non-iriya-v7-depth-regeneration-r93-resolved-union-root.json"
R94=M/"non-iriya-v7-depth-regeneration-r94-selection-root.json"
R1=M/"non-iriya-v7-depth-regeneration-r94-replacement1-selection-root.json"
OUT=M/"non-iriya-v7-depth-regeneration-r94-replacement2-selection-root.json"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p): return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def exclusive(p,v):
 data=(json.dumps(v,ensure_ascii=False,indent=2)+"\n").encode(); fd=os.open(p,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o644)
 try: os.write(fd,data); os.fsync(fd)
 finally: os.close(fd)
started=time.time(); gate=read(GATE); assert started>=gate["startedEpoch"]
resolved=set(read(UNION)["ids"]); original=set(read(R94)["selectedIds"]); failed={"t_2738431562e6",read(R1)["selected"]["identityId"]}
excluded=resolved|original|failed
selector=read(SELECTOR); rows=[]; chunks=[]
for c in selector["chunks"]:
 p=ROOT/c["path"]; assert sha(p)==c["sha256"]; rows.extend(read(p)["rows"]); chunks.append({"path":str(p.relative_to(ROOT)),"sha256":sha(p)})
s=next(r for r in rows if r["id"] not in excluded)
payload={
 "schemaVersion":"r94-replacement2-selection.v1","cohort":"R94-replacement2",
 "artifactZero":{"path":str(GATE.relative_to(ROOT)),"sha256":sha(GATE)},
 "authoritativeSelector":{"path":str(SELECTOR.relative_to(ROOT)),"sha256":sha(SELECTOR),"chunks":chunks},
 "exclusions":{"r93UnionSha256":sha(UNION),"originalR94SelectionSha256":sha(R94),"replacement1SelectionSha256":sha(R1),"excludedCount":len(excluded),"failedReplacementIds":sorted(failed)},
 "selectionRule":"first authoritative row outside exact R93 union, all original R94 IDs, failed 無字, and failed 戒",
 "selected":{"batchOrdinal":30,"identityId":s["id"],"term":s["term"],"minimumExactUnitIndependentTier1Or2FamiliesPerSense":3,"selectorRequiredFloor":s["requiredFloor"],"corpusHits":s.get("corpusHits"),"corpusFiles":s.get("corpusFiles"),"corpusWorks":s.get("corpusWorks")},
 "collisionCount":0,"sourceExtractionPerformed":False,"hardPass":time.time()-gate["startedEpoch"]<=gate["deadlinesSeconds"]["selection"]
}
exclusive(OUT,payload); print(json.dumps({"sha256":sha(OUT),"selected":payload["selected"],"hardPass":payload["hardPass"]},ensure_ascii=False))
