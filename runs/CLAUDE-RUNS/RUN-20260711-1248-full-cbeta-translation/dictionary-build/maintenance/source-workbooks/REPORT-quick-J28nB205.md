# Source-batched attribution report: J28nB205

Scope: the single regenerated-triage workbook row and complete case in `quick-J28nB205.md`, for the `結制` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line range, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case was reviewed before the sheet was signed. The co-located reviewed default survived exact-turn review; no override was required.

- `結制`: in Jie Weizhou's Lantern Festival release-from-restriction hall address, `師云` marks his replies and `乃云` continues his speech into the stored occurrence. Jie Weizhou explicitly says that the fifteenth day of the previous tenth month was called opening the restriction and the current first-month fifteenth was called releasing it, then describes binding the six faculties together during the restriction. No embedded quotation or interlocutor takes over this turn.

## Exact changed ID

- `t_b0f2ccf6d140` 結制

## Before/after counts

Workbook-scoped row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Unresolved exact actors | 1 | 0 |
| Attribution notes present | 1 | 1 |
| Notes naming source and actor | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Focused `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 6 | 6 | 0 |
| Named occurrences | 0 | 1 | +1 |
| Unresolved actors | 6 | 5 | -1 |
| Notes missing exact speaker | 6 | 5 | -1 |
| Notes missing source title | 6 | 5 | -1 |
| Hard failures | 19 | 16 | -3 |

The 16 remaining audit failures belong to untouched, out-of-scope occurrences and prose in this entry. Jie Weizhou is source-attested but not yet present in the roster, so the post-audit records one honest deferred non-roster exact actor.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` across its stored `0226a25`–`0226a27` range.
- The prepared decision records `wholeCaseReviewed: true` and `usedDefault: true` for the source-attested name Jie Weizhou.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
