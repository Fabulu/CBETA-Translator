# Source-batched attribution report: C077n1710

Scope: the 30 workbook rows in `quick-C077n1710.md`, spanning 28 complete cases and 27 disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exact-turn adjudication

Every complete case and exact turn was read before assignment. Header and title candidates were treated only as leads, never as automatic actors.

- 23 rows now name their exact master actor.
- Seven rows are reviewed unnamed non-master actors after all six rungs were exhausted: the two anonymous `如何是佛法大意` questioners, an unnamed head monk in `方丈`, an unnamed lecturer in `宗門`, and anonymous monks in `向上事`, `衲僧`, and `罔措`.
- `如人飲水` is spoken by Huiming, whom the source calls Ming Shangzuo (明上座), inside Huangbo Xiyun's later narration of Huineng's exchange. Huiming is preserved as a source-attested non-roster exact name rather than hidden behind an anonymous state.
- `赤肉團上` was incorrectly described as belonging to Nanyuan Huiyong. The source header places it in Muzhou Daoming's section: Muzhou first gives the hall statement, and an unnamed monk immediately repeats it in a question.
- `平常心是道` is Nanquan Puyuan's answer to Zhaozhou Congshen inside a later raising by Foyan Qingyuan; it is not assigned to the record owner.
- `合頭語` is Chuanzi Decheng's saying, identified through the parallel Boatman case; Yunmen Wenyan is the later raiser.
- `佛手` is spoken by Zhenjing Kewen in `雲庵真淨禪師語錄一`; Deshan Xuanjian and Huanglong Huinan are people discussed, not substitute speakers.
- `應機` at `0651b14` is a narrative appraisal whose exact subject is Linji Yixuan. `罔措` at `0659b01` similarly names the unnamed monk as the exact subject, with Muzhou as contextual actor.

## Exact changed IDs

- `t_ae026a775df5` 如人飲水
- `t_57ef1bbc3a81` 山河大地
- `t_2229af16905a` 威音王
- `t_c968268a64d1` 心印
- `t_bc7bbb4299f1` 如何是佛法大意
- `t_28fac5e98308` 問話
- `t_4d4cbd834b80` 速道
- `t_757827b8d4cb` 喪身失命
- `t_7cddddb76d37` 任運
- `t_21a3463bc0db` 隨處
- `t_db4979f3cddc` 應機
- `t_becc0a1ea8cb` 方丈
- `t_3972185a2e25` 宗門
- `t_b437e79a4646` 對機
- `t_e84753568cda` 向上事
- `t_acccac1051a4` 衲僧
- `t_7b7ca6f375b5` 罔措
- `t_bbee6625a4d5` 赤肉團上
- `t_4dd50050b279` 拾得
- `t_a7c8b47ff1a3` 寒山
- `t_6214dc704b24` 莫錯會
- `t_936118ea496c` 喫粥
- `t_ada407625f42` 珍重
- `t_b2f05c3e4b7d` 合頭語
- `t_7de4a77a97bd` 看話
- `t_9a5dc768cbc5` 平常心是道
- `t_bf467ac18ec0` 佛手

## Before/after counts

Workbook-scoped 30 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 23 |
| Reviewed unnamed exact actors | 0 | 7 |
| Unresolved exact actors | 30 | 0 |
| Attribution notes present | 30 | 30 |
| Notes naming `古尊宿語錄` | 2 | 30 |
| Structured context-master links | 0 | 16 |
| Exact `zc.verify` successes | not rerun | 30/30 |

Full `audit_attribution.py --json` run over all 27 modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 166 | 166 | 0 |
| Named occurrences | 25 | 48 | +23 |
| Reviewed unnamed occurrences | 0 | 7 | +7 |
| Unresolved actors | 141 | 111 | -30 |
| Attribution notes | 166 | 166 | 0 |
| Notes missing exact speaker/actor state | 155 | 125 | -30 |
| Notes missing source title | 152 | 124 | -28 |
| Context-master links | 0 | 16 | +16 |
| Vague attributors | 46 | 39 | -7 |
| Deferred non-roster exact names | 1 | 2 | +1 |
| Hard failures | 551 | 456 | -95 |

The one added deferred non-roster exact name is source-attested Huiming. The 456 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged because this batch did not alter prose evidence or KWICs: 232 Chinese prose strings, 175 anchored, 57 dangling.

## Mechanical checks

- The decision patcher dry-run prepared all 30 rows with zero failures before writing.
- The applied pass atomically updated all 27 entry files and reported zero failures.
- All 27 JSON files parse after editing.
- All 30 touched KWICs pass exact `zc.verify` with their stored sources and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files.
- No merge, commit, or push was performed.
