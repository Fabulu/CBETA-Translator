# GATE 3 RE-AUDIT VERDICT — t_b8063e3d60b4 · 直指人心

VERDICT: PASS

**Auditor:** Gate 3 re-audit (independent adversarial), 2026-07-11. Verified against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with tag-stripped (note/rdg excluded,
whitespace collapsed) exact-substring matching, lb re-anchoring, and allowlist-wide counts.
Scope: (1) prior punch list resolved, (2) no KWIC/allowlist/attribution regression,
(3) strip+enrich prose is describe-only and its added Chinese is corpus-attested.

## 1. Prior punch list — resolved
1. **[REQUIRED] 碧巖錄 genre mislabel — FIXED.** Note now reads "a 燈錄 (續傳燈錄), and the
   碧巖錄 (T48n2003, a 頌古/評唱 koan-commentary collection, not a lamp record)". Correct.
2. **[Optional] Occ 3 AttributionNote naming Ciming's section — not adopted.** Was optional;
   null MasterName remains defensible (所以道-introduced stock slogan). Non-blocking.
3. **[Non-blocking] Counts — refreshed with method stated.** Note now claims 624/216 and
   declares the convention (tags/notes/rdg stripped, whitespace collapsed, punctuation
   retained; 見性成佛 counted across ≤1 punctuation mark).

## 2. Regression re-grep — clean
- KWIC 1 T47n1997 @0769b07 `達磨西來不立文字。直指人心見性成佛。` — 1 hit, lb exact ✓
- KWIC 4 T51n2077 @0564c12 `達磨西來直指人心見性成佛。亦復如是。` — 1 hit, lb exact ✓
- KWIC 5 T48n2003 @0154c04 `謂之教外別傳。單傳心印。直指人心。見性成佛。` — 1 hit, lb exact ✓
- Occ 2 quote `唯直指人心。若論直指。只人人本有` — 1 hit @0779a16 ✓
- No MasterName changed since prior audit; attributions carry over as previously verified.
- Allowlist: all 4 occurrence RelPaths plus newly cited X82n1571 and J34nB311 are in
  zen-corpus.json ✓.

## 3. Strip+enrich pass — describe-only, attested
Prose check: the former interpretive framing is gone; the entry now closes with "the texts
leave the phrase without gloss, and so does this entry." No intent/force/"the point
is"/deflationary language found.

Added quotes, grep-verified verbatim and contiguous:
- X82n1571 @0263a12: `如何是直指人心？師曰：舌在口裏。曰：如何是見性成佛？師曰：金屑雖貴，落眼成翳` ✓
  (the two Q&A pairs adjacent exactly as the entry presents them)
- J34nB311 @0604a12–13: `如何是直指人心、見性成佛之旨？」師云：「腳跟下分明看取` ✓

Added counts, independently recounted over the 462-file allowlist:
| phrase | claimed | measured |
|---|---|---|
| 直指人心 | 624/216 | 622/216 |
| paired 見性成佛 (≤1 punct) | 470/190 | 469/190 |
| preceded by 不立文字 (≤1 punct) | 118/79 | 118/79 |
| 單傳直指 | 119/73 | 119/73 |
| 直指單傳 | 95/71 | 95/71 |
| 西來直指 | 42/34 | 42/34 |
| 直指之道 | 67/34 | 67/34 |
| 若論直指 | 13/13 | 13/13 |
| 如何是直指人心 | 4/4 | 4/4 |

The 624-vs-622 and 470-vs-469 deltas (<0.5%, file counts identical) are stripping-convention
sensitivity, not fabrication; all other figures reproduce exactly. Non-blocking.

## Residue (non-blocking)
- Occ 3 AttributionNote still does not name the governing 慈明禪師語錄 section (optional item
  from the prior audit). Cosmetic.
