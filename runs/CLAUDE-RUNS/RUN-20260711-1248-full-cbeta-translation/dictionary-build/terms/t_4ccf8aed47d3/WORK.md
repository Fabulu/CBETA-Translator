# WORK — 平常心 (t_4ccf8aed47d3)

## Frequency and depth inventory

- Allowlist count: **309 hits / 120 texts**.
- `平常心是道` (“ordinary mind is the Way”): 181 hits / 100 texts.
- `如何是平常心` (“what is ordinary mind?”): 30 hits / 19 texts.
- Exact-headword anchors retained for seven distinct evidence roles: direct definition, two independent witnesses of the Nanquan–Zhaozhou cognition/approach exchange, Changsha's bodily and temperature responses, Zhaozhou's animals, Xianglin's greetings, and Zhenjing's criticism of treating the proposition as a final rule.
- One additional `即心是佛` (“mind itself is buddha”) occurrence is stored as `EvidenceRole: family`; it anchors the inherited related-term claim and cannot count toward the headword floor.

## Final sense decision

- **One corpus-wide sense: `ordinary mind`.** The former target “ordinary mind is the Way” was a sentence containing the headword, not a gloss of it.
- Mazu's predicates, Nanquan's restrictions on aiming/knowing, and the direct public-interview answers all concern the same referent. Sleeping, sitting, cooling, warming, foxes, wolves, jackals, and greetings are different answers about ordinary mind, not different lexical things.
- Zhenjing's criticism narrows how the stock proposition may be used; favorable statement versus rebuke is stance, not polysemy.
- `sense-target-distinguishability:` not applicable to a one-sense entry. Item 8 was nevertheless rerun against every added witness; no second referent appeared.

## Exact-turn disposition

| occurrence | complete-unit decision |
|---|---|
| X78n1553 0449c07 | **Mazu Daoyi.** The Jiangxi Mazu section and `師示眾云` identify Mazu's new assembly address; the preceding Baizhang question belongs to the prior exchange. |
| X68n1318 0472b17 | **Nanquan Puyuan** owns the exact headword answer; Zhaozhou Congshen asks the surrounding questions. The containing later address raises the complete old case. |
| J36nB367 0866a22 | **Nanquan Puyuan** again owns the exact answer; Zhaozhou asks. Jiguang Huo is the later record owner quoting the old case, not its speaker. |
| X80n1568 0648c01 | **Changsha Jingcen.** The nearest section head names him; an unnamed questioner asks both questions and Changsha supplies both answers. |
| J24nB137 0362a13 | **Zhaozhou Congshen.** His single-master record and the complete one-turn unit agree; the questioner is unnamed. |
| X80n1565 0309b19 | **Xianglin Chengyuan.** The nearest section head and continuing exchange identify him; the questioner is unnamed. |
| X79n1559 0442a11 | **Zhenjing Kewen.** The Letan Zhenjing section opens `師示眾`; the criticism is his own assembly address. |
| T51n2076 0242b27 | **Sikong Benjing.** Yang Guangting asks; Benjing answers. Stored only as family evidence for `即心是佛`. |

All title/header candidates were checked against the complete extracted unit. No `MasterName` was copied from a title without turn reconstruction.

## Definition and family retest

- Mazu directly answers `何謂平常心` (“what is ordinary mind?”) with the five negative pairs; all are preserved and translated.
- Nanquan's answer and follow-up prohibit reducing the referent to an object one aims at or a category of knowing/not knowing.
- Changsha, Zhaozhou, and Xianglin supply three different public-answer shapes without changing the lexical referent.
- Zhenjing supplies the needed counterexample: repeating `平常心是道` as an ultimate rule is explicitly criticized.
- Family comparison: `平常心是道`, `道`, `即心是佛`, and `無造作` can all remain separately defined. The sentence and related phrases do not replace the headword's own gloss.
- Inherited research verdict: **revise and preserve**. The old research correctly found the Mazu definition, Nanquan case, daily-answer family, and criticism, but its earlier two-sense architecture and sentence target were rejected. Every useful finding remains anchored or explicitly retained as a research lead.

## Omission audit

