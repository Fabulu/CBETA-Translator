# WORK — 大機大用 (t_d03aa9267f79)

Rendering: "great capacity, great function".

## Concordance (allowlist-scoped)
- 大機大用: **276 hits / 124 files**
- 大機: 988 / 245; 大用: 2094 / 330
- 大機之用: 72 / 43; 大用之機: 13 / 10 (the sub-split)
- 大機圓應: 16 / 12; 全機大用: 115 / 60; 大用現前: 447 / 170 (expanded forms)
- 具大機大用: 5 / 4

## Sense analysis
One corpus-wide sense. The compound's structure is set BY THE CORPUS (describe-only, not annotator's read): the stock 溈山–仰山 dialogue splits it into 大機 (百丈) and 大用 (黃檗) — 百丈得大機，黃檗得大用，餘者盡是唱導之師 — recurring across 仰山語錄 (T47n1990), 古尊宿語錄 (C077n1710), 人天眼目, 五燈, 五家 material. Three attested deployments:
1. praise/description of a master's activity (黃檗 見馬祖大機大用; couplet 大機圓應大用縱橫 / 直截)
2. test-question split into 大機之用 / 大用之機
3. a named error to avoid (以麄放狂亂為大機大用; 大機大用不在蒲團禪板上)

## Attribution
- 仰山慧寂 (T47n1990) — the split IS his answer, but two-speaker 溈山問仰山 → null (noted)
- 圜悟克勤 (T47n1997) roster ✓ — own couplet
- 天隱圓修 (J25nB171 0525) roster ✓ — own warning
- 玉林通琇 (B27n0152 工夫說) roster ✓ — own warning
- C077n1710 dialogue + 黃檗/馬祖 line — raised/two-speaker → null

## Validation
multi-source (5 files, 4 roster masters incl. Guishan-line 仰山慧寂 + Linji-line 圜悟克勤 / 天隱圓修 / 玉林通琇). All 7 KWICs verbatim/unique/lb-matched (0 problems).

## GATE 2 (2026-07-12, independent re-derivation)
Re-derived from `xml-p5` + `zen-corpus.json`. **STATUS: verified.**
- All 7 KWICs re-confirmed EXACT CONTIGUOUS, unique, FromLb/ToLb correct. All RelPaths allowlisted. Zero contamination.
- Counts re-derived exactly: 大機大用 276/124, 大機之用 72/43, 大用之機 13/10, 大機圓應 16/12, 全機大用 115/60, 大用現前 447/170, 大機 988/245, 大用 2094/330 ✓.
- The 百丈得大機黃檗得大用 line re-checked at source: it is 仰山's answer (師云, in his own 語錄 T47n1990) inside the two-speaker 溈山問仰山 dialogue → MasterName stays null, 仰山慧寂 named in the note. ✓ per gate instruction.
- REPAIRS:
  - T47n1990 note claimed "(人天眼目 T47n1990 edition)" — T47n1990 is 袁州仰山慧寂禪師語錄 (Taishō); 人天眼目 parenthetical removed. Note's recurrence list also claimed 人天眼目 and 五家 material — 人天眼目 is NOT among the 23 allowlist files containing 百丈得大機; replaced with the grep-verified list (五燈會元 X80n1565, 續傳燈錄 T51n2077, 從容庵錄 T48n2004, 禪宗頌古聯珠通集 C078n1720, "23 allowlist files").
  - B27n0152 0611b15 (以麄放狂亂為大機大用): actual cb:mulu is 客問評註 (the 客問 section, under 第三須悟處諦當) — NOT 工夫說 as drafted. Fixed in AttributionNote and Explanation. The answering voice is the master's own reply to the 客, so 玉林通琇 attribution stands.
  - '大機圓應大用直截' located precisely: 1×, 禪宗頌古聯珠通集 (C078n1720); Explanation now says so instead of implying parity with the 縱橫 form.
- Interpretation scan: none to delete; the 機/用 split remains framed as the corpus's own (仰山 dialogue), closing describe-only formula intact.

## Support-inflation remediation — 2026-07-13

- Before: 4 exact / 3 unlabelled support / 3 exact sources; floor 6. After: 6 exact / 3 labelled support / 5 exact sources.
- Added exact witnesses from Guting Shanjian and Poshan Haiming; all component-only and expanded-couplet anchors are now `family`. Item 8 and master-specific retest retain one paired device.
- All 9 KWICs verify with synchronized bounds. Attribution gate: 0 hard failures; depth/sense gate passes.

## Independent semantic peer crosscheck — 2026-07-13

- Retained one sense after rechecking the whole sample. The component formula, public questions, cautions, and master-specific predications all concern one paired capacity/function device; none denotes a different referent.
- Exact/support roles and speaker/title attributions hold. Final depth remains 6 exact / 3 support across 5 exact sources; definition, #0g, English-first, and quote-anchor checks pass without entry changes.

## Semantic remediation r001

- feedback-inference-verdict: REVISE — the old “Literally” opening merely repeated the components; the evidence supports explaining the pair as responsive capacity together with its use, while not assigning a single hidden effect.
- feedback-observations: Yangshan explicitly distributes capacity and function between Baizhang and Huangbo; Tianyin recombines and reverses the pair in public questions; later records deny that coarse wildness or sitting equipment constitutes it.
- feedback-falsification-searches: tested capacity and function as different referents, phrase versus title, master-specific senses, component-only formulas, expanded couplets, warnings, questions, and bodily predication.
- feedback-counterexamples: component-only and expanded forms are supporting family evidence, not exact compound uses; warnings against wildness block an alias such as “unrestrained behavior.”
- feedback-scope: one corpus-wide paired expression for great responsive capacity and that capacity enacted or exercised.
- lookup-probes: great capacity and great function; great responsiveness and its use; great ability and action; great potential and its exercise.
- opening-interpretation-verdict: REVISE — replaced a word-for-word gloss with a plain-English account tied to the corpus's own component split.
- plain-english-image-verdict: PASS — the opening now tells the reader what capacity and function do together rather than asking the reader to decode the calque.
