# GATE 3 RE-AUDIT VERDICT — t_ebb0995c99fc · 頓悟

VERDICT: PASS

Independent adversarial re-audit (Gate 3, second pass), grep-backed against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with allowlist `Assets/Data/zen-corpus.json`.
Scope: confirm the 4 prior punch-list defects are resolved and spot-check for regressions.

## Prior punch list — all 4 defects RESOLVED (verified at source)

1. **T48n2008 edition label — FIXED.** AttributionNote now reads "六祖大師法寶壇經 (Zongbao
   edition, T48n2008), 頓漸品". 宗寶 attested in T48n2008 (x4, header/colophon); the Dunhuang
   text (T48n2007) remains a separate SourceTexts item. Correct.
2. **理頓事漸 quotation — FIXED.** Explanation now quotes 理須頓悟…事在漸除 with gloss
   "phenomena are removed gradually". Re-grep J28nB208: 理須頓悟 (x2) and 事在漸除，因次第盡
   (x2) both at lb 0358b22; the previously fabricated 事在漸修 does NOT occur in the file.
3. **"Quoting Zongmi" claim — FIXED.** T48n2016 AttributionNote now attributes the 頓漸四句 to
   Yanshou's own 問／答 grounded in the 楞伽經 四漸四頓, with only a schema-level comparison to
   Zongmi. Verified local: 如何是頓漸四句 @0626b28, the matrix KWIC @0626b29, 四漸四頓 @0626c01
   — all adjacent. No unsupported quotation claim remains.
4. **X63n1225 misidentification — FIXED.** Note now names X63n1225 as 中華傳心地禪門師資承襲圖
   (title verified x5 in file; 禪源諸詮 absent) and claims only 頓悟漸修 support via the 荷澤
   line. Verified: 荷澤則必先頓悟，依悟而修 verbatim @0875b05; 頓悟漸修 x1 @0875b08; the other
   three matrix terms (漸修頓悟／漸修漸悟／頓悟頓修) absent from X63n1225. Exactly as now stated.

## Regression spot-checks — PASS

- KWIC re-grep: T48n2008 自性自悟，頓悟頓修，亦無漸次，所以不立一切 → x1, lb 0358c27 exact;
  T48n2016 一漸修頓悟。二頓悟漸修。三漸修漸悟。四頓悟頓修。 → x1, lb 0626b29 exact;
  J28nB208 理須頓悟，乘悟并銷，白雲斷處見明月；事在漸 → x1, lb 0358b22 exact.
- Allowlist: all occurrence RelPaths and 8 SourceTexts still in zen-corpus.json (unchanged set).
- Attribution: Huineng speaker-frame (T48n2008 師曰 under 頓漸品) unchanged from first-pass
  verification; all other MasterName=null occurrences unchanged.
- Prior minor observation (J28nB208 "a master's own pairing" vs the adapted 楞嚴 couplet) was
  explicitly non-blocking and remains a matter of emphasis only; not a defect.

No new errors introduced by the revision. Entry passes Gate 3.
