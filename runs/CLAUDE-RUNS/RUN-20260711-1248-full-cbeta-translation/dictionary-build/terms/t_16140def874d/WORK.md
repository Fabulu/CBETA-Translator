# WORK — 主人公 (t_16140def874d)

**Gloss target:** "the host / the master-in-charge" — one's true self (relates to 無位真人)
**Batch:** b003

## Concordance (Zen allowlist only)
- ~150+ occurrences across B25n0144/0145, B27n0152, C077n1710, C078n1720, D-series, etc.
- Search helper: scratchpad/search.py (allowlist-filtered).

## Sense analysis
- ONE corpus-wide sense (SenseKey null). 主人 = host/master of a house/one in charge;
  公 = personifying honorific. 主人公 = "the master-in-charge [of the house of oneself]"
  = one's own true self, host not guest.
- Locus classicus: Ruiyan Shiyan (瑞巖師彥) daily calling 主人公 to himself and answering
  諾, warning 惺惺著…莫受人謾 (Wumenguan 12).
- Explicit corpus equation with Linji's 無位真人 and 即心是佛 (多將箇瑞巖主人公臨濟無位真人) —
  anchors the requested 無位真人 cross-ref.
- Deflationary / un-reifiable: Zhaozhou answers 如何是趙州主人公 with 田庫奴 ("field-and-
  granary slave"); the record's central warning is against mistaking the 昭昭靈靈的識神
  (bright identity-consciousness) for it (守住個昭昭靈靈的識神便是悟得主人公也 = the error).

## Multi-source gate → PASS (multi-source)
Independent witnesses across B25n0144, B25n0145, B27n0152, C077n1710, C078n1720; multiple masters.

## Curated occurrences (KWIC = exact contiguous, tag-free, line-bounded; grep-verified)
1. C078n1720 0796a09 — 每日喚主人公復應諾 (瑞巖師彥; locus classicus)
2. C077n1710 0916a10 — 尋常方丈內自召主人公自云喏又云惺惺 (null; cited & criticized)
3. B25n0145 0699a04 — 多將箇瑞巖主人公臨濟無位真人 (null; 無位真人 grouping)
4. B25n0144 0418b11 — 那個是闍梨主人公？」對曰：「現祇對和尚即是。」 (null; two-speaker)
5. B27n0152 0520b02 — 守住個昭昭靈靈的識神便是悟得主人公也 (null; 識神 polemic)

## Links
- RelatedMasters: 瑞巖師彥, 臨濟義玄, 趙州從諗
- RelatedTerms: 無位真人, 識神, 惺惺著, 本來面目

## Notes / risks
- Ruiyan self-calling continues on next lb (惺惺著他後莫受人謾); KWIC kept line-bounded.
- MasterName null for cited/two-speaker lines; Ruiyan narration attributed to 瑞巖師彥.

---

## GATE 2 (Claude adversarial verify+repair) — VERIFIED
- KWICs: all 5 exact-contiguous (count=1 each), tag-stripped substring match confirmed. Zero ellipses.
- Allowlist: all RelPaths (C078n1720, C077n1710, B25n0145, B25n0144, B27n0152) in zen-corpus.json. No contamination.
- FromLb: all 5 match nearest preceding <lb n>. OK.
- Attribution: #1 瑞巖師彥 CONFIRMED (section head 台州瑞岩師彥禪師嗣巖頭; his self-calling narration — note file uses variant 瑞岩, entry uses 瑞巖, same master). #2/#3/#4/#5 null (correct: cited/two-speaker/polemic lines).
- REPAIR (over-read/wrong collocation): Explanation claimed Zhaozhou answers 如何是趙州主人公 with 田庫奴 — FALSE. In J24nB137 the actual answer is 師咄云這箍桶漢; 田庫奴 is a SEPARATE Zhaozhou passage (師云田庫奴什麼處是揀擇). Corrected to 這箍桶漢.
- Explanation quotes otherwise verified: 每日喚主人公復應諾, 多將箇瑞巖主人公臨濟無位真人, 守住個昭昭靈靈的識神便是悟得主人公也, 昭昭靈靈的識神.
- Multi-source: 5 independent texts, holds. Single corpus-wide sense correct.
- Verdict: VERIFIED (after 田庫奴→這箍桶漢 fix).
## Public-feedback inference ledger

- feedback-inference-verdict: `accepted-with-limits` — household-title grammar supports “one in charge”; the Chan record itself supplies self-calling, explicit equivalence with mind/oneself, comparison with the true person of no rank, and repeated public location questions. “True self” remains a lookup alias, not the displayed definition.
- feedback-observations: Ruiyan's self-call, Dongshan Liangjie's guest/host correction, Tianru Weize's explicit equation, Yulin Tongxiu's identity-consciousness warning, Zhongfeng Mingben's comparison, and Miyun Yuanwu's location question converge on the personified title.
- feedback-falsification-searches: searched exact headword, `喚作主人公`, `主人公在`, `主人公何在`, `主人公是`, and the no-dream/no-thought question family; tested direct-address, household office, literary protagonist, and distinct-person referents.
- feedback-counterexamples: Yulin Tongxiu explicitly rejects the bright, numinous identity-consciousness as sufficient realization; Dongshan rejects the immediate respondent as the answer. These prevent collapsing the entry to “conscious self.”
- feedback-scope: corpus-wide personified title in Chan address and examination; no master-specific sense split.
- lookup-probes: `one in charge`, `master in charge`, `master of the house`, `inner master`, `true self`, `real self`, `who is in charge`, `where is the master`.
- opening-interpretation-verdict: `pass` — the household title is stated first, and the record's self-call, questions, equations, and corrections immediately establish the Zen deployment.

## Sense and depth audit

- sense-split verdict: one referent. Calling, locating, equating, and warning concern the same personified title.
- depth: 7 exact anchors from 7 independent texts against 738 hits in 198 texts; Miyun Yuanwu's exact location question added to meet the frequency floor.

## Exact-turn attribution correction (2026-07-13)

- Removed the Miyun exchange because the unnamed monk, not Miyun Yuanwu, utters the stored headword.
- Replacement: Zhaozhou Congshen's own `一從見老僧後。更不是別人。祇是箇主人公。` (X80n1565 0092a19–20). Seven named-speaker anchors remain.
