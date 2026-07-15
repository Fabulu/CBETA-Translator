# Gate 3 Verdict — 上堂 (t_4f7bd98ad40f)

VERDICT: PASS

Independent adversarial re-derivation from the primary Chinese (Gate 3, fresh model, 2026-07-11).
All checks run against `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` and
`Assets/Data/zen-corpus.json`. Entry NOT modified.

## Per-sense findings

### Sense 1 (only sense): "ascend the hall (give a formal Dharma-hall address)" — PASS

**Check 1 — KWIC exact + contiguous (4/4 verified verbatim):**
- occ 1 · `T/T47/T47n1985.xml` lb 0496b14 — grep hit on file line 252:
  `<lb n="0496b14" .../>...府主王常侍與諸官請師升座，師上堂，云：「山` — KWIC
  「府主王常侍與諸官請師升座，師上堂，云：」 is an exact contiguous substring on a single
  body line (punctuation ，/： preserved). VERIFIED.
- occ 2 · `T/T47/T47n1988.xml` lb 0545a18 — file line 443:
  `師上堂良久云。夫唱道之機。固難諧剖。若也` — KWIC exact contiguous, single line. VERIFIED.
- occ 3 · `T/T48/T48n2001.xml` lb 0002a03 — file line 1016:
  `入寺上堂云。古人道盡十方世界。是箇解脫` — KWIC exact contiguous, single line. VERIFIED.
- occ 4 · `T/T48/T48n2025.xml` lb 1113a14 — file line 753:
  `上堂令客頭掛上堂牌。維那於僧堂。早粥遍` — KWIC exact contiguous, single line. VERIFIED.
- No ellipses, no stitching, no altered punctuation in any KWIC. All FromLb values match the
  nearest-preceding `<lb n>` where the KWIC begins.

**Check 2 — RelPath real + allowlisted:** all four files exist and all four RelPaths appear in
`zen-corpus.json` (grep hits at lines 253, 258, 273, 300). No contamination.

**Check 3 — Multi-source claim:** `multi-source` HOLDS. Four independent witnesses: 臨濟語錄
(T47n1985), 雲門廣錄 (T47n1988), 宏智廣錄 (T48n2001), 敕修百丈清規 (T48n2025) — three
masters' records plus the monastic code; four distinct passages, none copies of each other.

**Check 4 — Over-read:** none. The entry explicitly says "Not master-specific — it is the shared
institutional form." No uniqueness claim made; corpus (e.g. 上堂 as ubiquitous section header in
X80n1565 and elsewhere) supports the shared-form framing.

**Check 5 — Imported abstraction:** none. Rendering is deflationary-literal (ascend + hall =
formal Dharma-hall address); institutional details (sponsor request, 良久 pause, 入寺上堂,
上堂牌 placard) are each anchored to a verified occurrence, not imported doctrine.

**Check 6 — Speaker attribution (all correct):**
- occ 1 Linji Yixuan: KWIC is the opening of 鎮州臨濟慧照禪師語錄 (byline 住三聖嗣法小師慧然集
  on the immediately preceding line 0496b13); 師 = Linji. The KWIC describes Linji's act of
  ascending, ends at 云： before any quoted speech — no two-speaker contamination. CORRECT.
- occ 2 Yunmen Wenyan: T47n1988 is the 雲門匡真禪師廣錄; the hit sits directly under the
  對機三百二十則 head (lb 0545a17); 師 = Yunmen. CORRECT.
- occ 3 Hongzhi Zhengjue: hit at 0002a03 falls under mulu 泗州大聖普照禪寺上堂語錄
  (lb 0001b06, file line 960); the whole guanglu is Hongzhi's; 入寺上堂 = inaugural address.
  CORRECT.
- occ 4 null: procedural passage of the 敕修百丈清規 (no speaker); null is the honest choice.
  CORRECT. (Baizhang Huaihai in RelatedMasters is appropriate as the code's eponym, and is not
  claimed as a speaker.)

## Issues (tagged)

None blocking. Two non-blocking observations:
- INFO: Frequency figures in Note (403 allowlist files / ~54,024 occurrences) were not
  re-derived by Gate 3 (corpus-wide count out of targeted-search scope); they are contextual,
  not evidentiary, and nothing in the entry depends on their exactness.
- INFO: RelatedMasters includes Baizhang Huaihai via the 清規 attribution-by-title; the
  AttributionNote for occ 4 correctly leaves MasterName null, so no laundering.

## Verified occurrences: 4/4 KWIC confirmed verbatim
