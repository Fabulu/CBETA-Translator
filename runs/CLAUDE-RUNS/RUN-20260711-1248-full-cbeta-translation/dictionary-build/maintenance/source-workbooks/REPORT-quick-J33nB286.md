# Source-batched attribution report: J33nB286

Scope: the 3 candidate occurrences in `quick-J33nB286.md`, covering 3 complete cases and 3 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

- `回互` retains Shitou Xiqian. Yingning Jing explicitly introduces Shitou's quoted lines with `石頭大師云`; the headword occurs inside that quotation.
- `凡情聖見` is assigned to Yingning Jing, author of the continued reply to Sanfeng Cang and Yunmen Zhan. The section addressee Yunmen Zhan is not Yunmen Wenyan and is not the prose speaker.
- `直心` is assigned to Vimalakirti. Yingning introduces the formula with `所云`; an independent full parallel identifies Light Adornment's question and Vimalakirti's exact answer, `直心是道場，無虛假故`.

`Yingning Jing` is preserved as the source-attested exact name; no generic master label substitutes for him.

## Changed IDs

- `t_1e3d3a5173a6` 回互
- `t_19705602b956` 凡情聖見
- `t_dcd5468f5104` 直心

## Counts

Workbook-scoped 3 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 3 |
| Bare unresolved actors | 3 | 0 |
| Notes naming `攖寧靜禪師語錄` and the exact actor | 0 | 3 |
| Exact `zc.verify` successes | not rerun | 3/3 |

Whole-source inventory for `J/J33/J33nB286.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 7 | 7 | 0 |
| Named occurrences | 0 | 3 | +3 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 7 | 4 | -3 |

Full `audit_attribution.py --json` over the 3 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 15 | 15 | 0 |
| Named occurrences | 5 | 8 | +3 |
| Impersonal occurrences | 1 | 1 | 0 |
| Unresolved actors | 9 | 6 | -3 |
| Notes missing exact speaker/state | 11 | 8 | -3 |
| Notes missing source title | 4 | 3 | -1 |
| Hard failures | 30 | 23 | -7 |

The 23 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: six unresolved actors, eight notes missing a speaker/state, three notes missing a source, one vague-attributor finding, and five dangling-Chinese findings.

## Mechanical checks

- Signed compile: 3 rows, 2 overrides; `real 0.11s`.
- Strict dry-run: 3 prepared rows, 3 entries, zero failures; `real 0.19s`.
- Strict apply: 3 prepared rows, 3 entries, zero failures; `real 0.25s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` found once; all intended masters and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 3/3.
- All 3 modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
