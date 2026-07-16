# `一句` (`t_4cc95950b59a`) risk remediation — 2026-07-13

## Outcome

Revised, not split. `一句` denotes one counted unit of wording: a sentence, phrase, clause, line, or saying. The corpus gives that ordinary unit conspicuous Chan deployments, but no second referent was found. English search coverage now includes the ordinary synonyms rather than requiring “phrase.”

## Before / after

| Measure | Before | After |
|---|---:|---:|
| Occurrences | 12 | 16 |
| Named actors | 2 | 15 |
| Reviewed-unnamed actors | 0 | 1 |
| Source-and-speaker attribution notes | 0 complete | 16 complete |
| Chinese prose strings anchored | 7/13 | 13/13 |
| Senses | 1 | 1 |

`Fada` is an exact named actor but is not yet on the current roster; the data preserves the name as a deferred roster reconciliation rather than erasing him. The sole unnamed actor is the monk who questions Linji about the first, second, and third phrases. All six attribution rungs were exhausted, while Linji is retained as the named respondent in `ContextMasters`.

## Added anchors

- expanded Linji's case through all three questions (`T47n1985 0497a15–19`);
- Yuanwu Keqin on `末後一句` (`X64n1260 0018c01`);
- Chushi Fanqi on `向上一句` (`X64n1260 0134c10–11`);
- Yunmen Wenyan on `一句下` (`X64n1260 0075b16–17`);
- Yantou Quanhuo on `末後句` (`J25nB163 0229b08–10`).

All six useful dangling prose strings are now supported. None was deleted to make the audit pass.

## Sense verdict

The enriched evidence was retested for ordinary wording versus a genuinely different title, person, work, object, or specialized referent. `一句集` and `一句書` returned zero; all harvested deployments remained counted wording units. Qualified forms such as first phrase, final phrase, upward phrase, out-of-pattern phrase, and conforming phrase are family members. They may merit their own longer-headword entries, but they do not make the bare word polysemous. Different English realizations and grammatical environments are not separate senses.

## Reproducible detail

The full actor ledger, family counts, inference/falsification ledger, and quote-closure mapping are appended to `terms/t_4cc95950b59a/WORK.md`. No merged artifact was regenerated, and no commit or push was made.

## Final gates

- JSON parse: pass.
- Exact KWIC verification: 16/16, zero failures.
- Attribution: zero hard failures; 15 named actors, one six-rung reviewed-unnamed monk, one named non-roster deferral, 16/16 source-and-speaker notes, 13/13 prose strings anchored.
- Public-feedback gate: pass, zero flags.
- Depth/sense gate: hard pass. Its sole review notice is the expected broad-concordance/single-sense prompt; the documented title/referent/family retest adjudicates that prompt rather than ignoring it.
- Cohort gate: hard pass; zero exact-KWIC failures and no forbidden English.

Machine reports: `maintenance/audit-public-feedback-risk-yiju-final.json` and `maintenance/cohort-gate-risk-yiju-final.json`.
