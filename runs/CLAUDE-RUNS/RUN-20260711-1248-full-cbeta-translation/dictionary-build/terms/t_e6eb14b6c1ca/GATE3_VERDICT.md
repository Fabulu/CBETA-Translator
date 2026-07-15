# GATE 3 VERDICT — t_e6eb14b6c1ca · 活人劍

**VERDICT: REVISE**

Independent adversarial audit, 2026-07-12. All checks run from scratch against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, allowlist `zen-corpus.json` (462 texts),
roster `master-dates.json`. Method: XML-tag/apparatus-stripped flow text (teiHeader, `<note>`,
`<cb:mulu>`, `<rdg>` removed; whitespace collapsed), per-char lb mapping.

The entry's evidence base is solid — every KWIC verbatim, every count exact, every attribution
verified. REVISE is for ONE substantive corpus-fact overclaim (§4/§6 below), grep-falsified.

## 1. KWIC integrity — ALL 6 PASS
| RelPath | Kwic (head) | matches | lb found | lb claimed |
|---|---|---|---|---|
| J/J10/J10nA158.xml | 如何是活人劍？」師打云：「自領出去。」 | 1 | 0017a24–0017a24 | ✓ |
| J/J25/J25nB163.xml | 殺人刀，活人劍，死者死，活者活。 | 1 | 0256c12–0256c12 | ✓ |
| J/J26/J26nB187.xml | 一轉語是殺人刀、一轉語是活人劍、… | 1 | 0707c16–0707c18 | ✓ |
| J/J26/J26nB183.xml | 活人須是活人劍，殺賊應還殺賊刀， | 1 | 0521a28–0521a29 | ✓ |
| B/B27/B27n0152.xml | 最初秪見師翁殺人刀未見師翁活人劍 | 1 | 0490b01–0490b02 | ✓ |
| J/J25/J25nB171.xml | 黃檗只有殺人刀，且無活人劍。 | 1 | 0549a07–0549a08 | ✓ |

Exact contiguous verbatim substrings, unique, punctuation byte-exact (B-canon correctly
unpunctuated). No ellipsis/stitch/apparatus.

## 2. Attribution — ALL CORRECT
- **J10nA158 @0017a24 → 密雲圓悟**: cb:mulu = 浙江寧波天童山景德禪寺語錄 ✓. 上堂，僧問…
  進云：「如何是活人劍？」師打云：「自領出去。」 — anonymous-monk test question, the answer is the
  master's own in his own 語錄. On roster ✓. (Observation, not a defect: the KWIC includes the
  anonymous questioner's words; attribution-to-master for 僧問/師答 in the master's own record is
  the established convention — the master-master two-speaker rule, correctly applied elsewhere in
  this batch, does not cover anonymous 僧問. AttributionNote is transparent about the structure.)
- **J25nB163 @0256c12 → null**: cb:mulu = 拈頌 ✓; 師拈云 opens the KWIC line, on the preceding
  洛浦(浦云)/從上座 exchange turning on 先師 (夾山)'s 目前無法，意在目前 — matches AttributionNote.
  古庭善堅 confirmed OFF-roster → null correct ✓.
- **J26nB187 @0707c16 → null**: head = 再住青州法慶禪寺語錄 ✓; 說戒上堂, master's own 四轉語 close;
  天岸昇 confirmed OFF-roster → null correct ✓.
- **J26nB183 @0521a28 → null**: cb:mulu = 關中次韻 ✓, verse couplet; 石奇通雲 confirmed OFF-roster
  → null correct ✓.
- **B27n0152 @0490b01 → 玉林通琇**: cb:mulu = 上堂 ✓; context verbatim 幻有大和尚誕日設供師指真云…
  令某小(gaiji)禿最初秪見師翁殺人刀…亦有殺人刀亦有活人劍…拈香云急着眼 — own monologue exactly as
  AttributionNote describes ✓. On roster ✓.
- **J25nB171 @0549a07 → null**: cb:mulu = 舉古 ✓; text: 妙喜云：『…黃檗只有殺人刀，且無活人劍。』
  且道他那裏是他無活人劍處 — a raised 妙喜 quotation probed by 天隱 → null correct ✓.

