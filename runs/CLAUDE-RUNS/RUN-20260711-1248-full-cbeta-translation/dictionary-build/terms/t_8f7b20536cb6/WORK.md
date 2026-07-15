# 和尚 — depth and sense repair

## Concordance and old-evidence audit

- Live `zc.count("和尚")`: **52,209 hits in 441 allowlisted texts**.
- The previous three anchors were retained as evidence inventory: name-following title, Huike's direct address, and the Cloth-Bag Monk public question. All remained exact, but three witnesses did not cover the entry's deployment range.
- Definition-formula searches do not yield a corpus statement of the form “和尚 means …”; the role is established by syntactic and institutional contrasts instead.

## Deployment inventory

- **Named title/person:** headings place 和尚 after a master's name; included with Bodhidharma.
- **Direct address in a lineage encounter:** included with Huike addressing Bodhidharma.
- **Public interview:** included with the Cloth-Bag Monk question.
- **Teaching-seat/institutional use:** included both the request that the master speak for the assembly and the “head of the hall” request to address the broad assembly.
- **Quoted authority in later instruction:** included “Master Wuzu Fayan said.”
- **Ordination relation:** included two biographies whose syntax explicitly says the person received precepts from the named 和尚.
- **Explicit ordination-office label:** included 得戒師 and 得戒和尚 witnesses; the latter contrasts the ordination preceptor with two tonsure teachers.
- **和尚子:** excluded from the standalone senses because it is an overlapping compound used to address monk-fellows.
- **Plain duplicate title tokens:** excluded; 52,209 hits do not justify padding with repetitions.

## Item 8 split test

- **Split:** teaching master/monastic head and ordination preceptor are different human offices. The biographies explicitly distinguish hearing teaching under one person from receiving precepts from another, and 得戒師 names the second role directly.
- **No split for direct address:** “Master” in a question and “the master” in narrative prose have the same referent; only English capitalization and grammar differ.
- **False-positive guard:** 戒和尚 frequently means “Master Jie,” especially Wuzu Jie. It is not ordination evidence without a receiving/conferring-precepts relation.
- Both senses are corpus-wide and supported by independent allowlisted texts; neither is master-specific.

## Family cross-check and definition decision

- Cross-checked the standalone precept entry 戒 and the attested family strings 受戒, 得戒, 戒和尚, 得戒和尚, and 和尚為得戒師.
- The two-sense structure lets the family definitions coexist: 戒 remains the hard rule/precept; 受戒 and 得戒 describe receiving it; the ordination-preceptor sense names the human conferrer.
- Decision: revise the old one-sense target rather than preserve “preceptor” as a loose alternate under “the master.” The old explanation blurred different offices.

## Mechanical QA target

- Ten curated occurrences, each anchoring a distinct prose fact.
- Every KWIC must contain 和尚, return `zc.verify(...).ok == true`, and use the verifier's primary-edition bounds.
- `SourceTexts` must exactly equal each sense's occurrence paths.

## D002-B keystone enrichment

- Re-read guide §5 #0–#0g and reopened both roles and their families against the 52,209-hit concordance and `DEPTH_RESEARCH_D002_B.md`.
- Definition-formula result: allowlisted `X和尚者` strings open named biographies rather than defining the word. A lexical etymology found outside the allowlist was excluded. Role formulas retained: named title, quotation, teaching-seat request, hall-head office, receiving precepts from a named preceptor, explicit preceptor appointment, and `得戒和尚`.
- Teaching-master inventory now anchors ten distinct classes: lineage heading, direct plea, public question, request to teach the assembly, quoted authority, hall-head office, teacher provenance, illness/death address, biographical opening, and collective “old masters.” Direct address remains a grammatical rendering difference, not a new sense.
- Ordination-preceptor inventory retains four non-duplicate relations: a full tonsure/study/precept/teaching sequence; teaching under one person contrasted with receiving precepts from another; explicit appointment as the preceptor from whom precepts were received; and `得戒和尚` beside separate tonsure masters.
- False-positive guard preserved and strengthened: the exact Wuzu Jie line `到五祖戒和尚處` is teacher-name evidence, not ordination evidence. Bare `戒和尚` never triggers the preceptor sense without a receiving/conferring relation.
- Family retest: `受戒`, `得戒師`, `得戒和尚`, `薙髮`, `堂頭和尚`, and `和尚子` can all coexist with the two-role split. The first sense names the teaching/community relation; the second names the human conferrer in ordination.
- Omission decision: repetitive named-title tokens were excluded despite the large concordance. Final architecture is ten teaching-master plus four ordination-preceptor anchors across eleven sources; all are exact-verified with synchronized bounds.

## 2026-07-13 item-8 ledger

