# WORK — 照用 (t_6b8e3b4f44bb)


## Public-feedback reconstruction ledger

- feedback-inference-verdict: PASS — For 照用, the displayed senses are illuminating and functioning; the definitions state only relations observed in the stored turns, contrasts, grammatical frames, and self-descriptions, without promoting an answer or symbolic association into the headword's meaning.
- feedback-observations: 7 exact headword/declared-variant occurrences across 5 source files support 1 different-thing sense(s); actor and source notes remain attached to every evidence row.
- feedback-falsification-searches: Re-tested literal versus Chan-loaded use, word versus title/person, corpus-wide versus master-specific scope, incompatible subject/event frames, and response diversity; only different referents or events justify the 1 retained sense(s).
- feedback-counterexamples: Negative, critical, quoted, narrated, and question-form witnesses were checked against the definition; differences of stance, answer, speaker, or grammar remain visible in evidence rather than being collapsed into an interpretive rule or inflated into polysemy.
- feedback-scope: multi-source; no master-specific sense. Corpus storage counts are concordance context, while the sense claims are limited to the exact witnesses and independent-work spread stored here.
- lookup-probes: illuminating and functioning; illumination and function; shining and acting; illumination and action; four illuminations and functions; simultaneous illumination and function; shining and functioning; to illuminate and to act. These probes cover ordinary English synonyms, word-order variants, and the principal Chan-facing retrieval wording without changing the displayed definition.
- opening-interpretation-verdict: PASS — A reader can identify illuminating and functioning from the PreferredTarget and opening sentence before counts, graph analysis, named examples, or source discussion.
**Gloss:** "illuminating and functioning" — head-term of Linji's 四照用 set.

## Concordance (zc)
- 照用: 1133 hits / 208 files. 照用同時 320, 照用不同時 188, 先照後用 191, 先用後照 185, 四照用 31.

## Sense analysis
ONE corpus-wide sense (null). The four-fold self-definition is Linji's 示眾, but every witness
is a raised/cataloged retelling (人天眼目 catalog, 五燈會元 lamp-record, later-master reuse) or
a memorial — Linji's own 語錄 T47n1985 carries only biographical 照用同時，本無前後 → MasterName
null throughout; 臨濟義玄 in RelatedMasters.

## Key describe-only findings
- Corpus supplies its own gloss (人天眼目, head 四照用): 照用同時 = 驅耕夫之牛，奪饑人之食…；
  照用不同時 = 有問有答，立主立賓，合水和泥應機接物.
- Frame reused verbatim by 慈明 (石霜楚圓): 慈明示眾云。有時先照後用… (T48n2006).
- Fielded live: 如何是照用同時？師喝，和聲便打 (J25nB171).

## Validation
multi-source. 5 curated occurrences, all zc.verify ok=True, lb-exact, allowlisted.
RelatedTerms: 四照用, 殺活, 賓主.

## 2026-07-14 fresh-build evidence and actor gate

- Expanded Linji Yixuan's defining witness through both ordering predicates and the complete simultaneous/different-time descriptions; the formerly dangling definition clauses now sit in one verified exact anchor.
- Added Tianyin Yuanxiu's explicit Four Illuminations and Functions family occurrence, marked `EvidenceRole=family` so the nested compound cannot buy bare-headword depth. Added Baiyan Jingfu's paired killing/giving-life and illumination/function list as another exact headword deployment.
- Corrected the preface row: Ma Fang is a personally named non-master preface author, not a roster master. He now uses the identified-non-master actor branch; Linji Yixuan is contextual person discussed.
- All named exact actors have `utterer` context links. Seven evidence rows pass exact `zc.verify`; all twenty-three Chinese prose strings are anchored and `audit_attribution.py --json` reports zero hard failures.
- Re-tested the definition and split: the four orderings and simultaneous/different-time modes organize one illumination/function pair; they are not four different things and remain one sense.
