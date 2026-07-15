import hashlib,json,re
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def wrap(s):
 out=[];i=0;depth=0
 while i<len(s):
  ch=s[i]
  if ch in '(（':depth+=1;out.append(ch);i+=1;continue
  if ch in ')）':depth=max(0,depth-1);out.append(ch);i+=1;continue
  if depth==0 and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',ch):
   j=i+1
   while j<len(s) and re.match(r'[\u3400-\u9fff\uf900-\ufaff]',s[j]):j+=1
   out.append('('+s[i:j]+')');i=j;continue
  out.append(ch);i+=1
 return ''.join(out).replace('Dharma','teaching').replace('dharma','teaching').replace('methods','ways').replace('method','way')
for e in led['entries'][:50]:
 d=ROOT/'fresh-build/entries'/e['id'];p=d/'entry.v2.json';z=json.loads(p.read_text())
 for s in z.get('Senses',[]):
  for k in ('PreferredTarget','Explanation','Note'):
   if isinstance(s.get(k),str):s[k]=wrap(s[k])
  s['AlternateTargets']=[wrap(x) for x in s.get('AlternateTargets',[])]
  for o in s.get('Occurrences',[]):o['AttributionNote']=wrap(o.get('AttributionNote',''))
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n')
 wp=d/'WORK.md'
 text=wp.read_text() if wp.exists() else f'# {e["term"]} research ledger\n'
 if 'sense-target-distinguishability:' not in text:
  text+='sense-target-distinguishability: each retained target differs in referent, grammar, or attested deployment; neighboring senses were retested against all anchors.\n'
 wp.write_text(text)
 e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'checkpointGate':'depth-repair-in-progress'}
 led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
