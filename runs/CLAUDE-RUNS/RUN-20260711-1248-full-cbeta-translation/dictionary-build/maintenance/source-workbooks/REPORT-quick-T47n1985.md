# Source-batched attribution report: T47n1985

Scope: all seven rows and seven compact complete cases in `quick-T47n1985.md`. Defaults were drafts until complete-case review. This was attribution-only remediation; no sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Defaults versus overrides

Five Linji Yixuan defaults survived exact-turn review. Two prefatory occurrences were overridden.

| Line | Term | Draft default | Reviewed result |
|---|---|---|---|
| `0495a10` | 正法眼 | Linji Yixuan | overridden: Linquan Conglun |
| `0496b01` | 照用 | Linji Yixuan | overridden: Ma Fang |
| `0496c08` | 便下座 | Linji Yixuan | accepted |
| `0496c21` | 便下座 | Linji Yixuan | accepted |
| `0497b14` | 無事人 | Linji Yixuan | accepted |
| `0497c01` | 觸目 | Linji Yixuan | accepted |
| `0500b22` | 逢祖殺祖 | Linji Yixuan | accepted |

Both contradictions came from excluded contributor sections rather than Linji's recorded discourse:

- Linquan Conglun signs the first preface and is the exact author who says that Linji made clear the nirvana mind with the true teaching eye. Linji is the praised subject.
- The TEI byline names Ma Fang as author of the later preface. Ma Fang is the exact author who says illumination and function occur simultaneously and have no before or after; Linji is again the subject.

## Before/after counts

Workbook-scoped rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 7 |
| Reviewed actor exceptions | 0 | 0 |
| Unresolved exact actors | 7 | 0 |
| Notes naming `鎮州臨濟慧照禪師語錄` and the exact actor | 0 | 7 |
| Exact KWIC and full stored-range verification | not rerun | 7/7 |

Whole-source inventory for `T/T47/T47n1985.xml`:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 55 | 55 | 0 |
| Named occurrences | 44 | 51 | +7 |
| Structured actor exceptions | 4 | 4 | 0 |
| Bare unresolved occurrences | 7 | 0 | -7 |

Full `audit_attribution.py --json` run over the six touched entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 34 | 34 | 0 |
| Named occurrences | 0 | 7 | +7 |
| Unresolved actors | 34 | 27 | -7 |
| Notes missing exact speaker | 34 | 27 | -7 |
| Notes missing source title | 25 | 20 | -5 |
| Deferred non-roster exact names | 0 | 1 | +1 |
| Hard failures | 101 | 82 | -19 |

The one newly deferred non-roster exact name is the source-named preface author Ma Fang. The 82 remaining audit failures are inherited, out-of-scope findings in untouched occurrences or prose in these entries.

## Workflow and timing

- Signed override sheet: `overrides-T47n1985.json`, seven reviewed cases, two overrides, five accepted defaults.
- Compile: 0.12 seconds.
- Strict dry-run: 0.25 seconds; seven prepared rows across six entries, zero failures.
- Atomic apply: 0.54 seconds; seven prepared rows across six entries, zero failures.
- Mechanical compile/dry-run/apply total: 0.91 seconds.
- Focused source and full-range KWIC gate: 8.58 seconds; all seven rows found exactly once and all seven stored `FromLb`/`ToLb` ranges verified.

The strict tool rejected nothing. Compact cases made the five discourse rows fast to confirm, while the two prefatory risks still required checking the signature and TEI author byline.

## Artifacts and mechanical checks

- `maintenance/source-workbooks/overrides-T47n1985.json`
- `maintenance/source-workbooks/decisions-T47n1985.json`
- `maintenance/source-workbooks/decisions-T47n1985-dryrun.json`
- `maintenance/source-workbooks/decisions-T47n1985-applied.json`
- All six touched entry JSON files parse after atomic rewriting.
- Final focused gate: seven named actors, zero conflicting states, 7/7 source-and-actor notes, and no bare unresolved occurrence remaining for this source.
- No merge, commit, or push was performed.
