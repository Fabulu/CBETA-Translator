#!/usr/bin/env python3
"""Mint the hash-bound authorization for contiguous Iriya release 311–360."""
from __future__ import annotations
import hashlib, json
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parent; M=ROOT/"maintenance"; F=ROOT/"fresh-build/entries"
def load(p): return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write(p,v):
    p=Path(p); t=p.with_suffix(p.suffix+".tmp"); t.write_text(json.dumps(v,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); t.replace(p)
def passish(d): return d.get("hardPass") is True and not (d.get("residuals") or [])

def main():
    selection=(("a",101,110),("b",111,130),("c",101,120)); ids=[]
    for lane,lo,hi in selection:
        ids += [r["id"] for r in load(M/f"iriya-construction-001-lane-{lane}.json")["rows"] if lo<=int(r["lanePosition"])<=hi]
    if len(ids)!=50 or len(set(ids))!=50: raise SystemExit(f"bad selection {len(ids)}/{len(set(ids))}")
    gate_path=M/"iriya-release50-311-360-full-cohort-gate.json"; gate=load(gate_path)
    required={"exact failures":gate["exactKwic"]["failureCount"],"attribution failures":gate["attribution"]["exitCode"],
              "worksheet failures":gate["worksheetRoundtrip"]["failureCount"],"template gate exit":gate["batchSemanticTemplates"]["exitCode"],
              "forbidden-English failures":len(gate["forbiddenEnglish"]),"source/work gate exit":gate["workSourceValidation"]["exitCode"],
              "depth/sense gate exit":gate["depthSense"]["exitCode"],"lineage-roster gate exit":gate["lineageRosterUntouched"]["exitCode"]}
    if any(required.values()) or len(gate.get("entries") or [])!=50: raise SystemExit(f"unclean gate {required}")
    names=[
      "iriya-author-A101-110-independent-full-review-final.json",
      "iriya-B111-120-ten-alias-independent-rereview.json",
      "iriya-B121-130-independent-changed-only-rereview-final.json",
      "iriya-C101-110-C103-o7-independent-changed-rereview.json",
      "iriya-C111-120-independent-changed-only-rereview-receipt.json",
    ]
    reviews=[]
    for n in names:
        p=M/n; d=load(p)
        if not passish(d): raise SystemExit(f"review not residual-free hard pass: {n}")
        reviews.append({"path":f"maintenance/{n}","sha256":sha(p)})
    hashes=[]
    for i in ids:
        paths={"entry":F/i/"entry.v2.json","worksheet":F/i/"evidence.draft.json","work":F/i/"WORK.md"}
        if not all(p.is_file() for p in paths.values()): raise SystemExit(f"incomplete {i}")
        hashes.append({"id":i,"entrySha256":sha(paths["entry"]),"worksheetSha256":sha(paths["worksheet"]),"workSha256":sha(paths["work"])})
    now=datetime.now(timezone.utc).isoformat(); auth_path=M/"iriya-release50-311-360-final-authorization.json"
    auth={"schemaVersion":"hash-bound-dictionary-release-authorization-v1","generatedUtc":now,"releaseAuthorization":True,"hardPass":True,
          "cohort":"Iriya construction batch 1 contiguous release 311–360","selection":{"A":"101-110","B":"111-130","C":"101-120","entries":50},
          "currentFullCohortGate":{"path":f"maintenance/{gate_path.name}","sha256":sha(gate_path)},"mechanicalGate":required,
          "semanticReviewResolution":{"reviewRequiredSignal":bool(gate.get("semanticReviewRequired")),"resolvedByIndependentFullCaseReceipts":reviews,"openResiduals":0},
          "entryHashes":hashes,"publicDeploymentAuthorized":False,"lineageRosterMutationAuthorized":False}
    write(auth_path,auth); manifest_path=M/"iriya-release50-311-360-install-manifest.json"
    manifest={"schemaVersion":"hash-bound-dictionary-install-manifest-v1","generatedUtc":now,"installAuthorized":True,"cohort":auth["cohort"],
              "entries":hashes,"closureReceipts":[{"path":f"maintenance/{auth_path.name}","sha256":sha(auth_path)}],"publicDeploymentAuthorized":False}
    write(manifest_path,manifest)
    print(json.dumps({"hardPass":True,"entries":len(hashes),"authorization":str(auth_path.relative_to(ROOT)),"authorizationSha256":sha(auth_path),
                      "manifest":str(manifest_path.relative_to(ROOT)),"manifestSha256":sha(manifest_path)}))
    return 0
if __name__=="__main__": raise SystemExit(main())
