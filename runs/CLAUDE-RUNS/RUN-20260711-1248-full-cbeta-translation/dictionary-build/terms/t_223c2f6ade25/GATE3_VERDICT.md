# GATE 3 VERDICT — t_223c2f6ade25 · 一大事因緣

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11.
**Method:** independent re-grep of `xml-p5` Zen corpus, allowlist `zen-corpus.json`, tag-stripped joined-text substring checks, lb anchoring, cb:mulu/head back-scan, front-matter rejection audit.

## 1. KWIC integrity — CLEAN (4/4 verbatim)
| RelPath | FromLb | Verbatim (tag-stripped) | At lb |
|---|---|---|---|
| T/T47/T47n1997.xml | 0757b25 | YES | YES |
| X/X72/X72n1435.xml | 0263a06 | YES | YES, `<lb ed="X" n="0263a06"/>` (correct ed="X") |
| T/T51/T51n2076.xml | 0320a07 | YES | YES |
| T/T47/T47n1998A.xml | 0936c03 | YES | YES |

Both "trimmed to one source line" claims verified: T47n1997 continues 傳持箇正法眼藏; T51n2076 continues 尹司空為老僧開堂 (attested, 4 files). No stitching, no ellipsis in any Kwic.

## 2. Attribution — CLEAN (including the two guarded cases)
- T47n1997 (圓悟佛果禪師語錄): governing mulu = 小參三 — matches; Yuanwu's own talk. ✓
- X72n1435 (無異元來禪師廣錄, title verified): governing head = 住建州大仰寶林禪寺語錄, passage opens 結制，上堂 — Wuyi Yuanlai's own 上堂; the contiguous source reads 諸佛出世為一大事因緣，幾幅素縑描不出；博山出世亦為一大事因緣… so 博山 = his own seat, exactly as noted. ✓
- T51n2076 (景德傳燈錄): two-speaker Q&A (問／師曰), MasterName correctly **null**. Governing mulu chain = 前京兆翠微無學禪師法嗣 → 舒州投子山大同禪師, so the answering 師 is Touzi Datong (投子大同, in master-dates.json), identified only in the AttributionNote. ✓
- T47n1998A (大慧普覺禪師語錄): governing head = 答黃知縣子餘 — Dahui's own reply-letter, matches. ✓

**Front-matter rejection audit (required check): CONFIRMED CORRECT.**
- 0813a23 sits under head 進大慧禪師語錄奏劄 (memorial-to-throne preface; text there: 大覺世尊為一大事因緣故出現於世…) — NOT Dahui's words; correctly NOT cited.
- 0842c13 sits under head 大慧普覺禪師塔銘 (post-mortem stupa epitaph; text: 為一大事因緣故。開佛知見…) — NOT Dahui's words; correctly NOT cited.
- The cited 0936c03 is inside his own letter. Guard held exactly as the Note claims.

## 3. Allowlist — CLEAN
All 4 occurrence RelPaths + all 6 SourceTexts in zen-corpus.json.

## 4. Explanation honesty — CLEAN (all quoted Chinese grep-attested)
以一大事因緣故出現於世 (11/9; the fuller 佛以一大事因緣故出現於世 4/4 incl. T51n2076 — the "Chan corpus keeps the source-wording" claim holds) · 開示悟入 (83/51) · 提持一大事因緣 (1, T47n1997) · 傳持箇 (3/3 incl. T47n1997) · 諸佛出世為一大事因緣 (25/22) · 博山出世亦為一大事因緣 (3/3) — and the Explanation's 諸佛出世…博山出世亦為 ellipsis join verified as ONE contiguous passage in X72n1435 · 如何是一大事因緣 (59/27) · 尹司空為老僧開堂 (4/4 incl. T51n2076, immediately after the cited question) · 知為此一大事因緣甚力 (1, T47n1998A).
Count claim "351 hits across 139 allowlist files" REPRODUCES via raw-XML grep (350/139; joined-text 415/152 — honest lower bound).

## 5. Multi-source — HOLDS
Four masters (Yuanwu Keqin, Wuyi Yuanlai, Touzi Datong via lamp record, Dahui Zonggao), four texts, three canons, Tang-through-Ming. `multi-source` justified.

## 6. Nesting / RelatedTerms — GENUINE
生死事大 greps 321 hits/135 files — a real, massively attested sibling concept ("the great matter" as birth-and-death), a deliberate semantic link, not a prefix coincidence.

## Punch list
None blocking. One cosmetic nit (no action required): the Explanation's compressed quote 祖師西來傳持箇[一大事] brackets a substitution — the source at 0757b25 continues 傳持箇正法眼藏. The bracket discloses the editing, and 傳持一大事 IS independently attested in Yuanwu's own record (T47n1997: 傳持一大事。提振向上機, also X64n1260, X68n1318), so the gloss is corpus-grounded; still, quoting 傳持箇正法眼藏 verbatim would be cleaner if the entry is ever touched again.
Defects: 0.
