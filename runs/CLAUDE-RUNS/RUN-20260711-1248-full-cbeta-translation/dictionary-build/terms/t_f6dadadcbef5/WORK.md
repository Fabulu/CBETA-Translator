# WORK — 無事 (t_f6dadadcbef5)

## Sense split
One corpus-wide sense (SenseKey null): "nothing to do" = the condition of one who has
stopped seeking, fabricating nothing. Linji and Huangbo mean the same thing, so one sense
(no separate Linji-keyed sense); Linji's 無事是貴人 / 無事人 formulas highlighted in prose.

## Multi-source gate: PASS (multi-source)
Two independent, cleanly-attributed witnesses in the Hongzhou line (teacher + disciple):
- T47n1985 臨濟語錄 (臨濟義玄) — 無事是貴人，但莫造作，秖是平常; 佛與祖師是無事人; 平常著衣喫飯無事過時.
- T48n2012A 傳心法要 (黃檗希運) — 道人是無事人，實無許多般心; 情盡都無依執，是無事人.

## Attribution checks
- Linji lines are 師示眾 in 鎮州臨濟慧照禪師語錄 (師 = Linji). Confirmed.
- Huangbo lines are 上堂 in 黃檗山斷際禪師傳心法要 (title verified). Confirmed.

## KWIC verification
Distinctive fragments (秖是平常著, 無事過時, 情盡都無, 實無許多般心) greped verbatim in-file.
Deflationary rendering "nothing to do"; did NOT inflate to "non-action"/"transcendence".

## Occurrences curated: 5 (3 Linji, 2 Huangbo)

## GATE 2 (Claude adversarial verify+repair) — VERIFIED
- All 5 KWICs re-grepped: exact-contiguous-verbatim (punctuation ，、。 matches source).
- Allowlist: T47n1985, T48n2012A in zen-corpus.json. Zero contamination.
- Attribution confirmed at section heads: Linji lines = 師示眾/continuous 道流 address in
  臨濟慧照禪師語錄 (師 = 臨濟義玄); Huangbo lines = 師云 / continuous discourse in 傳心法要
  (師 = 黃檗希運). Master's own words, no two-speaker/quoted lines. All correct.
- FromLb/ToLb re-derived: all 5 correct.
- Multi-source (Linji + Huangbo, 2 independent texts) upheld; rendering stays literal
  ("nothing to do", not "non-action"/"transcendence").
- No repairs needed. STATUS=verified.
