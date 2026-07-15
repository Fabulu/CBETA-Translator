import json,hashlib,datetime,os,tempfile,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';E=R/'fresh-build'/'entries';sys.path.insert(0,str(R));import zc
cfg=[(1201,'t_9808ec580b69','金屑落眼','KEEP',['All four full cases support one image: even fine gold dust obstructs sight when retained.','The prose correctly distinguishes Huangbo Wunian Shenyou, Huayan Shengke, Weilin Daopei, and the non-master participant Yuzhi; every quoted claim is contained by its stored occurrence.']),(1202,'t_130d20fb3834','眼裏著沙','REVISE',['Occurrence 1 stops after Baishui Benren’s paired maxim and does not store his requested explanation or the answer 應真無比 (“a worthy one without peer”), although the prose quotes that answer. Extend and re-verify o1 or add a separate anchored occurrence; do not delete the claim merely to clear the gate.','The remaining four cases support the exclusion/inclusion contrast and Daowu Wujin Wen’s transitive deployment; their actors and one-thing sense boundary hold.'])]
rows=[]
for n,i,t,v,findings in cfg:
 p=E/i/'entry.v2.json';e=json.loads(p.read_text());occ=[o for s in e['Senses'] for o in s['Occurrences']];checks=[]
 for o in occ:
  z=zc.verify(o['RelPath'],o['Kwic']);checks.append(bool(z.get('ok') and z.get('fromLb')==o['FromLb'] and z.get('toLb')==o['ToLb']))
 rows.append({'ordinal':n,'id':i,'term':t,'reviewedEntrySha256':hashlib.sha256(p.read_bytes()).hexdigest(),'occurrencesReadInFullCase':len(occ),'exactKwicsAndSpans':sum(checks),'verdict':v,'findings':findings})
out={'schemaVersion':1,'reviewType':'independent-full-case-canary-review','reviewer':'Codex independent reviewer','generatedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'entriesReviewed':2,'occurrencesReadInFullCase':sum(x['occurrencesReadInFullCase'] for x in rows),'exactKwicsAndSpans':sum(x['exactKwicsAndSpans'] for x in rows),'keep':1,'revise':1,'entries':rows,'reviewIntegrity':{'entriesEdited':False,'promoted':False,'merged':False}}
target=W/'f005-laneA-1201-1202-independent-review.json';fd,tmp=tempfile.mkstemp(prefix=target.name+'.',suffix='.tmp',dir=W)
with os.fdopen(fd,'w',encoding='utf-8') as f:json.dump(out,f,ensure_ascii=False,indent=2);f.write('\n');f.flush();os.fsync(f.fileno())
os.replace(tmp,target);print(hashlib.sha256(target.read_bytes()).hexdigest())
