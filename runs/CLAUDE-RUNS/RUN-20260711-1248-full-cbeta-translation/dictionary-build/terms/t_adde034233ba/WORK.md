# 即心是佛 (also 即心即佛) — WORK

**Id:** t_adde034233ba · attributed to Mazu Daoyi

## Concordance (Zen allowlist only; verbatim)
Corpus-wide the phrase is everywhere: **即心是佛 = 110 allowlist texts / 321 hits; 即心即佛 = comparably widespread.** Not remotely single-source. Selected defining hits:

| hit (verbatim) | CBETA | lb | master |
|---|---|---|---|
| 馬祖因大梅問。如何是佛。祖云。即心是佛。 | T48n2005 (無門關) | 0296c28 | Mazu Daoyi |
| 若求作佛，即心是佛；若欲問道，無心是道。 | B25n0144 (祖堂集) | 0364b10 | Sikong Benjing |
| 如今和尚亦言即心是佛，諸方尊宿亦言即心是佛，那得有異？ | J10nA158 | 0048a17 | Nanyang Huizhong |
| 大梅問馬祖：『即心是佛。』他便信得及，後來住山三十年 | J27nB198 | 0464c10 | Damei Fachang |
| 唯此一心更無微塵許法可得。即心是佛…向外求佛 | T48n2012A (傳心法要) | 0380a14 | Huangbo Xiyun |
| 馬祖因僧問…為什麼說即心即佛？…為止小兒啼…非心非佛。 | J34nB306 | 0427a10 | Mazu Daoyi |
| 大梅云：任他非心非佛，我只即心即佛。 | J38nB408 | 0274c21 | Damei Fachang |
| 南泉示眾…馬大師說即心即佛…不是心，不是佛，不是物 | J28nB211 | 0456a27 | Nanquan Puyuan |
| 若言即心即佛，權且認奴作郎 | J23nB134 | 0545c04 | Yunmen Wenyan |

## Earlier sense analysis, now superseded

- The draft separated a corpus-wide affirmation from a Mazu-specific withdrawal sequence. L003-A reopened this under item 8: affirmation, withdrawal, Damei's refusal, Nanquan's extension, and the stock warning are recorded stances surrounding one lexical statement, not different objects. The final entry therefore has one corpus-wide sense.
- The graph variants 是 and 即 are interchangeable in the corpus; their wording does not create a split.
- The J23nB134 servant/master line occurs inside Yunmen's `室中語要` under repeated `師有時云`; it is linked to Yunmen Wenyan and curated because the Explanation uses it.

---

## GATE 2 (Claude adversarial verify + repair) — STATUS: verified

- **All 9 occurrence KWICs verbatim exact-contiguous** in their cited files (confirmed by targeted per-file search; the only breaks in raw contiguity were `<lb/>` line-break whitespace, which tag-stripping removes). Zero ellipses.
- **No contamination:** all RelPaths (T48n2005, B25n0144, J10nA158, J27nB198, T48n2012A, J34nB306, J38nB408, J28nB211, J23nB134, C077n1710) are in zen-corpus.json.
- **Multi-source honest:** sense 1 (Mazu/Huizhong/Damei/Huangbo, ≥4 texts) and sense 2 (Mazu/Damei/Nanquan + stock, ≥3 texts) both hold ≥2 independent witnesses → multi-source retained.
- **Repairs made:**
  1. Explanation sense 1 misquoted Huangbo as `除此心外，終無別佛可得` — NOT in T48n2012A. Replaced with the verbatim line `心外更無別佛` ('outside this mind there is no other Buddha').
  2. AttributionNote on the T48n2005 occurrence wrongly cited the "Blue Cliff Record (碧巖錄)". T48n2005 is the **無門關 (Gateless Barrier)**; the passage is case 30 (即心即佛). Corrected.
- RelatedTerms are genuine semantic cross-refs (即心即佛 variant, 非心非佛 counterpart, 無心是道 paired, etc.), not coincidental prefixes — left as is.
- FromLb anchors spot-checked against the source; all resolve to the passage.

