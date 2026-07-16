import json, hashlib
from collections import Counter, defaultdict
from pathlib import Path

R=Path(__file__).resolve().parents[1]
W=R/'fresh-build'/'waves'
P=W/'f004-all-drafted-attribution-preflight.json'
pre=json.loads(P.read_text(encoding='utf-8'))
wave=json.loads((W/'f004.json').read_text(encoding='utf-8'))
meta={x['id']:x for x in wave['entries']}
fails=defaultdict(list)
for f in pre['failures']:
    ident=Path(f['entry']).parent.name
    fails[ident].append({'kind':f['kind'],'detail':f['detail']})

rows=[]
for ident,m in meta.items():
    status=R/'fresh-build'/'entries'/ident/'STATUS'
    state=status.read_text(encoding='utf-8').strip() if status.exists() else 'missing'
    if state not in {'drafted','researching'}: continue
    entry=R/'fresh-build'/'entries'/ident/'entry.v2.json'
    if not entry.exists(): continue
    rows.append({'ordinal':m['ordinal'],'id':ident,'term':m['term'],'lane':m.get('lane'),'status':state,
                 'classification':'author-repair' if fails[ident] else 'semantic-review-ready',
                 'failureKinds':dict(Counter(x['kind'] for x in fails[ident])),
                 'failureCount':len(fails[ident]),
                 'entrySha256':hashlib.sha256(entry.read_bytes()).hexdigest()})
rows.sort(key=lambda x:x['ordinal'])
repair=[x for x in rows if x['classification']=='author-repair']
clean=[x for x in rows if x['classification']=='semantic-review-ready']
def cohorts(xs,size=50): return [{'ordinalStart':c[0]['ordinal'],'ordinalEnd':c[-1]['ordinal'],'count':len(c),'entries':c} for i in range(0,len(xs),size) for c in [xs[i:i+size]]]
kinds=Counter(f['kind'] for f in pre['failures'])
out={'schemaVersion':1,'sourcePreflight':P.name,'sourcePreflightSha256':hashlib.sha256(P.read_bytes()).hexdigest(),
     'scope':'current f004 drafted/researching entries only','collisionRule':'each entry occurs in exactly one cohort and one assignment class',
     'counts':{'currentDrafted':len(rows),'authorRepair':len(repair),'semanticReviewReady':len(clean),'preflightFailures':sum(kinds.values())},
     'failureKinds':dict(kinds),'authorRepairCohorts':cohorts(repair,25),'semanticReviewCohorts':cohorts(clean,50),
     'recommendedNextThreeAssignments':[
       {'priority':1,'kind':'author-repair','cohort':0,'entries':len(cohorts(repair,25)[0]['entries']) if repair else 0,'reason':'clear cheap-gate defects before spending independent semantic-review effort'},
       {'priority':2,'kind':'author-repair','cohort':1,'entries':len(cohorts(repair,25)[1]['entries']) if len(cohorts(repair,25))>1 else 0,'reason':'finish remaining mechanically blocked entries without collision'},
       {'priority':3,'kind':'semantic-review','cohort':0,'entries':len(cohorts(clean)[0]['entries']) if clean else 0,'reason':'mechanically clean, hash-bound entries can enter independent full-case semantic review now'}]}
(W/'f004-all-drafted-attribution-triage.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(out['counts']|{'failureKinds':out['failureKinds'],'recommendations':out['recommendedNextThreeAssignments']},ensure_ascii=False,indent=2))
