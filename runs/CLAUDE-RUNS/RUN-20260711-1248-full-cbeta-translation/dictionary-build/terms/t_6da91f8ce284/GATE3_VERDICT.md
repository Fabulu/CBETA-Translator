# Gate 3 Verdict — 賓主 (t_6da91f8ce284) — FINAL RE-VERIFICATION (round 2, fresh verifier)

VERDICT: REVISE

Verifier: Gate 3 round 2 (Fable, fresh instance), 2026-07-11. Supersedes the round-1 REVISE
verdict previously in this file. All four round-1 fixes were applied and are individually
correct as prescribed; all 8 KWICs are now exact-contiguous verbatim; allowlist clean;
multi-source holds. HOWEVER independent re-derivation of the Linji lu found ONE residual
corpus-contradicted claim in the sense-2 prose (Explanation + Note): the entry asserts the
T47n1985 main text carries the 賓-graph four (賓看主/主看賓/主看主/賓看賓) with ONLY the
fourth case written with the 客 graph. In fact the T47n1985 MAIN text writes the guest with
客 in THREE of the four names (客看主 / 主看客 / 主看主 / 客看客); the 賓-graph four occur in
T47n1985 only inside a back-matter apparatus note carrying the Ming-recension parallel text.
One-passage prose fix; do not merge until fixed. No occurrence/KWIC field needs to change.

Method: programmatic exact-contiguous substring check of every KWIC against the cited TEI
(tags + whitespace stripped; second pass with `<note>` apparatus removed — all 8 KWICs match
in BOTH passes, i.e. main reading flow), lb-anchor adjacency checks in the raw XML, allowlist
membership, and graph-form counts. WORK.md used as context only.

## Verified occurrences: 8/8

| Sense | RelPath | lb | KWIC | anchor |
|---|---|---|---|---|
| 1 | T/T47/T47n1985.xml | 0496c20 | verbatim | OK |
| 1 | T/T47/T47n1985.xml | 0496b01 | verbatim | OK |
| 2 | J/J25/J25nB171.xml | 0534c13 | verbatim | OK |
| 2 | B/B27/B27n0152.xml | 0600a19–20 | verbatim | OK |
| 2 | J/J26/J26nB183.xml | 0497b25 | verbatim | OK |
| 3 | B/B25/B25n0144.xml | 0682a11 | verbatim | OK |
| 3 | J/J25/J25nB163.xml | 0256a22 | verbatim | OK |
| 3 | J/J28/J28nB212.xml | 0475c25 | verbatim | OK |

Allowlist: all occurrence RelPaths + all SourceTexts of all three senses (incl. T47n1987A,
T48n2003, T48n2006, X80n1565, J23nB134) are in zen-corpus.json — zero contamination.

## The four round-1 fixes — status

(a) **Sense-3 KWIC ellipses → FIXED.** Both former "……" KWICs are now exact contiguous:
    - J25nB163 0256a22: `賓主互換，偏正不拘，至位融和，所謂無礙。` — verified in flow
      (`…終日眠，未嘗睡一隻眼。賓主互換，偏正不拘，至位融和，所謂無礙。山不接意…`).
    - J28nB212 0475c25: `未免賓主相待，彼此回互。直須賓不是賓、賓中有主，主不是主、主中有賓，
      賓主交參、互換無位` — verified in flow (`…賓則始終賓，主則始終主。雖然如是，未免賓主相待，
      彼此回互。直須…互換無位，到這裏人我雙忘…`). Zero ellipses anywhere in the entry.
(b) **二俱是瞎漢 attribution → FIXED as prescribed, but see residual issue.** Verified:
    二俱是瞎漢 count = 0 in the ENTIRE T47n1985 file (main text + apparatus); it belongs to
    J25nB171 (present verbatim inside the cited 0534c13 KWIC). 彼此不辨 IS the Linji lu's own
    fourth-case gloss — main text lb 0501a15: `學人歡喜。彼此不辨，呼為客看客。` (and the
    Ming-recension apparatus note also reads 彼此不辨喚作賓看賓). The Explanation now names
    彼此不辨 as Linji's wording and flags 二俱是瞎漢 as J25nB171's — correct.
(c) **Caodong-exclusivity caveat → FIXED.** Verified in T48n2006 (人天眼目): 臨濟門庭 defines
    a LINJI 四賓主 in the 中-names — `四賓主者。師家有鼻孔。名主中主。學人有鼻孔。名賓中主。
    師家無鼻孔。名主中賓。學人無鼻孔。名賓中賓。與曹洞賓主不同。` (anchor 0311b16 OK) — and
    曹洞門庭 answers `四賓主。不同臨濟。主中賓。體中用也。賓中主。用中體也。…` (anchor 0320c12
    OK). The sense-3 Explanation now carries exactly this caveat, quotes both passages
    accurately, and correctly relocates the Caodong hallmark to the 偏正/回互 pairing. The
    Dongshan-asks / interlocutor-answers clarification for B25n0144 0682a11 is also present
    and matches the source (洞山便問…云：「白雲蓋青山。」…).
(d) **J23nB134 note → facts all verified.** 客看主 anchored at 0525c08, 主看客 at 0525c10;
    賓看主 = 0, 四賓主 = 0, 二俱是瞎漢 = 0 in that file; kept in SourceTexts only. Every stated
    fact about J23nB134 is true — but see the residual issue: its framing as "a variant
    witness … with the 客 graph throughout" inverts the actual witness relations.

## Residual issue (the reason for REVISE)

