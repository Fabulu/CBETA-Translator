# GATE 3 RE-AUDIT VERDICT — t_1e41b014d80e · 向上一路

VERDICT: PASS

**Re-auditor:** Gate 3 re-audit (independent, adversarial, grep-backed), 2026-07-11.
**Scope:** verify prior punch list (1 item) resolved; spot-check KWIC/allowlist/attribution for regressions.

## Prior punch list — resolution verified

1. **[Occ 3 · X82n1571 @0096a18 · AttributionNote] RESOLVED.** The false "(unnamed
   here) section master" claim is gone. The rewritten note now correctly states:
   governing section head = 臨安府徑山宗杲大慧普覺禪師 (Dahui Zonggao), same sermon
   in 大慧語錄 T47n1998A, and MasterName stays null (headword occurs only inside the
   quoted Panshan/Ciming lines). Independently re-verified from raw XML:
   - Last cb:mulu AND last head before the KWIC in X82n1571 = 臨安府徑山宗杲大慧普覺禪師 ✓.
   - Parallel in T47n1998A re-grepped: 向上一路千聖不然 (2 hits, first at 0818b18) and
     不傳不然海口難宣 (1 hit, 0834c21) — the note's parallel-sermon claim holds ✓.
   - MasterName is null in the entry, as required ✓.
   (Prior verdict's "consider adding Dahui Zonggao to RelatedMasters" was a suggestion,
   not a defect; not adopted — acceptable.)

## Regression spot-checks — clean

- **KWIC re-grep (3/4):** X82n1571 occ-3 KWIC exact, count=1, nearest ed="X" lb =
  0096a18 (R co-location 0972b14 correctly not used) ✓; X80n1565 occ-1 KWIC exact,
  count=1, ed="X" lb 0077b08, governing head 幽州盤山寶積禪師 ✓; T51n2076 occ-4 KWIC
  問如何是向上一路。師舉衣領示之。 exact, count=1, lb 0354b10, governing cb:mulu
  越州洞巖可休禪師 ✓.
- **Allowlist:** occurrence RelPaths unchanged (X80n1565, T51n2076, X82n1571 — all
  previously allowlist-verified; no path changes).

## Observation (non-blocking, pre-existing — passed by both prior gates)

- Occ 4 stores MasterName "Dongyan Kexiu" while its own note correctly states he is
  not in the canonical roster (grep of master-dates.json confirms: no Dongyan/洞巖/可休
  entry). Elsewhere in this batch (見性成佛 occ 1/occ 3) non-rostered section masters
  are stored as null with the name only in the note. This occurrence was explicitly
  verified clean by the prior Gate 3 pass and was not a punch-list item, so it is not
  treated as a regression here — but a consistency pass may want to null it and keep
  the identification in the AttributionNote.

No unresolved items. No new defects introduced.
