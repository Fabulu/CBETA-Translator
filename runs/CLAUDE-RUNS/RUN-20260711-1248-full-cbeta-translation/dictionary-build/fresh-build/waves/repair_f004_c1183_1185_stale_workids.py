from pathlib import Path
import datetime,hashlib,json,subprocess,sys
R=Path(__file__).resolve().parents[2];H=R/'fresh-build/waves';sys.path.insert(0,str(R));import zc
ROWS=[(1183,'t_dcb8d664b64f'),(1184,'t_b7fd3f3a1395'),(1185,'t_ce08a150ae0a')]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
out=[]
for ordinal,eid in ROWS:
 b=R/'fresh-build/entries'/eid;wp=b/'evidence.draft.json';wrapper=json.loads(wp.read_text(encoding='utf-8'));draft=wrapper['Entry']
 changes=[]
 for s in draft['Senses']:
  retained=list(dict.fromkeys(zc.work_id(o['RelPath']) for o in s['Occurrences']))
  old=(s.get('DraftEvidence') or {}).get('IndependentWorkIds',[])
  s['DraftEvidence']['IndependentWorkIds']=retained
  changes.append({'before':old,'after':retained,'removed':sorted(set(old)-set(retained))})
 wrapper['Entry']=draft;wp.write_text(json.dumps(wrapper,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 ep=b/'entry.v2.json';report=b/'stale-workids-compile-report.json'
 p=subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(wp),'--output',str(ep),'--report',str(report)],text=True,capture_output=True)
 if p.returncode:raise SystemExit(p.stdout+p.stderr)
 e=json.loads(ep.read_text(encoding='utf-8'));exact=0;total=0
 for s in e['Senses']:
  for o in s['Occurrences']:
   total+=1;v=zc.verify(o['RelPath'],o['Kwic']);exact+=int(v['ok'] and v['fromLb']==o['FromLb'] and v['toLb']==o['ToLb'] and e['SourceTerm'] in o['Kwic'])
 assert exact==total
 row={'ordinal':ordinal,'id':eid,'term':e['SourceTerm'],'changes':changes,'occurrences':total,'exactKwicsAndSpans':exact,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'compileReport':str(report.relative_to(R)),'compileHardPass':True,'selfReview':False,'promoted':False,'published':False}
 (H/f'f004-laneC-{ordinal}-stale-workids-author-checkpoint.json').write_text(json.dumps(row,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');out.append(row)
ledger={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'C1183-C1185 stale IndependentWorkIds synchronization only','sourceReview':'f004-laneC-1176-1185-reviewer9-independent.json','entries':out,'entriesRepaired':3,'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in out),'compileHardPass':True,'selfReview':False,'promoted':False,'published':False}
(H/'f004-laneC-1183-1185-stale-workids-author-ledger.json').write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({'entries':3,'exact':ledger['exactKwicsAndSpans']},ensure_ascii=False))
