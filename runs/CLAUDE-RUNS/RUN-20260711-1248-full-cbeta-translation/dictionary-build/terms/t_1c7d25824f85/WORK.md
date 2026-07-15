# WORK — 本來面目 (t_1c7d25824f85)

Zen-scoped concordance (462-text allowlist filter). Grep of `本來面目` over allowlist files:
**1320 hits / 264 texts** by the current apparatus-clean allowlist concordance — among the most pervasive question-phrases in the corpus. Saved KWICs are verbatim and independently verified.

## Concordance (key occurrences — verbatim)

| Chinese (verbatim) | CBETA id · lb | text | master / role |
|---|---|---|---|
| 盧曰：不思善，不思惡，正恁麼時，阿那箇是明上座本來面目？師當下大悟 | X81n1571 0214b14 | 五燈全書 | **Huineng** (Dayu Ridge origin) |
| 還我明上座本來面目來 (variant of the same story) | X81n1571 0520a12 | 五燈全書 | Huineng story (restated) |
| 如何是父母未生前本來面目？門云：今日汝且隨我往頂山寺… | J37nB399 0758a01 | 鶴林天樹植語錄 | public-question form 父母未生前 |
| 父母未生前本來面目 | J38nB429 0931b28 / J28nB213 0517b21 | multiple | raised question-phrase |
| 如何是本來面目？師閉目吐舌，又開目吐舌示之 | X81n1571 0352a07 | 五燈全書 | 覆船 (shown, not said) |
| 如何是本來面目？師曰：不行鳥道 | X66n1296 0146b22 | 宗門拈古彙集 | **Dongshan Liangjie** — “not traveling the bird's trackless flight-course,” inside the full travel/not-travel exchange |
| 三峰老和尚不許人提著本來面目…因什今日又要這上座明取本來面目？師曰：哀哉！法門不幸 | J34nB301 0317c12 | 南嶽繼起語錄 | prohibition and challenge |
| 見山是山，見水是水 …(answering 父母未生前本來面目) | J34nB304 0379c08 | 語錄 | public answer |

## Sense analysis (1 sense)
**SenseKey=null (corpus-wide)** — “original face.” The records repeatedly ask for it and preserve different attributed replies, gestures, corrections, and prohibitions. These are case-scoped responses to one question, not rival dictionary definitions and not different referents.

The phrase is **remarkably uniform** across the current 264-text concordance: the sampled records repeatedly ask for the same original face while varying the attributed response. No sampled witness establishes a second referent, so a single corpus-wide sense is the honest current structure.

## Multi-source verdict — PASS (overwhelmingly). Validation: multi-source.

## Honesty note captured in the entry
Nanyue Jiqi Hongchu records that Sanfeng Hanyue ordinarily did not permit people to mention the original face (不許人提著本來面目), then records a demand to clarify it and the answer “Alas! Misfortune for the teaching gate.” The passage supplies a prohibition and challenge; it does not state a motive for them and does not establish a rival referent.

## Honest thin spots
- Did not manually classify all 1,320 hits in 264 texts; the one-referent verdict rests on the representative, falsification-directed sample and remains reopenable if a different referent is found.
- Huineng story cited from the 五燈全書 (allowlist lamp compendium) rather than the 壇經 (title excluded from the Zen allowlist as a sutra); the phrase's multi-source status does not depend on any single witness.
- No ids/passages fabricated; every snippet read directly from the named allowlist file.

