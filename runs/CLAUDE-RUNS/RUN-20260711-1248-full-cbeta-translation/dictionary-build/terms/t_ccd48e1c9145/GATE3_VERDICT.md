# GATE 3 VERDICT — t_ccd48e1c9145 · 正中來

**Auditor:** Gate 3 independent adversarial pass (Claude, from-scratch corpus re-derivation)
**Date:** 2026-07-12 01:10 +02:00
**Method:** tag-stripped extraction of every cited TEI file (note/rdg/orig excluded; cb:mulu tracked separately), exact-substring KWIC matching, lb-milestone position mapping per edition, full 462-file allowlist recount of every numeric claim, cb:mulu governing-head check at each occurrence offset, roster lookup, title verification against teiHeader.

VERDICT: REVISE

One real provenance defect (wrong text/author name in two AttributionNotes); everything else — every KWIC, every lb, every count, every mulu head — verifies exactly.

## THE DEFECT (blocking, trivial to fix)
**X72n1437 is 永覺元賢禪師廣錄, NOT 無異元來禪師廣錄.** Verified from the file's teiHeader (`<title level="m">永覺元賢禪師廣錄`, author byline (嗣法)道霈重編). 無異元來禪師廣錄 is a *different* text (X72n1435). The 曹洞五位/洞山五位頌註 commentary quoted at 0539c17 and 0544a23 is therefore 永覺元賢's (Yongjue Yuanxian, 無異元來's dharma-heir — the file mentions 無異 20×, which likely seeded the confusion), compiled by 道霈. Affected fields:
- Occurrence 0539c17 AttributionNote: "X72n1437 (無異元來禪師廣錄)" → 永覺元賢禪師廣錄.
- Occurrence 0544a23 AttributionNote: "Same 曹洞五位 commentary (X72n1437)" — inherits the misnaming.
- "later Caodong commentators" in the Note stays true either way; MasterName fields are null, so no schema-level wrong-speaker — but the note misassigns authorship of a quoted self-definition and must be corrected before merge.

## 1. KWIC integrity — 6/6 verbatim, all lbs exact
| Occurrence | File | Verbatim? | lb |
|---|---|---|---|
| 正中來。無中有路隔塵埃。 | T47n1986B | EXACT | T:0525c04 ✓ |
| 如何是正中來。師曰。徧界絕纖埃。 | X80n1565 | EXACT | X:0301a06 ✓ (+R138:0547b09 exactly as noted) |
| 正中來一位，即是得法身，亦即是正位。 | X72n1437 | EXACT | X:0539c17 ✓ (+R125:0711b12 ✓) |
| 正中來則入正位而轉身者也。 | X72n1437 | EXACT | X:0544a23 ✓ (+R125:0720b06 ✓) |
| 正中來，乃五位之樞紐，前二位入此者也 | X72n1441 | EXACT | X:0681b03 ✓ (+R125:0993b09 ✓); continuation 後二位從此出者也 verbatim ✓ |
| 正中來，人境俱奪也。兼中至 | X82n1571 | EXACT | X:0279b21 ✓ (+R141:0347a16 ✓) |

No ellipsis/stitch/added punctuation. All X-canon FromLb use ed=X ✓.

## 2. Attribution — correct at every governing mulu
- **Verse = 洞山良价 ✓ (required).** 師。作五位君臣頌云 is verbatim at T:0525c01 in 瑞州洞山良价禪師語錄, and the five rank-names follow in canonical order at strictly increasing offsets: 正中偏(9064) → 偏中正(9092) → 正中來(9120) → 兼中至(9148) → 兼中到(9176).
- X80n1565 catechism: governing mulu = 明州雪竇聞庵嗣宗禪師 (level 4) under 天童覺禪師法嗣 (level 3) — matches the AttributionNote word-for-word; null ✓.
- X82n1571: governing mulu = 南昌府百丈瑞白明雪禪師 ✓; 雲門九週 verbatim at X:0279b10 preceding the shout-mapping ✓; null ✓.
- X72n1441: governing mulu = 五位宗旨 ✓ ("五位宗旨 section of the volume" — correct; the volume is 為霖禪師雲山法會錄, consistent with the entry's neutral wording); null ✓.
- Commentary/catechism → null throughout ✓ (per the raised/analytical rule).

## 3. Allowlist — 8/8 IN
T47n1986B, T47n1987A, X80n1565, X72n1437, X72n1441, X82n1571, X81n1568, X78n1553 all in zen-corpus.json. Every SourceText attests the headword: T47n1987A has 正中來 5× (incl. Caoshan's own 故云正中來也); X81n1568 9×; X78n1553 3× ✓.

## 4. Explanation honesty — every quote and count verified
- Caoshan pairing 君為正位。臣為偏位。臣向君是偏中正。君視臣是正中偏 — verbatim in T47n1987A (T:0527a10) ✓; "Caoshan's own record" correct (撫州曹山元證禪師語錄 = 本寂).
- Full verse 正中來。無中有路隔塵埃。但能不觸當今諱。也勝前朝斷舌才 verbatim ✓.
- 如何是正中來: **102 in 56 texts — exact** ✓.
- Capping phrases all attested immediately after 如何是正中來 in allowlist texts: 松瘁何曾老？花開滿未萌 (J27nB198, and X72n1437 attributing it 普賢秀云) ✓; 屎裏翻筋斗 (J33nB294, X79n1559) ✓; 獼猴戴席帽 (X71n1412, X81n1568) ✓.
- Commentator quotes verbatim: 正中來一位，即是得法身，亦即是正位 ✓; 入正位而轉身者也 ✓; 正中來，乃五位之樞紐 ✓ (+前二位入此/後二位從此出 paraphrase matches source exactly).
- X82n1571: 即此一喝，不帶名言，是正中來也 ✓ and the full 以濟宗論之，正中偏奪人也，偏中正奪境也。正中來，人境俱奪也。兼中至，人境俱不奪也。 verbatim ✓.
- Headword count: claimed 288/85 — replicates **exactly** under the entry's stated method (strict body-text excluding cb:mulu TOC = 287/85; method is stated, nit only).
- Note's variant data: 兼中至 **207** ✓, 偏中至 **85** ✓; 寂音改兼中至為偏中至，以對正中來，大悞後學 verbatim in X72n1437 (X:0539b11) ✓; lamp-compilation claim checked — 續燈正統 (X84n1583) does carry 如何是正中來 (5×) ✓.

## 5. Multi-source — holds
85 allowlist texts; curated witnesses span T (Dongshan's record, Caoshan's record) + X lamp records + X commentary. ✓

## 6. Describe-only — clean
Literal graph reading; deployment split (catechism-question vs commentator lemma) is observable; all glosses are the corpus's own self-definitions, quoted; ranks-interrelation stated structurally; closing formula present ("carries no gloss here beyond the literal reading and its place in Dongshan's set"). No banned vocabulary, no annotator readings.

## 7. Nesting / RelatedTerms — genuine
正中偏 / 偏中正 / 兼中至 / 兼中到 = the attested rank-set (order verified in T47n1986B); 偏中至 = the attested 寂音 variant (85×); 五位君臣 = the attested set-name (五位君臣頌 verbatim). All semantic, none coincidental-prefix. RelatedMasters 洞山良价, 曹山本寂 in roster ✓.

## Punch list
1. **(blocking)** Fix both X72n1437 AttributionNotes: 無異元來禪師廣錄 → 永覺元賢禪師廣錄 (the commentator is 永覺元賢, record compiled by 道霈; 無異元來's 廣錄 is X72n1435).
2. (optional nit) Headword count 288 includes 1 cb:mulu (TOC) duplicate; strict body-text = 287.
