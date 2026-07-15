# WORK — 恁麼 (t_7180f7431520)

## Frequency (allowlist-scoped)
- 400 allowlist files, ~27,436 raw occurrences.
- Idioms (allowlist-wide sums): 恁麼去 ≈ 1175, 恁麼來 ≈ 642. 與麼 is a graphic variant (also very common).
- Top files: X80n1565 (1069), X82n1571 (1068), X81n1571 (763)…

## Concordance (curated, verbatim; speaker confirmed against chapter head)
| KWIC | Text | FromLb | Speaker | How confirmed |
|---|---|---|---|---|
| 祖曰。什麼物恁麼來。曰說似一物即不中。 | T51n2076 景德傳燈錄 | 0240c12 | Huineng (祖), in Nanyue Huairang's chapter | mulu 南嶽懷讓禪師 (line 4676); 讓 goes to 曹谿 to 參六祖, 祖問 |
| 頭曰。恁麼也不得。不恁麼也不得。恁麼不恁麼總不得。子作麼生。 | X80n1565 五燈會元 | 0109a24 | Shitou Xiqian (頭=石頭), to Yaoshan | mulu 澧州藥山惟儼禪師 (line 8384); 首造石頭之室…頭曰 |
| 又問。恁麼來不立。恁麼去不泯時如何。師曰。 | X80n1565 五燈會元 | 0128b08 | monk→Luopu Yuanan | mulu 澧州洛浦山元安禪師 (line 9819) |
| 賓云總不與麼。師便打。 | T51n2076 景德傳燈錄 | 0295b10 | Kebin (克賓)→Xinghua | passage names 興化 + 克賓維那; 師 = 興化存獎 |

## Sense analysis
Single corpus-wide sense: the colloquial demonstrative "thus / like this / in this way." It POINTS at the concrete present without predicating — its force in Chan is exactly that it names nothing (Huairang: 說似一物即不中). Forms curated:
- founding 什麼物恁麼來 "what comes thus?" (Huineng→Huairang)
- double bind 恁麼…不恁麼…總不得 (Shitou→Yaoshan) — also carries 作麼生, cross-linked
- coming/going pair 恁麼來 / 恁麼去 (Luopu chapter)
- graphic variant 與麼 (Xinghua/Kebin)

## Multi-source verdict: MULTI-SOURCE
2 independent lamp records (景德傳燈錄, 五燈會元); 6 masters (Huineng, Huairang, Shitou, Yaoshan, Luopu, Xinghua). Pervasive across the corpus.

## Deflation check
Deliberately NOT rendered as reified "Suchness/Thusness" (the classic over-read). It is a bare pointing demonstrative — the finger, not a metaphysical noun. Explanation says so explicitly.

