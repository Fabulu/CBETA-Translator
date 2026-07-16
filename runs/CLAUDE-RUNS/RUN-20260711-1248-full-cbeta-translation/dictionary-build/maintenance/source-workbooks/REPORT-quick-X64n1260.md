# Source-batched attribution report: X64n1260

Scope: the five workbook rows and five complete cases in `quick-X64n1260.md`, spanning five disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All five complete cases were reviewed before the sheet was signed. Exact-turn review retained all five defaults; no override was required.

- `普說`: the preface byline `住黃梅四祖雙峰山東吳嗣祖沙門戒顯撰` names Jiexian as the author of the sentence classifying general addresses.
- `盡大地`: the unheaded imperial-command discourse continues Yuanwu Keqin's section. The parallel passage appears in Yuanwu's own `圓悟佛果禪師語錄` (`T47n1997`, 0716c11 onward), independently confirming Yuanwu as speaker.
- `赤肉團上`: the case begins `南院顒禪師。上堂`, and the stored headword is in Nanyuan Huiyong's opening statement rather than the following monk's question.
- `劍刃`: the paragraph begins `太陽玄禪師，上堂`; Dayang Jingxuan is the exact speaker of the sword-edge and thin-ice statement.
- `現成公案`: the previous named paragraph opens `德山密禪師上堂`, and the following unheaded `上堂` paragraphs remain within Deshan Mi's section until the next named master.

## Exact changed IDs

- `t_bb19ed0e0fab` 普說
- `t_9199b9a31645` 盡大地
- `t_bbee6625a4d5` 赤肉團上
- `t_f758d1e27978` 劍刃
- `t_408abe2e38ca` 現成公案

## Before/after counts

Workbook-scoped five rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 5 |
| Reviewed unnamed exact actors | 0 | 0 |
| Unresolved exact actors | 5 | 0 |
| Attribution notes present | 5 | 5 |
| Notes naming `列祖提綱錄` | 0 | 5 |
| Notes containing exact actor name | 0 | 5 |
| Exact `zc.verify` successes | not rerun | 5/5 |

Full `audit_attribution.py --json` run over all five modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 28 | 28 | 0 |
| Named occurrences | 9 | 14 | +5 |
| Unresolved actors | 18 | 13 | -5 |
| Missing exact speaker/actor state in notes | 21 | 16 | -5 |
| Notes missing source title | 22 | 17 | -5 |
| Hard failures | 72 | 57 | -15 |

The 57 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 21 Chinese prose strings, 18 anchored, 3 dangling.

## Speed and mechanical checks

- Signed-sheet compilation completed in 0.12 seconds; strict dry-run completed in 0.31 seconds and prepared 5/5 rows with zero failures.
- Atomic application completed in 0.36 seconds and reported 5/5 prepared with zero failures.
- All five entry JSON files parse after editing.
- All five touched KWICs pass exact `zc.verify` with their stored source and starting-line anchors.
- All five touched notes contain both `列祖提綱錄` and the exact actor's name.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
