# WORK — t_78f95517a347 · 生死事大

**Rendering:** "birth-and-death is the great matter"
**Senses:** 1 (corpus-wide, SenseKey null). **Validation:** multi-source.
**Concordance (allowlist-scoped):** 321 occurrences in 135 texts (B3, C2, D1, J64, T12, X53).

## Method
Concordance over the 462-text Zen allowlist only (`zen-corpus.json`), via a tag-stripping
linearizer that tracks the nearest preceding `<lb>` per edition. Every KWIC re-verified as a
verbatim contiguous substring of the file after XML-tag stripping.

## What the corpus shows (describe-only)
- **Literal:** 生 birth + 死 death + 事 matter + 大 great.
- **Dominant collocation:** 生死事大，無常迅速 (84× / 56 files); 無常迅速 total 160× (76 outside
  the pair). One text names the pair: 生死事大，無常迅速，兩句現話 (J28nB208, "two ready-made phrases").
- **Deployment range (observable):** reason to leave home (念生死事大 19×, 為生死事大 39×; e.g.
  念生死事大，奮志尋師 / 念生死事大，乃薙染完具, X82n1571); supplicant's plea (問生死事大請師相救);
  inscription (每書生死事大四字於案頭 — the 續燈正統 notice of 雲棲蓮池袾宏大師 at seventeen;
  堂門書「生死事大」 — the emperor's lodging in 北遊集); sermon opening (上堂：「大眾！又是九月半也，
  生死事大，無常迅速); raised 示眾 with directive (須是將生死兩字貼在額頭上始得, raising 天如和尚).
  Locus classicus: the Yongjia–Huineng exchange (覺曰：生死事大，無常迅速…).
- **Related:** 大事因緣 / 一大事因緣 (the "one great matter," 699× / 415×) — a separate stock phrase.

## Attribution
All six curated occurrences are narrated / raised / biographical → MasterName null. The Yongjia
line is Yongjia Xuanjue's but every allowlist witness retells it. No self-definition of the
`X者…也` form exists; the closest is the metalinguistic 兩句現話.

## Self-definition found
Metalinguistic only: 生死事大，無常迅速，兩句現話 (the corpus labels the pair as a set two-phrase saying).

## GATE 2 (verify-and-repair) — 2026-07-12
Independent re-derivation (tag-stripped linearizer with <note>/<rdg>/<orig> dropped, nearest-lb
per edition; counts cross-checked by a second gap-tolerant-regex method on apparatus-stripped raw).
- KWICs: 6/6 EXACT CONTIGUOUS, lbs 6/6 correct (C077n1710 KWIC occurs 2× in the file; the cited
  0896c08 is the 三家村人失却火 instance — now documented in the AttributionNote).
- Contamination: 0 (all RelPaths + SourceTexts in the 462 allowlist).
- Attribution: all null (narrated/raised/biographical) — correct; no fixes.
- Interpretation: none found; describe-only closing sentence retained.
- REPAIRED (draft counts were under-derived): 272/124 → **321/135** (spread includes T-canon,
  which the draft's "B, C, D, J and X" line omitted); pair 58/45 → **84/56**; 無常迅速 145 →
  **160 (76 outside pair)**; 念生死事大 16 → **19**; 為生死事大 42 → **39**; 大事因緣 603 → **699**;
  一大事因緣 350 → **415**.
- REPAIRED (accuracy): X84n1583 "a monk" → named notice of 雲棲蓮池袾宏大師 (年十七，補邑庠…
  一日，失手碎茶甌，有省 — all grep-verified); J26nB180 "a master had 堂門書…" → the EMPEROR's
  lodging (上所居), dialogue with 木陳道忞 (每對此輒萬緣寢削); J25nB171 AttributionNote ellipsis
  quote replaced by the full contiguous span (…須是將生死兩字貼在額頭上始得。』) + attested
  continuation (只將不生不死四字貼在額頭上 / 遂拈拄杖趁出); leave-home claim re-grounded on exact
  attested strings; X82n1571 added to SourceTexts (now quoted).
- All new quotes grep-verified against the corpus (21/21 PASS). JSON valid.

## Files
- entry.v2.json (1 DictionaryEntry, 1 sense, 6 curated occurrences)
- STATUS = verified
