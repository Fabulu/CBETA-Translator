import json,re,subprocess,sys
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
review=json.load(open(R/'fresh-build/waves/f003-laneC-801-850-repair2-independent-exact-rereview.json'))
rows={r['ordinal']:r for r in review['rows'] if r['verdict']=='REVISE'}
# One-based flat occurrence numbers requiring a recovered quoted voice rather than compiler narration.
targets={802:[4],804:[1,2,3,5,6,7],807:[1,2,4,6,7],808:[6,7],809:[1,3,5,6],810:[4,6],812:[3,4,6,7],815:[3,4,6],817:[2],818:[2,7],820:[2,3,4],821:[1,3,4,5,7],823:[3,4,5,7],825:[1,2,3,4,6],827:[4,5,6],828:[4,7],831:[3],832:[2,3,4,5,6,7],837:[2,4,5,7],840:[1,2,3,4,6,7],842:[1,2,4,5],846:[2,3,4,6,7],847:[5],848:[1,5,6,7]}
def actor(label,kind='quoted speech'):
 return {'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':'verse-author' if kind=='quoted verse' else 'utterer','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex f003 Lane C exact-turn repair','ReviewedUtc':datetime.now(timezone.utc).isoformat(),'GrammarEvidence':'The headword lies inside the quoted or verse turn, not in the compiler’s framing narration; the stored excerpt does not safely expose a roster-canonical personal name.'}
for n,row in rows.items():
 d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.load(open(p));flat=[]
 for s in x['Entry']['Senses']:
  flat.extend(s['Occurrences'])
 for i in targets.get(n,[]):
  o=flat[i-1];w=o['Kwic'];pos=w.find(x['Entry']['SourceTerm']);pre=w[max(0,pos-240):pos]
  if n==842 and i in (1,2):
   o['MasterName']='Bodhidharma';o['ContextMasters']=[{'MasterName':'Bodhidharma','Roles':['utterer']}];o.pop('ActorAttribution',None);label='Bodhidharma'
  elif n==831 and i==3:
   o.pop('MasterName',None);label='the old woman speaking in the quoted case';o['ActorAttribution']=actor(label)
  elif n==837 and i==5:
   o['MasterName']='Yuanwu Keqin';o['ContextMasters']=[{'MasterName':'Yuanwu Keqin','Roles':['utterer']}];o.pop('ActorAttribution',None);label='Yuanwu Keqin'
  elif re.search(r'僧問|問：|胡問|師問',pre[-80:]):
   o.pop('MasterName',None);label='the unnamed questioner asking the quoted turn';o['ActorAttribution']=actor(label)
  elif re.search(r'婆云',pre[-80:]):
   o.pop('MasterName',None);label='the old woman speaking in the quoted case';o['ActorAttribution']=actor(label)
  elif re.search(r'祖曰',pre[-80:]):
   o.pop('MasterName',None);label='the patriarch speaking in the quoted turn';o['ActorAttribution']=actor(label)
  elif re.search(r'頌|偈|林泉好商量',w[:80]):
   o.pop('MasterName',None);label='the verse voice carrying the headword line';o['ActorAttribution']=actor(label,'quoted verse')
  else:
   o.pop('MasterName',None);label='the presiding speaker in the quoted hall-address turn';o['ActorAttribution']=actor(label)
  title=zc.title(o['RelPath']);o['AttributionNote']=f'Source text ({title}): {label} owns the exact headword-bearing wording.'
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n')
 subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True)
print('repaired',len(rows))
