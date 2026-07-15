# GATE 3 VERDICT — t_16140def874d · 主人公

VERDICT: PASS

Auditor: Gate 3 independent adversarial pass (Claude, Fable 5). Method: tag-stripped verbatim
substring search over cited TEI files, nearest-preceding-`<lb>` check, allowlist check,
grep of every quoted Chinese phrase, section-head (`cb:jhead`/`cb:mulu`) verification.

## 1. KWIC integrity — ALL 5 VERBATIM
| Occ | RelPath | KWIC | Result |
|---|---|---|---|
| 1 | C/C078/C078n1720.xml | 每日喚主人公復應諾 | exact, lb 0796a09 confirmed |
| 2 | C/C077/C077n1710.xml | 尋常方丈內自召主人公自云喏又云惺惺 | exact (contiguous; source continues 惺惺著), lb 0916a10 confirmed |
| 3 | B/B25/B25n0145.xml | 多將箇瑞巖主人公臨濟無位真人 | exact, lb 0699a04 confirmed |
| 4 | B/B25/B25n0144.xml | 那個是闍梨主人公？」對曰：「現祇對和尚即是。」 | exact incl. punctuation, lb 0418b11 confirmed |
| 5 | B/B27/B27n0152.xml | 守住個昭昭靈靈的識神便是悟得主人公也 | exact, lb 0520b02 confirmed |

No ellipsis, no stitching. Occ 4's AttributionNote correctly says the question's opening 阿 sits on
the prior lb (source: 阿那個是闍梨主人公 — 阿 before lb 0418b11).

## 2. Attribution — CORRECT
- Occ 1: C078n1720 = 禪宗頌古聯珠通集; the governing master head 台州瑞岩師彥禪師 is directly above
  the case narrative. MasterName 瑞巖師彥 correct. The continuation claimed in the note is verbatim:
  乃曰惺惺著他後莫受人謾 ✓.
- Occ 2: C077n1710 = 古尊宿語錄 (jhead 卷苐四十二 verified), inside 雲峰禪師語錄二·舉古 — a cited
  case (舉瑞巖空寂禪師…) with the critic's comment 師云鬼窟裏作活計 verbatim as the note says.
  MasterName null — exactly what the gate requires for a raised case. Correct.
- Occ 3: 天目中峯和尚廣錄; two-master grouping inside Zhongfeng's critique — null correct.
- Occ 4: 祖堂集 two-speaker exchange — null correct.
- Occ 5: 玉林禪師語錄 polemic — null correct.

## 3. Allowlist — ALL 5 RelPaths present in zen-corpus.json. No contamination.

## 4. Explanation honesty — ALL QUOTED CHINESE ATTESTED (grep-verified)
- 每日喚主人公復應諾 ✓ · 惺惺著 / 莫受人謾 ✓ (source: 惺惺著他後莫受人謾 — the explanation's …
  marks the omitted 他後 in prose, not in a Kwic; both quoted parts exact).
- 多將箇瑞巖主人公臨濟無位真人 ✓, with 即心是佛 immediately following ✓ (the claimed adjacency real).
- **The Zhaozhou exchange is REAL and correctly attributed** (the specific mandate check): J24nB137
  (趙州和尚語錄, Zhaozhou's own record): 問：「如何是趙州主人公？」師咄云：「這箍桶漢。」學人應：
  「喏。」師云：「如法箍桶著。」 — the 咄, the exact answer 這箍桶漢, and the cooper framing all
  verbatim. The prior fabrication 田庫奴 is gone from the entry. ✓
- 守住個昭昭靈靈的識神便是悟得主人公也 ✓ — and the entry frames it correctly as the CONDEMNED error
  (source: 妄謂…如是認識神為自己如認驢鞍橋作阿爺邪謬之甚…遂道守住個…). Honest.
- Wumenguan case 12 identification (巖喚主人) is standard and consistent with the cited loci.

## 5. Multi-source — HOLDS. Five independent texts (聯珠通集, 古尊宿語錄, 中峯廣錄, 祖堂集,
玉林語錄) plus the Zhaozhou record for the explanation. `multi-source` justified.

## 6. Nesting / RelatedTerms — GENUINE. 無位真人 (anchored by occ 3), 識神 (occ 5), 惺惺著 (the
Ruiyan case itself), 本來面目 (genuine semantic relative). RelatedMasters 瑞巖師彥 / 臨濟義玄 /
趙州從諗 all directly evidenced. No coincidental character-overlap links.

## Punch list (non-blocking observations)
1. **Occ 4 note omits the master's rejection.** In 祖堂集 the master immediately condemns the monk's
   answer 現祇對和尚即是: 師曰：「苦哉，苦哉！今時學者，例皆如此。只認得驢前馬後，將當自己眼目…」.
   The AttributionNote presents the exchange neutrally; a reader could take the monk's answer as
   endorsed. Suggest adding one clause ("the master rejects this answer — 苦哉苦哉…認得驢前馬後").
   This actually STRENGTHENS the entry's own 識神-vs-master contrast. Not a fabrication or
   misattribution — informational.
2. **"explicitly equates … as one and the same pointer" (Explanation)** — the B25n0145 passage
   groups 瑞巖主人公 / 臨濟無位真人 / 即心是佛 as interchangeable stock pointers while criticizing
   teachers who misuse them (與人打交輥). "Equates" is defensible (they are listed as the same class
   of pointer), but the grouping occurs inside a critique of their misuse; a half-sentence noting
   that context would be more precise. Informational.

Defects: 0 blocking, 2 informational.
