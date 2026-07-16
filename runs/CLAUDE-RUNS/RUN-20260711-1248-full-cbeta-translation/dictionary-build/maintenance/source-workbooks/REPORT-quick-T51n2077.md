# Source-batched attribution report: T51n2077

Scope: all 11 rows and 11 complete cases in `quick-T51n2077.md`. The prebuilt defaults were treated only as drafts. This was attribution-only remediation; no sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Defaults versus reviewed overrides

All 11 draft defaults contradicted the exact headword turn or action, so the signed exception sheet contains 11 overrides and accepts zero defaults.

| Line | Term | Draft default | Reviewed exact actor state |
|---|---|---|---|
| `0469a29` | 言下大悟 | Shoushan Xingnian | Fenyang Shanzhao |
| `0476b10` | 良久曰 | Zhimen Guangzuo | Jiufeng Qin |
| `0479b28` | 入室 | Deshan Xuanjian | Kaixian Shanxian |
| `0480b05` | 立雪 | Deshan Xuanjian | Zisheng Shengqin |
| `0491c27` | 面壁 | Luzu | Liangshan Shanji, author of the verse about Luzu |
| `0492b08` | 張三李四 | Baizhang Huaihai | Qixian Chengshi |
| `0501b25` | 嗣法 | Xuedou Chongxian | reviewed unnamed group of Dharma heirs |
| `0503b23` | 一物 | Xuedou Chongxian | reviewed unnamed monk, questioner |
| `0504a13` | 省悟 | Xuedou Chongxian | Li Linzong |
| `0627c04` | 頂門眼 | Wuxiang | Lushan Fazhen; the roster's Wuxiang is a different person |
| `0661a18` | 湊泊 | Yuanwu Keqin | Xiatang Huiyuan |

The two anonymous states are non-master actors and record all six ordered rungs. Five parallel witnesses preserve only the collective `嗣法者甚眾`, without naming the group members. The unique Fengqi exchange and parallel occurrences of its question formula leave the monk asking `如何是一物` unnamed.

## Before/after counts

Workbook-scoped rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 9 |
| Reviewed unnamed exact actors | 0 | 2 |
| Unresolved exact actors | 11 | 0 |
| Notes naming `續傳燈錄` and the exact actor state | 0 | 11 |
| Exact KWIC and full stored-range verification | not rerun | 11/11 |

Whole-source inventory for `T/T51/T51n2077.xml`:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences | 108 | 108 | 0 |
| Named occurrences | 36 | 45 | +9 |
| Structured actor exceptions | 5 | 7 | +2 |
| Bare unresolved occurrences | 67 | 56 | -11 |

Full `audit_attribution.py --json` run over the 11 touched entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 67 | 67 | 0 |
| Named occurrences | 15 | 24 | +9 |
| Reviewed unnamed occurrences | 0 | 2 | +2 |
| Unresolved actors | 52 | 41 | -11 |
| Notes missing exact speaker/actor state | 54 | 44 | -10 |
| Notes missing source title | 59 | 49 | -10 |
| Deferred non-roster exact names | 1 | 8 | +7 |
| Hard failures | 196 | 164 | -32 |

The 164 remaining audit failures are inherited, out-of-scope findings in untouched occurrences or prose in these entries.

## Workflow and timing

- Signed override sheet: `overrides-T51n2077.json`, 11 reviewed cases, 11 overrides, zero accepted defaults.
- Final successful compile: 0.14 seconds.
- Final successful dry-run: 0.33 seconds; 11 prepared rows, 11 entries, zero failures.
- Final successful atomic apply: 0.68 seconds; 11 prepared rows, 11 entries, zero failures.
- Focused source/KWIC gate: 7.26 seconds; all 11 rows found exactly once and all 11 full stored line ranges verified.
- Full-case review time was not instrumented at task receipt, so no fabricated review-duration figure is reported. The mechanical compile/dry-run/apply path took 1.15 seconds in the final successful pass.

The compiler and apply tool rejected nothing. The first downstream focused gate did catch one prose-label mismatch in the otherwise valid `嗣法` exception note: it said “unnamed group” rather than repeating the exact label “unnamed group of Dharma heirs.” The signed override was corrected, then recompiled, dry-run, and applied successfully. This shows the bulk tool materially accelerates mechanics but does not itself enforce exact actor-label repetition inside attribution-note prose.

## Artifacts and mechanical checks

- `maintenance/source-workbooks/overrides-T51n2077.json`
- `maintenance/source-workbooks/decisions-T51n2077.json`
- `maintenance/source-workbooks/decisions-T51n2077-dryrun.json`
- `maintenance/source-workbooks/decisions-T51n2077-applied.json`
- All 11 touched JSON files parse.
- The final focused gate reports nine named actors, two reviewed unnamed non-master actors, zero conflicting states, and 11/11 source-and-actor notes.
- No merge, commit, or push was performed.
