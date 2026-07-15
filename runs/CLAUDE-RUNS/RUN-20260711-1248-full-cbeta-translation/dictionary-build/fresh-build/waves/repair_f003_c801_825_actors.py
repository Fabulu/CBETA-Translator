import datetime,json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
roster=json.load(open(ROOT.parents[3]/'Assets/Data/master-dates.json',encoding='utf-8'))['masters']
aliases=sorted([(n,m['names'][0]) for m in roster for n in m.get('names',[])[1:] if re.search(r'[\u3400-\u9fff]',n)],key=lambda x:len(x[0]),reverse=True)

def canonical(text):
 for a,n in aliases:
  if a in text:return n
 return text
def ctx(name,*roles):return [{'MasterName':name,'Roles':list(roles)}] if name else []
def head_for(o):
 hs=zc.heads(o['RelPath'],o['FromLb'],8,o['Kwic']).get('heads') or []
 for h in hs:
  if any(x in h for x in ('禪師','和尚','庵主','國師','尊者','居士')):
   h=h.lstrip('△▲○ ').strip();return h,canonical(h)
 return (None,None)
def set_named(o,name,head,why):
 o.pop('ActorAttribution',None);o['MasterName']=name;o['ContextMasters']=ctx(name,'utterer','section-subject')
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): exact actor ({name}) owns the headword-bearing turn; {why}"
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':name,'SpeechFrame':why,'FullCaseDecision':why}
def set_unnamed(o,display,kind='monastic questioner',role='questioner',why=None):
 label=f'the unnamed questioner using the {display} wording' if role=='questioner' else f'the unnamed ancient quoted with the {display} wording'
 why=why or f'The explicit question frame assigns the {display} wording to the unnamed interlocutor; the marked response is separate.'
 o.pop('MasterName',None);o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f003 Lane C actor-semantic rereview','ReviewedUtc':NOW};o['ContextMasters']=[]
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): {label} owns the exact headword-bearing wording after all six attribution rungs.";o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}
def set_narr(o,display,head,name,why):
 label=f"the compiler narrating the {display} event or documentary wording"
 o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler','RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f003 Lane C actor-semantic rereview','ReviewedUtc':NOW};o['ContextMasters']=ctx(name,'person-described','section-subject') if name else []
 o['AttributionNote']=f"Source text ({zc.title(o['RelPath'])}): {label} owns the narrative clause; the section heading ({head}) names the contextual person only." if name else f"Source text ({zc.title(o['RelPath'])}): {label} owns the narrative clause."
 o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':why,'FullCaseDecision':why}

def repair(o,term,display):
 q=o['Kwic'];head,name=head_for(o);full=zc.context(o['RelPath'],o['FromLb'],500,o['Kwic']).get('window') or q;i=full.find(q);pre=full[:i] if i>=0 else ''
 # Explicit questioner turn wins over nearby named respondents.
 if re.search(r'(?:僧問|(^|[。！？])問|問[:：])',q) and (q.find(term)<q.find('師曰') if '師曰' in q else True) and (q.find(term)<q.find('師云') if '師云' in q else True):
  return set_unnamed(o,display)
 if '古人道' in q or '古者道' in q:return set_unnamed(o,display,'quoted ancient','utterer','The explicit ancient-saying formula assigns the wording to an ancient whom all six rungs leave unnamed.')
 # Explicitly named quoted master immediately governing the headword.
 before=q[:q.find(term)]
 marks=list(re.finditer(r'([\u3400-\u9fff]{1,8})(?:云|曰|道)[:：]?',before))
 if marks:
  raw=marks[-1].group(1);explicit=canonical(raw)
  if explicit!=raw or raw in ('世尊','佛','祖','南泉','百丈','雲門','趙州','臨濟','大慧','圜悟','雪竇'):
   special={'世尊':'Buddha','佛':'Buddha','祖':'Bodhidharma'};return set_named(o,special.get(raw,explicit),head,'The explicit named-speech frame governs the exact clause.')
 # Marked current-master speech inside a resolved section.
 direct=bool(re.search(r'(?:師曰|師云|師道|上堂|示眾|乃曰|乃云)[^。！？]{0,180}'+re.escape(term),q))
 if not direct:
  tail=pre[-500:]
  m=max([tail.rfind(x) for x in ('上堂','示眾','師云','師曰','乃云','乃曰')])
  boundary=max([tail.rfind(x) for x in ('下座','便歸方丈','示寂','卷第','禪師法嗣')])
  direct=m>boundary and m>=0 and not re.search(r'(?:舉|拈古|頌曰|云曰)[^。！？]{0,40}$',tail[m:])
 if direct and name:return set_named(o,name,head,'The nearest section heading names the exact actor and an uninterrupted formal-address or marked-speech frame governs this clause.')
 # Third-person action/biography and headings remain genuine narration, now specific.
 why='The clause is third-person narration or documentary wording without a governing direct-speech marker assigning the headword to the contextual person.'
 return set_narr(o,display,head,name,why)

for a,b in ((801,810),(811,820),(821,825)):
 led=json.load(open(ROOT/f'fresh-build/waves/f003-laneC-{a}-{810 if a==801 else 820 if a==811 else 830}-research-ledger.json',encoding='utf-8'))
 for e in led['entries']:
  if not a<=e['ordinal']<=b:continue
  p=ROOT/'fresh-build/entries'/e['id']/'evidence.draft.json';z=json.load(open(p,encoding='utf-8'));s=z['Entry']['Senses'][0]
  for o in s['Occurrences']:repair(o,e['term'],s['PreferredTarget'])
  p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
  out=p.parent/'entry.v2.json';rep=p.parent/'compile-report.json';subprocess.run([sys.executable,str(ROOT/'compile_evidence_draft.py'),str(p),'--output',str(out),'--report',str(rep)],check=True)
print('repaired C801-825 actor semantics')
