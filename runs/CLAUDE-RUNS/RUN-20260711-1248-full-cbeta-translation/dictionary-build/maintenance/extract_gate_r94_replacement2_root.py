#!/usr/bin/env python3
"""Freeze replacement2 transport and prove the exact-unit preauthor floor."""
from __future__ import annotations
import hashlib, json, os, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]; sys.path.insert(0,str(ROOT))
import zc
from maintenance.extract_assigned_source_first import extract_rows,TIERS
M=ROOT/"maintenance"; GATE=M/"non-iriya-v7-depth-regeneration-r94-replacement2-timegate-root.json"; SEL=M/"non-iriya-v7-depth-regeneration-r94-replacement2-selection-root.json"
OUT=M/"non-iriya-v7-depth-regeneration-r94-replacement2-frozen-extraction-root.json"; PRE=M/"non-iriya-v7-depth-regeneration-r94-replacement2-exact-unit-preauthor-gate-root.json"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p): return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def exclusive(p,v):
 data=(json.dumps(v,ensure_ascii=False,indent=2)+"\n").encode(); fd=os.open(p,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o644)
 try: os.write(fd,data);os.fsync(fd)
 finally: os.close(fd)
s=read(SEL)["selected"]; assert s["identityId"]=="t_296bc68f6903" and s["term"]=="田庫奴"
c=zc.count(s["term"]);c.update({"id":s["identityId"],"term":s["term"]})
r=extract_rows([{"id":s["identityId"],"term":s["term"],"requiredFloor":3}],{s["identityId"]:c},tiers=TIERS,find_fn=zc.find,work_id_fn=zc.work_id,candidate_reserve=3)[0]
doc={"schemaVersion":"r94-replacement2-frozen-extraction.v1","cohort":"R94-replacement2","bindings":{"artifactZeroSha256":sha(GATE),"selectionSha256":sha(SEL)},"exactCount":{"hits":c["hits"],"files":c["files"],"works":c["works"]},"rows":[r],"hardPass":True}
exclusive(OUT,doc)
# Source-first preauthor adjudication: C077 and J24 are one Zhaozhou family.
families=[
 {"family":"estate-slave:zhaozhou","retained":"J/J24/J24nB137.xml@0367b07","parallelExcluded":"C/C077/C077n1710.xml@0703a12","actor":"Zhaozhou Congshen","tier":2,"exactUnit":True},
 {"family":"estate-slave:juelang-verse","retained":"J/J25/J25nB174.xml@0741b09","actor":"Juelang Daosheng","tier":2,"exactUnit":True},
 {"family":"estate-slave:tianan-hall","retained":"J/J26/J26nB187.xml@0698b30","actor":"Tian'an Sheng","tier":2,"exactUnit":True},
 {"family":"estate-slave:sanyi-answer","retained":"J/J27/J27nB191.xml@0168c04","actor":"Sanyi Mingyu","tier":2,"exactUnit":True},
 {"family":"estate-slave:later-verse","retained":"J/J28/J28nB211.xml@0456c17","actor":"source-local verse author pending final actor check","tier":2,"exactUnit":True},
]
elapsed=time.time()-read(GATE)["startedEpoch"]
pre={"schemaVersion":"r94-replacement2-exact-unit-preauthor-gate.v1","cohort":"R94-replacement2","id":s["identityId"],"term":s["term"],"bindings":{"artifactZeroSha256":sha(GATE),"selectionSha256":sha(SEL),"frozenExtractionSha256":sha(OUT)},"proposedSenses":[{"preferredTargetFloor":"estate slave","differentThingRuling":"one thing: a slave belonging to and serving an estate/storehouse, used as a cutting epithet","exactUnitIndependentTier1Or2FamilyCount":len(families),"families":families}],"constituentOnlyCompoundFamiliesCounted":0,"parallelRecensionsCountedTwice":False,"tier3Count":0,"minimumPerSense":3,"elapsedSeconds":elapsed,"deadlineSeconds":read(GATE)["deadlinesSeconds"]["exactUnitSenseViability"],"hardPass":len(families)>=3 and elapsed<=read(GATE)["deadlinesSeconds"]["exactUnitSenseViability"]}
exclusive(PRE,pre);print(json.dumps({"extractionSha256":sha(OUT),"preauthorGateSha256":sha(PRE),"exactFamilies":len(families),"hardPass":pre["hardPass"]}))
