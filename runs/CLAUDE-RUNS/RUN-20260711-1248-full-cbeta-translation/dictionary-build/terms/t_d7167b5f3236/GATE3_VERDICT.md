# GATE 3 VERDICT — t_d7167b5f3236 · 殺人刀

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial), 2026-07-11
**Method:** tag-stripped stream match with lb re-anchoring, raw-XML context pulls (incl. a deep dive on the T47n1997 speaker frame), allowlist grep, phrase-honesty greps.

## 1. KWIC integrity — 4/4 EXACT-CONTIGUOUS
| # | RelPath | FromLb | Result |
|---|---------|--------|--------|
| 1 | T/T48/T48n2003.xml | 0152c14 | exact, **2 hits** (0152c14 + 0155a18) — AttributionNote already discloses the case-15 recurrence ✓ |
| 2 | T/T47/T47n1997.xml | 0748a05 | exact, 1 hit, spans 0748a05–a06 ✓ (raw grep misses it — lb splits 不存/毫末 — stream match confirms) |
| 3 | J/J25/J25nB171.xml | 0520b22 | exact, 1 hit, anchors at 0520b22 ✓ |
| 4 | J/J25/J25nB171.xml | 0520b16 | exact, 1 hit, spans 0520b16–b17 ✓ |

No ellipsis, no stitching. All main text.

## 2. Attribution — all correct (occ 2 stress-tested)
- **Occ 1 (T48n2003 0152c14):** `垂示云。…` opening Biyanlu case 12 — the 垂示 is Yuanwu's authorial voice by construction. MasterName 圜悟克勤 ✓.
- **Occ 2 (T47n1997 0748a05) — the hard one.** The host paragraph opens `舉。丹霞裕長老為人入室上堂云。大眾。…`, which could read as a RAISED sermon of Elder Danxia Yu (which would make 圜悟克勤 a wrong-speaker FAIL). Adversarial resolution: in the SAME 上堂 section of this yulu, sibling paragraphs open `舉。悟和尚立僧上堂` (0743b26) and `舉。杲首座立僧上堂` (0746c04) — the appointment-occasion formula ("on promoting X, [the master] ascended the hall"), exactly parallel to occasion openers like 韓觀察請上堂 / 施主捨法衣上堂云 throughout the section. The sermon addresses 大眾, uses 山僧更問爾, echoes Yuanwu's signature Biyanlu pointer verbatim (殺人刀活人劍…上古之風規。亦是今時之樞要), and closes 問取丹霞和尚 — deflecting future questions to the newly appointed Danxia Yu (佛智端裕, Yuanwu's heir). Conclusion: Yuanwu's own 上堂 on the occasion of appointing Danxia Yu to 為人入室. MasterName 圜悟克勤 STANDS.
- **Occ 3 (J25nB171 0520b22):** governing cb:mulu = 烏瞻山法濟禪院語錄 inside 天隱和尚語錄; frame `師云：「…山僧今日亦是不惜眉毛與諸人明明拈出，雖然如是，且道如何是殺人刀、活人劍？…」` — 師 = 天隱圓修 in his own 示眾 comment; 山僧今日 attested at 0520b21 as the AttributionNote claims. MasterName 天隱圓修 ✓.
- **Occ 4 (0520b16):** inside 示眾，舉：「夾山會禪師…」 raised case; MasterName null ✓ — CORRECT value per the raised/quoted rule.

## 3. Allowlist — clean
T48n2003, T47n1997, J25nB171 and SourceTexts X66n1296 (殺人刀 35×), X64n1260 (13×) all in zen-corpus.json.

## 4. Explanation honesty — attested
- 垂示云。殺人刀活人劍。乃上古之風規 ✓; 若論殺人刀不存毫末。活人劍橫屍萬里 ✓
- 須知殺中有活…活中有殺 ✓ (T47n1997 0748a06–07: 須知殺中有活擒縱人天。活中有殺權衡佛祖)
- 石霜雖有殺人刀且無活人劍；巖頭亦有殺人刀亦有活人劍 ✓; 且道如何是殺人刀、活人劍 ✓
- 把住/放行: 把住放行 in 63 corpus files ✓; 擒縱 96 files ✓ (and literally in the occ-2 passage); 縱奪 328 files ✓
- "活人劍 co-occurs across ~100 allowlist texts": grep = 128 files — claim honest (conservative).

## 5. Multi-source — genuine
圜悟克勤 in two independent texts (Biyanlu + his yulu) plus 天隱圓修 (J25nB171), plus wide dyad distribution (128 files). `multi-source` justified; entry correctly refuses to call it one master's coinage.

## 6. Nesting / RelatedTerms — genuine
殺人刀↔活人劍 is the real dyad (co-attested in every cited passage). 把住放行 / 擒縱 / 縱奪 are the genuine functional-pair vocabulary (擒縱 occurs IN the cited T47n1997 passage). No coincidental character-overlap relations.

## Punch list (non-blocking, 1 minor)
1. **Occ 4 AttributionNote wording:** it says the gauge line is "not attributed to one master." In the raised case the speaker chain is `山云：『大眾還會麼？…老僧不惜兩莖眉道去也。』乃云：『石霜雖有…』` — i.e., within the story it IS spoken by 夾山善會 (夾山會禪師). MasterName=null remains the correct field value (raised material), but the note's rationale should read "spoken by Jiashan inside a case raised by Tianyin, hence null," not "not attributed to one master." Cosmetic; does not affect data fields.

Defect count: 1 (minor, note-wording only).
