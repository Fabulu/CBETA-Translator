# GATE 3 VERDICT — t_33d49f4710be · 開悟

VERDICT: PASS

Audited 2026-07-11 by independent adversarial pass (Gate 3). All greps run against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` restricted to the 462-text
`zen-corpus.json` allowlist, tags/notes/rdg stripped.

## What passed

- **KWIC integrity (5/5):** every Kwic is an exact, contiguous, verbatim substring of its
  cited file after tag-stripping, and each is UNIQUE in its file. No ellipsis, no
  stitching, no punctuation drift.
- **Lb integrity (5/5):** FromLb matches the nearest preceding `<lb>` of the correct
  edition — `ed="T"` for T51n2076 (0268a10, 0324c09) and T48n2016 (0554a19), `ed="X"` for
  X80n1565 (0066a12, 0070b03; co-located R138 numbers correctly NOT used).
- **Attribution — Yongming Yanshou (occ. 3) CONFIRMED:** at T48n2016 0554a19 the sentence
  若開悟時。不隔剎那。便成佛果。 sits in the author's own expository voice in 宗鏡錄 (it is not
  inside a 經云/又云 quotation; it INTRODUCES the following 所以首楞嚴經云 citation) —
  MasterName "Yongming Yanshou" is correct.
- **Attribution — nulls all verified:** occ. 1 is narrator's-voice biography of
  古靈神贊 (…本州大中寺受業後。行脚遇百丈開悟。却迴本寺…) — null correct. occ. 2 is the
  殃崛摩羅尊者 section, Aṅgulimāla hearing the Buddha — null correct. occ. 4 sits in
  馬祖道一's 五燈會元 biography, 一日謂眾曰…, Mazu describing Bodhidharma's transmission
  (傳上乘一心之法。令汝等開悟) — a raised case within Mazu's address; null is the correct
  conservative call and the note correctly names Mazu. occ. 5 is master/attendant
  narration (師拈殼就耳畔。搖三五下作蟬響聲。其僧於是開悟) — null correct.
- **Allowlist (5/5 occurrences + 5/5 SourceTexts):** all in zen-corpus.json. Term verified
  present in the two non-occurrence SourceTexts (X78n1556: 25 hits; B25n0145: 22 hits).
- **Explanation honesty — all quoted collocations attested in the allowlist:**
  豁然開悟 (73 occ / 39 files), 心忽開悟 (5/5), 遇百丈開悟 (occ. 1), 其僧於是開悟 (occ. 5),
  令汝等開悟 (14/13), 開悟眾生 (5 occ / 4 files: J28nB219, J36nB369, T48n2016, X70n1403 ×2),
  言下大悟 (542/160 — the "stock" claim is justified). The specific instruction items:
  **開悟眾生 DOES occur in the allowlist** (verified above), and **開悟群生 was correctly
  removed** — it has 0 contiguous occurrences in the allowlist (宗鏡錄 has only 開悟諸群生,
  with 諸 intervening) and no longer appears anywhere in entry.v2.json.
- **Multi-source:** genuinely 3 independent texts among curated witnesses (T51n2076,
  X80n1565, T48n2016) — claim holds.
- **Nesting/RelatedTerms:** 大悟, 頓悟, 見性成佛, 見性 all exist as entries; all are genuine
  semantic kin (same awakening-event family / its content), not coincidental character
  overlap. Gloss "to open into awakening" matches attested usage in both intransitive and
  causative valences.

## Minor observations (non-blocking, no fix required)

1. **[Note — count understatement]** Claims "566 raw occurrences across 147 allowlist
   texts"; independent strict count is 596 / 149. Within ~5%, qualitative claim unaffected.
2. **[Note — 宗鏡錄 transitive-valence nuance]** The sole 開悟眾生 hit inside T48n2016 sits
   in an 又云 quotation (Tiantai material). The Note's broader claim still holds — 宗鏡錄
   contains 21 開悟 tokens with multiple causative uses (令其開悟 ×2, 教令開悟, 開悟一切眾生,
   開悟諸群生) — but a curated example of the causative in Yanshou's own voice would make
   the claim airtight.
3. **[Occ. 4 AttributionNote wording]** "(expository/two-speaker)" — the passage is an
   assembly address (謂眾曰), not a two-speaker exchange; the raised-case rationale for
   null is the accurate one. Cosmetic only.

No FAIL-class defects (KWIC, contamination, wrong-speaker, fabricated collocation): none found.
