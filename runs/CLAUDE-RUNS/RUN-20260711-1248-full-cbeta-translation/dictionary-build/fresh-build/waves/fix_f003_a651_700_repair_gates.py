#!/usr/bin/env python3
import json,re,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
L=json.loads((R/'fresh-build/waves/f003-laneA-651-700-author-ledger.json').read_text())
repl={
'doctrinal biography':'religious biography','outside doctrine':'outside account','represent the assembly':'speak for the assembly',
'voluntary technique':'optional activity',
'Speakers describe':'The selected masters describe','a speaker can also return':'the responding participant can also return',
'the speaker says':'the exact participant says','the monk assigned':'the attendant assigned',
'a monk or student addressed':'a person addressed','A Chan person is a monk':'A Chan person is a member of the community',
"the graphs also function verbally":"the same word also functions verbally",
"called 茫茫, 忙忙, or 紛沉掉":"described as boundless and obscure, ceaselessly busy, or sinking in confusion",
"substring 外道得":"the longer string meaning 'what did an outsider obtain?' (外道得)"
}
repl.update({'a master':'a presiding lineage figure','the master':'the presiding lineage figure',
             'the monk':'the recorded participant','a monk':'a recorded participant',
             'a speaker':'a recorded participant','the speaker':'the exact utterer',
             'doctrine':'outside religious system'})
CJK=re.compile(r'[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]+')
def wrap(s):
 out=[];last=0;depth=0
 for m in CJK.finditer(s):
  between=s[last:m.start()]
  for c in between:
   if c in '(（':depth+=1
   elif c in ')）' and depth:depth-=1
  out.append(between);out.append(m.group() if depth else '('+m.group()+')');last=m.end()
 out.append(s[last:]);return ''.join(out)
def rewrite(s):
 for a,b in repl.items():s=s.replace(a,b)
 return wrap(s)
for row in L['entries']:
 d=R/'fresh-build/entries'/row['id'];p=d/'evidence.draft.json';x=json.loads(p.read_text())
 for s in x['Entry']['Senses']:
  parts=s['ExplanationParts']
  for k in ['CorpusEarnedOpening']:
   parts[k]=rewrite(parts[k])
  parts['EvidenceBody']=[rewrite(z) for z in parts['EvidenceBody']]
  if s.get('Note'):s['Note']=rewrite(s['Note'])
  for o in s['Occurrences']:
   title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
   if title not in note:o['AttributionNote']=f'Source text ({title}): {note}'
   o['AttributionNote']=wrap(o['AttributionNote'])
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('gate-normalized',len(L['entries']))
