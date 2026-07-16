# b009 Batch B research report

Completed the five assigned entries only. All use one corpus-wide `SenseKey: null` sense, place the Chan-record usage first, and qualify as `multi-source` from independent Chinese texts. No `STATUS`, manifest, wave-plan, termbase, translation, or instruction file was changed.

## 放行 (`t_561817f02529`)

- Corpus: 1,034 hits / 224 allowlisted texts.
- Entry: 1 sense, 6 curated occurrences, 6 source texts.
- Main sources: *Continuation of the Lamp from the Jianzhong Jingguo Era*, *Continued Record of the Lamp*, Yinyuan Longqi's record, the Northern Travel Collection of Tiantong Hongjue Min, Yuanwu Keqin's record, and Wumen's case collection.
- Depth: explicit `把定`/`把住` versus `放行` clauses; `收來`/`收放`; the question `如何是放行一句`; `放行一路`; compact `把定放行`; later case commentary.
- Self-definitions: all prescribed formulas searched. No equation-style definition found; the two `放行者` hits are predicates rather than definitions.
- Verification: 6/6 KWICs `ok=True`; all anchors synchronized and all KWICs contain the headword.

## 擔荷 (`t_efa1e241a7f0`)

- Corpus: 868 hits / 243 allowlisted texts.
- Entry: 1 sense, 7 curated occurrences, 5 source texts.
- Main sources: Yuanwu Keqin's record, *Records of Ancient Worthies*, Sanyi Yu's record, *Five Lamps Compendium*, and *Complete Book of the Five Lamps*.
- Depth: `擔荷得`, `全身擔荷`, `一肩擔荷`, `承當擔荷`, the reversed variant `荷擔`, invitation and inability deployments.
- Self-definition: included both halves of Jiufeng Daoqian's explicit classification—`承當擔荷` as `內紹`, followed by the contrast `無承當`/`無擔荷` before `同一色`. No other equation-style definition was found.
- Verification: 7/7 KWICs `ok=True`; all anchors synchronized and all KWICs contain the headword.

## 那畔 (`t_dc81acde25fd`)

- Corpus: 786 hits / 220 allowlisted texts.
- Entry: 1 sense, 5 curated occurrences, 5 source texts.
- Main sources: the records of Faxi Yin, Yunwai Ze, Cishou Huaishen, Yuanjie Ying, and Zhe'an Fan.
- Depth: direct `那畔` versus `者邊`/`遮畔` exchanges; matter, person, saying, road, and news noun formations; `威音那畔` (419 hits / 161 texts) represented in two distinct address shapes.
- Self-definitions: all prescribed formulas searched. No Chinese Chan equation-style definition found. A definition-form occurrence from outside the Chinese textual scope was inventoried and explicitly excluded.
- Verification: 5/5 KWICs `ok=True`; all anchors synchronized and all KWICs contain the headword.

## 一歸何處 (`t_9a7a00ea0cd1`)

- Corpus: 755 hits / 203 allowlisted texts.
- Entry: 1 sense, 6 curated occurrences, 6 source texts.
- Main sources: Yuanwu Keqin's comments, Hongzhi Zhengjue's record, *Lamp Record*, Gaofeng Yuanmiao's record, *Complete Book of the Five Lamps*, and *Draft Continuation of the Lamp*.
- Depth: Zhaozhou's Qingzhou cloth shirt answer; Wenshu's Yellow River answer; Baizhang Mingzhao's answer; an entire address consisting of the question; room testing; later alternative answer. This prevents reduction to Zhaozhou alone.
- Self-definitions: all prescribed formulas searched; none found. The famous Zhaozhou association was kept corpus-wide rather than mis-keyed as his private meaning.
- Verification: 6/6 KWICs `ok=True`; all anchors synchronized and all KWICs contain the headword.

## 休歇 (`t_da6965508721`)

- Corpus: 698 hits / 194 allowlisted texts.
- Entry: 1 sense, 7 curated occurrences, 6 source texts.
- Main sources: Zhenxie Qingliao's record, *Linked Lamps Compendium*, both lamp records, Dahui Zonggao's record, and Miyun Yuanwu's record.
- Depth: imperative, predicate question, `休歇處` and `休歇地` noun formations, Qingyuan Weixin's mountains/waters address, the second-ancestor retrospective, and direct assembly questioning.
- Self-definition/contrast: included Zhenxie Qingliao's explicit rejection of calling empty stillness and dead-tree lifelessness a `休歇處`; also included the lamp-record question and concrete stove/sleep answer. No unqualified equation-style definition was found.
- Verification: 7/7 KWICs `ok=True`; all anchors synchronized and all KWICs contain the headword.

## Final batch gates

- JSON: 5/5 parse; every `Id` equals the deterministic source-term hash and directory name.
- Occurrences: 31/31 `zc.verify(...).ok == True`; 31/31 verifier lbs match saved `FromLb`/`ToLb`; 31/31 contain their exact headword.
- Attribution: `zc.head` and `zc.title` checked for every saved occurrence; raised/multi-speaker material left unkeyed; every non-null master link is an exact roster canonical name.
- Sources: every `SourceTexts` value was re-queried and attests its entry headword.
- Prose: zero Chinese runs outside parentheses in all audited schema prose; zero hard imported-framing terms; zero describe-don't-interpret trigger phrases.
- Files written: five `entry.v2.json`, five `WORK.md`, and this report. No merge was run.
