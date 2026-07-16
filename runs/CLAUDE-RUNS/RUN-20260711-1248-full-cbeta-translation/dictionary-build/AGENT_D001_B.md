# AGENT_D001_B — depth repair report

Scope: `喝`, `僧問`, `且道`, `棒`, `一句`. No STATUS, manifest, registration, or merged termbase was changed.

## Totals

| Term | Corpus hits | Before | After | Sources | Sense correction |
|---|---:|---:|---:|---:|---|
| 喝 | 35,032 | 6 | 12 | 8 | merged two paraphrasing shout senses; added appraisal and anti-random-shouting deployments; no allowlisted drink sense |
| 僧問 | 27,950 | 4 | 11 | 9 | retained one event formula; heading/frame grammar is not a second thing |
| 且道 | 18,624 | 6 | 11 | 7 | retained one imperative; expanded continuation/deployment coverage |
| 棒 | 18,109 | 8 | 13 | 9 | preserved object-vs-blow split; 6 + 7 anchors |
| 一句 | 15,708 | 6 | 12 | 9 | retained one phrase unit; removed two non-headword KWICs |

Total occurrences: **30 → 59**. All five exceed the 10-anchor mechanical floor because the added witnesses anchor distinct lexical or deployment facts. The targeted anti-quota repair moved 喝 from 10 to 12 with an explicit appraisal and an explicit prohibition of blind/random shouting.

## Corrections and family findings

- 喝: “a shout” and “to shout” were a noun/verb paraphrase pair, not two things. `喝道` anchors ordinary calling out; `喝采` and `喝罵` remain compounds. No allowlisted drinking use was found. New evidence shows the interview explicitly appraising whether a shout is good and instruction forbidding blind/random shouting; it strengthens the single vocal-act definition without adding a sense.
- 僧問: case-opening and narrative/biographical frames preserve “a monk asked”; they do not turn the exact phrase into a genre noun.
- 且道: added identity, cause, comparison, existence, fault, outcome, response, resemblance, forced-alternative, ceremonial, and direct-dialogue forms.
- 棒: object and countable blow remain incontrovertibly different. `棒喝` is compatible. There is no current `竹篦` entry; `打一竹篦` is a future item-8 audit flag.
- 一句: first/last/upward/out-of-pattern/conforming positions remain one speech unit. Two old occurrences that did not contain exact `一句` were removed.

## Mechanical verification

- All five JSON files parse.
- Every retained occurrence is allowlisted and exact under `zc.verify` with `PYTHONIOENCODING=utf-8`.
- Recorded FromLb/ToLb values match the verifier.
- Every sense has occurrence support; no STATUS or merge operation was performed.
