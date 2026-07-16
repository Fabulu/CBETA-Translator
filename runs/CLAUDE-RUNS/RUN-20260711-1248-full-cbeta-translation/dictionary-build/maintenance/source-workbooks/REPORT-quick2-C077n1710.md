# Source-batched attribution report: C077n1710 promoted remainder

Scope: the 4 promoted candidate occurrences in `quick2-C077n1710.md`, covering 4 complete cases and 4 entry IDs. Comparison by exact `(entry ID, FromLb, KWIC)` found zero overlap with the prior 30-row `decisions-C077n1710.json` pass. This was attribution-only remediation.

## Exact-turn adjudication

- `一物` is Nanyue Huairang's direct report to Huineng: describing it as one thing would miss it. Huineng asks the following question and is not the speaker of the headword turn.
- `客塵` retains Baizhang Huaihai as speaker of the ruler/guest-dust classification in his extended discourse.
- `向上事` is spoken in both questions by an unnamed monk. Shoushan Xingnian answers `有` and then `新羅人不褁頭`; he is contextual respondent, not the headword speaker. All six attribution rungs were exhausted, and the unique exact full case plus broader parallels do not name the monk.
- `直指人心` retains Bodhidharma as the actor of the attributed direct-pointing slogan in Ciming Chuyuan's later opening address.

## Changed IDs

- `t_94ee610a30f7` 一物
- `t_57fd70bfc9ec` 客塵
- `t_e84753568cda` 向上事
- `t_b8063e3d60b4` 直指人心

## Counts

Workbook-scoped 4 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 3 |
| Reviewed-unnamed actors | 0 | 1 |
| Bare unresolved actors | 4 | 0 |
| Notes naming `古尊宿語錄` and the exact actor/state | 0 | 4 |
| Exact `zc.verify` successes | not rerun | 4/4 |

Whole-source inventory for `C/C077/C077n1710.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 191 | 191 | 0 |
| Named occurrences | 114 | 117 | +3 |
| Structured actor exceptions | 11 | 12 | +1 |
| Bare unresolved occurrences | 66 | 62 | -4 |

Full `audit_attribution.py --json` over the 4 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 20 | 20 | 0 |
| Named occurrences | 7 | 10 | +3 |
| Reviewed-unnamed occurrences | 2 | 3 | +1 |
| Unresolved actors | 11 | 7 | -4 |
| Notes missing exact speaker/state | 15 | 11 | -4 |
| Notes missing source title | 13 | 10 | -3 |
| Hard failures | 75 | 64 | -11 |

The 64 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: seven unresolved actors, 11 notes missing a speaker/state, 10 notes missing a source, five vague-attributor findings, and 31 dangling-Chinese findings.

## Mechanical checks

- Prior-pass overlap gate: 0/4 promoted rows overlap the prior 30 decisions.
- Signed compile: 4 rows, 2 overrides; `real 0.06s`.
- Strict dry-run: 4 prepared rows, 4 entries, zero failures; `real 0.10s`.
- Strict apply: 4 prepared rows, 4 entries, zero failures; `real 0.14s`.
- Focused gate: all exact `(entry ID, FromLb, KWIC)` tuples found once; intended named/reviewed-unnamed states and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 4/4.
- All 4 modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
