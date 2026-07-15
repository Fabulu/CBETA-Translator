# GATE 3 VERDICT — t_2d4525b4b123 · 教外別傳

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11. Verified from scratch against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with tag-stripped byte-exact matching
(KWIC extracted programmatically from entry.v2.json, not retyped).

## 1. KWIC integrity — 5/5 PASS (verbatim, contiguous, lb-exact)
| # | RelPath | Cited lb | Found | lb match |
|---|---|---|---|---|
| 1 | X/X80/X80n1565.xml | 0031a08 | verbatim, 1 hit | YES (0031a08–a10, ed="X") |
| 2 | T/T48/T48n2003.xml | 0154c04 | verbatim, 1 hit | YES |
| 3 | T/T51/T51n2076.xml | 0356c20 | verbatim, 1 hit | YES |
| 4 | T/T51/T51n2076.xml | 0351b29 | verbatim, 1 hit | YES (0351b29–c01) |
| 5 | C/C077/C077n1710.xml | 0732b03 | verbatim, 1 hit | YES |

No ellipsis, no stitching, no added punctuation.

## 2. Attribution — 5/5 PASS
- Occ 1: 五燈會元, governing mulu `釋迦牟尼佛`; pre-context is the Flower Sermon verbatim
  (拈華示眾…迦葉尊者破顏微笑…世尊曰。吾有…) → speaker is the World-Honored One; roster grep
  confirms Sakyamuni is NOT a roster master → **null correct**, AttributionNote says so.
- Occ 2: Blue Cliff Record case 14 prose commentary (after 舉僧問雲門…如何是一代時教) →
  authorial attribution to **Yuanwu Keqin** correct.
- Occ 3: mulu `韶州雲門山文偃禪師` → **Yunmen Wenyan** correct; the self-interrogating use
  (三乘十二分教豈是無言語…) is exactly as described.
- Occ 4: mulu `福州鼓山興聖國師` (= 鼓山神晏) → **Gushan Shenyan** correct; monk asks
  如何是教外別傳底事, 師曰喫茶去.
- Occ 5: 古尊宿 Yunmen 室中語要; pre-context is `舉石霜云湏知有教外別傳一句` — a RAISED case →
  **MasterName null + Curated:false is exactly right** per the raised-line rule.

## 3. Allowlist — PASS
All 4 RelPaths (X80n1565, T48n2003, T51n2076, C077n1710) present in zen-corpus.json.

## 4. Explanation honesty — PASS
- **教 = 三乘十二分教 gloss confirmed in-corpus:** Yunmen's line (occ 3) directly juxtaposes
  三乘十二分教 with 教外別傳; the collocation 三乘十二分教 has 400 hits in 108 allowlist files
  (also verbatim in the Huitang Zuxin context at T51n2077 0564c). 教 is unambiguously the
  doctrinal/scriptural teachings.
- **ewk's "outside the historical records" correctly rejected** — no corpus support for reading
  教 as "records"; the entry grounds the rejection in the Chinese, per guide §6 protocol.
- Test-question variants all attested: 如何是教外別傳 70×/36 files; …一句 63×/30; …底事 20×/15;
  **…底法 7×/6** (the entry's parenthetical 一句/底事/底法 is fully honest).
- Gushan 喫茶去 and Shishuang 非句 verbatim in the cited KWICs; 不立文字 435×/188;
  正法眼藏 verbatim inside occ 1; 單傳心印 verbatim inside occ 2.
- Four-phrase frame claim consistent with occ 2 and with C077n1710 0689b24 (verified for the
  sibling 直指人心 entry).

## 5. Multi-source — PASS
4 independent texts (五燈會元, 碧巖錄, 景德傳燈錄 ×2 sections, 古尊宿語錄); both claimed facets
(slogan; deflected test-question) each attested across many independent masters (test-question
form in 36 allowlist files).

## 6. Nesting — PASS
RelatedTerms = 不立文字 / 直指人心 / 見性成佛 / 正法眼藏 / 祖師西來意 — all corpus-attested
(祖師西來意: 2081×/250 files) and all genuinely semantic: four-phrase companions, the Flower
Sermon transmission object (正法眼藏 co-occurs inside occ 1's KWIC), and the parallel deflected
test-question. No coincidental character-overlap relations.

## Punch list (non-blocking)
- Note claims "542 raw occurrences across 194 allowlist texts"; independent recount
  (XML-parsed, notes/rdg excluded, 462-file allowlist) gives 640/211. Claimed figure
  understates (conservative direction — no evidence inflation) but is not reproducible;
  state the counting method or refresh the number.
