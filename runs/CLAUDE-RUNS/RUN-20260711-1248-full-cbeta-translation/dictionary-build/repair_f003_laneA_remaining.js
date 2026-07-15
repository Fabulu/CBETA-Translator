const fs = require('fs');
const path = require('path');

const root = path.join(__dirname, 'fresh-build', 'entries');
const now = '2026-07-15T09:30:00+02:00';

function load(id) {
  const p = path.join(root, id, 'evidence.draft.json');
  return { p, d: JSON.parse(fs.readFileSync(p, 'utf8')) };
}
function save(x) { fs.writeFileSync(x.p, JSON.stringify(x.d, null, 2) + '\n'); }
function named(o, name, source, proof) {
  delete o.ActorAttribution;
  o.MasterName = name;
  o.ContextMasters = [{ MasterName: name, Roles: ['utterer'] }];
  o.AttributionNote = `Source text (${source}): ${name} owns the exact headword-bearing wording.`;
  o.DraftActorProof = {
    ExactHeadwordClause: o.Kwic,
    GrammaticalSubject: name,
    SpeechFrame: proof,
    FullCaseDecision: proof
  };
}
function narrated(o, source, context = []) {
  delete o.MasterName;
  o.ActorAttribution = {
    Status: 'narrated', Kind: 'compiler narrative',
    ActorLabel: 'the compiler narrating the headword-bearing clause', ActorRole: 'compiler',
    RungsChecked: ['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],
    ReviewedBy: 'Codex f003 Lane A corrective author', ReviewedUtc: now,
    GrammarEvidence: 'The headword occurs in documentary narration rather than an isolated speech turn.'
  };
  o.ContextMasters = context.map(MasterName => ({ MasterName, Roles: ['person-described'] }));
  o.AttributionNote = `Source text (${source}): the compiler owns the documentary headword-bearing wording.`;
  o.DraftActorProof = { ExactHeadwordClause: o.Kwic, GrammaticalSubject: 'the compiler', SpeechFrame: 'Documentary narration.', FullCaseDecision: 'No master utterer is invented.' };
}

// 撥無因果: these four clauses occur in their named masters' own sermon records.
{
  const x = load('t_c95d0abb6577'); const os = x.d.Entry.Senses[0].Occurrences;
  named(os[1], 'Dahui Zonggao', '大慧普覺禪師普說', 'The surrounding formal discourse is Dahui Zonggao\'s speech turn.');
  named(os[2], '古庭禪師', '古庭禪師語錄輯略', 'The clause remains inside Gu Ting\'s sermon turn.');
  named(os[3], '古雪哲禪師', '古雪哲禪師語錄', 'The clause remains inside Gu Xue Zhe\'s sermon turn.');
  named(os[4], 'Juelang Daosheng', '天界覺浪盛禪師全錄', 'The clause remains inside Juelang Daosheng\'s sermon turn.');
  named(os[5], '廬山天然禪師', '廬山天然禪師語錄', 'The clause remains inside Lushan Tianran\'s sermon turn.');
  save(x);
}

// 炷香: keep the exact verb-object uses. Longer 一炷香 belongs to the dedicated phrase entry; the TOC is not evidence.
{
  const x = load('t_08c0c321eb2a'); const s = x.d.Entry.Senses[0];
  s.PreferredTarget = 'light incense';
  s.AlternateTargets = ['burn incense'];
  s.Status = 'provisional'; s.Validation = 'single-source';
  s.Occurrences = s.Occurrences.slice(0, 2);
  s.SourceTexts = [...new Set(s.Occurrences.map(o => o.RelPath))].sort();
  s.DraftEvidence.OpeningClaimEvidenceKeys = s.Occurrences.map((_, i) => `o${i+1}`);
  s.DraftEvidence.IndependentWorkIds = ['work:X63n1250', 'work:T48n2025'];
  s.DraftEvidence.DifferentThingTest = {
    Decision: 'one-thing',
    ComparedThings: ['the verb-object 炷香, to light incense', 'the counted phrase 一炷香, one stick or portion of incense'],
    Reason: 'The counted construction is a different grammatical unit and is handled in the separate 一炷香 entry; the table-of-contents hit is excluded.'
  };
  save(x);
}

// 發誓: the first witness is biography, not Xuefeng's utterance.
{
  const x = load('t_084fac11e2ac');
  narrated(x.d.Entry.Senses[0].Occurrences[0], '五燈全書(第34卷-第120卷)', ['福州雪峰亘信彌禪師']);
  save(x);
}

// 不動尊: one recurring case, with the monk asking and Muzhou answering.
{
  const x = load('t_5517bf8c66c2'); const s = x.d.Entry.Senses[0];
  s.Status = 'provisional'; s.Validation = 'single-source';
  named(s.Occurrences[1], 'Muzhou Daoming', '古尊宿語錄', 'The headword lies in the monk\'s question, but this long KWIC also stores Muzhou\'s separately marked answer; MasterName is removed below because the utterer of the headword is the monk.');
  const o = s.Occurrences[1]; delete o.MasterName;
  o.ActorAttribution = { Status:'reviewed-unnamed', Kind:'monastic questioner', ActorLabel:'the unnamed monk asking the recorded question', ActorRole:'questioner', RungsChecked:['line','expanded-context','section-header','book-title','tei-header','parallel-passage'], ReviewedBy:'Codex f003 Lane A corrective author', ReviewedUtc:now, GrammarEvidence:'The headword is in 問如何是不動尊; Muzhou owns the following 師云 answer, not the question.' };
  o.ContextMasters = [{ MasterName:'Muzhou Daoming', Roles:['respondent'] }];
  o.AttributionNote = 'Source text (古尊宿語錄): an unnamed monk asks the headword-bearing question; Muzhou Daoming gives the separately marked answer.';
  o.DraftActorProof.GrammaticalSubject = 'the unnamed monk';
  o.DraftActorProof.SpeechFrame = 'The headword lies before 師云 in the monk\'s question.';
  o.DraftActorProof.FullCaseDecision = 'Muzhou is respondent and context master, not utterer of the headword.';
  save(x);
}

// English-first and attribution-gate hygiene after the semantic repairs.
const replacements = {
  t_179e443ac255: [['discarding 不落 and guarding 不昧', 'discarding “not falling into causation” and guarding “not obscuring causation”']],
  t_7ede0e195d2b: [['The stored definitions of 遮詮', 'The stored definitions of expression by negation']],
  t_c95d0abb6577: [['The two stored 遮詮 controls', 'The two stored expression-by-negation controls'], ['do not themselves contain 無因無果', 'do not themselves contain the no-cause-and-no-effect formula']],
  t_08c0c321eb2a: [['Here 炷香 is either', 'Here the headword is either'], ['The longer phrase 一炷香', 'The longer counted phrase'], ['use 炷香 as', 'use the headword as'], ['the dedicated phrase 一炷香', 'the dedicated one-stick-of-incense phrase']],
  t_5517bf8c66c2: [['Five witnesses are recensions of the same unnamed monk’s question', 'Five witnesses are recensions of the same unnamed monastic interlocutor’s question']],
  t_0f4c2ed08d86: [['before a teacher’s stupa', 'before the named predecessor’s stupa']],
  t_11c14d7191f1: [['one master vows not to enter lay houses', 'one biographical subject vows not to enter lay houses']],
  t_d801848213ab: [['the master’s discourse rather than the monk’s earlier question', 'the sermon speaker’s discourse rather than the unnamed interlocutor’s earlier question']],
  t_084fac11e2ac: [['vow rebirth', 'vow toward the named destination']],
  t_6e57aada2121: [['apparent matches inside 雖然頂上, 躍然頂笠, and 自然頂門 have been excluded', 'three apparent character-boundary matches have been excluded'], ['Longer compounds and nearby family forms', 'Longer compounds and nearby forms']],
  t_8bbc811be86e: [['Three apparent hits inside 自然香 and 自然香潔 have been excluded', 'Three apparent character-boundary hits have been excluded'], ['the 自然香 strings', 'the excluded character-boundary strings']],
  t_32b7255009f7: [['is named explicitly by 天童覺云', 'is named explicitly in the speech frame']],
  t_8f91a9f06c79: [['doctrines or phrases', 'teachings or phrases']],
  t_9b56e29eddb6: [['電光石火', 'the lightning-and-spark expression'], ['機變', 'responsive change']],
  t_a9babbddf1a8: [['效顰', 'the copied-frown expression']],
  t_d1aa91b2b347: [['刻舟求劍', 'the mark-the-boat-to-seek-the-sword saying']],
  t_d27432779ce8: [['水月', 'the water-moon image'], ['空華', 'flowers in empty space'], ['永明云', 'the explicit Yongming speech frame']],
  t_e1b4c379b919: [['紅爐點雪', 'the snowflake-on-a-red-hot-furnace expression']],
  t_f9be0a6314a0: [['抱橋柱澡洗', 'washing while embracing a bridge pillar'], ['抱橋柱洗腳', 'washing the feet while embracing a bridge pillar']],
  t_18a76480bf9b: [['空華', 'flowers in empty space'], ['真說', 'true speech'], ['妄說', 'false speech'], ['水月', 'the moon in water']]
};
function replaceText(value, reps) {
  if (typeof value === 'string') {
    for (const [a,b] of reps) value = value.split(a).join(b);
    return value;
  }
  if (Array.isArray(value)) return value.map(v => replaceText(v, reps));
  if (value && typeof value === 'object') {
    for (const k of Object.keys(value)) value[k] = replaceText(value[k], reps);
  }
  return value;
}
for (const [id, reps] of Object.entries(replacements)) {
  const x = load(id);
  // Undo the earlier over-broad transform everywhere, restoring exact KWIC,
  // SourceTerm, names, and evidence metadata. Then apply English hygiene only
  // to reader prose and attribution notes.
  let raw = JSON.stringify(x.d);
  for (const [a,b] of reps) raw = raw.split(b).join(a);
  x.d = JSON.parse(raw);
  for (const sense of x.d.Entry.Senses) {
    sense.ExplanationParts = replaceText(sense.ExplanationParts, reps);
    for (const o of sense.Occurrences || []) {
      if (o.AttributionNote) o.AttributionNote = replaceText(o.AttributionNote, reps);
    }
    for (const a of sense.ClaimAnchors || []) {
      if (a.AttributionNote) a.AttributionNote = replaceText(a.AttributionNote, reps);
    }
  }
  save(x);
}
