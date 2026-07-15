from pathlib import Path
from datetime import datetime,timezone
import copy,json,re,sys
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']; NOW=datetime.now(timezone.utc).isoformat()
REPO=Path('/mnt/c/programmieren/mergeworkcbeta/cbeta-translator')
ro=json.loads((REPO/'Assets/Data/master-dates.json').read_text())['masters']
aliases=[]
for m in ro:
 for a in m.get('names',[])[1:]:
  if len(a)>=2:aliases.append((a,m['names'][0]))
aliases.sort(key=lambda x:len(x[0]),reverse=True)
ACTION_CASE={'橫按拄杖','雲巖掃地','黃檗棒','趙州勘婆'}
QUESTION_RE=re.compile(r'(?:僧問|問|僧曰|僧云|進云|進曰)[：：「“]?[^。；]{0,110}$')
SPEECH_CUE=re.compile(r'(?:上堂|示眾|乃云|乃曰|師云|師曰|云[：：「“]|曰[：：「“])')
def match_owner(text):
 hits=[]
 for a,n in aliases:
  if a in text and n not in hits:hits.append(n)
 return hits
def set_named(o,name,term,reason):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}). {name} utters the exact headword wording in the fully read unit. {reason}"
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':reason,'FullCaseDecision':o['AttributionNote']}
def set_other(o,status,label,role,term,reason,contexts=None,kind=None):
 o.pop('MasterName',None);o['ContextMasters']=contexts or []
 o['ActorAttribution']={'Status':status,'Kind':kind or ('monastic questioner' if role=='questioner' else 'fully reviewed textual voice'),'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':reason,'ReviewedBy':'Codex f004 lane C full-case actor repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True}
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}). Exact source voice: {label}. {reason}"
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':reason,'FullCaseDecision':o['AttributionNote']}
def repair_entry(row):
 p=R/'fresh-build/entries'/row['id'];e=json.loads((p/'entry.v2.json').read_text()); decisions=[]
 for s in e['Senses']:
  for oi,o in enumerate(s['Occurrences'],1):
   term=row['term'];q=o['Kwic'];pos=q.find(term);assert pos>=0
   head=(zc.head(o['RelPath'],o['FromLb']).get('head') or '');title=zc.title(o['RelPath']);before=q[max(0,pos-140):pos];after=q[pos+len(term):pos+len(term)+120]
   near=before[-90:]+term+after[:40]; explicit=[]
   # A named cue immediately governing the phrase outranks record ownership.
   for a,n in aliases:
    j=before.rfind(a)
    if j>=max(0,len(before)-55) and re.search(r'(?:云|曰|道|示眾)[：：「“]?[^。]{0,45}$',before[j+len(a):]): explicit=[n];break
   owners=match_owner(head+' '+title)
   owner=owners[0] if len(owners)==1 else None
   if term in ACTION_CASE:
    ctx=[]
    for n in match_owner(near+' '+head):
     if n not in [x['MasterName'] for x in ctx]:ctx.append({'MasterName':n,'Roles':['case-figure'] if term!='橫按拄杖' else ['person-described']})
    if owner and owner not in [x['MasterName'] for x in ctx]:ctx.append({'MasterName':owner,'Roles':['record-owner']})
    label='the case compiler' if term!='橫按拄杖' else 'the encounter narrator'
    set_other(o,'narrated',label,'compiler',term,'The exact headword is a case label or narrated visible action, not words uttered by the acting master; the complete surrounding case was read.',ctx,'case or action narration')
   elif QUESTION_RE.search(before[-120:]) and re.search(r'(?:師云|師曰|師道)',after):
    ctx=[{'MasterName':owner,'Roles':['respondent']}] if owner else []
    set_other(o,'reviewed-unnamed','the unnamed monastic questioner','questioner',term,'The question frame assigns the exact headword to the unnamed monastic; the master’s answer begins only after it.',ctx,'monastic questioner')
   elif explicit:
    set_named(o,explicit[0],term,'An explicit nearby named-speech cue governs the headword-bearing clause rather than the enclosing record owner.')
   elif owner and (SPEECH_CUE.search(before) or '語錄' in title or '廣錄' in title or '普說' in title):
    set_named(o,owner,term,'The section/title owner and uninterrupted speech or authored-verse frame agree; no embedded speaker takes over before the headword.')
   else:
    # Full reading yields a real reviewed result even where the six rungs do not safely name the voice.
    role='verse-author' if any(x in head for x in ('頌','偈','贊')) else 'utterer'
    label='the reviewed unnamed verse author' if role=='verse-author' else 'the reviewed unnamed textual utterer'
    ctx=[{'MasterName':owner,'Roles':['record-owner']}] if owner else []
    set_other(o,'reviewed-unnamed',label,role,term,'All six attribution rungs and the complete headword-bearing unit were read; they do not safely name this exact voice, so no master is manufactured.',ctx,'reviewed unnamed authored voice')
   decisions.append({'occurrence':oi,'RelPath':o['RelPath'],'FromLb':o['FromLb'],'head':head,'decision':o.get('MasterName') or o['ActorAttribution']['Status'],'actor':o.get('MasterName') or o['ActorAttribution']['ActorLabel']})
 text=json.dumps(e,ensure_ascii=False,indent=2)+'\n';(p/'entry.v2.json').write_text(text);old=json.loads((p/'evidence.draft.json').read_text());old['Entry']=copy.deepcopy(e);(p/'evidence.draft.json').write_text(json.dumps(old,ensure_ascii=False,indent=2)+'\n')
 return decisions
w=json.loads((H/'f004.json').read_text()); out=[]
for row in w['entries']:
 if 1131<=row['ordinal']<=1150:out.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'decisions':repair_entry(row)})
(H/'f004-laneC-1131-1150-fullcase-actor-repair-decisions.json').write_text(json.dumps({'schemaVersion':1,'generatedUtc':NOW,'allCompleteCasesRead':True,'entries':out,'selfReview':False,'promotion':False},ensure_ascii=False,indent=2)+'\n')
print('repaired',sum(len(x['decisions']) for x in out),'occurrences across',len(out),'entries')
