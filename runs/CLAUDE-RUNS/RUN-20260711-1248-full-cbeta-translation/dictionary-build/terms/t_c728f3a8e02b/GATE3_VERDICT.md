# Gate 3 Verdict — 家風 (t_c728f3a8e02b)

VERDICT: PASS

Independent adversarial re-derivation from the primary Chinese (fresh model, no reliance
on WORK.md claims). Method: programmatic exact-contiguous substring check of every KWIC
against the cited TEI file (all `<...>` tags + whitespace stripped, offset-mapped back to
the raw XML to read the nearest preceding `<lb n>`), plus allowlist and claim spot-checks.

## Per-sense findings

### Sense (corpus-wide, SenseKey=null) — "family wind (a house's teaching style)"

**Check 1 — KWIC exact + contiguous: 5/5 PASS (each found exactly once, at the cited lb).**
- `B/B14/B14n0082.xml` lb 0169b07: `…崇慧禪師者彭州人也姓陳氏|僧問如何是天柱家風師曰時有白雲來閉戶更無風月四山流|又問亡僧遷化…` — exact, contiguous, unique; nearest preceding lb = 0169b07 as recorded.
- `J/J24/J24nB137.xml` lb 0361c26: `…問：「如何是和尚家風？」師云：「老僧耳背，高聲問。」僧再問，師云：「你問我家風，我卻識你家風。」…` — exact incl. punctuation; lb matches.
- `J/J25/J25nB156.xml` lb 0059a02: `…佛祖家風，衲僧活計，百姓日用，無不在內，喫粥喫飯，也要仔細。…` — exact; lb matches.
- `J/J26/J26nB178.xml` lb 0111c04: `…僧問：「臨濟家風則且置，曹洞宗旨是如何？」師云：「牽連斷貫索。」進云…` — exact; lb matches.
- `J/J27/J27nB191.xml` lb 0167b24: `…慈炤圓禪師因僧問：「如何是古佛家風？」師曰：「銀蟾初出海，何處不分明？」…` — exact (contiguous after tag stripping); lb matches.

**Check 2 — RelPath real + Zen:** all 6 SourceTexts (5 occurrence files + C077n1710) exist on
disk and are present in `Assets/Data/zen-corpus.json`. No contamination.

**Check 3 — Multi-source:** trivially holds on the curated evidence alone — five independent
texts across three collections (B/J), different masters (Tianzhu Chonghui; several Ming/Qing
yulu masters), plus C077n1710 independently verified to contain 家風 57 times including the
stock exchange `問如何是和尚家風師云秋𭣣冬藏` (note: the file uses gaiji 𭣣 for 收 — WORK.md
transcribed 秋收冬藏; harmless since the entry only paraphrases in English, but recorded here).
`multi-source` is correct.

**Check 4 — Over-read:** none found. The entry claims NO master-specific bend, which the
evidence supports (the same stock question is answered by unrelated masters in unrelated
houses). The explanation's uncited example phrases were all independently verified in-corpus:
"white clouds close the door" (B14n0082, in the KWIC), "old ears deaf / I know YOUR house-style"
(J24nB137, in the KWIC), "harvest in autumn, store in winter" (C077n1710, 秋𭣣冬藏), 野老家風
(attested in ≥4 allowlisted files: X72n1435, X69n1343, X67n1304, X67n1299).

**Check 5 — Imported abstraction:** none. "Family wind / house style" is literal;
the explanation explicitly deflates ("never a mystical essence"), matching the J25nB156
witness (佛祖家風…喫粥喫飯，也要仔細) it cites.

**Check 6 — Attribution honesty:** good. Per-occurrence MasterName left null with the
speaking master named in AttributionNote where the text identifies him (Tianzhu Chonghui
崇慧 — confirmed in the file: 舒州天柱山崇慧禪師…僧問如何是天柱家風).

## Issues (tagged)

- (none blocking) MINOR/INFO: WORK.md's transcription 秋收冬藏 for C077n1710 is normalized —
  the file reads 秋𭣣冬藏 (gaiji for 收). Affects WORK.md only; no KWIC in the entry uses it.
  No fix required to entry.v2.json.

## Verified occurrences: 5/5 KWIC confirmed verbatim
