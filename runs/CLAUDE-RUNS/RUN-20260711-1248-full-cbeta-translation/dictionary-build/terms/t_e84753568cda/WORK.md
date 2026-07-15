# WORK — 向上事 (t_e84753568cda)

**Gloss target:** "the matter of the upward/beyond" (還有向上事也無); relate to 向上, 向上一路.

## Concordance (Zen allowlist only)
- 向上事: 1363 hits / 222 files. Top: X80n1565 五燈會元 (60), X81n1571, X81n1568, X82n1571,
  X78n1556 (38), D48n8939 古尊宿 (34), C077n1710 古尊宿 (31), X79n1557 聯燈會要 (29).
- Family (grep): 向上一路 957 · 向上宗乘 462 · 向上一著 279 · 向上一竅 270 · 向上人 229 ·
  向上關捩 101 · 佛向上事 149 · 知有向上事 69 · 還有向上事 90 · 有向上事也無 96 · 毘盧向上 9.

## Sense analysis → ONE sense (null, corpus-wide)
向上 = "toward the above / upward / beyond"; 向上事 = "the upward/further matter" — what lies beyond
what has been reached. Deployments (all observable):
- Dialogic frame: 向上還有事也無 → 有 → 如何是向上事 → turning-word (打破鏡來與汝相見; 新羅人不褁頭;
  向下文長; 八捧對十三). Recurs verbatim across houses.
- Criterion: 知有向上事 / 不知有向上事 (雪峯知有向上事始有語話分, D48 0163b05).
- Dismissive: 說甚麼向上事 (X80 0132a14).
- "beyond even X": 佛向上事 (beyond Buddha, 149×), 毘盧向上事 (beyond Vairocana, C077 0708a07).
- Paired with 末後句: 更須知有向上事末後句始得 (D48 0038b01, 佛眼).

## Multi-source gate → PASS (multi-source)
五燈會元 (X80n1565), 古尊宿語錄 D-canon (D48n8939) + C-canon (C077n1710), 聯燈會要, etc. Independent.

## Curated occurrences (all zc.verify ok=True, count=1, main text; ed=X where X-canon)
1. X80n1565 0104c12 (靈雲, 如何是向上事 → 打破鏡來與汝相見)
2. X80n1565 0132a13 (趙州, 知有向上事 → 不知)
3. D48n8939 0038b01 (佛眼, 向上事 + 末後句 pairing)
4. C077n1710 0670c01 (首山, full frame + 新羅人不褁頭)
5. C077n1710 0657c16 (睦州, 如何是向上事 → 向下文長)

## Attribution discipline
Stock question / two-speaker / criterion phrase → MasterName null on all occurrences. RelatedMasters
roster-confirmed: 趙州從諗, 佛眼清遠, 首山省念. 靈雲志勤 / 睦州道明 sections cited in notes only
(靈雲 not in roster; 睦州道明 IS in roster but occurrence is a two-speaker stock Q → left null).

## Links
RelatedTerms: 向上, 向上一路, 向上一著, 末後句, 佛向上事 (the "beyond even X" specialization).

## Notes / risks
- 毘盧向上事 KWIC (C077 0708a07) had a variant graph that broke zc.verify; dropped from curated set,
  cited in Explanation via the verified window only (as 毘盧向上, count-confirmed).
- D48/C077 are the two 古尊宿語錄 recensions — treated as independent witnesses (different canon files).

## 2026-07-14 fresh-build feedback and evidence gate

- Reader-first gloss changed from the opaque calque “the matter of the upward” to “the further matter beyond what has been reached.” English search aliases now cover upward, further, higher, beyond, and what comes next.
- Added four exact-headword witnesses for the Buddha-qualified form, Zhaozhou's dismissal, the recurring yes/what dialogue, and the Xuefeng criterion. Nine exact occurrences now span six source files.
- Added seven verified ClaimAnchors for the related upward road, move, aperture, person, lineage vehicle, pivot, and Vairocana-qualified family. They anchor every Chinese family string in the prose but buy no headword depth.
- Exact-turn review preserves Dongshan Liangjie as quoted utterer under Yunmen Wenyan's later raising, and two genuinely unnamed questioning monks under complete six-rung records. All named exact actors now have `utterer` context links and all contextual roles use the closed vocabulary.
- Re-tested the definition against all additions: the relative limit changes, but each use identifies a further matter, person, road, move, or device beyond what has just been named. The related nouns remain family terms rather than extra senses of the headword.
- All sixteen stored evidence rows pass `zc.verify` at exact bounds; `audit_attribution.py --json` reports zero hard failures.
