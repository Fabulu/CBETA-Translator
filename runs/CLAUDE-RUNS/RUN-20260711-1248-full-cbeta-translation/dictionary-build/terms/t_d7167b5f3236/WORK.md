# WORK — 殺人刀 (t_d7167b5f3236) "the sword that kills"

Authored 2026-07-11 20:49 +02:00. Batch b004, agent 3.

## Concordance (Zen allowlist only)
- 殺人刀: **TOTAL 407 occ / 147 files.** Densest X66n1296 (36), X64n1260 (15), X82n1571 (15), X79n1557 (12), J25nB171 (11), T47n1997 (11).
- 活人劍 (the paired blade): **TOTAL 263 occ / 101 files** — top files identical (X66n1296, J25nB171…), confirming the fixed dyad.
- 殺人刀活人劍 fused compound: 24 occ / 11 files (T47n1997, T48n2003 lead).

## Sense analysis
One corpus-wide sense (null). 殺人刀 is ALWAYS metaphorical in Chan = the teacher's taking-away / negating function ("kills" the student's attachment & standpoint), fixed opposite 活人劍 (life-giving/reviving). Two uses of the SAME sense:
- **Fused idiom** 殺人刀活人劍 = the master's complete two-edged capacity (Biyanlu 垂示: 乃上古之風規).
- **Analytic** — 殺人刀 alone = pure negation, contrasted with having 活人劍 too (天隱 J25nB171 0520b16-17: 石霜雖有殺人刀且無活人劍；巖頭亦有殺人刀亦有活人劍).
圜悟's gloss ties meaning down: 若論殺人刀不存毫末。活人劍橫屍萬里 … 須知殺中有活…活中有殺 (T47n1997 0748a05-06). Not two senses — one dyadic function-metaphor.

## Attribution evidence
- T48n2003 = 碧巖錄; the 垂示 (pointers) are 圜悟克勤's editorial voice. Case-12 pointer at 0152c14 (recurs case-15 0155a18). ✓ 圜悟克勤 roster.
- T47n1997 = 圓悟佛果禪師語錄 (mulu1 圓悟佛果禪師語錄序); 上堂 discourses = 圜悟克勤 (roster spelling 圜悟, text writes 圓悟 — same master). Governing heads 上堂一/上堂二 confirm own-record discourse.
- J25nB171 = 天隱和尚語錄; occurrence 0520b22 governed by head 烏瞻山法濟禪院語錄, and 山僧今日…拈出 marks 天隱圓修's own 示眾. ✓ 天隱圓修 roster. The 0520b16-17 line inside 『…』 is a quoted old gauge (德山/巖頭/石霜) → null.

## Multi-source verdict: MULTI-SOURCE
Two independent masters (圜悟克勤 across 2 texts, 天隱圓修) + pervasive 活人劍 co-occurrence (~100 texts). Explicitly noted in Note that it is a shared Linji-house/Song-Chan trope, NOT one master's coinage (guards against over-claim).

## Anti-pattern checks
- KWICs verbatim (friz_verify.py OK), note-free, no ellipsis/stitching.
- Deflationary: flagged as pedagogical-function metaphor (奪/與, 把住/放行), explicitly NOT literal/moral killing.
- lb: all cited files are T/J canon (single lb system); ed values fine.
- 劍 vs 劒 variant present in corpus (e.g. T48n2003 0172c23 note uses 劒) — avoided; all cited KWICs use 劍 matching their files.

## RelatedTerms rationale
活人劍 (the genuine pair — real semantic link, mirrors buffalo 水牯牛↔異類中行 pattern), 把住放行 / 擒縱 / 縱奪 (the same take-away/grant polarity). Genuine, not coincidental.

## GATE 2 (Claude adversarial verify+repair) — 2026-07-11 · STATUS=verified
Re-derived every occurrence from source (whitespace-normalized exact-substring + lb-anchor script).
- **KWICs (4): all EXACT CONTIGUOUS**, zero ellipses. 0152c14 pointer legitimately recurs (2 matches; FromLb picks the case-12 one). #corrected = 0.
- **FromLb/ToLb: all 4 confirmed** nearest-preceding lb (T/J single-ed files). #corrected = 0.
- **Contamination: 0.** All RelPaths + 5 SourceTexts in allowlist; extras contain term (X66n1296=35, X64n1260=13). #removed = 0.
- **Attribution (confirmed):**
  - T48n2003 0152c14 → 垂示 (pointer) = 圜悟克勤's editorial voice. ✓
  - T47n1997 0748a05 → mulu 上堂 (own-record discourse) = 圜悟克勤. ✓
  - J25nB171 0520b22 → mulu 烏瞻山法濟禪院語錄; line reads `山僧今日…明明拈出…且道如何是殺人刀、活人劍` (山僧 self-ref) = 天隱圓修's own 示眾. ✓
  - J25nB171 0520b16 → inside 『…』 (quoted old gauge 石霜/巖頭/夾山) → null correct.
  - #attribution fixes = 0.
- **Explanation quotes:** all curated KWICs; the non-KWIC span 須知殺中有活…活中有殺 verified in T47n1997 (`須知殺中有活擒縱…活中有殺`). #unverified-claims removed = 0.
- **殺人刀↔活人劍 dyad:** confirmed already flagged in Note as a shared Song-Chan / Linji-house trope (活人劍 co-occurs ~100 texts), NOT one master's coinage. Over-claim guard intact.
- **Multi-source:** 2 independent masters (圜悟克勤 ×2 texts, 天隱圓修) → holds. No downgrade.
- entry.v2.json unchanged (clean). VERDICT: verified.
