import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
G=H/'f002-laneB-401-450-gate.json';O=H/'f002-laneB-401-450-provisional-independent-semantic-review.json';N=H/'f002-laneB-401-450-provisional-independent-semantic-rereview.json';F=H/'f002-laneB-444-independent-semantic-rereview.json';P=H/'f002-laneB-401-450-consolidated-independent-semantic-review.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=json.loads(G.read_text());o=json.loads(O.read_text());n=json.loads(N.read_text());f=json.loads(F.read_text())
assert g['hardPass'] and len(g['entries'])==50
old={x['id']:x for x in o['findings'] if x['verdict']=='KEEP'};new={x['id']:x for x in n['findings'] if x['provisionalRereviewVerdict']=='KEEP'};new[f['findings'][0]['id']]=f['findings'][0]
rows=[]
for ordinal,x in zip(range(401,451),g['entries']):
 r=new.get(x['id']) or old.get(x['id']);assert r and r['entrySha256']==x['sha256'] and r.get('verdict',r.get('provisionalRereviewVerdict'))=='KEEP'
 assert sha(Path(x['path']))==x['sha256']
 e=json.loads(Path(x['path']).read_text());s=e['Senses']
 rows.append({'ordinal':ordinal,'id':x['id'],'term':x['term'],'path':str(Path(x['path']).relative_to(R)),'entrySha256':x['sha256'],'verdict':'KEEP','finding':r.get('finding') or r.get('reviewNotes'),'senseCount':len(s),'occurrenceCount':sum(len(y.get('Occurrences',[])) for y in s),'claimAnchorCount':sum(len(y.get('ClaimAnchors',[])) for y in s)})
d={'schemaVersion':1,'reviewType':'consolidated-exact-hash-independent-semantic-review','wave':'f002','lane':'B','ordinals':[401,450],'reviewedUtc':datetime.now(timezone.utc).isoformat(),'state':'independent-KEEP-at-exact-current-hashes','readOnly':True,'entryEditsMade':False,'promotionPerformed':False,'siteTouched':False,'formalGate':{'path':str(G.relative_to(R)),'sha256':sha(G),'hardPass':True},'inputs':[{'path':str(x.relative_to(R)),'sha256':sha(x)} for x in (O,N,F)],'verification':{'exactHashMatches':50,'allCurrentHashesHaveIndependentKEEP':True},'summary':{'entries':50,'KEEP':50,'REVISE':0},'findings':rows}
P.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(P.relative_to(R)),'sha256':sha(P),'KEEP':50}))
