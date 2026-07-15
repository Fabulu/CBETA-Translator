# GATE3 VERDICT — t_097f38f58678 · 庭前柏樹子

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial, Claude/Fable) · **Date:** 2026-07-11
**Method:** All KWICs re-derived from raw TEI XML (tag-stripped, whitespace-normalized) with raw-offset mapping back to `<lb>` / `<cb:mulu>`; explanation phrases re-grepped across the 462-file allowlist.

## 1. KWIC integrity — 5/5 PASS (verbatim, contiguous, no ellipsis/stitching)
- K1 `J/J24/J24nB137.xml` — found verbatim WITH the 「」／？ punctuation; FromLb `0358b16` exact AND ToLb `0358b19` independently re-derived from the KWIC end offset — exact.
- K2 `J/J24/J24nB137.xml` — found; lb `0365b13` exact.
- K3 `T/T48/T48n2004.xml` — found; lb `0256c22` exact.
- K4 `X/X66/X66n1296.xml` — found; lb `ed="X" 0252b01` exact (correct X edition, not R115 1017a05).
- K5 `X/X80/X80n1565.xml` — found; lb `ed="X" 0408c20` exact.

## 2. Attribution — PASS (matches the "only Zhaozhou's own record named" rule exactly)
- K1/K2: J24nB137 TEI title verified `趙州和尚語錄` (唐從諗說 文遠記) — Zhaozhou's own record; 師 = Zhaozhou. MasterName "Zhaozhou Congshen" (in roster) correct on both.
- K3: T48n2004 verified 萬松老人評唱天童覺和尚頌古從容庵錄; governing mulu = `47 趙州柏樹`, head `第四十七則趙州柏樹` — the entry's "從容錄 case 47 (趙州柏樹)" claim is confirmed to the digit. Raised case → null, correct.
- K4: X66n1296 verified 宗門拈古彙集; mulu = `蘄州五祖山法演禪師` — Wuzu Fayan's 示眾 raising Zhaozhou's case → null with attributing note, correct.
- K5: mulu = `建康府華藏密印安民禪師` (五燈會元 Anmin section); context confirms 悟 = 圜悟克勤 (Anmin's teacher; `悟出蜀。居夾山` etc.) handing over the phrase, `師即洞明` = Anmin. Raised case → null, correct.
- No later raising carries a MasterName. Rule applied cleanly.

## 3. Allowlist — PASS. All 4 RelPaths present in zen-corpus.json.

## 4. Explanation honesty — PASS
- 如何是祖師西來意 / 和尚莫將境示人 / 我不將境示人 / 庭前柏樹子 — all inside K1 verbatim.
- 柏樹子還有佛性也無 / 有 / 幾時成佛 / 待柏樹子成佛 — all inside K2 verbatim (待柏樹子成佛 also in 5 further allowlist files).
- 恁麼會則不是了也 / 恁麼會方始是 — inside K4 verbatim; 師即洞明 — inside K5 verbatim; 趙州柏樹 — the actual mulu/head of 從容錄 case 47.
- **Cypress-not-oak note (specific re-check):** correct and well-placed. 柏 is Platycladus/arborvitae (cypress family), not oak; the Note flags the famous "oak tree" English rendering as a mistranslation without importing any abstraction into the reading itself. Deflationary handling of the tree ("not a symbol standing in for some hidden meaning") is grounded in the quoted 莫將境示人 exchange, not imported.

## 5. Multi-source — PASS. Four independent texts: Zhaozhou's own record (J24nB137), 從容錄 (T48n2004), 宗門拈古彙集 (X66n1296), 五燈會元 (X80n1565). `multi-source` justified.

## 6. Nesting / RelatedTerms — PASS
- 祖師西來意 — genuine: it is the question the term answers, present inside K1/K3/K4 KWICs. Real semantic link, not a coincidental prefix.
- 柏樹子 — genuine constituent: Zhaozhou himself abbreviates the head term to 柏樹子 in K2 (the entry's note documents this consciously, per guide §5b rule 6).

## Punch list
None blocking. One minor observation (non-blocking, optional polish):
- `RelatedMasters` includes "Dahui Zonggao" though no occurrence or explanation sentence cites him. The link IS corpus-genuine — 庭前柏樹子 occurs 22x in T47n1998A (大慧語錄), incl. Dahui raising 五祖師翁's wrong-way/right-way version — but the entry itself carries no evidence for it. Either add a one-line note/occurrence or drop the name. Not a FAIL flag; all other RelatedMasters are evidenced in-entry.

Defect count: 0 (1 minor observation)