- Re-tested the 52,209-hit/441-file concordance against `堂頭和尚`, direct address, named biography headings, teacher provenance, `受戒`, `得戒師`, `得戒和尚`, `薙髮`, and the false-positive personal name `戒和尚`. Fourteen exact witnesses across eleven sources already exceed the strengthened floor without title-token padding.
- Direct-address `Master` and narrative `the master` remain one teaching/community office. The ordination evidence explicitly contrasts hearing teaching under one person with receiving precepts from another and names the preceptor from whom precepts were received; it therefore establishes a different human office.
- #0g re-test: the first sense remains the person questioned, cited, and invited to teach publicly; the second is retained under the precepts carve-out as the human conferrer in an ordination relation. Bare `戒和尚` never triggers it.
- sense-target-distinguishability: KEEP — `the master` names the teaching or community head in lineage and public interviews; `ordination preceptor` names the person who confers precepts in a documented ordination relation.

## 2026-07-13 retrospective gates 10–19

- inherited-lead verdict: **REVISE, not reject.** The two-office split and fourteen standalone witnesses survived cross-checking. The repair names every occurrence's responsible record/section figure, anchors two formerly dangling family/contrast claims, adds retrieval aliases, and removes null-attribution excuses.
- indexed discovery, website v3 sharded engine (`web_index_kwic.mjs --exact`): `和尚` 53,116/441; `和尚者` 124/74; `所謂和尚` 0; `謂之和尚` 0; `名為和尚` 0; `喚作和尚` 1/1; `何謂和尚` 0; `如何是和尚` 1,540/182; `堂頭和尚` 247/80; `老和尚` 3,746/355; `大和尚` 562/209; `和尚子` 62/33; `戒和尚` 43/32; `得戒和尚` 10/5; `受戒於` 10/9; `為得戒師` 1/1.
- desktop-artifact cross-check (`indexed_kwic.py`, v4 postings plus exact text sidecar): `和尚` 51,189/441; `和尚者` 96/61; the six definition formula probes returned 0 except `如何是和尚` 1,265/163; `堂頭和尚` 207/69; `老和尚` 3,463/350; `大和尚` 513/200; `和尚子` 52/30; `戒和尚` 39/30; `得戒和尚` 9/5; `受戒於` 10/9; `為得戒師` 0. Index differences are discovery-artifact differences; XML-scoped `zc.count` remains the evidence count, 52,209/441.
- definition-search verdict: `和尚者` predominantly opens named biographies, `如何是和尚` predominantly asks about the person or office, and the direct definition formulas do not define the word. Keep the role-based syntactic/institutional proof.
- item-11 observation: standalone o1–o10 place the title in lineage headings, direct pleas, public questions, teaching-seat requests, quoted authority, hall-head office, provenance, illness address, biography, and collective reference. Standalone o11–o14 place it in receipt/conferral formulas; family o11 anchors `和尚子`; contrast o5 anchors named Wuzu Jie.
- item-11 minimal-inference: the first sense is a teaching/community title; the second is the distinct person who confers precepts. `和尚子` is a nested address compound, while bare `戒和尚` cannot establish ordination because 戒 can be a personal name.
- item-11 ordinary-bridge: receiving precepts *from* someone and explicitly appointing a `得戒師` identify an ordination relation; direct-address capitalization does not change the person addressed.
- item-11 falsification-searches: direct definition formulas; `堂頭和尚/老和尚/大和尚`; `戒和尚/得戒和尚/受戒於/為得戒師`; nested `和尚子`; biography-form `和尚者`; question-form `如何是和尚`.
- item-11 counterexamples: `到五祖戒和尚處` is Longhua Xiaoyu arriving at Wuzu Jie, not receiving precepts. This narrows the second sense. No evidence collapses the two offices or makes direct address a third thing.
- item-11 scope: both senses corpus-wide; the false-positive control is formula-specific. verdict: **direct** for title/office relations; **licensed** for the concise institutional Zen bend.
- incompatible-frame/sense verdict: **KEEP TWO.** Teaching, presiding, addressing, and citation take the titled community figure; receiving/conferring precepts and `得戒師` take the ordination preceptor. These are different roles, not noun/verb or register variants.
- nested-compound ledger: `和尚子` = family evidence only, excluded from standalone depth; `戒和尚` = ambiguous sequence requiring relation-level adjudication; `堂頭和尚` = institutional family phrase consistent with sense 1; no compound creates a new bare-word sense.
- family propagation: `僧問`, `禪師`, `方丈`, `堂頭和尚`, and the precept family remain compatible. `和尚子` must not inherit either office as a standalone definition; `得戒和尚` routes to sense 2; `戒和尚` requires the personal-name guard.
- lookup probes, sense 1: `zen master`, `chan master`, `monastic master`, `abbot`, `teacher`. Approved as `SearchAliases`; exact display targets remain lexical translations.
- lookup probes, sense 2: `ordination master`, `ordination teacher`, `precept master`, `precept preceptor`, `ordination preceptor`. Approved as `SearchAliases`.
- opening-interpretation-verdict: **licensed.** Observation: named headings, lineage provenance, public questions, and teaching-seat requests converge on a person who teaches and presides; explicit receipt/conferral predicates converge on the ordination office. Minimal inference: name each role before quotations. Counterexample: direct address changes syntax only, and Wuzu Jie blocks a blanket `戒和尚` rule. Scope: corpus-wide.
- attribution gate: 16/16 occurrences now carry a responsible named figure and a source-and-speaker note. Four record-title names (`Linwo`, `He Yizi`, `Jie Weizhou`, `Zhufeng Huanmin`) plus `Longhua Xiaoyu` and `Budai` are retained as source-attested pinyin names pending the separately owned roster expansion.
- quote-anchor gate: `和尚子` gained a verified family occurrence under Xuefeng Yicun; `到五祖戒和尚處` gained a verified contrast occurrence under Longhua Xiaoyu. Both new witnesses re-confirm the split instead of changing it. No quoted Chinese string was deleted.
- forbidden-label scan: PASS — neither exact banned English label occurs in reader-facing fields.
- feedback-inference-verdict: `direct` for the two role relations; `licensed` for the institutional Zen bend.
- feedback-observations: standalone occurrences s1-o1–o10 and s2-o1–o4; family s1-o11; contrast s2-o5.
- feedback-falsification-searches: definition formulas; teaching-office families; ordination-relation families; personal-name and nested-compound controls.
- feedback-counterexamples: Longhua Xiaoyu's arrival at Wuzu Jie blocks treating bare `戒和尚` as ordination; direct address blocks no role but remains grammar only.
- feedback-scope: corpus-wide senses, formula-specific false-positive guard.
- lookup-probes: sense 1 = zen/chan/monastic master, abbot, teacher; sense 2 = ordination master/teacher, precept master/preceptor, ordination preceptor.

