#!/usr/bin/env python3
"""Prepare exclusive f004 lane-B research artifacts; never edits other lanes."""
import datetime,hashlib,json
from pathlib import Path
HERE=Path(__file__).resolve().parent;ROOT=HERE.parent.parent
PRE=HERE/'f004-laneB-1001-1100-preflight.json';WAVE=HERE/'f004.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 p=json.loads(PRE.read_text());w=json.loads(WAVE.read_text());rows=[x for x in w['entries'] if 1001<=x['ordinal']<=1100]
 assert len(rows)==len(p['entries'])==100
 assert [(r['id'],r['term'],r['lane']) for r in rows]==[(x['id'],x['term'],'B') for x in p['entries']]
 assert len({r['id'] for r in rows})==len({r['term'] for r in rows})==100
 now=datetime.datetime.now(datetime.timezone.utc).isoformat();by={x['id']:x for x in p['entries']}
 own={'schemaVersion':1,'generatedUtc':now,'wave':'f004','lane':'B','exclusiveOrdinals':[1001,1100],'owner':'/root/f003_a651_700_independent_final','immutableManifest':{'path':'fresh-build/waves/f004.json','sha256':sha(WAVE)},'immutablePreflight':{'path':'fresh-build/waves/f004-laneB-1001-1100-preflight.json','sha256':sha(PRE)},'corpusBaselineSha256':p['corpusBaselineSha256'],'checkpoints':[{'ordinal':1050,'state':'pending'},{'ordinal':1100,'state':'pending'}],'earlyFive':[r['id'] for r in rows[:5]],'bulkAuthoringBlockedUntilEarlyFiveGreen':True,'rows':[{'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'state':'owned-awaiting-research'} for r in rows],'f003Touched':False,'otherLanesTouched':False,'promotion':False,'merge':False,'siteTouched':False}
 ws={'schemaVersion':1,'generatedUtc':now,'wave':'f004','lane':'B','ordinals':[1001,1100],'state':'occurrence-research-queued','entryFilesEdited':0,'requiredOrder':['exact concordance','complete case','exact utterer/context actor','canonical roster','work identity','sense boundary','claim anchors','prose'],'rows':[]}
 for r in rows:
  q=by[r['id']];ws['rows'].append({'ordinal':r['ordinal'],'id':r['id'],'term':r['term'],'state':'awaiting-full-case-research','preflightCounts':{'hits':q['hits'],'files':q['files'],'works':q['works'],'evidenceFloor':q['evidenceFloor']},'candidateWorks':q.get('candidateWorks',[]),'inferenceLedger':{'observation':[],'minimalInference':None,'ordinaryBridge':None,'falsificationSearches':[],'counterexamples':[],'scope':None,'verdict':None},'differentThingDecision':None,'selectedOccurrences':[],'actorPackets':[],'canonicalRosterDecision':None,'independentWorkIds':[],'claimAnchors':[],'semanticCanaries':[],'proseBlocked':True})
 structural={'schemaVersion':1,'generatedUtc':now,'hardPass':True,'wave':'f004','lane':'B','ordinals':[1001,1100],'checks':{'manifestRows':100,'preflightRows':100,'orderExact':True,'uniqueIds':True,'uniqueTerms':True,'laneExact':True,'corpusBaselineMatches':w['corpusBaselineSha256']==p['corpusBaselineSha256'],'existingEntryFiles':sum((ROOT/r['entryPath']).exists() for r in rows),'duplicateOrdinals':sum(r.get('duplicateOfOrdinal') is not None for r in rows)},'warning':'Structural admission is not semantic approval; discovery windows require complete-case reading and zc.verify.'}
 (HERE/'f004-laneB-1001-1100-ownership-ledger.json').write_text(json.dumps(own,ensure_ascii=False,indent=2)+'\n')
 (HERE/'f004-laneB-1001-1100-occurrence-research-worksheet.json').write_text(json.dumps(ws,ensure_ascii=False,indent=2)+'\n')
 (HERE/'f004-laneB-1001-1100-structural-preflight.json').write_text(json.dumps(structural,ensure_ascii=False,indent=2)+'\n')
 brief=f'''# f004 lane B brief — ordinals 1001–1100

Status: **exclusive ownership recorded; research queued; bulk authoring blocked on the early-five gate**

Immutable inputs: `f004.json` ({sha(WAVE)}), `f004-laneB-1001-1100-preflight.json` ({sha(PRE)}), corpus baseline `{p['corpusBaselineSha256']}` (494 files / 487 independent works). Scope is exactly lane B ordinals 1001–1100; f003 and f004 lanes A/C are excluded.

Evidence identity precedes prose: concordance → complete case → exact utterer/context roles → canonical roster or cohort-local evidence-hard candidate → independent work and case-family identity → different-things sense test → inference/falsification ledger → ordinary scene plus Chan bend → claim anchors/canaries → English-first reader prose. Discovery windows are not evidence; every saved KWIC must pass `zc.verify`. MasterName is only the utterer of the exact headword. Work support is counted by work_id, never files.

The early five are 家珍, 續燈錄, 鐵牛之機, 監寺, 陶淵明. They must clear compile, exact KWIC, strict roster, full-case actor, claim-anchor, depth, forbidden-English, and semantic-canary checks before bulk authoring. Durable checkpoints: 1050 and 1100. No self-review, promotion, merge, or deployment is authorized.
'''
 (HERE/'f004-laneB-1001-1100-brief.md').write_text(brief)
 print(json.dumps({'rows':100,'earlyFive':own['earlyFive'],'hardPass':True,'existingEntryFiles':structural['checks']['existingEntryFiles']}))
if __name__=='__main__':main()
