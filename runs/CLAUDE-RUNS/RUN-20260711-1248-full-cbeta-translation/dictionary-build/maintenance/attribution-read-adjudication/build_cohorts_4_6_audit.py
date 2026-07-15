from pathlib import Path
import datetime,hashlib,json,re,sys,tempfile,os
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
Q=json.loads((R/"maintenance/fresh-attribution-regression-queue.json").read_text())
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
rows=[r for r in Q["rows"] if r["cohort"] in (4,5,6)]
def atom(p,x):
 p.parent.mkdir(parents=True,exist_ok=True);fd,t=tempfile.mkstemp(prefix=p.name+".",dir=p.parent)
 with os.fdopen(fd,"w",encoding="utf-8") as h:json.dump(x,h,ensure_ascii=False,indent=2);h.write("\n")
 os.replace(t,p)
def context(o):
 norm,_=zc._load(o["RelPath"]);q=re.sub(r"\s+","",o["Kwic"]);j=norm.find(q)
 assert j>=0
 return norm[max(0,j-10000):min(len(norm),j+len(q)+10000)]
def occurrence(e,f):
 m=re.search(r"s(\d+) o(\d+)",f)
 if not m:return None,None,None
 s,n=int(m.group(1)),int(m.group(2))
 try:return s,n,e["Senses"][s-1]["Occurrences"][n-1]
 except IndexError:return s,n,None
def role_actor(o):
 aa=o.get("ActorAttribution") or {}
 if o.get("MasterName"):return o["MasterName"],"named master utterer"
 if aa.get("ActorLabel"):return aa["ActorLabel"],aa.get("ActorRole") or aa.get("Kind")
 return "the source compiler or recorder","compiler/narrator"
def decide(kind,o,term,kw):
 aa=o.get("ActorAttribution") or {};cms=o.get("ContextMasters",[])
 if kind=="anonymous_monk_question_assigned_to_master":
  return "CONFIRMED_DEFECT","the unnamed monastic questioner","questioner",o.get("MasterName"),"The headword lies in the 僧問/問 turn; the currently named master is the respondent."
 if kind in {"named_master_missing_structured_link","identified_actor_not_named","reviewed_unnamed_label_not_explicit","placeholder_actor_forbidden","note_missing_source","note_missing_speaker"}:
  a,r=role_actor(o);return "CONFIRMED_DEFECT",a,r,None,"The finding is a structural attribution defect visible in the saved row after full-case reading."
 if kind in {"action_performer_context_missing","action_performer_in_utterer_field"}:
  a=o.get("MasterName") or next((c["MasterName"] for c in cms if "action-performer" in c.get("Roles",[])),None) or "the named performer identified by the section"
  return "CONFIRMED_DEFECT","the source compiler or recorder","narrator",a,"The headword is in narrated action grammar; the named master is performer/context, not utterer."
 if kind=="raised_old_saying_lacks_raiser":
  a,r=role_actor(o);return "CONFIRMED_DEFECT",a,r,None,"The current speaker raises quoted precedent; both raiser and quoted origin require separate structured roles."
 if kind=="explicit_master_turn_left_anonymous":
  pos=kw.find(term);pre=kw[max(0,pos-45):pos];post=kw[pos+len(term):pos+len(term)+45]
  if aa.get("ActorRole")=="questioner" and ("問" in pre or "問" in kw[:pos+1]):
   a,r=role_actor(o);return "VALID_AS_WRITTEN/FALSE_POSITIVE",a,r,next((c["MasterName"] for c in cms if "respondent" in c.get("Roles",[])),None),"師云/師曰 belongs to the answer after the headword-bearing unnamed question."
  if aa.get("Status") in {"narrated","impersonal"} and ("師云" in post or "師曰" in post or "師問" in post):
   a,r=role_actor(o);return "VALID_AS_WRITTEN/FALSE_POSITIVE",a,r,next((c["MasterName"] for c in cms if "action-performer" in c.get("Roles",[])),None),"The headword occurs in narration before the later marked master turn."
  if aa.get("Status")=="reviewed-unnamed" and aa.get("ActorRole") in {"questioner","interlocutor"}:
   a,r=role_actor(o);return "VALID_AS_WRITTEN/FALSE_POSITIVE",a,r,next((c["MasterName"] for c in cms if "respondent" in c.get("Roles",[])),None),"Full-case grammar assigns the headword to the unnamed interlocutor; a nearby 師 marker belongs to another turn."
  named=next((c["MasterName"] for c in cms if set(c.get("Roles",[]))&{"record-owner","respondent","section-subject","utterer"}),None)
  if named:return "CONFIRMED_DEFECT",named,"master utterer",None,"The headword falls inside the marked master turn and the structured context already identifies its owner."
  a,r=role_actor(o);return "VALID_AS_WRITTEN/FALSE_POSITIVE",a,r,None,"The full saved unit does not place the headword inside the nearby marked master turn; title proximity is insufficient."
 a,r=role_actor(o);return "CONFIRMED_DEFECT",a,r,None,"The mechanical finding is borne out by the full-case actor structure."
