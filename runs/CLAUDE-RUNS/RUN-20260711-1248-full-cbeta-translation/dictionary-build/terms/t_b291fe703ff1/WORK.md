# WORK — 參禪 (t_b291fe703ff1)

## Sense split
One corpus-wide sense (SenseKey null): "investigate Chan" = the concrete work of Chan
inquiry driven by the great matter of birth-and-death, aiming at 悟; by Song–Yuan effectively
the huatou (看話) method. Note fixed collocation 參禪學道.

## Multi-source gate: PASS (multi-source)
Two independent, cleanly-attributed masters:
- X70n1400 高峰原妙禪師語錄 (高峰原妙) — 參禪須是鐵漢; 生死一大事乃參禪學道之喉襟; 參禪貴圖求悟.
- B25n0145 天目中峰廣錄 (中峰明本) — 口說參禪 (the false, mouth-only kind).

## Attribution checks / dropped evidence
- Gaofeng lines are 上堂/小參 in his 語錄 (title verified). Zhongfeng line is his own critique.
- DROPPED the strongest Dahui passage T47n1998A 0864c (諸人總道來這裏參禪): that section expounds
  Linji's 四照用 and invokes 雲門, so the speaker was not cleanly separable — omitted per the
  verify-attribution rule (#1 prior error = wrong-master credit). Dahui named only in prose as the
  systematizer of 看話, in RelatedMasters.

## KWIC verification
只遮生死一大事 / 資生貴圖求富 / 參禪須是鐵漢 / 人心浮淺口說參禪 greped verbatim in-file.
Rendering "investigate Chan", not "meditate" (imported later abstraction).

## Occurrences curated: 4 (3 Gaofeng, 1 Zhongfeng)

## GATE 2 (Claude adversarial verify+repair) — VERIFIED (3 lb repairs)
- All 4 KWICs re-grepped: exact-contiguous-verbatim.
- Allowlist: X70n1400, B25n0145 in zen-corpus.json. Zero contamination.
- Attribution confirmed at section heads: X70 lines = Gaofeng's own words (元宵上堂 / 上堂 /
  生死事大 discourse in 高峰原妙禪師語錄); B25 = Zhongfeng's own 話頭 discourse (口說參禪 critique).
  All correct. Dahui correctly kept out of occurrences (unseparable speaker) — unchanged.
- REPAIRED: all three Gaofeng occurrences had WRONG FromLb/ToLb (each phrase occurs exactly
  once in X70n1400, but not at the cited line):
    參禪須是鐵漢:  0681b15 → 0661b09
    生死一大事…喉襟: 0686a13/a14 → 0671a01/a02
    資生貴圖求富:  0684b19 → 0667b13
- Multi-source (Gaofeng + Zhongfeng, 2 texts) upheld; rendering stays literal
  ("investigate Chan", not "meditate").
- STATUS=verified after lb repair.
