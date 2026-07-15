# GATE 3 VERDICT — t_66792ea088de · 拈古

VERDICT: PASS

Independent adversarial audit, 2026-07-11. Method: tag-stripped exact-substring match with
raw-offset mapping back to `<lb>` and governing `cb:mulu` chain; phrase counts over the full
462-file allowlist (notes/rdg removed).

## What PASSED

1. **KWIC integrity — 6/6 verbatim** (including the full-width spaces in the T47n1997 TOC line):
   - X67n1307 @0469a13 `古來有拈古、頌古、徵古、代古、別古` ✓
   - X66n1296 @0001a02 `宗門拈古彙集序` ✓ (4 hits in file; first is @0001a02 as claimed)
   - B27n0152 @0526b02 `余閱雪竇拈古至百丈再叅馬祖因` ✓
   - B27n0152 @0580a09 `呈拈古十餘則` ✓
   - B25n0145 @0692a05 `卷第三拈古頌古` ✓
   - T47n1997 @0714b08 `卷第十八　　拈古下　頌古上` ✓
2. **Attribution.** Genre term; all six occurrences are enumerations, titles, TOC lines, or acts
   of composing/reading niangu — MasterName null on every one, correct per the genre rule.
3. **Allowlist.** All 5 RelPaths in zen-corpus.json ✓.
4. **Explanation honesty — every claim reproduced:**
   - The five-genre enumeration is verbatim at X67n1307 @0469a13 ✓.
   - X67n1307 IS Wansong's 評唱 on Hongzhi's 拈古: the file's own title is
     萬松老人評唱天童覺和尚拈古請益錄 (宋 正覺拈古　元 行秀評唱) — the 請益錄 claim confirmed
     from the document itself ✓.
   - T47n1997 (圓悟佛果禪師語錄) TOC runs 拈古上 / 拈古中 / 拈古下 then 頌古上/下 ✓.
   - Cross-claim in the B27n0152 attributionNote ("the same case Yuanwu's 拈古 raises in
     T47n1997"): T47n1997's 拈古上 opens 舉。百丈再參馬祖 (@0788c23, again @0798b12) ✓.
   - "Also called 拈提": attested 354× in 142 allowlist files (e.g. X66n1296 舉唱之、拈提之;
     X67n1307 天童終日拈提) ✓.
5. **Count claims vs. measured (allowlist, notes/rdg stripped):** 拈云 ~4293 → 4412 ✓; 師拈云
   ~1050 → 1170 ✓ (tilde tolerance); 頌古 ~1085 → 1102 ✓; 代古 62 → 64 (within counting-method
   tolerance); 別古 36 → 36 EXACT ✓; 徵古 17 → 17 EXACT ✓.
6. **Multi-source.** 5 independent witnesses (X/X, B/B, T) spanning an anthology, two masters'
   records, a discourse-record TOC, and Wansong's commentary ✓.
7. **RelatedTerms.** 頌古 / 徵古 / 代古 are the sibling genres from the very enumeration cited;
   公案 is the object of the genre — all genuine semantic relations, no coincidental overlap ✓.

## Nits (non-blocking)

- 師拈云 stated "~1050×", measured 1170 (≈11% under). Tilde-qualified, so acceptable, but a
  recount would tighten it.
- 代古 stated as exact "62×", measured 64 — likely note/variant-reading handling; state the
  counting method or re-derive.

No defects that touch evidence integrity. PASS.