## 3. Allowlist — PASS
All 6 RelPaths + 6 SourceTexts in zen-corpus.json ✓; every SourceText attests the headword ✓.
Note-cited T47n1997 and J28nB206 also allowlisted, quotes verified 1 hit each:
須知殺中有活擒縱人天。活中有殺權衡佛祖 ✓; 殺人刀、活人劍，赤心片片，踞虎頭、收虎尾，始終不移，
把住則孤峰坐斷無人識，放行則亙古風光照大千 ✓.

## 4. Explanation/Note honesty — ONE DEFECT
Counts, all measured EXACT: 活人劍 **282× in 104 files** ✓; 殺人刀活人劍 contiguous **25× in 12** ✓;
殺活同時 **21× in 17** ✓. Quoted phrases all 1-hit verified: 如何是殺人刀？」師打云：「一棒打殺。」 ✓;
亦有殺人刀亦有活人劍 ✓; the 妙喜 gauge ✓; the four-轉語 line ✓; the verse couplet ✓.
Sibling cross-ref accurate: t_d7167b5f3236 (殺人刀) exists; its occurrences are exactly
T48n2003, T47n1997, J25nB171 (0520b22, 0520b16) — so this entry's witnesses ARE independent
(J25nB171 reused but at a different passage, 0549a07, and the Note discloses the reuse) ✓.

**DEFECT D1 — grep-falsified corpus claim.** Note: "Graphic contrast the texts keep: killing is a
刀 (single-edged blade), life-giving a 劍 (double-edged sword)." The allowlist corpus does NOT
keep it: **活人刀 28× in 23 allowlist files; 殺人劍 11× in 9**. Grep-backed counterexamples:
- J25nB174 @0713c09: 殺人須有活人刀，活人須有殺人劍。 (both graphs swapped, in one sentence)
- J27nB189 @0019a14: 先師恁麼道，不獨有殺人劍，且有活人刀。
- J27nB198 @0480a20: 頌曰：殺人劍裏活人刀，一句衝鋒命莫逃
- J28nB208 @0338b21: 殺人刀即活人刀
- C078n1720 @0646b14: 果然提起活人刀
The Explanation's related "It runs as the fixed complement of 殺人刀…the graphs pair a life-giving
劍 against a killing 刀" overstates the same point ("fixed"). The canonical dyad claim itself is
fine (殺人刀活人劍 25×; reversed contiguous 殺人劍活人刀 = 0×; 活人刀殺人劍 = 0×), but the
graph-assignment is a strong tendency with attested exceptions, not something "the texts keep."
Fix: soften both sentences and state the minority counts (e.g., "usually — 活人刀 28×/23 files and
殺人劍 11×/9 files also occur"), or drop the graphic-contrast sentence from the Note.

## 5. Multi-source — PASS
Two roster masters in their own records (密雲圓悟, 玉林通琇) + 4 independent witnesses across
J/B canons; the dyad additionally anchored by the sibling entry's T-canon witnesses. ✓

## 6. Describe-only — the D1 sentence is also the only interpretation-adjacent slip
Everything else is deployment description (answered as a set / split by timing / gauge measuring a
master), each mode grep-attested, closing with the no-gloss formula ✓. D1 is a corpus-fact
overgeneralization rather than doctrinal interpretation, but it asserts more than the texts show.

## 7. Nesting / RelatedTerms — PASS
- 殺人刀: genuine dyad partner, contiguous 25× ✓, entry exists (t_d7167b5f3236), cross-refs ✓.
- 殺活同時: attested 21× in 17 files, quoted in this entry's own J26nB187 KWIC ✓.
- 把住放行: genuine compound term, 81× in 58 allowlist files contiguous; deployed in the same
  breath as the dyad in the verified J28nB206 quote ✓.
- 擒縱: attested in the verified T47n1997 quote (擒縱人天) and as a free compound ✓.

## Punch list
1. **(substantive, blocking) D1**: Note sentence "Graphic contrast the texts keep…" — falsified by
   活人刀 28×/23 files, 殺人劍 11×/9 files (examples above). Soften with counts or delete.
2. (minor, same root) Explanation "fixed complement…the graphs pair a life-giving 劍 against a
   killing 刀" — qualify ("standard/dominant form"; reversed contiguous forms 0×, but swapped-graph
   phrasings attested).

**Defects: 1 substantive (+1 minor corollary).**
