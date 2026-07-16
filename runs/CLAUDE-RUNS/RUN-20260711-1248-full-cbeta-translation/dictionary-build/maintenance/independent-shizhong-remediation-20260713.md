# Independent remediation report — 示眾 (`t_1a7e251bda53`)

Date: 2026-07-13  
Reviewer: Codex independent exact-turn review  
Scope: entry, complete cases, definition/sense/depth/opening/search/quote audit, and full mechanical gates. No merge, commit, or push.

## Verdict

**PASS after targeted repair.** The two-sense article is semantically sound:

1. **public-address format** — a spoken or written address/instruction directed to the assembled community and used as a recurring record heading;
2. **physical display to the assembly** — raising or showing an object before the community.

These are different public acts and therefore different things under item 8. Spoken versus written delivery is medium/grammar within the first sense; flower, whisk, trousers, and mirror are different objects within one display action.

## Repairs applied

1. The editorial genre rule in *Essentials from the Patriarchs' Addresses* formerly carried a bare null `MasterName`. It now takes the exact-actor XOR **impersonal** branch with Kind, ActorLabel, ActorRole, concrete GrammarEvidence, reviewer, and timestamp. The sentence grammatically classifies headings; it is not a personal turn. Its attribution note explicitly names the source and the narrative editorial classification.
2. The Yaoshan Weiyan row formerly called the displayed object a generic garment while the stored KWIC began after the object. The complete case says that a donor gave a pair of trousers and Yaoshan raised them before the assembly. The KWIC was extended to include `有施主施裩` (a donor gave a pair of trousers), and the Explanation and AttributionNote now say **a donated pair of trousers**.
3. Corrected “Gutting” to **Guting** in the English title of the source preserving Luopu Yuanan's final address.
4. Refreshed the durable concordance count to **12,002 hits in 399 allowlisted files**.

## Complete-case actor review

| Row | Exact actor/state | Evidence and veto |
|---|---|---|
| address 1 | Yuanwu Keqin | Own record, seat ascent, continuous Yuanwu address |
| address 2 | Impersonal editorial classification | Editorial-rules grammar; no personal speech/action |
| address 3 | Huangbo Xiyun | Inline `黃檗和尚示眾云`; compiler/commentator rejected |
| address 4 | Yuejiang Zhengyin | Own record; direct comparison and naming of small convocation versus assembly address |
| address 5 | Luopu Yuanan | Inline `洛浦臨終示眾云`; Guting is commentary-container owner, not actor |
| address 6 | Vasumitra | Seventh-patriarch section and immediately preceding life account |
| address 7 | Ruibai Mingxue | Own record; `師` writes and presents the verse after the assembly replies |
| display 1 | Shakyamuni Buddha | Explicit World-Honored One actor in the Shakyamuni section |
| display 2 | Yuanwu Keqin | Own record; raises whisk and questions assembly |
| display 3 | Yaoshan Weiyan | Inline `藥山`; trousers now anchored; Mengxi container rejected |
| display 4 | Yangshan Huiji | Explicit Yangshan case and mirror action |

All ten master actors are named. Four source-attested English names remain reported as deferred non-roster by the current mechanical audit because roster expansion is a separate active task; none is erased or converted to anonymity.

## Definition and deployment audit

- Direct/near-definition probes: `示眾者` 1/1, `所謂示眾` 1/1, `謂之示眾` 1/1; `名為示眾`, `喚作示眾`, `何謂示眾`, and `如何是示眾` 0/0.
- Address shapes: `示眾云` 3,928/299; `示眾曰` 1,073/115; `上堂示眾` 127/42; `陞座示眾` 21/17; `臨終示眾` 17/14; `書偈示眾` 11/9.
- Display shapes: `拈花示眾` 118/80; `舉拂示眾` 7/3; `提起示眾` 35/26.
- The preface phrase `所謂示眾` describes observers listening to the address. It corroborates the first sense but adds no definition beyond the retained editorial classification and Yuejiang naming formula. It was not added because its exact actor is a named non-master preface writer, a state the current master-only XOR schema cannot encode honestly.
- Family retest: hall address and small convocation remain neighboring formats; final address is a longer event label; holding up the flower and raising the whisk instantiate the physical-display sense. No third thing emerged.
- Search aliases remain well scoped: five address/instruction probes and five show/display/raise probes.
- Both openings state the referent and Chan bend before counts or quotations.

## Mechanical gates

Full bundle: `maintenance/cohort-gate-independent-shizhong-final.json`.

- `zc.verify`: **11/11**, exact line bounds, zero failures.
- Exact actor XOR: **10 named + 1 impersonal**, zero unresolved actors and zero conflicts.
- Attribution notes: **11/11**; source and exact actor/state named.
- Chinese prose evidence: **6/6 anchored**, zero dangling strings.
- Depth/sense: **0 hard failures, 0 review flags**; two senses, eleven exact-headword anchors, required source spread satisfied.
- Public-feedback/search/opening: **1/1 pass, 0 flags**.
- Forbidden reader-facing English: zero matches.
- Attribution packet generation: pass; all eleven cases remain human-reviewed rather than packet-autoaccepted.
- Overall cohort gate: **HARD PASS**.

## Files changed

- `terms/t_1a7e251bda53/entry.v2.json`
- `terms/t_1a7e251bda53/WORK.md`
- this report
- final gate and attribution-packet reports

No generated termbase artifact was merged or edited.
