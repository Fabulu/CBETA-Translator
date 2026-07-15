# WORK — 祖師西來意 (t_49efe4fed8d4)

## Concordance (Zen allowlist only, 462 texts)
- 祖師西來意 → **1736 hits / 239 files**. Almost always in the frame 如何是祖師西來意.
- Top texts: X80n1565 (五燈會元, 128), X81n1571 (117), X81n1568 (100), X82n1571 (89), X78n1553 (81), T51n2077, T51n2076, C077n1710 (古尊宿), etc. — spread across 燈錄, 語錄, 頌古 alike.

## Sense analysis
**One sense (corpus-wide, SenseKey=null).** A fixed liturgical test-question: "What is the meaning of
the Patriarch's (Bodhidharma's) coming from the West [India→China]?" = the living point of Chan. It is
almost never answered informationally; the master deflects with a turning-word. The variety of the
deflections is the evidence that it is *one* stock question deployed everywhere, not several meanings:
- Zhaozhou → 庭前柏樹子 (cypress in the garden) — J37nB370 0003b01, and dozens of re-tellings.
- Mazu → a kick to the chest (欄胸一踏倒) — X73n1447 0446b12.
- Xianglin Chengyuan → 坐久成勞 (sitting long, you tire) — J32nB273 0222a01.
- Longya Judun → 待石烏龜解語 (when the stone tortoise talks) — J32nB272 0188c22.
- Guishan → 大好燈籠 (what a fine lantern) — J28nB202 0006c03.
- Others sampled: 汾陽 金風吹秀水, 九峰 有力者負之而趨, 香林 坐久成勞, Deshan-tradition 竪拳, "西來無意"
  denials, the 五祖演 "write the meaning in empty space" set-piece (X71n1405, T48n2002A).

No master-specific bending that changes the *referent* — masters differ only in their *reply*, which is
the genre's whole point. So a single corpus-wide sense is correct; no master-keyed sub-sense needed.

## Multi-source verdict
**multi-source** — trivially. 239 independent allowlist texts, every major lineage, ~1000 years.

## Deflationary check
Rendered "the meaning of the Patriarch's coming from the West" — literal (祖師=Patriarch, 西來=coming
from the West, 意=meaning/intent). Explicitly NOT glossed as a mystical essence; the entry states the
phrase functions as a ritual prompt that masters refuse to fill with doctrine. Avoids the 凡情聖見 fakeout.

## Honest thin spots
- Did not census all 1736 hits; corpus-wide sense asserted from a representative, cross-lineage sample +
  counts. The five curated answers are verified verbatim (strip-and-map extractor, nearest <lb> confirmed).
- The "西來無意 / the question has no meaning" denials are noted in the explanation but not separately
  curated — they are the same sense read self-critically, not a distinct meaning.

## GATE 2 (Claude adversarial verify-and-repair) — 2026-07-11
- **KWICs:** all 5 re-derived by targeted per-file search + tag-strip contiguity check → EXACT
  CONTIGUOUS VERBATIM (no ellipsis, no stitching, no altered punctuation). Char-lengths 28–41.
- **Allowlist:** all 5 RelPaths (J37nB370, X73n1447, J32nB273, J32nB272, J28nB202) ∈ zen-corpus.json. No contamination.
- **FromLb:** all 5 confirmed = nearest preceding `<lb n>` (0003b01, 0446b12, 0222a01, 0188c22, 0006c03).
- **Multi-source:** 5 independent allowlist texts, 5 masters, all lineages → `multi-source` stands.
- **Over-read/abstraction:** explanation is literal/deflationary; no imported mystical essence. OK.
- **Nesting (§5b):** RelatedTerms (庭前柏樹子, 西來意, 祖師意, 如何是佛, 教外別傳) are genuine synonyms/constituents/
  deliberate cross-refs — no coincidental prefixes. OK.
- **Verdict: VERIFIED.** No corrections required; entry.v2.json unchanged.
