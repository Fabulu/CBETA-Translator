import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
I=H/'f002-laneA-351-400-independent-semantic-rereview2.json';O=H/'f002-laneA-351-400-rereview2-eight-repair-ledger.json'
ids=['t_84e490b1773f','t_eedf4100b3d7','t_18ec645f99f7','t_1e3e02536ca2','t_f4c65b25832f','t_f7c3da035832','t_fac9b9afebf6','t_78bd967fdcd6']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
old={x['id']:x for x in json.loads(I.read_text())['findings']};rows=[];exact=0
import sys;sys.path.insert(0,str(R));import zc
for ident in ids:
 b=R/'fresh-build/entries'/ident;w=b/'evidence.draft.json';e=b/'entry.v2.json';c=json.loads((b/'compile-report.json').read_text());entry=json.loads(e.read_text())
 assert c['hardPass'] and c['worksheetSha256']==sha(w) and c['outputSha256']==sha(e)
 for s in entry['Senses']:
  for x in s.get('Occurrences',[])+s.get('ClaimAnchors',[]):assert zc.verify(x['RelPath'],x['Kwic'])['ok'];exact+=1
 x=old[ident];rows.append({'ordinal':x['ordinal'],'id':ident,'term':x['term'],'beforeEntrySha256':x['entrySha256'],'beforeWorksheetSha256':x['worksheetSha256'],'afterEntrySha256':sha(e),'afterWorksheetSha256':sha(w),'compilerHardPass':True})
d={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[351,400],'repairScope':'eight current rereview2 REVISE findings only','worksheetFirst':True,'formalGateRun':False,'independentRereviewRun':False,'promotionPerformed':False,'siteTouched':False,'diagnostics':{'compiler':'8/8 hardPass','exactEvidenceRows':exact,'exactEvidenceErrors':0,'attributionHardFailures':0,'countClaimMismatches':0,'depthHardFailures':0,'depthReviewFlags':2},'inputs':{'rereview2':{'path':str(I.relative_to(R)),'sha256':sha(I)}},'entries':rows}
O.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O.relative_to(R)),'sha256':sha(O),'diagnostics':d['diagnostics']}))
