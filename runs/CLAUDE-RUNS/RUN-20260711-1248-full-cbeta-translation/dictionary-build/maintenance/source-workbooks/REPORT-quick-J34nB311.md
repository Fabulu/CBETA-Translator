# Source-batched attribution report: J34nB311

Scope: the single candidate occurrence in `quick-J34nB311.md`, covering one complete chamber-instruction unit and one entry ID. This was attribution-only remediation.

## Regenerated-triage check

The prepared workbook row matched the current regenerated `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory contains fourteen additional selected rows outside this explicitly assigned one-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The inline-named default Yantou Quanhuo survived complete-case review; no exception override was required.

- `綱宗` lies inside the explicitly introduced quotation `巖頭云：「大統綱宗，先須識句。」` Yantou Quanhuo is therefore the exact quoted speaker.
- Juelang Daosheng, speaking as `杖人`, comments immediately afterward on recognizing the saying and the lineage principle, then raises Yantou's old exchange. His commentary does not absorb the preceding quoted sentence.

## Changed ID

- `t_80ea075a6c5d` 綱宗

## Counts

Workbook-scoped one row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Bare unresolved actors | 1 | 0 |
| Notes naming `天界覺浪盛禪師全錄` and Yantou Quanhuo | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Full `audit_attribution.py --json` over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 2 | 3 | +1 |
| Unresolved actors | 3 | 2 | -1 |
| Notes missing exact speaker/state | 3 | 2 | -1 |
| Notes missing source title | 0 | 0 | 0 |
| Hard failures | 17 | 15 | -2 |

The 15 inherited failures belong to untouched out-of-scope occurrences or prose in this entry: two unresolved actors, two notes missing a speaker/state, one vague-attributor finding, and ten dangling-Chinese findings.

## Mechanical checks

- Signed compile: 1 row, 0 overrides; `real 0.13s`.
- Strict dry-run: 1 prepared row, 1 entry, zero failures; `real 0.16s`.
- Strict apply: 1 prepared row, 1 entry, zero failures; `real 0.17s`.
- Focused gate: the exact `(entry ID, FromLb, KWIC)` was found once; intended master and note matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 1/1.
- The modified entry and all workflow JSON files parse successfully.
- No merge, commit, or push was performed.
