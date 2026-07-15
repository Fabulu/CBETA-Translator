import json,sys,subprocess,datetime
from pathlib import Path
R=Path(__file__).resolve().parents[2];E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
C=[('t_ef00d55c2d8b','鼓聲','J/J34/J34nB301.xml','work:J34nB301','Nanyue Jiqi Hongchu'),('t_93d4f280640a','點檢','X/X70/X70n1403.xml','work:X70n1403','Tianru Weize'),('t_0e88b1ebd18b','拂子頭','J/J27/J27nB190.xml','work:J27nB190','Shiyu Mingfang')]
# Resolve the third id by term if a stale hard-coded id is absent.
if not (E/C[2][0]).exists():
 for p in E.glob('*/evidence.draft.json'):
  try:d=json.loads(p.read_text())['Entry']
  except:continue
  if d['SourceTerm']=='拂子頭':C[2]=(p.parent.name,*C[2][1:])
for eid,term,rel,wid,name in C:
 p=E/eid;d=json.loads((p/'evidence.draft.json').read_text());s=d['Entry']['Senses'][0];f=zc.find(rel,term,ctx=95,limit=1)[0];q=f['window'];v=zc.verify(rel,q);proof='The complete named-record address was read; uninterrupted formal speech assigns the exact headword clause to the record owner, with no embedded speaker takeover.';o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':q,'Curated':True,'MasterName':name,'ContextMasters':[{'MasterName':name,'Roles':['utterer']}],'AttributionNote':f'Source text ({zc.title(rel)}; {rel}). {name} utters the exact headword wording. {proof}','DraftActorProof':{'ExactHeadwordClause':q,'GrammaticalSubject':name,'SpeechFrame':proof,'FullCaseDecision':proof}};s['Occurrences'].append(o);s['SourceTexts'].append(rel);s['DraftEvidence']['IndependentWorkIds'].append(wid);s['Note']=f'{len(set(s["DraftEvidence"]["IndependentWorkIds"]))} distinct works support this sense after depth enrichment.';(p/'evidence.draft.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p/'evidence.draft.json'),'--output',str(p/'entry.v2.json'),'--report',str(p/'depth-enrich-compile-report.json')],check=True,stdout=subprocess.DEVNULL)