---
## GATE 2 (Claude adversarial verify+repair) — 2026-07-11
- **KWIC fixes (2):** both previously curated public-question KWICs had a fabricated trailing `。`. X81n1571 0214b14 `…泣禮數拜。` → source has `…泣禮數拜，問曰…`; trimmed to `泣禮數拜`. J37nB399 0758a01 `…門驀豎起拳。` → source has `…門驀豎起拳曰：「這樣大栗子…`; trimmed to `門驀豎起拳`. Both now exact contiguous. The J34nB301 prohibition KWIC was confirmed verbatim as-is.
- **Contamination:** NONE. All RelPaths (X81n1571, J37nB399, J34nB301, B27n0152, X80n1565) in allowlist; each contains 本來面目 (9/2/4/52/14×).
- **FromLb corrections (3):** 0214b14→0214b13, 0758a01→0757c30, 0317c12→0317c11 (were keyword lines; reset to KWIC-start line).
- **Multi-source:** single corpus-wide sense retained 3 independent verbatim witnesses at this historical gate (X81n1571, J37nB399, J34nB301). Validation was unchanged. The prohibition (三峰/繼起 不許人提著本來面目) was kept as a scoped observation rather than assigned an unstated purpose.
- **Over-read / nesting:** PreferredTarget "original face" literal; RelatedTerms (父母未生前, 不思善不思惡, 見性) are genuine collocated constituents. No changes.
- STATUS → verified.

## 2026-07-13 bird-course propagation adjudication

- inherited-family-lead: the 鳥道 public-feedback repair established its primary referent as a bird's trackless flight-course through open air, not a thin ground-road calque and not an untravelable route.
- exact-linked-case: `X66n1296:0146b18-b22` preserves Dongshan Liangjie's whole sequence: ordinary instruction to travel the bird course; rejection of the interlocutor's equation of that travel with the original face; the direct answer “not traveling the bird course.”
- propagation-verdict: **REVISE AND ANCHOR.** The old “not walking the bird's path” rendering is replaced with “not traveling the bird's trackless flight-course.” The whole exchange is stored under 本來面目 so neither the travel instruction nor the not-travel answer can be promoted to a universal definition.
- sense-target-distinguishability: **KEEP ONE SENSE.** Huineng's question, Dongshan's travel/not-travel exchange, Fuchuan Hongjian's tongue gesture, Qiyuan Xinggang's facial answer, Baichi Yuan's correction, Helin Tianshu Zhi's raised-fist encounter, and Nanyue Jiqi Hongchu's prohibition all concern the same original-face question. Different answers and actions are deployments, not different things.
- depth-after-propagation: seven exact-headword occurrences across six source files, meeting the 1,320-hit floor of seven without family evidence.
- attribution-after-propagation: all seven occurrences name the responsible speaker and source; every Chinese string retained in reader-facing prose is anchored by one of those exact occurrences.

### Item-11 inference ledger — Dongshan linked case

- observation: Dongshan first says that he ordinarily teaches people to travel the bird course and describes how; he rejects the question that equates traveling it with the original face; only then does he answer the direct original-face question “not traveling the bird course.”
- minimal-inference: “not traveling the bird's trackless flight-course” is Dongshan's answer in this exchange, not a lexical definition of 本來面目 and not a reversal of the corpus-wide 鳥道 definition.
- ordinary-bridge: traveling and not traveling are opposed predicates applied to the same course; the sequence of questions controls which claim Dongshan rejects and which answer he gives.
- falsification-searches: checked the full Dongshan context, the independent 鳥道 travel/no-track evidence, later comments on this exact exchange, and whether another referent of 鳥道 controls the passage.
- counterexamples: Dongshan's immediately preceding instruction to travel the bird course blocks a universal non-travel reading; other original-face replies use gestures, facial description, correction, and prohibition, blocking promotion of Dongshan's answer into the headword gloss.
- scope: Dongshan Liangjie's recorded exchange only.
- verdict: **direct** for the attributed answer; **reject** as a universal definition.

## Public-feedback gate record