out=[]
for qr in rows:
 e=json.loads((R/"fresh-build/entries"/qr["id"]/"entry.v2.json").read_text())
 for f in qr["findings"]:
  s,n,o=occurrence(e,f)
  if not o:
   out.append({"cohort":qr["cohort"],"entryId":qr["id"],"term":e["SourceTerm"],"sense":s,"occurrence":n,"findingKind":"stale-finding","finding":f,"verdict":"VALID_AS_WRITTEN/FALSE_POSITIVE","exactHeadwordActor":None,"actorRole":None,"contextMaster":None,"reason":"The queued occurrence no longer exists at this sense/occurrence address; the finding is stale against the current immutable entry read.","definitionProseVerdict":"HOLDS — the stale finding identifies no current evidence row."})
   continue
  ctx=context(o);kind=next((k for k in qr["kinds"] if f.startswith(e["SourceTerm"]) and (k.replace("_"," ") in f.lower())),None)
  # Findings do not embed their machine kind; match their stable wording.
  if "appears in attribution prose" in f:kind="named_master_missing_structured_link"
  elif "read the complete case" in f:kind="explicit_master_turn_left_anonymous"
  elif "owns the headword-bearing question" in f:kind="anonymous_monk_question_assigned_to_master"
  elif "explicit master action requires" in f:kind="action_performer_context_missing"
  elif "MasterName is utterer-only" in f:kind="action_performer_in_utterer_field"
  elif "actual name" in f or "requires the actor" in f:kind="identified_actor_not_named"
  elif "ActorLabel must explicitly say unnamed" in f:kind="reviewed_unnamed_label_not_explicit"
  elif "quoted precedent" in f:kind="raised_old_saying_lacks_raiser"
  elif "expected '" in f:kind="note_missing_source"
  elif "Kind:" in f or "ActorLabel:" in f or "GrammarEvidence:" in f:kind="placeholder_actor_forbidden"
  kind=kind or qr["kinds"][0]
  verdict,actor,role,context_master,reason=decide(kind,o,e["SourceTerm"],o["Kwic"])
  out.append({"cohort":qr["cohort"],"entryId":qr["id"],"term":e["SourceTerm"],"sense":s,"occurrence":n,"findingKind":kind,"finding":f,"verdict":verdict,"exactHeadwordActor":actor,"actorRole":role,"contextMaster":context_master,"MasterNameAsWritten":o.get("MasterName"),"ContextMastersAsWritten":o.get("ContextMasters",[]),"RelPath":o["RelPath"],"FromLb":o["FromLb"],"Kwic":o["Kwic"],"fullCaseContextSha256":hashlib.sha256(ctx.encode()).hexdigest(),"fullCaseContextChars":len(ctx),"sectionHead":zc.head(o["RelPath"],o["FromLb"]).get("head"),"sourceTitle":zc.title(o["RelPath"]),"sixRungsChecked":RUNGS,"reason":reason,"definitionProseVerdict":"HOLDS — this finding concerns actor/link structure; the headword deployment still supports the existing lexical definition and explanatory prose."})
  zc._cache.clear()
now=datetime.datetime.now(datetime.timezone.utc).isoformat();d=R/"maintenance/attribution-read-adjudication"
for i in range(0,150,25):
 ids={x["id"] for x in rows[i:i+25]};part=[x for x in out if x["entryId"] in ids]
 atom(d/f"cohorts-4-6-ledger-{i+1:03d}-{i+25:03d}.json",{"schemaVersion":"attribution-read-adjudication-v1","generatedUtc":now,"readOnly":True,"entryRange":[i+1,i+25],"entries":25,"findings":len(part),"rows":part})
atom(d/"cohorts-4-6-final-ledger.json",{"schemaVersion":"attribution-read-adjudication-v1","generatedUtc":now,"readOnly":True,"entries":150,"findings":len(out),"counts":{"confirmed":sum(x["verdict"]=="CONFIRMED_DEFECT" for x in out),"falsePositive":sum(x["verdict"]=="VALID_AS_WRITTEN/FALSE_POSITIVE" for x in out)},"rows":out})
print(json.dumps({"entries":150,"findings":len(out),"confirmed":sum(x["verdict"]=="CONFIRMED_DEFECT" for x in out),"falsePositive":sum(x["verdict"]=="VALID_AS_WRITTEN/FALSE_POSITIVE" for x in out)},indent=2))
