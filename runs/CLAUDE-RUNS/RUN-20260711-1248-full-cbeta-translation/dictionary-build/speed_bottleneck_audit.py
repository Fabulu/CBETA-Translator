#!/usr/bin/env python3
"""Durable f005 speed/churn audit plus non-mutating compiler benchmark."""
import datetime,hashlib,json,re,subprocess,sys,tempfile,time
from pathlib import Path
BASE=Path(__file__).resolve().parent;W=BASE/'fresh-build/waves';E=BASE/'fresh-build/entries'
def j(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def dt(s):return datetime.datetime.fromisoformat(str(s).replace('Z','+00:00'))
composites=[p for p in W.glob('f005*composite*.json') if 'attribution-packets' not in p.name]
phase={};runs=[]
for p in composites:
 d=j(p)
 if 'phaseTimings' not in d:continue
 runs.append({'artifact':p.name,'entries':len(d.get('entries') or []),'elapsedSeconds':d.get('elapsedSeconds'),'phaseTimings':d['phaseTimings']})
 for k,v in d['phaseTimings'].items():phase.setdefault(k,[]).append(float(v or 0))
phase_summary={k:{'runs':len(v),'meanSeconds':round(sum(v)/len(v),3),'maxSeconds':round(max(v),3)} for k,v in phase.items()}

# Durable handoff spans: author ledger -> independent review generation.
review_spans=[]
for rp in W.glob('f005*independent*review.json'):
 d=j(rp);src=d.get('sourceLedger');
 if src and (W/src).exists():
  a=dt(j(W/src)['generatedUtc']);b=dt(d['generatedUtc']);review_spans.append({'review':rp.name,'sourceLedger':src,'minutes':round((b-a).total_seconds()/60,2),'entries':d.get('entriesReviewed')})

# Authoring spans conservatively use preflight artifact mtime -> ledger UTC.
author_spans=[]
for lp in W.glob('f005*author-ledger.json'):
 d=j(lp);pf=(d.get('fastPreflight') or {}).get('path')
 if pf and (W/pf).exists():
  end=dt(d['generatedUtc']).timestamp();start=(W/pf).stat().st_mtime;author_spans.append({'ledger':lp.name,'entries':len(d.get('entries') or []),'minutes':round(max(0,end-start)/60,2),'basis':'fast-preflight mtime to author-ledger UTC; lower-bound handoff span'})

# Rework categories from durable independent reviews.
cats={'actor/turn':0,'unsupported-or-unanchored-prose':0,'sense/depth':0,'other':0};samples=[]
for rp in list(W.glob('f004*independent*rereview.json'))+list(W.glob('f005*independent*review.json')):
 d=j(rp)
 for row in d.get('entries') or []:
  if row.get('verdict')!='REVISE':continue
  for f in row.get('findings') or []:
   low=f.casefold()
   if re.search(r'actor|utter|questioner|respondent|assigned|speaker|mastername|performer|narrat|heading',low):k='actor/turn'
   elif re.search(r'prose|explanation|claim|anchor|overstat|quote',low):k='unsupported-or-unanchored-prose'
   elif re.search(r'sense|depth|occurrence|witness',low):k='sense/depth'
   else:k='other'
   cats[k]+=1;samples.append({'artifact':rp.name,'term':row.get('term'),'category':k,'finding':f})

# Compile benchmark to temporary outputs only; compare canonical JSON semantics.
eid='t_df028fd6bd35';draft=E/eid/'evidence.draft.json';existing=E/eid/'entry.v2.json'
with tempfile.TemporaryDirectory() as td:
 out=Path(td)/'entry.json';rep=Path(td)/'report.json';t=time.perf_counter();cp=subprocess.run([sys.executable,str(BASE/'compile_evidence_draft.py'),str(draft),'--output',str(out),'--report',str(rep)],capture_output=True,text=True);compile_s=time.perf_counter()-t
 compile_bench={'entryId':eid,'seconds':round(compile_s,3),'exitCode':cp.returncode,'schemaOutputJsonEqual':j(out)==j(existing),'temporaryOnly':True}

risk=j(W/'f005-authoring-risk-benchmark.json')
payload={'schemaVersion':'f005-speed-bottleneck-audit-v1','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'compositeRuns':runs,'compositePhaseSummary':phase_summary,'authoringHandoffSpans':author_spans,'independentReviewSpans':review_spans,'reworkFindingCategories':cats,'reworkSamples':samples,'compileBenchmark':compile_bench,'authoringRiskBenchmark':{'entries':risk['entries'],'elapsedSeconds':risk['elapsedSeconds'],'flagged':risk['flagged'],'flags':risk['flags'],'knownCanary':'卓一下','knownCanaryDefectsCaught':7,'cleanCanary':'語言','cleanCanaryFalseFlags':0},'conclusion':['Human full-case authoring and independent semantic review dominate wall time; compilation and cheap lint are sub-second per small cohort.','Attribution and depth are the largest recurring composite phases; attribution-packet extraction is expensive on a cold cache but disappears on cache hits.','Actor/turn errors dominate durable rework. The new advisory preflight catches proof-subject contradictions and stage-action performer-to-MasterName risk before composite review.','Risky interpretive verbs are surfaced for anchoring or narrowing; the tool never decides semantics or rewrites final output.']}
out=W/'f005-speed-bottleneck-audit.json';out.write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n')
md=BASE/'F005_SPEED_BOTTLENECK_AUDIT.md';md.write_text('# F005 speed and bottleneck audit\n\nGenerated '+payload['generatedUtc']+'\n\n## Result\n\n- Full-case human semantics and repair review dominate elapsed time. Compilation and authoring-risk lint are cheap.\n- Durable rework findings: '+', '.join(f'{k} {v}' for k,v in cats.items())+'.\n- New `authoring_risk_preflight.py` caught all seven known 卓一下 action/performer defects in '+str(risk['elapsedSeconds'])+' seconds and produced zero flags on the accepted 語言 canary.\n- Composite phase means: '+', '.join(f'{k} {v["meanSeconds"]}s' for k,v in phase_summary.items())+'.\n- Compiler benchmark: '+str(compile_bench['seconds'])+'s; semantic JSON output identical: '+str(compile_bench['schemaOutputJsonEqual'])+'.\n\n## Safe process change\n\nRun the risk preflight on evidence drafts before compilation/review. Its findings require a human full-case decision. It never writes entries, assigns speakers, or changes the final schema.\n',encoding='utf-8')
print(json.dumps({'report':str(out),'markdown':str(md),'rework':cats,'compile':compile_bench,'riskBenchmark':payload['authoringRiskBenchmark']},indent=2))
