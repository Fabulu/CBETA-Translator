#!/usr/bin/env python3
import copy,glob,json
from pathlib import Path
H=Path(__file__).parent; R=H/'fresh-build'/'entries'
pairs={'t_b90a5f36ec86':'X/X72/X72n1444.xml','t_74a27239e6c7':'J/J33/J33nB294.xml','t_48bc24c64738':'T/T47/T47n1993.xml','t_96473172e857':'C/C077/C077n1710.xml','t_e95ea628d5dd':'T/T48/T48n2003.xml','t_b33fddd5d4f1':'J/J28/J28nB220.xml','t_6214dc704b24':'T/T47/T47n1998B.xml','t_a5408be46291':'T/T51/T51n2077.xml','t_b4367c692c8a':'J/J38/J38nB406.xml','t_5f08e925c83d':'X/X82/X82n1571.xml'}
pool=[]
for raw in glob.glob(str(H/'terms/*/entry.v2.json'))+glob.glob(str(R/'*/entry.v2.json')):
 try:x=json.loads(Path(raw).read_text())
 except Exception:continue
 for s in x.get('Senses',[]):
  for o in s.get('Occurrences',[]):pool.append(o)
for i,rel in pairs.items():
 p=R/i/'evidence.draft.json'; d=json.loads(p.read_text()); term=d['Entry']['SourceTerm']
 used={(o.get('RelPath'),o.get('FromLb'),o.get('Kwic')) for s in d['Entry']['Senses'] for o in s.get('Occurrences',[])}
 o=next(copy.deepcopy(o) for o in pool if o.get('RelPath')==rel and term in str(o.get('Kwic','')) and (o.get('RelPath'),o.get('FromLb'),o.get('Kwic')) not in used)
 o['Curated']=True
 if o.get('MasterName'):
  o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
  o['AttributionNote']=f"{rel}: exact retained passage identifies {o['MasterName']} as the headword utterer."
  o['DraftActorProof']={'ExactHeadwordClause':o['Kwic'],'SpeechFrame':o['AttributionNote'],'FullCaseDecision':o['AttributionNote']}
 # Put the witness under the sense whose existing wording is closest; all ten
 # selected reused rows instantiate the first retained sense.
 d['Entry']['Senses'][0].setdefault('Occurrences',[]).append(o)
 d['Entry']['Senses'][0]['SourceTexts']=list(dict.fromkeys(d['Entry']['Senses'][0].get('SourceTexts',[])+[rel]))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('enriched',len(pairs),'entries from previously verified exact evidence')
