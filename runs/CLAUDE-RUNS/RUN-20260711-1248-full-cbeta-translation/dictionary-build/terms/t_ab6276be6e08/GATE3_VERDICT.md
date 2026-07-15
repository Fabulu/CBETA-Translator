# Gate 3 Verdict — 末後句 (t_ab6276be6e08)

VERDICT: PASS

Independent adversarial re-derivation from source (Gate 3, fresh model). Method: programmatic
exact-contiguity check — each cited file's `<body>` tag-stripped (`<note>`/`<rdg>` content removed
entirely), each KWIC checked as an exact contiguous substring, nearest preceding `<lb n=... ed=...>`
captured to verify FromLb. Allowlist grepped in `Assets/Data/zen-corpus.json`. Note-level claims
(extra SourceTexts, master identity) independently spot-checked.

Pre-cleared by launcher: the two senses both carrying SenseKey=null is legitimate here, not an error.

## Per-sense findings

### Sense 1 (SenseKey=null, "the last word") — PASS

KWIC checks (4/4 exact-contiguous, all FromLb confirmed):
1. `J/J25/J25nB163.xml` 0229b05 — `雪峰舉似巖頭。頭曰：「老漢未會末後句在。」山謂頭曰：「你不肯老僧那？」`
   — HIT incl. all CBETA punctuation (：「」？); preceding lb = 0229b05 (ed=J). Context read:
   「…德山無語。雪峰舉似巖頭。頭曰…頭密啟其意。山次日說法，果異常…」. The locus classicus as claimed.
2. `J/J25/J25nB163.xml` 0229b08 — `僧到巖頭舉似頭。頭云：「雪峰不會末後句。」…要識末後句，只者是。」`
   (full span) — HIT verbatim-contiguous; preceding lb = 0229b08. Immediately after the span the file
   reads 「大眾！巖頭果然知末後句麼？」 — verifying the Explanation's claim that the compiler
   interrogates Yantou himself.
3. `X/X80/X80n1565.xml` 0143c24 — `小德山未會末後句在。山聞。令侍者喚師去。` — HIT; preceding lb =
   0143c24 (primary ed="X"). Source reads 「師曰。大小德山未會末後句在。」 — KWIC starts one character
   in (at 小), a legitimate contiguous substring, and the Explanation quotes the full 大小德山… form
   correctly.
4. `X/X80/X80n1565.xml` 0249b14 — `如何是末後句。師曰。雙林樹下。` — HIT; preceding lb = 0249b14
   (ed=X). Independent stock-challenge context (「…曰。如何是末後句。師曰。雙林樹下。問。如何是學人
   轉身處…」), NOT the Deshan story — supports the "hardened into a stock question" claim.

Multi-source: J25nB163 and X80n1565 are independent collections (distinct wording: 老漢未會末後句在
vs 大小德山未會末後句在), plus the independent stock-challenge occurrence at 0249b14. The Note's
extra witnesses were adversarially confirmed: B27n0152 carries the Deshan verdict (…小德山未會末後句在…,
…喫老漢未會末後句在…), C078n1720 (…小德山未會末後句師…), D51n8948 engages the case and the stock
question (老漢會末後句還端的 / 如何是末後句師云… / 得徳山末後句香嚴…). `multi-source` is solid.

Pairing claim 向上一路/向上機: directly witnessed in B25n0145 — 「揣按不行處喚作向上機。坐脫立亡喚作
末後句。」

Imported abstraction: none — rendered "the last word," explicitly refusing to reify ("not a secret
formula"); the texts' own refusal to define (只者是) is preserved.

### Sense 2 (SenseKey=null, "a master's last word (spoken at death)") — PASS

KWIC checks (4/4 exact-contiguous, all FromLb confirmed):
1. `B/B25/B25n0145.xml` 0696a13 — `坐脫立亡喚作末後句。` — HIT; preceding lb = 0696a13 (ed=B).
   Explicit gloss of the death-sense, exactly as claimed.
2. `J/J35/J35nB336.xml` 0691c06 — `偉哉大丈夫，不會末後句。」遂就寢，右脅而化。` — HIT
   verbatim-contiguous; preceding lb = 0691c06. Span begins mid-quote (after 師曰：「偉哉…) which is a
   contiguous-substring start, not an alteration; honestly flagged in WORK.md. Master identity
   verified: section head at lb 0691c04 reads 「西京天寧禧誧禪師」 with inline note 「青十三芙蓉楷嗣」
   and opens 「師臨終，謂眾曰…」 — the AttributionNote (Caodong/Furong-Daokai heir dying while quoting
   the verdict) is accurate.
3. `J/J33/J33nB287.xml` 0545b15 — `聲聲道：『願聞先師末後句！』` — HIT incl. nested 『』 punctuation;
   preceding lb = 0545b15. Memorial usage as claimed.
4. `C/C077/C077n1710.xml` 0817a16 — `忌晨上堂先師當年末後句與人皮下挑出` — HIT (unpunctuated C-text,
   matches raw); preceding lb = 0817a16. Context: 「…東山和尚忌晨上堂先師當年末後句與人皮下挑出刺…」.

Multi-source: four independent verified witnesses across text-classes and lineages. Uncited
SourceText D48n8939 confirmed to carry the memorial phrasing (…堂先師當年末後句與…).

Sense split integrity: the split is defended by the explicit gloss in B25n0145 (坐脫立亡喚作末後句)
— the referent shift is stated by the corpus itself, not imposed. Both senses openly acknowledge the
shared pun. Defensible.

Allowlist (both senses): J25nB163, X80n1565, B27n0152, C078n1720, D51n8948, B25n0145, J35nB336,
J33nB287, C077n1710, D48n8939 — ALL present in zen-corpus.json. No contamination.

## Issues (tagged)

None blocking. Observation (no action needed): C077n1710 (0817a16) and D48n8939 carry what appears
to be the SAME memorial sermon line (先師當年末後句與人皮下挑出) — possibly two editions of one yulu;
sense 2's independence does not rest on that pair (B25n0145 + J35nB336 + J33nB287 + C077n1710 already
give ≥3 clearly independent witnesses), so `multi-source` stands.

## Verified occurrences: 8/8 KWIC confirmed verbatim
