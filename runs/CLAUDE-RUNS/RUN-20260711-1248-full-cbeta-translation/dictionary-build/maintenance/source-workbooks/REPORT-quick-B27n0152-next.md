# Source-batched attribution report: B27n0152 promoted remainder

Scope: the six workbook rows in `quick-B27n0152-next.md`, spanning five complete cases and five disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All five complete cases were reviewed before the sheet was signed. The strict compiler and apply gate required every attribution note to contain both `普濟玉琳國師語錄` and the exact actor's canonical name or reviewed actor label. Three defaults survived exact-turn review; three required overrides.

Retained defaults:

- `一念不生` at `0554b05`: Xuedou Chongxian asks Zhimen Guangzuo why no thought arising still has fault.
- `本來無一物`: Huineng is the explicitly raised speaker of the saying.
- `本自具足`: the biographical notice records Huineng presenting the five `何期自性` statements to the Fifth Patriarch.

Overrides:

- `拈古`: Zhang Shangying, called Zhang Wujin (張無盡), is the exact first-person speaker of `余閱雪竇拈古`. He says that he read Xuedou Chongxian's comments on old cases; Xuedou is the author discussed, and Yulin Tongxiu is the later raiser.
- `一念不生` at `0554b04`: an unnamed monk asks Yunmen Wenyan whether not producing a single thought still has fault. Yunmen answers `須彌山`. The graph `門` abbreviates Yunmen; it does not name Wumen Huikai.
- `無相`: Yulin explicitly raises Huineng's formulation `無相為體，無念為本，無住為宗`. Huineng is the quoted speaker; `無相` is the headword inside the formulation, not Master Wuxiang.

The sole reviewed exception is a genuinely unnamed non-master questioner after all six rungs.

## Exact changed IDs

- `t_66792ea088de` 拈古
- `t_d065698c14a8` 一念不生
- `t_62bc43101d57` 無相
- `t_93ab42fecdca` 本來無一物
- `t_a2612eb1f803` 本自具足

## Before/after counts

Workbook-scoped six rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 5 |
| Reviewed unnamed exact actors | 0 | 1 |
| Unresolved exact actors | 6 | 0 |
| Attribution notes present | 6 | 6 |
| Notes naming `普濟玉琳國師語錄` | 1 | 6 |
| Notes containing exact actor name/label | 0 | 6 |
| Structured context-master links | 0 | 5 |
| Exact `zc.verify` successes | not rerun | 6/6 |

Full `audit_attribution.py --json` run over all five modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 29 | 29 | 0 |
| Named occurrences | 10 | 15 | +5 |
| Reviewed unnamed occurrences | 0 | 1 | +1 |
| Unresolved actors | 19 | 13 | -6 |
| Notes missing exact speaker/actor state | 25 | 19 | -6 |
| Notes missing source title | 20 | 15 | -5 |
| Context-master links | 1 | 6 | +5 |
| Vague attributors | 10 | 9 | -1 |
| Hard failures | 111 | 93 | -18 |

The 93 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 93 Chinese prose strings, 56 anchored, 37 dangling.

## Speed and mechanical checks

- Signed-sheet compile plus strict dry-run completed in about 0.4 seconds and prepared 6/6 rows with zero failures.
- The baseline focused audit plus atomic application completed together in about 2.0 seconds; the apply reported 6/6 prepared and zero failures.
- The post-apply scoped audit, JSON parse gate, six exact KWIC replays, and whitespace check completed in about 2.2 seconds.
- All five entry JSON files parse after editing.
- All six touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
