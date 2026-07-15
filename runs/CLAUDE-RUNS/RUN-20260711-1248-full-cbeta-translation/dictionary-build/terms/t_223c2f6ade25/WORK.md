# WORK — 一大事因緣 (t_223c2f6ade25)

Drafted 2026-07-11. Concordance scoped to zen-corpus.json (462 texts) only.

## Concordance
- **351 hits / 139 allowlist files.** Strongly multi-source.
- Top files: X82n1571 五燈全書 (13), J28nB208 古雪哲語錄 (13), X72n1435 無異元來廣錄 (9),
  X64n1260 列祖提綱錄 (9), X81n1568 五燈嚴統 (7), X80n1565 五燈會元 (6), T51n2076 景德傳燈錄,
  T47n1997 圓悟語錄, T47n1998A 大慧語錄.

## Sense analysis
- **ONE corpus-wide sense**, three facets, all "the one great matter":
  1. **Cosmological source-phrase (from the Lotus)**: 佛以一大事因緣故出現於世 (buddhas appear
     solely for the one great matter → 開示悟入 the Buddha's insight). Chan keeps the exact wording.
  2. **Chan extension**: the same purpose is carried by the patriarchs and by the living master —
     祖師西來傳持箇一大事 (Yuanwu); 博山出世亦為一大事因緣 (Wuyi Yuanlai applies it to himself).
  3. **Checkpoint question**: 如何是一大事因緣 answered with a turning-word (Touzi: 尹司空為老僧開堂).
  4. **The practitioner's own task**: Dahui 知為此一大事因緣甚力 (to a layman).
- Deflationary: not a metaphysical "Great Cause" — literally THE one matter everything is for
  (awakening / the matter of birth-and-death). RelatedTerm 生死事大 (272 hits) is the genuine link.

## Attribution evidence (heads checked — two REJECTIONS)
- Yuanwu Keqin — T47n1997 head 小參三. ✓ roster.
- Wuyi Yuanlai (無異元來) — X72n1435 head 住建州大仰寶林禪寺語錄 (上堂); 博山 = his own seat. ✓ roster.
- Touzi Datong — T51n2076 entry under 前京兆翠微無學禪師法嗣 (heir of Cuiwei Wuxue = Touzi Datong);
  two-speaker Q&A → **MasterName=null**, determinate 師 noted. ✓ roster.
- Dahui Zonggao — used **0936c03** head 答黃知縣子餘 (his own letter). ✓ roster.
  - **REJECTED 0813a23** (head 進大慧禪師語錄奏劄 = memorial-to-throne preface by the submitting
    official) and **0842c13** (head 大慧普覺禪師塔銘 = post-mortem stupa epitaph) — neither is
    Dahui's own words. Front-matter trap avoided.

## KWIC verification
All 4 KWICs confirmed EXACT contiguous substrings after tag+<note> stripping (zc_verify.py),
each within a single <lb> line. The Yuanwu and Touzi KWICs are trimmed to one line (continuation /
answer cross the <lb>).

## Validation: multi-source
4 independent texts / masters (Yuanwu, Wuyi, Touzi, Dahui) across Tang→Ming, 139-file corpus spread.

## Gate 2 (Claude adversarial verify+repair) — VERIFIED 2026-07-11
- All 4 KWICs re-derived EXACT contiguous in cited files. Zero ellipsis.
- Zero contamination: all 4 occurrence RelPaths + all 6 SourceTexts in zen-corpus.json.
- FromLb re-derived = nearest preceding <lb>. X72n1435 (X-canon) correctly uses ed="X" 0263a06
  (co-located ed="R125" ignored per X-canon rule). Others ed=T match.
- Attribution confirmed at cb:mulu head: 223-1 head 小參三 (Yuanwu); 223-2 mulu
  住建州大仰寶林禪寺語錄, 上堂 (Wuyi Yuanlai, 博山=his seat); 223-3 mulu 舒州投子山大同禪師
  under 前京兆翠微無學禪師法嗣 → Touzi Datong, two-speaker 問/師曰 → MasterName=null ✓;
  223-4 mulu 答黃知縣子餘 (Dahui's own letter). All roster-confirmed.
  FRONT-MATTER REJECTIONS re-confirmed correct: 0813a23 (奏劄 memorial preface) and
  0842c13 (塔銘 stupa epitaph) rightly excluded — not Dahui's voice.
- Explanation quotes grep-verified: 佛以一大事因緣故出現於世✓(22f), 諸佛世尊唯以一大事因緣故
  出現於世✓(23f, Lotus source-wording kept in Chan corpus), 開示悟入✓(344f), 傳持箇✓,
  諸佛出世為一大事因緣✓, 一條白練驀頭穿✓, 尹司空為老僧開堂✓, 如何是一大事因緣✓.
- RelatedTerm 生死事大 genuine (252f). Deflationary gloss of the Chan repurposing kept.
- Validation multi-source upheld (4 independent texts/masters Tang→Ming, 139 files).
- STATUS → verified. No repairs needed.
