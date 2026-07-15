import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
p=ROOT/'fresh-build/entries/t_32289452a85b/entry.v2.json'
old=hashlib.sha256(p.read_bytes()).hexdigest(); d=json.loads(p.read_text(encoding='utf-8')); os=d['Senses'][0]['Occurrences']
os[0]['AttributionNote']='Record of Shanhui Dashi (善慧大士錄): the named petitioners speak collectively as “we,” saying that they now cut their ears and burn fingers while urgently asking Shanhui Dashi to remain in the world.'
os[1]['ActorAttribution']={'Status':'impersonal','Kind':'quoted prescriptive scripture','ActorLabel':'the quoted Brahma Net scripture','ActorRole':'document voice','GrammarEvidence':'The headword occurs inside an explicitly introduced quotation from the Brahma Net scripture prescribing austerities, not in compiler narration or a human speech turn.','ReviewedBy':'Codex exact full-case rereview','ReviewedUtc':datetime.now(timezone.utc).isoformat()}
os[1]['AttributionNote']='Collection on the Convergence of All Good (萬善同歸集) explicitly quotes the Brahma Net scripture prescribing that a teacher explain austerities including burning the body, arms, and fingers; the quoted scripture is the impersonal document voice.'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); new=hashlib.sha256(p.read_bytes()).hexdigest()
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-066-075-burn-rereview-ledger.json'
out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':[{'id':d['Id'],'term':d['SourceTerm'],'oldSha256':old,'newSha256':new,'changes':['Corrected the petition occurrence note to assign the exact wording to the petitioners.','Reclassified the quoted Brahma Net prescription as an impersonal scripture voice, not compiler narration.']}]},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(old,new)
