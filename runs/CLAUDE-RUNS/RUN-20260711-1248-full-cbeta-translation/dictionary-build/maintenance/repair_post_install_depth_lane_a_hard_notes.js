const fs = require('fs');
const path = require('path');

const build = path.resolve(__dirname, '..');
const edits = {
  t_8a016f49e5b8: [
    [' in 五燈會元 owns', ' in the Five Lamps Compendium owns'],
    [', in 五燈會元, owns', ', in the Five Lamps Compendium, owns'],
    [' in 景德傳燈錄 owns', ' in the Jingde Record of the Transmission of the Lamp owns'],
    [', in 景德傳燈錄, owns', ', in the Jingde Record of the Transmission of the Lamp, owns'],
  ],
  t_d2c3f40d45c6: [['asking with 大通智勝佛', 'asking about Buddha Great Penetrating Wisdom Victory']],
  t_2ea5c21c08e0: [['asking with 盧舍那', 'asking about Vairocana']],
  t_b6906635439a: [['asking with 一問一答', 'asking about one question and one answer']],
  t_368268e023e3: [[', section 越州雲門靈侃禪師:', ':'], ['and 上堂 speech frame', 'and hall-address speech frame']],
  t_811316de4c5f: [['In the 第34卷-第120卷 volume range;', 'In volumes 34–120;']],
  t_a301d1290a7a: [['僧問 assigns', 'The “a monk asked” frame assigns']],
  t_cd69e0f9c10a: [['after 師曰.', 'after the “the master said” marker.']],
  t_f69bf9de345e: [['Dharma Grove of the Tradition’s Mirror', 'Teaching Grove of the Tradition’s Mirror']],
  t_12f74718424d: [['問 assigns', 'The question marker assigns']],
  t_32f0847e5d1e: [['asking with 佛向上事', 'asking about the matter beyond buddha']],
  t_6c9d50dc8f55: [['asking with 鐘板', 'asking about the bell and board']],
  t_7c5f24652dfa: [['Dharma Grove of the Tradition’s Mirror', 'Teaching Grove of the Tradition’s Mirror']],
  t_bf67613e4573: [['In the 第34卷-第120卷 volume range;', 'In volumes 34–120;']],
  t_df2096b961c1: [['explicitly headed上堂', 'explicitly headed hall address']],
  t_efc6a42814ee: [['with (師) and 曰.', 'with the “master” and “said” markers.']],
};

function replaceExact(text, before, after, id) {
  if (!text.includes(before)) throw new Error(`${id}: expected text missing: ${before}`);
  return text.split(before).join(after);
}

for (const [id, replacements] of Object.entries(edits)) {
  const file = path.join(build, 'fresh-build', 'entries', id, 'evidence.draft.json');
  const payload = JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, ''));
  for (const sense of payload.Entry.Senses || []) {
    for (const occurrence of sense.Occurrences || []) {
      let note = occurrence.AttributionNote || '';
      for (const [before, after] of replacements) {
        if (note.includes(before)) note = replaceExact(note, before, after, id);
      }
      occurrence.AttributionNote = note;
    }
  }
  fs.writeFileSync(file, JSON.stringify(payload, null, 2) + '\n', 'utf8');
}

for (const [id, removeIndex] of [['t_600aa5eb8aee', 3], ['t_6293dead3bb2', 7]]) {
  const file = path.join(build, 'fresh-build', 'entries', id, 'evidence.draft.json');
  const payload = JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, ''));
  const occurrences = payload.Entry.Senses[0].Occurrences;
  occurrences.splice(removeIndex, 1);
  payload.Entry.Senses[0].Note = payload.Entry.Senses[0].Note
    .replace(/^8 exact/, '7 exact')
    .replace(/^10 exact/, '9 exact');
  fs.writeFileSync(file, JSON.stringify(payload, null, 2) + '\n', 'utf8');
}
