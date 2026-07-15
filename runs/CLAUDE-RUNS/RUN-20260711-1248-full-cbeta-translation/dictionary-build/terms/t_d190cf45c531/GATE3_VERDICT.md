# Gate 3 Verdict — 話頭 (t_d190cf45c531) — RE-VERIFICATION after repair

VERDICT: PASS

Both prior REVISE issues are FIXED and independently confirmed from source: the X72n1437 passage is
now correctly attributed to Yongjue Yuanxian (his own 上堂 to layman 張華宇 in 永覺元賢禪師廣錄 — not
Boshan's 參禪警語), and the 0900b05 AttributionNote now reads 提掇. All 7 KWICs exact contiguous
verbatim; allowlist clean; multi-source holds for both senses; no imported abstraction. One
non-blocking prose nit noted below (does not gate the merge).

## Confirmation of the prior fixes (re-derived from source, not from WORK.md)

- Fix (a) — X/X72/X72n1437.xml attribution:
  - Title `永覺元賢禪師廣錄` present ×3 (incl. the <title> element); `參禪警語` occurs **0×** in the
    file — the old Boshan/"Chan Police Warnings" attribution is gone from source reality, and the
    entry no longer claims it.
  - The 上堂 opener `上堂。今日張華宇居士命老僧冒登此座` verified EXACT ×1 at lb X 0396a18 / R125 0424a06
    — matching the new AttributionNote's "~0396a18".
  - Extracted the full cleaned span from the opener to the KWIC (298 chars): one continuous talk —
    識心達本/忘情默契 framing, the 中庸/大學/孟子/孔子 comparisons, then `切不得落于知解…將通身精神、通身
    力量用在一句無義味話頭上，精研之久，一朝黑漆桶忽然爆裂` — no speaker change; the 老僧 addressing
    張華宇居士 is Yuanxian himself. 博山 appears in the file only as a revered elder elsewhere
    (0386a07), not as this speaker. MasterName "Yongjue Yuanxian" → CORRECT.
  - Explanation now "(Yongjue Yuanxian: 用在一句無義味話頭上)" and RelatedMasters now lists
    "Yongjue Yuanxian" (Wuyi Yuanlai removed) → CORRECT.
- Fix (b) — the 0900b05 AttributionNote parenthetical now "(提掇)": the KWIC itself ends
  `…行住坐臥但時時提掇` → CORRECT. The Explanation's separate use of 提撕 remains legitimate Dahui
  vocabulary — `時時提撕` verified ×7 in T47n1998A (first at 0886a03, immediately before the first
  curated KWIC).

## Per-sense findings

### Sense 1 (plain: the point/topic of a saying) — PASS
1. **KWIC exact (2/2, each ×1, tag-stripped contiguous):**
   - X/X79/X79n1557.xml at X 0080b08..10 / R136 0573a08..10 (FromLb 0573a09 = the R-edition line of
     話頭, consistent with the entry-wide convention). Dongshan speaks the line (曹舉似洞山，山云…).
   - X/X85/X85n1590.xml at X 0156c12..13 / R145 0597b03..04.
2. **Allowlist:** X79n1557, X85n1590, X80n1565 all in zen-corpus.json.
3. **Multi-source:** two independent episodes in two texts; X80n1565 independently carries BOTH the
   Dongshan story (好箇話頭 ×2, at R138 0151a05..06) and the Explanation's 話頭也不照顧 (verified
   verbatim at R138 0431b17..18 — tag-split across an <lb>, invisible to raw grep, exact after
   stripping). Holds.
4. **No over-read / no imported abstraction:** "no mystique: simply the saying under discussion" is
   deflationary and matches the passages.

### Sense 2 (Dahui: the watched critical phrase) — PASS
1. **KWIC exact (5/5, each ×1):**
   - T/T47/T47n1998A.xml at 0886a04..06 and 0900b05..07 (Dahui's yulu; his own instruction).
   - X/X72/X72n1437.xml at X 0396b05..07 / R125 0424a17..0424b01 — now correctly Yuanxian (above).
   - X/X72/X72n1440.xml at R125 0938a16..17; 為霖 ×18 in the file (Weilin Daopei's record) —
     MasterName null + "later retrospective" remains honest.
   - X/X70/X70n1400.xml at R122 0700a09; preceding context `見斷橋倫，令參…` at 0700a08 confirms the
     Gaofeng biography sequence; Curated=false + Xueyan-assigns note remains accurate.
2. **Allowlist:** T47n1998A, X72n1437, X72n1440, X70n1400, X63n1255 all allowlisted; X63n1255
   spot-checked as a real uncurated witness (話頭 ×4, first at R112 0913b03).
3. **Multi-source:** Dahui ×2 + Yuanxian + Weilin retrospective + Gaofeng record → ≥3 independent
   masters/texts. Holds with the corrected attribution.
4. **No imported abstraction:** 無義味 / 疑情 / 黑漆桶爆裂 all verbatim from the cited files; the
   "method-object" reading is the sources' own logic. The Song-innovation claim is the corpus's own
   statement (X72n1440), not an imported thesis.

## Issues (tagged)
- NIT (non-blocking, sense 1 Explanation): the clause "a master … scolds a monk for missing it"
  introduces two quotes whose cited instances actually run junior→master: in X85n1590 an interlocutor
  tells the 師 `老大宗師話頭也不識` (師曰：放你三十棒), and in X80n1565 it is 曉愚 telling 五祖戒
  `老老大大。話頭也不照顧`. Mitigations: the entry's own glosses expose the true direction ("a grand
  old master who doesn't…"), the occurrence MasterName is honestly null, the lexical point (話頭 = the
  point of what was said, which one can fail to catch) is direction-independent, and 話頭也不照顧 IS
  attested master→monk elsewhere in the allowlisted corpus (X73n1448: 上堂。僧問：…師云：話頭也不照顧).
  Recommended future tweak: "…or someone is scolded for missing it". Not a named-master misattribution;
  does not block merge.
- INFO (carried over, systemic): X-canon FromLbs follow the R-edition lb numbering (verified again on
  all five X occurrences); the link pipeline (ZenUriParser) should be confirmed against this convention
  once, globally.

## Verified occurrences: 7/7 KWIC confirmed verbatim (plus 6 secondary quotes/anchors re-derived:
title ×3, 上堂 opener 0396a18, 時時提撕 0886a03, 話頭也不照顧 0431b17, 好箇話頭@X80 0151a05, 見斷橋倫 0700a08)
