import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build/entries';W=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
RUNGS=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
rev=json.load(open(W/'f004-cohort1-round2-independent-rereview.json'))
def named(o,n):
 why='The complete case and exact speech/action frame identify this actor; no enclosing record owner or quoted figure is substituted.';o.pop('ActorAttribution',None);o['MasterName']=n;o['ContextMasters']=[{'MasterName':n,'Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {n}. {why}'
def reviewed(o,label,role='utterer'):
 why='The complete unit identifies the participant role but supplies no safe canonical personal name; all six rungs were checked.';o['MasterName']=None;o['ContextMasters']=[];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':label,'ActorLabel':label,'ActorRole':role,'RungsChecked':RUNGS,'GrammarEvidence':why,'ReviewedBy':'Codex f004 cohort1 round3 author','AuthoredVoiceRiskReviewed':True};o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}; {o["RelPath"]}). Exact actor: {label}. {why}'
fix={('法身向上事','0718b17'):'Longya Judun',('眼睛','0659c08'):'Muzhou Daoming',('來機','0720c24'):'Longya Judun',('鼓聲','0151a02'):'Pizao Duo'}
for row in rev['entries']:
 p=E/row['id'];d=json.load(open(p/'evidence.draft.json'))
 for s in d['Entry']['Senses']:
  for o in s['Occurrences']:
   key=(row['term'],o['FromLb'])
   if key in fix:named(o,fix[key])
   a=o.get('ActorAttribution') or {}
   if a.get('ActorLabel') in {'Dayu’s attendant','the unnamed monastic questioner'}:reviewed(o,'the reviewed unnamed monastic participant',a.get('ActorRole') if a.get('ActorRole') in {'questioner','utterer'} else 'utterer')
   if a.get('ActorLabel') in {'the named participant Ying','the named head monk Ying'}:
    a['Status']='identified-non-master';a['ActorLabel']='Ying';a['Kind']='named head monk';a['ActorRole']='utterer'
  parts=s.get('ExplanationParts') or {}
  for k in ['CorpusEarnedOpening']:
   parts[k]=str(parts.get(k,'')).replace('a master','the cited source speaker').replace('A master','The cited source speaker').replace('the master','the cited source speaker')
  parts['EvidenceBody']=[x.replace('a master','the cited source speaker').replace('A master','The cited source speaker').replace('the master','the cited source speaker') for x in parts.get('EvidenceBody',[])]
 (p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'round3-gatefix-report.json')],check=True,stdout=subprocess.DEVNULL)

# Build a validated pending-roster packet for exact source owners not yet in the
# public roster. This does not modify public roster data.
public={m['names'][0] for m in json.load(open(R.parents[3]/'Assets/Data/master-dates.json'))['masters']};pending={c['canonicalName'] for c in json.load(open(R/'fresh-build/pending-roster.json'))['candidates']};rows={}
for row in rev['entries']:
 e=json.load(open(E/row['id']/'entry.v2.json'))
 for s in e['Senses']:
  for o in s['Occurrences']:
   n=o.get('MasterName')
   if n and n not in public and n not in pending and n not in rows:
    rows[n]={'canonicalName':n,'aliases':[n],'evidence':[{'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o['ToLb'],'Kwic':o['Kwic']}],'reviewedBy':'Codex f004 cohort1 round3 repair author','reviewReport':'fresh-build/waves/f004-cohort1-round2-independent-rereview.json','status':'awaiting-roster-integration'}
(W/'f004-cohort1-round3-roster-candidates.json').write_text(json.dumps({'schemaVersion':1,'candidates':list(rows.values())},ensure_ascii=False,indent=2)+'\n')
print(len(rows))
