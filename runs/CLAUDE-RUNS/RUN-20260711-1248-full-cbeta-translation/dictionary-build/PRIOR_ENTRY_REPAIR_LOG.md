# Prior-entry semantic/depth repair log

This is the durable execution ledger for findings in:

- `AGENT_DEPTH_AUDIT_B001_B005.md`
- `AGENT_DEPTH_AUDIT_B006_B010.md`

Verified occurrence evidence, corpus links, and source paths must be preserved unless a listed depth repair explicitly requires an additional zc-verified witness.

## Saved repairs

- 異類中行 `t_b4a4ae6874d0`: karma→recompense; Guizong's answer no longer called a gloss; vocal answer described and Chinese retained after English; collocations translated; Caoshan technical use null-keyed; target translates 異類中事.
- 平常心 `t_4ccf8aed47d3`: 道不用修 rendered “the Way does not need refining”; title and attribution made English-first.
- 佛性 `t_ad0a8e5aac3d`: Zhaozhou deployment null-keyed; 業識性 rendered “action-consciousness nature”; defensive sutra/doctrine framing removed; dog-case prose English-first.
- 頓悟 `t_ebb0995c99fc`: 修/頓修/漸修 rendered as refining; cultivation-system and external Zongmi overlay removed; every cited label translated.
- 截斷眾流 `t_f7bdd2def0ec`: 詮表不及 rendered “cannot be reached by formulation”; title/rubric attribution English-first.
- 萬法歸一 `t_e96268628f2c`: deleted automatic “dharmas” wording; retained “myriad things.”
- 分別 `t_15026800437e`: deleted “parsing experience” cognitive overlay; retained literal distinguish/separate and attributed corpus predicates.
- 殺活 `t_26d1f4bf3890`: deleted inferred two-ways-of-addressing taxonomy; retained killing/giving-life formulas.
- 照用 `t_6b8e3b4f44bb`: deleted inferred two-ways-of-addressing taxonomy; retained Linji's four recorded modes and predicates.
- 向上事 `t_e84753568cda`: deleted “further than what has been reached or said” synthesis; described observable dialogues and named-limit constructions.
- 無心 `t_041f65670cd4`: removed cultivation category from the cited successive-ages statement; translated title and action English-first.
- 作麼生 `t_51fe593d9ffe`: rendered 修 locally as “refine,” translated the title and deictic pairing English-first.
- 疑情 `t_edabab064644`: replaced external “training at nineteen” with “what Xueyan did at nineteen,” preserving the reported speaker structure.
- 三玄三要 `t_52391cba2cdf`: deleted defensive metaphysical-triad wording; translated the layering formula, three named mysteries, titles, and source distinctions English-first.
- 疑情 `t_edabab064644`: globally normalized “myriad dharmas” to “myriad things” after the audit missed the loan; earlier training wording was already removed.
- 無念 `t_d35dc9e3723e`: normalized “all dharmas” to “all things” in the Platform-record predicate.
- 末後句 `t_ab6276be6e08`: normalized “ten thousand dharmas” to “myriad things” in the quoted final verse.
- 五位 `t_ff50c6974a36`: normalized the peripheral label to “the hundred things in five groups.”
- 敗闕 `t_b8d2633b12ef`: root global scan caught imported “dharma-combat” and “Zen bend” in a completed b017 entry; rewrote it as the corpus's military-register failure verdict, English-first.

## Required before closing this repair set

- Reorder 平常心 so “ordinary mind is the Way” is first; add headword-bearing Zhaozhou and one independent ordinary-answer witness or record exact exclusion reasons.
- Fold the 佛性 dog-case deployment into the corpus-wide sense; it is not separate polysemy.
- Reorder 截斷眾流 so Yunmen-three-phrases use is first.
- Seven high old-format entries in b006–b007 are repaired: 四料揀, 四照用, 喝, 意旨如何, 思量, 宗旨, 枯木. Report `AGENT_REPAIR_HIGH_B006_B007.md`; 38/38 preserved occurrences verified with unchanged evidence fields, zero prohibited framing or bare-Chinese prose.
- Apply all remaining medium #0b/#0c repairs from both audit reports, then lows that affect entry prose; keep ledger-only suggestions separate.
- Parse all edited JSON, run expanded conformance and English-first audits, rerun exact occurrence verification, remerge, and record exact report paths/counts here and in CODEX_RESUME.md.

## Validation state

- The first five high-entry JSON files parsed after the initial saved edits.
- The later five medium-entry edits also parse successfully; all ten currently edited entries await the combined conformance/English/exact-occurrence/remerge gate after the remaining structural and high repairs.
- No occurrence, KWIC, line bound, SourceText path, STATUS file, manifest line, corpus XML, or translation XML has been changed by this repair pass.
- Active repair assignments: `/root/b010_batch_b` owns the three structural tasks above; `/root/b011_batch_a` owns the twelve remaining b001–b005 medium repairs; `/root/b010_batch_c` now owns the eight remaining b006–b010 medium repairs plus five prose/depth lows, including a new exact 活句 definition witness. Root owns global audit/integration.
