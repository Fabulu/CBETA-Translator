# Gate 3 Verdict — 恁麼 (t_7180f7431520)

VERDICT: PASS

Independent adversarial re-derivation from the primary Chinese (Gate 3, fresh model, 2026-07-11).
All checks run against `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` and
`Assets/Data/zen-corpus.json`. Entry NOT modified.

## Per-sense findings

### Sense 1 (only sense): "thus / in this way (like this)" — PASS

**Check 1 — KWIC exact + contiguous (4/4 verified verbatim; all four span an `<lb>` break
and join contiguously after tag-stripping):**
- occ 1 · `T/T51/T51n2076.xml` lb 0240c12 — file lines 4681–4682:
  `祖曰。什麼物恁麼來。曰說似` (0240c12) + `一物即不中。祖曰。` (0240c13) →
  「祖曰。什麼物恁麼來。曰說似一物即不中。」 exact contiguous. VERIFIED.
- occ 2 · `X/X80/X80n1565.xml` lb 0109a24 — file lines 8390–8392:
  `頭曰。恁麼也不得。不恁麼` (ed="X" 0109a24) + `也不得。恁麼不恁麼總不得。子作麼生。師罔措。`
  (0109b01) → KWIC exact contiguous across the page break. VERIFIED. (Entry cites ed="X"
  numbering in this dual X/R138 file — correct.)
- occ 3 · `X/X80/X80n1565.xml` lb 0128b08 — file lines 9825–9826:
  `又問。恁` (0128b08) + `麼來不立。恁麼去不泯時如何。師曰。` (0128b09) → KWIC exact
  contiguous; note grep on the whole string fails because 恁/麼 straddle the lb — confirmed by
  reading the joined lines. VERIFIED.
- occ 4 · `T/T51/T51n2076.xml` lb 0295b10 — file lines 9594–9595:
  `賓云總` (0295b10) + `不與麼。師便打。` (0295b11) → 「賓云總不與麼。師便打。」 exact
  contiguous. VERIFIED. (Taishō base text `<lem wit="#wit.orig">賓云總不與麼</lem>`; the Ming
  variant reads 曰沒交涉 — KWIC correctly follows the base text.)
- No ellipses, no stitching, no altered punctuation. FromLb = line where each KWIC begins.

**Check 2 — RelPath real + allowlisted:** both files exist; `T/T51/T51n2076.xml` (line 301)
and `X/X80/X80n1565.xml` (line 453) are in `zen-corpus.json`. No contamination.

**Check 3 — Multi-source claim:** `multi-source` HOLDS. Two lamp records, but four DISTINCT
episodes (Huineng↔Huairang; Shitou↔Yaoshan; monk↔Luopu; Kebin↔Xinghua) across six masters —
not copies of one passage. (五燈會元 partially derives from 景德傳燈錄, but no cited passage
duplicates another cited passage.)

**Check 4 — Over-read:** none. The 恁麼也不得…總不得 double bind is in fact a shared formula
(my grep of X80n1565 alone finds it also at 0295b03, 0328b06, 0367c24, 0393a13, 0393b16,
0418c15, 0427c07) — and the entry does NOT claim it as Shitou's signature; it says "Masters
wield it" and cites Shitou↔Yaoshan as one instance. Consistent with the corpus.

**Check 5 — Imported abstraction:** none — the opposite. The entry explicitly refuses the
reified "Suchness/Thusness" noun and renders a bare colloquial demonstrative. The 與麼 variant
claim is corroborated in-corpus (e.g. Linji's 與麼聽法底人, T47n1985 0499b16, same frame as
恁麼) plus the curated occ 4.

**Check 6 — Speaker attribution (all four correct; the three nulls are exactly right):**
- occ 1 null: chapter head 南嶽懷讓禪師 (T51n2076 lb 0240c07, file line 4676); the narrative
  reads 乃直詣曹谿參六祖。祖問… — so 祖 = Huineng speaks 什麼物恁麼來 inside HUAIRANG's
  chapter. Crediting the chapter-master would be wrong; null + AttributionNote naming Huineng
  is honest. CORRECT.
- occ 2 Shitou Xiqian: chapter head 澧州藥山惟儼禪師 (X80n1565 ed="X" 0109a19, file line 8385)
  under 石頭遷禪師法嗣; narrative 首造石頭之室。便問…頭曰 (0109a22–24) — 頭 = 石頭. The whole
  KWIC is Shitou's single utterance. Corroborated by the parallel at 0418c15 (藥山問石頭…頭曰).
  CORRECT.
- occ 3 null: chapter head 澧州洛浦山元安禪師 (0127c16, file line 9783); the 恁麼來/恁麼去
  line is an anonymous monk's question (又問…時如何); 師 (Luopu) only answers 鬻薪樵子貴…
  Null is right. CORRECT.
- occ 4 null: chapter head 魏府興化存獎禪師 begins lb 0295b01 (file line 9585); hit at 0295b10
  is inside it; 賓 = 克賓維那 (named 0295b08–09) speaks 總不與麼; 師 (Xinghua) 便打 — an
  action, not speech. Null is right. CORRECT.

## Issues (tagged)

None blocking. Non-blocking observations:
- INFO: Frequency/idiom counts in Note (~27,436 occ.; 恁麼來 ≈642, 恁麼去 ≈1,175) not
  re-derived corpus-wide by Gate 3; contextual only.
- INFO: occ 4 KWIC rests on the Taishō base reading where the Ming edition substitutes
  曰沒交涉 (app note 0295012). The KWIC matches the base text verbatim, so no defect; flagging
  for completeness since the 與麼-variant documentation hangs on this occurrence.

## Verified occurrences: 4/4 KWIC confirmed verbatim
