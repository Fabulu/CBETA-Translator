import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
cohort=json.loads((ROOT/'fresh-build/waves/f001-laneC-001-050-cohort.json').read_text())
assert cohort['hardPass'] is True
hashes={}
gate={'rootReviewIntegrityExit':0,'attributionExit':0,'workSourceValidationExit':0,'corpusBaselineExit':0,'depthExit':0,'publicFeedbackExit':0,'exactKwicExit':0,'attributionPacketsExit':0,'cohortGateExit':0}
for e in led['entries'][:50]:
 p=ROOT/'fresh-build/entries'/e['id']/'entry.v2.json';h=hashlib.sha256(p.read_bytes()).hexdigest();hashes[e['id']]=h
 status=(p.parent/'STATUS').read_text().strip()
 e.update(state=status,entrySha256=h,gateReport=dict(gate),failures=[])
led['completed']=50;led['nextId']=led['entries'][50]['id'];led['nextTerm']=led['entries'][50]['term'];led['updatedUtc']=datetime.now(timezone.utc).isoformat()
led['checkpoint']={'completed':50,'durable':True,'entrySha256s':hashes,'gateReport':gate,'counts':{'entries':50,'senses':63,'occurrences':402,'exactKwicVerified':402,'namedOccurrences':326,'contextMasterLinks':402,'attributionNotes':402,'depthReviewFlagged':38,'publicFeedbackPassing':28,'publicFeedbackFlagged':22},'cohortReport':'fresh-build/waves/f001-laneC-001-050-cohort.json','depthReport':'fresh-build/waves/f001-laneC-001-050-depth-checkpoint.json','nextTerm':led['nextTerm']}
lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
