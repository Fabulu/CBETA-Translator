# Source-batched attribution report: X72n1443

Scope: the 2 candidate occurrences in `quick-X72n1443.md`, covering one complete case and 2 entry IDs. This was attribution-only remediation.

## Regenerated-triage check

Both prepared workbook rows matched the current `maintenance/attribution-triage-all.json` exactly for source, entry ID, term, case-cluster ID, line anchors, and KWIC. The current source inventory also contains five selected rows outside this explicitly assigned two-row workbook; they were not changed in this wave.

## Exact-turn adjudication

The complete case contains Zongbao Daodu's later raising, an unnamed monk's report, and Damei Fachang's nested report of Mazu Daoyi's words.

- `非心非佛` remains attributed to Damei Fachang for the selected deployment in his direct answer, `任你非心非佛，我祇是即心即佛`. The unnamed monk first reports that Mazu now says the phrase, then Damei uses it himself while rejecting the change.
- `即心即佛` was overridden from Damei Fachang to Mazu Daoyi. Damei explicitly says, `我當時見馬大師向我道：即心即佛`; Mazu is therefore the exact quoted speaker, while Damei is the reporter and Zongbao Daodu is the later raiser.

## Changed IDs

- `t_6457935dff62` 非心非佛
- `t_dfd1dbffe9f2` 即心即佛

## Counts

Workbook-scoped 2 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 2 |
| Bare unresolved actors | 2 | 0 |
| Default decisions retained | — | 1 |
| Defaults contradicted and overridden | — | 1 |
| Exact `zc.verify` successes | not rerun | 2/2 |

Whole-source inventory for `X/X72/X72n1443.xml` across all entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 11 | 11 | 0 |
| Named occurrences | 4 | 6 | +2 |
| Structured actor exceptions | 0 | 0 | 0 |
| Bare unresolved occurrences | 7 | 5 | -2 |

Full `audit_attribution.py --json` over the 2 modified entries:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 10 | 10 | 0 |
| Named occurrences | 1 | 3 | +2 |
| Context-master links | 0 | 2 | +2 |
| Unresolved actors | 9 | 7 | -2 |
| Notes missing exact speaker/state | 9 | 7 | -2 |
| Notes missing source title | 2 | 2 | 0 |
| Hard failures | 31 | 27 | -4 |

The 27 inherited failures belong to untouched out-of-scope occurrences or prose in these entries: seven unresolved actors, seven notes missing a speaker/state, two notes missing a source, one vague-attributor finding, and ten dangling-Chinese findings.

## Mechanical checks

- Signed compile: 2 rows, 1 override; `real 0.05s`.
- Strict dry-run: 2 prepared rows, 2 entries, zero failures; `real 0.08s`.
- Strict apply: 2 prepared rows, 2 entries, zero failures; `real 0.12s`.
- Focused gate: every exact `(entry ID, FromLb, KWIC)` was found once; intended masters, notes, and context masters matched; stored `FromLb`/`ToLb` anchors passed `zc.verify` 2/2.
- Both modified entry JSON files parse through the audit and focused gate.
- No merge, commit, or push was performed.
