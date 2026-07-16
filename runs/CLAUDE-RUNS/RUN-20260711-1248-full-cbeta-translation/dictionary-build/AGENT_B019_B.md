# b019 Batch B — build report

## Scope

Built the five assigned entries from scratch using only the allowlisted Chinese Chan corpus and `zc.py`:

- `t_1d1a833551a9` 活鱍鱍
- `t_d7a73033c034` 擔板漢
- `t_f0cb4dcfc70c` 掛搭
- `t_6ec308f7766e` 迷頭認影
- `t_283dce854520` 觸目菩提

Each term has one corpus-wide sense, an `entry.v2.json`, and a full `WORK.md` depth inventory. No status, manifest, termbase, wave-plan, merge, or corpus file was changed.

## Results by term

### 活鱍鱍

- Target: **alive and darting**
- Corpus: 229 hits / 99 texts
- Depth retained: literal fish deployment; motion/stillness contrast; pearl-running-on-a-tray description; person-type; direct phrase question; Yuanwu paired predicates.
- Variants: the ordinary lively spelling 175/82; Linji’s fish-side variant 7/4; adverbial form 63/37.
- Curated occurrences: 6.

### 擔板漢

- Target: **a board-carrying fellow**
- Corpus: 193 / 90
- Depth retained: the record’s recurrent “sees only one side” description; Muezhou call-and-turn case; Xuedou/Huanglong disagreement; opposed feast/thicket applications; explicit learning/awakening naming formula; “raise it whole” contrast.
- Short form: 589 / 186; “Xu Six carries a board and sees only one side” 5/5.
- Curated occurrences: 6.

### 掛搭

- Target: **register and take a monastic place**
- Corpus: 145 / 38
- Depth retained: guest-office and hall-office sequence; ordination-document presentation; assigned place; storage of luggage and arrangement of bowl/bedding; abbot’s permission; office notice, tea, bed register, and common-quarters admission; vacancy and waiting-list uses in recorded sayings.
- The entry remains a concrete institutional lodging/admission verb and does not recategorize it as a special spiritual activity.
- Curated occurrences: 6.

### 迷頭認影

- Target: **lose the head and take the shadow for it**
- Corpus: 136 / 76
- Depth retained: Yajnadatta account with the head not lost or gained; direct “how is it stopped?” exchange; forgotten-head variant; pairing with abandoning the root and pursuing the tips; guest-within-guest person-type; chamber test.
- Variants: reversed form 64/40; forgotten-head form 1/1; stop-question 5/5.
- Curated occurrences: 6.

### 觸目菩提

- Target: **awakening wherever the eye meets**
- Corpus: 127 / 58
- Depth retained: named Daowu/Shishuang novice-and-water-jar case and correction; reciprocal “all things constantly abide” exchange; Xuansha’s dead-monk statement; buddha-hall answer; kicked-dog action; Linquan’s later “no empty gap” comment.
- Direct question: 69 / 31; dead-monk line 8/8.
- The entry translates the graphs and actual sentence predicates without imported Buddhist, present-moment, Japanese, or technique framing.
- Curated occurrences: 6.

## Final QA

- JSON files parsed: **5/5**
- WORK depth inventories present: **5/5**
- occurrences checked: **30/30**
- `zc.verify(...).ok`: **30/30**
- stored bounds exactly match verifier bounds: **30/30**
- allowlist checks: **30/30**
- banned imported-framing findings: **0**
- Chinese outside permitted evidence/schema fields: **0**

Two deliberately retained occurrences do not contain the complete seeded headword:

1. 擔板漢 includes the stock short form “Xu Six carries a board and sees only one side”; it omits only the final graph for “fellow” and supplies the corpus’s recurrent direct description.
2. 迷頭認影 includes the reversed form “take the shadow and lose the head”; it supplies the text’s explicit not-lost/not-gained correction.

Both exceptions are exact, verified, lexicographically unique, and explicitly documented in their attribution notes and WORK inventories.

