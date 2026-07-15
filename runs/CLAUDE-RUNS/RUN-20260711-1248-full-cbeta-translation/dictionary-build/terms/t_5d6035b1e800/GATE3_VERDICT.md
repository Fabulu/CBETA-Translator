# Gate 3 Verdict — 露地白牛 (t_5d6035b1e800) — RE-VERIFY after repair

VERDICT: PASS

Independent adversarial re-derivation (Gate 3, fresh pass, 2026-07-11). This entry was
previously REVISE'd for one ATTRIBUTION_ERROR (occ1's AttributionNote claimed "Speaker
unattributed" where the source explicitly quotes Nanquan). The repair is verified correct
against the primary Chinese; all other checks re-run and hold. Merge as-is.

## Prior-fix verification (the reason for this re-verify)

**occ1 (`X/X86/X86n1607.xml` FromLb 0890b06) — FIXED CORRECTLY.**
Re-derived from source (tag-strip + offset-mapped lb scan):
- Immediately before the KWIC, at lb **0890b05** (verified by nearest-preceding-`<lb>`
  computation), the file reads: 「傑嘗謂僧曰：『大凡學道之人，十二時中，嘗須照顧，**不見南泉道**
  三十年看一頭水牯牛…』」 — the KWIC (starting 三十年… at lb 0890b06) is explicitly introduced
  as "Nanquan said."
- The speaker 傑 is Yang Jie (楊傑): 楊傑 appears 4x in the file (incl. the section roster
  「…張方平 楊傑 劉經臣…」), and the surrounding dialogue addresses him as 提刑
  (「未審提刑作麼生？」傑曰：「硬。」).
- The AttributionNote's parallel claim also verifies: T51n2076 at lb **0464c12** reads
  「因僧談道。侍郎遂云。…大凡參學之人。十二時中長須照顧。不見南泉道。三十年看一頭水牯牛。
  若犯他人苗稼。摘鼻拽迴。如今變成露地白牛。裸裸地放他不肯去。」 — the same discourse; occ1 is
  correctly described as "a parallel of the Nanquan-version passage at T51n2076 0464c12,
  not an independent third rendition" (no independence over-claim).
- MasterName now "Nanquan Puyuan" with the quoting frame transparent in the note, flagged
  as the Nanquan side of the floating Da'an/Nanquan attribution. Exactly the demanded fix.

## Per-sense findings

### Sense 0 — "the white ox in the open ground" (multi-source)

1. **KWIC exact + contiguous: PASS (6/6).** Every KWIC re-verified as an EXACT CONTIGUOUS
   substring of its cited file after XML-tag/whitespace stripping (script check, found x1
   each; no ellipsis, no stitching, no altered punctuation):
   - `T/T51/T51n2076.xml` — 只看一頭水牯牛。…如今變作箇露地白牛常在面前。 (52 chars; keeps the
     full 白牛常在面前 continuation that failed the buffalo pilot). Nearest lb = 0267c07 ✓
   - `X/X86/X86n1607.xml` — 三十年看一頭水牯牛，…躶躶地放他不肯去 (38 chars). Nearest lb = 0890b06 ✓
   - `X/X67/X67n1307.xml` — 洞山道：露地白牛，牧人懶放。… (32 chars). Nearest lb = 0866a04 ✓
   - `X/X67/X67n1299.xml` — 德山鑑禪師。僧問：如何是露地白牛？師云：吽！吽！ (24 chars). Nearest lb = 0240b02 ✓
   - `J/J32/J32nB276.xml` — 北禪賢大師除夕示眾：『年窮歲盡，…大家喫了 (53 chars). Nearest lb = 0347b26 ✓
   - `X/X69/X69n1356.xml` — 華嚴論主深歎法華經中露地白牛，… (29 chars; curated:false pedigree witness). Nearest lb = 0588a13 ✓

2. **RelPath real + Zen: PASS.** All 6 files exist under `xml-p5` and all 6 RelPaths are
   present in `Assets/Data/zen-corpus.json` (grep-verified). No contamination; the
   B19n0103 lexicon that burned the buffalo entry is not cited here.

3. **Multi-source: PASS.** Independent witnesses beyond the floating pair: the Da'an
   sermon (T51n2076 0267c07 — speaker re-confirmed as Da'an: 「就**安**求覓什麼」…「**安**在溈山
   三十來年。喫溈山飯屙溈山屎」), the Dongshan-credited comment in the 南泉水牯 case commentary
   (X67n1307 第六十則, 師云：這箇公案…洞山道…), the Deshan test-question (X67n1299), Beichan's
   year-end case quoted with 遂舉 (J32nB276), and the Lotus-pedigree line (X69n1356). The
   occ0/occ1 non-independence (floating Da'an↔Nanquan variants of one line) is now honestly
   flagged inside occ1's note, so the multi-source claim rests on genuinely independent legs.

4. **Over-read: none.** The maturation reading is the sources' own wording (如今變作箇/變成
   露地白牛); no single-owner claim is made ("No single owner — Lotus-Sutra-derived…
   floating"); occ2 stays hedged ("Credited to Dongshan (洞山道) in this verse-commentary").
   Raw counts (341/153) not re-derived; non-load-bearing.

5. **Imported abstraction: none.** "a plain white ox standing in a bare field, not a
   mystical absolute" — deflationary register maintained; the Lotus pedigree is cited from
   within the corpus (X69n1356 …**法華經中**露地白牛), not imported.

6. **Attribution honesty: PASS.** The floating Da'an/Nanquan attribution is flagged in both
   occ0 and occ1 notes and matches the sources exactly (0267c07 Da'an vs 0464c12/0890b05
   "Nanquan said"). No laundering.

## Issues (tagged)

- None blocking. (Carried observation, no action forced: occ2's 「洞山道」 → MasterName
  "Dongshan Liangjie" relies on the bare-洞山-conventionally-=-Liangjie reading; the
  AttributionNote hedges this properly.)

## Verified occurrences: 6/6 KWIC confirmed verbatim
