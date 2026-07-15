# WORK — 轉語 (t_f2181872b682)

**Batch:** b002 · **Status:** verified (Gate 2)

## Term
轉語 — "a turning word": a pivoting response demanded/offered to resolve a case.
Task brief form: 下一轉語.

## Concordance (Zen allowlist only)
- Raw allowlist hits: **261 texts / 1,376 occurrences**.
- Top texts: X66n1296 (50), X82n1571 (34), X80n1565 (32), X79n1557 (30), C077n1710 (29),
  D48n8939, J25nB171, X64n1260, T47n2000, 碧巖錄, 無門關 …
- Multi-source; single corpus-wide technical sense.

## Sense analysis
One sense (SenseKey = null). A 轉語 is the single word that turns a stuck case: demanded
(下/著/道一轉語) or supplied for another (代一轉語). Shared across all houses — no
master-specific bending. Locus classicus = Baizhang's wild-fox case (代一轉語 → 不昧因果).

## Multi-source gate → PASS (multi-source)
5 curated occurrences across 5 masters / 3 texts:
1. Baizhang Huaihai — 五燈會元 X80n1565 0071c04 (verified: section head 洪州百丈山懷海禪師; the fox case; turning word 不昧因果).
2. Wumen Huikai — 無門關 T48n2005 0294c17 (無門曰; case 14 challenge 若向者裏下得一轉語).
3. Yunmen Wenyan — 碧巖錄 T48n2003 0154b03 (雲門云; prizing Baling's 三轉語).
4. Xuedou Chongxian — 碧巖錄 T48n2003 0216b05 (雪竇復云; 請禪客各下一轉語, rhino-fan case).
5. Baiyan Mingzhe — 五燈會元 X80n1565 0116a01 (師; everyday live use, not a koan device).

## KWIC verification
All 5 KWICs re-checked as EXACT contiguous substrings of the normalized source; each
contains 轉語; all RelPaths on allowlist; all FromLb nearest-preceding `<lb>` present in file.

## Notes / risks
- The three Baling turning words are Baling Haojian's; the occurrence's speaker (老僧/雲門)
  is Yunmen valuing them — attribution note makes this explicit (no wrong-master credit).
- Baiyan Mingzhe is not in `master-dates.json`; kept as an occurrence attribution but NOT
  added to RelatedMasters.
- Deflationary rendering "a turning word"; no mystified "magic phrase" gloss.

## Gate 2 — independent adversarial verify (Claude, Opus)
Re-derived from source by targeted grep of each cited file. VERDICT: **verified**, 0 repairs.
- **KWIC (5/5 exact-contiguous verbatim after tag-strip):**
  1. X80n1565 0071c04 `今請和尚代一轉語。貴脫野狐身。師曰。汝問。老人曰。大修行人還落因果也無。師曰。不昧因果。` — lines 5568–5569 (lb split), contiguous.
  2. T48n2005 0294c17 `無門曰。且道。趙州頂草鞋意作麼生。若向者裏下得一轉語。便見南泉令不虛行。` — lines 381–382, case 14. (NB a *second* real occurrence of 若向者裏下得一轉語 exists at 0295c16, case 22 — cited one is genuine.)
  3. T48n2003 0154b03 `他日老僧忌辰只舉此三轉語。報恩足矣。` — line 1880, verbatim.
  4. T48n2003 0216b05 `雪竇復云。若要清風再復頭角重生。請禪客各下一轉語。問云。扇子既破。還我犀牛兒來。` — lines 7372–7374; the clean prose copy (an earlier 0216a15 copy is broken by inline notes — correctly NOT cited).
  5. X80n1565 0116a01 `今請闍黎別下一轉語。若愜老僧意。` — line 8892 (after pb 0116a), verbatim.
- **Contamination:** 0. All RelPaths (X80n1565, T48n2005, T48n2003) on allowlist.
- **Attribution (all confirmed):** #1 師=Baizhang — section head 洪州百丈山懷海禪師 (line 5523, lb 0071a09), fox case. #2 無門曰 = Wumen. #3 雲門云 (line 1879) = Yunmen; note correctly credits the 三轉語 themselves to Baling. #4 雪竇復云 = Xuedou. #5 師=Baiyan Mingzhe — section head 鄂州百巖明哲禪師 (line 8882, lb 0115c16); not in master-dates → RelatedMasters exclusion correct.
- **Multi-source:** holds (3 texts, 5 masters). **FromLb:** all = nearest preceding `<lb n>`. **RelatedTerms** 著語/公案/勘破 = genuine encounter-technique siblings.
