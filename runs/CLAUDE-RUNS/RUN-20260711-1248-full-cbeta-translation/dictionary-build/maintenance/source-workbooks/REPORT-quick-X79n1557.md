# Source-batched attribution report: X79n1557

Scope: the 20 workbook rows in `quick-X79n1557.md`, spanning 17 complete cases and 16 disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All 17 complete cases were reviewed before the sheet was signed. The compiler retained 8 reviewed defaults and applied 12 explicit overrides. Defaults were treated as drafts, not approvals.

Retained defaults:

- `無相` at `0001c18`: Wuxiang, named in the lineage notice.
- `歸方丈` at `0039c17`: Nanquan Puyuan performs the return to the abbot's quarters.
- `一隻眼` at `0043c23`: Xuedou Chongxian supplies the explicit second eye statement in his comment on Jiashan Shanhui's first statement.
- `如何是祖師西來意` at `0027c06`: Nanyue Huairang is a named co-questioner with Tanran.
- `提起` at `0039b05`: Nanquan Puyuan lifts the cat.
- `只如` at `0040a03`: Nanquan Puyuan speaks the comparison in his answer.
- `隨處` at `0047c18`: Dazhu Huihai speaks the Dharma-body instruction.
- `擬心即差` at `0118a05`: Huanglong Huinan delivers the hall instruction.

Overrides:

- `較些子`: Fenzhou Wuye, not the following inline Baizhang turn.
- `盡大地`: Xuefeng Yicun's quoted habitual saying, not Xuedou's later comment.
- `威音王`: Xuance (玄策/策云), not Yongjia Xuanjue.
- `無語`: Deng Yinfeng is the exact named subject, not his teacher Mazu Daoyi.
- `舉問`: Dongshan Liangjie raises the question to Yunju Daoying; Nanquan is absent from the case.
- `速道`: Baizhang Huaihai gives the opening command; Guishan Lingyou responds afterward.
- `如何是祖師西來意` at `0044a20`: an unnamed monk asks; Damei Fachang responds.
- `死中得活` at `0045c14`: Piyun (披雲云), not Mayu Baoche.
- `一隻眼` at `0058c19`: Xuefeng Yicun's saying quoted in Zhaozhou Congshen's exchange.
- `提起` at `0068b01`: an unnamed newcomer lifts the sitting cloth; Huangbo Xiyun speaks afterward.
- `師子吼`: Guishan Lingyou appraises Yangshan Huiji; Yangshan is the addressee and person appraised.
- `死中得活` at `0181a20`: Zhaozhou Congshen asks the question; Touzi Datong answers.

The two reviewed exceptions are genuinely unnamed non-master actors after all six rungs: Damei's anonymous questioning monk and Huangbo's anonymous newcomer. Xuance, Deng Yinfeng, and Piyun are preserved as source-attested exact names pending roster reconciliation.

## Exact changed IDs

- `t_62bc43101d57` 無相
- `t_15eac1a3b037` 歸方丈
- `t_ccae22e8375d` 一隻眼
- `t_f3488daf27fd` 較些子
- `t_9199b9a31645` 盡大地
- `t_37771a869b4f` 如何是祖師西來意
- `t_2229af16905a` 威音王
- `t_63ca7d059ee8` 無語
- `t_18b083a026ba` 提起
- `t_af92172da506` 只如
- `t_1793c3514a69` 舉問
- `t_4d4cbd834b80` 速道
- `t_592227b212c1` 死中得活
- `t_21a3463bc0db` 隨處
- `t_eedf4100b3d7` 師子吼
- `t_cf1445e57ef2` 擬心即差

## Before/after counts

Workbook-scoped 20 rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 18 |
| Reviewed unnamed exact actors | 0 | 2 |
| Unresolved exact actors | 20 | 0 |
| Attribution notes present | 20 | 20 |
| Notes naming `聯燈會要` | 2 | 20 |
| Structured context-master links | 0 | 13 |
| Exact `zc.verify` successes | not rerun | 20/20 |

Full `audit_attribution.py --json` run over all 16 modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 94 | 94 | 0 |
| Named occurrences | 10 | 28 | +18 |
| Reviewed unnamed occurrences | 0 | 2 | +2 |
| Unresolved actors | 84 | 64 | -20 |
| Notes missing exact speaker/actor state | 87 | 67 | -20 |
| Notes missing source title | 85 | 67 | -18 |
| Context-master links | 2 | 15 | +13 |
| Deferred non-roster exact names | 0 | 3 | +3 |
| Hard failures | 295 | 237 | -58 |

The 237 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 104 Chinese prose strings, 97 anchored, 7 dangling.

## Speed and mechanical checks

- Compile plus dry-run completed in about 1.2 seconds and prepared 20/20 rows with zero failures.
- The atomic apply completed in about 1.2 seconds with zero failures.
- The full post-apply scoped audit, JSON parse gate, 20 exact KWIC replays, and whitespace check completed in about 6 seconds.
- Human complete-case adjudication was not independently instrumented. The exception sheet materially reduced the mechanical write phase: one signed review artifact replaced 16 separate semantic entry edits, while the required source-reading time remained unchanged.
- All 16 entry JSON files parse after editing.
- All 20 touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
