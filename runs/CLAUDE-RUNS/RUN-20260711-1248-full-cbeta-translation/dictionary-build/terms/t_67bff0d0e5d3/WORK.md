# WORK — t_67bff0d0e5d3 · 僧問 · d001-B

## Definition and sense decision

One subject-plus-verb event formula, normally “a monk asked.” First mention versus already-known monk changes the English article, not the sense. Event frames (`因僧問`, `有僧問`, `時有僧問`, `住後僧問`) and a compiler's raised-case frame (`復舉僧問`) retain the same proposition; no heading/genre noun was created.

## Deployment inventory

- Gateless Checkpoint case opening.
- Linji interview answered by shout and repeated interview sequence.
- Modified questioner: a Flower Garland lecturer monk.
- `或有僧問` question about coming from the west.
- Retrospective `因僧問` around the flower case.
- Later teaching-seat raising of Baizhang's old exchange.
- Early Patriarchs' Hall question/answer.
- Street encounter.
- Post-residence biographical transition into recorded exchanges.
- `時有僧問` follow-up on the nondual gate.

Result: 1 sense, 11 occurrences, 9 source texts. The response varies across speech, shout, action, and refusal; those are deployments of the interview, not senses of the narrative formula.

## Verification

All 11 KWICs passed exact allowlisted `zc.verify` with matching lb spans. The original four evidence classes were preserved in shorter exact-headword witnesses and expanded without duplicate padding.

## 2026-07-13 retrospective exact-actor pass

- Re-read every complete encounter, section, title, and available parallel. The named respondents or later raisers are Zhaozhou Congshen, Linji Yixuan, Oxhead Zhiwei, Helin Xuansu, Yunfeng Wenyue, Mi'an Xianjie with Baizhang Huaihai, Qingyuan Xingsi, Budai Qici, Tianyi Yihuai, and Baiyan Fu.
- The exact actor encoded by `僧問` is the questioning monastic, not the respondent, title owner, or later commentator. All eleven selected questioners remain genuinely unnamed after the six-rung ladder. The rows therefore retain `MasterName: null`; assigning Zhaozhou, Linji, or another respondent would manufacture an exact-headword speaker and violate Rule 10.
- Every attribution note now names the exact source, states that the questioner is unnamed, names the respondent or later raiser, and records the failed ladder. `RelatedMasters` now inventories all named people actually used by the explanation.
- Rewrote the opening to name the respondent-side deployments without turning the anonymous questioner into “a master.” Retested the sense: case opening, biographical transition, raised old case, and live interview all remain one subject-plus-verb event formula.
- Exact KWIC replay remains 11/11 and `SourceTexts` equals the nine distinct occurrence paths. The attribution audit intentionally retains eleven null-master hard conditions; these are honest semantic exceptions, not unresolved title searches.

## Public-feedback gate record

- feedback-observations: Across lamp records, recorded sayings, and raised cases, the formula introduces a monastic questioner and a public response; every selected questioner is unnamed, while the respondents and later raisers are recoverable.
- feedback-inference-verdict: licensed — define the subject-plus-verb event formula and its public-interview hinge, while refusing to assign the anonymous questioner's words to the respondent.
- feedback-falsification-searches: first versus subsequent mention; modified questioners; live and raised cases; speech, shout, and action responses; heading-versus-event use; six-rung attribution search for every questioner.
- feedback-counterexamples: the Flower Garland lecturer narrows the subject without creating a new sense; Mi'an Xianjie's raised Baizhang case shows editorial deployment without turning the formula into a genre title; all eleven anonymous questioners defeat respondent-as-speaker attribution.
- feedback-scope: one corpus-wide grammatical event formula; response content and documentary position remain case-scoped.
- lookup-probes: `a monk asked`, `a monk asks`, `monk asked`, `the monk asked`, `a monk inquired`, `monastic questioner`.
- opening-interpretation-verdict: pass — the opening states in English what the formula reports and how Chan records use it before giving named cases.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 11/11, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-sengwen-gate.json`.
