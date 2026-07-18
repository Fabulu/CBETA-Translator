#!/usr/bin/env python3
"""Mint authorization/manifest from a declarative bounded-release spec."""
from __future__ import annotations
import argparse, hashlib, json
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parent; M=ROOT/"maintenance"; F=ROOT/"fresh-build/entries"
def load(p): return json.loads(Path(p).read_text(encoding="utf-8-sig"))
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write(p,v):
    p=Path(p); t=p.with_suffix(p.suffix+".tmp"); t.write_text(json.dumps(v,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); t.replace(p)

def main(argv=None):
    ap=argparse.ArgumentParser(); ap.add_argument("spec",type=Path); args=ap.parse_args(argv)
    spec_path=args.spec.resolve(); spec=load(spec_path); ids=[]; selection_label={}
    for row in spec["selection"]:
        lane=str(row["lane"]).lower(); lo,hi=int(row["low"]),int(row["high"])
        selection_label[lane.upper()]=f"{lo}-{hi}"
        ids += [r["id"] for r in load(M/f"iriya-construction-001-lane-{lane}.json")["rows"] if lo<=int(r["lanePosition"])<=hi]
    expected=int(spec["entries"])
    if len(ids)!=expected or len(set(ids))!=expected: raise SystemExit(f"bad selection {len(ids)}/{len(set(ids))}, expected {expected}")
    gate_path=M/spec["gate"]; gate=load(gate_path)
    required={"exact failures":gate["exactKwic"]["failureCount"],"attribution failures":gate["attribution"]["exitCode"],
              "worksheet failures":gate["worksheetRoundtrip"]["failureCount"],"template gate exit":gate["batchSemanticTemplates"]["exitCode"],
              "forbidden-English failures":len(gate["forbiddenEnglish"]),"source/work gate exit":gate["workSourceValidation"]["exitCode"],
              "depth/sense gate exit":gate["depthSense"]["exitCode"],"lineage-roster gate exit":gate["lineageRosterUntouched"]["exitCode"]}
    if any(required.values()) or len(gate.get("entries") or [])!=expected: raise SystemExit(f"unclean gate {required}")
    reviews=[]
    for name in spec["reviews"]:
        p=M/name; d=load(p)
        disposition=d.get("disposition") or {}
        review_hard_pass=d.get("hardPass") is True or disposition.get("hardPass") is True
        residuals=d.get("residuals") or []
        residual_count=disposition.get("residualCount", 0)
        if not review_hard_pass or residuals or residual_count:
            raise SystemExit(f"review not residual-free hard pass: {name}")
        reviews.append({"path":f"maintenance/{name}","sha256":sha(p)})
    hashes=[]
    for i in ids:
        paths={"entry":F/i/"entry.v2.json","worksheet":F/i/"evidence.draft.json","work":F/i/"WORK.md"}
        if not all(p.is_file() for p in paths.values()): raise SystemExit(f"incomplete {i}")
        hashes.append({"id":i,"entrySha256":sha(paths["entry"]),"worksheetSha256":sha(paths["worksheet"]),"workSha256":sha(paths["work"])})
    now=datetime.now(timezone.utc).isoformat(); auth_path=M/spec["authorizationOutput"]; manifest_path=M/spec["manifestOutput"]
    selection_label["entries"]=expected
    auth={"schemaVersion":"hash-bound-dictionary-release-authorization-v1","generatedUtc":now,"releaseAuthorization":True,"hardPass":True,
          "cohort":spec["cohort"],"selection":selection_label,"currentFullCohortGate":{"path":f"maintenance/{gate_path.name}","sha256":sha(gate_path)},
          "mechanicalGate":required,"semanticReviewResolution":{"reviewRequiredSignal":bool(gate.get("semanticReviewRequired")),
          "resolvedByIndependentFullCaseReceipts":reviews,"openResiduals":0},"entryHashes":hashes,
          "publicDeploymentAuthorized":False,"lineageRosterMutationAuthorized":False,"releaseSpec":{"path":str(spec_path.relative_to(ROOT)),"sha256":sha(spec_path)}}
    write(auth_path,auth)
    manifest={"schemaVersion":"hash-bound-dictionary-install-manifest-v1","generatedUtc":now,"installAuthorized":True,"cohort":auth["cohort"],
              "entries":hashes,"closureReceipts":[{"path":f"maintenance/{auth_path.name}","sha256":sha(auth_path)}],"publicDeploymentAuthorized":False}
    write(manifest_path,manifest)
    print(json.dumps({"hardPass":True,"entries":len(hashes),"authorization":str(auth_path.relative_to(ROOT)),"authorizationSha256":sha(auth_path),
                      "manifest":str(manifest_path.relative_to(ROOT)),"manifestSha256":sha(manifest_path)})); return 0
if __name__=="__main__": raise SystemExit(main())
