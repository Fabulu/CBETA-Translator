# GATE 3 VERDICT — t_831f84399d0b · 本地風光

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11.
**Method:** independent re-grep of `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, allowlist `zen-corpus.json` (462 texts), tag-stripped joined-text substring checks, lb anchoring, cb:mulu/head back-scan.

## 1. KWIC integrity — CLEAN (4/4 verbatim)
| RelPath | FromLb | Verbatim (tag-stripped) | At lb |
|---|---|---|---|
| X/X69/X69n1357.xml | 0471b21 | YES | YES, `<lb ed="X" n="0471b21"/>` (correct ed="X", not ed="R") |
| T/T47/T47n1997.xml | 0764b12 | YES | YES |
| T/T47/T47n1998A.xml | 0910a28 | YES | YES |
| J/J10/J10nA158.xml | 0029a11 | YES | YES |

No ellipses, no stitching, no altered punctuation. All four KWICs are exact contiguous substrings including quotation/punctuation marks.

## 2. Attribution — CLEAN
- X69n1357 (佛果克勤禪師心要, title verified): governing cb:mulu/head = 示民上人 — matches AttributionNote; Yuanwu's own 法語. ✓
- T47n1997 (圓悟佛果禪師語錄, title verified): governing mulu = 小參四 — matches. ✓
- T47n1998A (大慧普覺禪師語錄, title verified): governing head = 示妙明居士李知省伯和 — matches; Dahui's own 法語. ✓
- J10nA158 (密雲禪師語錄, title verified): two-speaker Q&A (比丘尼問／師云) under mulu 勘辨／問答機緣上; MasterName correctly **null** per the two-speaker rule; the note's identification of the answering 師 as Miyun Yuanwu (密雲圓悟, confirmed in master-dates.json) is confined to the AttributionNote. ✓

## 3. Allowlist — CLEAN
All 4 occurrence RelPaths + all 6 SourceTexts are in zen-corpus.json.

## 4. Explanation honesty — CLEAN (12/12 phrases grep-attested)
本地風光本來面目 (9 hits/4 files) · 踏著本地風光 (22/16) · 蹋著本地風光 (11/4) · 明見本地風光 (2/2) · 自識本地風光 (1, T47n1998A) · 明證本地風光 (1, X69n1357) · 契合本來面目 (1, X69n1357) · 不隨聲色不居凡聖 (1, T47n1997) · 嘗在汝面門出入 (1, J10nA158) · 令其自識本地風光 (1, T47n1998A) · 蹋著本地風光，明見本來面目 (3/2 incl. X69n1357) — all attested.
**Specifically checked:** the unattested collocation 明徹本地風光 does NOT appear in the entry (and greps 0 in the corpus). No fabricated spans survive.
Count claim "382 hits across 161 allowlist files" REPRODUCES via raw-XML grep (381/161; joined-text count is higher, 454/173 — the claim is an honest lower bound, not inflation).

## 5. Multi-source — HOLDS
Three independent masters (Yuanwu Keqin, Dahui Zonggao, Miyun Yuanwu) across four texts in three canons (X/T/J). `multi-source` justified.

## 6. Nesting / RelatedTerms — GENUINE
本地風光 ↔ 本來面目 is corpus-real: the fused set phrase 本地風光本來面目 greps 9 hits/4 files, and the paired formula 蹋著本地風光，明見本來面目 greps in 2 files. Not a coincidental-prefix relation.

## Punch list
None. Zero defects.