**OVERREAD / witness-conflation in sense-2 prose (Explanation + Note) about which graphs
T47n1985 itself writes.**

What the entry currently says:
- Explanation: "In the Linji lu the four are 賓看主 …, 主看賓 …, 主看主 … and 賓看賓 …;
  … (— the main text writes this fourth case with the 客 graph, 客看客, T47n1985 lb 0501a15)"
  — implying the main text writes the OTHER three with 賓.
- Note: "recur in T47n1985 (the Linji lu original — 主看賓/主看主/賓看賓, its fourth case
  glossed 彼此不辨 and written with the 客 graph 客看客 at lb 0501a15)".
- Note on J23nB134: "reproduces the passage but with the 客 graph throughout … a variant
  witness".

What the corpus actually shows (re-derived):
- T47n1985 MAIN text (lb 0500c26–0501a15) writes the four as **客看主 / 主看客 / 主看主 /
  客看客** — grep counts on the tag-stripped body: 客看主=1, 主看客=1, 主看主=1, 客看客=1;
  賓看主=0, 主看賓=0, 賓看賓=0. Contexts: `…喚作客看主。…此是主看客。…此喚作主看主。…
  彼此不辨，呼為客看客。` So it is NOT only the fourth case that uses the 客 graph — three of
  the four do. 主看賓 and 賓看賓 do not occur in the T47n1985 main text at all.
- The 賓-graph four DO occur in the T47n1985 FILE — but only in the back-matter apparatus
  note n="0505003" (type="orig", anchored at 0505a28, `次下明本有…`, the Ming-recension
  parallel text): `喚作賓看主…此是主看賓…此喚作主看主…彼此不辨喚作賓看賓`. In THAT witness
  the fourth case is 賓看賓, not 客看客. So the entry's sentence is true of NEITHER witness —
  it stitches the Ming note's 賓-names onto the main text's 客看客 fourth case.
- Consequently J23nB134 (客看主 0525c08 / 主看客 0525c10 / 主看主 / 客看客) AGREES with the
  Taisho main text's graphs; it is a faithful reproduction of the main-text recension, not a
  lone "客-variant". The 賓-graph naming belongs to the Ming recension and the later
  tradition (J25nB171, J26nB183, B27n0152, T48n2006), where it is genuinely attested by this
  entry's own verbatim occurrences.
- Also note: the label 四賓主 itself does not occur in T47n1985 (count 0 file-wide); the
  entry sources the label from B27n0152 (舉臨濟大師四賓主 — verified) and T48n2006 — that
  attribution chain is fine and needs no change.

**Recommended fix (prose only; no KWIC/occurrence changes):** In the sense-2 Explanation and
Note, state that (i) the T47n1985 main text writes the four as 客看主/主看客/主看主/客看客
(guest = 客 throughout, lb 0500c26–0501a15), (ii) the now-standard 賓-graph names
賓看主/主看賓/主看主/賓看賓 appear in T47n1985 only via the Ming-recension apparatus note
(n=0505003 at 0505a28, fourth case 賓看賓, likewise glossed 彼此不辨) and are the forms used
by the later tradition (J25nB171, J26nB183, B27n0152, T48n2006), and (iii) reframe J23nB134
as matching the Taisho main-text graphs rather than as a variant. All of the entry's
evidence already supports this corrected framing.

## Other checks (all senses)
- **Sense integrity:** the three senses are genuinely distinct (plain encounter roles /
  Linji's 看-taxonomy / Caodong positional interchange) and the entry itself polices the
  boundary (中-names shared cross-house; hallmark = 偏正/回互). No imported abstraction —
  renderings are literal ("guest and host"); the "who commands the encounter" reading is
  grounded in the quoted 賓主歷然 passages.
- **Multi-source:** Sense 1 pervasive (T47n1985 curated ×2; SourceTexts X80n1565/T48n2003/
  T48n2006/B25n0144 attest independently). Sense 2: J25nB171 + J26nB183 + B27n0152 curated,
  plus T47n1985 original (客-graph) — holds. Sense 3: B25n0144 (Dongshan) + J25nB163 +
  J28nB212 (+ T47n1987A: 賓主/回互/偏正 present) — holds.
- **Attribution honesty:** J25nB171 occurrence keeps MasterName null with an honest "a
  Linji-house master … catechism" note; sense 3 keeps the "Dongshan-Caoshan shared usage,
  not Caoshan's private coinage" caveat.
- Carried-over minor note (non-blocking, from round 1): sense 1's two curated occurrences
  are both from T47n1985; breadth is carried by verified SourceTexts. Optionally add one
  curated occurrence from a second text (e.g. X80n1565's 賓主歷然 line).

## Issues (tagged)
- OVERREAD (sense 2, Explanation + Note — blocking): claims T47n1985 main text carries
  賓看主/主看賓/賓看賓 with only the fourth case written 客看客 · evidence: main text
  0500c26–0501a15 reads 客看主/主看客/主看主/客看客 (賓-forms count 0 in body); 賓-graph four
  only in apparatus note n=0505003 (Ming recension, fourth case 賓看賓) · fix: correct the
  graph attribution as specified above; reframe J23nB134 as agreeing with the main text.
- INFO (non-blocking): sense-1 curated occurrences single-text (breadth via SourceTexts).

## Verified occurrences: 8/8 KWIC confirmed verbatim (plus re-derived: T47n1985 0501a15
彼此不辨/客看客; apparatus note 0505003; B27n0152 headings 0600b03/b06/b09; J26nB183
0497b27/b30; T48n2006 0311b16/0320c12; J23nB134 0525c08/c10)
