# Independent review — blind clean-regeneration benchmark

**Date:** 2026-07-14  
**Scope:** the five clean drafts and their stored Occurrences/ClaimAnchor, read against the full passage windows in `packets/02-reading-packet.json`. I read `DICTIONARY_ENTRY_GUIDE.md`, `ATTRIBUTION_FIX.md`, `ACTOR_AUDIT.md`, and `ALLOWLIST_AUDIT.md` first. I did **not** inspect live `terms/*` entries or repair ledgers. The benchmark's fixed term order is retained here: **合頭語, 和尚, 棒, 正法眼, 本來無一物**.

## Bottom line

**None of the five drafts is merge-ready.** Final verdicts: **0 KEEP · 4 REVISE · 1 REJECT**.

The drafts are clean at the gates the benchmark actually ran: all 20 stored evidence rows are exact contiguous source substrings with exact line bounds, all RelPaths are allowlisted, every headword Occurrence contains the headword, the one non-headword quotation is correctly stored as a ClaimAnchor, JSON/IDs/roster spellings pass, and forbidden English is absent. Those are real strengths.

They nevertheless fail the current definition of done in two systematic ways:

1. **Every entry is below the mandatory frequency-scaled occurrence floor.** The packet counts are 126, 52,209, 18,109, 2,272, and 214 hits respectively. The corresponding floors are 6, 10, 10, 8, and 6 exact-headword Occurrences; the drafts retain only 3, 4, 5, 3, and 4. The ClaimAnchor for 本來無一物 correctly does not buy depth. All five are therefore mechanically rejectable by the final guide even though the benchmark's custom check reports zero errors.
2. **The hard attribution audit validates record shape, not the truth of its exact-turn decision.** Full-context reading exposes at least four wrong actor classifications in three entries. A complete `ActorAttribution` object does not cure a false attribution.

The benchmark also omits the required durable research artifacts: there is no per-entry `WORK.md` containing the definition-formula sweep, deployment inventory, omission audit, inference ledger, family/nested-compound adjudication, search probes, and opening-interpretation verdict. The reading packet contains only 74 candidate windows for five terms, including terms with tens of thousands of hits; it cannot demonstrate exhaustive high-value harvesting.

## Per-entry verdicts

### 1. 合頭語 — **REVISE**

**What works:** This is the strongest compact semantic draft. “Fitting phrase” is a plausible literal target. The three retained passages establish a coherent Chan bend rather than a mere calque: Yunmen repeats `一句合頭語萬劫繫驢橛`, Chuanzi applies the same tether formula to Jiashan's answer, and Foyan explicitly says one cannot make a fitting phrase fit this matter. The opening is informative, the tether claim is directly supported, the three named utterers are correct in their passage contexts, and the C077/C078 witnesses are independent works rather than duplicate files of one work.

**Defects requiring revision:**

- 126 hits require at least **6** exact-headword Occurrences and four source texts where available; the draft has **3 Occurrences in 2 works**.
- The prose says the phrase “fits or closes over” the matter. “Fits” is direct from `合`; **“closes over” is an unlogged inference** and should either be licensed from predicates or removed.
- The research packet itself shows broader period/text spread than the draft uses. No complete definition-formula, contradictory-use, ordinary-use, genre, or later-comment sweep is recorded, so the apparent one-sense decision and corpus-wide scope remain under-tested.
- `SearchAliases` is empty despite the guide's 3–5 lookup-probe requirement.

**Merge readiness:** Not merge-ready. Preserve the core gloss and the three passages, then deepen and document the evidence rather than replacing this semantic center.

### 2. 和尚 — **REVISE**

**What works:** The entry correctly recognizes that 和尚 functions as a title/form of address and correctly warns that the addressed person need not be the utterer. Occurrences 1, 3, and 4 are useful, exact speech-turn examples: Nanyue refers to “Reverend An,” and Baizhang twice uses 和尚 of Mazu.

**Exact defects:**

