# Gate 3 Verdict — 參禪 (t_b291fe703ff1)

VERDICT: REVISE

Independent adversarial re-derivation (fresh model, no trust in WORK.md). The philology, KWICs,
attributions, allowlist, and multi-source claim ALL hold — but the three X70n1400 occurrences cite
the WRONG lb edition. X-canon files carry dual line numbering (`ed="X"` and `ed="R122"`); the rule
is that the `ed="X"` numbers are correct, and this entry cites the R122 numbers. Fixable in place;
do not merge until fixed.

## Per-occurrence findings (single sense, 4 occurrences)

1. **X/X70/X70n1400.xml cited 0661b09 · 參禪須是鐵漢，著手心頭便判**
   - KWIC: FOUND exactly once, contiguous, verbatim (13-char span, no interleaved tags).
   - Lb: the match sits after `<lb ed="X" n="0681b15"/>` … `<lb ed="R122" n="0661b09"/>`. Cited
     0661b09 is the R122 number; the correct ed="X" lb is **0681b15** (FromLb=ToLb).
   - Attribution: immediately preceded by 「元宵，上堂。」 in 高峰原妙禪師語錄 — Gaofeng speaking.
     MasterName and AttributionNote (元宵上堂) correct.
2. **X/X70/X70n1400.xml cited 0671a01–0671a02 · 只遮生死一大事，乃是參禪學道之喉襟**
   - KWIC: FOUND x1, contiguous, verbatim (crosses `<lb ed="X" n="0686a14"/>` +
     `<lb ed="R122" n="0671a02"/>` only).
   - Lb: cited 0671a01/0671a02 are R122 numbers; correct ed="X" lbs are **0686a13 → 0686a14**.
   - Attribution: Gaofeng's own 示眾-style discourse (「生死事大，無常迅速。生不知來處，謂之生大。
     死不知去處，謂之死大。」 directly precedes). Correct.
3. **X/X70/X70n1400.xml cited 0667b13 · 資生貴圖求富，參禪貴圖求悟**
   - KWIC: FOUND x1, contiguous, verbatim (13-char span, no interleaved tags).
   - Lb: cited 0667b13 is the R122 number; correct ed="X" lb is **0684b19** (FromLb=ToLb).
   - Attribution: immediately preceded by 「上堂：」 — Gaofeng. Correct (matches AttributionNote).
4. **B/B25/B25n0145.xml 0761b14 · 人心浮淺口說參禪。但欲明悟機緣以資談柄**
   - KWIC: FOUND x1, contiguous, verbatim. Lb re-derived: start and end both after
     `<lb n="0761b14" ed="B"/>` — cited FromLb=ToLb=0761b14 correct.
   - Attribution: Zhongfeng Mingben's own continuous discourse on 話頭/看話 in 天目中峰廣錄
     (authorial voice, 「嗟乎。人心浮淺口說參禪…」). Correct. Notably the surrounding passage itself
     ties 參禪 to 話頭 investigation, which supports the entry's huatou framing from within the corpus.

## Cross-checks

- **Allowlist:** X/X70/X70n1400.xml and B/B25/B25n0145.xml both present in
  `Assets/Data/zen-corpus.json`. No contamination.
- **Multi-source:** two independent masters/texts (Gaofeng 語錄; Zhongfeng 廣錄). Upheld.
- **Explanation quotes re-attested:** 參禪須是鐵漢 (x1), 著手心頭便判 (x1), 參禪學道之喉襟 (x1),
  參禪貴圖求悟 (x1), plus the Zhongfeng critique line. No fabricated paraphrase.
- **Dropped-Dahui honesty:** the Note openly documents excluding the T47n1998A 0864c passage over
  speaker separability, and keeps Dahui out of Occurrences — the right call, honestly recorded.
- **Over-read / imported abstraction:** rendering "investigate Chan" is literal; "meditate" is
  correctly rejected. Minor caution (non-blocking): "by the Song–Yuan the word is effectively
  synonymous with the huatou method" is a broad historical gloss beyond the four citations, though
  the hedge ("effectively") and the B25 context (參禪 discussed via 話頭 practice) keep it defensible.

## Issues (tagged)

- **WRONG_LB_EDITION** (citation-coordinate error; per the X-canon rule, `ed="X"` lb is correct, and
  the entry cites `ed="R122"` numbers) · evidence: in X70n1400 the matches sit after
  `<lb ed="X" n="0681b15"/>` / `<lb ed="X" n="0686a13"/>`–`0686a14` / `<lb ed="X" n="0684b19"/>`,
  while the entry cites 0661b09 / 0671a01–0671a02 / 0667b13 (all `ed="R122"`) · recommended fix:
  - occ 1: FromLb/ToLb 0661b09 → **0681b15**
  - occ 2: FromLb 0671a01 → **0686a13**, ToLb 0671a02 → **0686a14**
  - occ 3: FromLb/ToLb 0667b13 → **0684b19**
  Note: WORK.md shows Gate 2 "repaired" the lbs in exactly the wrong direction (the original entry
  had the correct ed="X" values 0681b15 / 0686a13–a14 / 0684b19 and Gate 2 moved them to R122).
  Revert that repair. Systemic warning: Gate 2's X-canon lb convention may have miscorrected other
  entries too (e.g. other entries in this batch cite R-only lb numbers such as X81n1571 0214b13,
  ed="R140") — worth a targeted sweep.

## Verified occurrences: 4/4 KWIC confirmed verbatim (3 with wrong-edition lb coordinates)
