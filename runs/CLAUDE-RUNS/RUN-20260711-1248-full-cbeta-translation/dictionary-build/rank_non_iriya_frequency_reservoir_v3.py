#!/usr/bin/env python3
"""Discovery-only v3 overlay for the frozen non-Iriya frequency reservoir.

The ranker never accepts entries. It detects likely internal n-gram boundaries,
suggests exact one-graph expansions, and preserves every candidate for manual
full-case review in either the ranked or deferred lane.
"""
from __future__ import annotations
import argparse,collections,datetime as dt,hashlib,json,math,re,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT))
import zc
M=ROOT/'maintenance'; V2=M/'non-iriya-frequency-reservoir-v2-20260718.json'
LEDGERS={8:'non-iriya-v2-semantic-canary25-batch8-ledger.json',9:'non-iriya-v2-semantic-canary25-batch9-ledger.json',10:'non-iriya-v2-semantic-canary25-batch10-ledger.json',11:'non-iriya-v2-frequency-batch11-semantic-ledger-c19-e8.json',12:'non-iriya-v2-frequency-batch12-semantic-ledger-b6.json',13:'non-iriya-v2-frequency-batch13-semantic-ledger-b6.json',14:'non-iriya-v2-frequency-batch14-semantic-ledger-b6.json',15:'non-iriya-v2-frequency-batch15-semantic-ledger-d7.json'}
CJK=re.compile(r'[\u3400-\u9fff]{3,}')
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()

def settled():
 out={}
 for b,name in LEDGERS.items():
  d=json.load(open(M/name))
  for x in d['decisions']: out[x['term']]={'batch':b,'disposition':x['disposition'],'reason':x.get('reason','')}
 return out

def contexts(terms):
 bylen=collections.defaultdict(set)
 for t in terms:bylen[len(t)].add(t)
 total=collections.Counter();left=collections.defaultdict(collections.Counter);right=collections.defaultdict(collections.Counter)
 allow=json.load(open(zc.ALLOW,encoding='utf-8-sig'))
 for rel in allow['texts']:
  text,_=zc._load(rel)
  for run in CJK.findall(text):
   for n,cands in bylen.items():
    for i in range(len(run)-n+1):
     t=run[i:i+n]
     if t not in cands:continue
     total[t]+=1
     if i:left[t][run[i-1]]+=1
     if i+n<len(run):right[t][run[i+n]]+=1
  zc._cache.get('files',{}).pop(rel,None)
 return total,left,right

def top(counter,total):
 if not counter or not total:return (None,0,0)
 g,n=counter.most_common(1)[0];return g,n,n/total

