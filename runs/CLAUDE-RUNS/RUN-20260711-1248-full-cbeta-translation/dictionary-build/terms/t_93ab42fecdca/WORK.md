# WORK — t_93ab42fecdca · 本來無一物 "from the origin, not one thing exists"

**Author:** Frizzle · **Gate 2:** Fable (verify-and-repair) · **Status:** verified · **Senses:** 1 (corpus-wide) · **Validation:** multi-source

## Concordance (Zen-scoped; Gate-2 re-measured, exact match with draft)
- 本來無一物 — 214 hits / 106 texts (headword) [re-verified exact]
- 本無一物 — 34 / 30 (contracted variant) [re-verified exact]
- 本來面目 — 1320 / 264 (related term, task-specified cross-ref) [re-verified exact]

## Origin + a hard textual fact (re-verified at Gate 2)
The line is the 3rd of Huineng's verse in the RECEIVED (宗寶) Platform Sutra
(六祖大師法寶壇經, T48n2008 0349a07): 菩提本無樹，明鏡亦非臺；本來無一物，何處惹塵埃 (text writes 惠能偈曰).
- **The phrase is a feature of the LATER text.** The Dunhuang 壇經 (T48n2007 0338a08) gives the verse with the
  3rd line 佛性常清淨，何處有塵埃 — WITHOUT 本來無一物. (Quote grep-verified in the allowlist file; reported for
  contrast only, NOT an occurrence — Gate 2 removed T48n2007 from SourceTexts since it does not attest the headword.)
- **Couplet variants across lamp records** (all grep-verified): 何處惹塵埃 (法寶壇經) / 何處有塵埃 (祖堂集 — the
  verse appears TWICE there, first lines 菩提本無樹 and 身非菩提樹; enrichment added at Gate 2) /
  本來無一物何假拂塵埃 with 1st line 菩提本非樹 (傳燈玉英集 0167a07).

## Deployment beyond the verse (grep-verified)
- Dongshan's verdict (2 independent witnesses): 洞山答曰：「直道本來無一物，也未得衣缽在。」 (祖堂集 0416b11;
  quote now carries the source's own brackets so it is exact-contiguous);
  師有時垂語曰直道本來無一物猶未消得佗鉢袋子 (傳燈玉英集 0236b12).
- Huangbo (宛陵錄, twice: 0633b04 + 0636c03): quotes 本來無一物何處有塵埃; to 本來無一物無物便是否 answers 無亦不是.
- Raised stock tag: 舉六祖云本來無一物長慶云萬象之中獨露身 (B27n0152 0563b02; Gate 2 removed the draft's
  inserted comma — the source has none).

## Attribution decisions (all confirmed at the cb:mulu at Gate 2)
- 慧能 (roster; text spells 惠能): the verse in T48n2008 (行由第一) + the same-episode variant in 傳燈玉英集
  (cb:mulu 三十二祖弘忍大師 — the episode narrative sits in Hongren's chapter, but the verse is written by 能
  in the narrative; kept 慧能 as the verse's speaker, per the task rule "received Platform Sutra verse → Huineng").
- 洞山良价 (roster): named speaker of the verdict; cb:mulu 洞山和尚 (祖堂集) / 筠州洞山良价禪師 (傳燈玉英集).
- 黃檗希運 (roster): cb:mulu 黃檗斷際禪師宛陵錄 inside 古尊宿語錄.
- Later raising (B27n0152, 長慶 pairing) → MasterName null per the raised-case rule.

## Gate 2 (Fable) verification log
- All 6 KWICs re-derived EXACT CONTIGUOUS; FromLb re-derived; ToLb corrected to end-lb (all 6 span two/three lines).
- Zero contamination: all RelPaths + SourceTexts in zen-corpus.json; SourceTexts pruned of T48n2007 (no headword there).
- Quote repairs: 舉六祖云… comma removed; 洞山答曰 quote bracketed to the exact source span.
- Enrichment added: 祖堂集 double attestation of the verse; Huangbo's two couplet quotes located.
- No interpretation found to delete; entry stays describe-only.

## RelatedTerms
本來面目 (task-specified), 何處惹塵埃, 菩提本無樹, 明鏡亦非臺, 本無一物.

## Files
entry.v2.json (7 curated occurrences), STATUS=verified, WORK.md

## Retrospective public-feedback and falsification gate
- feedback-observations: A word-for-word fragment such as “originally, not one thing” does not by itself tell an English reader that the line makes an exhaustive denial, belongs to Huineng's received verse, or is repeatedly tested rather than merely recited.
- minimal-inference: The grammar supports “originally, there is not a single thing.” Dongshan Liangjie's and Huangbo Xiyun's replies prove that the records treat the line as a claim open to public challenge. No metaphysical system is inferred from the phrase alone.
- ordinary-bridge: “Not a single thing” is the ordinary exhaustive English construction; “originally” locates that denial at the root or beginning.
- feedback-falsification-searches: Rechecked the exact headword, contracted `本無一物`, received and Dunhuang Platform-Sutra readings, lamp-record couplet variants, Dongshan verdicts, Huangbo question frames, and later `舉六祖云` raisings. Tested whether the phrase names a person/title, a concrete missing object, or more than one lexical thing; no second referent emerged.
- feedback-counterexamples: The Dunhuang verse lacks this phrase and reads `佛性常清淨`; therefore the later received wording is not projected backward as a universal original text. Huangbo's `無亦不是` and Dongshan's robe-and-bowl verdict prevent the article from turning the line itself into a sufficient doctrinal answer.
- feedback-scope: The definition is grammatical and corpus-historical. It does not decide whether the denial is ontological, pedagogical, rhetorical, or a final statement of realization.
- verdict: KEEP one verse-line sense, revise the display English to a complete statement, add the independently worded Patriarchs' Hall verse anchor, and name Yulin Tongxiu as the later quoter.
- feedback-inference-verdict: ACCEPT the exhaustive-denial grammar and the public-testing deployment; REJECT a universal metaphysical paraphrase.
- lookup-probes: `originally not one thing`; `not a single thing`; `nothing from the beginning`; `fundamentally nothing`; `originally nothing`.
- opening-interpretation-verdict: PASS after revision. The first sentence now tells a reader what the words state before giving textual history.
- plain-english-image-verdict: PASS. This is a proposition rather than a physical image, and its ordinary denial frame is explicit.
- nested-compound-verdict: PASS. Contracted `本無一物` is labelled as a variant; `本來面目` remains only a related term and contributes no meaning to this headword.
- verb-frame-verdict: PASS. Quoting, saying, asking whether, and raising all take the same proposition as their object; no incompatible referent appears.
- family-definition-retest: PASS against `何處惹塵埃`, `菩提本無樹`, `明鏡亦非臺`, `本無一物`, and `本來面目`. Only the first four belong to the verse/variant family; `本來面目` is a related but distinct expression.
- sense-target-distinguishability: KEEP one sense. Received variants, contracted wording, quotation, and criticism concern the same proposition rather than different things.
- omission-audit: Seven anchors now cover the received verse, two independently worded lamp variants, Dongshan's verdict in two sources, Huangbo's explicit counterquestion, and Yulin's later paired raising. The Dunhuang non-occurrence remains contrast evidence only.
