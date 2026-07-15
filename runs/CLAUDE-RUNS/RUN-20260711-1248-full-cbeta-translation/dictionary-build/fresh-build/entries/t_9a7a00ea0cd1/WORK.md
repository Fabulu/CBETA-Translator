# 一歸何處 — research and depth audit

## Concordance

- Refreshed `zc.count("一歸何處")`: 838 hits in 216 allowlisted storage files.
- Refreshed related forms: `萬法歸一` 955/236; punctuated `萬法歸一，一歸何處` 476/157; unpunctuated `萬法歸一一歸何處` 56/22; `如何是一歸何處` 7/6; `一歸何處者` 2/2.

## #0f inventory

- Definition formulas searched: `一歸何處者`, `所謂一歸何處`, `謂之一歸何處`, `名為一歸何處`, `喚作一歸何處`, `何謂一歸何處`, `如何是一歸何處`. No equation-style definition was found.
- Public-case history retained: Zhaozhou's Qingzhou cloth shirt weighing seven catties; Wenshu's Yellow River with nine bends; Baizhang Mingzhao's 'not one has failed to ask.' These show that the question is not only the Zhaozhou exchange.
- Later deployments retained: an entire address followed by looking left/right; a room test with 'Silla'; a later master supplying 'today is as hot as yesterday.'
- Structural decision: one corpus-wide null-key sense. Zhaozhou's famous answer is historical association, not a Zhaozhou-specific meaning of the question.
- Omission audit: all distinct answer/deployment types found are included. Numerous repetitions of the Zhaozhou case and late instructional framing were not duplicated.

## Verification

All 7 saved KWICs returned `zc.verify(...).ok == True`: the six inherited witnesses plus the added Zhongfeng Mingben assigned-saying witness. Line anchors came from the verifier. `zc.head` and `zc.title` were checked for every occurrence. Every SourceTexts value contains `一歸何處`.

## Semantic r003 remediation

- inherited-occurrence-ledger: KEEP all six exact witnesses and the single-sense decision; REVISE the graph-first opening and remove the off-roster pseudo-context link for an unnamed respondent.
- ordinary-scene: a return question requires both something returning and a destination. The immediately preceding clause supplies the subject—the one to which the ten thousand things return—then asks where that one itself returns.
- nested-compound-audit: the exact shorter question, the full two-clause question with and without punctuation, nominal questioner forms, and 'what is' question forms were compared. The full construction supplies antecedent context; it does not create a second headword sense.
- sense-target-distinguishability: KEEP one question. Zhaozhou's shirt, Wenshu's river bends, Baizhang An's reply, Silla, heat, silence, and later appraisals are different answers or deployments, not different referents.
- opening-interpretation-verdict: DIRECT. The repeated full construction establishes both the antecedent of 'one' and the question's stable syntax; attributed interviews establish its public-case history.
- observation: the two-clause question recurs widely; multiple named respondents answer it differently; Gaofeng Yuanmiao uses it as a whole address; Lanan Dingxu uses it in the chamber.
- minimal-inference: the headword is the stable second half of a two-part public question, with 'the one' referring back to the first half.
- ordinary-bridge: pronouns and reduced noun phrases take their antecedent from preceding discourse; a question remains the same question when different people answer it differently.
- falsification-searches: exact, punctuated and unpunctuated full forms, nominal forms, 'what is' forms, return-place overlaps, Zhaozhou shirt family, independent respondents, and definition formulas.
- counterexamples: no equation-style self-definition was found; multiple answers prevent equating the question with Zhaozhou's shirt or any single response.
- scope: corpus-wide stock public question.
- verdict: DIRECT.

- feedback-inference-verdict: REVISE — the old opening parsed graphs but delayed the crucial antecedent. The revision immediately states that this is the second half of the full ten-thousand-things question.
- feedback-observations: repeated full two-clause construction, independent respondents and answers, whole-address use, chamber-test use.
- feedback-falsification-searches: exact, full-form, punctuation, nominal, definition, answer-family, and independent-deployment searches were completed.
- feedback-counterexamples: divergent replies are answer history, not lexical senses; no source equates the headword with the seven-catty shirt.
- feedback-scope: one corpus-wide question.
- lookup-probes: where does the one return / all things return to one / ten thousand things return to one / Zhaozhou's seven-pound robe / one returns where.

## Fresh-build finalization

- Expanded English retrieval to return/go back phrasing and both seven-catty/seven-pound cloth-shirt searches.
- Standardized the historical respondents as Wenshu Yingzhen and Baizhang Mingzhao An across the paired entries.
- Replayed 7/7 exact KWICs; attribution/quotation audit has zero hard failures.
