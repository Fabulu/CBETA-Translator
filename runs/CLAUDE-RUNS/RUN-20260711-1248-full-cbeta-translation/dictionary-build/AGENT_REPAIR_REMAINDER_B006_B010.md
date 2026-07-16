# Agent Repair Report — Remaining B006–B010 Entry-Level Findings

## Scope

Repaired the thirteen authorized `entry.v2.json` files identified by the depth audit:

- 逢佛殺佛
- 喫茶去
- 且道
- 一句
- 一喝
- 向上
- 便喝
- 承當
- 良久
- 珍重
- 宗風
- 格外
- 活句

No status, manifest, termbase, corpus, wave-plan, or merge files were changed. All pre-existing occurrence evidence and source-text lists were preserved. One occurrence was added to 活句 as specifically authorized.

## Entry-level repairs

### 逢佛殺佛

Translated the full Linji series and all surrounding frames into English. Preserved the distinction between Linji's complete recorded series, a later attributed question about the saying, and Kumu Facheng's shorter deployment. The entry reports the textual wording and later questions without supplying an interpretation.

### 喫茶去

Made the originating Zhaozhou exchange fully English-first while retaining the witness-order difference: Old Recorded Sayings places the monk who had not visited first, while Five Lamps Compendium places the returning monk first. Preserved the steward's third command, the alternate graph for “drink,” later deployments, and the named allusion “Zhaozhou's tea.”

### 且道

Recast the entry as an English description of a stock hortative discourse marker. Translated the recurrent interrogative continuations and retained the pause-or-capping-line pattern, corpus counts, and null speaker semantics.

### 一句

Removed the unsupported claim that every occurrence is a decisive utterance. Retained the attested clusters around the last phrase, first phrase, a phrase not previously told, the phrase that stops every tongue, and the donkey-tethering-stake proverb.

### 一喝

Removed the imported romanization from the targets. Translated Linji's four comparisons, kept the Xinghua–Minde case distinct, correctly retained Guyin as the attribution of the guest-and-host verse, and preserved the staff-and-shout pairing.

### 向上

Preserved the two-sense ordering: the recurrent Chan “upward/further-up” register first and the ordinary spatial direction second. Translated the compound family and the quoted questions, including the case whose answer is “no,” without imposing a single doctrinal gloss on the register.

### 便喝

Translated the reciprocal master/monk action sequences, hesitation trigger, mirror demand, strike–shout–cover-the-ears chain, arrival, and bow examples. Kept the entry at the literal narrative value “thereupon shouted.”

### 承當

Removed the inferred contrast with seeking outside and the supplied list of unstated objects. Defined the verb from its recorded receiving, bearing, and assuming uses; retained direct, self-directed, interrogative, negative, and shoulder-verb evidence.

### 良久

Completed an English-first cleanup while retaining the pause-then-speech, pause-then-question, pause-then-shout, pause-as-answer, and World-Honored-One case witnesses. The entry describes the recorded duration and silence without assigning it a hidden meaning.

### 珍重

Retained and translated the range of closing settings: hall address, informal convocation, address to the assembly, whisk talk, departure in dialogue, and respectful written closing.

### 宗風

Translated the named-house, upright/inclined, expansion, rousing, inheritance, revival, lineage, and direct-question frames. Kept one corpus-wide sense and translated the related term 宗旨 as “essential purport.”

### 格外

Removed the global characterization “term of commendation.” The entry now describes the word as applied to sayings, phrases, actions, workings, and people, and preserves the comment that examines and rejects a claimed instance.

### 活句

Preserved the contrastive definition, Gulin's description, recurrent instruction, Blue Cliff comparison, direct question and answer, intermediate category, and explicit reversal. Added the direct-definition occurrence from `X/X73/X73n1457.xml`:

- bounds: `0864c05`–`0864c06`
- exact KWIC: `不落言詮，不墮理路，不入思惟，不容擬議，千變萬化，八面受敵者，謂之活句`

The occurrence was verified against the corpus and its English annotation translates the entire definition.

## English-first repair

All Explanation, Note, and AttributionNote prose in these thirteen entries was rewritten or cleaned so the Chinese evidence remains in the KWIC/schema fields while the dictionary prose is English. Names, titles, rubrics, quoted formulas, component glosses, and continuations are translated.

## Verification

Final QA result:

- JSON files parsed: **13/13**
- occurrences checked: **70/70**
- `zc.verify(...).ok`: **70/70 true**
- stored line bounds equal verifier bounds: **70/70**
- corpus allowlist checks: **70/70**
- banned imported-framing findings in audited prose: **0**
- Chinese outside permitted parentheses in audited prose: **0**
- pre-existing occurrence evidence changed: **0**
- pre-existing source-text lists changed: **0**
- added occurrences: **1**, the authorized 活句 direct definition

## Deferred WORK-only notes

The following were not edited because they are ledger-level follow-ups outside this task's write scope:

- **如何是祖師西來意** — future formula, self-definition, and answer-class audit.
- **當下** — future formula and collocation audit; current entry left unchanged.
- **知解** — future rule-#0f audit covering formulas, food/digestion wording, warnings, and exclusions; current entry left unchanged.

