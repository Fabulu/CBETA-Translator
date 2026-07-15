# WORK — 正中偏 (t_d4661c1b4dbb)

## Concordance (allowlist-only)
- Raw hits: **315 occurrences in 99 allowlist files**. Concentrated in Caodong / Five-Ranks material
  in lamp records: 禪宗正脉 X85n1593, 五燈全書 X81/X82n1571, 續燈正統 X84n1583, 續燈存稿 X84n1585,
  X86n1605. One sense (corpus-wide Five-Ranks term).

## Sense analysis — ONE sense (Five Ranks 五位, rank #1)
Literal graphs: 正 upright (正位 = 空界, principle, 君) · 中 within · 偏 biased (偏位 = 色界, phenomena, 臣).

Three describe-only pillars, all grep-verified:
1. **Dongshan verse** (師作五位君臣頌曰): `正中偏，三更初夜月明前。莫怪相逢不相識，隱隱猶懷舊日嫌`
   (X85n1593, mulu 洞山良价禪師).
2. **Caoshan self-definition** (師因僧問五位君臣旨訣): `正位即空界，本來無物。偏位即色界，有萬象形。
   正中偏者，背理就事。偏中正者，舍事入理` (X85n1593, mulu 曹山本寂禪師) — the richest content;
   `正中偏者，背理就事` is the in-corpus definition.
3. **King-minister allegory**: `君為正位，臣為偏位…君視臣是正中偏，君臣道合是兼帶語` (same Caoshan answer).

Deployment range:
- **Test-question** `如何是正中偏？` answered with turning phrases across many sections — e.g. 宏智正覺
  (roster) `師曰：雲散長空後，虗堂夜月明` (X81n1571); 夜半日當天; 木人夜半打鞦韆; etc.
- **Cross-tradition mapping** the texts themselves draw: `以濟宗論之，正中偏奪人也，偏中正奪境也` (X82n1571)
  — maps the Caodong rank onto Linji's 奪人/奪境.
- Ordering within 五位: `右看成偏中正，中看成正中來，下看成兼中至` (X82n1571).

## Attribution evidence
- Verse → **洞山良价** (Dongshan Liangjie, roster 807–869); his 五位君臣頌, his mulu section.
- 背理就事 / 空界·色界 / 君臣 gloss → **曹山本寂** (Caoshan Benji, roster 840–901); 師曰 in his mulu section,
  answering 五位君臣旨訣. DO NOT cross-attribute verse↔gloss.
- Test-question → **宏智正覺** (Hongzhi Zhengjue, roster 1091–1157); 僧問…師曰 in his own section.
- 濟宗 cross-map → 百丈瑞白明雪 (off-roster) → MasterName null.

## Validation
**multi-source** — verse + self-definition each witnessed in ≥2 independent lamp records (X85n1593, X81n1571).

## KWIC verbatim check
All 5 curated KWICs confirmed exact contiguous substrings (single source line, ed="X" lb). Long spans that
cross an `<lb>` (verse tail; 背理就事…舍事入理) were trimmed to one line per the verbatim rule.

## GATE 2 (verify-and-repair, 2026-07-12)
Independent re-derivation. Results:
- **KWICs:** all 5 drafted KWICs re-verified EXACT, unique (count=1), lb ed="X" MATCH, governing cb:mulu
  head MATCH (洞山良价禪師 / 曹山本寂禪師 / 明州天童宏智正覺禪師 / 南昌府百丈瑞白明雪禪師). 0 corrections.
- **Contamination:** 0 — all RelPaths + SourceTexts in zen-corpus.json (462).
- **Attribution:** 0 fixes. Roster confirms 洞山良价 (807–869), 曹山本寂 (840–901), 宏智正覺 (1091–1157);
  瑞白明雪 not on roster → null correct. Verse↔gloss separation (Five Ranks rule) correctly maintained.
- **Multi-source:** re-derived stronger than claimed — verse (三更初夜月明前) in 19 allowlist files incl.
  Dongshan's own record T47n1986B; 背理就事 in 11 files incl. Caoshan's own records T47n1987A/B.
