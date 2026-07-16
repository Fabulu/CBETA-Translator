# Source-batched attribution report: X68n1318

Scope: the 6 candidate occurrences in `quick-X68n1318.md`, covering 5 complete cases and 5 entry IDs. This was attribution-only remediation; no senses, glosses, KWICs, evidence inventories, or source ranges were changed.

## Exact-turn adjudication

Every complete case and exact headword-bearing turn was reviewed. Two draft defaults were retained and four contradictions were overridden.

- `入泥入水` retains Bodhidharma: he is the historical actor described as entering mud and water before Shaoshi Peak; Ciming Chuyuan is the later speaker of that description.
- `體露` retains Yunmen Wenyan, the direct respondent who says `體露金風`.
- `格外` is Tianyi Yihuai's direct discourse. Xuedou Chongxian appears only as the lineage-header teacher.
- Both overlapping `心行處滅` rows are impersonal, source-unattributed quoted-formula states. The headword belongs to the proposition before `保寧道`; Baoning Renyong's explicit counter says `心行不滅`. All ordered evidence rungs and the closest parallel preserve no named originator.
- `大徹大悟` belongs to Zhuyuan Yuan, the exact section speaker. Dahui Zonggao is his lineage-header teacher, not the speaker.

`Zhuyuan Yuan` is retained as a source-attested non-roster exact name rather than hidden behind a generic master label.

## Changed IDs

- `t_e95ea628d5dd` 入泥入水
- `t_94be914de45d` 體露
- `t_7bd745af24d7` 格外
- `t_438eb81f17bf` 心行處滅 (2 rows)
- `t_8184622cecd7` 大徹大悟

## Counts

Workbook-scoped 6 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 4 |
| Impersonal exact-actor states | 0 | 2 |
| Bare unresolved actors | 6 | 0 |
| Notes naming `續古尊宿語要` and the exact actor/state | 0 | 6 |
| Exact `zc.verify` successes | not rerun | 6/6 |

Whole-source inventory for `X/X68/X68n1318.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 42 | 42 | 0 |
| Named occurrences | 15 | 19 | +4 |
| Structured actor exceptions | 0 | 2 | +2 |
| Bare unresolved occurrences | 27 | 21 | -6 |

Full `audit_attribution.py --json` over the 5 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 25 | 25 | 0 |
| Named occurrences | 3 | 7 | +4 |
| Impersonal occurrences | 0 | 2 | +2 |
| Unresolved actors | 22 | 16 | -6 |
| Notes missing exact speaker/state | 24 | 20 | -4 |
| Notes missing source title | 24 | 18 | -6 |
| Deferred non-roster exact names | 0 | 1 | +1 |
| Hard failures | 78 | 62 | -16 |

The 62 inherited failures are confined to untouched out-of-scope occurrences or prose in these five entries: 16 unresolved actors, 20 notes missing an exact speaker/state, 18 notes missing a source, three vague-attributor findings, and five dangling-Chinese findings.

## Mechanical checks

- Signed compile: 6 rows, 4 overrides; `real 0.06s`.
- Strict dry-run: 6 prepared rows, 5 entries, zero failures; `real 0.09s`.
- Strict apply: 6 prepared rows, 5 entries, zero failures; `real 0.16s`.
- The focused gate found every exact `(entry ID, FromLb, KWIC)` once, matched each intended named or impersonal state and note, and passed stored `FromLb`/`ToLb` verification for all 6 rows.
- All 5 modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
