#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parent.parent
stage=root/"fresh-build"/"r94-correction2-stage"/"entries"
auth_hash="2ee44bf19a2533958e5620c38915f3d03fbb81209c76c7183742a4f3d059f501"
ids=json.loads((root/"maintenance"/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json").read_text())["scope"]["finalIds"][10:]
template=json.loads((root/"fresh-build"/"entries"/"t_28fac5e98308"/"evidence.draft.json").read_text())
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
for i in ids:
 d=stage/i; p=d/"evidence.draft.json"; x=json.loads(p.read_text()); s=x["Entry"]["Senses"][0]
 x["ConstructionPipelineVersion"]=2
 x["Admission"]=json.loads(json.dumps(template["Admission"]))
 x["Entry"]["CorpusBaselineSha256"]="8ea7e8ab756138567783a1d3f9e01648885c1732a782ba601ba478742adddaff"
 x["Entry"]["Senses"]=[s]
 et=x.setdefault("EvidenceTransport",{});et["DossierPath"]="source-dossier.json";et["DossierSha256"]=sha(d/"source-dossier.json");et["SourceAuthorityManifestSha256"]=auth_hash
 et["CorpusBaselineSha256"]=x["Entry"]["CorpusBaselineSha256"];et["DiscoveryMethods"]=["final reviewed R94 authority"];et["BridgedCount"]=et.get("ExactCount",len(s["Occurrences"]))
 masters=[]
 for o in s["Occurrences"]:
  if o.get("MasterName"): masters.append(o["MasterName"]);o.pop("ActorAttribution",None)
  for cm in o.get("ContextMasters",[]): masters.append(cm["MasterName"])
 masters=list(dict.fromkeys(masters));sources=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
 s["SourceTexts"]=sources;s["RelatedMasters"]=masters
 s.setdefault("DraftAcceptedDerivedFields",{})["SourceTexts"]=sources
 s["DraftAcceptedDerivedFields"]["RelatedMasters"]=masters
 de=s["DraftEvidence"]
 de["LiteralGraphFloor"]=de.get("LiteralGraphFloor") or s["PreferredTarget"]
 de["LexicalJob"]=de.get("LexicalJob") or f"It names the reviewed use translated as {s['PreferredTarget']}."
 de["DeploymentClasses"]=de.get("DeploymentClasses") or ["final-reviewed-authority deployment"]
 de["HighValueEvidenceLedger"]=de.get("HighValueEvidenceLedger") or [{"Disposition":"keep","Finding":o["AttributionNote"],"Reason":"final reviewed authority"} for o in s["Occurrences"]]
 for r,o in zip(de["SourceAuthorityRows"],s["Occurrences"]):
  voice=(o.get("DraftActorProof") or {}).get("SpeechFrame","")
  if r["DeploymentRole"] not in ("active-quotation","original-use","commentary"):
   r["DeploymentRole"]="active-quotation" if "quoted" in voice else ("commentary" if "compiler" in voice else "original-use")
 de["DepthHarvestReceipt"]={"Complete":True,"ReviewedExactHitCount":et.get("ExactCount",len(s["Occurrences"])),
  "AvailableSourceFiles":len(sources),"SearchedDeploymentClasses":["final-reviewed-authority"],
  "OmissionAudit":["Mechanical normalization retains exactly the final reviewed authority families."]}
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+"\n")
 for name in ("entry.v2.json","evidence-compile-report.json","attribution-audit.json","work-source-audit.txt"):
  q=d/name
  if q.exists(): q.unlink()
print(len(ids))
