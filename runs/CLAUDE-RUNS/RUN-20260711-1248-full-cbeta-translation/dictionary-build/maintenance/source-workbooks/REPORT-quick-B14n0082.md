# Source-batched attribution report: B14n0082

Scope: the 2 candidate occurrences in `quick-B14n0082.md`, covering 2 complete cases and 2 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

Both draft defaults survived complete-case review; no exception override was required.

- `如何是佛法大意` belongs to Bai Juyi, the named exact questioner. Niaoke Daolin answers `諸惡莫作，眾善奉行`, and Bai challenges the answer afterward.
- `大用` belongs to Guishan Lingyou, explicitly marked by `溈山上堂云`. Jiufeng Cihui is the student who leaves without looking back.

## Changed IDs

- `t_bc7bbb4299f1` 如何是佛法大意
- `t_966bc615eb6e` 大用

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Notes naming `傳燈玉英集（殘卷）` and the exact actor | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `B/B14/B14n0082.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 24 | 24 | 0 |
| Named occurrences | 15 | 17 | +2 |
| Structured actor exceptions | 3 | 3 | 0 |
| Bare unresolved occurrences | 6 | 4 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 12 | 12 | 0 |
| Named occurrences | 0 | 2 | +2 |
| Reviewed-unnamed occurrences | 4 | 4 | 0 |
| Unresolved actors | 8 | 6 | -2 |
| Notes missing exact speaker/state | 8 | 6 | -2 |
| Notes missing source title | 8 | 6 | -2 |
| Hard failures | 29 | 23 | -6 |

The 23 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: six unresolved actors, six notes missing a speaker/state, six notes missing a source, three vague-attributor findings, and two dangling-Chinese findings.

## Mechanical checks

- Signed compile: 2 rows, 0 overrides; `real 0.14s`.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.27s`.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.28s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` found once; both intended masters and notes matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
