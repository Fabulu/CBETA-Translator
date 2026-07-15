# Gate 3 Verdict — 棒喝 (t_0f97bfab265c)

VERDICT: PASS

Independent adversarial re-derivation from the primary Chinese (fresh model). Method:
programmatic exact-contiguous substring check of every KWIC against the cited TEI file
(tags + whitespace stripped, offset-mapped back to raw XML to confirm the nearest
preceding `<lb n>`), plus allowlist and explanation-claim spot-checks.

## Per-sense findings

### Sense (corpus-wide, SenseKey=null) — "blows and shouts (the stick and the shout)"

**Check 1 — KWIC exact + contiguous: 5/5 PASS (each found exactly once, at the cited lb).**
- `B/B27/B27n0152.xml` lb 0552a19: `…晚叅問|德山入門便棒臨濟入門便喝不動棒喝還有為人處也無|師云有僧禮拜師打云你作棒會那…` — exact, contiguous after tag stripping; lb matches.
- `J/J28/J28nB202.xml` lb 0088c03: `…臨濟出世後，唯以棒喝示徒，凡見僧入門便喝。據令聲前我獨雄…` — exact; lb matches.
- `J/J20/J20nB098.xml` lb 0516b15: `…然後德山、臨濟，棒喝交馳，機鋒掣電，令你捫摸不入、插足不得，不過勦絕情見…` — exact; lb matches.
- `J/J26/J26nB185.xml` lb 0583a26: `…問：「德山棒、臨濟喝，除此二途，有何指示？」師打云：「喚作棒喝，入地獄如箭。」僧喝，師又打。…` — exact; lb matches.
- `J/J27/J27nB198.xml` lb 0449c05: `…近時有一等拍盲禪者，高談臨濟專事棒喝，離此別無長處；更有一類麤蠻底漢子…` — exact; lb matches.

**Check 2 — RelPath real + Zen:** all 7 SourceTexts (5 occurrence files + C077n1710 +
D48n8939) exist and are in `zen-corpus.json`. No contamination. Extra SourceTexts verified
to actually attest the term: C077n1710 contains 棒喝 ×13, D48n8939 ×8.

**Check 3 — Multi-source:** holds decisively — five independent texts (B27, J28, J20, J26,
J27 collections, different masters/compilers) plus two more verified SourceTexts.
`multi-source` is correct.

**Check 4 — Over-read:** none. The key structural decision — ONE corpus-wide sense for the
compound, with the single-master attributions assigned to the components 棒 (Deshan) / 喝
(Linji) rather than to 棒喝 itself — is exactly what the verified witnesses show: the compound
always appears as the paired emblem (德山棒、臨濟喝; 德山入門便棒臨濟入門便喝; 棒喝交馳), never
as one master's private word. No unsupported uniqueness claim. The explanation's uncited
"blind stick and wild shout" was independently verified: 瞎棒狂喝 appears in the cited file
J27nB198 (`豈比瞎棒狂喝、瞞鼾儱侗`), ×3 for 瞎棒.

**Check 5 — Imported abstraction:** none. "Blows and shouts" is literal; the gloss keeps the
corpus's own self-critical register (喚作棒喝，入地獄如箭 — verified verbatim) instead of
inflating the term into a doctrine of "sudden awakening" or similar.

**Check 6 — Attribution honesty:** good. MasterName null on all curated occurrences (they
are later passages ABOUT Deshan/Linji, not first-person sayings), with the originating
masters carried as RelatedMasters — honest and correct.

## Issues (tagged)

- (none blocking) MINOR/INFO: the Explanation renders the B27n0152 quote with an inserted
  separator `德山入門便棒／臨濟入門便喝`; the source is unpunctuated. Acceptable in prose
  (the KWIC itself is verbatim), noted for completeness.
- (none blocking) MINOR/INFO: RelatedTerm 三玄三要 is a loose "fellow Linji device" link
  rather than a constituent; navigationally defensible, no fix demanded.

## Verified occurrences: 5/5 KWIC confirmed verbatim
