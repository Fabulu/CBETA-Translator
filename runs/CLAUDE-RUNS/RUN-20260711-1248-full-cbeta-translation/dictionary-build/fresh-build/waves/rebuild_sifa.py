import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];z=json.loads((ROOT/'terms/t_7ccccfa5fe9a/entry.v2.json').read_text());s=z['Senses'][0]
states=[
('impersonal','editorial title line','the record title metadata','Zhicao',['compiler'],'The headword occurs in the title’s editor/successor credit, not spoken discourse.'),
('impersonal','preface signature','the signed preface credit','Weilin Daopei',['compiler'],'The headword occurs in Daopei’s signed successor-disciple credit at the end of a preface.'),
('narrated','compiler biography','the lamp-record compiler','Minshu Xiang',['person-described'],'The compiler narrates that Minshu Xiang inherited transmission from Wanfeng Ming.'),
('narrated','stele biography','the inscription author','Wufeng Ruxue',['person-described'],'The inscription author narrates Wufeng Ruxue’s succession to Miyun Yuanwu.'),
('narrated','compiler biography','the lamp-record compiler','Tianyi Yihuai',['teacher','person-described'],'The compiler narrates that many people inherited Tianyi Yihuai’s lineage.'),
None,
('narrated','case commentary','the case-record compiler','Touzi Yiqing',['person-described'],'The compiler narrates Touzi Yiqing’s succession to Dayang Jingxuan.'),
]
for o,state in zip(s['Occurrences'],states):
 if state is None:o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}];continue
 status,kind,label,name,roles,grammar=state;o.pop('MasterName',None);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':'compiler','GrammarEvidence':grammar,'ReviewedBy':'Codex fresh lane-C complete-case review','ReviewedUtc':'2026-07-14T20:45:00Z'};o['ContextMasters']=[{'MasterName':name,'Roles':roles}];o['AttributionNote']=('Document heading/signature metadata. ' if status=='impersonal' else 'Compiler narration. ')+o['AttributionNote']
s.update(PreferredTarget='inherit lineage transmission',AlternateTargets=['succeed to the lineage','become a transmission heir'],SearchAliases=['inherit lineage','lineage successor','transmission heir','succeed teacher'],Explanation='To inherit lineage transmission is to be recorded as succeeding to a named teacher or as belonging among that teacher’s transmission heirs. The phrase occurs in biographies, succession statements, title credits by successor-disciples, and direct discussion of the consequences of succeeding a monastery founder. Narrated succession is attributed to its compiler while the successor and teacher remain contextual figures.',Note='The frozen corpus has 1,738 exact hits in 262 files representing 257 works. Seven anchors cover biography, inscription, case commentary, successor credits, collective heirs, and direct discussion across independent works.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_7ccccfa5fe9a';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n');(out/'WORK.md').write_text('''# 嗣法 research ledger
feedback-inference-verdict: direct
feedback-observations: succession predicates name an heir and often a teacher.
feedback-falsification-searches: title credits, biographies, direct speech, and unrelated inheritance.
feedback-counterexamples: title metadata is not utterance.
feedback-scope: corpus-wide lineage institution.
lookup-probes: 嗣法於; 嗣法者; 嗣法門人; 嗣法弟子.
opening-interpretation-verdict: direct institutional action.
definition-formula-results: checked succession formulas.
deployment-inventory: biography; inscription; title credit; direct discussion.
period-genre-spread: lamp, own record, commentary, inscription.
family-comparison: compared 嗣法門人 and standalone verb.
family-definition-retest: one succession referent.
omission-audit: unique deployment classes represented.
flyswatter: no hidden intention or symbolic claim.
inference-ledger: named succession predicates; ordinary inheritance warrant; direct verdict.
''')
