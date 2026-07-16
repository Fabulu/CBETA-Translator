# Source-batched attribution report: J33nB294

Scope: the single regenerated-triage workbook row and complete case in `quick-J33nB294.md`, for the `一指頭禪` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line range, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case was reviewed before the sheet was signed. The Juzhi default survived exact-actor review, but a full decision was retained to name the later narrator and make the turn boundary explicit.

- `一指頭禪`: Langting Jingting is instructing the attendant Qianyun and retells the old Juzhi case. Inside the stored sentence, Juzhi is the explicit narrated actor: `俱胝和尚逢人但伸一指頭，喚做一指頭禪`. The attendant, Juzhi's boy, and the later quoted Caoshan appraisal do not speak or perform this sentence. Juzhi is therefore the exact actor; Langting Jingting is linked separately as later narrator and record owner.

## Exact changed ID

- `t_b655ff97e2c3` 一指頭禪

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
| Occurrences audited | 5 | 5 | 0 |
| Named occurrences | 3 | 4 | +1 |
| Unresolved actors | 2 | 1 | -1 |
| Notes missing exact speaker/actor | 2 | 1 | -1 |
| Notes missing source title | 2 | 1 | -1 |
| Context-master links | 1 | 2 | +1 |
| Hard failures | 7 | 4 | -3 |

The four remaining audit failures belong to untouched, out-of-scope material: the separate Sanshan occurrence and one dangling Chinese related-term string. Langting Jingting is source-attested but absent from the current roster, so his context link is recorded as an honest deferred non-roster context master.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` across its stored `0755c18`–`0755c19` range.
- Juzhi matches the roster's canonical `names[0]`; Langting Jingting is retained under the established source-attested deferred non-roster precedent.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
