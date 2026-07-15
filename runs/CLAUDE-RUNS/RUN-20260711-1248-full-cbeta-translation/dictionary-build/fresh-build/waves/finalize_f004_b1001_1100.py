#!/usr/bin/env python3
"""Combined immutable author handoff for f004 lane B ordinals 1001-1100."""
import datetime,hashlib,json,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;sys.path.insert(0,str(R));import zc
NOW=datetime.datetime.now(datetime.timezone.utc).isoformat()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
wave=json.loads((H/'f004.json').read_text());rows=[x for x in wave['entries'] if 1001<=x['ordinal']<=1100]
assert len(rows)==100 and [x['ordinal'] for x in rows]==list(range(1001,1101))
localp=H/'f004-laneB-1051-1100-lane-local-roster-packet.json';local=json.loads(localp.read_text())
sharedp=R/'fresh-build/pending-roster.json';shared=json.loads(sharedp.read_text()) if sharedp.exists() else {'candidates':[]}
view={'schemaVersion':1,'generatedUtc':NOW,'rule':'Read-only gate-scoped union; neither source roster is edited.','sharedRosterSource':str(sharedp.relative_to(R)) if sharedp.exists() else None,'sharedRosterSha256':sha(sharedp) if sharedp.exists() else None,'laneLocalSource':localp.name,'laneLocalSha256':sha(localp),'candidates':shared.get('candidates',[])+local.get('candidates',[])}
vp=H/'f004-laneB-1001-1100-gate-roster-view.json';vp.write_text(json.dumps(view,ensure_ascii=False,indent=2)+'\n')
entries=[];packets=[];total=exact=0;forbidden=[];statuses={};bad=[]
for row in rows:
 d=R/'fresh-build/entries'/row['id'];ep=d/'entry.v2.json';e=json.loads(ep.read_text());occ=[o for s in e['Senses'] for o in s['Occurrences']];total+=len(occ)
 ebad=[]
 for n,o in enumerate(occ,1):
  v=zc.verify(o['RelPath'],o['Kwic']);ok=bool(v.get('ok')) and row['term'] in o['Kwic'];exact+=int(ok)
  aa=o.get('ActorAttribution');status=aa.get('Status') if aa else ('named' if o.get('MasterName') else 'missing');statuses[status]=statuses.get(status,0)+1
  if not ok or status=='missing':ebad.append(n)
  packets.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'occurrence':n,'entrySha256':sha(ep),'RelPath':o['RelPath'],'FromLb':o['FromLb'],'ToLb':o.get('ToLb'),'Kwic':o['Kwic'],'exactVerified':ok,'MasterName':o.get('MasterName'),'ContextMasters':o.get('ContextMasters',[]),'ActorAttribution':aa,'AttributionNote':o.get('AttributionNote'),'DraftActorProof':o.get('DraftActorProof')})
 prose=json.dumps(e,ensure_ascii=False).lower()
 for word in ('buddhism','meditation','doctrinal'):
  if word in prose:forbidden.append({'ordinal':row['ordinal'],'term':row['term'],'word':word})
 entries.append({'ordinal':row['ordinal'],'id':row['id'],'term':row['term'],'occurrences':len(occ),'entrySha256':sha(ep),'badOccurrenceIndexes':ebad})
 if ebad:bad.append({'ordinal':row['ordinal'],'indexes':ebad})
pp=H/'f004-laneB-1001-1100-formal-gate-v3-attribution-packets.json';pp.write_text(json.dumps({'schemaVersion':3,'generatedUtc':NOW,'rosterViewSha256':sha(vp),'packets':packets},ensure_ascii=False,indent=2)+'\n')
hard=len(entries)==100 and total==595 and exact==total and not forbidden and not bad
gate={'schemaVersion':3,'generatedUtc':NOW,'wave':'f004','lane':'B','ordinals':[1001,1100],'entries':entries,'summary':{'entries':len(entries),'occurrences':total,'expectedOccurrences':595,'exactKwics':exact,'actorStatuses':statuses,'forbiddenEnglish':forbidden,'badEntries':bad,'rosterViewSha256':sha(vp),'attributionPacketsSha256':sha(pp)},'hardPass':hard,'selfReview':False,'independentSemanticActorReview':'required','promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
gp=H/'f004-laneB-1001-1100-combined-formal-gate-v3.json';gp.write_text(json.dumps(gate,ensure_ascii=False,indent=2)+'\n')
ledger={'schemaVersion':1,'generatedUtc':NOW,'wave':'f004','lane':'B','ordinals':[1001,1100],'entryHashes':{e['id']:e['entrySha256'] for e in entries},'checkpointSources':[p.name for p in sorted(H.glob('f004-laneB-*-author-checkpoint.json')) if any(str(n) in p.name for n in (1010,1020,1030,1040,1050,1060,1070,1080,1090,1100))],'combinedGateSha256':sha(gp),'hardPass':hard,'selfReview':False}
lp=H/'f004-laneB-1001-1100-combined-author-ledger.json';lp.write_text(json.dumps(ledger,ensure_ascii=False,indent=2)+'\n')
receipt={'schemaVersion':1,'generatedUtc':NOW,'wave':'f004','lane':'B','ordinals':[1001,1100],'entries':100,'occurrences':total,'exactKwics':exact,'gateSha256':sha(gp),'ledgerSha256':sha(lp),'rosterViewSha256':sha(vp),'attributionPacketsSha256':sha(pp),'hardPass':hard,'selfReview':False,'independentSemanticActorReview':'required','promotion':False,'merge':False,'siteTouched':False,'sharedRosterTouched':False}
rp=H/'f004-laneB-1001-1100-combined-author-receipt.json';rp.write_text(json.dumps(receipt,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'hardPass':hard,'entries':100,'occurrences':total,'exactKwics':exact,'gateSha256':sha(gp),'ledgerSha256':sha(lp),'receiptSha256':sha(rp),'packetsSha256':sha(pp),'rosterViewSha256':sha(vp)},ensure_ascii=False));sys.exit(0 if hard else 1)
