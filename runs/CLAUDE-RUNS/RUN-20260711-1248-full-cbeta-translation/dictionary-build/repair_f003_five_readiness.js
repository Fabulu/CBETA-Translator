const fs = require('fs');
const base = 'runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build/fresh-build/entries';
function edit(id, fn) {
  const p = `${base}/${id}/evidence.draft.json`;
  const x = JSON.parse(fs.readFileSync(p, 'utf8'));
  fn(x.Entry.Senses[0]);
  x.Entry.WrittenUtc = '2026-07-15T10:15:00Z';
  x.Entry.CreatedBy = 'Codex f003 Lane A independent-reject corrective author';
  fs.writeFileSync(p, JSON.stringify(x, null, 2) + '\n');
}
edit('t_3279df87a9f1', s => {
  s.ExplanationParts.CorpusEarnedOpening = 'Household-ordinary marks what belongs to a house’s accustomed fare, talk, calculation, or formula—not necessarily what Chan approves as sufficient.';
  s.ExplanationParts.EvidenceBody = ["Kuang’an Shiyuan calls the saying ordinary household tea and rice, then explicitly says a patch-robed monk must know a special place besides it. Buhui uses the same meal phrase for what people wrongly find startling; other records apply the headword to customary reckoning, a routine celebration, and a house’s begging formula. The common thread is native or habitual usage, while each passage supplies its own praise, criticism, or contrast."];
  s.DraftEvidence.DifferentThingTest.Reason = 'Fare, talk, reckoning, and formula are contextual nouns governed by the same household-customary modifier; the corpus does not establish separate lexical referents, and Kuang’an’s contrast prevents equating “ordinary” with “sufficient.”';
});
edit('t_ad54a629dff9', s => {
  s.ExplanationParts.CorpusEarnedOpening = 'The compound names Gaofeng’s stated requirement to burn fingers and the crown as proof of an earnest, weighty mind.';
  s.ExplanationParts.EvidenceBody = ['Xueguan is the only exact witness: he reports that Gaofeng required people to burn fingers and crown and immediately glosses the demand as showing an earnest and weighty mind. The line states a requirement and its rationale; it supplies no completed burning, named performer, public vow ceremony, or institutional rule.'];
  s.DraftEvidence.DifferentThingTest.Reason = 'The sole witness denotes one paired bodily-burning demand. Its stated rationale is not a second sense, and a demanded act must not be rewritten as an accomplished event.';
});
edit('t_32289452a85b', s => {
  s.ExplanationParts.CorpusEarnedOpening = 'To burn a finger is literal bodily burning; the selected sources place it in distinct pleas, austerity lists, and disciplinary lists.';
  s.ExplanationParts.EvidenceBody = ['In Fu Dashi’s record, petitioners say they cut their ears and burn fingers while urgently pressing a request. The Records of Returning All Good lists finger burning beside burning the body and arm as austerities; the Admonitions text lists it beside burning incense in a legal or disciplinary sequence. Only the first witness ties it to that plea, so no single vow, offering, or institutional purpose is projected across all three.'];
  s.DraftEvidence.DifferentThingTest.Reason = 'All witnesses denote the same physical injury. Plea, austerity, and disciplinary-list settings are distinct deployments, not separate referents or a license to infer one universal vow function.';
});
edit('t_a805d0c76bbd', s => {
  s.Occurrences = s.Occurrences.filter(o => !(o.RelPath === 'J/J39/J39nB454.xml' && o.FromLb === '0607c30'));
  s.ExplanationParts.CorpusEarnedOpening = 'To take up incense is the ritual gesture that introduces the words or action attached to that incense in the particular ceremony.';
  s.ExplanationParts.EvidenceBody = ['Konggu Daocheng takes up incense and names its recipient before saying it will be burned. Elsewhere an official, a disciple, or a named teacher takes it up before a question, dedication, lineage statement, or compact verdict. The gesture is stable; its actor, audience, recipient, and purpose are source-specific. It is therefore not defined as invariably public, dedicatory, or restricted to named teachers, and it remains distinct from subsequently burning the incense.'];
  s.DraftEvidence.DifferentThingTest.Reason = 'The exact witnesses share the act of taking up incense. Their following speech acts differ by case but do not turn the gesture itself into multiple referents.';
  for (const o of s.Occurrences) {
    if (o.DraftActorProof?.FullCaseDecision?.includes('not a master')) o.DraftActorProof.FullCaseDecision = 'The actor is a non-roster participant, so no utterer link is assigned.';
    if (o.ActorAttribution?.Kind) o.ActorAttribution.Kind = o.ActorAttribution.Kind.replace('non-master','lay');
    if (o.ActorAttribution?.ActorLabel) o.ActorAttribution.ActorLabel = o.ActorAttribution.ActorLabel.replace('non-master','lay');
  }
  s.Occurrences[2].AttributionNote = 'Source text (古尊宿語錄): exact actor (石門山慈照禪師蘊聦) owns the headword-bearing wording and action.';
  s.Occurrences[3].AttributionNote = 'Source text (雪嶠信禪師語錄(第7卷-第10卷)): the named disciple Yu, a lay participant taking up incense to ask a question, is the exact actor and speaker.';
  s.Occurrences.push({
    RelPath:'J/J39/J39nB454.xml', FromLb:'0607c30', ToLb:'0608a01',
    Kwic:'白巖位老和尚訃至，上堂。師拈香入爐中，曰：「千鈞之弩已有人發了。看箭！」', Curated:true,
    ContextMasters:[{MasterName:'Pin Jixiang',Roles:['person-described']}],
    AttributionNote:'Source text (頻吉祥禪師語錄): the compiler narrates Pin Jixiang taking up incense and putting it into the burner before quoting his words.',
    ActorAttribution:{Status:'narrated',Kind:'compiler narrative',ActorLabel:'the compiler narrating Pin Jixiang’s action',ActorRole:'compiler',RungsChecked:['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],ReviewedBy:'Codex f003 Lane A independent-reject corrective author',ReviewedUtc:'2026-07-15T10:15:00Z',GrammarEvidence:'The headword occurs in narrative before the quotation marker; Pin Jixiang performs the act but does not utter the headword.'},
    DraftActorProof:{ExactHeadwordClause:'師拈香入爐中',GrammaticalSubject:'Pin Jixiang, described by the compiler',SpeechFrame:'Narration precedes the quotation marker and assigns the action to the record’s master.',FullCaseDecision:'The compiler utters the headword-bearing narration; Pin Jixiang is the named person described.'}
  });
  s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))];
  s.RelatedMasters=[...new Set(s.Occurrences.flatMap(o=>[o.MasterName,...(o.ContextMasters||[]).map(c=>c.MasterName)].filter(Boolean)))];
  s.DraftEvidence.OpeningClaimEvidenceKeys=s.Occurrences.map((_,i)=>`o${i+1}`);
  s.DraftEvidence.IndependentWorkIds=[...new Set(s.SourceTexts.map(p=>'work:'+p.split('/').pop().replace('.xml','')))];
});
edit('t_0ad6dd1a4717', s => {
  s.Occurrences = s.Occurrences.filter(o => !(o.RelPath === 'X/X63/X63n1245.xml' && o.FromLb === '0522c04') && !(o.RelPath === 'J/J39/J39nB454.xml' && o.FromLb === '0615b30'));
  s.Occurrences.push({
    RelPath:'J/J39/J39nB454.xml', FromLb:'0615b30', ToLb:'0615c01',
    Kwic:'元旦，上堂。「三十六旬之始，七十二候之初，但只燒香祝聖，不必者也之乎。', Curated:true,
    MasterName:'Pin Jixiang', ContextMasters:[{MasterName:'Pin Jixiang',Roles:['utterer']}],
    AttributionNote:'Source text (頻吉祥禪師語錄): Pin Jixiang utters the exact headword-bearing sentence in a New Year hall address.',
    DraftActorProof:{ExactHeadwordClause:'但只燒香祝聖，不必者也之乎。',GrammaticalSubject:'Pin Jixiang',SpeechFrame:'The record marks a New Year hall address and introduces the quoted sentence with the master speaking.',FullCaseDecision:'Pin Jixiang is the utterer of the exact headword-bearing sentence; the ceremonial actor is left implicit.'}
  });
  s.SourceTexts=[...new Set(s.Occurrences.map(o=>o.RelPath))];
  s.RelatedMasters=[...new Set(s.Occurrences.flatMap(o=>o.MasterName?[o.MasterName]:[]))];
  s.DraftEvidence.OpeningClaimEvidenceKeys = s.Occurrences.map((_,i)=>`o${i+1}`);
  s.DraftEvidence.IndependentWorkIds=[...new Set(s.SourceTexts.map(p=>'work:'+p.split('/').pop().replace('.xml','')))];
  s.ExplanationParts.CorpusEarnedOpening = 'To burn incense is a physical ceremonial act; Chan sources assign it as a duty, report it in halls, and quote it inside encounter speech.';
  s.ExplanationParts.EvidenceBody = ['Monastic regulations prescribe incense-burning tasks and occasions. In encounter records, Lingkan answers with “burn incense in the buddha hall,” while Pin Jixiang says simply to burn incense in blessing the sovereign. These sources establish the act but not one universal intention: honor, supplication, institutional duty, and verbal redeployment must be stated only where the individual passage supports them. A table-of-contents hit has been excluded.'];
  s.DraftEvidence.DifferentThingTest.Reason = 'The witnesses denote the same physical act. Institutional instruction and encounter-speech deployment are contextual uses, not distinct objects or a single inferred motive.';
});
