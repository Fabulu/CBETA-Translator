# WORK — 本地風光 (t_831f84399d0b)

Drafted 2026-07-11. Concordance scoped to zen-corpus.json (462 texts) only.

## Concordance
- **382 hits / 161 allowlist files.** Overwhelmingly multi-source; the term is
  pervasively paired with **本來面目** (via the Yuanwu formula 蹋著本地風光，明見本來面目
  and 契合本來面目), and the fully-contiguous set phrase **本地風光本來面目** occurs in
  T47n1997 + T47n1998A (allowlist) as well.
- Top files: X82n1571 五燈全書 (17), X69n1357 佛果克勤心要 (14), J10nA158 密雲語錄 (10),
  T47n1997 圓悟語錄 (9), J27nB198 雪關語錄 (8), X64n1260 列祖提綱錄 (7), T47n1998A 大慧語錄 (7).

## Sense analysis
- **ONE corpus-wide sense.** No master idiosyncrasy. 本地 (one's own ground) + 風光
  (scenery). Uniformly the state one **步/踏/蹋著** (steps onto), **明見** (clearly sees),
  **自識** (recognizes for oneself), **明證** (verifies), **契合** (merges with).
- Effectively synonymous / twinned with 本來面目 ("original face") — they alternate and
  co-occur constantly. That is the one genuine RelatedTerm.
- Deflation: Miyun's 嘗在汝面門出入 ("always going in and out of your face-gate") shows it is
  ordinary immediate functioning, not a mystical vista. Gloss kept concrete.

## Attribution evidence (section heads checked via cb:mulu / <head>)
- Yuanwu Keqin — X69n1357 head 示民上人 (法語); T47n1997 head 小參四 → both his own words. ✓ roster.
- Dahui Zonggao — T47n1998A 0910a28 head 示妙明居士李知省伯和 (法語 to a layman). ✓ roster.
- Miyun (密雲圓悟) — J10nA158 0029a11 is a nun's question + 師云 answer → **two-speaker → MasterName=null**,
  determinate 師 = Miyun noted. ✓ roster (won't be the linked field, but recorded).

## KWIC verification
All 4 KWICs confirmed EXACT contiguous substrings of the cited file after tag+<note> stripping
(zc_verify.py). Each kept within a single <lb> line.

## Validation: multi-source
3 distinct roster masters (Yuanwu, Dahui, Miyun) across 4 texts + 161-file corpus spread.

## Gate 2 (Claude adversarial verify+repair) — VERIFIED 2026-07-11
- All 4 KWICs re-derived EXACT contiguous (tag+note stripped) in cited files. Zero ellipsis.
- Zero contamination: all 4 occurrence RelPaths + all 6 SourceTexts are in zen-corpus.json.
- FromLb re-derived = nearest preceding <lb>. X69n1357 (X-canon) correctly uses ed="X" 0471b21
  (co-located ed="R120" ignored per X-canon rule). Others ed=T/J match.
- Attribution confirmed at cb:mulu head: 831-1 mulu 示民上人 (Yuanwu, X69n1357 心要);
  831-3 mulu 示妙明居士李知省伯和 (Dahui, his 法語); 831-4 mulu 問答機緣上, two-speaker
  比丘尼問/師云 → MasterName=null (師=Miyun). All roster-confirmed in master-dates.json.
- Explanation collocations grep-verified: 嘗在汝面門出入✓, 契合本來面目✓, 自識本地風光✓,
  明見本地風光✓(3f), 明證本地風光✓(1f). **REPAIR: 明徹本地風光 = 0 corpus hits → removed
  "明徹" from Explanation (kept 明證).**
- Validation multi-source upheld (3 roster masters / 4 texts / 161-file spread).
- STATUS → verified.
