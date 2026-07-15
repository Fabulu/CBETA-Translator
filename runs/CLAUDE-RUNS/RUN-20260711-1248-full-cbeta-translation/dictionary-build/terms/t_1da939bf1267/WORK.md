# WORK — t_1da939bf1267 · 呵佛罵祖 "revile the buddhas and curse the patriarchs"

**Author:** Frizzle · **Gate 2:** Fable (verify-and-repair) · **Status:** verified · **Senses:** 1 (corpus-wide) · **Validation:** multi-source

## Concordance (Zen-scoped, zen-corpus.json allowlist, 462 texts; main body text, notes/apparatus excluded)
- 呵佛罵祖 — 232 hits / 118 texts (headword) [Gate-2 re-verified exact]
- 訶佛罵祖 — 33 / 24 (訶 graphic variant) [re-verified exact]
- 呵佛叱祖 — 4 / 4 (叱 variant; ALL are the 楊岐一語 line: T47n1994A, C077n1710, D48n8939, X78n1556) [re-verified]
- 罵祖呵佛 — 2 / 2 (inverted); 罵佛呵祖 — 1; 訶佛叱祖 / 呵佛喝祖 — 0 [re-verified]

## Sense
One corpus-wide sense. No master bends it to a private meaning; it is a descriptive idiom
deployed across the record. Literal graphs: 呵/訶 scold + 佛 + 罵 curse + 祖.

## Deployment range (all grep-verified at Gate 2)
1. Epithet for a master's conduct, esp. 德山: 上堂德山呵佛罵祖承其言者多見德山者少 (真淨克文 上堂);
   paired with Linji's shout 德山呵佛罵祖臨濟喝 — same passage in C- and D-canon editions of 古尊宿語錄.
2. Master's own record of himself: 楊岐一語。呵佛叱祖。明眼人前。不得錯舉 (Yangqi's record; source punctuation).
3. Answer to a question: 如何是徑截法門 → 訶佛罵祖去; 如何是主中主 → 訶佛罵祖.
4. Conduct-label: 和尚終日訶佛罵祖 (J26nB180, attendant to the dying 真點胸); 據猊床，而訶佛罵祖 (J28nB203, exact span — Gate 2 replaced the drafted ellipsis form 據猊床…訶佛罵祖).
5. In 玉林通琇's reply letter: 我輩不能于尊宿林林之日向孤峰絕頂蟠結草庵呵佛罵祖去迺以鷄肋之力撑持大廈之危辯魔斥異誠大不幸 (full exact span, no ellipsis).
6. Gloss-by-reply (in-corpus definition): 訶佛罵祖是甚麼人 → 闡提漢 (an icchantika).

## Contrasts the texts draw
- 辨魔揀異，須是頂門具眼；訶佛罵祖，還他腦後見腮 (2 texts: J29nB235 0399c02, X85n1590) [re-verified]
- set beside 辯魔斥異 inside the B27n0152 letter span

## Attribution decisions (Gate 2 corrections marked ✱)
- 楊岐方會 (roster) speaks the 呵佛叱祖 line in his OWN record (T47n1994A; cb:mulu 袁州楊岐山普通禪院會和尚語錄). ATTRIBUTED.
- ✱ C077n1710 0924a20: cb:mulu = 雲庵真淨禪師語錄一 (卷43) → the 上堂 speaker is 真淨克文 (ON ROSTER).
  Draft had MasterName null ("later 上堂 master"); Gate 2 attributed. 德山宣鑒 remains the SUBJECT only.
  The 德山呵佛罵祖臨濟喝 passage (0939a05) is 真淨's 卷44; "2 texts" = C+D editions of the SAME compilation — wording made precise.
- ✱ B27n0152 0627a18: section 復寶華朝和尚 — the incoming letter is the inline 附原書 <note>; the main-text
  passage is 玉林通琇's own reply (duplicate at 0656a20 opens 師復書云). Draft had null ("epistolary");
  Gate 2 attributed to 玉林通琇 (ON ROSTER, roster spelling 玉林 not 玉琳).
- J-canon answers: masters 大溈五峰學 / 蓮月 / 三山燈來 are Ming–Qing, NOT on roster → null.
  TRAP AVOIDED: roster "五峰" is the Tang master, ≠ 大溈五峰學 (Wufeng Ruxue). Did not cross-attribute.