- The second occurrence is **misclassified**. Its headword is in compiler narration: `師令僧馳書與徑山欽和尚` (“the master ordered a monk to carry a letter to Reverend Qin of Jingshan”). It is not uttered by “the unnamed questioning monk.” This row must be `narrated`, with the compiler as actor and the people in context; the current six-rung `reviewed-unnamed` result is substantively false.
- `Validation: multi-source` is false on the stored evidence. All four Occurrences and the sole `SourceTexts` value come from **C077n1710, one work**. Multiple sections/masters inside one compilation do not satisfy the work-level source-independence gate. The sense is provisional until independently attested in another work.
- 52,209 hits in 441 files require at least **10** Occurrences; the draft has **4**, all from one work. This is far below both the numeric floor and a credible deployment sample for a keystone institutional/address term.
- The target **“reverend”** is possible as an address translation but too narrow as the sole preferred gloss for the corpus-wide lexical item. The draft itself alternates senior cleric, teacher, and abbot without demonstrating when those English relations hold. It needs an evidence-based institutional/title account, not a synonym menu.
- Headings, personal-title compounds, direct vocatives, third-person reference, and ordinary designation are acknowledged only in a note, not harvested and separated in representative evidence. Nested names such as `徑山欽和尚` also need longest-match/family control.
- `SearchAliases` is empty.

**Merge readiness:** Not merge-ready. The title/address core is salvageable, but the actor error, false multi-source grade, and extreme under-depth are mandatory revisions.

### 3. 棒 — **REVISE**

**What works:** The object/event split is justified in principle by the guide's own `棒` calibration: an implement that is held or handed over and a countable staff-blow are different referents, not merely competing readings. Linji's passage usefully contains both (`拈棒` versus `一頓棒`), and Xuefeng's `二十棒` securely anchors the countable-blow sense. The two preferred targets are distinguishable.

**Exact defects:**

- Sense 1, occurrence 2 is **not compiler narration**. The stored Chinese explicitly says `師云：「德山老漢只憑目前一個白棒…」`: the section's master utters the sentence while quoting Deshan's formula. The exact utterer must be resolved from the complete section; a narrated/compiler record is wrong.
- Sense 2, occurrence 2 is also **not compiler narration**. `長慶舉似泉州太傅，卻云：「此僧合喚轉與一頓棒。」` assigns the headword turn to **Changqing Huileng**. He should be `MasterName`; labeling him merely `person-discussed` reverses the actor/context distinction.
- The two senses reuse essentially the same Linji passage as separate depth rows. That is legitimate evidence for both referents, but it does not supply the missing breadth.
- 18,109 hits require at least **10** exact-headword Occurrences; the entry has **5 total**. The physical-object sense has only two rows, and both senses lack the required period/genre/deployment range.
- Sense 1's claim that records describe the implement “as a white staff” rests on one embedded quotation about Deshan; it does not establish a corpus-level characteristic. `白棒` may itself be a compound/family item and must be tested under the nested-compound rule.
- The entry does not inventory incompatible verb frames or test further referents/usages (implement, blow, beating expression, compounds) despite item 14 specifically naming 棒 as a split-sensitive case.
- Both `SearchAliases` lists are empty.

**Merge readiness:** Not merge-ready. Keep the proposed two-referent architecture provisionally, correct the two actors, and rebuild depth around independent instances of each referent.

### 4. 正法眼 — **REJECT**

**Why rejection rather than revision:** The draft's central semantic claim is built mainly from the longer lexical object `正法眼藏`, while the guide expressly says nested compounds cannot buy the shorter headword's meaning or depth. This requires re-derivation, not a local prose or metadata repair.

**Exact defects:**

- Occurrence 1 contains **正法眼藏**, not an independently active shorter 正法眼. Treating substring presence as shorter-headword evidence violates the longest-match/nested-compound gate.
- Occurrence 2 contains shorter 正法眼 but the full passage is **direct Bodhidharma speech**, not compiler narration: `乃顧慧可而告之曰…昔如來以正法眼付迦葉…我今付汝`. `MasterName` should be **Bodhidharma**. The current narration classification discards the explicit `告之曰` speech frame.
- Occurrence 3 is a genuine standalone question by an unnamed monk, but its respondent is named immediately in the section heading as 越州清化全付禪師. The draft leaves `ContextMasters` empty and says full review did not transfer a nearby identity. Even though the respondent must not become `MasterName`, he belongs in named context if roster resolution is available; otherwise the unresolved roster/name issue must be reported.
- The preferred explanation—“entrusted capacity or possession”—is not the minimal statement the anchors establish. The transmission formula directly depicts an “eye” being entrusted and guarded; **capacity** is an interpretive abstraction with no inference ledger. The standalone question/answer supplies no gloss at all.
- `Validation: multi-source` is not established for a single lexical sense after compound contamination is removed. The only clean standalone deployments retained are both in B14; the C077 row belongs first to the `正法眼藏` family object.
- 2,272 hits require **8** Occurrences; the draft has **3 apparent rows**, fewer once the nested-compound control is applied.
- No evidence establishes whether 正法眼 and 正法眼藏 should be related entries, one lexical family, or distinct objects. That adjudication is prerequisite to drafting.
- `SearchAliases` is empty.

