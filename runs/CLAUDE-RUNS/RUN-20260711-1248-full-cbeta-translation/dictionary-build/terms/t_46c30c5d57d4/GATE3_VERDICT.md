# GATE 3 VERDICT — t_46c30c5d57d4 · 不立文字

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11.
**Method:** independent re-grep of `xml-p5` Zen corpus, allowlist `zen-corpus.json`, tag-stripped joined-text substring checks, lb anchoring, cb:mulu/head back-scan.

## 1. KWIC integrity — CLEAN (4/4 verbatim)
| RelPath | FromLb | Verbatim (tag-stripped) | At lb |
|---|---|---|---|
| T/T48/T48n2008.xml | 0360b26 | YES (incl. 『』 quotes) | YES |
| T/T47/T47n1997.xml | 0769b07 | YES | YES |
| J/J34/J34nB311.xml | 0648a14 | YES | YES |
| B/B25/B25n0145.xml | 0791a19 | YES | YES |

Both "trimmed to one source line" claims verified: T48n2008 continues 即此不立兩字，亦是文字 immediately after the KWIC; T47n1997 continues into the four-phrase formula. Trimming to an exact shorter span is the guide-sanctioned move — no stitching.

## 2. Attribution — CLEAN
- T48n2008 (六祖大師法寶壇經, title verified): governing head = 付囑第十 — matches; Huineng's parting instruction. ✓
- T47n1997 (圓悟佛果禪師語錄): governing mulu = 小參五 — matches; Yuanwu's own talk. ✓
- J34nB311 (天界覺浪盛禪師全錄, title verified): governing head = 觀音殿燈節夜茶筵垂示 — matches; Juelang's 垂示. 覺浪道盛 confirmed in master-dates.json. ✓
- B25n0145 (天目中峰廣錄, title verified): governing mulu = 山房夜話上 — matches; Zhongfeng's own essay. 中峰明本 confirmed in roster. ✓
- No raised/quoted-speaker or two-speaker lines among the four; all MasterName values correct.

## 3. Allowlist — CLEAN
All 4 occurrence RelPaths + all 6 SourceTexts in zen-corpus.json.

## 4. Explanation honesty — CLEAN (all quoted Chinese grep-attested)
- Four-phrase formula in the Explanation's order 教外別傳，不立文字，直指人心，見性成佛: 13 hits/11 files. The J34 KWIC's variant order 不立文字，教外別傳，…: 2 hits/2 files (incl. the cited file). Both real.
- 達磨西來不立文字 (16/14) · 直指人心 (624/216) · 九年面壁 (435/150; 少林九年面壁 17/16) · 直道不立文字 (1, T48n2008) · **即此不立兩字，亦是文字 (1, T48n2008 — the required anti-literalist deflation, attested verbatim on the line after the cited KWIC)** · 不通文字，為不立文字乎哉 (1, J34nB311) · 不立文字語句 (30/14) · 不立文字語言 (12/11) · 拈花微笑 (163/102) · 教外別傳 (640/211). The Platform-Sutra "use no letters" slander line (直言不用文字) also verified in context at T48n2008 0360b26ff.
- Count claim "375 hits across 172 allowlist files" REPRODUCES exactly via raw-XML grep (375/172; joined-text count 435/188 — claim is an honest lower bound).

## 5. Multi-source — HOLDS
Four masters (Huineng, Yuanwu Keqin, Juelang Daosheng, Zhongfeng Mingben), four texts, four canons (T/T/J/B), Tang-through-Ming spread. `multi-source` amply justified.

## 6. Nesting / RelatedTerms — GENUINE
RelatedTerms 教外別傳／直指人心／見性成佛 are the real four-slogan cluster — the fused formula greps 13+2 hits across ≥12 files; each member is independently massive (640/624 hits). Deliberate semantic links, not coincidental prefixes.

## Punch list
None blocking. One cosmetic nit (no action required): Explanation says "later masters mock the literalist directly" (plural) but quotes a single attestation (不通文字，為不立文字乎哉, Juelang, 1 hit/1 file); the plural is defensible only via the Platform Sutra's own policing quoted alongside. Consider "a later master mocks" if ever revised for other reasons.
Defects: 0.
