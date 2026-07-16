# Cohort C evidence repair — 平常心 (2026-07-13)

## Disposition

- Entry: 平常心 (`t_4ccf8aed47d3`).
- Repaired `entry.v2.json` and replaced the contradictory legacy `WORK.md` with a current evidence ledger.
- Used `maintenance/next-wave-prep/case-workbook-c-next3.{md,json}` only as retrieval support; every exact turn and every stored line was rechecked against the source corpus.
- No merge, commit, or push was run.

## Lexicographic decision

- Preferred target remains **ordinary mind**. The sentence “ordinary mind is the Way” is evidence about the headword, not a gloss of it.
- One corpus-wide sense survives item 8. Mazu's predicates, Nanquan's approach/cognition exchange, Changsha's bodily and temperature answers, Zhaozhou's animals, Xianglin's greetings, and Zhenjing's rebuke are different statements concerning one referent.
- Zhenjing's negative appraisal does not create a hostile second sense; stance is not polysemy.
- The opening now states the corpus-earned restriction before the quotations: ordinary mind is not presented as a special state to approach or as a stock formula that settles the matter.

## Evidence changes

| Metric | Before | After |
|---|---:|---:|
| exact-headword occurrences | 6 | 7 |
| supporting family occurrences | 0 | 1 |
| total occurrences | 6 | 8 |
| named occurrences | 5 | 8 |
| dangling Chinese strings | 1 | 0 |
| vague/missing source attributions | 6 source + 6 speaker defects | 0 |
| senses | 1 | 1 |

Added:

1. Zhenjing Kewen's exact criticism of reporting `平常心是道` as the final rule, a unique headword deployment rather than quota padding.
2. Sikong Benjing's `即心是佛` answer as `EvidenceRole: family`, anchoring the inherited related-term phrase without allowing it to buy headword depth.

## Exact-turn dispositions

- Mazu definition: Mazu Daoyi's assembly address; the preceding Baizhang exchange is a closed earlier unit.
- Two Nanquan witnesses: Zhaozhou Congshen asks, Nanquan Puyuan owns the exact `平常心` answer; the later record owners only raise or quote the case.
- Changsha answer: an unnamed monk asks; Changsha Jingcen owns both answers.
- Zhaozhou answer: an unnamed monk asks; Zhaozhou Congshen answers.
- Xianglin answer: an unnamed monk asks; Xianglin Chengyuan answers.
- Zhenjing criticism: Zhenjing Kewen's own assembly address.
- Family anchor: Yang Guangting asks; Sikong Benjing answers.

No occurrence assigns an anonymous questioner's wording to the respondent, and no embedded old case is attributed to the containing book's resident master.

## Depth and family audit

- Corpus count: 309 hits / 120 files; mechanical floor is six exact headword anchors.
- Final exact-headword depth is seven across seven source files, with each anchor representing a distinct high-value fact.
- The duplicate Nanquan witness is retained because it independently establishes case circulation and exercises the embedded-case attribution veto.
- Repeated logion and ordinary-action witnesses were excluded after the definition, approach/cognition, bodily-response, animal, greeting, and rebuke classes were represented.
- Family entries `平常心是道`, `道`, `即心是佛`, and `無造作` remain compatible and separate; none substitutes for the headword definition.

## Searchability

Added approved aliases for `normal mind`, `usual mind`, `ordinary state of mind`, `everyday state of mind`, and `plain mind`, alongside the preferred and alternate ordinary/everyday targets.

## Mechanical results

- `zc.verify`: **8/8 pass**, exact `FromLb` and `ToLb`, with `PYTHONIOENCODING=utf-8`.
- Source equality: **1/1 sense passes**.
- `audit_attribution.py`: **0 hard failures**; 8/8 named, 9/9 Chinese prose strings anchored, one supporting occurrence correctly classified, one non-roster spelling deferred (`Sikong Benjing`).
- `audit_depth_sense.py`: **0 hard failures, 0 review flags**.
- `audit_public_feedback.py`: **1/1 pass, 0 flags**.
- Forbidden reader labels: none.

## Merge status

Not merged, per assignment.
