# Gate 3 Verdict — 作家 (t_dab856504b69)

VERDICT: PASS

Verifier: independent adversarial pass (fresh model). All evidence re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` with a tag-stripping contiguity
checker that DROPS `<note>`/`<rdg>` apparatus content (so a KWIC cannot be confirmed
via footnote text) and records the `lb n=` at match start/end.

## Per-sense findings (single sense, SenseKey null)

### Check 1 — KWIC exact + contiguous: 5/5 PASS
1. T/T48/T48n2003.xml 0155a24 「這僧不妨是箇作家。」 — FOUND x1, lb 0155a24→0155a24 exact.
   Raw line 1959: new paragraph `pT48p0155a2401` directly after case 【一五】 (僧問雲門…門云。倒一說).
2. T/T48/T48n2003.xml 0170c28-29 「大凡作家宗師。要與人解粘去縛。抽釘拔楔。」 — FOUND x1,
   lb 0170c28→0170c29 exact (straddles lb, reassembles contiguous). Preceded by the 張拙 exchange,
   followed by the 仰山/中邑 illustration — commentary prose.
3. T/T48/T48n2003.xml 0174b13 「爾若要作家相見。便與爾作家相見。」 — FOUND x1, lb 0174b13 exact.
   Context: 長沙鹿苑招賢大師…機鋒敏捷。有人問教。便與說教。要頌便與頌。 then the KWIC.
4. T/T51/T51n2076.xml 0260a18 「師乃掩耳而已。居士云。作家作家。」 — FOUND x1, lb 0260a18 exact
   (raw line 6429).
5. T/T51/T51n2076.xml 0299a24-25 「若是作家戰將。便請單刀直入。更莫如何若何。」 — FOUND x1,
   lb 0299a24→0299a25 exact.

No ellipses, no stitching, no altered punctuation. Each KWIC occurs exactly once (x1) — no
ambiguity about which passage is cited.

### Check 2 — RelPath real + Zen allowlist: PASS
T/T48/T48n2003.xml and T/T51/T51n2076.xml both exist and both appear in
`Assets/Data/zen-corpus.json`. No contamination.

### Check 3 — Multi-source: PASS
Two independent texts (碧巖錄 T48n2003; 景德傳燈錄 T51n2076), three distinct speakers
(Yuanwu Keqin; Pang Yun; Xinghua Cunjiang), same meaning ("adept / real hand") in all five.
`multi-source` is justified.

### Check 4 — Over-read: none found
Single corpus-wide sense claim matches what I read: an attributive/nominal compliment of skill
(作家宗師, 作家戰將), the encounter collocation 作家相見, and the doubled exclamation 作家作家.
No master-specific uniqueness claim is made ("not bent by any one master") — correct and
appropriately modest.

### Check 5 — Imported abstraction: none
"An adept / a real hand" is deflationary and literal (作 do + 家 -er). The Explanation explicitly
refuses mystical rank ("measures skill, not mystical rank"). The horns/smoke simile referenced in
the Explanation is a known Blue Cliff commentary trope for 作家相見 and is presented as imagery,
not doctrine.

### Check 6 — Attribution honesty: PASS (all three verified against heads, not laundered)
- T48 occurrences: Yuanwu's 評唱. Verified structurally at 0155a24 — the KWIC opens the commentary
  paragraph AFTER the cited case; the 著語 capping phrases are separate inline `<note>` elements
  (stripped by my checker, KWIC still found → KWIC is commentary flow, not a note). AttributionNote
  "碧巖錄 case 15" confirmed: 【一五】 at 0155a21.
- T51 0260a18: section head at 0260a10 = 石林和尚 (cb:mulu, line 6421), and the section opens
  石林和尚一日**龐居士**來 — so the running 居士云 is Layman Pang. The exclamation 作家作家 is
  spoken by 居士, i.e. Pang Yun, NOT the section master. Entry attributes to Pang Yun — CORRECT
  (this is exactly the head-vs-speaker trap, and the entry does not fall into it).
- T51 0299a24: inside section 廬州澄心院旻德和尚, but the file reads 在興化時。遇**興化和尚示眾云**
  followed immediately by the KWIC — the speaker is textually explicit as Xinghua (興化存獎 =
  Xinghua Cunjiang, the Linji-line master whose 示眾 Minde attends; the following 旻德/興化 mutual
  shout exchange confirms the setting). Entry attributes to Xinghua Cunjiang, not Minde — CORRECT.

## Issues (tagged)
None.

## Verified occurrences: 5/5 KWIC confirmed verbatim
