#!/usr/bin/env python3
"""Author the exact-unit-gated R94 replacement2 entry 田庫奴."""
from __future__ import annotations
import hashlib,json,os,time
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];M=ROOT/"maintenance"
GATE=M/"non-iriya-v7-depth-regeneration-r94-replacement2-timegate-root.json";SEL=M/"non-iriya-v7-depth-regeneration-r94-replacement2-selection-root.json";EXT=M/"non-iriya-v7-depth-regeneration-r94-replacement2-frozen-extraction-root.json";PRE=M/"non-iriya-v7-depth-regeneration-r94-replacement2-exact-unit-preauthor-gate-root.json";REVIEW=M/"r94-replacement2-cross-review-by-c.json";OUT=M/"r94-replacement2-correction1-closure-root.json"
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def exclusive(p,v):
 data=(json.dumps(v,ensure_ascii=False,indent=2)+"\n").encode();fd=os.open(p,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o644)
 try:os.write(fd,data);os.fsync(fd)
 finally:os.close(fd)
assert read(PRE)["hardPass"] is True
candidates={x["relPath"]:x for x in read(EXT)["rows"][0]["sourceCandidates"]}
specs=[
 ("J/J24/J24nB137.xml","Zhaozhou Congshen","utterer","estate-slave:zhaozhou","In the 至道無難 exchange, Zhaozhou directly retorts 田庫奴 when the questioner says his preceding answer still involves discrimination."),
 ("J/J25/J25nB174.xml","Juelang Daosheng","verse-author","estate-slave:juelang-verse","Juelang's authored lineage verse calls the buddhas 田庫奴 in a rebuking comparison."),
 ("J/J26/J26nB187.xml","Tian'an Sheng","utterer","estate-slave:tianan-hall","Tian'an contrasts a rich man guarding his city with ending his life willingly as a 田庫奴."),
 ("J/J27/J27nB191.xml","Sanyi Mingyu","utterer","estate-slave:sanyi-answer","Sanyi answers a request to explain Xuansha's warning with 者田庫奴 and repeats it in his capping verse."),
]
rows=[]
for path,actor,role,family,grammar in specs:
 c=candidates[path];assert c["tier"]==2 and c["matchedTerm"]=="田庫奴"
 row={"relPath":path,"workId":c["workId"],"tier":2,"fromLb":c["fromLb"],"toLb":c["toLb"],"context":c["context"],"contextSha256":c["contextSha256"],"spanSha256":c["spanSha256"],"actorDecision":{"status":"linked","actor":actor,"role":role},"voiceLayer":"direct-turn" if role=="utterer" else "transmitted-verse","witnessFamilyId":family,"deploymentRole":"original-use","grammarEvidence":grammar}
 if path=="J/J27/J27nB191.xml":
  row["voiceLayers"]=[
   {"text":"答：者田庫奴","voiceLayer":"direct-turn","actor":"Sanyi Mingyu"},
   {"text":"頌：者田庫奴，一刀兩斷","voiceLayer":"authored-capping-verse","actor":"Sanyi Mingyu"},
  ]
  row["exactOccurrencesInContext"]=2
  row["familyCountForBothOccurrences"]=1
 rows.append(row)
out={"schemaVersion":"r94-replacement2-correction-closure.v1","cohort":"R94-replacement2","id":"t_296bc68f6903","term":"田庫奴","replacesFailedIds":["t_2738431562e6","t_292ac4c33b4f"],"bindings":{"artifactZero":{"path":str(GATE.relative_to(ROOT)),"sha256":sha(GATE)},"selection":{"path":str(SEL.relative_to(ROOT)),"sha256":sha(SEL)},"frozenExtraction":{"path":str(EXT.relative_to(ROOT)),"sha256":sha(EXT)},"exactUnitPreauthorGate":{"path":str(PRE.relative_to(ROOT)),"sha256":sha(PRE)},"independentReview":{"path":str(REVIEW.relative_to(ROOT)),"sha256":sha(REVIEW)}},"correctionPass":{"finiteDeltaCount":4,"allReviewDeltasApplied":True,"newSearchPerformed":False,"lampPaddingAdded":False},"admission":{"decision":"admit","oneThingRuling":"one thing: a slave belonging to an estate, used literally as the image behind a cutting epithet","exactUnitIndependentTier1Or2FamiliesAvailable":5},"sense":{"senseKey":None,"preferredTarget":"estate slave","alternateTargets":["farm-and-storehouse slave"],"opening":"A slave belonging to an estate, used as a cutting epithet for someone who serves or guards what should be at his disposal.","body":"Zhaozhou Congshen gives the phrase as a retort in an exchange about avoiding discrimination. Juelang Daosheng turns it against the buddhas in verse. Tian'an Sheng pictures a rich man who guards his holdings yet remains their slave, and Sanyi Mingyu uses the same epithet as an answer and capping line.","note":"田 and 庫 denote the estate's fields and stores; 奴 is the person reduced to serving them. The retained records support the social image and its insulting use without converting it into a doctrinal category.","validation":"multi-source"},"retained":rows,"independentFamilyCount":len(rows),"tierMix":{"tier1":0,"tier2":4,"tier3":0},"lampFallbackRequired":False,"semanticReadComplete":True,"productMutationPerformed":False,"publicMutationPerformed":False,"hardPass":False,"releaseAuthorized":False,"pending":"changed-coordinate independent rereview","elapsedSeconds":time.time()-read(GATE)["startedEpoch"],"writtenUtc":datetime.now(timezone.utc).isoformat()}
exclusive(OUT,out);print(json.dumps({"sha256":sha(OUT),"retained":len(rows),"tier2":4,"tier3":0}))
