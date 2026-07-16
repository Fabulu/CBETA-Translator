# Gloss-hygiene audit — unassigned completed multi-sense entries

Date: 2026-07-13

## Scope and method

- Read-only sweep of every `STATUS=done` entry having more than one sense, excluding the 13 IDs assigned to agents A/B/C in the current item-8 pass.
- Audited: 83 entries.
- Test: can every `PreferredTarget` be distinguished from every other target from the targets alone? I also screened for a word-class-only split of the same event/product, sentences or mini-explanations used as glosses, and paraphrase duplicates.
- No entry listed below was edited. Entries not listed did not present a target-level defect on this pass; that is not a new corpus-level sense adjudication.

## High-confidence candidates

| ID | Term | Current targets at issue | Finding |
|---|---|---|---|
| `t_0ad9fc2dfdda` | 向上 | `upward; the further-up` / `up; upward (spatial)` | The targets overlap on **upward** and cannot be distinguished standalone. Both also fuse alternatives with semicolons. Preserve the attested technical/spatial distinction only if the technical target explicitly names the Chan register. |
| `t_2852a9ae231c` | 隨波逐浪 | `following the waves, chasing the swells` / `to drift along with the waves` | Paraphrase duplicates. The explanation claims named 三句 phrase versus plain idiom, but target 1 does not name the fixed phrase. Rename the first referent explicitly or merge. |
| `t_3414320aa87c` | 命根 | `the root that holds one's life` / `the vital faculty` | The explanations claim an idiomatic severable “life-root” versus an enumerated faculty, but the targets alone remain near-synonyms. The first must name the Chan severing idiom more explicitly if the split stands. |
| `t_358f56dbf990` | 善知識 | `a good teacher` / `good friends!` | The second split is presently justified by vocative grammar/direct address, not a different thing: the addressed people remain people called 善知識. Re-test for merger under item 8. |
| `t_36aa29eb1287` | 水牯牛 | `water buffalo` / `the water buffalo as figure for the 'person who knows-there-is' ...` | Sense 2's target is an explanatory sentence containing the translated headword, not an independently usable gloss. It also embeds a disputed interpretation. Replace it with a concise named Nanquan deployment if the split survives. |
| `t_38ad422a3ce7` | 發明 | `to bring to light` / `a clarification` | Verb versus result noun of the same bringing-to-light event/product. The explanation itself relies on grammar. Merge unless corpus evidence establishes a genuinely different referent. |
| `t_6293dead3bb2` | 轉身 | `turn oneself around` / `turn the body` | Standalone paraphrase duplicates; the distinction appears to be wording rather than different things. Merge or supply targets that expose distinct attested referents. |
| `t_6dadcc69c361` | 料揀 | `to appraise and distinguish` / `a selection or classificatory distinction` | Action versus nominalized result/category. Re-test under the same-event/product rule; the current targets do not establish different things. |
| `t_7cddddb76d37` | 任運 | `proceed of itself` / `Spontaneous Accord` | Capitalization alone does not identify the second referent. The explanation says it is Puming's seventh oxherding section; the target must say that explicitly. |
| `t_8cf244d2d802` | 拈出 | `to pick out and bring forward` / `to take out and present` | Near-paraphrases standalone. The explanations distinguish cited words from physical objects, so targets must name those objects directly (for example, bring a saying forward / present an object). |
| `t_9b71e523fe10` | 具足 | `to be fully endowed` / `full` | Predicate versus attributive adjective, with the second restricted to 具足戒. Word class and compound position alone do not establish a different thing; re-test for merger. |
| `t_ab6276be6e08` | 末後句 | `the last word` / `a master's last word (spoken at death)` | The second is an attested contextual subtype of the same referent, an utterance called the last word, not yet a different thing. Re-test for merger. |
| `t_b4a4ae6874d0` | 異類中行 | three targets beginning `going among the different kinds ...` | The master-specific targets are long explanatory sentences rather than glosses; the Nanquan target additionally states an inference (`acting where knowing cannot reach`). Replace with concise, named deployment labels, then re-test whether generic/Nanquan/Caodong referents truly differ rather than merely having different readings. |
| `t_bcc96a299271` | 開示 | `to disclose and show` / `instruction given` | Verb versus completed teaching/product. The explanation relies on request-versus-receipt grammar. Merge unless a different referent can be established independently. |
| `t_e5259ce8bbf5` | 垂示 | `an indication, a prefatory pointer` / `to offer an indication` | Noun versus verb of offering the same indication. Although the rubric may lexicalize a textual unit, current targets also conflate ordinary indication with prefatory pointer. Re-test and either merge or name the textual rubric as its own referent. |
| `t_e69d7df930ca` | 金牛 | `Jinniu; Jinniu's meal-call case` / `golden ox` | The first target fuses a named master/referent and a case into one gloss. Split those if independently attested as different things, or retain only the referent the occurrences support. |

## Target repairs likely sufficient, with sense structure plausibly sound

| ID | Term | Current target | Finding |
|---|---|---|---|
| `t_0661e03e65e3` | 勘辨 | `examinations and discriminations` | Explanation establishes a collected record section, but the target reads like a mere nominalization of `to examine and discriminate`. Rename it as an examinations-and-discriminations section; otherwise the pair fails standalone distinction. |
| `t_21f09b3726e7` | 血脈 | `connective line; transmission line` and `blood vessels; bodily circulation` | Each target fuses paraphrases with a semicolon. The three underlying referents—transmission line, hereditary bloodline, and bodily vessels—are distinguishable; give each one clean standalone target. |
| `t_4f7bd98ad40f` | 上堂 | `ascend the hall; take the teaching seat` | This target fuses two actions. The institutional-address versus physical-ascent distinction is otherwise independently recognizable; choose the corpus-supported action as the target and leave the other as an alternate if warranted. |
| `t_6f47a97d45b0` | 序 | `rank; division` / `sequence; order` | Both targets fuse alternatives, which obscures whether the three senses are independently distinguishable. Normalize to one referent per preferred target before re-auditing. |
| `t_8a06e7d99b19` | 法嗣 | `lineage succession; lineage affiliation` | Person (`lineage heir`) versus relation is a valid different-thing split, but the relational target itself fuses succession and affiliation. Select the attested relation as the preferred target and move any true synonym to alternates. |
| `t_c891f0944482` | 腳跟 | `one's footing, the spot under one's heels` | The figurative/locative target fuses two glosses. The entry otherwise establishes Chan footing versus injured anatomical heel; use one clean target for each referent. |

## Recommended order

1. Merge/re-test the clear grammar-or-subtype cases: 發明, 轉身, 料揀, 具足, 開示, 垂示, 末後句, 善知識.
2. Repair targets that currently conceal an otherwise plausible split: 向上, 隨波逐浪, 命根, 任運, 拈出, 勘辨.
3. Rewrite sentence-like or fused targets, then adjudicate again: 水牯牛, 異類中行, 金牛, 血脈, 上堂, 序, 法嗣, 腳跟.