## Gate 2 (Fable) verification log
- All 6 KWICs re-derived EXACT CONTIGUOUS against the cited files (notes/apparatus stripped); FromLb re-derived
  = nearest preceding lb (canon edition); ToLb corrected to the lb containing the KWIC end (3 were same-line, 3 spanned lines).
- Zero contamination: every RelPath and SourceText in zen-corpus.json (re-checked).
- Explanation quote repairs: 楊岐 line restored to source punctuation (。not ，); 據猊床…訶佛罵祖 ellipsis
  replaced by exact 據猊床，而訶佛罵祖; letter aspiration replaced by the full exact span; 終日訶佛罵祖
  anchored to its single witness (J26nB180) and quoted as 和尚終日訶佛罵祖.
- SourceTexts extended with the quote witnesses J26nB180, J28nB203, X85n1590 (all allowlist).
- No interpretation found to delete; entry stays describe-only.

## Files
entry.v2.json (6 curated occurrences), STATUS=verified, WORK.md

## Evidence-role and attribution repair (2026-07-13)

- Before: 2 exact-headword witnesses and 4 unlabelled graphic/verbal variants. After: 6 exact-headword witnesses across 4 exact-bearing sources plus all 4 variants retained as `family` evidence.
- Added exact deployments from Guishan Lingyou, Baoning Renyong, and two distinct Zhenjing Kewen addresses. The Ming/Qing speakers formerly nulled for roster absence are now preserved under stable names; roster reconciliation remains deferred.
- Definition/item-8 retest: exact and variant forms all name the same conduct. Subject, praise, rebuke, question-answer use, and self-description are readings and evaluations, not different things.

## Semantic remediation r001

- observation: six exact anchors and four labelled graphic/verbal variants use the phrase as conduct label, question answer, prediction, self-description, criticism, or positive appraisal.
- minimal-inference: Chan records turn literal verbal attack on buddhas and patriarchs into a recurring public conduct label; evaluation varies and is not part of the definition.
- ordinary-bridge: “revile” and “curse” name the two audible verbal acts directly, while the objects identify the figures attacked.
- falsification-searches: checked exact and alternate graphs, rebuke/curse substitutions, inverted forms, Deshan attributions, self-use, public answers, praise/rebuke, and the icchantika reply.
- opening-interpretation-verdict: keep after revision. It now states the actionable English meaning and observable Chan deployment before frequency and named examples.
- search-recall: approved aliases are revile buddhas and curse patriarchs, scold buddhas and patriarchs, curse buddhas and patriarchs, and abuse buddhas and patriarchs.
- rejected-inference: approval, iconoclasm, or a general license to abuse were rejected; the entry reports attributed conduct and case-local appraisals only.
- nested-quote-ledger: all reader-facing evidence is anchored by exact or explicitly labelled family rows; graphic variants cannot buy exact-headword depth.
- attribution-ledger: all ten exact voices are named; Deshan remains subject where Zhenjing Kewen owns the speech, and Yulin's reply remains Yulin's.
- family-ledger: 訶佛罵祖, 呵佛叱祖, and inverted forms are graphic/verbal family evidence for the same conduct but remain outside exact depth.
- independent-falsification-verdict: keep one sense. Praise, rebuke, prediction, answer, and self-description are readings of the same act.
- feedback-inference-verdict: KEEP — recurrent public conduct-label deployment is licensed; no hidden virtue or universal rule is asserted.
- feedback-observations: Deshan labels, solitary-peak predictions, great-work appraisal, self-use, variants, and question answers are anchored.
- feedback-falsification-searches: searched literal attack, evaluative polarity, actor/subject attribution, graph variants, inverted forms, and incompatible referents.
- feedback-counterexamples: positive and negative appraisals block either a uniformly praised or uniformly condemned definition while preserving one action.
- feedback-scope: one corpus-wide conduct phrase with case-specific speakers and evaluations.
- lookup-probes: revile buddhas and curse patriarchs; scold buddhas and patriarchs; curse buddhas and patriarchs; abuse buddhas and patriarchs.
- plain-english-image-verdict: PASS — the opening no longer begins with “literally” and immediately tells the reader what the conduct is.
