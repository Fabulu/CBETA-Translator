# Source-batched attribution report: X70n1382

Scope: the 2 candidate occurrences in `quick-X70n1382.md`, covering 2 complete cases and 2 entry IDs. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook matched the regenerated `maintenance/attribution-triage-all.json` exactly for both selected rows: source, entry ID, term, case-cluster ID, line anchors, and KWIC. No stale or additional row was found.

## Exact-turn adjudication

Both draft defaults survived complete-case review; no exception override was required.

- `沒巴鼻` belongs to Wuzhun Shifan. In his opening ritual at Qingliang Monastery, Wuzhun picks up the prefectural notice and contrasts the patriarchs' handle, patch-robed monks' handle, having some handle, and having none at all.
- `開口即錯` belongs to Wuzhun Shifan. In his own hall address, Wuzhun says opening the mouth is already error and closing it is already loss, then asks how communication is possible.
- The enclosing `佛鑑禪師` section is part of Wuzhun Shifan's record; `佛鑑` is his bestowed title, not a second speaker.

## Changed IDs

- `t_6af80faddcf0` 沒巴鼻
- `t_2745ffff5972` 開口即錯

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Notes naming `無準師範禪師語錄` and Wuzhun Shifan | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `X/X70/X70n1382.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 3 | 3 | 0 |
| Named occurrences | 1 | 3 | +2 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 2 | 0 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 9 | 9 | 0 |
| Named occurrences | 2 | 4 | +2 |
| Unresolved actors | 7 | 5 | -2 |
| Notes missing exact speaker/state | 7 | 5 | -2 |
| Notes missing source title | 7 | 5 | -2 |
| Hard failures | 27 | 21 | -6 |

The 21 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: five unresolved actors, five notes missing a speaker/state, five notes missing a source, five vague-attributor findings, and one dangling-Chinese finding.

## Mechanical checks

- Signed compile: 2 rows, 0 overrides; `real 0.14s`.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.25s`.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.31s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` was found once; both intended masters and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
