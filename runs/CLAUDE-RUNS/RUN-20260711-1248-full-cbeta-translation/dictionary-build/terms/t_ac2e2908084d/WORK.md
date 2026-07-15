# WORK — 見性成佛 (t_ac2e2908084d) · batch b003

## Gloss target
"see the nature, become Buddha" — the culminating half of the four-phrase slogan.

## Evidence (Zen-scoped allowlist only)
- 602 raw occurrences across 206 allowlist texts. Pervasive as the four-clause formula 教外別傳／不立文字／直指人心／見性成佛.

## Sense analysis
ONE corpus-wide sense: the school's compressed banner-claim that awakening = directly seeing one's own nature, with Buddhahood not added from outside. Two functional registers of the SAME sense:
- (a) banner phrase, usually inside the four-clause formula (直指人心，見性成佛);
- (b) topic masters gloss — 雲居智 to 繼宗: 性即佛。佛即性。故曰見性成佛 (deflationary equation).
Not master-specific; a shared banner → single null sense.

## Nested-term handling (§5b)
CONTAINS the standalone entry 見性 (t_c13928184189, already done/verified). Kept as a SEPARATE entry; genuine constituent 見性 + partners 直指人心/教外別傳/不立文字 placed in RelatedTerms. NOT merged. 見性成佛 = 見性 with its consequence (成佛) stated.

## Multi-source gate
PASS (multi-source): 3 independent curated texts — 六祖大師法寶壇經 (T48n2008, Huineng's own voice), 五燈會元 (X80n1565: Yunju gloss + Yaoshan report), 景德傳燈錄 (T51n2076).

## Anti-fakeout
Rendered literally "see [one's] nature, become Buddha" — NOT "attain a metaphysical essence." Grounded in 性即佛佛即性 and the plain slogan. ewk's candidate rendering ("See nature, become Buddha") independently confirmed against the Chinese.

## Curated occurrences (4, all curated)
1. X80n1565 0051a21 — 雲居智 gloss 性即佛佛即性故曰見性成佛 (ed=X lb; answer is the master's, MasterName null as 雲居智 not confirmed canonical)
2. T48n2008 0351c15 — Huineng 大梵寺: 普願法界眾生，言下見性成佛 (Huineng)
3. T51n2076 0429a22 — 祖師西來只道見性成佛。其餘所說不及此說 (whole-point claim, null)
4. X80n1565 0109a23 — Yaoshan→Shitou: 甞聞南方直指人心。見性成佛。實未明了 (slogan as known Southern teaching, null; ed=X lb)

## X-canon note
X80n1565 FromLb values use ed="X" (verified), not the co-located ed="R138" reprint.

## Verification
verify.py: 4/4 OK — KWICs exact-contiguous, allowlist-clean, FromLb matches, term present. JSON valid.

## GATE 2 (Claude adversarial verify-and-repair)
- 4/4 KWICs re-derived by targeted grep of the cited file: all EXACT contiguous, zero ellipses.
- FromLb per-edition check: X80n1565 occ1/occ4 use ed="X" (0051a21 / 0109a23) correctly, NOT the co-located ed="R138" reprint; T occs use ed="T". All match nearest preceding lb.
- Allowlist: all RelPaths (X80n1565, T48n2008, T51n2076 + SourceTexts C077n1710, X68n1318) in zen-corpus.json. Zero contamination.
- Attribution confirmed against source: occ1 雲居智 answering 繼宗 (two-speaker; null OK, name unconfirmed); occ2 Huineng at 大梵寺 in the Platform Sutra's 師復曰 voice (Huineng CORRECT); occ3 hall-discourse whole-point claim (null OK); occ4 Yaoshan (藥山惟儼) asking Shitou (石頭) (two-speaker, null OK).
- Explanation quotes grep-verified: four-phrase 教外別傳，不立文字，直指人心，見性成佛 (13 hits comma-form; 直指人心見性成佛 66 hits), 性即佛佛即性 / 祖師西來只道見性成佛 / 甞聞南方… all attested. Reading kept literal ("see [one's] nature, become Buddha"), no metaphysical-essence over-read.
- Multi-source PASS (T48n2008 / X80n1565 / T51n2076). Nesting: 見性成佛 ⊃ 見性 kept as separate cross-referenced entries; RelatedTerms genuine.
- STATUS → verified. No repairs needed.
