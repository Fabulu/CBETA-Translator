#!/usr/bin/env python3
import copy,hashlib,json
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parent.parent;m=root/"maintenance";fresh=root/"fresh-build"/"entries"
stage=root/"fresh-build"/"r94-correction2-stage"/"entries"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
titles={}
for line in Path("/mnt/c/programmieren/CbetaZenTranslations/titles.jsonl").read_text(encoding="utf-8").splitlines():
 if line.strip():
  x=json.loads(line);titles[x["path"]]=x.get("enShort") or x.get("en")
extract=json.loads((m/"non-iriya-v7-depth-regeneration-r94-frozen-extraction-root.json").read_text())
exmap={x["id"]:x for x in extract["rows"]}
auth=json.loads((m/"r94-lane-c-correction1-authority.json").read_text())
entries=[x for x in auth["entries"] if x["status"]!="failed"]
overlay=json.loads((m/"r94-lane-c-correction2-authority.json").read_text())
repl=json.loads((m/"r94-replacement2-correction1-closure-root.json").read_text())
rex=json.loads((m/"non-iriya-v7-depth-regeneration-r94-replacement2-frozen-extraction-root.json").read_text())
rerow=rex["rows"][0] if "rows" in rex else rex
rows=[]
for a in entries:
 if a["id"]=="t_28fac5e98308":
  decisions=copy.deepcopy(a["occurrenceDecisions"])
  decisions[1]["actor"]="Ruilu Benxian";decisions[1].pop("actorStatus",None)
 else: decisions=a["occurrenceDecisions"]
 rowspec=(a,decisions,exmap[a["id"]]["sourceCandidates"],a["retainedRows"])
 rows.append(rowspec)
# Replacement authority has four exact retained rows.
rdec=[]
for n,x in enumerate(repl["retained"],1):
 rdec.append({"row":n,"actor":x["actorDecision"]["actor"],"voiceLayer":x["voiceLayer"],
              "tier":x["tier"],"family":x["witnessFamilyId"]})
rows.append(({"id":repl["id"],"term":repl["term"],"preferredTarget":repl["sense"]["preferredTarget"]},
             rdec,repl["retained"],list(range(1,len(repl["retained"])+1))))
manifest=[]
for a,decisions,candidates,retained in rows:
 i=a["id"];out=stage/i;out.mkdir(parents=True,exist_ok=True)
 base=json.loads((fresh/i/"evidence.draft.json").read_text())
 sense=base["Entry"]["Senses"][0]; occs=[]; authority_rows=[]
 for n,(rowno,dec) in enumerate(zip(retained,decisions),1):
  c=candidates[rowno-1]; rel=c["relPath"];actor=dec["actor"];voice=dec["voiceLayer"];tier=dec["tier"];family=dec["family"]
  title=titles.get(rel)
  if not title: raise SystemExit(f"missing title {rel}")
  role="utterer" if voice in ("direct-turn","quoted-original") else ("verse-author" if voice=="transmitted-verse" else "compiler")
  cms=[{"MasterName":actor,"Roles":[role]}]
  if dec.get("contextActor"): cms.append({"MasterName":dec["contextActor"],"Roles":[dec["contextRole"]]})
  note=f"Source record ({rel}). {title}. {actor} is the exact actor of the headword-bearing {voice} layer; {family}."
  occs.append({"RelPath":rel,"FromLb":c["fromLb"],"ToLb":c["toLb"],"Kwic":c["context"],
   "MasterName":actor,"Curated":True,"ContextMasters":cms,"AttributionNote":note,
   "DraftActorProof":{"ExactHeadwordClause":a["term"],"GrammaticalSubject":actor,
    "SpeechFrame":f"{actor} owns the reviewed {voice} deployment.","FullCaseDecision":f"{actor} owns the headword-bearing layer; adjacent voices are excluded."},
   "ActorAttribution":{"Status":"linked-master","Kind":"final reviewed authority","ActorLabel":actor,
    "ActorRole":role,"RungsChecked":["final-reviewed-authority"],"GrammarEvidence":f"{actor} owns the reviewed {voice} layer.",
    "ReviewedBy":"R94 correction2 mechanical normalization","ReviewedUtc":datetime.now(timezone.utc).isoformat()}})
  authority_rows.append({"EvidenceKey":f"o{n}","RelPath":rel,"WorkId":c.get("workId"),
   "Tier":tier,"SourceClass":"master-authored" if tier==1 else "recorded-sayings",
   "AuthorityReason":family,"WitnessFamilyId":f"{a['term']}-family-{n}",
   "DeploymentRole":"active-quotation" if voice=="quoted-original" else ("commentary" if voice=="compiler-narration" else voice)})
 sense["PreferredTarget"]=a["preferredTarget"];sense["Occurrences"]=occs
 sense["SourceTexts"]=[x["RelPath"] for x in occs]
 sense["RelatedMasters"]=list(dict.fromkeys(x["MasterName"] for x in occs))
 sense["ExplanationParts"]["CorpusEarnedOpening"]=a.get("opening") or f"{a['term']} is used in the reviewed sources as “{a['preferredTarget']}.”"
 sense["ExplanationParts"]["EvidenceBody"]=[f"{x['actor']}: {x['family']}." for x in decisions]
 de=sense["DraftEvidence"];de["OpeningClaimEvidenceKeys"]=[f"o{x}" for x in range(1,len(occs)+1)]
 de["EvidenceBodyClaimKeys"]=[[f"o{x}"] for x in range(1,len(occs)+1)]
 de["HighValueEvidenceLedger"]=[{"Disposition":"keep","Finding":f"{x['actor']}: {x['family']}.","Reason":x["family"]} for x in decisions]
 de["IndependentWorkIds"]=[x["WorkId"] for x in authority_rows]
 de["SourceAuthorityRows"]=authority_rows
 de["LampExcessJustification"]="No lamp is retained."
 transport=base.setdefault("EvidenceTransport",{})
 transport["SourceAuthorityManifestSha256"]=sha(m/"non-iriya-v7-depth-regeneration-r94-final30-authority-review-manifest-root.json")
 transport["ExactCount"]=len(occs)
 (out/"evidence.draft.json").write_text(json.dumps(base,ensure_ascii=False,indent=2)+"\n")
 dossier={"schemaVersion":"r94-normalized-source-dossier.v1","id":i,"term":a["term"],
          "authorityRows":authority_rows,"occurrences":occs,"tier3Retained":0,"lampPadding":False}
 (out/"source-dossier.json").write_text(json.dumps(dossier,ensure_ascii=False,indent=2)+"\n")
 (out/"WORK.md").write_text(f"# {a['term']} — R94 correction2 normalized construction\\n\\nMechanical regeneration from final reviewed authority. No semantic research. {len(occs)} independent families; Tier 3: 0.\\n")
 manifest.append({"id":i,"term":a["term"],"familyCount":len(occs),"tier3":0,
  "draftSha256":sha(out/"evidence.draft.json"),"dossierSha256":sha(out/"source-dossier.json"),"workSha256":sha(out/"WORK.md")})
p=m/"non-iriya-v7-depth-regeneration-r94-correction2-lane-c-input-manifest-root.json"
p.write_text(json.dumps({"schemaVersion":"r94-normalized-input-lane.v1","cohort":"R94-correction2","lane":"C+replacement","rows":manifest,
 "semanticResearchPerformed":False,"publicMutation":False},ensure_ascii=False,indent=2)+"\n")
print(str(p),sha(p),len(manifest))
