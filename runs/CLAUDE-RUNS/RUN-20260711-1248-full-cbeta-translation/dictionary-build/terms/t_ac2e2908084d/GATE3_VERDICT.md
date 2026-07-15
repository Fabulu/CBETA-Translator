# GATE 3 RE-AUDIT VERDICT — t_ac2e2908084d · 見性成佛

VERDICT: PASS

**Re-auditor:** Gate 3 re-audit (independent, adversarial, grep-backed), 2026-07-11.
**Scope:** verify prior punch list (3 items) resolved; spot-check KWIC/allowlist/attribution for regressions.

## Prior punch list — resolution verified

1. **[Occ 3 · T51n2076 @0429a22 · AttributionNote] RESOLVED.** The false "Speaker not
   identified at the section head" claim is gone. The note now states: governing
   section head 廬山歸宗第十四世慧誠禪師 (Guizong Huicheng); not a rostered master,
   so MasterName null. Independently re-verified: last cb:mulu before the KWIC in
   T51n2076 = 廬山歸宗第十四世慧誠禪師 ✓; grep of master-dates.json for 慧誠/Huicheng
   = no entry ✓; MasterName is null in the entry ✓. Exactly the prescribed fix.

2. **[Note · count understatement] RESOLVED.** Note now reads "approximately 684 raw
   occurrences across 213 allowlist texts, independent strict count; an earlier tally
   of 602/206 did not reproduce." My own fresh strict recount (tag/note/rdg-stripped,
   all 462 allowlist files): **684 occurrences across 213 files** — reproduces the
   stated numbers exactly.

3. **[RelatedTerms dangling refs] ACCEPTABLE per re-audit instructions.** 直指人心,
   教外別傳, 不立文字 remain as forward references; explicitly allowed ("dangling
   RelatedTerms acceptable"). Semantic links genuine (the four-clause slogan).

## Regression spot-checks — clean

- **KWIC re-grep (3/4):** T48n2008 普願法界眾生，言下見性成佛。 exact, count=1,
  lb 0351c15 ✓; T51n2076 祖師西來只道見性成佛。其餘所說不及此說。 exact, count=1,
  lb 0429a22 ✓; X80n1565 乃名見性。性即佛。佛即性。故曰見性成佛。 exact, count=1,
  nearest ed="X" lb = 0051a21 (R co-location 0047a18 correctly not used), governing
  cb:mulu 天台山雲居智禪師 ✓ (matches occ-1 note; 雲居智 non-rostered, null correct).
- **Attribution:** Huineng (occ 2) unchanged; occ 1/3/4 nulls unchanged and justified.
- **Allowlist:** occurrence RelPaths and SourceTexts unchanged from the
  prior-verified set; no path changes.

No unresolved items. No new defects introduced.
