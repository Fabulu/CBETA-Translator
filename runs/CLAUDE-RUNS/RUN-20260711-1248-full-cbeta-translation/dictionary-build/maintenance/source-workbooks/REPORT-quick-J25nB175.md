# Source-batched attribution report: J25nB175

Scope: the single regenerated-triage workbook row and complete case in `quick-J25nB175.md`, for the `嗣法` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line range, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case was reviewed before the sheet was signed. The short title default was not safe and required a full override.

- `嗣法`: Tao Runai's pagoda inscription states `五峰禪師，嗣法於天童密雲悟和尚`. Wufeng Ruxue is the exact grammatical actor who inherits the Dharma; Miyun Yuanwu is his named transmission teacher and predecessor. The current roster's canonical `Wufeng` is a different Tang master and cannot be assigned to the late-Ming `五峰如學`. The occurrence therefore uses the established source-attested deferred non-roster name `Wufeng Ruxue`, with Miyun Yuanwu linked as context.

## Exact changed ID

- `t_7ccccfa5fe9a` 嗣法

## Before/after counts

Workbook-scoped row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Unresolved exact actors | 1 | 0 |
| Attribution notes present | 1 | 1 |
| Exact source-and-actor notes | 0 | 1 |
| Context-master links | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Focused `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 6 | 6 | 0 |
| Named occurrences | 1 | 2 | +1 |
| Unresolved actors | 4 | 3 | -1 |
| Notes missing exact speaker/actor | 4 | 3 | -1 |
| Notes missing source title | 4 | 3 | -1 |
| Context-master links | 1 | 2 | +1 |
| Hard failures | 15 | 12 | -3 |

The 12 remaining audit failures belong to untouched, out-of-scope occurrences and prose in this entry. The post-audit records Wufeng Ruxue as an honest deferred non-roster master instead of conflating him with the roster's ancient Wufeng.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` across its stored `0757b30`–`0757c13` range.
- Miyun Yuanwu matches the roster's canonical `names[0]`; Wufeng Ruxue is retained under the established deferred non-roster precedent.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
