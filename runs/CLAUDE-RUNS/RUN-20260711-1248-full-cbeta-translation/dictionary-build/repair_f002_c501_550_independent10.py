#!/usr/bin/env python3
import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
H=Path(__file__).parent;R=H/'fresh-build'/'entries';rows=[]
ids=['t_b90a5f36ec86','t_d4df8bc75ad7','t_e95ea628d5dd','t_6214dc704b24','t_3b3034d1731f','t_b986851dcdd8','t_b1c32bd93e66','t_75a477117870','t_4dd50050b279','t_d3dbc300bfac']
def ld(i):
 p=R/i/'evidence.draft.json';return p,json.loads(p.read_text())
for i in ids:
 p,d=ld(i);rows.append({'id':i,'term':d['Entry']['SourceTerm'],'beforeWorksheetSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'beforeEntrySha256':hashlib.sha256((p.parent/'entry.v2.json').read_bytes()).hexdigest()})
 s=d['Entry']['Senses'][0]
 if i=='t_b90a5f36ec86':s['AlternateTargets'].append('a stock rebuke for empty or fixed display');s['SearchAliases'].append('rebuke for empty display')
 elif i=='t_d4df8bc75ad7':s['AlternateTargets'].append('a formidable Chan fellow');s['SearchAliases'].extend(['formidable fellow','formidable Chan fellow'])
 elif i=='t_e95ea628d5dd':s['AlternateTargets'].append('become involved in entangling words or encounters for others');s['SearchAliases'].extend(['become involved for others','enter entangling words and encounters'])
 elif i=='t_6214dc704b24':s['ExplanationParts']['EvidenceBody']=[x.replace('attach attached','attach') for x in s['ExplanationParts']['EvidenceBody']]
 elif i=='t_3b3034d1731f':s['AlternateTargets'].append('joined in exceptionally close conjunction');s['SearchAliases'].extend(['joined in exceptionally close conjunction','exceptionally close conjunction'])
 elif i=='t_b986851dcdd8':
  bad={'before your parents gave birth to you','before you were born','prior to your birth'};s['AlternateTargets']=[x for x in s['AlternateTargets'] if x not in bad];s['SearchAliases']=[x for x in s['SearchAliases'] if x not in bad]
 elif i=='t_b1c32bd93e66':s['AlternateTargets'].append('the legendary goose that separates milk from water');s['SearchAliases'].extend(['goose that separates milk from water','selection and discrimination goose'])
 elif i=='t_75a477117870':
  routed=d['Entry']['Senses'].pop(1);(H/'fresh-build/waves/f002-laneC-542-zizhi-zang-roster-routing.json').write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'term':'紫芝','decision':'remove securely identified lineage master from lexical senses and route to master roster','rosterName':'Zizhi Zang','routedSense':routed,'siteTouched':False},ensure_ascii=False,indent=2)+'\n')
  s=d['Entry']['Senses'][0];s['DraftEvidence']['DifferentThingTest']={'Decision':'one-thing','ComparedThings':['purple fungus and the Purple Fungus Song'],'Reason':'The lineage master has been routed to the roster; the retained lexical sense covers the plant and the song named for it.'}
 elif i=='t_4dd50050b279':
  s=d['Entry']['Senses'][1];s['PreferredTarget']='pick up';s['AlternateTargets']=[];s['SearchAliases']=['pick up','pick up a found object'];s['ExplanationParts']['CorpusEarnedOpening']='As an ordinary verb, the same graphs describe picking up a found object.';s['DraftEvidence']['ZenBend']=s['ExplanationParts']['CorpusEarnedOpening']
 elif i=='t_d3dbc300bfac':s['SearchAliases']=[x for x in s['SearchAliases'] if x!='Laughing Buddha']
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
(H/'fresh-build/waves/f002-laneC-501-550-independent10-repair-work.json').write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'entries':rows,'siteTouched':False,'formalGateRun':False},ensure_ascii=False,indent=2)+'\n')
