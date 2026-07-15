# GATE 3 VERDICT — t_61c90d3a8edd · 兼中到

**Auditor:** Gate 3 independent adversarial pass (Claude, from-scratch corpus re-derivation)
**Date:** 2026-07-12 01:10 +02:00
**Method:** tag-stripped extraction of every cited TEI file (note/rdg/orig excluded; cb:mulu tracked separately), exact-substring KWIC matching, lb-milestone position mapping per edition, full 462-file allowlist recount of every numeric claim, cb:mulu governing-head check at each occurrence offset, proximity analysis for the 功位俱隱 claim, roster lookup, title verification against teiHeader.

VERDICT: REVISE

One real provenance defect (shared with t_ccd48e1c9145: wrong text/author name for X72n1437) plus three note-level flags. Every Kwic field, every lb, and every count verifies exactly.

## THE DEFECT (blocking, trivial to fix)
**X72n1437 is 永覺元賢禪師廣錄, NOT 無異元來禪師廣錄.** Verified from the file's teiHeader (`<title level="m">永覺元賢禪師廣錄`, byline (嗣法)道霈重編). 無異元來禪師廣錄 is a different text (X72n1435). Affected: the AttributionNote of the 0541b19 occurrence ("X72n1437 (無異元來禪師廣錄), 曹洞五位 commentary") and, by inheritance, the 0539b11 寂音-emendation occurrence in the same file. The commentator is 永覺元賢 (無異元來's dharma-heir). MasterName fields are null, so no schema-level wrong-speaker, but the provenance note must be corrected before merge.

## 1. KWIC integrity — 6/6 verbatim, all lbs exact
| Occurrence | File | Verbatim? | lb |
|---|---|---|---|
| 兼中到。不落有無誰敢和。 | T47n1986B | EXACT | T:0525c07 ✓ |
| 如何是兼中到。師曰。撥開雲外路。 | X80n1565 | EXACT | X:0296a08 ✓ (+R138:0537b11 exactly as noted); continuation 脫去月明前 verbatim ✓ |
| 體用如如為兼中到。 | X73n1457 | EXACT | X:0861a13 ✓ (+R127:1006a10 ✓) |
| 此即兼中到也。造道之極，理事俱泯，非獨凡 | X72n1437 | EXACT | X:0541b19 ✓ (+R125:0715a08 ✓) |
| 兼中至，約功位雙彰時立，兼中到，約 | X72n1441 | EXACT | X:0681b18 ✓ (+R125:0994a06 ✓) |
| 寂音改兼中至為偏中至，以對正中來，大悞後學，今 | X72n1437 | EXACT (continues 今為訂之 ✓) | X:0539b11 ✓ (+R125:0710b18 ✓) |

No ellipsis/stitch/added punctuation in any Kwic. All X-canon FromLb use ed=X ✓.

## 2. Attribution — correct
- **Verse = 洞山良价 ✓ (required).** 師。作五位君臣頌云 verbatim at T:0525c01; full verse 兼中到。不落有無誰敢和。人人盡欲出常流。折合還歸炭裏坐 verbatim; canonical order confirmed by offsets 正中偏(9064) → 偏中正(9092) → 正中來(9120) → 兼中至(9148) → 兼中到(9176) — 兼中到 is indeed FIFTH/terminal.
- X80n1565 catechism: governing mulu = 福州普賢善秀禪師 — matches AttributionNote ✓; null ✓.
- X73n1457 (雲門麥浪懷禪師宗門設難 — title verified), X72n1437, X72n1441 (五位宗旨 mulu confirmed), X82n1571: analytical/raised, null ✓ throughout.

## 3. Allowlist — 7/7 IN
T47n1986B, X80n1565, X73n1457, X72n1437, X72n1441, X81n1568, X82n1571 all in zen-corpus.json. Every SourceText attests the headword (X81n1568: 9×; X82n1571: 15×) ✓.

