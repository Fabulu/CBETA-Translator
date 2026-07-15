#!/usr/bin/env python3
import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
g=H/'f004-laneA-901-905-early-sample-formal-gate.json'; gate=json.loads(g.read_text()); assert gate['hardPass'] and gate['exactKwic']['verified']==32
w=json.loads((H/'f004.json').read_text()); rows=[]
for x in w['entries']:
 if 901<=x['ordinal']<=905:
  ep=R/x['entryPath']; wp=ep.parent/'evidence.draft.json'; cr=ep.parent/'compile-report.json'
  rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'contextsRead':sum(len(s['Occurrences']) for s in json.loads(wp.read_text())['Entry']['Senses']),'worksheetSha256':sha(wp),'entrySha256':sha(ep),'compileHardPass':json.loads(cr.read_text())['hardPass'],'status':'drafted-gate-green'})
l={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'A','ordinals':[901,905],'decision':'early-five green; author-side bulk continuation allowed','hardPass':True,'fullGateSha256':sha(g),'entries':rows,'exactKwics':32,'actorDecision':'complete cases read; exact headword utterers separated from respondents, context masters, editors, and narrators','senseDecision':'five term-specific different-thing decisions and counterexamples stored','workDecision':'distinct work_id support stored; repeated title occurrences do not inflate work count','laneLocalRosterView':'f004-laneA-901-905-gate-roster-view.json','sharedPendingRosterTouched':False,'promotion':False,'merge':False,'siteTouched':False,'f003Touched':False,'otherLanesTouched':False}
lp=H/'f004-laneA-901-905-early-sample-ledger.json';lp.write_text(json.dumps(l,ensure_ascii=False,indent=2)+'\n')
rec={'schemaVersion':1,'hardPass':True,'ledgerSha256':sha(lp),'fullGateSha256':sha(g),'bulkAuthoringAllowed':True,'nextOrdinal':906};(H/'f004-laneA-901-905-early-sample-receipt.json').write_text(json.dumps(rec,ensure_ascii=False,indent=2)+'\n');print(json.dumps(rec))
