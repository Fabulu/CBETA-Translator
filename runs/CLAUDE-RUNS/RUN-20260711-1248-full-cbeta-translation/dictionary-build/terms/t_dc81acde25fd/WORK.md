# 那畔 — research and depth audit

## Concordance

- Refreshed `zc.count("那畔")`: 866 hits in 233 allowlisted storage files.
- Refreshed collocations: `威音那畔` 470/173; `那畔事` 41/29; `那畔人` 10/7; `那畔一句` 4/3; `如何是那畔` 5/5; `那畔者` 4/4.

## #0f inventory

- Definition formulas searched: `那畔者`, `所謂那畔`, `謂之那畔`, `名為那畔`, `喚作那畔`, `何謂那畔`, `如何是那畔`. No Chinese Chan equation-style definition was found.
- Exclusion: `T/T48/T48n2019A.xml` has `名曰威音那畔人`, but the text is Korean-authored and was excluded under the Chinese-Chan-only rule even though the allowlist index returns it.
- Deployment shapes retained: paired test-question (`那畔事`/`者邊事`), news from that side, road on that side in a funeral address, and two differing deployments of the fixed `威音那畔` expansion.
- Text-drawn contrast retained: `那畔` against `遮畔`/`者邊`. Morphological families for matter, person, saying, road, and news are summarized with counts where available.
- Omission audit: all unique Chinese high-value findings are in the entry; repetitive late `威音那畔` verse lines were not duplicated.

## Verification

All 5 saved KWICs returned `zc.verify(...).ok == True`; line anchors came from the verifier. `zc.head` and `zc.title` were checked for every occurrence. Every SourceTexts value contains `那畔`.

## Fresh-build repair

- Added an English synonym mesh covering side/bank/shore/beyond so retrieval is not hostage to one English noun.
- Corrected the first public exchange's exact actor: Faxi Yin asks the headword-bearing questions; the unnamed monk answers them.
- Normalized all actor/context records, named the exact texts, refreshed counts, and passed the attribution/quotation audit with zero hard failures.
