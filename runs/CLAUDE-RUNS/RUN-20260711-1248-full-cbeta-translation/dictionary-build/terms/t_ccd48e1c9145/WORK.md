# WORK — t_ccd48e1c9145 · 正中來

**Rendering:** "coming from within the upright"
**Senses:** 1 (corpus-wide, SenseKey null). **Validation:** multi-source.
**Concordance (allowlist-scoped):** 288 occurrences in 85 texts (X-canon lamp/commentary + T-canon 語錄/傳燈錄).

## Method
Allowlist-only concordance; nearest-`<lb>` tracked; every KWIC raw-verified verbatim (6/6 PASS).
FromLb uses the ed="X" number for X-canon files (co-located ed="R" recorded in AttributionNote,
not used). The T-canon source verse uses ed="T".

## What the corpus shows (describe-only)
- **Literal:** 正 upright/correct + 中 within + 來 come/coming. The 君臣 pairing is the corpus's
  own: 君為正位。臣為偏位。臣向君是偏中正。君視臣是正中偏 (T47n1987A, Caoshan's record).
- **Origin:** the THIRD of Dongshan Liangjie's Five Ranks. His own record (T47n1986B,
  瑞州洞山良价禪師語錄) has 師。作五位君臣頌云…正中偏·偏中正·正中來·兼中至·兼中到, the verse-line
  being 正中來。無中有路隔塵埃。但能不觸當今諱。也勝前朝斷舌才.
- **Deployment:** (1) fixed catechism 如何是正中來 (102× / 56 files), capped variously
  (徧界絕纖埃; 松瘁何曾老。花開滿未萌; 屎裏翻筋斗; 獼猴戴席帽 — each grep-verified as a cap to this
  question). (2) commentators' lemma.
- **Self-definitions (grep-verified):** 正中來一位，即是得法身，亦即是正位 (X1437);
  入正位而轉身者也 (X1437); 正中來，乃五位之樞紐，前二位入此者也 (X1441, the pivot).
- **Contrast the texts draw:** a 五燈全書 memorial 上堂 maps a single shout onto the ranks
  (即此一喝，不帶名言，是正中來也) and restates them 以濟宗論之: 正中偏奪人也，偏中正奪境也。
  正中來，人境俱奪也 (X1571).

## Variant note (per task)
Order is stable at rank 3 (正中來). The neighbouring 4th-rank NAME is unstable: 兼中至 (207×) vs
偏中至 (85×); X1437 records 寂音 (Juefan Huihong) 改兼中至為偏中至，以對正中來 and rejects it as
大悞後學. Reported in both this entry and 兼中到.

## Attribution
T47n1986B verse → Dongshan Liangjie (his own record). Catechism + commentary occurrences are
raised/analytical → MasterName null. cb:mulu heads verified: X80n1565@0301a06 sits in the entry
of 明州雪竇聞庵嗣宗禪師 (天童覺法嗣); X82n1571@0279b21 in the entry of 南昌府百丈瑞白明雪禪師.

## GATE 2 (verify-and-repair) — 2026-07-12
Independent re-derivation (linearizer with <note>/<rdg>/<orig> dropped; counts cross-checked by a
second gap-tolerant-regex method).
- KWICs: 6/6 EXACT CONTIGUOUS; lbs 6/6 correct (ed=X for X-canon; claimed co-located ed=R lbs
  also verified 11/11 across both rank entries). Contamination: 0. Attribution: verse = 洞山良价,
  catechism/commentary null — correct per rule.
- REPAIRED (draft counts under-derived): 267/82 → **288/85**; 如何是正中來 74/48 → **102/56**;
  兼中至 191 → **207**; 偏中至 83 → **85**.
- REPAIRED (grounding): the "正 = 君/host pole" gloss was unattested as phrased — replaced with
  Caoshan's own 君為正位。臣為偏位… (T47n1987A, added to SourceTexts); Dongshan verse quoted with
  file punctuation (。 not ，).
- REPAIRED (accuracy): X82n1571 AttributionNote said "雲門宗 section" — wrong; it is the entry of
  百丈瑞白明雪禪師, a memorial 上堂 (雲門九週 = his late master's 9th anniversary); the 以濟宗論之
  quote's punctuation corrected to the file's (…奪境也。正中來…); added the attested
  即此一喝，不帶名言，是正中來也.
- REPAIRED (links): dropped 雲居道膺 from RelatedMasters (no attested content in this entry);
  洞山良价 + 曹山本寂 both grounded in cited texts. RelatedTerms (the interrelating ranks) kept.
- JSON valid.

## Files
- entry.v2.json (1 entry, 1 sense, 6 curated occurrences). STATUS = verified.
