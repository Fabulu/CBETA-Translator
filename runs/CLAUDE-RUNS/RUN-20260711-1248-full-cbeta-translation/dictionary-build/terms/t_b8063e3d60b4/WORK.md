# WORK — 直指人心 (t_b8063e3d60b4)

## Concordance (Zen allowlist only, 462 texts)
- 直指人心 → **545 hits / 201 files**. Spread across all genres/lineages.
- Top texts: X82n1571 (18), X69n1357 (16), J34nB311 (16), X84n1583 (15), X68n1318 (14), T47n1997 大慧語錄 (11),
  T51n2077 續傳燈錄 (8), C077n1710 (6), T48n2003 碧巖錄.

## Sense analysis
**One sense (corpus-wide, SenseKey=null).** One of the four stock phrases characterising Chan itself,
nearly always tied to Bodhidharma's coming from the West and paired with 見性成佛:
達磨西來，(不立文字/教外別傳)，直指人心，見性成佛. Parsing: 直 directly · 指 point · 人心 the person's mind.
The claim is **methodological, not metaphysical** — instead of routing a student through scriptures,
vehicles and gradual stages, the master points straight at mind.

Masters who unpack it stay deflationary:
- Dahui (T47n1997 0769b07): 達磨西來不立文字。直指人心見性成佛 — the slogan verbatim.
- Dahui (T47n1997 0779a16): 唯直指人心。若論直指。只人人本有 — "direct pointing" points only at what everyone
  already innately has (人人本有), nothing added. This is the key deflationary gloss.
- Huitang Zuxin (T51n2077 0564c12): 達磨西來直指人心見性成佛。亦復如是 — settled description of Bodhidharma's gift.
- C077n1710 0689b24 (general slogan voice, MasterName=null): 達磨西來教外別傳一句且道別傳個什麼直指人心見性成佛
  — cross-links 教外別傳.
- Yuanwu, Blue Cliff Record (T48n2003 0154c04): 謂之教外別傳。單傳心印。直指人心。見性成佛 — the formula with
  單傳心印 in place of 不立文字 (a stable variant).

No master changes the *referent*; one corpus-wide sense (cf. 祖師西來意).

## Attribution evidence (cb:mulu heads, checked)
- T47n1997 = 大慧普覺禪師語錄 → **Dahui Zonggao** (roster ✓) for both curated occurrences.
- C077n1710 0689b24 → section 語錄 (generic) → MasterName=null (general slogan statement).
- T51n2077 0564c12 → 黃龍祖心禪師 = **Huitang Zuxin** (roster ✓).
- T48n2003 0154c04 → Blue Cliff Record 垂示 (pointer) = **Yuanwu Keqin** (roster ✓); curated=false (cross-listed).

## Multi-source verdict
**multi-source** — 4 independent texts (T47n1997 大慧語錄, C077 古尊宿, T51n2077 續傳燈錄, T48n2003 碧巖錄),
3 rostered masters + a null-voiced slogan witness.

## Deflationary check / ewk
Rendered literally "directly pointing at the human mind." 人心 kept as the ordinary human mind — NOT
inflated into a capital-M Mind / Absolute. Dahui's 人人本有 gloss anchors the deflation. ewk's leads not
needed here; the literal is uncontested.

## Nesting (§5b)
RelatedTerms 教外別傳 · 不立文字 · 見性成佛 · 見性 · 即心是佛 are genuine constituents/companions of the
four-phrase formula (deliberate cross-refs), not coincidental prefixes.

## Gate 1 self-check
All 5 KWICs verified exact-contiguous, in-allowlist, FromLb = nearest preceding primary-ed <lb>.

## Gate 2 (Claude adversarial verify+repair) — verified
- All 5 KWICs re-derived EXACT-CONTIGUOUS against cited files. No ellipses/stitching.
- Contamination: 0. All 4 RelPaths (T47n1997, C077n1710, T51n2077, T48n2003) in zen-corpus.json.
- Attribution re-confirmed at cb:mulu heads: Dahui (大慧語錄, whole text) x2; C077 0689b24 correctly null (general slogan voice); Huitang Zuxin (黃龍祖心禪師 @L9865); Yuanwu (碧巖錄 垂示 pointer).
- FromLb all = nearest preceding <lb> (verified programmatically).
- Collocations verified: 人人本有 (Dahui deflationary gloss) verbatim in T47n1997; four-phrase frame with 見性成佛 confirmed.
- Deflationary rendering intact: "directly pointing at the human mind"; 人心 kept ordinary (not capital-M Mind), per four-slogan cluster. No repairs needed to content; RelatedTerms genuine (four-slogan constituents + 見性 + 即心是佛).
- STATUS → verified.

## GATE 3 repair (Claude, 2026-07-11 21:46 +02:00) — STATUS=verified
Applied Gate-3 punch item #1 (REQUIRED, only blocking defect): Note mislabeled 碧巖錄 as a 燈錄.
- Fix: Note wording "and two 燈錄 (續傳燈錄, 碧巖錄)" → "a 燈錄 (續傳燈錄), and the 碧巖錄 (T48n2003, a 頌古/評唱 koan-commentary collection, not a lamp record)".
- Grep-verify: 續傳燈錄 = T51n2077 (a 燈錄); 碧巖錄 = T48n2003 is Yuanwu's 評唱/頌古 koan commentary, not a lamp record. All 5 KWICs already gate-3-PASS (verbatim, lb-exact, allowlist-clean). Optional items #2/#3 (occ-3 note wording, count method) left as-is; not blocking.

## GATE 3 re-verification (Claude/Frizzle, 2026-07-11 22:24 +02:00) — STATUS=verified
Re-audited: Note already reads "a 燈錄 (續傳燈錄), and the 碧巖錄 (T48n2003, a 頌古/評唱 koan-commentary
collection, not a lamp record)" — the required genre-mislabel fix is present and correct. GREP-confirmed
碧巖錄 = T48n2003 (TEI work 碧巖錄, a 頌古/評唱 case-commentary collection); 續傳燈錄 = T51n2077 (the only
lamp record cited). No blocking defect remains. entry.v2.json unchanged this pass; JSON parses; Status=preferred, Validation=multi-source.
