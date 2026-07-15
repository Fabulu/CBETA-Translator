import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent;I=H/'f002-laneA-351-400-independent-semantic-rereview3.json';O=H/'f002-laneA-351-400-rereview3-two-repair-ledger.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
old={x['id']:x for x in json.loads(I.read_text())['findings']};rows=[]
for ident in ['t_f4c65b25832f','t_78bd967fdcd6']:
 b=R/'fresh-build/entries'/ident;e=b/'entry.v2.json';w=b/'evidence.draft.json';c=json.loads((b/'compile-report.json').read_text());assert c['hardPass'] and c['outputSha256']==sha(e) and c['worksheetSha256']==sha(w);x=old[ident];rows.append({'ordinal':x['ordinal'],'id':ident,'term':x['term'],'beforeEntrySha256':x['entrySha256'],'beforeWorksheetSha256':x['worksheetSha256'],'afterEntrySha256':sha(e),'afterWorksheetSha256':sha(w),'compilerHardPass':True})
d={'generatedUtc':datetime.now(timezone.utc).isoformat(),'wave':'f002','lane':'A','ordinals':[351,400],'repairScope':'rereview3 residual ordinals 386 and 399','worksheetFirst':True,'formalGateRun':False,'independentRereviewRun':False,'siteTouched':False,'diagnostics':{'compiler':'2/2 hardPass','attributionHardFailures':0,'countClaimMismatches':0,'depthHardFailures':0},'entries':rows};O.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'output':str(O.relative_to(R)),'sha256':sha(O)}))
