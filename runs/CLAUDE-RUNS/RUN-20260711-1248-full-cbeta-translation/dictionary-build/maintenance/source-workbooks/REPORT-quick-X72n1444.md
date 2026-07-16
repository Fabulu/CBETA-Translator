# Source-batched attribution report: X72n1444

Scope: the two regenerated-triage workbook rows and complete cases in `quick-X72n1444.md`, spanning the `惺惺` and `綿密` entries. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, both workbook rows were matched against `maintenance/attribution-triage-all.json`. Entry IDs, terms, source, line anchors, KWICs, case-cluster offsets, and the two selected-occurrence total match the current regenerated triage exactly.

## Exception-sheet result

Both complete cases were reviewed before the sheet was signed. Both proposed defaults contradicted the exact turns and required full overrides.

- `惺惺`: Ruiyan Shiyan is the exact quoted speaker. Zhanran Yuancheng raises the old Ruiyan/Yantou case and later comments on it; Ruiyan calls to himself, answers, and tells himself to remain alert and not be fooled. The stale Yongjue Yuanxian attribution was corrected. Zhanran Yuancheng is retained as later raiser and record owner in `ContextMasters`.
- `綿密`: Cizhou Fangnian is the exact speaker abbreviated `舟`. The tower inscription first identifies the lineage as passing to `慈舟`, and the same source elsewhere gives `慈舟念`; independent corpus headings give the full source-attested name `慈舟方念`. Cizhou appraises Zhanran Yuancheng's verse and then transmits the Dharma to him. The Yunmen heading denotes Zhanran's monastery/place association, not Yunmen Wenyan as speaker. Cizhou is absent from the current roster, so this is an honest deferred non-roster name rather than an unresolved actor.

## Exact changed IDs

- `t_882860247a9b` 惺惺
- `t_412d9358cc70` 綿密

## Before/after counts

Workbook-scoped two rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Unresolved exact actors | 2 | 0 |
| Attribution notes present | 2 | 2 |
| Exact source-and-speaker notes | 0 | 2 |
| Context-master links | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Focused `audit_attribution.py --json` run over both modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 12 | 12 | 0 |
| Named occurrences | 4 | 6 | +2 |
| Unresolved actors | 8 | 6 | -2 |
| Notes missing exact speaker | 12 | 10 | -2 |
| Notes missing source title | 9 | 7 | -2 |
| Hard failures | 37 | 31 | -6 |

The 31 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 34 Chinese prose strings, 30 anchored, 4 dangling. The post-audit also records one deferred non-roster exact speaker (`Cizhou Fangnian`) and two deferred non-roster context links (`Zhanran Yuancheng`), rather than silently discarding either named master.

## Mechanical checks

- Strict dry-run prepared 2/2 rows with zero failures; atomic application prepared and applied 2/2 with zero failures.
- Both entry JSON files and all workbook decision/report JSON artifacts parse after editing.
- Both touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors: `0791c04`–`0791c05` and `0839b02`–`0839b03`.
- `Ruiyan Shiyan` matches the roster's canonical `names[0]`; `Cizhou Fangnian` is retained as a source-attested deferred non-roster master.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