## Gate-3 REVISE fix (2026-07-11 17:47 +0200)
Fixed the B25n0144 (祖堂集) 中使楊光庭 exchange misattribution flagged by Gate-3 (Fable).
Verified against source C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5\B\B25\B25n0144.xml:
chapter head at lb 0364b02 = "司空山本淨和尚嗣六祖"; commissioner 楊光庭往司空山 at 0364b05;
KWIC pairing (若求作佛，即心是佛；若欲問道，無心是道) sits at 0364b10 inside the Sikong Benjing chapter.
Changes (sense 1):
- Occurrence 2 (B25n0144) MasterName: "Nanyang Huizhong" -> "Sikong Benjing"; rewrote its AttributionNote (was "The National Teacher (國師)..."; Benjing was NOT 國師).
- Explanation: "Nanyang Huizhong pairs it, 若求作佛即心是佛..." -> "Sikong Benjing pairs it, ...; while Nanyang Huizhong notes 諸方尊宿亦言即心是佛 (...)". Huizhong retained for the genuine J10nA158 disputation citation (諸方尊宿 line), which is unchanged.
- RelatedMasters: added "Sikong Benjing" (Huizhong kept, justified by J10nA158).
All 9 KWIC strings unchanged (verbatim). Validation stays multi-source. STATUS=verified.

## L003-A item-8 ledger and full retest (2026-07-13)

- sense-target-distinguishability: MERGE “this very mind is Buddha” and the former “provisional device later withdrawn” sense. Mazu's “to stop a small child's crying,” his subsequent “not mind, not Buddha,” Damei's refusal, Nanquan's extension, and the servant/master warning are explanations, responses, and stances toward the same lexical phrase; none denotes a different referent.
- Family retest: the variants “this very mind is itself Buddha” (即心即佛), “mind itself is Buddha” (心即是佛), and “one's own mind is Buddha” (自心是佛), together with “not mind, not Buddha,” “not mind, not Buddha, not a thing,” “no-mind is the Way,” and “see nature and become Buddha,” now coexist as linked phrases without being promoted into rival senses.
- Definition retest: the clean target remains the literal statement “this very mind is Buddha.” Mazu's recorded reason, the later withdrawal, Damei's answer, Nanquan's wording, Huangbo's outward-seeking context, and the stock warning are all reported and anchored without deciding which stance governs the phrase.
- #0g retest: the phrase is deployed as a public answer to “what is Buddha?”, circulated across masters, paired with another formula, challenged, withdrawn, reaffirmed, and warned against. That observable public record is the Zen deviation; no outside Buddhist framework or interpretation menu is imported.
- Depth decision: all nine exact anchors across nine source texts remain because each supports a distinct deployment—stock case, paired formula, corpus-wide circulation, Damei reception, Huangbo context, Mazu explanation and withdrawal, Damei refusal, Nanquan extension, and recurrent warning. The 388-hit floor is six; nine witnesses are warranted by deployment diversity, not quota padding.

## 2026-07-13 attribution/depth remediation

- Corrected the stale ledger: the earlier nine occurrences contained 5 exact-headword and 4 family/contrast witnesses, not nine exact anchors. The rebuilt entry has 6 exact-headword witnesses across six sources, meeting the 388-hit floor, plus 6 explicitly marked family/contrast witnesses.
- Added an independent exact witness from `傳燈玉英集（殘卷）`; added anchored Huangbo witnesses for the prose variants `心即是佛` and `自心是佛`. Family and contrast material remains visible but buys no depth.
- Attribution audit: every occurrence now names the exact `zc.title` source and the exact or explicitly unidentified speaker; all 12 KWICs verify at exact `FromLb`/`ToLb`. All 23 Chinese prose strings are anchored.
- Definition/sense re-test: KEEP one sense. The added evidence confirms variants, quotation, withdrawal, refusal, and warning as different deployments/readings of the same statement, not different things. The literal English-first target remains clean.

## 2026-07-13 semantic peer repair

