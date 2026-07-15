# GATE 3 RE-AUDIT VERDICT — t_2738431562e6 · 無字

VERDICT: PASS

**Re-auditor:** Gate 3 re-audit (independent, adversarial, grep-backed), 2026-07-11.
**Scope:** verify prior punch list (1 item) resolved; spot-check KWIC/allowlist/attribution for regressions.

## Prior punch list — resolution verified

1. **[Explanation · 看話禪 unattested-as-corpus-language] RESOLVED.** The Explanation
   now reads: "Dahui made it the engine of his 看話 (huatou-watching) method — what
   later tradition calls 看話禪". This is exactly the sanctioned fix (attested term
   看話 as the corpus language, 看話禪 explicitly flagged as a later label, no longer
   stated as corpus vocabulary). Independently re-verified across the full 462-text
   allowlist (tag/note-stripped): 看話 = 140 hits in 58 files (incl. Dahui's own
   T47n1998A) ✓; 看話禪 = 0 hits ✓ — so the "later label" framing is the honest one,
   and no other place in the entry presents 看話禪 as a corpus term.

## Regression spot-checks — clean

- **KWIC re-grep (3/5):** T48n2005 如何是祖師關。只者一箇無字。乃宗門一關也。 exact,
  count=1, lb 0292c27, governing cb:mulu 趙州狗子 (Wumen's case-1 commentary) ✓;
  T47n1998A 州云無。只這一字。…但舉箇無字。 exact, count=1, lb 0903c03, section
  示妙心居士 (Dahui's own instruction) ✓; X70n1401 前所看無字，將及三載，… exact,
  count=1, nearest ed="X" lb = 0703b08 (R co-location 0704b02 correctly not used),
  section 開堂普說 (Gaofeng) ✓.
- **Attribution:** Wumen Huikai / Dahui Zonggao / Gaofeng Yuanmiao unchanged and
  re-confirmed at their section contexts above.
- **Allowlist:** occurrence RelPaths unchanged (T48n2005, T47n1998A, X70n1401 — all
  previously allowlist-verified; no path changes).

No unresolved items. No new defects introduced.
