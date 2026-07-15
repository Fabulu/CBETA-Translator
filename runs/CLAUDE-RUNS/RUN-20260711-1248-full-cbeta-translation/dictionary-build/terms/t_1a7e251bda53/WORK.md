# WORK — 示眾 (t_1a7e251bda53)

## Concordance (Zen allowlist only, 462 texts)
- 示眾 total **12,002 hits across 399 allowlist texts** under the current apparatus-clean `zc.py` index. Overwhelmingly multi-source.
- Top texts: X82n1571 五燈全書 (1039), X79n1557 聯燈會要 (928), X64n1260 列祖提綱錄 (242), X84n1583 (241), X81n1568 (215), C077n1710 (192), J25nB171 天隱和尚語錄 (185), D48n8939 (164), T51n2077 (…), Yuanwu T47n1997 (many).

## Initial sense analysis (historical; superseded by the two-sense repair below)
One corpus-wide sense (SenseKey=null). 示眾 is a **discourse-genre / format marker**, not a doctrine:
- Verbal/lexicalized use: 師示眾云 / 陞座示眾云 = a master's formal instruction to the whole assembly, standing beside 上堂 and 小參. (Yuanwu T47n1997.)
- Literal display root: 示 = to show physically. 拈花示眾 (World-Honored One held up a flower and showed the assembly, 聯燈會要), 舉拂示眾 (held up the whisk). Same word; kept in one sense with the literal shade noted.
- Definitional evidence: 列祖提綱錄 editor lists 上堂 and 示眾 as un-named categories of address subsumed under the five-day (五參) convocation — proof it names a genre.

