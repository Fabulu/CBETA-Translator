"""Apply the explicit human-read repairs from checkpoint 036-045 only."""
from __future__ import annotations
import datetime, hashlib, json
from pathlib import Path

HERE=Path(__file__).resolve().parent; BUILD=HERE.parents[1]; ENTRIES=BUILD/"fresh-build"/"entries"
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
changed=[]
def contexts(*xs): return [{"MasterName":n,"Roles":r} for n,r in xs]
def get(tid):
 p=ENTRIES/tid/"entry.v2.json"; return p,json.loads(p.read_text(encoding="utf-8"))
def o(e,n): return e["Senses"][0]["Occurrences"][n-1]
def note(source,actor): return f"Source text ({source}). Exact headword actor: {actor}. The complete case and its turn boundaries were read before attribution."
def narrated(x,source,label,cms,evidence):
 x.pop("MasterName",None);x["ActorAttribution"]={"Status":"narrated","Kind":"compiler narrative","ActorLabel":label,"ActorRole":"compiler","RungsChecked":RUNGS,"GrammarEvidence":evidence,"ReviewedBy":"Codex real-read cohorts 4-6","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True};x["ContextMasters"]=cms;x["AttributionNote"]=note(source,label)
def questioner(x,source,label,cms,evidence):
 x.pop("MasterName",None);x["ActorAttribution"]={"Status":"reviewed-unnamed","Kind":"monastic questioner","ActorLabel":label,"ActorRole":"questioner","RungsChecked":RUNGS,"GrammarEvidence":evidence,"ReviewedBy":"Codex real-read cohorts 4-6","ReviewedUtc":NOW,"AuthoredVoiceRiskReviewed":True};x["ContextMasters"]=cms;x["AttributionNote"]=note(source,label)
def master(x,source,name,roles,cms,evidence):
 x["MasterName"]=name;x.pop("ActorAttribution",None);x["ContextMasters"]=contexts((name,roles),*cms);x["AttributionNote"]=note(source,name)
def edit(tid,fn):
 p,e=get(tid);before=hashlib.sha256(p.read_bytes()).hexdigest();fn(e);p.write_text(json.dumps(e,ensure_ascii=False,indent=2)+"\n",encoding="utf-8");changed.append({"entryId":tid,"term":e["SourceTerm"],"beforeSha256":before,"afterSha256":hashlib.sha256(p.read_bytes()).hexdigest()})

edit("t_ccc39a4559bf",lambda e:narrated(o(e,2),"五燈會元","the compiler supplying the monastic-rector speaker label",contexts(("Fuchuan Hongjian",["addressee","section-subject"])),"The monastic-rector title is a recorder-supplied speaker label; the official's quoted words begin after the speech marker."))
edit("t_cd69e0f9c10a",lambda e:questioner(o(e,1),"五燈全書(第34卷-第120卷)","an unnamed monastic questioner",contexts(("Fengqi Zhongqing",["respondent","action-performer","section-subject"])),"The monk's question contains the headword; Fengqi Zhongqing responds afterward by opening his mouth and showing his tongue."))
edit("t_d1ca36839312",lambda e:master(o(e,6),"雲峨喜禪師語錄","Yun'e Xi",["utterer","record-owner"],(),"The explicit master-speech turn contains the headword throughout Yun'e Xi's address."))
edit("t_d1e06fd225fa",lambda e:narrated(o(e,6),"禪宗頌古聯珠通集","the compiler narrating an unnamed visitor's entry",contexts(("Ciming Chuyuan",["respondent","case-figure"])),"The entry into the room is narrated; Ciming Chuyuan's quoted warning starts afterward."))
edit("t_d2c3f40d45c6",lambda e:questioner(o(e,5),"古尊宿語錄","an unnamed monastic questioner",contexts(("Baizhang Huaihai",["respondent","record-owner"])),"The question contains the named Buddha; Baizhang Huaihai's explanation begins in the following master turn."))
edit("t_d4673502b2d2",lambda e:questioner(o(e,2),"古尊宿語錄","an unnamed monastic questioner",contexts(("Foyan Qingyuan",["respondent","record-owner"])),"The monk's question contains the polite interrogative; Foyan Qingyuan answers afterward."))
edit("t_dd5f8d8801d2",lambda e:master(o(e,5),"禪宗頌古聯珠通集","Yanguan Qi'an",["utterer","questioner","case-figure"],(("Touzi Datong",["later-raiser"]),),"Both headword tokens in the bounded case are directly introduced as Yanguan Qi'an's speech; Touzi speaks only the later substitute line."))

ledger={"schemaVersion":"attribution-real-read-repair-v1","generatedUtc":NOW,"scope":"cohorts 4-6 real-read entries 036-045","promoted":False,"merged":False,"changedEntries":len(changed),"entries":changed}
(HERE/"cohorts-4-6-real-read-repair-036-045-ledger.json").write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print(json.dumps({"changed":len(changed)},indent=2))
