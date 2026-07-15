# WORK — 水牯牛 (t_36aa29eb1287)

**Term:** 水牯牛 (water buffalo) · **Id:** t_36aa29eb1287 · **CreatedBy:** Fabulu · **Status:** verified

## Summary of the entry
Two senses, both grounded in verbatim Zen-corpus occurrences:

1. **Corpus-wide (SenseKey null) — "water buffalo."** The ordinary draft buffalo, the most
   common animal image in the Chan record. Two stock figurative uses: (a) the ox-herding
   metaphor for taming one's nature → "white ox in the open field" (露地白牛); (b) the
   self-deprecating "reborn as a buffalo" trope (classic form: Guishan's). Also used to
   level/tease (Zhaozhou's "two water buffaloes"). `multi-source` — Guishan (X80n1565),
   Da'an (T51n2076), Zhaozhou (J24nB137).

2. **Nanquan Puyuan (SenseKey "Nanquan Puyuan").** The buffalo bound to 異類中行, "going
   among the different kinds": the 知有底人 who goes to the patron's house to become a
   water buffalo, plus the all-fours gesture (兩手拓地). `disputed` by ATTRIBUTION — the
   strongest self-as-named-buffalo line is Guishan's, the ox-herding line floats between
   Da'an and Nanquan, and 異類中行 is also a Caodong term. Support: X80n1565, J24nB137,
   C077n1710.

## Fixes applied (repairing Codex gate-3 FAIL)
1. **KWIC verbatim (5 fixed).** Every non-verbatim KWIC replaced with an EXACT contiguous
   tag-stripped substring of the cited file, grepped directly:
   - X80n1565 0319b07 (Guishan) — removed the "…" ellipsis; full exact span.
   - T51n2076 0267c07 (Da'an) — removed "…" and the truncation; restored the true
     continuation `…露地白牛常在面前。終日露逈逈地。趁亦不去也。`
     (NB: the verdict itself mis-transcribed 迢迢; the file actually reads **逈逈** —
     used the file's true text.)
   - X80n1565 0091b11 (Nanquan hall-talk) — removed "…"; full exact span.
   - X80n1565 0127b16 (Nanquan / 知有底人) — removed "…"; full exact span.
   - The two J24nB137 and one C077n1710 KWICs were already verbatim; re-confirmed.
2. **Contamination removed.** Deleted the `B/B19/B19n0103.xml` (禪林象器箋, NOT in
   `zen-corpus.json`) occurrence from sense 2 and dropped it from that sense's SourceTexts.
3. **Sense-2 target tightened.** PreferredTarget changed from the "realized/enlightened
   self" framing to a literal deflationary target: "the water buffalo as figure for the
   'person who knows-there-is' (知有底人) going among the different kinds (異類中行)." The
   enlightened-self reading is retained only as an explicit interpretive gloss in the
   Explanation/Note, not as the rendering.
4. **Witness count.** Sense 2 retains ≥2 independent allowlisted witnesses after removing
   B19: X80n1565, J24nB137, C077n1710. Kept `Validation = disputed` (Guishan / Caodong
   attribution caveats stand).
5. **Envelope → single object.** Rewrote from a DictionaryFile envelope
   ({SchemaVersion, Entries:[…]}) into a single PascalCase DictionaryEntry object,
   matching the other terms.

## Verification
- All 7 final KWICs confirmed as exact contiguous substrings of the tag-stripped cited
  files (scripted containment check — all PASS).
- Allowlist check: B19n0103 = 0 hits in `zen-corpus.json`; X80n1565, T51n2076, J24nB137,
  C077n1710 all present. Zero non-allowlist RelPaths remain.
- No "…" ellipses, no altered punctuation in any KWIC.

## 2026-07-13 item-8 target and sense correction

- Re-tested all 1,076 hits/257 files against the `異類中行`, `知有底人`, `露地白牛`, ox-herding, Guishan, Nanquan, and Caoshan families. The earlier Nanquan sense was a menu of deployments and interpretations, not a different buffalo referent.
- Merged the Nanquan material into the corpus-wide animal sense. Eight exact witnesses now cover Guishan's named-buffalo problem, ox-herding, Zhaozhou's teasing use, Nanquan's two buffalo deployments, Caoshan's explicit statement that the buffalo was borrowed for the different kind, and an independent crop-damage interview.
- The definition still holds across the expanded family: every retained passage denotes a water buffalo, even when the animal is deployed in a public question, case, self-reference, or technical comparison. No `enlightened self` interpretation is asserted.
- sense-target-distinguishability: MERGE — `water buffalo` and the former Nanquan target identified the same animal in different recorded deployments; the master-specific wording did not name a second thing.

## Semantic remediation r002

- feedback-inference-verdict: KEEP one animal sense. The former Nanquan-specific interpretation was correctly merged away; the entry reports self-naming, oxherding, teasing, different-kind, and crop-damage deployments without claiming that the buffalo secretly is the self.
- feedback-observations: Guishan's posthumous naming problem, Da'an's oxherding account, Zhaozhou's two-buffalo tease, Nanquan's raised buffalo and patron-house answer, Caoshan's explicit borrowed-for-the-different-kind statement, and Shoushan's crop-damage test are anchored.
- feedback-falsification-searches: tested animal versus self, Guishan versus Nanquan ownership, oxherding versus posthumous declaration, person-who-knows-there-is, going among kinds, white ox family, teasing epithet, and floating transmission attribution.
- feedback-counterexamples: Caoshan explicitly says the animal is provisionally borrowed for the different kind, which licenses reporting that deployment but not replacing `water buffalo` with an enlightened-self gloss; the Da'an/Nanquan parallel blocks false certainty about ownership of one oxherding line.
- feedback-scope: one corpus-wide water-buffalo animal referent across public cases, oxherding language, self-reference, teasing, and technical comparison.
- lookup-probes: water buffalo; buffalo; water ox; ox one herds; oxherding buffalo.
- opening-interpretation-verdict: KEEP — the opening names the animal and immediately shows its distinct Zen deployments while rejecting the imported self interpretation.
- plain-english-image-verdict: PASS — the reader sees the buffalo being raised, herded, named, questioned, and accused of crop damage.