No master-specific bending found (unlike Nanquan's buffalo). Kept as one sense.

## Speaker attribution — how confirmed
- **T47n1997** = 圓悟佛果禪師語錄 (title element No. 1997). Whole record is Yuanwu Keqin's; 陞座示眾云 is his genre-form address. Canonical name in master-dates.json: 圜悟克勤.
- **X64n1260** = 列祖提綱錄 (title element). Occurrence #2 is the **editor's preface** (提綱篇首…) describing the genre → MasterName null. Occurrence #3 is 復舉：黃檗和尚示眾云… — the compiler cites Huangbo; instruction is Huangbo's (黃檗希運 in master-dates.json). Marked with AttributionNote.
- **X79n1557** = 聯燈會要. The 拈花示眾 story: 世尊 (the Buddha) holds up the flower — MasterName 世尊, used only to show the literal display sense.

## Initial KWIC verification (historical four-row draft)
All 4 KWICs confirmed as exact contiguous tag-stripped substrings (tags + layout newlines removed) via scripted check — all PASS. FromLb = nearest preceding `<lb>` in the canon edition (T for Taishō, X for 卍續).

## Multi-source verdict
**multi-source.** Sense holds across Yuanwu yulu + 列祖提綱錄 (editorial + cited Huangbo) + 聯燈會要, and ~398 allowlist texts. Genre-marker reading is corpus-wide and consistent.

## GATE 2 (Claude adversarial verify+repair)
- KWIC exactness: all 4 KWICs re-derived as EXACT contiguous tag-stripped substrings of the cited files (scripted, punctuation-preserving). Zero ellipsis/stitching/alteration.
- Allowlist: T47n1997, X64n1260, X79n1557, X82n1571, T51n2077 all IN zen-corpus.json. No contamination.
- FromLb: all confirmed nearest preceding <lb n>. X64 uses the primary ed="X" numbering (0002b20, 0015b09) — correct (the co-located ed="R112" reprint lb is not the reference).
- Attribution: occ[0] Yuanwu (師…陞座示眾云, in 圓悟佛果禪師語錄 上堂 section) OK; occ[1] null (提綱篇首 editorial preface) OK; occ[2] Huangbo (line names 黃檗和尚示眾云) OK; occ[3] 世尊 (line names 世尊…拈花示眾, literal-display exemplar) OK.
- Multi-source: Yuanwu yulu + 列祖提綱錄 + 聯燈會要 (+~398 texts). Confirmed.
- RelatedTerms (上堂/小參/普說/垂示): deliberate semantic sibling-genre cross-refs (guide §5b.4), not coincidental nesting. Kept.
- VERDICT: verified. No repairs needed.

## d001-A depth repair (2026-07-13)

- Re-ran item 8 and split verbal public address from physical display to the assembly. The Zen public-address format is primary.
- Preserved all five old anchors and added five verified anchors: final instruction, spoken declaration, and displays of whisk, garment, and mirror.
- Final depth: 2 senses, 10 occurrences: verbal address 6; physical display 4.
- Family check: 上堂 and 小參 are neighboring public formats, not senses. 拈花示眾 defines the Zen Buddha through the flower-sermon deployment; 舉拂示眾 shows the teaching-seat implement without symbolic interpretation.
- Further displayed-object examples were excluded as repetitions of the productive display class.

## d001 anti-quota follow-up (2026-07-13)

- Added a written-verse instruction, bringing depth from 10 to 11 without changing the two-sense structure.
- After the assembly's answers fail to accord, the master `書偈示眾`: writes a verse and presents it to the assembly. This is a distinct delivery medium for public instruction, not another displayed object and not a third sense.
- Final distribution: verbal/public instruction 7; physical display 4.

## L001-B gloss-hygiene and family retest

- sense-target-distinguishability: sense 0 `public-address format` names a verbal discourse format and record heading; sense 1 `physical display to the assembly` names the act of raising or showing an object before the community. These are different public actions, not noun/verb packaging of one event.
- Definition retest: 示眾云, the genre comparisons with 上堂 and 小參, and written or final instructions all support the address format; 拈花, 舉拂, the garment, and the mirror independently support visible display.
- Family retest: 上堂 and 小參 remain neighboring formats, while 拈花示眾 and 舉拂示眾 remain physical-display compounds. No third referent emerges from written delivery or from the displayed object's identity.

## 2026-07-13 Cohort A complete-case and public-feedback pass

- Reconstructed all eleven complete units. Exact actors are Yuanwu Keqin (two), Huangbo Xiyun, Yuejiang Zhengyin, Luopu Yuanan, Vasumitra, Ruibai Mingxue, Shakyamuni Buddha, Yaoshan Weiyan, and Yangshan Huiji. The remaining null is the editorial rules section of *Essentials from the Patriarchs' Addresses*, not a master turn.
- Three embedded-case vetoes mattered: Luopu's final address sits inside Gutting's commentary; Yaoshan's garment display sits inside the Mengxi section; Yangshan's mirror display sits in a later compilation. Inline names, not container owners, decide all three.
- Definition/family retest: the complete cases confirm two different acts. Spoken or written public instruction belongs to the address-format sense; flower, whisk, garment, and mirror remain physical displays. Written delivery is not a third referent, and the different displayed objects do not multiply senses.
- Roster deferral is explicit rather than concealed: Yuejiang Zhengyin, Vasumitra, Ruibai Mingxue, and Shakyamuni Buddha are exact source names but may await the separate roster expansion already in progress.

feedback-observations: s1o1–s1o7 show a named public-address format delivered from the seat, classified editorially, compared with small convocation, used for final instruction, and delivered in writing. s2o1–s2o4 show named actors physically displaying a flower, whisk, garment, or mirror before the assembly.

feedback-inference-verdict: **direct/licensed** — the verbal and physical uses are different public acts. The institutional address is the Chan bend of “show to the assembly”; the flower and whisk cases preserve the literal display action without decoding either object symbolically.

feedback-falsification-searches: Rechecked `示眾云`, `示眾曰`, `上堂示眾`, `小參`, `臨終示眾`, `書偈示眾`, `拈花示眾`, `舉拂示眾`, garment and mirror displays, embedded old cases, title-owner conflicts, and possible written-delivery third senses.

feedback-counterexamples: The editorial classification is not a personal utterance; Ruibai's written verse proves that medium alone does not define the address sense; the four physical objects prove productive display without making each object a separate sense.

feedback-scope: Two corpus-wide senses: a public verbal/written address format and a physical display to the assembly. Speaker, medium, occasion, and displayed-object identity remain case-level deployments.

lookup-probes: address-format probes `address the assembly`, `assembly address`, `public address`, `public instruction`, `instruction to the assembly`; display probes `show to the assembly`, `display to the assembly`, `show before the assembly`, `raise before the assembly`, and `hold up to the assembly` are covered by targets, alternates, and aliases.

opening-interpretation-verdict: **pass** — the first sense now states the public institutional address before counts and named examples; the second begins with the physical public display and immediately distinguishes it from verbal address.

sense-target-distinguishability: **KEEP** — `public-address format` denotes discourse addressed to the community; `physical display to the assembly` denotes showing an object. The targets are independently intelligible and not noun/verb packaging.

## 2026-07-13 independent exact-actor and semantic remediation

### Mechanical baseline and repairs

- Current apparatus-clean concordance: **12,002 hits / 399 files**.
- Replayed all eleven inherited KWICs with `PYTHONIOENCODING=utf-8`; **11/11 exact**, including exact primary-edition line bounds.
- Reconstructed every complete case independently. Ten rows have named exact actors; the editorial taxonomy is now an explicit `impersonal` actor state with grammar evidence, not a bare null.
- Extended the Yaoshan Weiyan KWIC leftward to anchor the object itself. The source says a donor gave a pair of trousers (`有施主施裩`), which Yaoshan raises before the assembly. Reader prose and attribution now say **a donated pair of trousers**, not the less exact “garment.”
- Corrected the English source-title typo “Gutting” to **Guting** in Luopu Yuanan's attribution note.

### Definition-formula and deployment search

| Probe | Hits / files | Decision |
|---|---:|---|
| `示眾者` | 1 / 1 | Preface observer's phrase; confirms an audible address but adds no definition beyond the retained direct classification and Yuejiang formula |
| `所謂示眾` | 1 / 1 | Same preface occurrence; not added because its exact actor is a named non-master preface writer, for whom the present XOR schema has no honest named-person branch |
| `謂之示眾` | 1 / 1 | Retained Yuejiang Zhengyin self-naming formula |
| `名為示眾` / `喚作示眾` / `何謂示眾` / `如何是示眾` | 0 / 0 each | No missing self-definition |
| `示眾云` | 3,928 / 299 | Address-format deployment represented by Yuanwu and Huangbo |
| `示眾曰` | 1,073 / 115 | Address and display forms represented across both senses |
| `上堂示眾` | 127 / 42 | Editorial genre relation retained |
| `陞座示眾` | 21 / 17 | Yuanwu seat/address form retained |
| `臨終示眾` | 17 / 14 | Luopu final address retained |
| `書偈示眾` | 11 / 9 | Ruibai written delivery retained; medium does not create a third thing |
| `拈花示眾` | 118 / 80 | The Zen Buddha's flower display retained |
| `舉拂示眾` | 7 / 3 | Yuanwu whisk display retained |
| `提起示眾` | 35 / 26 | Yaoshan trousers and Yangshan mirror displays retained |

### Exact-turn dispositions

| Row | Exact actor | Independent disposition |
|---|---|---|
| S1/O1 Yuanwu record | Yuanwu Keqin | Keep; own-record address after ascending the seat |
| S1/O2 editorial rules | Impersonal editorial classification | Keep with `ActorAttribution.Status: impersonal`; grammar predicates genre grouping, not a person's turn |
| S1/O3 Huangbo quotation | Huangbo Xiyun | Keep; inline `黃檗和尚示眾云` overrides compiler and later commenter |
| S1/O4 Yuejiang record | Yuejiang Zhengyin | Keep; own-record direct naming and comparison with small convocation |
| S1/O5 embedded final case | Luopu Yuanan | Keep; inline `洛浦臨終示眾云` overrides Guting's commentary container |
| S1/O6 patriarch section | Vasumitra | Keep; seventh-patriarch section plus preceding life account fixes the actor |
| S1/O7 Ruibai record | Ruibai Mingxue | Keep; own-record `師` writes and presents the verse after the assembly's replies |
| S2/O1 flower case | Shakyamuni Buddha | Keep; explicit World-Honored One actor in the Shakyamuni section |
| S2/O2 Yuanwu record | Yuanwu Keqin | Keep; own-record actor raises the whisk and asks the assembly |
| S2/O3 embedded Yaoshan case | Yaoshan Weiyan | Keep and extend; explicit `藥山`, donated trousers now inside KWIC, Mengxi is only container context |
| S2/O4 Yangshan case | Yangshan Huiji | Keep; explicit `仰山寂禪師` and mirror case fix actor |

### Definition, sense, family, opening, and lookup verdicts

- family-definition-retest: **KEEP WITH TARGETED REPAIR**. The new evidence does not weaken the two-sense definition. It sharpens Yaoshan's displayed object and resolves the editorial attribution state.
- sense-target-distinguishability: **KEEP**. A named discourse format and the action of visibly showing an object are different things. Spoken versus written delivery remains grammar/medium within the address sense; flower, whisk, trousers, and mirror remain objects within one display action.
- nested-family verdict: `上堂`, `小參`, and `臨終示眾` are neighboring or longer genre forms; `拈花示眾` and `舉拂示眾` instantiate the physical-display sense. None independently buys a new bare-headword sense.
- opening-interpretation-verdict: `pass` — both senses state their referent and the Chan institutional/public bend before counts or examples.
- lookup-probes: address sense `address the assembly`, `assembly address`, `public address`, `public instruction`, `instruction to the assembly`; display sense `show to the assembly`, `display to the assembly`, `show before the assembly`, `raise before the assembly`, `hold up to the assembly`.
- quote audit: all six Chinese prose strings are translated and anchored; the extended Yaoshan row now also anchors the precise displayed object.

### Independent gate result

- Full cohort gate report: `maintenance/cohort-gate-independent-shizhong-final.json`.
- Merge/commit/push: not run.