- **Repairs (2):**
  1. Describe-only: Explanation's "Later Caodong writers cross-map it to a Linji device" (unattested school/
     plural claim) → "A later sermon maps the ranks onto Linji-school terms (以濟宗論之): …" (the text's own
     framing). Same sermon found duplicated in X80n1566 — noted in the occurrence AttributionNote.
  2. Enrichment: added the PRIMARY-record occurrence — 瑞州洞山良价禪師語錄 T47n1986B lb 0525c01
     `師。作五位君臣頌云。正中偏。三更初夜月明` (verified exact, unique, ed="T"); SourceTexts + Note now
     carry T47n1986B / T47n1987A.
- **Counts refresh** (tag+note-stripped method): 正中偏 = 332 occurrences in 99 allowlist files (draft said
  315/99; delta from lb-crossing hits the line-based grep missed).
- Verdict: **verified** (6 occurrences, all re-checked).

## 2026-07-14 public-feedback semantic review

- feedback-inference-verdict: RETAIN ONE NAMED FIVE-RANKS POSITION, LEAD WITH CAOSHAN'S ATTESTED RELATION RATHER THAN GRAPH ORDER, ADD RETRIEVAL, EXPAND ANCHORS, AND REPAIR PROVENANCE — the inherited entry contained the right evidence but presented it as untranslated parenthetical notes.
- feedback-observations: `正中偏` has 329 hits in 99 files; `正中偏者` 17/13; and direct `如何是正中偏` 106/60. Dongshan Liangjie's verse, Caoshan Benji's definition and ruler-minister relation, Hongzhi Zhengjue's direct answer, and Baizhang Ruibai Mingxue's cross-map are independently attested.
- feedback-falsification-searches: checked rank order, reverse-rank contamination, verse ownership, direct-definition ownership, empty/form realm language, ruler/minister direction, direct questions, Linji cross-map, source-title attribution, and the already-remediated `偏中正` entry for symmetry and distinction.
- feedback-counterexamples: Hongzhi Zhengjue's answer is an image rather than Caoshan's prose definition; Baizhang Ruibai Mingxue's Linji mapping is a later cross-map rather than the rank's universal meaning. These remain deployments rather than replacements for Caoshan's direct definition.
- feedback-scope: one named first position in Dongshan Liangjie's Five Ranks. Caoshan defines its movement as turning from principle toward affairs and models it as ruler looking at minister; later verses, answers, and cross-maps do not create new senses.
- lookup-probes: `first of Dongshan's Five Ranks`; `leave principle and enter affairs`; `ruler looking at the minister`; `moon before the first night watch`; `Caodong Five Positions first rank`.
- opening-interpretation-verdict: PASS after rewrite — the opening identifies the named system and immediately gives Caoshan's attested relation, so readers need not infer meaning from graph order.
- definition-and-sense-verdict: KEEP one rank sense. Verse title, direct definition, ruler-minister relation, public question, and later cross-map all point to the same named position.
- sense-target-distinguishability: one sense only; `正中偏` and `偏中正` remain separate headwords because Caoshan gives them reversed movements and ruler-minister directions.
- family-verdict: member one of the Five Ranks, paired with `偏中正` and followed by `正中來`, `兼中至`, and `兼中到`; the family relation belongs in RelatedTerms and retrieval, not as extra senses.
- provenance-verdict: Dongshan Liangjie owns the verse; Caoshan Benji owns the direct definition and ruler-minister relation; Hongzhi Zhengjue owns the public answer; Baizhang Ruibai Mingxue owns the Linji cross-map. Corrected the inherited false Baizhang Huaihai attribution.
- propagation-verdict: named rank entries must lead with the corpus's own relational definition, keep verse and gloss owners separate, and cross-check reverse-rank siblings before publication.
- final-gate: PASS — 6/6 exact KWICs verified; attribution, public-feedback, depth/sense, forbidden-English, and prose-attribution checks passed in `semantic-r003-owner1-zhengzhongpian-gate.json`; the refreshed six-occurrence attribution packet is `semantic-r003-owner1-zhengzhongpian-gate-attribution-packets.json`.
