# Source-batched attribution report: T48n2003 promoted remainder

Scope: the seven promoted workbook rows in `quick2-T48n2003.md`, spanning five complete cases and seven disjoint entry IDs. A key comparison against the previous `decisions-T48n2003.json` confirmed zero overlap with its 27 rows. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All five complete cases were reviewed before the sheet was signed. The strict compiler and apply gate required every attribution note to contain both `佛果圜悟禪師碧巖錄` and the exact actor's canonical name. Six Yuanwu Keqin defaults survived exact-turn review; one quoted first-person turn required an override.

Retained Yuanwu Keqin defaults:

- `弄精魂`, `沒交涉`, and `漏逗`: Yuanwu supplies the surrounding appraisal of mistaken readings and Bodhidharma's exchange.
- `卜度`: Yuanwu asks whether the case can be conjectured through discriminating consciousness.
- `有什麼交涉`: Yuanwu rejects the contemporary left-eye/right-eye construal of Mazu's saying.
- `淨裸裸`: Yuanwu describes passing the barrier and emerging clean naked and bare free.

Override:

- `德山棒`: the headword occurs in Xuefeng Yicun's quoted first-person recollection, `我當時在德山棒下，如桶底脫相似`. Xuefeng is the exact speaker; Deshan Xuanjian is the teacher and striker in the recounted encounter; Yuanwu Keqin is the later commentator and record owner.

## Exact changed IDs

- `t_b90a5f36ec86` 弄精魂
- `t_aef7434b8470` 沒交涉
- `t_898279a78ecf` 漏逗
- `t_a0f2bb1de215` 卜度
- `t_b4367c692c8a` 有什麼交涉
- `t_a14a883193a5` 德山棒
- `t_6c1f113fbdcd` 淨裸裸

## Before/after counts

Workbook-scoped seven rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 7 |
| Reviewed unnamed exact actors | 0 | 0 |
| Unresolved exact actors | 7 | 0 |
| Attribution notes present | 7 | 7 |
| Notes naming `佛果圜悟禪師碧巖錄` | 1 | 7 |
| Notes containing exact actor name | 0 | 7 |
| Structured context-master links | 0 | 2 |
| Exact `zc.verify` successes | not rerun | 7/7 |

Full `audit_attribution.py --json` run over all seven modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 36 | 36 | 0 |
| Named occurrences | 5 | 12 | +7 |
| Unresolved actors | 30 | 23 | -7 |
| Notes missing exact speaker/actor state | 31 | 24 | -7 |
| Notes missing source title | 25 | 19 | -6 |
| Context-master links | 1 | 3 | +2 |
| Hard failures | 103 | 83 | -20 |

The 83 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 56 Chinese prose strings, 47 anchored, 9 dangling.

## Speed and mechanical checks

- Signed-sheet compile plus strict dry-run completed in about 0.8 seconds and prepared 7/7 rows with zero failures.
- The baseline focused audit plus atomic application completed together in about 1.4 seconds; the apply reported 7/7 prepared and zero failures.
- The post-apply scoped audit, JSON parse gate, seven exact KWIC replays, and whitespace check completed in about 2.1 seconds.
- All seven entry JSON files parse after editing.
- All seven touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
