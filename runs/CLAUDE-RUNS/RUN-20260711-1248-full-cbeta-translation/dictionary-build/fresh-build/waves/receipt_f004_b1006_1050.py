import datetime,hashlib,json
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=H/'f004-laneB-1006-1050-full-gate.json';gd=json.loads(g.read_text());assert gd['hardPass'] and gd['exactKwic']['failureCount']==0
w=json.loads((H/'f004.json').read_text());rows=[]
for x in w['entries']:
 if 1006<=x['ordinal']<=1050:
  ep=R/x['entryPath'];wp=ep.parent/'evidence.draft.json';rows.append({'ordinal':x['ordinal'],'id':x['id'],'term':x['term'],'entrySha256':sha(ep),'worksheetSha256':sha(wp),'occurrences':sum(len(s['Occurrences']) for s in json.loads(ep.read_text())['Senses']),'formalGate':'green','independentSemanticReview':'required'})
l={'schemaVersion':1,'generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'wave':'f004','lane':'B','ordinals':[1006,1050],'entries':45,'exactKwics':gd['exactKwic']['verified'],'fullGateSha256':sha(g),'hardPass':True,'rows':rows,'checkpointLedgers':[f'f004-laneB-{a}-{b}-author-checkpoint.json' for a,b in [(1006,1010),(1011,1020),(1021,1030),(1031,1040),(1041,1050)]],'selfReview':False,'promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
p=H/'f004-laneB-1006-1050-author-receipt.json';p.write_text(json.dumps(l,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'path':p.name,'sha256':sha(p),'entries':45,'exactKwics':l['exactKwics'],'hardPass':True}))
