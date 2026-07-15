#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent;p=R/'fresh-build/entries/t_19784084ccb4/evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
rel='X/X82/X82n1571.xml';kw='揚州建隆原禪師揚州建隆原禪師姑蘇夏氏子。上堂，拈拄杖曰：買帽相頭，依模畫樣。從他野老自顰眉，誌公不是閑和尚。';note='Complete Collection of the Five Lamps (五燈全書), Yangzhou Jianlong Yuan section. Jianlong Yuan ascends the hall, raises his staff, and says that while the rustic may knit his brows at buying a cap by measuring the head and copying a model, Zhigong is not an idle monk.';v=zc.verify(rel,kw)
if not v['ok']:raise SystemExit(v)
if not any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):
 s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':'Jianlong Yuan','ContextMasters':[{'MasterName':'Baozhi','Roles':['person-discussed']}],'DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}});s['SourceTexts'].append(rel);s['DraftEvidence']['IndependentWorkIds'].append(zc.work_id(rel))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
