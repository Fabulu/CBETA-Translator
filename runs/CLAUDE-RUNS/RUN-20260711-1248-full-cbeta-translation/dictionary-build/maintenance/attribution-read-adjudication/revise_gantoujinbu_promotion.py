import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
B=Path(__file__).resolve().parents[2];E=B/'fresh-build'/'entries'/'t_5d8f2f79dc60'/'entry.v2.json';HERE=Path(__file__).parent
def sha():return hashlib.sha256(E.read_bytes()).hexdigest()
old=sha();d=json.loads(E.read_text());s=d['Senses'][0]
s['Note']='Frozen-corpus concordance: 123 exact hits in 87 storage files representing 86 independent works. Six exact witnesses plus one marked family verse cover case title, Changsha attribution, early lamp discourse, public interviews, and later commentary. Title, quotation, instruction, and question all name the same step from the same pole-top image, not different things.'
if 'T/T48/T48n2004.xml' not in s['SourceTexts']:s['SourceTexts'].append('T/T48/T48n2004.xml')
E.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');new=sha();p={'generatedUtc':datetime.now(timezone.utc).isoformat(),'entryId':d['Id'],'term':d['SourceTerm'],'disposition':'REVISE','oldSha256':old,'newSha256':new,'selfApproved':False,'promotionReady':False,'requiresIndependentReview':True,'findings':['Removed three duplicated copies of the frozen-corpus concordance sentence.','Added missing T/T48/T48n2004.xml SourceTexts pointer for the retained Wansong Xingxiu occurrence.']};(HERE/'gantoujinbu-promotion-review.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n');print(json.dumps(p,ensure_ascii=False))
