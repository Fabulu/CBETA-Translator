# Source-batched attribution report: T48n2001

Scope: the 23 workbook rows in `quick-T48n2001.md`, spanning 18 disjoint entries. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Turn adjudication

Every complete case and exact headword-bearing turn was read before assignment. The book title was treated only as a candidate.

- 17 rows are Hongzhi Zhengjue's own exact turns.
- `沒蹤跡` at `0006a13` is spoken by Chuanzi Decheng inside the Boatman case raised by Hongzhi.
- `心地` at `0058c22` is spoken by Huineng in an explicit quotation raised by Hongzhi.
- `無住` at `0025a20` is asked by one unnamed monk; Fayan Wenyi responds and Hongzhi later raises and verses the case.
- `萬法歸一` at `0040b03` and `一歸何處` at `0040b06` each preserve two parallel questions by two unnamed monks; Zhaozhou Congshen and Wenshu respond, and Hongzhi raises the cases side by side.
- `淨裸裸` at `0069b18` is asked by an unnamed monk; Hongzhi responds.

The four anonymous-questioner rows use the strict `reviewed-unnamed` branch with all six ordered rungs recorded. Named respondents, record owners, raisers, and commentators remain separate context, never substitutes for the exact actor.

## Exact changed IDs

- `t_9ec945311be0` 還會麼
- `t_a9f422b3b249` 生緣
- `t_20cc4b0bc96e` 光影門頭
- `t_9dfa307c0458` 沒蹤跡
- `t_7cddddb76d37` 任運
- `t_c3a7862b9971` 自在
- `t_2d92f15fa0ab` 雲水
- `t_fb331b159983` 死蛇
- `t_f6532de212f3` 圓陀陀
- `t_395ae8fd7f32` 無住
- `t_e96268628f2c` 萬法歸一
- `t_9a7a00ea0cd1` 一歸何處
- `t_ab715aa474d5` 一日不作一日不食
- `t_e7f672904614` 心地
- `t_6c1f113fbdcd` 淨裸裸
- `t_b437e79a4646` 對機
- `t_560356022866` 孤明
- `t_fc585583b815` 機用

## Before/after counts

Workbook-scoped 23 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 19 |
| Reviewed unnamed exact actors | 0 | 4 |
| Unresolved exact actors | 23 | 0 |
| Attribution notes present | 23 | 23 |
| Notes naming the exact actor state | 0 | 23 |
| Notes naming `宏智禪師廣錄` | 1 | 23 |
| Structured context-master links | 0 | 10 |

Full `audit_attribution.py --json` run over all 18 modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 102 | 102 | 0 |
| Named occurrences | 7 | 26 | +19 |
| Reviewed unnamed occurrences | 0 | 4 | +4 |
| Unresolved actors | 95 | 72 | -23 |
| Notes missing exact speaker/actor state | 99 | 76 | -23 |
| Notes missing source title | 96 | 74 | -22 |
| Context-master links | 0 | 10 | +10 |
| Hard failures | 332 | 264 | -68 |

The 264 remaining failures belong to untouched, out-of-scope occurrences and prose in these entries. They are inherited defects, not failures in any of the 23 assigned source rows. The quote-anchor counters are unchanged because this batch did not alter prose evidence or KWICs: 161 Chinese prose strings, 132 anchored, 29 dangling.

## Mechanical checks

- All 18 JSON files parse after editing.
- `git diff --check` reports no whitespace errors for the 18 files.
- The exact 23-row scoped audit returns 19 named, 4 reviewed unnamed, 0 unresolved, 23/23 source-named notes, and 23/23 exact-actor-state notes.
- No merge, commit, or push was performed.
