# 休歇 — research and depth audit

## Concordance

- `zc.count("休歇")`: 698 hits in 194 allowlisted texts.
- Morphology: `休歇去` 101/54; `休歇處` 65/49; `大休歇` 67/40; `休去歇去` 123/68; `休歇也未` 6/6; `休歇地` 10/8; `喚作休歇` 1/1; `休歇者` 1/1.

## #0f inventory

- Definition formulas searched: `休歇者`, `所謂休歇`, `謂之休歇`, `名為休歇`, `喚作休歇`, `何謂休歇`, `如何是休歇`.
- Explicit definition/contrast retained: Zhenxie Qingliao records a claimed `休歇處` based on empty stillness and dead-tree lifelessness, then says it has `沒交涉`; this rejection is represented prominently.
- Other deployments retained: Linji's imperative plus eating/sleeping clause; a direct `作麼生是休歇處` question and answer; Qingyuan Weixin's three-stage mountain/water address; a master's counterquestion; Dahui's retrospective of the second ancestor; and `休歇也未` as an assembly question.
- Morphology retained in prose and counts: verb, imperative, predicate question, `處`/`地` noun phrases, `大休歇`, and doubled `休去歇去`.
- Omission audit: all unique high-value findings are included. Repetitive later claims involving `大休歇田地` were summarized rather than imported as a separate abstract sense.

## Verification

All 7 saved KWICs returned `zc.verify(...).ok == True`; line anchors came from the verifier. `zc.head` and `zc.title` were checked for every occurrence. Every SourceTexts value contains `休歇`.

## 2026-07-14 semantic remediation (r002 owner 2)

- research-paths: exact count/replay and 休歇去, 休歇處, 休歇地, 不得休歇, 無休歇 countersearches in `semantic-r002-owner2-countercounts-3.json`.
- feedback-inference-verdict: LICENSED — rest/cease is the shared observable action; the entry preserves both positive uses and the explicit rejection of dead-tree stillness as rest.
- feedback-observations: X/X79/X79n1557.xml#0088c24; T/T51/T51n2077.xml#0557b07 and #0614c02; X/X71/X71n1426.xml#0781b10.
- feedback-falsification-searches: imperatives, resting-place nouns, failure to rest, empty-stillness rejection, ordinary eating/sleeping, and noun/verb packaging.
- feedback-counterexamples: Zhenxie Qingliao explicitly rejects one claimed resting place; this narrows deployment without deleting the word's stopping/resting sense.
- feedback-scope: one corpus-wide stopping/resting action; appraisals source-specific.
- lookup-probes: rest / cease / stop / come to rest / resting place.
- observation: the same word commands rest, asks whether rest has been found, and builds place/ground compounds.
- minimal-inference: noun and verb constructions package the same stopping/resting referent.
- ordinary-bridge: eating, sleeping, stove-sitting, and ordinary rest keep the public uses grounded.
- falsification-searches: grammatical role, incompatible activity, rejected stillness, longer compounds, and alternate referents.
- counterexamples: no second action survived; critiques remain critiques of claimed rest.
- scope: one sense.
- verdict: licensed.
- nested-compound-verdict: 休歇處 and 休歇地 name places of the same action and do not create separate bare senses.
- verb-frame-verdict: go-rest, obtain-rest, resting-place, and not-rest retain one action/state.
- sense-target-distinguishability: ONE SENSE — noun/verb and approval/rejection do not warrant splitting.
- family-definition-retest: 大休歇 and 休去歇去 remain related longer forms, not aliases that overwrite the headword.
- opening-interpretation-verdict: PASS.
- omission-audit: all seven witnesses and the explicit exclusion remain.
- plain-english-image-verdict: PASS.
- display-modifier-verdict: not applicable.

## 2026-07-14 shared-gate completion

- Expanded from seven to eleven stored anchors to preserve rather than delete the four prose families: 休去歇去, 休歇地, 休歇者, and 大休歇.
- The doubled 休去歇去 witness is explicitly marked family evidence because it does not contain the contiguous headword; it does not inflate primary depth.
- Exact actors are Zhenjing Kewen, Xisou Shaotan, Xiuyelin, and Yuanwu Keqin, with Dahui Zonggao retained as later raiser where appropriate.
- Final shared gate: 11/11 KWICs exact; zero attribution, dangling-quote, English-first, depth, or sense failures.
