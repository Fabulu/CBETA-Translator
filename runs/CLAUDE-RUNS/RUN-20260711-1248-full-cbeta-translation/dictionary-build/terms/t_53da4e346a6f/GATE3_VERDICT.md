# GATE 3 RE-AUDIT VERDICT — t_53da4e346a6f · 百尺竿頭

VERDICT: PASS

**Auditor:** Gate 3 re-audit (independent adversarial), 2026-07-11. Method: tag-stripped
(note/rdg excluded) exact-substring matching against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, lb re-anchoring, governing cb:mulu
extraction, allowlist-wide phrase counts, roster cross-check.

## 1. Prior punch list — all resolved
1. **[MAJOR] B25n0144 misidentified as 中峰廣錄 — FIXED.** Occurrence note now says 祖堂集
   (correct; explicitly "NOT 中峰廣錄"). Grep-confirmed at the source: the verse @0623b07 is
   governed by mulu level=2 head `岑和尚` (section head line `岑和尚嗣南泉` @0622a02) and is
   introduced `師當時有偈曰` — Changsha's own verse. **MasterName changed to Changsha Jingcen:
   justified by the governing head.** The follow-on `三聖和尚問：「承師有言『百尺竿頭須進步』…」
   師云：「朗州山，澧州水。」` sits immediately after, exactly as the entry describes.
   The "locus classicus is 五燈會元" phrasing was also amended: entry now says "earliest
   witness 祖堂集". ✓
2. **[MINOR] Counts — recounted and now reproducible.** Measured vs claimed: 百尺竿頭 623/196
   = claimed; 百尺竿頭進步 53/41 = claimed (the prior-fix figure); 百尺竿頭如何進步 72/51 exact
   and 88/58 with ≤1 punctuation = claimed; 十方世界是全身 15/15; 百尺竿頭坐底人 29/26;
   百尺竿頭不動人 8/8 — all exact. 竿頭進步 112 vs claimed 113, 更進一步 80 vs 81: off-by-one
   convention sensitivity, file counts identical (81/60). Non-blocking.
3. **[NIT] Shishuang Chuyuan dates — FIXED.** Note now gives 987–1040 "per master-dates.json";
   roster confirms floruit 987 / death 1040. Qingzhu (807–888) matches roster notes.

## 2. Regression re-grep — clean
All 5 KWICs verbatim, 1 hit each (X80n1565 2 hits, cited one @0094c18), lb anchors exact:
X80n1565 @0094c18 ✓, B25n0144 @0623b07 ✓, C078n1720 @0705a16 ✓, T48n2005 @0298c12 ✓,
D46n8930 @0030a06 ✓. All RelPaths in zen-corpus.json ✓. Roster: Changsha Jingcen and
Sansheng Huiran both present ✓.

## 3. Strip+enrich pass — describe-only, attested
Prose is descriptive throughout ("The contrast 得入 / 未為真 is the verse's own words");
no intent/force/"the point is" language.

Every added Chinese quote grep-verified verbatim in its cited allowlist file:
- C077n1710: `僧問法燈百尺竿頭如何進步燈云噁` (2 hits, incl. @0851c07 in the 茶陵郁 story) ✓
- T47n1996 @0675a14–15: `泉云。更進一步` and `百尺竿頭用進作什麼。僧不肯。官便打` ✓ — and the
  context confirms the second master is 瓦官 in this telling, exactly as the entry says
  (`僧復問瓦官。官云。…`), while X78n1556 @0807b09 reads `僧復問鹽官，官云：百尺竿頭用進作
  什麼？僧不肯，拂袖便出，官便打` — the 鹽官-variant claim is precisely right ✓
- C078n1720 @0756a16: `石霜示眾云百尺竿頭如何進步` ✓
- 茶陵郁 story: 教令看 @0853a12 + 凡三年 @0853a13 (C078n1720, co-located with 茶陵郁 @0853a10);
  由是每日叅詳 @0851c08 (C077n1710, co-located); 忽然大悟 co-located with the story in all
  three cited witnesses (C077n1710 @0851c10, C078n1720 @0853a14, D48n8939 @0108a06 —
  donkey-over-the-bridge context confirmed in D48n8939) ✓
- Newly cited files T47n1996, X78n1556, D48n8939 are all in the allowlist ✓

No fabricated Chinese, no wrong-speaker attribution, no allowlist contamination. The two
off-by-one counts are the only residue and are non-blocking.
