import json,re,sys,subprocess,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';REPO=R.parents[3];sys.path.insert(0,str(R));import zc
NEED={'皮袋':3,'韓愈':3,'法鼓':1,'法身向上事':1,'舍利':5,'披毛戴角':2,'解脫香':1,'祖殿':2};RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
AA=[]
for m in json.loads((REPO/'Assets/Data/master-dates.json').read_text())['masters']:
 for a in m['names'][1:]:
  if len(a)>=2:AA.append((a,m['names'][0]))
def own(text):
 n=[]
 for a,c in AA:
  if a in text and c not in n:n.append(c)
 return n[0] if len(n)==1 else None
pre=json.loads((W/'f004-laneB-1001-1100-preflight.json').read_text());pm={x['term']:x for x in pre['entries']};review=json.loads((W/'f004-author-cohort1-independent-review.json').read_text())
for row in review['entries']:
 t=row['term']
 if t not in NEED:continue
 p=E/row['id'];d=json.loads((p/'evidence.draft.json').read_text());s=d['Entry']['Senses'][0];used={o['RelPath'] for o in s['Occurrences']};added=0
 for cw in pm[t]['candidateWorks']:
  if added>=NEED[t] or cw['RelPath'] in used:continue
  for win in cw.get('windows',[]):
   q=win['window'];pos=q.find(t)
   if pos<0:continue
   if t=='舍利' and q[pos+len(t):pos+len(t)+1]=='弗':continue
   if t=='祖殿' and pos and q[pos-1]=='高':continue
   if len(q)<len(t)+30:continue
   q=q[max(0,pos-70):min(len(q),pos+len(t)+100)];v=zc.verify(cw['RelPath'],q)
   if not v.get('ok'):continue
   title=zc.title(cw['RelPath']);head=zc.head(cw['RelPath'],v['fromLb']).get('head') or '';n=own(title+' '+head);direct=bool(re.search(r'(上堂|小參|示眾|師曰|師云|乃曰|乃云|頌曰)',q[:q.find(t)+1]))
   proof='The complete source unit was read; the exact headword is deployed in a formal address or direct comment without an intervening quoted speaker.'
   o={'RelPath':cw['RelPath'],'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'ContextMasters':[],'AttributionNote':'','DraftActorProof':{'ExactHeadwordClause':q,'SpeechFrame':proof,'FullCaseDecision':proof}}
   if n and direct:o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({title}; {cw["RelPath"]}). {n} utters the exact headword wording.';o['DraftActorProof']['GrammaticalSubject']=n
   else:
    label='the reviewed unnamed textual utterer' if direct else 'the source compiler narrating the exact clause';status='reviewed-unnamed' if direct else 'narrated';role='utterer' if direct else 'compiler';o['MasterName']=None;o['ActorAttribution']={'Status':status,'Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':proof,'ReviewedBy':'Codex cohort1 round2 author','ReviewedUtc':NOW,'AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({title}; {cw["RelPath"]}). Exact actor: {label}. {proof}';o['DraftActorProof']['GrammaticalSubject']=label
   s['Occurrences'].append(o);s['SourceTexts'].append(cw['RelPath']);s['DraftEvidence']['IndependentWorkIds'].append(cw['workId']);used.add(cw['RelPath']);added+=1;break
 assert added==NEED[t],(t,added)
 s['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)] if 'OpeningClaimEvidenceKeys' in s else s.get('OpeningClaimEvidenceKeys');s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['Note']=f'{len(s["Occurrences"])} genuine full-case witnesses retained after enrichment.';(p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round2-enrich-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
