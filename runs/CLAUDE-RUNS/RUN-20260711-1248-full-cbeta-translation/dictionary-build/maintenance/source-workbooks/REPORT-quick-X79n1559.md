# Source-batched attribution report: X79n1559

Scope: all nine rows and nine complete cases in `quick-X79n1559.md`. The prebuilt defaults were treated only as drafts. This was attribution-only remediation; no sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Defaults versus reviewed overrides

Six draft defaults survived complete-case exact-turn review. Three were overridden.

| Line | Term | Draft default | Reviewed result |
|---|---|---|---|
| `0289b16` | 安心 | Bodhidharma | accepted |
| `0301a19` | 擬對 | Touzi Yiqing | accepted |
| `0302c23` | 佛手 | Huanglong Huinan | accepted |
| `0303a01` | 生緣 | Huanglong Huinan | accepted |
| `0304c18` | 面壁 | Luzu | overridden: Cuiyan Kezhen |
| `0309c05` | 擬議 | Furong Daokai | accepted |
| `0343c18` | 迦葉 | Yuelin Zongshan | accepted |
| `0362b08` | 異類中行 | Dasui Fazhen | overridden: Nantang Yuanjing |
| `0429b09` | 德山托鉢 | Zhang Shangying | overridden: Doushuai Congyue |

The contradictions were distinct:

- Cuiyan Kezhen asks why Luzu faced the wall. Luzu is the embedded subject, while Cuiyan owns the headword-bearing chamber question.
- The `異類中行` address belongs to Nantang Yuanjing, later named Daoxing. The draft conflated him with the different master Dasui Fazhen through the shared place-name.
- Doushuai Congyue raises Deshan's bowl-carrying case and assigns it to Zhang Shangying. Zhang studies it and awakens later, but he is not the actor who raises the headword case here.

## Before/after counts

Workbook-scoped rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 9 |
| Reviewed actor exceptions | 0 | 0 |
| Unresolved exact actors | 9 | 0 |
| Notes naming `嘉泰普燈錄` and the exact actor | 0 | 9 |
| Exact KWIC and full stored-range verification | not rerun | 9/9 |

Whole-source inventory for `X/X79/X79n1559.xml`:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 41 | 41 | 0 |
| Named occurrences | 16 | 25 | +9 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 25 | 16 | -9 |

Full `audit_attribution.py --json` run over the nine touched entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 62 | 62 | 0 |
| Named occurrences | 13 | 22 | +9 |
| Unresolved actors | 49 | 40 | -9 |
| Notes missing exact speaker | 58 | 49 | -9 |
| Notes missing source title | 53 | 45 | -8 |
| Deferred non-roster exact names | 1 | 4 | +3 |
| Hard failures | 168 | 142 | -26 |

The 142 remaining audit failures are inherited, out-of-scope findings in untouched occurrences or prose in these entries.

## Workflow and timing

- Signed override sheet: `overrides-X79n1559.json`, nine reviewed cases, three overrides, six accepted defaults.
- Compile: 0.17 seconds.
- Stricter dry-run: 0.41 seconds; nine prepared rows, nine entries, zero failures.
- Atomic apply: 0.61 seconds; nine prepared rows, nine entries, zero failures.
- Mechanical compile/dry-run/apply total: 1.19 seconds.
- Focused source and full-range KWIC gate: 8.57 seconds; all nine rows found exactly once and all nine stored `FromLb`/`ToLb` ranges verified.
- Review time was not instrumented at task receipt, so no fabricated wall-clock figure is reported. Qualitatively, the compact workbook materially improved review speed: all nine complete cases could be adjudicated in one pass, with targeted header and identity checks only.

The stricter tool rejected nothing. It verified exact actor-name and source-title presence in each note, unique stored occurrence identity, and the source anchor before applying.

## Artifacts and mechanical checks

- `maintenance/source-workbooks/overrides-X79n1559.json`
- `maintenance/source-workbooks/decisions-X79n1559.json`
- `maintenance/source-workbooks/decisions-X79n1559-dryrun.json`
- `maintenance/source-workbooks/decisions-X79n1559-applied.json`
- All nine touched entry JSON files parse after atomic rewriting.
- Final focused gate: nine named actors, zero conflicting actor states, and 9/9 source-and-actor notes.
- No merge, commit, or push was performed.
