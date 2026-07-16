# Source-batched attribution report: X67n1304

Scope: the 2 candidate occurrences in `quick-X67n1304.md`, covering 2 complete cases and 2 entry IDs. This was attribution-only remediation.

## Exact-turn adjudication

Both draft header defaults were contradicted by complete-case review and were overridden with Linquan Conglun.

- `劫外` occurs in Linquan Conglun's continuous case-five commentary introduced by `師云`. Linquan asks whether one who undertakes it beyond the eon still needs to meet now. Dongshan Liangjie is the case-heading subject, not the speaker.
- `活鱍鱍` occurs in Linquan Conglun's continuous case-seventeen commentary introduced by `師云`. After discussing the old exchanges, Linquan says that the matter turns freely and is alive and darting, like a pearl running on a tray without a stagnant trace.
- The collection title's `丹霞淳` identifies verse author Danxia Zichun, not Danxia Tianran; neither Danxia nor Dongshan is the exact speaker of these comments.

## Changed IDs

- `t_17c1d8b4f105` 劫外
- `t_1d1a833551a9` 活鱍鱍

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Notes naming `林泉老人評唱丹霞淳禪師頌古虗堂集` and Linquan Conglun | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `X/X67/X67n1304.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 8 | 8 | 0 |
| Named occurrences | 3 | 5 | +2 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 5 | 3 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 11 | 11 | 0 |
| Named occurrences | 2 | 4 | +2 |
| Context-master links | 0 | 4 | +4 |
| Unresolved actors | 9 | 7 | -2 |
| Notes missing exact speaker/state | 10 | 8 | -2 |
| Notes missing source title | 10 | 8 | -2 |
| Hard failures | 30 | 24 | -6 |

The 24 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: seven unresolved actors, eight notes missing a speaker/state, eight notes missing a source, and one dangling-Chinese finding.

## Mechanical checks

- Signed compile: 2 rows, 2 overrides; `real 0.11s`.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.20s`.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.22s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` was found once; both intended masters, notes, and context masters matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
