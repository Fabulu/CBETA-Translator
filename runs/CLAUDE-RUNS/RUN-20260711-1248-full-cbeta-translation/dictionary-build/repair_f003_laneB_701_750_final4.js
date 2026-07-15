const fs = require('fs');
const path = require('path');
const base = path.join(__dirname, 'fresh-build', 'entries');
const now = '2026-07-15T14:00:00Z';

function load(id) {
  const ep = path.join(base, id, 'entry.v2.json');
  const wp = path.join(base, id, 'evidence.draft.json');
  return { ep, wp, e: JSON.parse(fs.readFileSync(ep)), w: JSON.parse(fs.readFileSync(wp)) };
}
function save(x) {
  x.e.CreatedBy = 'Codex f003 B701-750 final4 exact-turn repair author';
  x.e.WrittenUtc = now;
  x.w.Entry.CreatedBy = x.e.CreatedBy;
  x.w.Entry.WrittenUtc = now;
  fs.writeFileSync(x.ep, JSON.stringify(x.e, null, 2) + '\n');
  fs.writeFileSync(x.wp, JSON.stringify(x.w, null, 2) + '\n');
}
function both(x, sense, occurrence, fn) {
  fn(x.e.Senses[sense].Occurrences[occurrence], false);
  fn(x.w.Entry.Senses[sense].Occurrences[occurrence], true);
}
function replaceRelated(x, oldName, newName) {
  for (const root of [x.e, x.w.Entry]) for (const sense of root.Senses) {
    if (sense.RelatedMasters) sense.RelatedMasters = sense.RelatedMasters.map(n => n === oldName ? newName : n);
  }
}

{
  const x = load('t_c212062774f9');
  both(x, 0, 0, (o, draft) => {
    delete o.MasterName;
    o.ContextMasters = [{ MasterName: 'Foyin Liaoyuan', Roles: ['person-discussed'] }];
    o.AttributionNote = 'Source text (五燈全書(第34卷-第120卷)): Su Shi utters the headword in his explicitly introduced verse (士乃作偈曰); Foyin Liaoyuan is the person addressed and discussed.';
    o.ActorAttribution = { Status: 'identified-non-master', Kind: 'lay authorial speech', ActorLabel: 'Su Shi', ActorRole: 'verse author', RungsChecked: ['line', 'expanded-context', 'section-header', 'book-title', 'tei-header', 'parallel-passage'], ReviewedBy: 'Codex f003 B701-750 final4 repair author', ReviewedUtc: now, GrammarEvidence: 'The explicit frame 士乃作偈曰 introduces Su Shi’s verse; the verse itself contains 借君四大作禪床.' };
    if (draft) o.DraftActorProof = { ExactHeadwordClause: '是故東坡不敢惜，借君四大作禪床。', GrammaticalSubject: 'Su Shi', SpeechFrame: '士乃作偈曰 explicitly introduces Su Shi’s verse.', FullCaseDecision: 'Su Shi, not Foyin Liaoyuan, utters the headword-bearing line.' };
  });
  save(x);
}
{
  const x = load('t_2da0e2fc0478');
  both(x, 0, 2, (o, draft) => {
    o.MasterName = 'Zhantang Wenzhun';
    o.ContextMasters = [{ MasterName: 'Zhantang Wenzhun', Roles: ['utterer'] }];
    o.AttributionNote = 'Source text (列祖提綱錄): Zhantang Wenzhun utters the headword in the explicitly headed rain-clearing hall address (湛堂準禪師祈晴，上堂).';
    delete o.ActorAttribution;
    if (draft) o.DraftActorProof = { ExactHeadwordClause: o.Kwic, GrammaticalSubject: 'Zhantang Wenzhun', SpeechFrame: '湛堂準禪師祈晴，上堂 explicitly opens the containing address.', FullCaseDecision: 'Zhantang Wenzhun is the exact utterer; Gaofeng Yuanmiao is not the section speaker.' };
  });
  replaceRelated(x, 'Gaofeng Yuanmiao', 'Zhantang Wenzhun');
  save(x);
}
{
  const x = load('t_298f7fdd14bd');
  both(x, 0, 0, (o, draft) => {
    delete o.MasterName;
    o.Kwic = '開爐日，以一力撾鼓陞座';
    o.ContextMasters = [{ MasterName: 'Fachang Yiyu', Roles: ['person-discussed'] }];
    o.AttributionNote = 'Source text (五燈全書(第34卷-第120卷)): the recorder supplies the event frame “on furnace-opening day”; Fachang Yiyu performs the subsequent action and speaks only after 曰.';
    o.ActorAttribution = { Status: 'narrated', Kind: 'compiler narrative', ActorLabel: 'the case recorder', ActorRole: 'compiler', RungsChecked: ['line', 'expanded-context', 'section-header', 'book-title', 'tei-header', 'parallel-passage'], ReviewedBy: 'Codex f003 B701-750 final4 repair author', ReviewedUtc: now, GrammarEvidence: '開爐日 is narrative event framing before the marked speech transition 曰.' };
    if (draft) o.DraftActorProof = { ExactHeadwordClause: '開爐日，以一力撾鼓陞座', GrammaticalSubject: 'the case recorder', SpeechFrame: 'The headword precedes 曰 and belongs to the event frame.', FullCaseDecision: 'The recorder owns 開爐日; Fachang Yiyu is the person acting in the narrated event.' };
  });
  save(x);
}
{
  const x = load('t_74390b40f658');
  both(x, 0, 4, (o, draft) => {
    o.MasterName = 'Tianning Qi';
    o.ContextMasters = [{ MasterName: 'Tianning Qi', Roles: ['utterer'] }, { MasterName: 'Mazu Daoyi', Roles: ['person-discussed'] }];
    o.AttributionNote = 'Source text (宗門拈古彙集): Tianning Qi utters the headword in the comment explicitly introduced by 天寧琦云; Mazu Daoyi is the master whose case he discusses.';
    delete o.ActorAttribution;
    if (draft) o.DraftActorProof = { ExactHeadwordClause: o.Kwic, GrammaticalSubject: 'Tianning Qi', SpeechFrame: '天寧琦云 explicitly introduces the containing comment.', FullCaseDecision: 'Tianning Qi, not Mazu Daoyi, utters the headword-bearing recommendation.' };
  });
  replaceRelated(x, 'Mazu Daoyi', 'Tianning Qi');
  save(x);
}