def classify(row,total,left,right,known):
 t=row['term'];n=max(1,total[t]);lg,ln,lp=top(left[t],n);rg,rn,rp=top(right[t],n)
 suggestions=[]
 if lg:suggestions.append({'term':lg+t,'side':'left','support':ln,'dominance':round(lp,6),'alreadyKnown':lg+t in known})
 if rg:suggestions.append({'term':t+rg,'side':'right','support':rn,'dominance':round(rp,6),'alreadyKnown':t+rg in known})
 suggestions.sort(key=lambda x:(-x['dominance'],-x['support'],x['term']))
 dom=max(lp,rp); support=max(ln,rn)
 trap=dom>=0.95 and support>=12
 parent=bool(row['substringParents'])
 generic='triage:generic-character-present' in row['flags']
 lane='DEFER_BOUNDARY_EXPAND' if trap else 'RANKED_MANUAL_REVIEW'
 structural=bool(re.search(r'(曰|云|問)$',t))
 penalty=(3.0 if trap else 0)+(1.15 if parent else 0)+(0.85 if generic else 0)+(0.65 if row['coveredChildren'] else 0)+(0.75 if structural else 0)
 score=math.log1p(row['zcExactHits'])+0.20*math.log1p(row['zcExactDistinctWorks'])+0.10*row['graphs']-penalty
 return {'lane':lane,'score':round(score,6),'contextOccurrences':n,'leftTop':{'graph':lg,'support':ln,'dominance':round(lp,6)},'rightTop':{'graph':rg,'support':rn,'dominance':round(rp,6)},'boundaryDominance':round(dom,6),'boundarySupport':support,'substringOfCovered':parent,'containsCovered':bool(row['coveredChildren']),'genericCharacterPresent':generic,'structuralReportingFrame':structural,'expansionSuggestions':suggestions[:2]}

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--top',type=int,default=500);ap.add_argument('--deferred-sample',type=int,default=100);ap.add_argument('--compact',action='store_true');ap.add_argument('--terms-only',action='store_true');a=ap.parse_args()
 v2=json.load(open(V2));rows=v2['bands']['3-8']['rows'];known={x['term'] for x in rows}; truth=settled();total,left,right=contexts(known)
 enriched=[]
 for rank,row in enumerate(rows,1): enriched.append({'v2Rank':rank,**row,'v3':classify(row,total,left,right,known)})
 ranked=sorted((x for x in enriched if x['v3']['lane']=='RANKED_MANUAL_REVIEW'),key=lambda x:(-x['v3']['score'],x['v2Rank']))
 deferred=[x for x in enriched if x['v3']['lane']=='DEFER_BOUNDARY_EXPAND']
 test=[x for x in enriched if x['term'] in truth]
 def matrix(pred):
  c=collections.Counter()
  for x in test:c[(truth[x['term']]['disposition'],pred(x))]+=1
  return {f'{k[0]}::{k[1]}':v for k,v in sorted(c.items())}
 boundary_re=re.compile(r'fragment|substring|clipp|truncat|boundary|seam|incomplete|ordinary|generic|productive|covered|tail of|beginning of',re.I)
 rank_test=sorted(test,key=lambda x:(-x['v3']['score'],x['v2Rank']))
 v2_test=sorted(test,key=lambda x:x['v2Rank'])
 precision=[]
 for k in (25,50,100,200):
  precision.append({'k':k,'v2KEEP':sum(truth[x['term']]['disposition']=='KEEP' for x in v2_test[:k]),'v3KEEP':sum(truth[x['term']]['disposition']=='KEEP' for x in rank_test[:k])})
 thresholds=[]
 for th in (0.80,0.85,0.90,0.95,0.98,1.0):
  flagged=[x for x in test if x['v3']['boundaryDominance']>=th and x['v3']['boundarySupport']>=12]
  thresholds.append({'dominance':th,'flagged':len(flagged),'KEEP':sum(truth[x['term']]['disposition']=='KEEP' for x in flagged),'REJECT':sum(truth[x['term']]['disposition']=='REJECT' for x in flagged),'PROVISIONAL':sum(truth[x['term']]['disposition']=='PROVISIONAL' for x in flagged),'knownBoundaryOrGenericRejects':sum(truth[x['term']]['disposition']=='REJECT' and boundary_re.search(truth[x['term']]['reason']) is not None for x in flagged)})
 out={'schemaVersion':'non-iriya-frequency-reservoir-v3-overlay.v1','generatedUtc':dt.datetime.now(dt.timezone.utc).isoformat(),'scope':'discovery/ranking only; no accepted entries','sourceV2':str(V2.relative_to(ROOT)),'sourceV2Sha256':sha(V2),'method':{'context':'apparatus-clean allowlist CJK-run immediate left/right graph dominance','boundaryRule':'defer when one immediate graph occurs on either side in >=95% of overlapping run occurrences with support >=12','expansion':'suggest exact one-graph left/right forms only; suggestions remain unaccepted and require exact recount plus full-case review','ranking':'frequency/work spread/length score with covered-family, generic-grammar, and reporting-frame penalties; deferred rows are preserved, never silently deleted','finalStandard':'unchanged: manual full-case semantic decision, exact zc count, and canonical-distinct verified works'},'backtest':{'batches':'8-15','settledRows':len(test),'matrix':matrix(lambda x:x['v3']['lane']),'rankPrecision':precision,'thresholdTable':thresholds,'deferredKnownKeeps':[{'term':x['term'],'batch':truth[x['term']]['batch'],'dominance':x['v3']['boundaryDominance']} for x in test if truth[x['term']]['disposition']=='KEEP' and x['v3']['lane']=='DEFER_BOUNDARY_EXPAND'],'knownBoundaryOrGenericRejects':sum(truth[x['term']]['disposition']=='REJECT' and boundary_re.search(truth[x['term']]['reason']) is not None for x in test),'knownBoundaryOrGenericRejectsDeferred':sum(truth[x['term']]['disposition']=='REJECT' and boundary_re.search(truth[x['term']]['reason']) is not None and x['v3']['lane']=='DEFER_BOUNDARY_EXPAND' for x in test),'batch15Deferred':sum(truth[x['term']]['batch']==15 and x['v3']['lane']=='DEFER_BOUNDARY_EXPAND' for x in test)},'counts':{'sourceRows':len(rows),'rankedRows':len(ranked),'deferredRows':len(deferred),'emittedTopRanked':min(a.top,len(ranked))},'topRanked':ranked[:a.top],'deferredSample':sorted(deferred,key=lambda x:(-x['v3']['boundaryDominance'],-x['v3']['boundarySupport'],x['v2Rank']))[:a.deferred_sample],'authorityMutation':False,'dictionaryMutation':False,'registryMutation':False,'entriesBuilt':False,'manualReviewStillRequired':True}
 if a.terms_only:
  out={'topRanked':[{'v2Rank':x['v2Rank'],'term':x['term'],'hits':x['zcExactHits'],'works':x['zcExactDistinctWorks'],'score':x['v3']['score'],'flags':x['flags']} for x in out['topRanked']],'deferred':[{'v2Rank':x['v2Rank'],'term':x['term'],'suggestions':x['v3']['expansionSuggestions']} for x in out['deferredSample']]}
 elif a.compact:
  out={'schemaVersion':'non-iriya-frequency-v3-compact-navigation.v1','sourceV2Sha256':out['sourceV2Sha256'],'backtest':out['backtest'],'counts':out['counts'],'topRanked':[{'v2Rank':x['v2Rank'],'term':x['term'],'graphs':x['graphs'],'hits':x['zcExactHits'],'works':x['zcExactDistinctWorks'],'flags':x['flags'],'parents':x['substringParents'],'children':x['coveredChildren'],'v3':x['v3']} for x in out['topRanked']],'deferredSample':[{'v2Rank':x['v2Rank'],'term':x['term'],'hits':x['zcExactHits'],'works':x['zcExactDistinctWorks'],'flags':x['flags'],'parents':x['substringParents'],'children':x['coveredChildren'],'v3':x['v3']} for x in out['deferredSample']]}
 print(json.dumps(out,ensure_ascii=False,indent=2))
if __name__=='__main__':main()
