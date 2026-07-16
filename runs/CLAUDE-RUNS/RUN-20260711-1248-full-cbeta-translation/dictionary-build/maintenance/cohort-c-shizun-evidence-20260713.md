# Cohort C Agent 2 evidence report — 世尊

Date: 2026-07-13

Entry: `terms/t_c875e45fbb9d/entry.v2.json`

Agent scope: rebuild the prepared 世尊 calibration entry under the invoked-Zen-figure rule, split complete cases into actor-pure evidence, anchor all claims, run full gates, and do not merge.

## Outcome

The entry now defines the World-Honored One through recurrent Chan deployment rather than title etymology or an external life story. Its opening identifies the invoked Buddha as a public actor who holds up and entrusts the flower, mounts and leaves the teaching seat, pauses and answers in an interview, points, displays, commands, and is later quoted, challenged, compared, or re-performed by named masters.

The old draft had 10 mixed occurrences: 8 null `MasterName` rows and 2 named later-master rows. Several null rows combined the World-Honored One, Mahakasyapa, Manjusri, an outsider, Yunmen, Indra, or Wangming in one KWIC. The rebuilt article has **19 actor-pure occurrences**:

- **11 primary** exact World-Honored-One actor/speaker anchors;
- **8 contrast/support** anchors owned by Mahakasyapa, Manjusri (2), Yunmen Wenyan, Wangming Bodhisattva, Indra, Zhenjing Kewen, and Zhanran Yuancheng;
- **0 family** rows;
- **8 source files total**, with **6 independent primary source texts** counted by the depth gate.

No respondent, commentator, or later re-raiser is placed in `MasterName` for the World-Honored One's turn. No later quotation buys primary depth.

## Required core families

All assignment-mandated families are represented:

1. **Flower / Mahakasyapa / entrustment:** explicit Shakyamuni section of `五燈會元`, split into the Buddha lifting the flower, Mahakasyapa smiling, and the Buddha's entrustment speech.
2. **Mounting and leaving the seat:** `敏樹禪師語錄`, split into the Buddha mounting, Manjusri striking the block and announcing, and the Buddha leaving.
3. **Silence with the outsider:** `宗門拈古彙集`, with the Buddha's pause and later fine-horse answer stored separately; the unnamed outsider remains case context and is not falsely linked.
4. **Birth declaration plus Yunmen:** `浮石禪師語錄`, split into the Buddha's gesture/declaration and Yunmen Wenyan's separately attributed violent response.
5. **Later direct re-raising:** Zhenjing Kewen's flower/staff/table action and Zhanran Yuancheng's seat/silence comparison are both `contrast`, not Buddha-primary evidence.
6. **Additional public acts:** command/failure/success in the woman-in-composure case; ground-pointing and Indra's blade of grass; color-changing-jewel display and question.

## Prepared workbook disposition

All ten 世尊 workbook rows were reviewed in complete context:

- row 1 flower case: replaced by clearer explicit-section parallel and split into 3 actors/turns;
- row 2 teaching-seat case: split into 3 actors/turns;
- row 3 outsider case: split into 2 retained Buddha turns; unnamed outsider preserved in context only;
- row 4 Zhenjing: retained as later-speaker contrast;
- row 5 Zhanran: retained as later-speaker contrast;
- row 6 birth case: split into Buddha primary plus Yunmen contrast;
- row 7 later birth/Yunmen transmission: excluded as duplicate padding after row 6 supplied clearer actor-pure anchors;
- row 8 woman-in-composure: split into 4 actors/turns;
- row 9 ground/grass-temple: split into Buddha primary plus Indra contrast;
- row 10 jewel display: retained as Buddha primary; unnamed/group king answers remain full-case context.

The detailed row-by-row record, definition searches, inference ledger, search probes, omission audit, and public-feedback ledger are in `terms/t_c875e45fbb9d/WORK.md`.

## Sense decision

The depth gate raised its expected manual flag, `broad-concordance-single-sense-review`, because the term has 8,093 matches in 406 files. The flag is adjudicated **keep one sense**:

- all retained episodes use the same title for the same invoked figure;
- different acts do not name different lexical things;
- plural `諸佛世尊` is a longer family frame and does not force a second singular headword sense;
- definition-form searches found general biography, plural grammar, label exchange, and longer secret-speech/not-speaking phrases, but no second independently attested referent for the headword.

The one-sense decision therefore passes item 8 while retaining enough depth for the broad concordance.

## Gate results

Final command-level results:

- JSON parse: pass.
- `zc_batch.py verify-entries`: **19/19 exact**, 0 failures.
- SourceTexts equality: pass.
- Role inventory: **11 primary / 8 contrast / 0 family**.
- Attribution/quote audit: **19 named exact actors, 19 source-and-actor notes, 0 hard failures**.
- Attribution roster status: 17 names are temporarily reported as `deferred_non_roster`; the gate intentionally defers roster expansion owned by the separate roster task. No name was invented to silence that report.
- Public-feedback/search/opening audit: **1 passing, 0 flagged**.
- Depth/sense audit: **0 hard failures**, 1 adjudicated broad-single-sense review flag.
- Forbidden reader-facing English: **0 matches**.
- Final cohort gate: **hardPass=true**, 19 exact KWICs, 0 exact failures.

Final cohort report: `/tmp/shizun-cohort-gate-final.json` (ephemeral command artifact). Durable conclusions are recorded here and in `WORK.md`.

## Merge status

No merge, commit, or push was performed.
