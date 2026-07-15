# WORK — 照用 (t_6b8e3b4f44bb)

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
