# WORK — 偏中正 (t_dc02eefd07f5)

## Concordance (allowlist-only)
- Raw hits: **296 occurrences in 87 allowlist files**. Same Caodong / Five-Ranks material as 正中偏:
  禪宗正脉 X85n1593, 五燈全書 X81/X82n1571, 續燈正統 X84n1583, 續燈存稿 X84n1585, X86n1605.
  One sense (corpus-wide Five-Ranks term); the exact inverse of 正中偏.

## Sense analysis — ONE sense (Five Ranks 五位, rank #2)
Literal graphs: 偏 biased (偏位 = 色界, phenomena, 臣) · 中 within · 正 upright (正位 = 空界, principle, 君).

Three describe-only pillars, all grep-verified:
1. **Dongshan verse**: `偏中正，失曉老婆逢古鏡。分明覿面別無真，休更迷頭猶認影` (X85n1593, mulu 洞山良价禪師).
2. **Caoshan self-definition**: `正中偏者，背理就事。偏中正者，舍事入理` — i.e. `偏中正者，舍事入理`, the exact
   inverse of `正中偏者，背理就事`. Full gloss on one line in 五燈全書 X81n1571 (mulu 撫州曹山元證本寂禪師);
   in 禪宗正脉 X85n1593 the `舍事入理` crosses an `<lb>`, so the X81n1571 line is used as the verbatim witness.
3. **King-minister allegory**: `臣向君是偏中正，君視臣是正中偏，君臣道合是兼帶語` (X85n1593, Caoshan).

Deployment range:
- **Test-question** `如何是偏中正？` answered with turning phrases — 宏智正覺 (roster) `師曰：白髮老婆羞看鏡`
  (X81n1571); 苔封古殿; 白頭翁子著皂衫; 雪覆崑崙頂; etc.
- **Cross-tradition mapping** the texts draw: `以濟宗論之，正中偏奪人也，偏中正奪境也` (X82n1571).
- Ordering: `右看成偏中正，中看成正中來` (X82n1571).

## Attribution evidence
- Verse → **洞山良价** (roster 807–869); 五位君臣頌, his mulu section.
- 舍事入理 / 空界·色界 / 君臣 gloss → **曹山本寂** (roster 840–901); 師曰 in his section (五燈全書 head reads
  撫州曹山元證本寂禪師 — 元證 = posthumous title). DO NOT cross-attribute verse↔gloss.
- Test-question → **宏智正覺** (roster 1091–1157); 僧問…師曰 in his own section (same exchange as the 正中偏
  test-question one line earlier).
- 濟宗 cross-map → 百丈瑞白明雪 (off-roster) → MasterName null.

## Validation
**multi-source** — verse + self-definition each witnessed in ≥2 independent lamp records (X85n1593, X81n1571).

## KWIC verbatim check
All 5 curated KWICs confirmed exact contiguous substrings (single source line, ed="X" lb). Spans crossing an
`<lb>` (verse tail; 臣向|君 in the allegory) were trimmed / re-anchored to one line per the verbatim rule.

## GATE 2 (verify-and-repair, 2026-07-12)
Independent re-derivation. Results:
- **KWICs:** all 5 drafted KWICs re-verified EXACT, unique (count=1), lb ed="X" MATCH, governing cb:mulu
  head MATCH (洞山良价禪師 / 撫州曹山元證本寂禪師 / 曹山本寂禪師 / 明州天童宏智正覺禪師 / 南昌府百丈瑞白明雪禪師).
  0 corrections.
- **Contamination:** 0 — all RelPaths + SourceTexts in zen-corpus.json (462).
- **Attribution:** 0 fixes. Roster confirms the three named masters; 瑞白明雪 off-roster → null correct.
  元證 = Caoshan's posthumous title (head 撫州曹山元證本寂禪師) — accurately noted. Verse↔gloss separation
  correctly maintained.
- **Multi-source:** re-derived stronger than claimed — verse (失曉老婆逢古鏡) in 14 allowlist files incl.
  T47n1986B; 舍事入理 in 9 files incl. Caoshan's own records T47n1987A/B.
- **Repairs (2):**
  1. Describe-only: "Later Caodong writers cross-map it to a Linji device" → "A later sermon maps the ranks
     onto Linji-school terms (以濟宗論之): …". Same sermon duplicated in X80n1566 — noted.
  2. Enrichment: added the PRIMARY-record occurrence — 撫州曹山元證禪師語錄 T47n1987A lb 0527a07
     `背理就事。偏中正者舍事入理。兼帶者冥應` (verified exact, unique, ed="T", inside the 因有僧問五位君臣旨訣
     answer opening 0527a05); SourceTexts + Note now carry T47n1986B / T47n1987A.
- **Counts refresh** (tag+note-stripped method): 偏中正 = 316 occurrences in 93 allowlist files (draft said
  296/87; delta from lb-crossing hits).
- Verdict: **verified** (6 occurrences, all re-checked).

## 2026-07-14 public-feedback semantic review

- feedback-inference-verdict: REWRITE — the inherited article contained the right direct definitions but buried them inside untranslated parenthetical graphs and source mechanics.
- feedback-observations: `偏中正` has 312 hits in 93 allowlisted files; `偏中正者` 18/13; `舍事入理` 9/9; the minister-facing-ruler formula 8/8; the direct question 105/60; and the object-removal formula 2/2.
- feedback-falsification-searches: re-read Dongshan's verse, both Caoshan definition witnesses, the ruler-minister relation, Hongzhi's public answer, the Linji cross-map, and the paired `正中偏` entry materials.
- feedback-counterexamples: Hongzhi answers the rank question with an image rather than repeating Caoshan's definition, and Baizhang Ruibai Mingxue maps it into different vocabulary. These are later deployments of the rank, not rival dictionary senses.
- feedback-scope: the entry reports one named rank, Caoshan's definitions and relation, Dongshan's verse image, and later public uses. It does not infer a universal metaphysics from “principle,” “affairs,” ruler, minister, or mirror.
- lookup-probes: `second of Dongshan's Five Ranks`; `leave affairs and enter principle`; `minister facing the ruler`; `old woman meets an ancient mirror`; `Caodong Five Positions second rank`.
- opening-interpretation-verdict: PASS after rewrite — the opening identifies the rank, gives Caoshan's direct definition and relational formula, and only then presents verse and later answers.
- definition-and-sense-verdict: KEEP one rank sense. Verse, direct definition, ruler-minister relation, test question, and cross-school mapping all predicate or deploy the same named position.
- sense-target-distinguishability: one sense only; “upright,” “straight,” “biased,” and “partial” are translation variants, not different objects.
- family-verdict: `正中偏` is the explicitly paired inverse; the other Five-Ranks labels remain separate entries and cannot buy this entry's depth.
- provenance-verdict: all six exact KWICs remain valid. Prose quotes are now limited to strings visibly anchored in the saved witnesses; notes name each text and speaker in English-first form.
- propagation-verdict: schematic rank entries must put their in-corpus definition and relation in ordinary English before verse citation, graph decomposition, or source mechanics.
- final-gate: PASS — 6/6 exact KWICs verified; attribution, public-feedback, depth/sense, forbidden-English, and packet checks all passed in `semantic-r003-owner1-pianzhong-gate.json`.
