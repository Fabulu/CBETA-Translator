# Gate 3 Verdict — 小參 (t_c945c2cc0e79)

VERDICT: PASS

Independent adversarial re-derivation (fresh model, Claude Opus 4.8 acting as Gate 3). All checks
re-derived from the raw TEI; WORK.md not trusted as evidence.

## Per-occurrence findings

1. **T/T48/T48n2025.xml @1119c10** — `小參初無定所。看眾多少。或就寢堂。`
   EXACT contiguous tag-stripped substring, 1 hit, nearest `<lb ed="T">` = 1119c10. Context continues
   `或就法堂。至日午後。侍者覆住持云今晚小參…掛小參牌。當晚不鳴放參鐘。昏鐘鳴時…住持登座與五參
   上堂同` — every definitional claim in the Explanation (no fixed place, 寢堂/法堂 by assembly size,
   小參牌, 昏鐘 timing, abbot 登座 same as five-day 上堂) is verbatim in this passage of 勅修百丈清規
   (docNumber No. 2025; head 勅修百丈清規目錄). MasterName null correct (monastic code). PASS.
2. **T/T48/T48n2025.xml @1119c07** — `臘則移於昏鐘鳴。而謂之小參。可以敘世禮。`
   EXACT, 1 hit, nearest `<lb ed="T">` = 1119c07. Context: `…或受人特請。或謂亡者開示。或四節
   臘則移於昏鐘鳴。而謂之小參。…` PASS. (Nit, non-blocking: the KWIC opens mid-phrase — the source
   runs `或四節臘則移…`, so the leading 臘 is severed from 四節臘. Still exact+contiguous per rule;
   a cleaner span would start at 或四節.)
3. **T/T47/T47n1998A.xml @0812a28** — `當晚小參。大道只在口前。要且目前難覩。`
   EXACT, 1 hit, nearest `<lb ed="T">` = 0812a28. Context: `入院上堂。山僧未離泉州時…當晚小參。…`
   — Dahui's own evening address in 大慧普覺禪師語錄 (docNumber No. 1998A; head 大慧普覺禪師塔銘).
   MasterName 大慧宗杲 correct. PASS.
4. **T/T47/T47n1997.xml @0714a27** — `卷第八　　　上堂八　小參一`
   EXACT including the U+3000 ideographic spaces, 1 hit, nearest `<lb ed="T">` = 0714a27, inside
   圓悟佛果禪師語錄目錄 (`…卷第七　上堂七卷第八　上堂八　小參一卷第九　小參二…`). MasterName null
   is the CORRECT call (structural TOC apparatus, not an utterance) — the Gate-2 repair holds up.
   PASS.

## Checks

- **Allowlist:** T48n2025, T47n1998A, T47n1997 + SourceTexts X82n1571, J37nB394 ALL in
  `zen-corpus.json`. No contamination. (T48n2025 is on the 462-text allowlist, which is the mandatory
  authority; its monastic-code genre is therefore legitimate evidence.)
- **Multi-source:** Baizhang code (definition) + Dahui yulu (live use) + Yuanwu yulu (genre heading)
  = 3 independent texts. Spot-check: 小參 appears 599× in X82n1571, 206× in J37nB394; the stock form
  晚小參 independently confirmed in T47n1998A, T47n1997, X64n1260. `multi-source` justified.
- **Over-read:** none. "Informal convocation" contrasted with 上堂 is exactly what 勅修百丈清規 states
  (與五參上堂同 for procedure, differing in place/timing); no master-uniqueness claim.
- **Imported abstraction:** none — pure genre/format marker, deflationary.
- **Attribution honesty:** all four verified against surrounding context; nulls used where no speaker
  exists (code, TOC). Correct.

## Issues (tagged)

- (nit, non-blocking): occurrence #2 KWIC boundary splits the phrase 四節臘 (starts at 臘). Exact and
  contiguous, so no violation; optional polish only.

## Verified occurrences: 4/4 KWIC confirmed verbatim
