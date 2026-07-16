# Source-batched attribution report: X78n1556

Scope: the 11 workbook rows in `quick-X78n1556.md`, spanning 10 complete cases and 11 disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All ten complete cases were reviewed before the sheet was signed. The stricter compiler and apply gate required every attribution note to contain both `建中靖國續燈錄` and the exact actor's canonical name or reviewed actor label. The compiler retained five reviewed defaults and applied six explicit overrides.

Retained defaults:

- `立雪`: Huike, still called Shenguang (神光) in the sentence, is the exact actor who stands in the snow and cuts off his arm.
- `宗風`: Fengxue Yanzhao is the biographical subject who later succeeds to the house style.
- `顧視`: Xuedou Chongxian surveys the assembly before ascending the Dharma seat.
- `拾得` and `寒山`: Xuedou Chongxian is the exact speaker of the answer `寒山訪拾得`.

Overrides:

- `如何是佛`: an unnamed monk asks; Xuefeng Yicun answers, and Yunmen Wenyan hears the exchange. Nanyuan Huiyong is absent from the case.
- `恁麼則`: an unnamed monk speaks the headword turn; Xianglin Chengyuan answers.
- `如何是道`: an unnamed questioner speaks; Zhimen Guangzuo answers.
- `阿誰`: an unnamed monk asks. The actual section is headed `洪州百丈山智映寶月禪師`, so Baizhang Zhiying is the respondent; it is not Baizhang Huaihai's section.
- `象王`: Fushan Fayuan, named `遠禪師`, speaks the elephant-king line; Kaixian Shanxian hears it and awakens.
- `正令`: Longtan Yuan, under the explicit `唐州龍潭圓禪師` heading, speaks `正令已行` to Fenyang Shanzhao. He is not Longtan Chongxin.

The four reviewed exceptions are genuinely unnamed non-master questioners or speakers after all six rungs. Longtan Yuan is preserved as a source-attested exact name pending roster reconciliation; Baizhang Zhiying is likewise preserved as a source-attested contextual respondent rather than silently replaced by Baizhang Huaihai.

## Exact changed IDs

- `t_d2892b1eaae0` 立雪
- `t_e4d6ebff1bb2` 如何是佛
- `t_7c1991e9eabb` 宗風
- `t_707c9af5cb8e` 恁麼則
- `t_38695d7fdbe2` 如何是道
- `t_93e6bc6b2103` 顧視
- `t_4dd50050b279` 拾得
- `t_a7c8b47ff1a3` 寒山
- `t_43ecdacadde0` 阿誰
- `t_8482770fe735` 象王
- `t_68835cda6c3f` 正令

## Before/after counts

Workbook-scoped 11 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 7 |
| Reviewed unnamed exact actors | 0 | 4 |
| Unresolved exact actors | 11 | 0 |
| Attribution notes present | 11 | 11 |
| Notes naming `建中靖國續燈錄` | 1 | 11 |
| Notes containing exact actor name/label | 0 | 11 |
| Structured context-master links | 0 | 7 |
| Exact `zc.verify` successes | not rerun | 11/11 |

Full `audit_attribution.py --json` run over all 11 modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 70 | 70 | 0 |
| Named occurrences | 8 | 15 | +7 |
| Reviewed unnamed occurrences | 0 | 4 | +4 |
| Unresolved actors | 62 | 51 | -11 |
| Notes missing exact speaker/actor state | 64 | 53 | -11 |
| Notes missing source title | 63 | 53 | -10 |
| Context-master links | 0 | 7 | +7 |
| Vague attributors | 23 | 22 | -1 |
| Deferred non-roster exact names | 2 | 3 | +1 |
| Deferred non-roster context links | 0 | 1 | +1 |
| Hard failures | 222 | 189 | -33 |

The 189 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 76 Chinese prose strings, 66 anchored, 10 dangling.

## Speed and mechanical checks

- Compile plus stricter dry-run completed in about 1.0 second and prepared 11/11 rows with zero failures.
- The atomic apply completed in about 1.1 seconds with zero failures.
- The post-apply scoped audit, JSON parse gate, 11 exact KWIC replays, and whitespace check completed in about 3.1 seconds.
- Human review was not independently instrumented. The compact packets materially improved adjudication speed: all ten cases were bounded and readable, unlike X79's malformed giant cluster. The signed exception sheet again eliminated separate semantic edits across 11 entry files.
- All 11 entry JSON files parse after editing.
- All 11 touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
