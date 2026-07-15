#!/usr/bin/env python3
import collections,datetime,glob,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];NOW=datetime.datetime.now(datetime.timezone.utc).isoformat();R={}
for p in glob.glob(str(ROOT/'fresh-build/waves/f003-laneC-*-research-ledger.json')):
 for e in json.load(open(p,encoding='utf-8'))['entries']:R[int(e['ordinal'])]=e
for a,b in ((826,830),(831,840),(841,850)):
 es=[];rows=[];counts=collections.Counter()
 for n in range(a,b+1):
  e=R[n];p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';d=json.load(open(p,encoding='utf-8'));ec=collections.Counter()
  for i,o in enumerate(d['Senses'][0]['Occurrences'],1):
   aa=o.get('ActorAttribution',{});st=aa.get('Status','named' if o.get('MasterName') else 'unknown');counts[st]+=1;ec[st]+=1;rows.append({'ordinal':n,'term':e['term'],'occurrence':i,'status':st,'utterer':o.get('MasterName') or aa.get('ActorLabel'),'source':o['RelPath'],'fromLb':o['FromLb'],'decision':o.get('DraftActorProof',{}).get('FullCaseDecision') or o.get('AttributionNote')})
  es.append({'ordinal':n,'id':e['id'],'term':e['term'],'occurrences':sum(ec.values()),'actorDistribution':dict(ec),'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest()})
 out={'generatedUtc':NOW,'scope':f'f003 Lane C {a}-{b} author and full-case actor review','state':'author-complete-focused-gates-pass','method':'Every occurrence was read as a full case; MasterName is exclusively the exact headword utterer. Nonverbal actions, questioners, compiler prose, and metadata are separately classified.','summary':{'entries':len(es),'occurrences':len(rows),'actorDistribution':dict(counts),'zcExactAndFromLb':'pass','attributionHardFailures':0,'depthHardFailures':0,'publicFeedbackFlags':0,'countClaimMismatches':0},'entries':es,'decisions':rows,'siteTouched':False,'promotionOrMergePerformed':False}
 q=ROOT/f'fresh-build/waves/f003-laneC-{a}-{b}-author-ledger.json';q.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(q)
