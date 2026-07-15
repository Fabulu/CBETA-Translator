# GATE 3 VERDICT — t_427fa502a11b · 話墮

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11. Verified from scratch against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with tag-stripped byte-exact matching
(KWIC extracted programmatically from entry.v2.json, not retyped).

## 1. KWIC integrity — 5/5 PASS (verbatim, contiguous, lb-exact)
| # | RelPath | Cited lb | Found | lb match |
|---|---|---|---|---|
| 1 | C/C077/C077n1710.xml | 0657c09 | verbatim, 1 hit | YES (0657c09–c10) |
| 2 | T/T47/T47n1988.xml | 0550b06 | verbatim, 1 hit | YES |
| 3 | T/T51/T51n2076.xml | 0375c01 | verbatim, 1 hit | YES |
| 4 | X/X80/X80n1565.xml | 0404a19 | verbatim, 1 hit | YES (ed="X" lb, correct edition) |
| 5 | X/X80/X80n1565.xml | 0167a22 | verbatim, 1 hit | YES (ed="X") |

Occurrence 1 contains gaiji U+2F804 (你-variant); confirmed byte-exact against the source
file (a plain-你 retype would NOT match — the entry stored the correct source bytes). No
ellipsis, no stitching, no added punctuation anywhere.

## 2. Attribution — 5/5 PASS
- Occ 1: governing cb:mulu `睦州禪師語錄`, head `睦州禪師大鑑下四世嗣黃檗` → **Muzhou Daoming** correct.
- Occ 2: Yunmen 廣錄, mulu `對機三百二十則` → 師 = **Yunmen Wenyan** correct.
- Occ 3: mulu `婺州金鱗報恩院寶資曉悟大師` → Baozi Xiaowu, grep of roster confirms he is NOT
  in master-dates.json → **MasterName null is correct** and the AttributionNote says exactly that.
- Occ 4: mulu `臨安府徑山宗杲大慧普覺禪師`; speaker self-refers as 徑山 and attacks 默照之徒
  (Dahui's signature polemic) → **Dahui Zonggao** correct.
- Occ 5: mulu `漳州羅漢院桂琛禪師` → **Luohan Guichen** correct; monk asks 和尚因甚麼如此, 師 answers.

## 3. Allowlist — PASS
All 4 RelPaths present in zen-corpus.json (C077n1710, T47n1988, T51n2076, X80n1565).

## 4. Explanation honesty — PASS (every claimed collocation grep-attested)
- 你話墮 9×/5 files, 汝話墮 16×/7, 爾話墮 2×/1 — the 你/汝/爾話墮也 formula is real.
- 什麼處是話墮 7×/6 files (occ 1 has the full 什麼處是話墮處).
- **彼此話墮 verified at exactly T51n2076 lb 0401c02** as the Note claims (Fuqing Guangfa
  section; distinct master from the Baozi occurrence — the reciprocal use is independently attested).
- 我話亦墮 5×/5 files. 墮負 4×/4 files. Muzhou's 擔枷…崖州 and Yunmen's 七棒對十三 are verbatim
  inside the cited KWICs. Dahui's 老胡九年話墮 (Bodhidharma charge) verbatim at occ 4.

## 5. Multi-source — PASS
4 independent texts (古尊宿語錄, 雲門廣錄, 景德傳燈錄, 五燈會元), 5 distinct speakers.
The reciprocal facet has ≥2 independent witnesses (彼此話墮 Fuqing Guangfa; 我話亦墮 Baozi, plus 5 files total).

## 6. Nesting — PASS
RelatedTerms = 墮負 / 轉語 / 機鋒: all corpus-attested, all genuinely semantic (dialogic
defeat / turning-word / repartee). **話頭 correctly absent** — no coincidental 話-prefix relation.

## Punch list (non-blocking)
- Note claims "552 raw occurrences across 170 allowlist texts"; independent recount
  (XML-parsed, notes/rdg excluded, 462-file allowlist) gives 577/172. Direction is
  conservative (no evidence inflation), delta ~4% — likely a stripping-pipeline difference.
  Consider stating the counting method or refreshing the number. Not a defect in evidence.
