#!/usr/bin/env python3
"""Memory-bounded authoritative zc preflight for one lane slice."""
import argparse,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
from audit_depth_sense import evidence_floor
p=argparse.ArgumentParser();p.add_argument('--start',type=int,required=True);p.add_argument('--limit',type=int,default=10);a=p.parse_args()
wave=json.loads((ROOT/'fresh-build/waves/f001.json').read_text()); rows=[r for r in wave['entries'] if r.get('lane')=='C'][a.start:a.start+a.limit]
packet={'schemaVersion':1,'wave':'f001','lane':'C','start':a.start,'limit':a.limit,'corpusBaselineSha256':wave['corpusBaselineSha256'],'warning':'Authoritative zc discovery packet only; full-case reading and zc.verify remain mandatory.','entries':[]}
for row in rows:
 c=zc.count(row['term']); seen=set(); candidates=[]
 for rel,hits in c['per_file']:
  work=zc.work_id(rel)
  if work in seen:continue
  seen.add(work); candidates.append({'workId':work,'RelPath':rel,'fileHits':hits,'title':zc.title(rel),'windows':zc.find(rel,row['term'],ctx=96,limit=2)})
  if len(candidates)>=12:break
 packet['entries'].append({'id':row['id'],'term':row['term'],'lane':'C','hits':c['hits'],'files':c['files'],'works':c['works'],'evidenceFloor':evidence_floor(c['hits']),'depthRule':'Rejection floor only; harvest every unique deployment.','candidateWorks':candidates})
 zc._cache.pop('files',None)
out=ROOT/f"fresh-build/waves/f001-laneC-{a.start+1:03d}-{a.start+len(rows):03d}-preflight.json";out.write_text(json.dumps(packet,ensure_ascii=False,indent=2)+'\n');print(out)
