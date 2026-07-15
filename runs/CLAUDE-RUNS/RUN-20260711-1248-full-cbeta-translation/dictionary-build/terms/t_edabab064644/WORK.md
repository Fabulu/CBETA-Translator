# WORK — 疑情 (t_edabab064644) · batch b003

**Gloss target:** "the sensation of doubt" — the ball of doubt driving huatou practice (Dahui/Gaofeng).

## Method
- Zen-scoped concordance (allowlist filter). **Corpus-wide count: 937 occurrences across 208 texts.** A technical term of 看話 (kanhua) practice.
- Read the explicit definition in 博山禪警語 and the founding/lineage uses (Dahui, Gaofeng, Xueyan).

## Sense analysis
One corpus-wide sense (SenseKey=null). 疑 doubt + 情 feeling/state = "the doubt-sensation." NOT intellectual skepticism: it is the sustained, felt, bodily mass of not-knowing raised by concentrating on a 話頭. Boshan defines it («何謂疑情？») via the born-whence/dead-whither wonder; describes it as 「結在眉睫上」 (knotted on the brows), bursting as 「撲破疑團」 (smashing the ball of doubt). Measured by 「大疑大悟，小疑小悟，不疑不悟」. Dahui makes it the hinge of liberation; Gaofeng narrates it erupting on the 萬法歸一 huatou.

## Multi-source gate → PASS (multi-source)
Four named masters across three texts, spanning both surviving lineages (Linji-kanhua: Dahui→Xueyan→Gaofeng; Caodong: Boshan):
| RelPath | Text | Lb | Master | Role |
|---|---|---|---|---|
| X63n1257 | 博山禪警語 | 0756a15 (ed=X) | Wuyi Yuanlai (Boshan) | 「做工夫貴在起疑情。何謂疑情？」 — explicit definition |
| X63n1257 | 博山禪警語 | 0756a17 (ed=X) | Wuyi Yuanlai | 「疑情頓發，結在眉睫上…」 — somatic description |
| T47n1998A | 大慧語錄 (答呂郎中隆禮) | 0930b25 | Dahui Zonggao | 「疑情不破。生死交加」 — the hinge |
| T48n2024 | 禪關策進 | 1101a05 | Gaofeng Yuanmiao | 「自此疑情頓發，直得東西[不辨]」 — his own awakening |
| T48n2024 | 禪關策進 | 1100b02 | Xueyan Zuqin | 「參禪須是起疑情。小疑小悟。」 |

## Speaker confirmation (verified at nearest section head)
- X63n1257: author line 「明 元來說　成正集」 → single-author admonitions, spoken by Boshan → **MasterName=Wuyi Yuanlai** (canonical 無異元來). 示衆/警語 voice.
- T47n1998A 0930b25: nearest mulu = 「答呂郎中隆禮」 (a letter by Dahui) → **Dahui Zonggao**.
- T48n2024 1101a05: nearest head = 「天目高峯妙禪師示眾」 → **Gaofeng Yuanmiao** (autobiographical, on the 萬法歸一，一歸何處 huatou).
- T48n2024 1100b02: nearest head = 「袁州雪巖欽禪師普說」 → **Xueyan Zuqin** (Gaofeng's teacher).
- (禪關策進 is an anthology; each excerpt is head-attributed, so per-occurrence attribution is reliable.)

## Byte-for-byte KWIC verification
All five KWICs confirmed present in tag-stripped source files (whole-file grep). All single physical lines, no ellipsis/stitching. CBETA's odd punctuation on the Dahui line (「生死交加疑。情若破」) was avoided by cutting the KWIC at 「生死交加」.

## Links
RelatedMasters: Dahui Zonggao, Gaofeng Yuanmiao, Xueyan Zuqin, Wuyi Yuanlai. RelatedTerms: 話頭, 疑團, 公案, 大疑.

## GATE 2 (Claude adversarial verify+repair)
- All 5 KWICs re-grepped: EXACT contiguous verbatim after tag-strip.
  - X63n1257 0756a15 「做工夫貴在起疑情。何謂疑情？」 ✓ (line 363, ed="X")
  - X63n1257 0756a17 「則疑情頓發，結在眉睫上，放亦不下，趂亦不去」 ✓ (line 365, ed="X")
  - T47n1998A 0930b25 「疑情不破。生死交加」 ✓ (line 11314; raw 生死交加疑。情若破 — KWIC correctly cut at 生死交加)
  - T48n2024 1101a05 「一歸何處。自此疑情頓發。直得東西」 ✓ (line 431)
  - T48n2024 1100b02 「參禪須是起疑情。小疑小悟。」 ✓ (line 368, anchor tags stripped)
- Contamination: 0. All RelPaths in allowlist.
- Attribution re-verified at 禪關策進 (anthology) section heads:
  - T48n2024 1100b02: preceding head 袁州雪巖欽禪師普說 (line 349) = Xueyan Zuqin ✓
  - T48n2024 1101a05: preceding head 天目高峯妙禪師示眾 (line 407) = Gaofeng Yuanmiao ✓
  - T47n1998A 0930b25: preceding head 答呂郎中(隆禮) (line 11284) = Dahui's letter ✓
  - X63n1257: single-author 博山禪警語 (明 元來說) = Wuyi Yuanlai ✓
- Explanation quotes grep-verified: 撲破疑團 ✓ (contiguous after stripping anchor tags, X63n1257 line 366); 「大疑大悟，小疑小悟，不疑不悟」 VERIFIED verbatim WITH commas in X63n1257 (Boshan, line 366-367) and X64n1260 (line 5151) — both allowlist, so commas kept.
- REPAIRS (Explanation quotes made verbatim to cited source):
  - 「疑情不破，生死交加」 → 「疑情不破。生死交加」 (T47n1998A has 。)
  - 「萬法歸一，一歸何處」 → 「萬法歸一。一歸何處」 (T48n2024 has 。)
  - 「自此疑情頓發，直得東西不辨，南北不分」 → 「自此疑情頓發。直得東西不辨。南北不分」 (T48n2024 has 。)
- Multi-source: 4 named masters / 3 texts, both lineages → holds.

STATUS: verified
