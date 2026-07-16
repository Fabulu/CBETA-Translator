# Source-batched attribution report: X78n1553

Scope: all seven rows and seven complete cases in `quick-X78n1553.md`. Defaults remained drafts until complete-case exact-turn review. This was attribution-only remediation; no sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Defaults versus overrides

Five draft defaults survived review. Two were overridden.

| Line | Term | Draft default | Reviewed result |
|---|---|---|---|
| `0457c10` | 死語 | Baizhang Huaihai | accepted |
| `0458c17` | 入定 | Baizhang Huaihai | accepted |
| `0463a19` | 無事人 | Baizhang Huaihai | accepted |
| `0463c22` | 自在 | Baizhang Huaihai | accepted |
| `0467c06` | 如何是祖師西來意 | Linji Yixuan | overridden: Zhaozhou Congshen |
| `0469a22` | 鵝王 | Linji Yixuan | accepted |
| `0539c24` | 保任 | Deshan Xuanjian | overridden: Deshan Zhixian |

The two contradictions are exact-turn and identity errors:

- Inside Linji Yixuan's section, Zhaozhou Congshen is explicitly named as the traveling questioner who asks the stored patriarchal-intent question. Linji supplies the following foot-washing answer.
- The final case is headed `鼎州德山志先禪師`. Deshan Zhixian is the exact respondent who says that maintaining is itself mistaken; the draft conflated him with the different master Deshan Xuanjian.

## Before/after counts

Workbook-scoped rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 7 |
| Reviewed actor exceptions | 0 | 0 |
| Unresolved exact actors | 7 | 0 |
| Notes naming `天聖廣燈錄` and the exact actor | 0 | 7 |
| Exact KWIC and full stored-range verification | not rerun | 7/7 |

Whole-source inventory for `X/X78/X78n1553.xml`:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 25 | 25 | 0 |
| Named occurrences | 9 | 16 | +7 |
| Structured actor exceptions | 2 | 2 | 0 |
| Bare unresolved occurrences | 14 | 7 | -7 |

Full `audit_attribution.py --json` run over the seven touched entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 45 | 45 | 0 |
| Named occurrences | 10 | 17 | +7 |
| Reviewed unnamed occurrences | 1 | 1 | 0 |
| Unresolved actors | 34 | 27 | -7 |
| Notes missing exact speaker | 38 | 31 | -7 |
| Notes missing source title | 37 | 31 | -6 |
| Deferred non-roster exact names | 0 | 1 | +1 |
| Hard failures | 130 | 110 | -20 |

The newly deferred non-roster exact name is the source-attested Deshan Zhixian. The 110 remaining audit failures are inherited, out-of-scope findings in untouched occurrences or prose in these entries.

## Workflow and timing

- Signed override sheet: `overrides-X78n1553.json`, seven reviewed cases, two overrides, five accepted defaults.
- Compile: 0.15 seconds.
- Strict dry-run: 0.26 seconds; seven prepared rows across seven entries, zero failures.
- Atomic apply: 0.42 seconds; seven prepared rows across seven entries, zero failures.
- Mechanical compile/dry-run/apply total: 0.83 seconds.
- Focused source and full-range KWIC gate: 6.77 seconds; all seven rows found exactly once and all stored `FromLb`/`ToLb` ranges verified.

The strict tool rejected nothing. The compact cases made the four Baizhang discourse rows and Linji's goose-king comparison straightforward; targeted turn and header checks exposed both contradictions.

## Artifacts and mechanical checks

- `maintenance/source-workbooks/overrides-X78n1553.json`
- `maintenance/source-workbooks/decisions-X78n1553.json`
- `maintenance/source-workbooks/decisions-X78n1553-dryrun.json`
- `maintenance/source-workbooks/decisions-X78n1553-applied.json`
- All seven touched entry JSON files parse after atomic rewriting.
- Final focused gate: seven named actors, zero conflicting states, and 7/7 source-and-actor notes.
- No merge, commit, or push was performed.
