# Source-batched attribution report: C078n1720

Scope: the 5 candidate occurrences in `quick-C078n1720.md`, covering 5 complete cases and 5 entry IDs. This was attribution-only remediation; no senses, glosses, KWICs, inventories, or source ranges were changed.

## Exact-turn adjudication

All five complete cases and exact headword-bearing turns were reviewed. Three draft defaults were retained and two contradictions overridden.

- `安心` is assigned to Bodhidharma, the exact respondent who concludes that he has finished setting Huike's mind at ease. Huike requests the action and awakens afterward.
- `百尺竿頭` retains Fojian Huiqin. The excluded inline source note `佛鑑懃` names him as author of the complete comment-verse; `老長沙` names Changsha Jingcen inside that verse.
- `法眼` is assigned to Jiashan Shanhui, who answers the unnamed monk with `法眼無瑕`. The graphs are the doctrinal noun, not the title-name Fayan Wenyi.
- `目前` retains Jiashan Shanhui under the explicit formula `夾山示眾云`.
- `顧鑒咦` retains Yunmen Wenyan as actor of the named sequence. Deshan Yuanming is the later editor who removes `顧`, not the performer of the original sequence.

## Changed IDs

- `t_79e00cdbc129` 安心
- `t_53da4e346a6f` 百尺竿頭
- `t_ca8f7f2d5d03` 法眼
- `t_937f63a4fb51` 目前
- `t_461377f9b1da` 顧鑒咦

## Counts

Workbook-scoped 5 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 5 |
| Bare unresolved actors | 5 | 0 |
| Notes naming `禪宗頌古聯珠通集` and the exact actor | 0 | 5 |
| Exact `zc.verify` successes | not rerun | 5/5 |

Whole-source inventory for `C/C078/C078n1720.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 57 | 57 | 0 |
| Named occurrences | 30 | 35 | +5 |
| Structured actor exceptions | 1 | 1 | 0 |
| Bare unresolved occurrences | 26 | 21 | -5 |

Full `audit_attribution.py --json` over the 5 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 31 | 31 | 0 |
| Named occurrences | 7 | 12 | +5 |
| Unresolved actors | 24 | 19 | -5 |
| Notes missing exact speaker/actor | 25 | 20 | -5 |
| Notes missing source title | 24 | 20 | -4 |
| Hard failures | 115 | 101 | -14 |

The 101 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: 19 unresolved actors, 20 notes missing a speaker, 20 notes missing a source, 11 vague-attributor findings, and 31 dangling-Chinese findings.

## Mechanical checks

- Signed compile: 5 rows, 2 overrides; `real 0.10s`.
- Strict dry-run: 5 prepared rows, 5 entries, zero failures; `real 0.20s`.
- Strict apply: 5 prepared rows, 5 entries, zero failures; `real 0.29s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` found once, all intended masters and notes matched, and stored `FromLb`/`ToLb` anchors passed `zc.verify` for 5/5 rows.
- All 5 modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
