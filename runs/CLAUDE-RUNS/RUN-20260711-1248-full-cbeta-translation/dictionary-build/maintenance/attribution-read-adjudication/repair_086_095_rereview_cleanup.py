import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; ids=['t_a7f67b4983d9','t_a9f422b3b249'];rows=[]
for i in ids:
 p=ROOT/'fresh-build/entries'/i/'entry.v2.json';old=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text(encoding='utf8'))
 if i=='t_a7f67b4983d9':
  s=d['Senses'][0];s['SourceTexts']=list(dict.fromkeys(o['RelPath'] for o in s['Occurrences']));s['Note']='6 genuine occurrences delimit this sense.'
  s['Explanation']='Daily functioning is what occurs in the ordinary course of the day and in activities repeatedly carried out there. Speakers say it cannot be hidden, ask what the daily matter is, and locate it amid recurring work, perception, doubt, and dust-and-toil rather than outside them.'
  s['RelatedMasters']=sorted(set(o['MasterName'] for o in s['Occurrences'] if o.get('MasterName')))
  change='Removed all rejected boundary-collision wording; Note, Explanation, sources, and links now describe the six genuine 日用 rows only.'
 else:
  s3=d['Senses'][2];s3['SourceTexts']=[o['RelPath'] for o in s3['Occurrences']];s3['RelatedTerms']=['戀生緣'];s3['Status']='allowed';s3['Note']='One exact anonymous case verse anchors this distinct worldly/life-ties sense; it remains provisional.'
  d['Senses'][0]['Explanation']="A person's place or circumstance of origin. In public interviews Dongshan asks Lingyun where his 生緣 is, and Huanglong Huinan turns the same origin question into one of his three chamber checkpoints. Later records quote, versify, appraise, and answer that origin question. The corpus separately uses the same graphs for a condition of arising and for worldly or life-binding ties; those different things are split below."
  change='Completed 戀生緣 occurrence linkage (SourceTexts, RelatedTerms, allowed/provisional metadata). Jinsu Rong remains an explicit roster-only deferral, as authorized; no substitute identity was guessed.'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':d['SourceTerm'],'oldSha256':old,'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'change':change})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-086-095-rereview-cleanup-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
