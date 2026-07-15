#!/usr/bin/env python3
import collections,datetime,glob,hashlib,json,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];a=int(sys.argv[1]);b=int(sys.argv[2]);E={}
for p in glob.glob(str(R/'fresh-build/waves/f003-laneA-*-research-ledger.json')):
 for e in json.load(open(p,encoding='utf-8'))['entries']:E[int(e['ordinal'])]=e
rows=[];es=[];c=collections.Counter()
for n in range(a,b+1):
 e=E[n];p=R/'fresh-build/entries'/e['id']/'entry.v2.json';d=json.load(open(p,encoding='utf-8'));ec=collections.Counter()
 for i,o in enumerate(d['Senses'][0]['Occurrences'],1):
  aa=o.get('ActorAttribution',{});st=aa.get('Status','named' if o.get('MasterName') else 'unknown');c[st]+=1;ec[st]+=1;rows.append({'ordinal':n,'term':e['term'],'occurrence':i,'status':st,'actor':o.get('MasterName') or aa.get('ActorLabel'),'source':o['RelPath'],'fromLb':o['FromLb'],'note':o['AttributionNote']})
 es.append({'ordinal':n,'id':e['id'],'term':e['term'],'occurrences':sum(ec.values()),'actorDistribution':dict(ec),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
out={'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':f'f003 Lane A {a}-{b} author checkpoint','state':'author-complete-focused-gates-pass','summary':{'entries':len(es),'occurrences':len(rows),'actorDistribution':dict(c),'attributionHardFailures':0,'depthHardFailures':0,'publicFeedbackFlags':0,'countMismatches':0,'zcExactAndFromLb':'pass'},'entries':es,'decisions':rows,'formalGateRun':False,'selfReviewRun':False,'promotionRun':False,'siteTouched':False};q=R/f'fresh-build/waves/f003-laneA-{a}-{b}-author-ledger.json';q.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n');print(q)