## Thin spots / caveats
- occ. 1 is spoken by Huineng but sits in Huairang's *chapter*; AttributionNote names the actual speaker (祖=Huineng). Huineng in RelatedMasters.
- 與麼 is filed here as a variant (occ. 4); it also merits its own entry — noted, not merged (guide §5b: coincidental/variant relations are the lexicographer's call; here 與麼 is a genuine orthographic variant → in RelatedTerms).

## Gate 2 verification (Claude, 2026-07-11)
- **All 4 KWICs re-derived from source and confirmed EXACT CONTIGUOUS after XML-tag stripping.** Two are split across `<lb>` boundaries in the raw file (grep on the full string fails; they join contiguously): occ. 2 (恁麼也不得 ends 0109a24 / 也不得… continues 0109b01) and occ. 4 (賓云總 ends 0295b10 / 不與麼 continues 0295b11). Verified by reading around each.
- **Attribution fixes (3): MasterName → null on two-speaker lines** (the term-user is NOT the field's master):
  - occ. 1: was Nanyue Huairang → null. 恁麼 spoken by Huineng (祖), answered by Huairang. Two speakers.
  - occ. 3: was Luopu Yuanan → null. 恁麼來/恁麼去 is in an anonymous monk's question (又問…); Luopu only answers.
  - occ. 4: was Xinghua Cunjiang → null. 與麼 spoken by the disciple Kebin (克賓); Xinghua only strikes (師便打, action).
  - occ. 2 KEEPS Shitou Xiqian: 頭曰… is Shitou's single-speaker utterance (師罔措 is narration, not a second spoken line). Speaker confirmed 頭=石頭 via parallel X80n1565 0418c15 藥山問石頭.
- All 6 RelPaths in zen-corpus.json (no contamination). Validation stays **multi-source** (2 independent lamp records, term pervasive; per-occurrence null attributions don't affect the corpus-wide reading's multi-source status). RelatedTerms all genuine (與麼 variant; 恁麼來/恁麼去 genuine constituents; 作麼生/如是 semantic).
- **STATUS: verified**

## d001-A depth repair (2026-07-13)

- Re-ran item 8: inference, condition, temporal deixis, proposed understanding, negation, and graphic variant all retain one demonstrative referent.
- Preserved four old anchors and added six verified deployment classes: 恁麼則 inference, 恁麼會 appraisal, 正當恁麼時, 若恁麼, doubled positive/negative citation, and a tested proposed understanding.
- Final depth: 1 sense, 10 occurrences.
- Family check: 與麼 is a graphic variant; 恁麼則 remains a separate fixed-compound entry “if so, then.”
- Omission decision: further connective repetitions were excluded because they establish no new syntax or referent.

## Gloss-hygiene and family retest

- Item 8: `thus`, `like this`, and `in this way` are English renderings of one demonstrative, not different referents. Retained the clean preferred target `in this way`; the remaining renderings are alternates.
- Family retest: 與麼 is a graphic variant of 恁麼, not a sense. The positive, negative, conditional, temporal, and inferential frames all preserve one demonstrative use; 恁麼則 remains a linked fixed compound with its own entry.

## Full attribution/depth remediation (2026-07-13)

- Reworked the speaker ladder occurrence by occurrence. Huineng, Shitou Xiqian, Kebin, Yuanwu Keqin, and Foyan Qingyuan's responsibility for a citation are now explicit. Three weak anonymous-monk questions were replaced rather than falsely reassigned to their answering masters.
- Rewrote every source label English-first with the exact Chinese title and anchored every Chinese phrase used in prose.
- Added direct Hongzhi and Sixin witnesses to meet the frequency-scaled floor. The retained set carries ten exact-headword occurrences plus two family witnesses: the graphic variant and Hongzhi's fixed compound 恁麼則. Neither family witness counts toward base-headword depth.
- Definition retest: all evidence still points with the same colloquial demonstrative. No noun, title, person, or distinct referent emerged, so item 8 remains one sense.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP one corpus-wide deictic sense. Positive, negative, conditional, temporal, inferential, and doubled frames change what is pointed to or how the clause works; they do not create different lexical things.
- feedback-observations: Huineng asks what thing comes “in this way”; Shitou rejects “in this way,” “not in this way,” and both together; Yuanwu tests understanding and the exact moment at issue; Hongzhi uses direct assertion, movement, and the linked inferential compound; Sixin negates a manner of acting. Each frame still points back to a stated manner, condition, event, or proposal.
- feedback-falsification-searches: Rechecked the headword, its graphic variant, coming and going frames, proposed-understanding and conditional frames, the exact-time frame, positive/negative doubling, the inference compound, and direct “must be this way” constructions. Searched for noun, title, person, concrete-object, and technical-category uses; none required a second referent.
- feedback-counterexamples: Shitou's rejection of all three alternatives and Yuanwu's warning that understanding it “in this way” misses do not erase the demonstrative meaning; they reject the proposal it points to. Huineng's exchange likewise prevents inflating the pointer into the named metaphysical object “Thusness.”
- feedback-scope: One corpus-wide colloquial demonstrative, pervasive across lamp records and individual records. Particular exchanges supply examples but no named speaker owns a separate sense.
- lookup-probes: Reader probes covered “this way,” “that way,” “like that,” “so,” and “just like this,” alongside the existing targets “thus,” “like this,” and “just so.” These are now stored as sense-approved SearchAliases.
- opening-interpretation-verdict: KEEP the corpus-earned opening. It begins with the ordinary demonstrative and immediately shows the Chan bend: the same pointer becomes a hinge in public questioning, inference, proposed understanding, exact-time testing, and refusal.
- definition-formula-audit: No self-definition formula turns the word into a named object. The strongest test is distributional: it accepts coming, going, seeing, speaking, acting, condition, exact-time, negation, and inference frames while preserving one pointing function.
- nested-family-audit: The graphic variant and the fixed inferential compound were rechecked. The variant remains the same word; the compound retains its own entry and its two family witnesses do not buy base-headword depth.
- modifier-and-provenance-audit: No feedback modifier is at issue. All twelve stored witnesses were re-read; ten exact-headword anchors and two clearly labeled family witnesses preserve exact named responsibility and English-first source labels.
- semantic-propagation: Apply the same non-reifying treatment to the graphic variant, “comes this way,” “goes this way,” “how?,” and “such/as so.” The linked inferential compound must remain searchable without being collapsed into the base word.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 12/12, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-renmo-gate.json`.
