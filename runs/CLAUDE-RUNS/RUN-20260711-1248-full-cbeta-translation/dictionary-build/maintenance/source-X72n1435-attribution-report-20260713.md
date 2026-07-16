# Source-batched attribution report — `X/X72/X72n1435.xml`

Source: *Extensive Record of Chan Master Wuyi Yuanlai* (`無異元來禪師廣錄`)

Scope: attribution-only repair of the 15 occurrences assigned in `maintenance/source-workbooks/quick-X72n1435.md`. This report does **not** claim whole-entry semantic or attribution remediation.

## Changed entry IDs

1. `t_1e3e02536ca2` — 疑團 (2 occurrences)
2. `t_d95b944e0749` — 透網金鱗
3. `t_6dadcc69c361` — 料揀
4. `t_3972185a2e25` — 宗門 (2 occurrences)
5. `t_78bd967fdcd6` — 大疑
6. `t_368268e023e3` — 塵勞
7. `t_48e808f5d2a7` — 門庭
8. `t_428845c4921d` — 剎那
9. `t_bb19ed0e0fab` — 普說
10. `t_3414320aa87c` — 命根
11. `t_dcd5468f5104` — 直心
12. `t_e156057131dc` — 本參
13. `t_e228a7015e6f` — 絕後再甦

## Exact-turn decisions

- Twelve complete cases and thirteen occurrences are actor-pure speech by Wuyi Yuanlai after review of the full structural unit.
- The `透網金鱗` question belongs to an unnamed questioning monk, not Wuyi. It uses the complete six-rung `reviewed-unnamed` branch; Wuyi is stored separately as respondent and record owner.
- The `直心` definition is explicitly governed by “the Awakening of Faith Treatise says.” It uses an impersonal textual-citation branch; Wuyi is separately stored as quoter and record owner.
- The `命根` occurrence originally mixed the unnamed monk's question and Wuyi's answer. Its KWIC was narrowed to the actor-pure response, `師云：待闍黎命根斷即道。`, which verifies at the same source lines.
- Embedded quotations in the other units were checked against the exact headword turn. The headword-bearing speech remained Wuyi's own framing or statement rather than the embedded person's turn.

## Assigned-cohort before / after

| Measure | Before | After |
|---|---:|---:|
| Assigned occurrences | 15 | 15 |
| Unresolved exact actors | 15 | 0 |
| Named actors | 0 | 13 |
| Reviewed unnamed non-master actors | 0 | 1 |
| Impersonal textual citations | 0 | 1 |
| Notes missing an actor | 15 | 0 |
| Notes missing the source title | 14 | 0 |

## Whole modified-file audit, reported honestly

The 13 touched entries contain 73 occurrences, many outside this source assignment. Because this was not a whole-entry pass, their unrelated old failures remain:

| Audit measure across all 13 files | Before | After |
|---|---:|---:|
| Named occurrences | 3 | 16 |
| Unresolved actors | 70 | 55 |
| Notes missing a speaker | 72 | 57 |
| Notes missing a source | 69 | 55 |
| Total attribution hard failures | 237 | 191 |

All 191 residual hard failures belong to occurrences or prose outside the 15-item assigned cohort. The assigned cohort has zero attribution failures.

## Mechanical evidence

- JSON parsing: all 13 modified files pass.
- Exact verification: 73/73 stored KWICs across the modified files pass, zero failures (`maintenance/verify-source-X72n1435-final.json`).
- Attribution audit: assigned 15/15 pass; full-file residuals are recorded in `maintenance/audit-attribution-source-X72n1435-final.json`.
- Complete cases reviewed: 14/14.
- No merged artifact was regenerated; no commit or push was made.
