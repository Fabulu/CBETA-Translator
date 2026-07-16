# b009 Batch C research report

Scope was limited to the five assigned term directories. No status, manifest, wave-plan, guide, termbase, translation, or other-term file was changed.

## Results

### 面門 — the face (`t_d6453d4183ed`)

- Corpus: **939 hits / 264 allowlisted texts**.
- Structure: 1 corpus-wide, multi-source sense; 5 curated occurrences from Wudeng huiyuan and Yuanwu's record, with supporting inventory from two further records.
- Depth: physical face action; scorching/impact collocations; Linji's ‘entering and leaving through the faces’ case; Yuanwu's ‘dynamic at the face.’ The apparent ‘face + one who’ (面門者) hit and ‘so-called face’ (所謂面門) hit were checked and rejected as false self-definitions; their actual grammar is recorded in `WORK.md` and the entry note.
- Verification: **5/5 KWICs passed**; all contain the headword; all anchors synchronized.

### 全機 — the whole dynamic (`t_644a3152952c`)

- Corpus: **843 hits / 224 allowlisted texts**.
- Structure: 1 corpus-wide, multi-source sense; 5 curated occurrences across Yuanwu's record, the Blue Cliff Record, and Wudeng huiyuan.
- Depth: ‘whole dynamic and great function’ (全機大用), 115 hits / 60 texts; ‘alone exposed’ (全機獨露), 21 / 19; ‘thoroughly freed’ (全機透脫), 5 / 5; ‘alone functioning’ (全機獨用), 2 / 2; nominal, predicative, rubric, and prose-comment deployments. The only ‘whole dynamic + one who’ (全機者) hit is a person nominalizer; the one ‘what is the whole dynamic?’ (如何是全機) exchange is reported without interpretation.
- Verification: **5/5 KWICs passed**; all contain the headword; all anchors synchronized.

### 卜度 — to conjecture by calculation (`t_a0f2bb1de215`)

- Corpus: **702 hits / 206 allowlisted texts**.
- Structure: 1 corpus-wide, multi-source sense; 5 curated occurrences across Dahui's record and memorial, the Blue Cliff Record, and two Wudeng huiyuan sections.
- Depth: the corpus coordinates the word with thinking, affective cognition, the root of thought, and weighing. Counts and all distinct deployment shapes are inventoried. Nine apparent ‘conjectural calculation + one who’ (卜度者) hits are person nominalizers, not definitions; no true self-definition found.
- Verification: **5/5 KWICs passed**; all contain the headword; all anchors synchronized.

### 把定 — to hold fast (`t_8c870cb5e69d`)

- Corpus: **684 hits / 186 allowlisted texts**.
- Structure: 1 corpus-wide, multi-source sense; 6 curated occurrences across Wansong's comments, the Book of Serenity, Xuedou's record, and Xudeng lu.
- Depth: explicit hold/release contrast; Wansong's naming formula and eye-equivalence formula; Jiufeng's direct ‘what is the eye that holds heaven and earth fast?’ exchange; object/collocation spread including heaven and earth, world, key crossing, and throat; Wansong's inversion of holding and releasing. Full context identifies the lineage-headed Xudeng lu speaker as Jiangshan Fang, who is not roster-linked.
- Verification: **6/6 KWICs passed**; all contain the headword; all anchors synchronized.

### 垂示 — an indication; to offer an indication (`t_e5259ce8bbf5`)

- Corpus: **640 hits / 178 allowlisted texts**.
- Structure: 1 corpus-wide, multi-source sense; 5 curated occurrences across the Blue Cliff Record, Yunmen's Extended Record, the Jianzhong Jingguo continuation lamp record, and Wudeng huiyuan.
- Depth: editorial pointer rubric, verbal request in two exchanges, and Yunmen's genre heading ‘indications and substitute answers’ (垂示代語). The fixed rubric ‘the indication says’ (垂示云) has 109 hits / 21 texts; request forms have 107 / 56 and 57 / 26. Alternate reporting verbs and the apparent reversed string were checked and accounted for. No true self-definition found.
- Verification: **5/5 KWICs passed**; all contain the headword; all anchors synchronized.

## Combined QA

- JSON: **5/5 parse**, directory IDs and SHA-256 term IDs correct.
- Occurrences: **26/26 `zc.verify(...).ok == True`**; 26/26 contain the headword; exact current `FromLb`/`ToLb` values saved.
- Source texts: every listed source text has at least one current allowlist-scoped headword hit.
- Attribution: every selected occurrence was checked against its governing head and surrounding speaker context; raised cases, two-speaker exchanges, editorial rubrics, headings, and unrostered speakers remain null.
- Roster: all non-null sense/occurrence/related-master values match the first-name roster keys exactly.
- Final prose gates: zero banned-framing hits under the current conformance patterns; zero Chinese runs outside parentheses in entry prose; every Chinese phrase in prose carries adjacent English. No imported meditation, practice, method, present-moment, dualism, doctrine, Japanese, or romanized-overlay framing appears.

## Files written

- `terms/t_d6453d4183ed/entry.v2.json` and `WORK.md`
- `terms/t_644a3152952c/entry.v2.json` and `WORK.md`
- `terms/t_a0f2bb1de215/entry.v2.json` and `WORK.md`
- `terms/t_8c870cb5e69d/entry.v2.json` and `WORK.md`
- `terms/t_e5259ce8bbf5/entry.v2.json` and `WORK.md`
- `AGENT_B009_C.md`
