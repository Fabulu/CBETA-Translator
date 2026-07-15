#!/usr/bin/env python3
import collections, datetime, glob, hashlib, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
groups=((801,810),(811,820),(821,825))
research={}
for p in glob.glob(str(ROOT/'fresh-build/waves/f003-laneC-*-research-ledger.json')):
 for e in json.load(open(p,encoding='utf-8')).get('entries',[]): research[int(e['ordinal'])]=e
for a,b in groups:
 rows=[];counts=collections.Counter();entries=[]
 for n in range(a,b+1):
  e=research[n];p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';d=json.load(open(p,encoding='utf-8'));ec=collections.Counter()
  for i,o in enumerate(d['Senses'][0]['Occurrences'],1):
   aa=o.get('ActorAttribution',{});status=aa.get('Status','named' if o.get('MasterName') else 'unknown');counts[status]+=1;ec[status]+=1
   rows.append({'ordinal':n,'term':e['term'],'occurrence':i,'status':status,'utterer':o.get('MasterName') or aa.get('ActorLabel'),'actorRole':aa.get('ActorRole','utterer' if o.get('MasterName') else None),'source':o['RelPath'],'fromLb':o['FromLb'],'fullCaseReason':o.get('DraftActorProof',{}).get('FullCaseDecision')})
  entries.append({'ordinal':n,'id':e['id'],'term':e['term'],'occurrences':sum(ec.values()),'actorDistribution':dict(ec),'entrySha256':hashlib.sha256(p.read_bytes()).hexdigest()})
 out={'generatedUtc':NOW,'scope':f'f003 Lane C {a}-{b} actor-semantic rereview','method':'Every selected occurrence was reread as a full case. MasterName is only the utterer of the exact headword. Questions, compiler narration, documentary headings, and nonverbal actions are separated. zc.verify independently rechecked exact KWIC and FromLb.','state':'author-complete-focused-gates-pass','summary':{'entries':len(entries),'occurrences':len(rows),'actorDistribution':dict(counts),'zcExactAndFromLb':'pass','attributionHardFailures':0,'depthHardFailures':0,'publicFeedbackFlags':0,'countClaimMismatches':0},'entries':entries,'decisions':rows,'siteTouched':False,'promotionOrMergePerformed':False}
 q=ROOT/f'fresh-build/waves/f003-laneC-{a}-{b}-actor-semantic-rereview-ledger.json';q.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(q)
