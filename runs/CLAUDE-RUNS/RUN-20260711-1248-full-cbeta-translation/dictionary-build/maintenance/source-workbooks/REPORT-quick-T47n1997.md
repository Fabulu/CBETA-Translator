# Source-batched attribution report: T47n1997

Scope: the six workbook rows and six complete cases in `quick-T47n1997.md`, spanning five disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All six complete cases were reviewed before the sheet was signed. The strict compiler and apply gate required every attribution note to contain both `圓悟佛果禪師語錄` and the exact actor's canonical name. Four defaults survived exact-turn review; the two `俱胝一指` rows required overrides.

Retained defaults:

- `拈古`: the structural volume heading belongs to Yuanwu Keqin's recorded-sayings collection.
- `隨處`: Yuanwu says that, if it is thoroughly understood, one acts as host wherever one is.
- `淨裸裸`: Yuanwu's hall address says `淨裸裸無遺，赤灑灑全露`.
- `牢關`: Yuanwu delivers `末後一句始到牢關` in his own address.

Overrides:

- `俱胝一指` at `0741a21`: Yuanwu Keqin is the exact speaker who lists Xuefeng's rolling ball, Yunmen's looking, Muzhou's ready-made response, and Juzhi's one finger as cast like raw iron. Juzhi and the other masters are discussed figures, not speakers of this line.
- `俱胝一指` at `0750a21`: Yuanwu is again the exact speaker, saying that Juzhi used this occasion atop his one finger and that Niaoke saw it when blowing the cloth hair. Juzhi and Niaoke are contextual people, not the line's voices.

## Exact changed IDs

- `t_66792ea088de` 拈古
- `t_21a3463bc0db` 隨處
- `t_6c1f113fbdcd` 淨裸裸
- `t_b701ed7c340b` 俱胝一指
- `t_ffb0ee18f1a2` 牢關

## Before/after counts

Workbook-scoped six rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 6 |
| Reviewed unnamed exact actors | 0 | 0 |
| Unresolved exact actors | 6 | 0 |
| Attribution notes present | 6 | 6 |
| Notes naming `圓悟佛果禪師語錄` | 1 | 6 |
| Notes containing exact actor name | 0 | 6 |
| Structured context-master links | 0 | 6 |
| Exact `zc.verify` successes | not rerun | 6/6 |

Full `audit_attribution.py --json` run over all five modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 24 | 24 | 0 |
| Named occurrences | 6 | 12 | +6 |
| Unresolved actors | 17 | 11 | -6 |
| Notes missing exact speaker/actor state | 17 | 11 | -6 |
| Notes missing source title | 15 | 10 | -5 |
| Context-master links | 4 | 10 | +6 |
| Deferred non-roster context links | 0 | 1 | +1 |
| Hard failures | 82 | 65 | -17 |

The deferred contextual name is source-attested Niaoke Daolin. The 65 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 70 Chinese prose strings, 44 anchored, 26 dangling.

## Speed and mechanical checks

- Signed-sheet compile plus strict dry-run completed in about 0.4 seconds and prepared 6/6 rows with zero failures.
- The baseline focused audit plus atomic application completed together in about 0.8 seconds; the apply reported 6/6 prepared and zero failures.
- The post-apply scoped audit, JSON parse gate, six exact KWIC replays, and whitespace check completed in about 1.0 second.
- All five entry JSON files parse after editing.
- All six touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
