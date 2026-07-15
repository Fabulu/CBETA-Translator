#!/usr/bin/env python3
import json,subprocess,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];sys.path.insert(0,str(R));import zc
G=json.loads((R/'fresh-build/waves/f003-laneB-751-800-formal-gate.json').read_text());by={751+i:x['id'] for i,x in enumerate(G['entries'])}
openings={
753:'This formula hands the public exchange to the listener and demands an adequate next response.',
756:'This formula dismisses the proposed explanation as unrelated to the matter being tested.',
759:'Patch-robed household names the collective professional standpoint claimed by Chan monks in public speech.',
764:'This formula demands the final handling of the matter after the preceding setup has been exhausted.',
778:'This question asks where one can escape the paired physical conditions of heat and cold.',
781:'The formula records a verdict that travel among teachers and inquiry have reached completion.',
784:'This direct interview question requires the respondent to say what Chan is.',
787:'The expression asks for the concrete point at which the presented thing functions.',
789:'The phrase records an immediate staff blow delivered when the visitor crosses the entrance.',
793:'In an exclamatory speech frame, the word appeals to heaven in shock, grief, or protest.'}
for n,eid in by.items():
 d=R/'fresh-build/entries'/eid;p=d/'evidence.draft.json';x=json.loads(p.read_text())
 if n in openings:x['Entry']['Senses'][0]['ExplanationParts']['CorpusEarnedOpening']=openings[n]
 if n==775:
  s=x['Entry']['Senses'][0];rel='C/C077/C077n1710.xml';kw='浴佛上堂指天指地逞嘍囉凌辱宗風罪過多惡水驀頭澆一杓';v=zc.verify(rel,kw);assert v['ok']
  s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'Curated':True,'MasterName':'Baiyun Shouduan','ContextMasters':[{'MasterName':'Baiyun Shouduan','Roles':['utterer']}],'AttributionNote':f"Source text ({zc.title(rel)}): Baiyun Shouduan gives this bathing-Buddha hall address.",'DraftActorProof':{'ExactHeadwordClause':kw,'GrammaticalSubject':'Baiyun Shouduan','SpeechFrame':'The headword marks a hall address within Baiyun Shouduan’s continuous record.','FullCaseDecision':'The complete record section names Baiyun Shouduan, and the bathing-Buddha hall-address boundary makes him the current speaker.'}})
  s['SourceTexts']=sorted({o['RelPath'] for o in s['Occurrences']});s['DraftEvidence']['OpeningClaimEvidenceKeys']=[f'o{i}' for i in range(1,len(s['Occurrences'])+1)];s['DraftEvidence']['IndependentWorkIds']=sorted({zc.work_id(o['RelPath']) for o in s['Occurrences']})
 if n==784:
  # Remove raw Chinese syntax words from the reader-facing note.
  for o in x['Entry']['Senses'][0]['Occurrences']:
   o['AttributionNote']=o['AttributionNote'].replace('The explicit 問 marker','The explicit question marker').replace('the master begins only at 師云','the master begins only with the separately marked reply')
 p.write_text(json.dumps(x,ensure_ascii=False,indent=2)+'\n');subprocess.run([sys.executable,str(R/'compile_evidence_draft.py'),str(p),'--output',str(d/'entry.v2.json'),'--report',str(d/'compile-report.json')],check=True,stdout=subprocess.DEVNULL)
print('gate fixes complete')
