import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];z=json.loads((ROOT/'terms/t_b191c4fa2e9f/entry.v2.json').read_text());s=z['Senses'][0]
closed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
for o in s['Occurrences']:
 if o['RelPath']=='X/X84/X84n1583.xml':
  o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler biography','ActorLabel':'the biographical compiler','ActorRole':'compiler','GrammarEvidence':'The compiler narrates that Yun’an Denggu requested further instruction from Wansong; neither master utters the stored narrative wording.','ReviewedBy':'Codex fresh lane-C complete-case review','ReviewedUtc':'2026-07-14T20:10:00Z'};o['ContextMasters']=[{'MasterName':'Yunan Denggu','Roles':['person-described','student']},{'MasterName':'Wansong Xingxiu','Roles':['teacher']}];o['AttributionNote']='Orthodox Continuation of the Lamp (續燈正統): the compiler narrates Yun’an Denggu requesting further instruction from Wansong Xingxiu; the wording is biography, not either participant’s utterance.'
 elif o['RelPath']=='J/J10/J10nA158.xml':
  o.pop('MasterName',None);o['ActorAttribution']={'Status':'narrated','Kind':'compiler biography','ActorLabel':'the record compiler','ActorRole':'compiler','GrammarEvidence':'The compiler narrates Miyun Yuanwu’s repeated requests for further instruction from Longchi Huanyou.','ReviewedBy':'Codex fresh lane-C complete-case review','ReviewedUtc':'2026-07-14T20:10:00Z'};o['ContextMasters']=[{'MasterName':'Miyun Yuanwu','Roles':['person-described','student']},{'MasterName':'Longchi Huanyou','Roles':['teacher']}];o['AttributionNote']='Recorded Sayings of Miyun (密雲禪師語錄): the compiler narrates Miyun Yuanwu requesting further instruction from Longchi Huanyou; the line is biography rather than Miyun’s utterance.'
 elif o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
 else:
  cs=[]
  for c in o.get('ContextMasters') or []:
   if isinstance(c,str):cs.append({'MasterName':c,'Roles':['respondent']})
   elif isinstance(c,dict) and c.get('MasterName'):cs.append({'MasterName':c['MasterName'],'Roles':[r for r in c.get('Roles',[]) if r in closed] or ['respondent']})
  o['ContextMasters']=cs;a=o.get('ActorAttribution') or {}
  if a.get('ActorRole') not in closed:a['ActorRole']='questioner'
s.update(PreferredTarget='request further instruction',AlternateTargets=['ask for clarification','seek additional instruction'],SearchAliases=['request further instruction','ask clarification','seek instruction','follow-up question'],Explanation='To request further instruction is to approach an instructor for additional clarification or testing after an initial encounter, statement, or partial understanding. Records use it for narrated visits, a monk explicitly announcing his purpose, an attendant following up after an exchange, and a named type of question in encounter analysis. The grammatical forms all denote the same act of seeking more instruction.',Note='The frozen corpus has 1,949 exact hits in 314 files representing 310 works. Eight anchors cover direct requests, narrated teacher-student visits, follow-up questioning, and explicit classification of the request-for-instruction question across independent works.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_b191c4fa2e9f';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
(out/'WORK.md').write_text('''# 請益 research ledger
feedback-inference-verdict: direct
feedback-observations: exact cases show requests for additional instruction.
feedback-falsification-searches: title-only, ordinary benefit, nested compounds, and contradictory uses.
feedback-counterexamples: biographical narration is not participant speech.
feedback-scope: corpus-wide institutional action.
lookup-probes: 請益問; 特來請益; 隨後請益; 屢請益.
opening-interpretation-verdict: direct plain-English action.
definition-formula-results: checked named question classifications and direct requests.
deployment-inventory: direct request; narrated visit; attendant follow-up; classified question.
period-genre-spread: own records, lamp compilations, and case commentary.
family-comparison: 請益問 denotes the same request action.
family-definition-retest: keep one referent.
omission-audit: unique deployment classes represented.
flyswatter: no intention, symbolism, or psychology asserted.
inference-ledger: exact action predicates; ordinary request semantics; direct verdict.
''')
