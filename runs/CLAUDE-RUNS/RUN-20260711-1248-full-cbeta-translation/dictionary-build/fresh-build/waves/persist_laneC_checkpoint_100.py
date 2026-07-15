import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
R=Path(__file__).resolve().parents[2];lp=R/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text());rp=R/'fresh-build/waves/f001-laneC-001-100-cohort.json';c=json.loads(rp.read_text());assert c['hardPass'] is True and len(c['entries'])==100
assert all(e['path'].startswith('fresh-build/entries/') and '/terms/' not in e['path'] for e in c['entries'])
ch={e['id']:e['sha256'] for e in c['entries']};hashes={}
gate={'rootReviewIntegrityExit':0,'attributionExit':0,'workSourceValidationExit':0,'corpusBaselineExit':0,'depthExit':0,'publicFeedbackExit':0,'exactKwicExit':0,'frozenHistoricalTermsExit':0,'attributionPacketsExit':0,'cohortGateExit':0}
for e in led['entries'][:100]:
 p=R/'fresh-build/entries'/e['id']/'entry.v2.json';h=hashlib.sha256(p.read_bytes()).hexdigest();assert h==ch[e['id']];hashes[e['id']]=h;e.update(state=(p.parent/'STATUS').read_text().strip(),entrySha256=h,gateReport=dict(gate),failures=[])
a=c['attribution']['payload']['counts'];d=c['depthSense']['payload'];f=c['publicFeedback']['payload']
counts={'entries':100,'senses':a['senses'],'occurrences':a['occurrences'],'exactKwicVerified':c['exactKwic']['verified'],'namedOccurrences':a['named_occurrences'],'contextMasterLinks':a['context_master_links'],'attributionNotes':a['attribution_notes'],'depthReviewFlagged':d['reviewFlagged'],'publicFeedbackPassing':f['passing'],'publicFeedbackFlagged':f['flagged']}
led.update(completed=100,nextId=None,nextTerm=None,updatedUtc=datetime.now(timezone.utc).isoformat());led['checkpoint']={'completed':100,'durable':True,'entrySha256s':hashes,'gateReport':gate,'counts':counts,'cohortReport':'fresh-build/waves/f001-laneC-001-100-cohort.json','depthReport':'fresh-build/waves/f001-laneC-001-100-depth-checkpoint.json','nextTerm':None}
lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n');print(json.dumps(counts,ensure_ascii=False))
