# Gate 3 Verdict — 勘破 (t_218e4815d84a) — POST-REPAIR RE-VERIFICATION

VERDICT: PASS

Independent adversarial re-verification after the prior REVISE (fabricated collocation 一勘便破).
Fresh Gate 3 pass; ALL evidence re-derived from the raw TEI at
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag+whitespace-stripped exact-substring
matching with lb anchoring. WORK.md used as context only. This verdict supersedes the prior
REVISE verdict (2026-07-11); the one flagged issue is confirmed FIXED.

## The prior REVISE issue — confirmed FIXED

- The fabricated clause quoting 一勘便破 is GONE from `entry.v2.json` (grep of the entry file:
  0 occurrences). No replacement was smuggled in; the attested stock declaration 勘破了也 carries
  the point, exactly as the fix note in WORK.md says.
- Corpus re-check (independent): `一勘便破` = **0 files** corpus-wide, by BOTH fixed-string rg AND
  the tag-tolerant multiline regex `一(\s|<[^>]*>)*勘(\s|<[^>]*>)*便(\s|<[^>]*>)*破` (also 0 files).
  Control: 勘破了也 fixed-string = 159 files — tooling sound. The phrase remains unattested and is
  no longer claimed.

## Per-occurrence findings (sense 1 of 1)

1. **X/X80/X80n1565.xml @0092b11** — `師歸院謂僧曰。臺山婆子為汝勘破了也` EXACT contiguous, 1 hit,
   immediately after `<lb ed="X" n="0092b11"/>` (correct ed="X" anchor; co-located R138 lb ignored
   per guide). Section head `<cb:mulu level="4">趙州觀音院從諗禪師` @0091b05 (raw line 7045) precedes
   with NO intervening mulu before the KWIC (raw line 7126) → 師 = Zhaozhou. Narrative arc verified:
   `師曰。待我去勘過` … `師歸院謂僧曰。臺山婆子為汝勘破了也`, followed by the inline note 玄覺云…
   甚麼處是勘破婆子處. PASS.
2. **T/T48/T48n2003.xml @0144a07** — `雪竇著語云。勘破了也。一似鐵橛相似。` EXACT, 1 hit, at
   `<lb n="0144a07" ed="T"/>`. Deshan–Guishan case; commentary continues `作麼生會他道勘破了也。
   什麼處是勘破處。且道勘破德山。勘破溈山` — exactly as the AttributionNote states; 雪竇著語云
   self-attributes to Xuedou. PASS.
3. **T/T48/T48n2004.xml @0233b20** — `萬松道。勘破了也。` 2 hits in file; the cited one at
   `<lb n="0233b20" ed="T"/>` in the Mt. Wutai crone case (趙州/婆子/玄覺 context), continuing
   `萬松道。非但累及玄覺。亦乃累及萬松` — the reflexive twist quoted in the AttributionNote, verbatim.
   (Second hit @0241a06 = Yunyan–Daowu case, not cited; no issue.) PASS.
4. **T/T47/T47n1998A.xml @0850b14** — `師云。老僧被爾勘破。僧擬議。師便打。` EXACT, 1 hit, at
   `<lb n="0850b14" ed="T"/>`, inside 室中機緣 (mulu/head @0849c09, juan close @0850b28) of
   大慧普覺禪師語錄; preceding `問僧。爾名甚麼。僧云法如。師云。僧堂佛殿如否。僧云如。` → 師 = Dahui;
   inverted use + strike exactly as described. PASS.
5. **T/T47/T47n2000.xml @0994b29** — `州歸院云。婆子被我勘破了也。` EXACT, 1 hit, at
   `<lb n="0994b29" ed="T"/>`, inside a raised case in Xutang's yulu; Xutang's comment follows:
   `師云。者婆子…趙州不施韜略。直欲破之。及乎交鋒之際。又却失利` — matches the AttributionNote, which
   honestly attributes the line to Zhaozhou (州) and names Xutang as the independent witness. PASS.

## Checks

- **KWIC exact + contiguous:** 5/5 verbatim, no ellipsis, no stitching, all anchored at the cited
  lb (X-canon occurrence correctly on the ed="X" lb).
- **Allowlist:** all 5 RelPaths in `Assets/Data/zen-corpus.json`. No contamination.
- **Multi-source:** HOLDS. 5 independent texts, 4 distinct speaking masters; the 臺山婆子 case
  underlies 3 of 5 but via independent commentators (WDHY denglu / Congrong lu / Xutang yulu),
  plus Xuedou (a different case entirely) and Dahui (his own live encounter) for breadth.
- **Over-read:** none. "One corpus-wide sense; no master bends the word idiosyncratically" is
  consistent with everything read; no uniqueness claim; the previously flagged unattested-phrase
  claim is removed.
- **Imported abstraction:** none — "to see through" is literal; the Explanation explicitly refuses
  the mystical reading ("not a mystical 'penetration of emptiness'").
- **Attribution honesty:** all 5 re-confirmed against section heads / self-attributing formulas
  (see per-occurrence findings).
- **Explanation support quotes re-verified verbatim in the cited files:** 待我去勘過 (X80n1565),
  什麼處是勘破處 (T48n2003), 亦乃累及萬松 (T48n2004), 老僧被爾勘破 + 僧擬議。師便打 (T47n1998A),
  趙州不施韜略 (T47n2000).

## Issues (tagged)

None blocking. One non-blocking observation, for the record: the Explanation's gloss of
什麼處是勘破處 as "where **she** got seen through" leans on the crone case, where the commentaries'
actual orthography is 甚處是勘破處 (T48n2004 @0233b19) / 甚麼處是勘破婆子處 (X80n1565); the exact
什-form quoted is verbatim from the cited T48n2003 (about Deshan/Guishan). Same formula, 什/甚
variant; the substantive claim ("commentators keep the word working by asking where the
seeing-through happened") is attested in all three cited files. No change required.

## Verified occurrences: 5/5 KWIC confirmed verbatim
