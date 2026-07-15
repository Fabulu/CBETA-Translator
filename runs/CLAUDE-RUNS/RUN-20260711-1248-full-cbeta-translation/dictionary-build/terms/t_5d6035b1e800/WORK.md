# WORK — 露地白牛 (t_5d6035b1e800) — CROSS-REFERENCE of the 水牯牛 buffalo entry

## Concordance (Zen allowlist only)
- 露地白牛 → **341 hits / 153 files**. Spread evenly across 燈錄/語錄/頌古 (D48n8939 12, X78n1556 11,
  C077n1710 11, X82n1571 9, B25n0144 8, T51n2077 7, X80n1565 6, T51n2076 4, ...).

## Reconciliation with BUFFALO_ENTRY.v2.json / BUFFALO_PILOT.md
The prompt's question: **is 露地白牛 the "matured" ox-herding image? whose? multi-source?**

**Verdict — YES, matured ox; no single owner; strongly multi-source.**
1. **It is the tamed end-state of the ox-herding sequence.** The buffalo (水牯牛) is the still-wild
   nature that must be watched and reined back from the crops; after long taming "it now turns into the
   white ox in the open" — verbatim, twice, in the same herding line:
   - T51n2076 0267c07: 只看一頭水牯牛…既久…如今變作箇露地白牛常在面前 (景德傳燈錄).
   - X86n1607 0890b06: 三十年看一頭水牯牛…如今變成露地白牛，躶躶地放他不肯去 (30 years → let loose, won't leave).
   So 露地白牛 = the fully tamed / realized nature that needs no more herding. This *extends* the buffalo
   entry, whose corpus-wide sense already lists 露地白牛 in RelatedTerms and quotes the same Da'an line.
2. **Whose?** No single owner.
   - **Lotus-Sutra origin:** the great white-ox cart on open ground (大白牛車 / 露地) of the burning-house
     parable = the one true vehicle. Chan writers cite it explicitly: X69n1356 0588a13 華嚴論主深歎法華經中露地白牛.
   - **Chan locus classicus:** the Guiyang ox-herding line credited to **Changqing Da'an** at Guishan's
     assembly — the SAME line that floats between Da'an and "Nanquan said" in the sources (flagged in
     BUFFALO_PILOT CAVEAT). So the maturation image inherits the buffalo entry's floating attribution.
   - **Dongshan** pairs it against Nanquan's buffalo (X67n1307 0866a04: 洞山道：露地白牛，牧人懶放).
3. **Multi-source:** yes, decisively — 153 texts, Lotus base + Guiyang ox-herding + a whole 如何是露地白牛
   koan-question tradition (Deshan 吽吽 X67n1299; Touzi 叱叱; Xingshan↔Linji B14n0082) + Beichan Zhixian's
   much-quoted 烹露地白牛分歲 year-end case (J32nB276, and 10+ re-tellings).

## Sense structure
One corpus-wide sense (SenseKey=null). Beichan's "cook the white ox for year-end" is the *same referent*
in a famous rhetorical set-piece, not a distinct meaning — folded into the explanation + one occurrence,
not split into a master-keyed sense. The 如何是露地白牛 stock-question use is likewise the same referent as
a test-phrase (parallel to 祖師西來意), noted and given one Deshan occurrence.

## Multi-source verdict: **multi-source.**

## Deflationary check
"the white ox in the open ground" — literal (露地 = open/bare ground, 白牛 = white ox). Explanation grounds
it in the herding line and the Lotus parable; explicitly "a plain white ox in a bare field, not a mystical
absolute." No imported abstraction.

## Honest thin spots
- 341 hits not fully censused; senses asserted from representative sample + counts.
- The maturation line's attribution is genuinely floating (Da'an ↔ Nanquan) — inherited from the buffalo
  pilot, left flagged (AttributionNote), not resolved. X86n1607's maturation witness is speaker-unattributed
  here → MasterName null, honestly.
- Dongshan-credited line (洞山道) is from a 頌古/commentary reporting him, not his own 語錄 — noted.

## GATE 2 (Claude adversarial verify-and-repair) — 2026-07-11
- **KWICs:** all 6 re-derived by targeted per-file search + tag-strip contiguity check → EXACT
  CONTIGUOUS VERBATIM (no ellipsis, no stitching, no altered punctuation). Char-lengths 24–53.
- **Allowlist:** all 6 RelPaths (T51n2076, X86n1607, X67n1307, X67n1299, J32nB276, X69n1356) ∈ zen-corpus.json. No contamination.
- **FromLb:** all 6 confirmed = nearest preceding `<lb n>` (0267c07, 0890b06, 0866a04, 0240b02, 0347b26, 0588a13).
- **Multi-source:** 6 independent allowlist texts (Lotus base + Guiyang herding + koan-question + 分歲 case) → `multi-source` stands.
- **Buffalo reconciliation:** bidirectional cross-ref holds — 露地白牛.RelatedTerms⊇水牯牛 ↔ BUFFALO.RelatedTerms⊇露地白牛;
  the T51n2076 0267c07 Da'an line + its floating Da'an↔Nanquan attribution note match the buffalo entry.
  NOTE (not my fix): BUFFALO_ENTRY.v2.json still cites B/B19/B19n0103.xml (禪林象器箋 lexicon, NON-allowlist) —
  contamination for the parallel buffalo repair to remove; my 露地白牛 entry does not cite it.
- **Over-read/abstraction:** literal/deflationary ("plain white ox in a bare field, not a mystical absolute"). OK.
- **Nesting (§5b):** RelatedTerms (水牯牛, 異類中行, 牧牛) are genuine cross-refs/constituents — no coincidental prefixes. OK.
- **Verdict: VERIFIED.** No corrections required; entry.v2.json unchanged.

## GATE 3 FIX (Claude, Ms. Frizzle) — 2026-07-11 17:42 +02:00
- Gate 3 (Fable) verdict: REVISE — one ATTRIBUTION_ERROR on occ1 (X86n1607 0890b06).
- **Verified in source** `X/X86/X86n1607.xml` lb 0890b05 (line 2020): 「傑…曰：『大凡學道之人，
  十二時中，嘗須照顧，不見南泉道 三十年看一頭水牯牛…如今變成露地白牛，躶躶地放他不肯去』」 —
  the layman Yang Jie (傑/提刑) is EXPLICITLY quoting 南泉 (不見南泉道), not an unattributed speaker.
- **Fix applied:** occ1 MasterName null → "Nanquan Puyuan"; AttributionNote rewritten from
  "Speaker unattributed in this witness" → Yang-Jie-quoting-Nanquan, flagged as the Nanquan side of the
  floating Da'an/Nanquan attribution and a parallel of T51n2076 0464c12 (strengthens the floating story).
- **KWIC unchanged** (verbatim-contiguous, in-source `<lb>` split between 如今|變成 at 0890b06/b07). No occurrences added/removed. Validation stays `multi-source`.
- **STATUS = verified.**
