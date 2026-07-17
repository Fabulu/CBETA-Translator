# Investigation next-300 — construction lane A

Corpus baseline: `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`

Lineage mutation is forbidden. Missing dictionary identities go only to
`fresh-build/pending-roster.json`.

## Durable progress

- Positions 1–5 authored from explicit, full-case decisions and compiled:
  `無生曲`, `回光返照`, `無生話`, `拈花微笑`, `瓊樓玉殿`.
- Decision packet:
  `maintenance/investigation-next300-lane-a-calibration5-explicit-decisions.json`.
- Batch compile receipt:
  `maintenance/investigation-next300-lane-a-calibration5-batch-compile.json`
  (`hardPass: true`).
- Cohort gate:
  `maintenance/investigation-next300-lane-a-calibration5-cohort-gate.json`
  (`hardPass: true`; 37/37 occurrences verify).
- Independent repeated-template audit:
  `maintenance/investigation-next300-lane-a-calibration5-template-audit.json`
  (`hardPass: true`).
- Dictionary-only pending identity added and verified: Xiyan Zonghui, sourced
  from `X/X82/X82n1571.xml`, lines `0073a13–0073a20`.
- Position 6, `當面蹉過`, is explicitly authored and compiled with six exact
  occurrences. Compile and semantic-template audits pass. Its focused cohort
  gate has one external-only failure: `Assets/Data/lineage-masters.json` became
  dirty during the run (pre-existing external worktree object
  `d0dd59471fbfecde043c138b143a147828c89633` versus HEAD
  `6e9f692fe752560c3890dea3ba5847a41874114d`); this lane did not edit it and
  must not repair it. Entry-local gates remain mandatory, and no cohort may be
  called finally hard-green until the roster owner resolves that state.
- Position 7, `大安樂`, is explicitly authored and compiled with six exact
  full-case occurrences. Every entry-local cohort gate and the rolling
  seven-entry semantic-template audit pass; only the same recorded external
  lineage worktree state prevents the aggregate gate's `hardPass` boolean.
- Position 8, `鐵漢`, is explicitly authored with two distinguishable senses:
  the recurrent human epithet and Zizhou Chuan's provisional personification
  of the monastery bell. Seven exact occurrences verify; compile, focused
  attribution, and the rolling eight-entry semantic-template audit pass.
- Position 9, `沒可把`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict attribution, and the rolling template
  audit pass. Guanxi Zhixian was absent from the display roster and was added
  only to `fresh-build/pending-roster.json` with exact evidence; no lineage
  file was touched.
- Position 10, `死水裏`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict attribution, and focused semantic-
  template audit pass. The only deferred name is the already evidence-bound
  Guanxi Zhixian dictionary identity.
- Position 11, `轉身處`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict attribution, and focused template audit
  pass.
- Position 12, `轉身句`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict attribution, and focused template audit
  pass. The sixth speaker was resolved from the raw TEI section heading as
  Zhantang Wenzhun; no anonymous-master shortcut remains.
- Position 13, `滿目青山`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict utterer attribution, and focused template
  audit pass; the headword-bearing monk's question is not misassigned to the
  responding master.
- Position 14, `禪病`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict attribution, and focused template audit
  pass. Dahui's corpus distinction between Chan and practitioners' diagnosed
  errors is explicit in the opening.
- Position 15, `面壁九年`, is explicitly authored with six exact witnesses.
  Compile, exact verification, strict utterer/context attribution, and focused
  template audit pass. Bodhidharma and Huike are structured case-figure links,
  never falsely assigned as headword utterers.

## Checkpoint 15 reseal

- Positions 1–15 are now hard-green as an explicit cohort: 15 entries, 16
  senses, and 107/107 exact occurrences verified.
- The nine entries formerly concentrated at the six-occurrence evidence floor
  each gained one independently read, headword-bearing full case. The combined
  depth audit now reports no batch-floor cluster and zero hard failures.
- `鐵漢`'s provisional bell sense is English-first throughout; `轉身句`'s new
  evidence uses corpus-nearer “not two” wording rather than imported framing.
- Strict utterer attribution and pending-roster linking pass with zero hard
  failures. Yuetang Daochang was added only to `fresh-build/pending-roster.json`
  from his explicitly headed Complete Five Lamps section; no lineage file was
  edited.
- Durable receipt:
  `maintenance/investigation-next300-lane-a-checkpoint015-receipt.json`.

## Resume point

- Position 16, `信心銘` (`t_a9c25ff38478`), passed the named-text deployment
  gate before authoring: Juelang Daosheng, Yongjue Yuanxian, Tianyin Yuanxiu,
  Wansong Xingxiu, Yongming Yanshou, and Zhongfeng Mingben actually quote,
  question, criticize, or deploy the text. Catalogue and heading containment
  were not used as sufficient evidence. Seven exact occurrences compile and
  pass strict attribution and focused template checks.
- Position 17, `抱璞` (`t_f050bdd79dfe`), is authored from the two public
  interview families with Baizhang Huaihai and Touzi Datong. Seven exact
  occurrences preserve the unnamed questioners as utterers and the two named
  respondents as context figures. Compile, exact verification, strict
  attribution, and focused template checks pass.

Position 18, `轉身一路` (`t_11351dba572e`). Fresh shared research already
exists in `maintenance/investigation-next300-construction-research-a.json`;
the full-case navigation bundle is
`maintenance/investigation-next300-construction-fullcase-a.json`. Continue
explicit term-by-term semantic and utterer decisions through position 50.
Before authoring position 16, prove that masters actually cite, discuss, or
deploy the named text; headings and catalogue containment do not qualify.
