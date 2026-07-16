# Quick Owner Bundle Q3 consolidated report

All 67 filtered occurrences were reviewed in complete-case context and applied within worker 3's exclusive 51-entry ownership set.

| Source | Rows | Entries | Named | Reviewed unnamed | Impersonal | Overrides | Focused / verify |
|---|---:|---:|---:|---:|---:|---:|---:|
| X80n1565 | 30 | 24 | 21 | 8 | 1 | 15 | 30/30 |
| X82n1571 | 24 | 23 | 22 | 2 | 0 | 4 | 24/24 |
| T51n2076 | 13 | 10 | 11 | 2 | 0 | 4 | 13/13 |
| Total | 67 | 51 unique | 54 | 12 | 1 | 23 | 67/67 |

All three signed compiles, strict dry-runs, and applies succeeded. The 67 exact stored occurrences match their decision fields and all 67 replay against CBETA with matching `FromLb` and `ToLb`. Twelve generated JSON artifacts parse. The entry-ID set equals `maintenance/quick-ownership/worker-3-entries.txt` exactly.

The full audit of the 51 touched entries covers 295 occurrences: 157 named, 14 reviewed unnamed, one impersonal, and 123 unresolved occurrences outside this focused ownership batch. Its 696 residual findings belong to out-of-scope occurrence/prose remediation and do not invalidate the 67 focused gates.

Crash-resume review checkpoints at 10, 20, 30, 40, 50, 60, and 67 rows, plus per-source applied checkpoints, are recorded in `maintenance/bundle-ledgers/quick-owner-3.json` and `.md`. No unit remains failed or deferred; `nextUnit` is null.

No merge, commit, or push was performed.
