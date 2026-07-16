# Source-batched attribution report: X70n1403

Scope: the nine workbook rows and nine complete cases in `quick-X70n1403.md`, spanning nine disjoint entry IDs. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

## Exception-sheet result

All nine complete cases were reviewed before the sheet was signed. The strict compiler and apply gate required every attribution note to contain both `天如惟則禪師語錄` and the exact actor's canonical name. Eight title-owner defaults survived exact-turn review; one required an override.

Retained Tianru Weize defaults:

- `單提`: Tianru compares singly raising `無` to leaning against Mount Sumeru.
- `普說`: the heading and opening speech belong to Tianru's general address.
- `尊宿`: Tianru describes earlier venerable masters' expedient use of a tasteless saying.
- `三寸`: Tianru delivers the death-and-cremation warning in his public address.
- `自性`: Tianru names the everyday endowment `自性天真佛` and `自己主人公`.
- `閑道人`: Tianru lists the person of leisure among names for great rest.
- `賓中主`: the verse belongs to Tianru's record.
- `一著`: Tianru addresses those who singly raise the higher matter in his precepts discourse.

Override:

- `昭昭靈靈`: the occurrence is not Tianru Weize's speech. It sits in the appended biography of Tieniu Chiding. Xueyan Zuqin, referred to as `巖`, gives Chiding the verse beginning `昭昭靈靈是什麼`; Xueyan is therefore the exact speaker and verse author. Tieniu Chiding is retained as the source-attested contextual addressee and biographical subject.

## Exact changed IDs

- `t_f59209907f3d` 單提
- `t_bb19ed0e0fab` 普說
- `t_7887dc8d449f` 尊宿
- `t_e2c55f8feca0` 三寸
- `t_dd3bf8dd507a` 自性
- `t_f04c29743e77` 閑道人
- `t_18ec645f99f7` 賓中主
- `t_faf30cf1fb87` 昭昭靈靈
- `t_549e7766dfa1` 一著

## Before/after counts

Workbook-scoped nine rows:

| Measure | Before | After |
|---|---:|---:|
| Named exact actors | 0 | 9 |
| Reviewed unnamed exact actors | 0 | 0 |
| Unresolved exact actors | 9 | 0 |
| Attribution notes present | 9 | 9 |
| Notes naming `天如惟則禪師語錄` | 0 | 9 |
| Notes containing exact actor name | 0 | 9 |
| Structured context-master links | 0 | 1 |
| Exact `zc.verify` successes | not rerun | 9/9 |

Full `audit_attribution.py --json` run over all nine modified entry files:

| Measure | Before | After | Delta |
|---|---:|---:|---:|
| Occurrences audited | 51 | 51 | 0 |
| Named occurrences | 9 | 18 | +9 |
| Unresolved actors | 40 | 31 | -9 |
| Notes missing exact speaker/actor state | 44 | 35 | -9 |
| Notes missing source title | 44 | 35 | -9 |
| Context-master links | 1 | 2 | +1 |
| Deferred non-roster context links | 0 | 1 | +1 |
| Hard failures | 152 | 125 | -27 |

The added deferred context name is source-attested Tieniu Chiding. The 125 remaining audit failures belong to untouched, out-of-scope occurrences and prose in these entries. Quote-anchor counters are unchanged: 60 Chinese prose strings, 46 anchored, 14 dangling.

## Speed and mechanical checks

- Signed-sheet compile plus strict dry-run completed in about 0.9 seconds and prepared 9/9 rows with zero failures.
- The baseline focused audit plus atomic application completed together in about 1.9 seconds; the apply reported 9/9 prepared and zero failures.
- The post-apply scoped audit, JSON parse gate, nine exact KWIC replays, and whitespace check completed in about 3.6 seconds.
- Human review was not independently instrumented. The compact packets kept the review bounded despite one long public address and one long appended biography.
- All nine entry JSON files parse after editing.
- All nine touched KWICs pass exact `zc.verify` with their stored source and starting line anchors.
- `git diff --check` reports no whitespace errors for the touched files and review artifacts.
- No merge, commit, or push was performed.
