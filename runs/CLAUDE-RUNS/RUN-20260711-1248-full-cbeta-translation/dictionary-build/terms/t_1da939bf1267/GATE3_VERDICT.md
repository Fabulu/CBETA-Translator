# GATE 3 VERDICT — t_1da939bf1267 · 呵佛罵祖

**VERDICT: PASS**

**Auditor:** Gate 3 independent adversarial pass (Frizzle batch, entry.v2.json)
**Date:** 2026-07-12 01:06 +02:00
**Method:** Python re-derivation over `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`,
Zen-scoped to the 462-text `zen-corpus.json` allowlist; body text only (`<note>`, `<rdg>`,
`<sic>`, `<orig>` stripped, whitespace collapsed). Every KWIC re-anchored at its `FromLb`.

## 1. KWIC integrity — PASS
All 6 curated KWICs are exact contiguous verbatim substrings of the cited files (tags/apparatus
stripped), each found once in the file and confirmed inside the FromLb line window:
- T47n1994A @0640b23 `上堂。楊岐一語。呵佛叱祖。明眼人前。不得錯舉。` — 1x, anchored ✓
- C077n1710 @0924a20 `上堂德山呵佛罵祖承其言者多見德山者少` — 1x, anchored ✓
- B27n0152 @0627a18 `向孤峰絕頂蟠結草庵呵佛罵祖去迺以鷄肋之力撑持大廈之危辯魔斥異` — 1x, anchored ✓
- J25nB175 @0755b04, J29nB244 @0710c14, J29nB235 @0407a26 — all 1x, anchored ✓
No ellipsis, no stitching, no added punctuation (the J-canon 「」？！ marks are in the source files).

## 2. Attribution — PASS
- **楊岐方會** (T47n1994A): governing cb:mulu `袁州楊岐山普通禪院會和尚語錄`; file title
  楊岐方會和尚語錄. Master speaking of his own word ✓. Roster spelling ✓.
- **真淨克文** (C077n1710): governing cb:mulu `雲庵真淨禪師語錄一` — the 上堂 speaker is 真淨克文,
  德山 is the subject, exactly as the AttributionNote says ✓.
- **玉林通琇** (B27n0152): raw XML confirms the structure claimed — section head 復寶華朝和尚 at
  0627a09; the correspondent's incoming letter is `附原書` + `<note place="inline">` (0627a10–17);
  the cited passage at 0627a18 is main text (the reply). Duplicate at 0656a20 verified: opens
  `師復書云我輩不能於尊宿林林之日…` ✓. Roster ✓.
- **Three J-canon nulls correct**: J25nB175 = 大溈五峰學禪師語錄 (五峰學 in text, NOT on roster —
  and correctly not conflated with roster 五峰); J29nB244 = 三山來禪師語錄 (三山燈來 not on roster);
  J29nB235 = 蓮月禪師語錄 (蓮月 not on roster). All three MISSING from master-dates.json, MasterName
  null is right ✓.

## 3. Allowlist — PASS
All 6 occurrence RelPaths and all 10 SourceTexts are in zen-corpus.json ✓ (also the 4th 呵佛叱祖
witness X78n1556 = 建中靖國續燈錄, mentioned in a note, is allowlisted).

## 4. Explanation honesty — PASS (every count re-derived EXACT)
- 呵佛罵祖 232 hits / 118 texts — recount: **232 / 118** ✓
- 訶佛罵祖 33 / 24 — recount: **33 / 24** ✓
- 呵佛叱祖 4 hits, files exactly {T47n1994A, C077n1710, D48n8939, X78n1556}, all four verified to be
  the 楊岐一語 line ✓ (D48 writes 楊𭛛 with a gaiji but same line)
- 罵祖呵佛 2 ✓; 罵佛呵祖 1 ✓; 訶佛叱祖 0 ✓; 呵佛喝祖 0 ✓
- 辨魔揀異…couplet "(2 texts)" — recount: exactly 2 (J29nB235 @0399c02 anchored ✓, X85n1590 ✓)
- Every quoted deployment verified verbatim: 德山呵佛罵祖臨濟喝 @C077 0939a05 ✓ and in D48n8939 ✓;
  the full B27 letter sentence (…誠大不幸) 1x ✓; 如何是徑截法門/總持道人/訶佛罵祖去 ✓;
  訶佛罵祖是甚麼人/闡提漢 ✓; 如何是主中主 ✓; 和尚終日訶佛罵祖 1x (J26nB180 = 天童弘覺忞禪師北遊集,
  attendant to the dying 真點胸 — context verbatim: 臨示寂展轉痛苦。侍者云：『和尚終日訶佛罵祖…』) ✓;
  據猊床，而訶佛罵祖 1x (J28nB203 = 雲峨喜禪師語錄, 十載據猊床 — "on the lion-seat" is descriptive) ✓.

## 5. Multi-source — PASS
T, C, B, D, J, X canons; independent masters (楊岐方會, 真淨克文, 玉林通琇 + three unrostered
Ming/Qing masters). `multi-source` is earned.

## 6. Describe-only — PASS
Literal graph gloss + attested deployment taxonomy (epithet / self-description / answer /
conduct-label / letter) — all observable genre facts. English renderings are translations of quoted
Chinese, not glosses of significance. The in-corpus gloss (闡提漢) is the corpus's own. Ends with the
no-gloss sentence. No banned vocabulary found.

## 7. Nesting / RelatedTerms — PASS
訶佛罵祖 and 呵佛叱祖 are grep-verified graphic variants; 辨魔揀異 is the attested 2-text pairing;
闡提 comes from the attested reply. All genuine, none coincidental.

## Punch list (advisory only — nothing blocks PASS)
1. **(minor)** 7 of 10 SourceTexts attest only a documented graphic variant, not the exact headword
   string 呵佛罵祖: T47n1994A (呵佛叱祖), J25nB175/J29nB244/J29nB235/J26nB180/J28nB203/X85n1590
   (訶佛罵祖). The entry is transparent about which text carries which form, so this is defensible —
   but a strict "SourceText attests the headword" reading would prefer variant-only witnesses to be
   flagged as such in the SourceTexts note.
2. **(minor)** The C077 AttributionNote says "both passages are carried again in the D-canon edition
   (D48n8939)". Substantively true, but passage 1 appears there with the variant graph 駡:
   `上堂德山呵佛駡祖承其言者多見德山者少` — the exact string with 罵 is only the 臨濟喝 passage.
   Worth one word ("with 駡") if the entry is ever revised.
