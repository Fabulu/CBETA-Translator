import datetime, hashlib, json, sys
from pathlib import Path

root=Path(__file__).resolve().parents[2]; sys.path.insert(0,str(root))
import audit_attribution
now="2026-07-15T23:30:00Z"; roster=audit_attribution.roster_names()
ledger=json.loads((root/"maintenance/attribution-read-adjudication/cohorts-7-9-ledger-006-015.json").read_text())
ids=[]
for row in ledger["rows"]:
 if row["verdict"].startswith("CONFIRMED_DEFECT") and row["adjudicatedRole"] not in {"mixed-owner witness"}:
  ids.append(row["entryId"])
for eid in sorted(set(ids)):
 p=root/f"fresh-build/entries/{eid}/entry.v2.json"; e=json.loads(p.read_text()); rows=[r for r in ledger["rows"] if r["entryId"]==eid and r["verdict"].startswith("CONFIRMED_DEFECT")]
 for r in rows:
  if r["adjudicatedRole"]=="mixed-owner witness": continue
  o=e["Senses"][r["sense"]-1]["Occurrences"][r["occurrence"]-1]; actor=r["adjudicatedActor"]; role=r["adjudicatedRole"]
  o["AttributionNote"]=f'{r["sourceTitle"]}: {r["decisionAuthored"]}'
  if actor in roster and role in {"utterer","later-quoter","cited-verse-author"}:
   o["MasterName"]=actor; o.pop("ActorAttribution",None); o["ContextMasters"]=[{"MasterName":actor,"Roles":["utterer" if role in {"utterer","cited-verse-author"} else role]}]
  elif actor in roster:
   o.pop("MasterName",None); o["ActorAttribution"]={"Status":"narrated","Kind":"narrated physical action","ActorLabel":actor,"ActorRole":role,"ReviewedBy":"Codex manual full-case cohorts 7-9","ReviewedUtc":now,"GrammarEvidence":r["decisionAuthored"]}; o["ContextMasters"]=[{"MasterName":actor,"Roles":[role]}]
  elif actor.startswith("an unnamed"):
   o.pop("MasterName",None); o["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"unnamed participant","ActorLabel":actor,"ActorRole":role,"ReviewedBy":"Codex manual full-case cohorts 7-9","ReviewedUtc":now,"GrammarEvidence":r["decisionAuthored"],"SixRungsChecked":["line","context-500","context-2000","context-10000","section-heading","book-title","tei-header","parallel-text"]}; o["ContextMasters"]=[]
  elif role=="compiler":
   o.pop("MasterName",None); o["ActorAttribution"]={"Status":"impersonal","Kind":"editorial heading","ActorLabel":actor,"ActorRole":"compiler","ReviewedBy":"Codex manual full-case cohorts 7-9","ReviewedUtc":now,"GrammarEvidence":r["decisionAuthored"]}; o["ContextMasters"]=[]
  else:
   o.pop("MasterName",None); o["ActorAttribution"]={"Status":"identified-non-master","Kind":"named or collective participant","ActorLabel":actor,"ActorRole":role,"ReviewedBy":"Codex manual full-case cohorts 7-9","ReviewedUtc":now,"GrammarEvidence":r["decisionAuthored"]}; o["ContextMasters"]=[]
 p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+"\n")
out=[]
for eid in sorted(set(ids)):
 p=root/f"fresh-build/entries/{eid}/entry.v2.json"; out.append({"entryId":eid,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()})
(root/"maintenance/attribution-read-adjudication/cohorts-7-9-repair-006-015.json").write_text(json.dumps({"generatedUtc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"entries":out},indent=2)+"\n")
print(json.dumps(out,indent=2))
