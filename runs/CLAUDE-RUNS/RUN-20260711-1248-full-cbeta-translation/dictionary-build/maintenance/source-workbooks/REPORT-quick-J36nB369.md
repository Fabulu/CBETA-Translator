# Source-batched attribution report: J36nB369

Scope: the five workbook rows and five complete cases in `quick-J36nB369.md`, spanning four disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All five complete cases were reviewed before the sheet was signed. The strict compiler and apply gate required every attribution note to contain both `蔗菴範禪師語錄` and the exact actor's canonical or source-attested name. Two quoted-master defaults survived exact-turn review; all three Yunmen header defaults required overrides.

Retained defaults:

- `綱宗`: Yantou Quanhuo is explicitly quoted saying `但識綱宗，本無實法` in the record's preface.
- `劍刃`: Guishan Lingyou is explicitly marked `溈山曰` before `寂子用劍刃上事`.

Overrides:

- `迷悟` at `0906b18`: Zhean Jingfan is the exact speaker saying that treating confusion and awakening as absent and dust as fallen away remains halfway.
- `迷悟` at `0910b22`: Zhean Jingfan delivers the winter-solstice address pairing confusion and awakening as not different and motion and stillness as one source.
- `立定腳跟`: Zhean Jingfan warns that merely planting one's feet is a one-sided understanding unable to adapt.

In these three cases, `雲門` is the monastery/place context in Zhean's record, not Yunmen Wenyan speaking. Zhean Jingfan is source-attested but remains outside the current roster; the exact name is preserved rather than replaced or nulled.

## Exact changed IDs

- `t_80ea075a6c5d` 綱宗
- `t_4d83c364fefd` 迷悟
- `t_b1487d8fc8f9` 立定腳跟
- `t_f758d1e27978` 劍刃

## Before/after counts

Workbook-scoped five rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 5 |
| Reviewed unnamed exact actors | 0 | 0 |
| Unresolved exact actors | 5 | 0 |
| Attribution notes present | 4 | 5 |
| Notes naming `蔗菴範禪師語錄` | 1 | 5 |
| Notes containing exact actor name | 0 | 5 |
| Exact `zc.verify` successes | not rerun | 5/5 |

Full `audit_attribution.py --json` run over all four modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 19 | 19 | 0 |
| Named occurrences | 2 | 7 | +5 |
| Unresolved actors | 16 | 11 | -5 |
| Missing attribution notes | 5 | 4 | -1 |
| Notes missing exact speaker/actor state | 13 | 9 | -4 |
| Notes missing source title | 8 | 5 | -3 |
| Deferred non-roster exact names | 1 | 4 | +3 |
| Hard failures | 56 | 43 | -13 |

The 43 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 41 Chinese prose strings, 31 anchored, 10 dangling.

## Speed and mechanical checks

- Signed-sheet compile plus strict dry-run completed in about 0.4 seconds and prepared 5/5 rows with zero failures.
- The baseline focused audit plus atomic application completed together in about 0.6 seconds; the apply reported 5/5 prepared and zero failures.
- The post-apply scoped audit, JSON parse gate, five exact KWIC replays, and whitespace check completed in about 0.6 seconds.
- All four entry JSON files parse after editing.
- All five touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
