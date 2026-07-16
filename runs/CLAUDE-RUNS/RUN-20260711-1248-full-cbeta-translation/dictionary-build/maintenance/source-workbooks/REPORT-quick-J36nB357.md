# Source-batched attribution report: J36nB357

Scope: the single regenerated-triage workbook row and complete case in `quick-J36nB357.md`, for the `落處` entry. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Regenerated-triage confirmation

Before signing, the workbook row was matched against `maintenance/attribution-triage-all.json`. Entry ID, term, source, line anchor, KWIC, case-cluster offsets, source title, and the one selected-occurrence total all match the current regenerated triage exactly.

## Exception-sheet result

The complete case was reviewed before the sheet was signed. The co-located Xiuyelin default survived exact-turn review; a full decision was retained to make the document section and quotation boundary explicit.

- `落處`: the governing section is `示夢齋潘居士`, Xiuyelin's instruction to Layman Mengzhai Pan. Xiuyelin directly says `本分事者，即當人本命元辰之落處也`. The quoted old worthy's line ends several sentences earlier; neither that old worthy nor Layman Pan speaks the stored sentence. Xiuyelin is therefore the exact speaker.

## Exact changed ID

- `t_1459058101b7` 落處

## Before/after counts

Workbook-scoped row:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 1 |
| Unresolved exact actors | 1 | 0 |
| Attribution notes present | 1 | 1 |
| Exact source-and-speaker notes | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 1/1 |

Focused `audit_attribution.py --json` run over the modified entry:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 6 | 6 | 0 |
| Named occurrences | 3 | 4 | +1 |
| Unresolved actors | 3 | 2 | -1 |
| Notes missing exact speaker | 6 | 5 | -1 |
| Notes missing source title | 6 | 5 | -1 |
| Hard failures | 16 | 13 | -3 |

The 13 remaining audit failures belong to untouched, out-of-scope occurrences and prose in this entry. Quote-anchor counters remain mechanically clean: all 10 detected Chinese prose strings are anchored. Xiuyelin is source-attested but absent from the current roster, so the audit records one honest deferred non-roster speaker.

## Mechanical checks

- Strict compile and dry-run prepared 1/1 row with zero failures; atomic application prepared and applied 1/1 with zero failures.
- The entry JSON and all workbook decision/report JSON artifacts parse after editing.
- The touched KWIC passes exact `zc.verify` at its stored `0589c13` line anchor.
- Xiuyelin is retained under the established source-attested deferred non-roster precedent rather than left unresolved.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