## 4. Explanation honesty — quotes and counts verified
- 如何是兼中到: **104 in 57 texts — exact** ✓.
- 折合還歸炭裏坐: **24 across 20 texts — exact** ✓.
- Headword count: claimed 276/84 — replicates **exactly** under the entry's stated method (strict body-text excluding cb:mulu TOC = 275/84; method stated, nit only).
- Capping phrases attested immediately after 如何是兼中到: 撥開雲外路。脫去月明前 (X80n1565) ✓; 崑崙夜裏行 (J33nB294, T51n2077) ✓; 十道不通耗 (T48n2006, T51n2077, X68n1319) ✓.
- Self-definitions verbatim: 體用如如為兼中到 ✓; 此即兼中到也。造道之極，理事俱泯 ✓; 兼中至，約功位雙彰時立，兼中到，約功位雙泯時立 ✓ (contiguous in stripped text).
- X82n1571: 我此一喝，聖凡情盡，能所兩忘，妙盡有無，是兼中到也 verbatim ✓; 兼中到，即元要妙旨也 verbatim ✓.
- Note's variant data: 兼中至 **207** ✓, 偏中至 **85** ✓; the 寂音 emendation record verbatim ✓.
- FLAG (wording): "the phrase 功位俱隱 for this rank recurs 27 times" — the count is exact (**27**), and the corpus DOES tie the phrase to the rank (X72n1437, immediately after the verse: 兼中到，就功位俱隱時立 — grep-verified), but 19 of the 27 occurrences sit in the distinct 功勳 four-question catechism (轉功就位／功位齊彰／轉位就功／功位俱隱) with no explicit 兼中到 label. "for this rank" generalizes what only some witnesses state. Suggest: "…功位俱隱, which X72n1437 ties to this rank (兼中到，就功位俱隱時立), recurs 27 times."
- FLAG (quote punctuation, note-level): AttributionNote at 0681b18 quotes 「…功位雙泯時立，大意如此。」 — source continues 大意如此**，且止葛藤**。 (comma, not full stop). Quote-final punctuation altered; the Kwic itself and the Explanation's shorter quote are verbatim.
- FLAG (quote normalization, note-level): X73n1457 AttributionNote quote 「…為偏中正，體用如如為兼中到。如他宗之四賓主、三玄要，莫不皆然。」 — source has a gaiji/PUA rank-marker between 偏中正 and the comma, and reads 四賓主**．**三玄要 (middle dot, not 、). The Kwic 體用如如為兼中到。 is verbatim; only the extended note quote is normalized.

## 5. Multi-source — holds
84 allowlist texts; curated witnesses span T (Dongshan's record) + X lamp + two independent X commentaries + a polemical treatise. ✓

## 6. Describe-only — clean
Literal graph reading; the 至/到 near-synonym remark is a graph-sense statement (allowed category); the 4th/5th-rank distinction is carried entirely by quoted corpus contrasts (功位雙彰／雙泯, X72n1441); closing formula present. No banned vocabulary, no reading-menu.

## 7. Nesting / RelatedTerms — genuine
Rank-set cross-refs (正中偏／偏中正／正中來／兼中至) verified in canonical order; 偏中至 = attested variant (85×, 寂音 emendation documented in-corpus); 五位君臣 attested (五位君臣頌). RelatedMasters 洞山良价, 曹山本寂 in roster ✓. No coincidental-prefix relations.

## Punch list
1. **(blocking)** Fix the X72n1437 AttributionNote(s): 無異元來禪師廣錄 → 永覺元賢禪師廣錄 (same fix as t_ccd48e1c9145).
2. Reword the 功位俱隱 sentence to anchor "for this rank" to its actual witness (X72n1437: 兼中到，就功位俱隱時立) rather than all 27 occurrences.
3. Fix quote-final punctuation in the 0681b18 AttributionNote (大意如此，且止葛藤。) and normalize honestly or trim the X73n1457 note quote (四賓主．三玄要; intervening gaiji marker).
4. (optional nit) Headword count 276 includes 1 cb:mulu (TOC) duplicate; strict body-text = 275.