- Included every unique high-value deployment found in the prepared workbook and inherited notes.
- Retained the second Nanquan witness because it independently establishes case circulation and forces the embedded-case attribution veto.
- Added Zhenjing rather than another repetitive `平常心是道` witness because criticism is lexicographically unique.
- Excluded additional eating/sleeping verses and repeated logion witnesses as duplication after the bodily-response, animal, greeting, and critique classes were anchored.
- The later equation `真心乃平常心也` was not used to define the headword: it is a downstream authorial equation, not a second corpus-wide referent established across named Chan masters.

feedback-observations: Mazu's s1o1 self-definition excludes contrivance, right/wrong, grasping/rejecting, annihilation/permanence, and ordinary/holy; Nanquan's s1o2–o3 exchange excludes aiming and the knowing/not-knowing pair; s1o4–o6 answer with sleep/sitting, cooling/warming, animals, and greetings; Zhenjing's s1o7 rejects treating the stock proposition as a final rule.

feedback-inference-verdict: **licensed** — ordinary mind is not presented as a special state to approach or a formula that settles the matter. This is the smallest conclusion jointly required by Mazu's predicates, Nanquan's exchange, the direct answers, and Zhenjing's counterexample; it imports no doctrine or hidden symbolism.

feedback-falsification-searches: Searched direct definitions (`平常心者`, `何謂平常心`, `如何是平常心`), the full `平常心是道` family, aim/approach and know/not-know predicates, sleep/eat/sit and hot/cold responses, animal answers, greetings, `以為極則` criticism, the `真心` equation, `即心是佛`, `無造作`, and possible master-specific or different-referent uses.

feedback-counterexamples: Zhenjing Kewen explicitly criticizes repeating “ordinary mind is the Way” as the final rule; Nanquan says aiming is already off and the Way belongs neither to knowing nor not knowing. These prevent a definition of ordinary mind as mere routine behavior, complacency, a target state, or a memorized proposition.

feedback-scope: One corpus-wide referent. Mazu's direct predicates, Nanquan's case, and the later public answers are differently shaped evidence about it; the Zhenjing rebuke is a case-scoped restriction on a stock formula, not a new hostile sense.

lookup-probes: `ordinary mind`, `everyday mind`, `plain ordinary mind`, `normal mind`, `usual mind`, `ordinary state of mind`, `everyday state of mind`, and `plain mind` are covered by the preferred target, alternate targets, and search aliases.

opening-interpretation-verdict: **pass** — the explanation opens with the corpus-earned restriction that this is neither a special state to approach nor a stock rule, then immediately supplies Mazu's direct definition and the named counterevidence. It no longer begins with graph composition, frequency, or an interpretation-free quotation list.

## Current mechanical status

- Research route: `zc_batch.py count`, `indexed_kwic.py`, and exact XML verification. Website-v3 cross-check was unavailable because Node is not installed in this shell.
- Definition-formula counts: 平常心者 2 / 1; 謂之平常心 2 / 2; 喚作平常心 3 / 3; 何謂平常心 3 / 3; 如何是平常心 30 / 19; 所謂平常心 and 名為平常心 0. The Mazu formula supplies a direct definition; later naming formulas were treated as downstream equations or commentary and did not overrule it.
- Incompatible-frame audit: predicates of aiming, knowing, contriving, choosing, sleeping, sitting, cooling, warming, greetings, and animal answers all address one ordinary mind. None selects a different object or named work.
- Nested-family audit: 平常心是道 is a sentence containing the headword, not a second target; 道, 即心是佛, 無造作, 無是非, and 無取捨 remain separately adjudicated families. Their counts and meanings cannot buy a second headword sense.
- Family propagation: the earlier correction from sentence-target to `ordinary mind` remains compatible with all linked articles; no dependent entry should inherit the rejected full-sentence gloss.
- modifier verdict: not applicable.
- Final cohort gate: `run_cohort_gate.py t_4ccf8aed47d3` returned `hardPass: true`; exact KWIC 8/8, attribution failures 0, public-feedback flags 0, depth/sense hard failures 0, forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-pingchangxin-gate.json`.
- No merge, commit, or push was run.
