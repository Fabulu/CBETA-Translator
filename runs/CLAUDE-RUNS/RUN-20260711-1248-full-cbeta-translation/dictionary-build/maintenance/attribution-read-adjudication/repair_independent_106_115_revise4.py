import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];ids=['t_f56016646d8f','t_f4c65b25832f','t_f7c3da035832','t_fac9b9afebf6'];rows=[]
def unnamed(o,kind,label,role,evidence,note):
 o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,'RungsChecked':R,'GrammarEvidence':evidence,'ReviewedBy':'Codex independent 106-115 revise4','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['AttributionNote']=note
for i in ids:
 p=ROOT/'fresh-build/entries'/i/'entry.v2.json';old=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text(encoding='utf8'));s=d['Senses'][0];changes=[]
 if i=='t_f56016646d8f':
  a=s['Occurrences'][2]['ActorAttribution'];a['Status']='reviewed-unnamed';a['ActorLabel']='the unnamed visitor identified only as Nian in the Ciming exchange';changes.append('Changed role-only Nian visitor from identified-non-master to reviewed-unnamed with six rungs.')
 elif i=='t_f4c65b25832f':
  s['Occurrences'][6]['ActorAttribution']['ActorLabel']='the unnamed compiler-narrator responsible for the headword-bearing clause at J/J26/J26nB188.xml';changes.append('Made compiler label explicitly unnamed.')
 elif i=='t_f7c3da035832':
  unnamed(s['Occurrences'][2],'unnamed monastic questioner','the unnamed monk asking about the shout like the Diamond King’s precious sword','questioner','僧問 introduces the headword-bearing question; 師云 begins Feiyin Tongrong’s answer.','費隱禪師語錄: an unnamed monk asks what a shout like the Diamond King’s precious sword is.')
  s['Occurrences'][6]['ActorAttribution']['ActorLabel']='the unnamed compiler-narrator responsible for the headword-bearing clause at X/X84/X84n1583.xml';changes.append('Removed literal placeholder MasterName and made both unnamed actors explicit.')
 else:
  unnamed(s['Occurrences'][1],'unnamed monastic questioner','the unnamed monk asking about the shout as probing pole and shadowing grass','questioner','僧問 introduces the headword-bearing question; the following master turn answers it.','費隱禪師語錄: an unnamed monk asks what a shout like probing pole and shadowing grass is.')
  changes.append('Removed literal placeholder MasterName and represented the monk as reviewed-unnamed.')
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':d['SourceTerm'],'oldSha256':old,'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':changes})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-106-115-independent-revise4-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
