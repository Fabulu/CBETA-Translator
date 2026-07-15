#!/usr/bin/env python3
import json,subprocess
from pathlib import Path
import zc
R=Path(__file__).parent
# Yuansou's direct instruction.
p=R/'fresh-build/entries/t_240ea0594a5f/evidence.draft.json';d=json.loads(p.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
rel='X/X71/X71n1419.xml';kw='直到大休大歇、大安樂田地而已，即非向外別有一塵一法、一技一能、一言一句擔得將來誑謼於汝。';note='Recorded Sayings of Yuansou Xingduan (元叟行端禪師語錄), instruction requested by Layman Zhuoyin. Yuansou says the expedients aim only to have each person step back, clarify mind and see nature, and arrive at great rest, great cessation, and great ease—not carry in an external thing, skill, or phrase.';v=zc.verify(rel,kw)
if not v['ok']:raise SystemExit(v)
if not any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):
 s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'MasterName':'Yuansou Xingduan','DraftActorProof':{'ExactHeadwordClause':kw,'SpeechFrame':note,'FullCaseDecision':note}});s['SourceTexts'].append(rel);s['DraftEvidence']['IndependentWorkIds'].append(zc.work_id(rel))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
# Xutang case: the monk, not Xutang, utters the headword.
p2=R/'fresh-build/entries/t_27e66043b271/evidence.draft.json';d=json.loads(p2.read_text(encoding='utf8'));s=d['Entry']['Senses'][0]
rel='X/X82/X82n1571.xml';kw='僧問：聲前一句，不墮常機。轉位就功，如何相見？師曰：問訊不出手。';note='Complete Collection of the Five Lamps (五燈全書), Xutang Zhiyu section. An unnamed monk utters the headword in asking how the phrase before sound, not falling into the usual mechanism, is met when changing position and approaching function; Xutang answers, “a greeting without putting out a hand.”';v=zc.verify(rel,kw)
if not v['ok']:raise SystemExit(v)
if not any(o['RelPath']==rel and o['Kwic']==kw for o in s['Occurrences']):
 s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'AttributionNote':note,'ActorAttribution':{'Status':'reviewed-unnamed','Kind':'monk','ActorType':'reviewed-unnamed','ActorLabel':'unnamed monk','ActorRole':'questioner','RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'GrammarEvidence':'The explicit 僧問 speech frame assigns the headword-bearing question to a monk whose personal name is absent throughout the full case.','ContextEvidence':note},'ContextMasters':[{'MasterName':'Xutang Zhiyu','Roles':['respondent','section-subject']}],'DraftActorProof':{'GrammaticalSubject':'unnamed monk','FullCaseDecision':note}});s['SourceTexts'].append(rel);s['DraftEvidence']['IndependentWorkIds'].append(zc.work_id(rel))
p2.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
for p in [p,p2]:subprocess.run(['python3',str(R/'compile_evidence_draft.py'),str(p)],check=True)
