# 法嗣 independent remediation — exact actor, depth, and definition (2026-07-13)

## Verdict

**PASS, with roster integration deferred to the separate roster project.** The article remains two-sense, but its evidence and opening are materially stronger. All sixteen stored occurrences now take exactly one actor branch: ten named exact actors, two fully reviewed unnamed non-master questioners, and four grammar-proved impersonal catalogue/classification rows. There are no bare nulls.

No merge, commit, or push was run.

## What the independent review changed

- Re-read all prior complete cases, `WORK.md`, `maintenance/cohort-a-last3-evidence-20260713.md`, and the current guide through Rule 19.
- Preserved the valid split:
  - **lineage heir** — the person selected, recognized, predicted, counted, questioned, or absent;
  - **lineage affiliation** — the succession relation/status that can be unknown, received from someone, predicated with a predecessor, or publicly challenged.
- Replaced the weakest part of the old opening with Qianyan Yuanzhang's direct corpus account: one person is picked from among thousands to make the transmission and seed the lineage, `如燈照燈、如水與水，故有法嗣之說`.
- Added Dahui Zonggao's complete Gaoting/Deshan unit. Dahui records Gaoting's later succession and then says accepting `法嗣德山` is `即未可`; the relation is therefore a public claim that the record can contest, not an automatic honorific.
- Preserved two useful named non-master witnesses rather than discarding them: Tang Shiji is the bylined memorial author who restricts Miyun Yuanwu's heirs to twelve; Shunzhi Emperor owns the direct `誰之法嗣` question. Miyun Yuanwu and Muchen Daomin are labelled as context, not substituted as actors.
- Corrected **Yexian Guisheng** to the roster spelling **Shexian Guisheng**.
- Added natural lookup coverage for `Dharma heir`, `Dharma successor`, `Dharma succession`, and affiliation/successor paraphrases without promoting those aliases into extra meanings.
- Applied compound isolation: `法嗣書` and `嗣法於` are labelled `family`; neither buys bare-法嗣 depth.

## Exact-actor audit

### Named actors

- Prajnatara recognizes Bodhidharma as the heir.
- Michaka predicts an heir.
- Qianyan Yuanzhang explains the selection category.
- Tang Shiji authors the memorial restriction to twelve heirs.
- Shunzhi Emperor asks whose heir Wunian was.
- Chuiwan Guangzhen cites Muzhou's having no heir.
- Wufeng Ruxue is the grammatical actor of `嗣法於天童密雲悟和尚` in both labelled family rows.
- Wuzu Fayan receives and raises the succession document at the teaching seat; the note separately names messenger Wenxiang's carrying action.
- Dahui Zonggao raises and adjudicates the Gaoting/Deshan succession case.

### Reviewed unnamed exceptions

- In *Expanded Collection of the Continued Transmission of the Lamp* (`增集續傳燈錄`), `先是有問公何不擇法嗣者` supplies only an unnamed questioner. Line, ±500/±2,000/±10,000 context, section, title, TEI header, and exact parallel search do not identify that participant.
- In *Tiansheng Expanded Lamp Record* (`天聖廣燈錄`), the complete Shexian Guisheng encounter and travelling parallels call the actor only `問`/`僧問`. Shexian is the named respondent, not the question's actor.

Both store the exact six ordered rungs, reviewer, timestamp, kind, label, and role.

### Impersonal rows

Three person-sense catalogue headings and one relation-sense classification are nominal structures, not suppressed master turns. Each now records the precise grammar: counted genealogy heading, predecessor/heir heading, collateral-heir contents heading, or the nominal class `未詳法嗣者`. None uses `impersonal` as an anonymity escape.

## Definition and depth cross-check

The full concordance remains **10,572 hits in 168 allowlisted texts**. The person sense has **10 exact-headword witnesses plus one labelled family witness**. The relation sense has **3 bare-headword relation witnesses plus two labelled family witnesses**. This is evidence-class depth, not a flat quota: counted heading, recognition, prediction, explicit selection account, restricted count, direct question, absence, lay heir, collateral heir, unknown affiliation, received affiliation, challenged predication, document family, and separate inheritance-verb family are all represented.

The sense split survived falsification. A counted group of people cannot be glossed as an affiliation; conversely, `師今得法嗣何人`, `未詳法嗣`, and `法嗣德山` cannot all denote a person without changing their grammar. Noun/verb packaging alone did not create the split: the different referents did.

## Mechanical results

- JSON parse: pass.
- `PYTHONIOENCODING=utf-8 python3 zc_batch.py verify-entries`: **16/16 pass**, zero line drift.
- Source equality: **2/2 senses pass**.
- `audit_attribution.py`: **0 hard failures**; 10 named, 2 reviewed unnamed, 4 impersonal, 6/6 Chinese prose strings anchored.
- `audit_public_feedback.py`: **1/1 pass**, zero flags.
- `audit_depth_sense.py`: **0 hard failures, 0 review flags**.
- `run_cohort_gate.py --skip-packets`: **hard pass**, 16/16 exact KWIC checks.
- Forbidden English: none.

## Roster handoff

The current attribution auditor deliberately defers roster failures while the separate roster expansion is in flight. Seven occurrence rows currently use source-established names not yet present as `names[0]`: **Prajnatara, Michaka, Tang Shiji, Shunzhi Emperor, Chuiwan Guangzhen, and Wufeng Ruxue** (Wufeng occurs twice). These identities must be reconciled with the roster project before the deferred roster gate is re-enabled; they must not be nulled or replaced by nearby rostered people.