- feedback-observations: the original face is repeatedly asked for and receives attributed words, gestures, corrections, and prohibitions; Dongshan's full exchange contains both travel and not-travel predicates for the corrected trackless bird-flight course.
- feedback-inference-verdict: licensed — retain “original face” as one question/referent, describe how the record tests replies, and keep Dongshan's answer strictly case-scoped.
- opening-interpretation-verdict: **pass after revision** — the opening identifies the recurring public question and the record's refusal to let one response become a universal definition before presenting named evidence.
- feedback-falsification-searches: direct-question forms, parent-before-birth forms, named replies and gestures, prohibitions, Dongshan's complete bird-course exchange, different-referent and master-specific-sense controls.
- feedback-counterexamples: varied answers do not split the referent; Dongshan's travel instruction defeats defining the original face as simple non-travel; Nanyue Jiqi Hongchu's prohibition prevents treating the phrase as an uncontested object.
- feedback-scope: one corpus-wide question/referent; every answer and correction remains speaker- and case-scoped.
- lookup-probes: `original face`, `your original face`, `face before your parents were born`, `original appearance`, `original countenance`, `original face case`.
- nested-compound verdict: **REVISE DISPLAY / KEEP FOR SEARCH.** “The face you had before your parents were born” belongs to the longer `父母未生前本來面目` formulation, so it is a retrieval alias rather than a displayed alternate translation of bare `本來面目`.

## Final propagation QA

- JSON and dependency schema parse: **PASS**.
- Exact `zc.verify` replay: **PASS 7/7**, including the full Dongshan sequence at `X66n1296:0146b18-b22`.
- `audit_depth_sense.py`: **zero hard failures**; seven exact-headword anchors across six source files meet the floor of seven. The one expected broad-single-sense review flag is adjudicated above: varied replies address one original-face referent rather than different things.
- `audit_attribution.py` in the two-entry propagation run: **zero hard failures**; this entry contributes 7/7 named occurrences and source-and-speaker notes, with every retained Chinese prose string anchored.
- `audit_public_feedback.py`: **PASS** in the 2/2 targeted run, zero flags.
- Forbidden-English check: **PASS**, zero reader-facing occurrences of the two prohibited labels.
- No merge or status change was performed.

## 2026-07-13 actor-pure supersession

The former seven rows all mixed questioner and respondent inside one occurrence while assigning the whole span to one person. They have been replaced or split so each stored row belongs to its named actor.

- **7 primary exact-headword anchors:** Huineng, Helin Tianshu Zhi, Nanyue Jiqi Hongchu, Huanglong Huinan, Tianru Weize, Shiche Tongcheng, and Baichi Yuan.
- **5 no-headword response/support rows:** Muyun Tongmen's raised fist; Nanyue's reply to the anonymous challenge; Qiyuan Xinggang's facial answer; Dongshan Liangjie's bird-course answer; Fuchuan Hongjian's tongue gesture. All five are `EvidenceRole=contrast` and do not buy depth.
- Dongshan's, Fuchuan's, and Fushi's headword questions belonged to anonymous or different named questioners, so they no longer masquerade as the respondents' exact turns.
- The Nanyue primary anchor is now his immediately preceding own statement, “go to the time before father and mother were born and clarify the original face,” rather than the anonymous challenge.
- Current inventory: **12 stored rows / 8 source files / 11 named actors**; depth **7 primary exact anchors**, meeting the floor of seven.
- Targeted QA: `zc.verify` **12/12**; attribution **12/12 named and noted, all Chinese prose anchors resolved, zero hard failures**; depth/sense **PASS**; public-feedback **PASS**.

## 2026-07-13 final complete-case correction

- The autobiography says that the future Qiyuan went to Jinsu to call on Shiche (`金粟參石車和尚`). The later `粟問` is therefore **Shiche Tongcheng's** question, not Doushuai Huocun's.
- The record's continuing autobiographical subject `師` is **Qiyuan Xinggang (祇園行剛)**, not the conflated label “Fushi Qiyuan”; Fushi is the monastery name in the book title.
- The actor-pure evidence split remains unchanged: Shiche's exact-headword question is primary, and Qiyuan's no-headword answer `眉橫鼻直` is contrast support.
