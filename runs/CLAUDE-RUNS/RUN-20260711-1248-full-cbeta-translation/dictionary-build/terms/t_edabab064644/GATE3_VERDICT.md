# GATE 3 RE-AUDIT VERDICT — t_edabab064644 · 疑情

VERDICT: PASS

Re-audited 2026-07-11 (Gate 3 re-audit, independent adversarial pass, Claude/Fable 5).
Method: tag-stripped (notes/rdg removed) verbatim substring search over cited TEI files in
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, nearest-preceding-`<lb>` + governing
`cb:mulu` check, allowlist check against zen-corpus.json, roster check against
master-dates.json.

## Prior punch list — resolution confirmed

1. **DEFECT 1 (Xueyan occ MasterName → null): RESOLVED.** Occurrence 5 (T48n2024,
   FromLb 1100b02) now has `MasterName: null`. AttributionNote correctly rewritten: the
   line 參禪須是起疑情。小疑小悟。大疑大悟 is identified as the admonition of 處州來書記
   (Librarian Lai of Chuzhou), reported speech inside Xueyan's 普說, with 被州說得著 /
   便改了話頭 cited exactly as the prior verdict prescribed.
2. **DEFECT 2 (Explanation misattribution): RESOLVED.** Explanation now reads "the advice
   that turned his own practice … spoken to him by Librarian Lai of Chuzhou (處州來書記)
   and preserved in Xueyan's 普說" — no longer attributes the line to Xueyan himself.
3. **DEFECT 3 (Note overstated verification): RESOLVED.** Note now explicitly qualifies:
   "though the Xueyan witness's specific line is speech reported inside his 普說 — the
   advice of 處州來書記 … so that occurrence carries a null MasterName."
   RelatedMasters retains "Xueyan Zuqin" — permitted by the prior verdict (his 普說, his
   practice-turn) and now a conscious, documented choice.

## Regression spot-checks — clean

- KWIC re-grep (3 of 5): 參禪須是起疑情。小疑小悟。 → unique, lb T 1100b02, governing mulu
  袁州雪巖欽禪師普說 ✓. 做工夫貴在起疑情。何謂疑情？ → unique, lb X 0756a15 ✓.
  一歸何處。自此疑情頓發。直得東西 → unique, lb T 1101a05, governing mulu
  天目高峯妙禪師示眾 ✓. All verbatim, correct editions, no drift.
- Allowlist: all 3 RelPaths (X63n1257, T47n1998A, T48n2024) unchanged and in
  zen-corpus.json.
- Roster: Wuyi Yuanlai, Dahui Zonggao, Gaofeng Yuanmiao, Xueyan Zuqin all present in
  master-dates.json.
- No attribution regression on the untouched occurrences (Boshan x2, Dahui, Gaofeng —
  MasterNames unchanged and previously verified).

## Residual informational items (non-blocking, carried over unchanged)

- Note still says "208 allowlist texts" (prior recount found ~212) — approximate figure,
  explicitly ruled "not a defect" in the prior pass.
- 「博山禪警語」 shortening and the CBETA punctuation quirk around 疑情若破 — cosmetic,
  unchanged, previously ruled informational.

All blocking and minor defects from the prior punch list resolved; no new defects
introduced.
