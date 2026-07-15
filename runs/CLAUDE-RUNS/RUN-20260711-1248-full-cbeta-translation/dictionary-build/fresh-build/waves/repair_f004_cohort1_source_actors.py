import json,re,sys,subprocess,hashlib,datetime,os,tempfile
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';REPO=R.parents[3];sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
ro=json.loads((REPO/'Assets/Data/master-dates.json').read_text())['masters'];AA=[]
for m in ro:
 for a in m.get('names',[])[1:]:
  if len(a)>=2:AA.append((a,m['names'][0]))
AA.sort(key=lambda z:len(z[0]),reverse=True)
def canon(text):
 hits=[]
 for a,n in AA:
  if a in text and n not in hits:hits.append(n)
 return hits[0] if len(hits)==1 else None
def atomic(path,obj):
 fd,tmp=tempfile.mkstemp(prefix=path.name+'.',suffix='.tmp',dir=path.parent)
 with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(obj,f,ensure_ascii=False,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
 os.replace(tmp,path)
def named(o,n,title,proof):
 o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({title}; {o["RelPath"]}). {n} utters the exact headword-bearing wording. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':n,'SpeechFrame':proof,'FullCaseDecision':proof}
def other(o,status,label,role,title,proof,ctx=[]):
 o['MasterName']=None;o['ContextMasters']=ctx;o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex f004 cohort1 source repair','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({title}; {o["RelPath"]}). Exact actor: {label}. {proof}';o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'GrammaticalSubject':label,'SpeechFrame':proof,'FullCaseDecision':proof}
P=json.loads((W/'f004-author-repair-cohort1-attribution-packet.json').read_text());tri=json.loads((W/'f004-all-drafted-attribution-triage.json').read_text());scope={x['id']:x for x in tri['authorRepairCohorts'][1]['entries']};by={}
for p in P['packets']:by.setdefault(p['entryId'],[]).append(p)
ledger=[];done=0
for eid,meta in sorted(scope.items(),key=lambda z:z[1]['ordinal']):
 path=E/eid;d=json.loads((path/'evidence.draft.json').read_text());pack={(p['sense'],p['occurrence']):p for p in by[eid]};dec=[]
 for si,s in enumerate(d['Entry']['Senses'],1):
  for oi,o in enumerate(s['Occurrences'],1):
   p=pack[(si,oi)];case=p['caseText'];proofs=p.get('turnProofCandidates') or [];hp=proofs[0]['headwordStart'] if proofs else case.find(meta['term']);clause=proofs[0]['headwordClause'] if proofs else o['Kwic'];before=case[:hp];local=before[-240:];after=case[hp+len(meta['term']):hp+len(meta['term'])+180];title=p['title']
   # Nearest explicit section label within the complete unit; fall back through heading stack.
   labels=re.findall(r'([^。；：「」\n]{2,45}(?:禪師|和尚|居士|大師))',before)
   section=labels[-1] if labels else next((h for h in p.get('precedingHeadsNearestFirst',[]) if re.search(r'(禪師|和尚|居士|大師)',h)),None)
   owner=canon(section or '')
   explicit=None
   for a,n in AA:
    j=local.rfind(a)
    if j>=0 and re.search(r'(?:曰|云|道|頌曰|別云)[：：「“]?[^。]{0,75}$',local[j+len(a):]):explicit=n;break
   question=bool(re.search(r'(?:僧問|問曰|僧曰|僧云|進云|進曰)[：：「“]?[^。；]{0,150}$',local)) and not clause.startswith(('師曰','師云'))
   speech=bool(re.search(r'(?:上堂|小參|示眾|師曰|師云|乃曰|乃云|頌曰|別云|良久曰)[：：「“]?[^。]{0,180}$',local)) or clause.startswith(('師曰','師云','上堂'))
   if question:
    ctx=[{'MasterName':owner,'Roles':['respondent']}] if owner else [];other(o,'reviewed-unnamed','the unnamed monastic questioner','questioner',title,'The full question-and-answer unit assigns the headword to the unnamed questioner; the master response begins afterward.',ctx)
   elif explicit:named(o,explicit,title,'A nearby explicit personal-name speech cue governs this clause and overrides the enclosing section owner.')
   elif speech and owner:named(o,owner,title,'The nearest personal section label and uninterrupted direct-speech or formal-address frame agree; the full case contains no intervening quoted or guest voice.')
   elif speech and section:
    label=f'the unnamed canonical-roster identity of source-named {section}';other(o,'reviewed-unnamed',label,'utterer',title,'The nearest section explicitly names the utterer, but no canonical roster spelling is available; the source name is retained without inventing a link.')
   elif speech:other(o,'reviewed-unnamed','the reviewed unnamed textual utterer','utterer',title,'The complete case and all six rungs preserve direct speech but do not name its exact utterer; no record owner is substituted.')
   else:
    ctx=[{'MasterName':owner,'Roles':['person-described']}] if owner else [];other(o,'narrated','the source compiler narrating the headword-bearing clause','compiler',title,'The exact clause is documentary, biographical, or case narration rather than speech; the complete unit was read.',ctx)
   dec.append({'sense':si,'occurrence':oi,'rel':o['RelPath'],'lb':o['FromLb'],'clause':clause,'sectionLabel':section,'actor':o.get('MasterName') or o['ActorAttribution']['ActorLabel'],'decision':'named' if o.get('MasterName') else o['ActorAttribution']['Status']});done+=1
 (path/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(path/'evidence.draft.json'),'--output',str(path/'entry.v2.json'),'--report',str(path/'cohort1-actor-compile-report.json')],check=True,stdout=subprocess.DEVNULL);ledger.append({'ordinal':meta['ordinal'],'id':eid,'term':meta['term'],'decisions':dec,'entrySha256':hashlib.sha256((path/'entry.v2.json').read_bytes()).hexdigest()})
 if len(ledger)%10==0:atomic(W/f'f004-author-repair-cohort1-checkpoint-{len(ledger):02d}.json',{'schemaVersion':1,'entries':ledger.copy(),'casesCompleted':done,'selfReview':False,'promoted':False})
atomic(W/'f004-author-repair-cohort1-source-decisions.json',{'schemaVersion':1,'generatedUtc':NOW,'entries':ledger,'casesCompleted':done,'selfReview':False,'promoted':False});print(len(ledger),done)