- Replaced the synthetic non-person `MasterName` for J10nA158 with Miyun Yuanwu, the attributable reporter/deployer. The AttributionNote explicitly preserves that the quoted words are spoken by an unnamed monk in the Nanyang Huizhong exchange.
- Confirmed J23nB134 in Yunmen's own section, set the explanation-used witness `Curated: true`, and corrected the stale stock-line ledger.
- Tightened `權且認奴作郎` to “temporarily taking a servant to be his lord.” All twelve AttributionNotes are English-first and retain the exact Chinese `zc.title`.


## 2026-07-14 semantic remediation (r001 owner 2)

- research-paths: apparatus-clean `zc.count`; the existing full-concordance, definition-formula, collocation, and deployment inventory above; and exact `zc.verify` replay of every stored occurrence.
- corpus-count-refresh: 388 hits across 120 allowlisted files.
- observation: T/T48/T48n2005.xml#0296c28, B/B25/B25n0144.xml#0364b09 anchor the defining predicates and distinct deployment classes summarized above.
- minimal-inference: This very mind is Buddha is a stock answer to the public question “what is Buddha?” and a statement that later masters quote, qualify, counter, and test.
- ordinary-bridge: graph/scene layer = this very mind is Buddha; ordinary referent = a declarative answer; Chan deployment = public formula, counterformula, and test.
- falsification-searches: rechecked literal uses, definition formulas, longer compounds, grammatical role changes, incompatible predicates, alternate referents, and linked family terms.
- counterexamples: ordinary, family, title, and compound uses were retained only at their demonstrated scope; none was allowed to lend an unanchored sense to the headword.
- scope: corpus-wide unless a retained sense explicitly names a narrower set or local definition.
- verdict: licensed — the opening is the smallest reproducible inference from stored predicates and assigns neither outside symbolism nor speaker intention.
- search-probes: this very mind is Buddha / mind itself is Buddha / the mind is Buddha / this mind is Buddha. These are retrieval metadata, not extra interpretation menus.
- nested-compound-verdict: longer compounds were inventoried and do not buy the bare headword's meaning or depth.
- verb-frame-verdict: governing predicates were re-clustered; the retained split/merge follows referent identity rather than noun/verb packaging, role, or favorable/hostile reading.
- sense-target-distinguishability: ONE SENSE — grammatical roles, appraisals, and alternate phrasings do not establish another referent.
- display-modifier-verdict: not applicable; the visible targets make no unsupported construction-material claim.
- family-definition-retest: related and overlapping entries named in the prior inventory were compared; no retained definition requires one witness to mean incompatible things.
- opening-interpretation-verdict: PASS — T/T48/T48n2005.xml#0296c28, B/B25/B25n0144.xml#0364b09 license the reader-ready opening at the stated scope; literal/family counterexamples narrow rather than defeat it.
- omission-audit: every unique prose claim remains anchored or explicitly tied to a recorded count/collocation; no useful quotation was deleted.

### Prescribed public-feedback ledger keys

- feedback-inference-verdict: LICENSED — the reader-facing opening is the least conclusion that makes the stored predicates and deployment classes intelligible; no outside doctrine, symbolism, psychology, or intention is imported.
- feedback-observations: T/T48/T48n2005.xml#0296c28, B/B25/B25n0144.xml#0364b09; the full occurrence/deployment inventory above supplies the remaining observations.
- feedback-falsification-searches: literal/ordinary uses; definition formulas; incompatible predicates; longer nested compounds; alternate referents; titles/persons; and linked family entries were rechecked against the allowlisted concordance.
- feedback-counterexamples: ordinary and compound uses remain at their attested scope and were not allowed to manufacture a headword sense; any retained second sense has its own exact-headword witness.
- feedback-scope: corpus-wide unless a sense target and its anchors explicitly identify a named set, local equation, title, object, or institutional referent.
- lookup-probes: this very mind is Buddha / mind itself is Buddha / the mind is Buddha / this mind is Buddha.
- plain-english-image-verdict: PASS — each opening names the referent before frequency, graph parsing, or quotations; concrete images retain the load-bearing ordinary scene.