**Merge readiness:** Reject this draft and restart from an exact/longest-match concordance for standalone 正法眼, using 正法眼藏 only as labelled family evidence or ClaimAnchors where it supports a specific claim.

### 5. 本來無一物 — **REVISE**

**What works:** This is the richest reader-facing draft. The preferred target is literal and clear; the alternate verse line is correctly kept as a non-depth-bearing ClaimAnchor; the explanation does more than list quotations and is careful not to turn the line into generic doctrine. Huineng, Huangbo, and Tianyin provide meaningfully different deployments across independent works. The work-level multi-source grade is supportable from B25, C077, and J25, without relying on duplicate editions.

**Defects requiring revision:**

- 214 hits require at least **6** exact-headword Occurrences in four source texts where available; the draft has **4 Occurrences in 3 works**. The ClaimAnchor correctly cannot fill the gap.
- The Dongshan occurrence needs a more explicit source/turn explanation. The passage embeds the Huineng verse inside Dongshan's retelling/commentary; `MasterName: Dongshan Liangjie` is plausible only if complete-case review establishes that Dongshan is the utterer of the whole quoted retrospective. The current note merely asserts this and does not expose the quotation boundary.
- The opening “denying that there was originally even one thing” is a reasonable minimal inference, but “denying” and the later claims about what Dongshan and Huangbo “reject” require the missing inference ledger and counterexample search. Huangbo directly says `無亦不是`; that supports refusal to fix on “no,” but the prose should distinguish direct statement from reviewer synthesis.
- The alternate targets are near-duplicates rather than useful lexical alternatives, while `SearchAliases` is empty. Retrieval probes should be stored as aliases instead of padding the translation menu.
- The draft samples a founding verse, two later comments, and a later reuse, but it does not inventory the many quotation/title/formula repetitions among 214 hits, distinguish quotation from independent semantic deployment, or test variant lines comprehensively.

**Merge readiness:** Not merge-ready, but close in semantic direction. Add independent, lexicographically distinct evidence and the missing provenance/inference controls; retain the ClaimAnchor distinction.

## Comparative quality assessment

Judged only against one another and the source packet, without consulting prior drafts:

1. **合頭語** has the cleanest compact semantic convergence, but is under-researched.
2. **本來無一物** has the best reader-facing depth and evidence/ClaimAnchor distinction, but remains below floor and under-documented.
3. **棒** identifies a genuinely useful sense split, but two of five actor decisions are wrong and coverage is thin.
4. **和尚** captures a basic grammatical caution, but one actor is wrong, every witness comes from one work, and four examples cannot support a 52,209-hit institutional term.
5. **正法眼** is the weakest: nested-compound contamination, a direct-speech passage mislabeled as narration, missing named context, and an unlicensed abstract gloss undermine the article's foundation.

The batch's prose is generally concise, literal, and free of the familiar mystical/doctrinal overlays. Its comparative weakness is not verbosity or stylistic drift; it is **premature closure from a tiny candidate sample**. The exact same four-or-fewer-row drafting pattern appears across radically different frequency classes, which is precisely the floor-clustering/under-harvesting failure the guide warns against.

## Mechanical and release conclusion

- Exact XML/KWIC/lb verification: **PASS (20/20)** according to the stored validation and spot/full-context review.
- Allowlist containment: **PASS for stored RelPaths**.
- Headword-in-Occurrence / ClaimAnchor separation: **PASS mechanically**, with the semantic longest-match exception noted for 正法眼藏.
- Exact actor truth: **FAIL** (和尚 occurrence 2; 棒 sense 1 occurrence 2; 棒 sense 2 occurrence 2; 正法眼 occurrence 2).
- Work-level source independence: **FAIL for 和尚**; **not established for the proposed 正法眼 sense after nested-compound control**.
- Frequency-scaled depth: **FAIL for all five**.
- Required WORK/inference/family/search ledgers: **FAIL/absent for all five**.
- Genuine merge readiness: **NO**.

These drafts demonstrate fast production of syntactically valid, exactly anchored JSON. They do **not** demonstrate clean regeneration of merge-equivalent lexicographic articles in eleven minutes. The dominant semantic work—exhaustive high-value harvesting, exact-turn reading, work-level independence, family control, inference falsification, and independent review—was compressed or omitted, and the omissions materially changed the verdicts.
