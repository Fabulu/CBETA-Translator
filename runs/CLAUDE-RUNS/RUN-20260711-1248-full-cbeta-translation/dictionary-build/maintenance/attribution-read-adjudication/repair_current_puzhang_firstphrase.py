#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];E=ROOT/'fresh-build'/'entries';rows=[]
for tid in ['t_84e490b1773f','t_d0d82a2681a0']:
 p=E/tid/'entry.v2.json';d=json.loads(p.read_text());before=hashlib.sha256(p.read_bytes()).hexdigest()
 if d['SourceTerm']=='撫掌':
  o=d['Senses'][0]['Occurrences'][7]
  o['AttributionNote']='宗鑑法林. Exact headword actor: the unnamed verse author. The explicitly presented anonymous verse imagines an immortal clapping and laughing; neither the contextual figures nor the compiler utters the action phrase.'
  change='Corrected occurrence 8 note from case narrator to the already structured unnamed verse author.'
 else:
  d['Senses'][0]['Explanation']='The first phrase is the opening verbal turn singled out for examination or contrasted with later phrases. Yuanwu Keqin states what follows when it is recognized in that first position, and Linji Yixuan’s material ranks phrases in a larger verbal structure. Since Yuanwu Keqin and Hanyue Fazang supply different surrounding formulations, “first” identifies a slot in an exchange, not one fixed sentence shared by every lineage.'
  change='Removed unsupported Muchen Conglang claim and corrected Yuanwu wording from asks to states, matching retained evidence.'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');rows.append({'id':tid,'term':d['SourceTerm'],'beforeSha256':before,'afterSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'change':change,'requiresIndependentRereview':True})
Path(__file__).with_name('current-puzhang-firstphrase-full-read-repair-ledger.json').write_text(json.dumps({'rows':rows},ensure_ascii=False,indent=2)+'\n')
