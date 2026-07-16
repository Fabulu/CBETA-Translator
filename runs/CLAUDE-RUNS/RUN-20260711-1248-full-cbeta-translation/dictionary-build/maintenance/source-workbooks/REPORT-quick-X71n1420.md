# Source-batched attribution report: X71n1420

Scope: the 4 candidate occurrences in `quick-X71n1420.md`, covering 4 complete cases and 4 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

- `擬議` retains Chushi Fanqi, who directly presses the assembly to answer the bamboo-slat test and threatens a blow if deliberation does not produce an answer.
- `一莖草` is assigned to Baiyun Shouduan. Chushi explicitly introduces the quoted instruction with `白雲端和尚示眾云`; Chushi's own comment begins only at `師云`.
- `日日是好日` belongs to an unnamed monk who advances the good-year/good-day line. Chushi answers `瞎老婆吹火`. The line, expanded context, section, title, TEI metadata, exact-case search, and independent parallel witnesses do not name the monk, so the row uses the six-rung reviewed-unnamed state.
- `千手眼` retains Chushi Fanqi as the signed author of the image record, which opens by identifying the thousand-hand-and-eye form as an adaptive body of Avalokiteshvara.

## Changed IDs

- `t_ef39bdc0eb99` 擬議
- `t_897ae83b4a57` 一莖草
- `t_a326343ab7c3` 日日是好日
- `t_bf67613e4573` 千手眼

## Counts

Workbook-scoped 4 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 3 |
| Reviewed-unnamed actors | 0 | 1 |
| Bare unresolved actors | 4 | 0 |
| Notes naming `楚石梵琦禪師語錄` and the exact actor/state | 0 | 4 |
| Exact `zc.verify` successes | not rerun | 4/4 |

Whole-source inventory for `X/X71/X71n1420.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 10 | 10 | 0 |
| Named occurrences | 6 | 9 | +3 |
| Structured actor exceptions | 0 | 1 | +1 |
| Bare unresolved occurrences | 4 | 0 | -4 |

Full `audit_attribution.py --json` over the 4 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 22 | 22 | 0 |
| Named occurrences | 3 | 6 | +3 |
| Reviewed-unnamed occurrences | 0 | 1 | +1 |
| Unresolved actors | 19 | 15 | -4 |
| Notes missing exact speaker/state | 20 | 16 | -4 |
| Notes missing source title | 15 | 11 | -4 |
| Hard failures | 58 | 46 | -12 |

The 46 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: 15 unresolved actors, 16 notes missing a speaker/state, 11 notes missing a source, one vague-attributor finding, and three dangling-Chinese findings.

## Mechanical checks

- Signed compile: 4 rows, 2 overrides; `real 0.14s`.
- Strict dry-run: 4 prepared rows, 4 entries, zero failures; `real 0.23s`.
- Strict apply: 4 prepared rows, 4 entries, zero failures; `real 0.33s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` found once; intended named/reviewed-unnamed state and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 4/4.
- All 4 modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
