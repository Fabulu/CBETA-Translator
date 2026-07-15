# WORK — 不立文字 (t_46c30c5d57d4)

Drafted 2026-07-11. Concordance scoped to zen-corpus.json (462 texts) only.

## Concordance
- **375 hits / 172 allowlist files.** Strongly multi-source.
- Top files: X82n1571 五燈全書 (11), X71n1404 (9), X64n1260 列祖提綱錄 (9), J34nB311 覺浪盛全錄 (9),
  B25n0145 中峰廣錄 (9), X84n1583 (7), X80n1565 五燈會元 (7), T51n2076 景德傳燈錄 (7), T47n1997 圓悟語錄 (7).

## Sense analysis
- **ONE corpus-wide sense** = the school-slogan "not setting up words/letters." Consistently
  a member of the four-phrase formula **教外別傳，不立文字，直指人心，見性成佛** and bound to
  Bodhidharma (**達磨西來不立文字**, 少林九年面壁). Frequently expanded 不立文字語句 / 語言.
- DEFLATIONARY, and the corpus polices this itself: it does NOT mean "be illiterate / abolish
  language." Huineng (壇經, 付囑第十): 直道不立文字 — 即此不立兩字，亦是文字 ("even the two graphs
  'not-set-up' are themselves letters"). J34nB311 mocks the literalist: 不通文字，為不立文字乎哉.
  So: the transmission is not LODGED in words (pointed directly), not that words are thrown away.

## Attribution evidence (heads checked)
- Huineng — T48n2008 head 付囑第十 (his parting teaching). ✓ roster.
- Yuanwu Keqin — T47n1997 head 小參五. ✓ roster.
- Juelang Daosheng (覺浪道盛) — J34nB311 head 觀音殿燈節夜茶筵垂示 (his 垂示); full 4-phrase line. ✓ roster.
- Zhongfeng Mingben — B25n0145 head 山房夜話上 (his own essay); Yuan-era independent witness. ✓ roster.
- Bodhidharma listed as RelatedMaster (the slogan is attached to his coming), not attributed to a KWIC.

## KWIC verification
All 4 KWICs confirmed EXACT contiguous substrings after tag+<note> stripping (zc_verify.py),
each within a single <lb> line. The Huineng and Yuanwu KWICs are trimmed to one line because the
continuation (即此不立兩字亦是文字 / 直指人心見性成佛) falls on the next line.

## RelatedTerms
教外別傳 · 直指人心 · 見性成佛 — the genuine four-slogan cluster (each independently attested:
542 / 545 / 604 hits in the allowlist). Not a coincidental-prefix relation.

## Validation: multi-source
4 distinct roster masters (Huineng, Yuanwu, Juelang, Zhongfeng) spanning Tang→Yuan→Ming, 172 files.

## Gate 2 (Claude adversarial verify+repair) — VERIFIED 2026-07-11
- All 4 KWICs re-derived EXACT contiguous in cited files. Zero ellipsis.
- Zero contamination: all 4 occurrence RelPaths + all 6 SourceTexts in zen-corpus.json.
- FromLb re-derived = nearest preceding <lb> (all ed=T/J/B, match).
- Attribution confirmed at head: 46c-1 mulu 付囑第十 (Huineng, 壇經 — his parting sermon;
  又云…直道不立文字 sits inside his own rebuttal of 執空之人, immediately continued by his
  deflation 即此不立兩字亦是文字, single-speaker → Huineng ✓); 46c-2 head 小參五 (Yuanwu);
  46c-3 mulu 觀音殿燈節夜茶筵垂示 (Juelang Daosheng — full 4-phrase line in his 垂示);
  46c-4 head 山房夜話上 (Zhongfeng Mingben). All roster-confirmed.
- Explanation quotes grep-verified: 即此不立兩字，亦是文字✓, 直道不立文字✓, 直言不用文字✓,
  不通文字，為不立文字乎哉✓(J34nB311), 直指人心見性成佛✓, 不立文字語句✓(21f)/語言✓(16f),
  少林九年面壁✓(13f), 拈花微笑✓(130f). No unverifiable claims.
- RelatedTerms four-slogan cluster genuine (教外別傳/直指人心/見性成佛 each pervasive).
- Validation multi-source upheld (4 roster masters Tang→Yuan, 172 files).
- STATUS → verified. No repairs needed.
