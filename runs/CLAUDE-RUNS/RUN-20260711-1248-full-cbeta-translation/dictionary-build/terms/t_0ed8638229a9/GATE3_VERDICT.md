# GATE 3 VERDICT — t_0ed8638229a9 · 無位真人

VERDICT: PASS

Auditor: Gate 3 independent adversarial pass (Claude, Fable 5). Method: tag-stripped verbatim
substring search over cited TEI files, nearest-preceding-`<lb>` check (primary edition,
ed="X" for X-canon), allowlist check, grep of every quoted Chinese phrase.

## 1. KWIC integrity — ALL VERBATIM
| Occ | RelPath | KWIC | Result |
|---|---|---|---|
| 1 | T/T47/T47n1985.xml | 赤肉團上有一無位真人 | exact, 1 hit |
| 2 | T/T47/T47n1985.xml | 無位真人是什麼乾屎橛 | exact, 1 hit |
| 3 | C/C077/C077n1710.xml | 上堂云赤肉團上有一無位真人常 | exact, 2 hits (cited lb selects the 1st) |
| 4 | X/X64/X64n1260.xml | 西臺辯禪師上堂，舉臨濟無位真人語 | exact incl. the ，(1 hit) |
| 5 | X/X64/X64n1260.xml | 無位真人突出難辨。頂門上時時顯現，眼睛裏處處 | exact incl. punctuation (1 hit) |

T47n1985 is a punctuated CBETA text — the commas/brackets in KWICs 1–2 and in the Explanation
quote are byte-exact against the file. No ellipsis, no stitching.

## 2. lb anchors — ALL CORRECT
0496c10, 0496c13 (T-ed); 0640b08 (C-ed); 0080b19, 0096c22 (X-ed, correctly the ed="X" numbers).
Occ 3's KWIC also matches at 0969b21 (a later 舉 of the same case elsewhere in the collection);
the cited FromLb 0640b08 unambiguously anchors the first, correct instance.

## 3. Allowlist — ALL 3 RelPaths present in zen-corpus.json. No contamination.

## 4. Attribution — CORRECT
- Occs 1–2 (臨濟錄, T47n1985): context verified — 「上堂云：「赤肉團上有一無位真人，常從汝等諸人
  面門出入，未證據者看看。」時有僧出問：「如何是無位真人？」師下禪床把住…其僧擬議，師托開，云：
  「無位真人是什麼乾屎橛？」便歸方丈」. Both KWICs are 師 (Linji) speaking. MasterName = Linji
  Yixuan correct. Every micro-quote in the AttributionNotes (未證據者看看 / 其僧擬議 / 師托開)
  attested at 0496c11–c13.
- Occ 3 (C077n1710 = 古尊宿語錄, 頤藏主集): governing section head 「臨濟慧照禪師諱義玄」 verified
  at 0640a04, occurrence at 0640b08 inside it. Same 上堂, independent transmission. Correct.
- Occ 4 (X64n1260, 行悅集 列祖提綱錄-type collection): the line itself is the head 「西臺辯禪師
  上堂，舉臨濟無位真人語」 — a raised case; MasterName = null is exactly what the gate requires,
  and the note explains why. Correct.
- Occ 5: a later master's 冬至小參 in the same collection; MasterName null, Curated=false,
  offered only as reception evidence. Acceptable.

## 5. Explanation honesty — ALL QUOTED CHINESE ATTESTED
「赤肉團上有一無位真人，常從汝等諸人面門出入」 (verbatim WITH the comma — the file is punctuated;
0496c10) · 「如何是無位真人」 (0496c12) · 「無位真人是什麼乾屎橛」 (0496c13) · 「舉臨濟無位真人語」
(0080b19) · 「無位真人突出難辨。頂門上時時顯現，眼睛裏處處…」 (0096c22; the trailing … is outside
the quoted span, marking truncation — the quoted part is exact). Gloss is deflationary and
grounded (位 = rank; the 乾屎橛 deflation is in the same exchange). No imported abstraction.

## 6. Multi-source — HOLDS. 臨濟錄 + 古尊宿語錄 Linji section + X64n1260 reception (2 further
raisings). The uniqueness claim ("distinctively Linji's; every later occurrence credits 臨濟")
is supported by the audited occurrences (occ 4 credits 臨濟 by name in the head; occ 5 sits in
a passage that immediately invokes 德山、臨濟). `multi-source` justified.

## 7. Nesting / RelatedTerms — 赤肉團, 乾屎橛, 面門 all occur inside the locus classicus
exchange itself; genuine constituent/context links, no coincidental overlap.

## Punch list (non-blocking observations)
- Note claims "241 allowlist texts"; my recount (note text included) finds 251. Approximate,
  tooling-dependent; treat the number as circa. Not a defect.
- Occ 3's KWIC matches twice in C077n1710 (0640b08 and a 舉 at 0969b21); harmless because the
  FromLb disambiguates, but worth knowing the KWIC alone is not unique in that file.

Defects: 0 blocking, 2 informational.
