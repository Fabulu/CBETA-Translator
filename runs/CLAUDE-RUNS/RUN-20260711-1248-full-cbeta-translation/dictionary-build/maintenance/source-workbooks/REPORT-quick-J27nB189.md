# Source-batched attribution report: J27nB189

Scope: the 2 candidate occurrences in `quick-J27nB189.md`, covering 2 complete cases and 2 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

Both draft defaults were contradicted by complete-case review and were overridden.

- `換卻眼睛` belongs to Sanyi Mingyu, who addresses the assembly and warns them not to let the writing brush exchange black beans for their eyes.
- `擔荷` belongs to Sanyi Mingyu, the old mountain monk speaking in the enclosing address, who says even the ancestral teachers could not shoulder it.
- In both headers, Yunmen names Yunmen Xiansheng Monastery; it does not identify Yunmen Wenyan as the speaker.

## Changed IDs

- `t_c81bf91e508f` 換卻眼睛
- `t_efa1e241a7f0` 擔荷

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Notes naming `三宜盂禪師語錄` and Sanyi Mingyu | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `J/J27/J27nB189.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 14 | 14 | 0 |
| Named occurrences | 10 | 12 | +2 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 4 | 2 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 13 | 13 | 0 |
| Named occurrences | 6 | 8 | +2 |
| Deferred non-roster names | 1 | 3 | +2 |
| Unresolved actors | 7 | 5 | -2 |
| Notes missing exact speaker/state | 7 | 5 | -2 |
| Notes missing source title | 10 | 8 | -2 |
| Hard failures | 27 | 21 | -6 |

The 21 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: five unresolved actors, five notes missing a speaker/state, eight notes missing a source, two vague-attributor findings, and one dangling-Chinese finding.

## Mechanical checks

- Signed compile: 2 rows, 2 overrides; `real 0.11s` on the final run.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.19s` on the final run.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.23s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` was found once; both intended masters and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
