#!/usr/bin/env python3
import datetime,glob,json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

DIRECT={827:{1,3,4,5,6},828:{2,3,4,5,6,7},831:{1,2,4,5},832:{2,3,4,7},833:{1,2,3,5,6},834:{1,2,3,4,5,6,7},835:{7},837:{1,2,4,5,6,7},838:{2},839:{3,5,6,7},840:{1,2,3,4,5,6,7},842:{1,2,3,4,5,7},843:{1,2,3,4,5},845:{3},846:{1,2,3,4,5,6,7},847:{3,5},848:{1,2,3,5,6,7},849:{4,5},850:{2,3,4,5,6}}
QUEST={(827,2),(828,1),(832,1),(832,5),(832,6),(832,8),(847,6),(848,4),(849,2),(850,1)}
IMPERSONAL={(826,3),(826,7),(831,7),(836,1),(836,2),(836,3),(838,1),(838,3),(841,1),(841,2),(844,1),(844,2),(844,3),(844,4),(844,5),(849,1),(849,3)}
EXACT={(834,1):'Daoxin',(834,2):'Daoxin',(834,3):'Daoxin',(834,4):'Daoxin',(834,5):'Huineng',(834,7):'Linji Yixuan',(837,1):'Yuanwu Keqin',(837,2):'東禪觀禪師',(837,4):'Nanquan Puyuan',(837,5):'Yuanwu Keqin',(837,6):'Yuanwu Keqin',(842,1):'Bodhidharma',(842,2):'Bodhidharma',(842,3):'Bodhidharma',(842,5):'Bodhidharma',(842,7):'Bodhidharma',(843,1):'Buddha',(843,5):'Buddha',(845,3):'Huangbo Xiyun'}

def clean(s):
 if not s:return None
 s=s.strip('△▲○ 。');s=re.sub(r'(?:語錄|廣錄|法檀|全錄|心要)(?:總目|目錄|目次|序|秉一)?.*$','',s);s=re.sub(r'(?:法嗣|者|凡一|凡四|說法之圖|像)$','',s)
 return s.strip() or None
def resolve(o):
 title=zc.title(o['RelPath']);own=clean(title) if any(x in title for x in ('語錄','廣錄','全錄','心要')) else None
 if own and own not in ('古尊宿','續古尊宿'):return own
 hs=zc.heads(o['RelPath'],o['FromLb'],30,o['Kwic']).get('heads',[])
 for h in hs:
  h=clean(h)
  if h and any(x in h for x in ('禪師','和尚','庵主','國師','尊者')) and not any(x in h for x in ('目次','目錄','法嗣','語錄','序')):return h
 return None
def named(o,name):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=[{'MasterName':name,'Roles':['utterer']}]
 why='Full-case reading places the exact headword inside this exact actor’s marked speech or uninterrupted formal address.';o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): exact actor ({name}) utters the headword-bearing wording; {why}";o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':why,'FullCaseDecision':why}
def other(o,label,role,status,why,context=None):
 if role=='impersonal':role='compiler'
 o.pop('MasterName',None);o['ActorAttribution']={'Status':status,'Kind':'compiler narrative' if status=='narrated' else ('non-human/documentary text' if status=='impersonal' else 'identified role without personal name'),'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f003 Lane C full-case actor rereview','ReviewedUtc':NOW};o['ContextMasters']=([{'MasterName':context,'Roles':['person-described']}] if context else []);o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): {label} owns the headword-bearing wording; {why}";o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}

for lp in glob.glob(str(ROOT/'fresh-build/waves/f003-laneC-*-research-ledger.json')):
 for e in json.load(open(lp,encoding='utf-8')).get('entries',[]):
  n=int(e['ordinal'])
  if not 826<=n<=850:continue
  p=ROOT/'fresh-build/entries'/e['id']/'evidence.draft.json';d=json.load(open(p,encoding='utf-8'));os=d['Entry']['Senses'][0]['Occurrences']
  for i,o in enumerate(os,1):
   key=(n,i);context=resolve(o)
   if key in EXACT:named(o,EXACT[key])
   elif key in QUEST:other(o,'the interlocutor asking the recorded question','questioner','reviewed-unnamed','The headword lies in the question before the master’s separately marked response.')
   elif i in DIRECT.get(n,set()):
    if context:named(o,context)
    else:other(o,'the named speaker preserved only in abbreviated form','utterer','reviewed-unnamed','The full case is direct speech, but all six rungs do not expand its abbreviated speaker label safely.')
   elif key in IMPERSONAL:other(o,'the title, catalogue, or rule heading itself','impersonal','impersonal','The headword is documentary metadata rather than a human utterance.')
   else:other(o,'the compiler or recorder','compiler','narrated','The headword occurs in third-person narration, an action-stage direction, an office/name label, or documentary prose rather than in a master’s utterance.',context)
  # Public prose must not orphan a quotation behind a generic master/monk/speaker.
  s=d['Entry']['Senses'][0]
  for part in ('CorpusEarnedOpening',):
   if part in s.get('ExplanationParts',{}):
    s['ExplanationParts'][part]=re.sub(r'\b(?:a|the) (?:teacher|master|monk|speaker)\b','the recorded participant',s['ExplanationParts'][part],flags=re.I)
  s['ExplanationParts']['EvidenceBody']=[re.sub(r'\b(?:a|the) (?:teacher|master|monk|speaker)\b','the recorded participant',x,flags=re.I) for x in s.get('ExplanationParts',{}).get('EvidenceBody',[])]
  p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');out=p.parent/'entry.v2.json';subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(p),'--output',str(out),'--report',str(p.parent/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('finalized C826-850 actor semantics')