## 2026-07-13 exact-speaker repair

- The earlier “responsible named figure” attribution rule was too loose. It confused the person addressed or described with the person who actually uttered the headword-bearing turn. This section supersedes that attribution claim.
- Corrected Huike's plea from Bodhidharma-as-addressee to Huike-as-speaker.
- Replaced anonymous-monk or institutional-address witnesses aimed at Budai and Guishan with named-speaker equivalents: Huang Tingjian addressing Huitang Zuxin, and Guishan Lingyou addressing Baizhang Huaihai.
- Replaced the anonymous hall-head address to Wuzu Fayan with Xiaotang Chaoyuan's speaker-owned portrait address to Master Sanmei.
- Replaced the cloister director's anonymous health question to Mazu with Zhaozhou Congshen's named thanks to Nanquan Puyuan.
- Replaced narrator-only title tokens for Bodhidharma and Puhua with Linji Yixuan's direct statement about the old masters under heaven and Yezhu Fusheng's direct raising of the Cloth-Bag Master.
- In the ordination sense, replaced the Linwo biography's section-subject attribution first with a compiler-owned statement, then (after the stricter exact-speaker review) with Tianze Neng's directly authored `得戒和尚贊`. Replaced Longhua Xiaoyu as the subject of an arrival narrative with Huanglong Huinan's direct raising of a Wuzu Jie case.
- Retained Nanyue Huairang, Wuzu Fayan, Huangbo Xiyun, He Yizi, Jie Weizhou, and Zhufeng Huanmin only after reading their full units and confirming that each is the exact speaker or named author responsible for the saved statement.
- Anonymous addressees and section subjects no longer masquerade as speakers. Each retained AttributionNote now states speaker versus addressee/subject explicitly where the distinction matters.
## 2026-07-13 exact-speaker follow-up

- Replaced the `錦江禪燈` biographical witness attributed to compiler Zhangxue Tongzui with Tianze Neng's directly authored `得戒和尚贊`. This removes a compiler-as-speaker ambiguity while preserving a distinct, exact-headword ordination-preceptor witness.

## 2026-07-13 independent A4 full-case review

- Re-read all sixteen complete cases without inheriting the prior actor verdicts. The two senses survive falsification: a teaching/community master and the human ordination preceptor are distinguishable institutional referents, while direct address remains grammar rather than a third sense.
- Trimmed four mixed-dialogue rows to Huike's, Huang Tingjian's, Guishan Lingyou's, and Zhaozhou Congshen's exact headword-bearing turns. Trimmed Nanyue Huairang's provenance reply away from Huineng's surrounding questions. Names and definitions did not change merely because the evidentiary unit became more exact.
- Rechecked the large-concordance depth: ten standalone teaching/community witnesses and four standalone ordination witnesses remain, plus one family and one contrast row. The personal-name control `五祖戒和尚` still blocks a blanket ordination inference from bare `戒和尚`.
- Final A4 result: sixteen occurrences, all named, all exact-verifying, and all reader-facing Chinese evidence anchored.
