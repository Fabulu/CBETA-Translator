# Gate 3 Verdict — 大悟 (t_db4a932ce500)

VERDICT: PASS

Verifier: independent adversarial pass (fresh model). All evidence re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with a tag-stripping contiguity
checker that DROPS `<note>`/`<rdg>` apparatus content and records `lb n=` at match
start/end (preferring the file's own tags; X-canon dual-edition handled per spec —
`ed="X"` is authoritative).

## Per-sense findings (single sense, SenseKey null)

### Check 1 — KWIC exact + contiguous: 4/4 PASS
1. T/T51/T51n2076.xml 0299b28-29 「師於言下大悟云。元來黃檗佛法無多子。」 — FOUND x1,
   lb 0299b28→0299b29 exact. Context is the canonical narrative: 義玄親問佛法…三問三遭被打 →
   大愚's 黃檗恁麼老婆 → the KWIC → 大愚搊住云。者尿床鬼子….
2. X/X80/X80n1565.xml 0271b06-07 「大悟底人為甚麼却迷。師曰。破鏡不重照。落花難上枝。」 —
   FOUND x1. My checker reported lb 0488a17→0488a18: that is the paired `ed="R138"` value; the
   file carries dual tags `<lb ed="X" n="0271b06"/><lb ed="R138" n="0488a17"/>` (and b07/a18) at
   exactly this point — the entry cites the `ed="X"` numbers, which is correct per spec. Verbatim
   incl. the 却 (not 卻) of this witness.
3. J/J25/J25nB171.xml 0564a27-28 「舉僧問華嚴：「大悟底人為甚麼卻迷？」嚴云：「破鏡不重照，
   落花難上枝。」」 — FOUND x1, lb 0564a27→0564a28 exact, with this witness's own CBETA
   punctuation （：「」？，) verbatim — the entry did NOT normalize punctuation across witnesses.
4. J/J10/J10nA158.xml 0074a07-08 「大悟十八遍，小悟不計數，本是宋儒言，非大慧所說。」 —
   FOUND x1, lb 0074a07→0074a08 exact.

No ellipses, no stitching. Each KWIC occurs exactly once in its file.

### Check 2 — RelPath real + Zen allowlist: PASS
All four (T51n2076, X80n1565, J25nB171 = 天隱和尚語錄, J10nA158 = 密雲禪師語錄) exist and are in
`Assets/Data/zen-corpus.json`. No contamination.

### Check 3 — Multi-source: PASS
Independence correctly reasoned: T51 (Linji's 於言下大悟) and X80 (Huayan Xiujing's 大悟底人 case)
are two independent texts AND two different sayings. J25 is the SAME case as X80 (a 舉 citation) —
the entry itself says it is corroboration for the attribution, not an independent attestation of a
different saying; multi-source does not rest on it. J10 is a fourth text using the term. Claim holds.

### Check 4 — Over-read: none blocking
Attribution of the 大悟底人卻迷 case to Huayan Xiujing is corroborated by an independent witness
(J25: 舉僧問**華嚴**…**嚴**云) — exactly the right way to support a name claim. Minor observation
(non-blocking): the Explanation's "i.e. a true awakening does not relapse" is an interpretive gloss
on 破鏡不重照，落花難上枝; it is the standard reading and is flagged as inference ("i.e."), and the
rendering of 大悟 itself does not depend on it.

### Check 5 — Imported abstraction: none
"Great awakening / to wake up thoroughly" is literal (大 + 悟). The Explanation explicitly refuses
the metaphysical reading ("not attaining a metaphysical Absolute").

### Check 6 — Attribution honesty: PASS — exemplary
- T51 0299b28: section head verified at 0299b15 (raw line 9959): 鎮州臨濟。義玄禪師。曹州南華人也 —
  師 = Linji Yixuan. CORRECT.
- X80 0271b06: section head verified at `ed="X"` 0271a19 (raw line 20537): cb:mulu + head
  京兆華嚴寺休靜禪師; NO intervening head before the KWIC; the intervening narrative is the 洞山
  dialogue (汝記吾言。向南住有一千人…), 初住福州東山之華嚴。眾滿一千, and the 後唐莊宗 summons —
  all matching the AttributionNote. 師 = Huayan Xiujing (Dongshan's heir). CORRECT.
- J25 0564a27: two-speaker quoted case (舉僧問…嚴云) → MasterName null. CORRECT per gate rule.
- J10 0074a07: the famous 大悟十八遍 line is cited ONLY for its own disclaimer 本是宋儒言，非大慧所說,
  MasterName null, and the Note field explicitly forbids crediting Dahui. This is attribution honesty
  done right — a popular misattribution surfaced and encoded as disputed instead of laundered.

## Issues (tagged)
None blocking. (Minor, optional: the "does not relapse" reading of 破鏡不重照 is interpretive —
already hedged with "i.e."; no change required.)

## Verified occurrences: 4/4 KWIC confirmed verbatim
