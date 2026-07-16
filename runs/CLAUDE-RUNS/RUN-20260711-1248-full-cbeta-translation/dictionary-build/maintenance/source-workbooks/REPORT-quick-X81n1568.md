# Source-batched attribution report: X81n1568

Scope: the 14 workbook rows in `quick-X81n1568.md`, spanning 13 complete cases and 14 disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exact-turn adjudication

Every complete case was read before assignment. The anthology title and header candidates were not treated as actors without exact-turn confirmation.

- 13 rows now name their exact master actor.
- `請益` at `0036a21` is the sole reviewed exception: an unnamed attendant asks Fengxue Yanzhao about Shoushan Xingnian. All six attribution rungs were exhausted; the two named masters remain contextual people rather than substitutes for the attendant.
- `轉身` is Mayu Baoche's action inside Linji Yixuan's section; Linji's following blow is context.
- `一刀兩段` belongs to Longtan Zhiyuan (唐州龍潭智圓), not Longtan Chongxin.
- `契悟` belongs to Dongshan Daoquan under the same-line `洞山道全禪師` header, not the preceding Beiyuan Tong section and not Dongshan Liangjie, whose answer precedes Daoquan's awakening.
- `出身處` belongs to Lingyun Baoyin under `越州雲門山靈運寶印禪師`, not Yunmen Wenyan.
- `立雪` belongs to Danxia Zichun, not Danxia Tianran.
- Both shared `0114a06` rows belong to Deshan Yuanming, not Deshan Xuanjian.
- In the passive biographical line `謁汾陽、葉縣，皆蒙印可`, Fushan Fayuan is the grammatical subject and exact recipient; Fenyang Shanzhao and Shexian Guisheng are separately named as approvers.

## Exact changed IDs

- `t_6293dead3bb2` 轉身
- `t_b191c4fa2e9f` 請益
- `t_b96051d06349` 室中
- `t_74a27239e6c7` 一刀兩段
- `t_fb23e0284d73` 印可
- `t_db4979f3cddc` 應機
- `t_b88b6a8a5659` 契悟
- `t_447ad9648add` 出身處
- `t_d2892b1eaae0` 立雪
- `t_19f9e99d5304` 莊周
- `t_6ba271127127` 破草鞋
- `t_a326343ab7c3` 日日是好日
- `t_207efae5f6bd` 死句
- `t_df3e128ab4c1` 竪拂

## Before/after counts

Workbook-scoped 14 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 13 |
| Reviewed unnamed exact actors | 0 | 1 |
| Unresolved exact actors | 14 | 0 |
| Attribution notes present | 13 | 14 |
| Notes naming the exact actor state | 0 | 14 |
| Notes naming `五燈嚴統(第10卷-第25卷)` | 0 | 14 |
| Structured context-master links | 0 | 7 |
| Exact `zc.verify` successes | not rerun | 14/14 |

Full `audit_attribution.py --json` run over all 14 modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 84 | 84 | 0 |
| Named occurrences | 12 | 25 | +13 |
| Reviewed unnamed occurrences | 0 | 1 | +1 |
| Unresolved actors | 72 | 58 | -14 |
| Missing attribution notes | 6 | 5 | -1 |
| Notes missing exact speaker/actor state | 75 | 62 | -13 |
| Notes missing source title | 78 | 65 | -13 |
| Context-master links | 0 | 7 | +7 |
| Deferred non-roster exact names | 2 | 5 | +3 |
| Hard failures | 266 | 225 | -41 |

The three added deferred non-roster exact names are source-attested Longtan Zhiyuan, Dongshan Daoquan, and Lingyun Baoyin. They are written rather than hidden behind anonymous-master states; roster expansion remains a separate workstream.

The 225 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged because this batch did not alter prose evidence or KWICs: 128 Chinese prose strings, 115 anchored, 13 dangling.

## Mechanical checks

- All 14 JSON files parse after editing.
- All 14 touched KWICs pass exact `zc.verify` with the stored source and line ranges.
- `git diff --check` reports no whitespace errors for the 14 files.
- No merge, commit, or push was performed.
