#!/usr/bin/env python3
"""Assemble the exact reviewed R94 30-entry construction authority."""
from __future__ import annotations
import hashlib,json,os
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];M=ROOT/"maintenance"
paths={
 "selection":M/"non-iriya-v7-depth-regeneration-r94-selection-root.json",
 "r93Union":M/"non-iriya-v7-depth-regeneration-r93-resolved-union-root.json",
 "laneAAuthority":M/"r94-lane-a-correction1-closure.json",
 "laneAReview":M/"r94-lane-a-correction1-rereview-by-c.json",
 "laneBBaseAuthority":M/"r94-lane-b-correction2-closure.json",
 "laneBOverlayAuthority":M/"r94-lane-b-correction3-closure.json",
 "laneBReview":M/"r94-lane-b-correction3-rereview-by-a.json",
 "laneCBaseAuthority":M/"r94-lane-c-correction1-authority.json",
 "laneCOverlayAuthority":M/"r94-lane-c-correction2-authority.json",
 "laneCClosure":M/"r94-lane-c-correction2-closure.json",
 "laneCReview":M/"r94-lane-c-correction2-rereview-by-b.json",
 "failedWuziReceipt":M/"r94-t_2738431562e6-frozen-exhaustion-receipt-root.json",
 "failedJieReceipt":M/"r94-t_292ac4c33b4f-frozen-exhaustion-receipt-root.json",
 "replacementSelection":M/"non-iriya-v7-depth-regeneration-r94-replacement2-selection-root.json",
 "replacementPreauthorGate":M/"non-iriya-v7-depth-regeneration-r94-replacement2-exact-unit-preauthor-gate-root.json",
 "replacementAuthority":M/"r94-replacement2-correction1-closure-root.json",
 "replacementReview":M/"r94-replacement2-correction1-rereview-by-c.json",
}
expected={
 "laneAAuthority":"7252e542c6bc7cfcd93681626841ba2136c093bc338910aa0f4850dfca60fb10",
 "laneAReview":"78fa732d4af33dfac77cac5569e62d2d4482a7ea2f2146ea63eb3517387e8118",
 "laneBBaseAuthority":"9be2b115f7878710b8e771ed3151797dbf3a70e4608b411b1e5f3fee1f4df8bc",
 "laneBOverlayAuthority":"9e95911155358daa1da98604649b98c59abcfda8107720557805125d21678e3b",
 "laneBReview":"4262281691d16c111f877f21816969f7d8dc349fcbc9d47f50978ad3c51a0487",
 "laneCBaseAuthority":"a80f2d37b4d5f0ceb11df429c6d9db348791ba56520fd0157ddb7a267bca56e0",
 "laneCOverlayAuthority":"00a08a9f13e242a9c1b2234bfa582b34b8f2d5df172759f722528d21a08956d0",
 "laneCClosure":"fe08d527e8b27ca433b8f9c665ad0061082bf75c1a06300fdd2854af7fd8907a",
 "laneCReview":"9f5a91b28cbf5642a61b89b629db86d38b248514665f094e141a3010ac3bb39d",
 "failedWuziReceipt":"0e0543077db5621b8b3451ffeb70c91d0e9c8099990d5c783380874629bf4a8e",
 "failedJieReceipt":"2c47655c43b20b4947c79e119b9c4ff645816aab8f41eb5535298c4201e384cd",
 "replacementSelection":"e143c445ea7613191144c9f1f8d46ed785ab7325d222646b12eb60a292e8dac6",
 "replacementPreauthorGate":"634b96229c59c25dc857e47ea643e0eb867ffd6c88754d5a6dd61d24a102ccc2",
 "replacementAuthority":"bbaffb0b78023ea26acc3c0cfc3c69150dab4e3898f39747e487631086d35148",
 "replacementReview":"560e942eaa6494a57d7595273950b3de721928edf0056ecbc3854a5eee38e658",
}
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(Path(p).read_text(encoding="utf-8-sig"))
for k,v in expected.items():assert sha(paths[k])==v,(k,sha(paths[k]),v)
assert read(paths["laneAReview"])["hardPass"] is True
assert read(paths["laneBBaseAuthority"])["hardPass"] is True
assert read(paths["laneBOverlayAuthority"])["hardPass"] is True
assert read(paths["laneBReview"])["hardPass"] is True
assert read(paths["replacementPreauthorGate"])["hardPass"] is True
assert read(paths["replacementReview"])["hardPass"] is True
original=read(paths["selection"]); original_ids=original["selectedIds"]; failed="t_2738431562e6"; replacement=read(paths["replacementSelection"])["selected"]["identityId"]; assert replacement=="t_296bc68f6903"
a=read(paths["laneAAuthority"]);a_ids=[e["id"] for e in a["entries"]]
b=read(paths["laneBBaseAuthority"]);b_ids=[e["id"] for e in b["rows"]]
cbase=read(paths["laneCBaseAuthority"]);c_ids=[e["id"] for e in cbase["entries"] if e["status"]!="failed"]
final_ids=a_ids+b_ids+c_ids+[replacement]
assert len(a_ids)==10 and len(b_ids)==10 and len(c_ids)==9 and len(final_ids)==30 and len(set(final_ids))==30
assert set(final_ids)==(set(original_ids)-{failed})|{replacement}
assert failed not in final_ids and "t_292ac4c33b4f" not in final_ids
resolved=set(read(paths["r93Union"])["ids"]);assert not resolved.intersection(final_ids)
# Tier-3 and floors from each final authority.
assert a["summary"]["tier3"]==0 and all(e["independentProofFamilies"]>=3 for e in a["entries"])
assert b["tier3Retained"]==0 and b["governedFloor"]==3
assert all(len(e["retainedRows"])>=3 for e in cbase["entries"] if e["status"]!="failed")
rep=read(paths["replacementAuthority"]);assert rep["tierMix"]["tier3"]==0 and rep["independentFamilyCount"]>=3
bindings={k:{"path":str(p.relative_to(ROOT)),"sha256":sha(p)} for k,p in paths.items()}
out={"schemaVersion":"r94-final30-authority-review-manifest.v1","cohort":"R94","scope":{"entryCount":30,"originalValidCount":29,"replacementCount":1,"failedOriginalId":failed,"failedReplacementAttemptId":"t_292ac4c33b4f","finalIds":final_ids},"partitions":{"laneA":a_ids,"laneB":b_ids,"laneCValid":c_ids,"replacement2":[replacement]},"bindings":bindings,"reviewRulings":{"laneA":"PASS","laneB":"PASS","laneCValidNine":"PASS; failed 無字 excluded and immutably receipted","replacement2":"PASS"},"sourcePolicy":{"tierPriority":["Tier 1 authored","Tier 2 recorded sayings","Tier 3 lamps"],"tier3Retained":0,"lampPadding":False,"minimumIndependentFamilies":3,"exactUnitPreauthorGateAppliedToReplacement2":True},"collisionAudit":{"r93ResolvedCollisionCount":0,"withinFinalDuplicateCount":0,"failedIdsIncludedCount":0},"authorityReadyForConstruction":True,"publicMutationPerformed":False,"releaseAuthorized":False,"hardPass":True,"writtenUtc":datetime.now(timezone.utc).isoformat()}
target=M/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json";data=(json.dumps(out,ensure_ascii=False,indent=2)+"\n").encode();fd=os.open(target,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o644)
try:os.write(fd,data);os.fsync(fd)
finally:os.close(fd)
print(json.dumps({"path":str(target),"sha256":sha(target),"entryCount":30,"hardPass":True}))
