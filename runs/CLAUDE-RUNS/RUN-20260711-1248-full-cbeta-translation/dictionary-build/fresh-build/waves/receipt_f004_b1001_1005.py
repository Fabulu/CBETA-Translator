import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=H/'f004-laneB-1001-1005-early-sample-formal-gate.json';d=json.loads(g.read_text());assert d['hardPass'] and d['exactKwic']['verified']==30
w=json.loads((H/'f004.json').read_text());rows=[]
for x in w['entries']:
 if 1001<=x['ordinal']<=1005:
  ep=R/x['entryPath'];wp=ep.parent/'evidence.draft.json';rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'contextsRead':len(json.loads(wp.read_text())['Entry']['Senses'][0]['Occurrences']),'worksheetSha256':sha(wp),'entrySha256':sha(ep),'compileHardPass':json.loads((ep.parent/'compile-report.json').read_text())['hardPass'],'status':'drafted-gate-green'})
l={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1001,1005],'decision':'early-five green; bulk authoring allowed','hardPass':True,'fullGateSha256':sha(g),'entries':rows,'exactKwics':30,'actorDecision':'complete-context single-token adjudication stored','senseDecision':'five term-specific different-thing and canary decisions stored','workDecision':'distinct work_id lists stored','sharedPendingRosterTouched':False,'promotion':False,'merge':False,'siteTouched':False,'f003Touched':False,'otherLanesTouched':False}
lp=H/'f004-laneB-1001-1005-early-sample-ledger.json';lp.write_text(json.dumps(l,ensure_ascii=False,indent=2)+'\n');rec={'schemaVersion':1,'hardPass':True,'ledgerSha256':sha(lp),'fullGateSha256':sha(g),'bulkAuthoringAllowed':True};(H/'f004-laneB-1001-1005-early-sample-receipt.json').write_text(json.dumps(rec,ensure_ascii=False,indent=2)+'\n');print(json.dumps(rec))
