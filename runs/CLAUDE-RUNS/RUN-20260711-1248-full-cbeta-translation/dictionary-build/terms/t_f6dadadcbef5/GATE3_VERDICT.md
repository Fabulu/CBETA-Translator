# Gate 3 Verdict — 無事 (t_f6dadadcbef5)

VERDICT: PASS

Independent adversarial re-derivation (fresh model, no trust in WORK.md). All checks re-run from
the primary Chinese in `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`.

## Per-occurrence findings (single sense, 5 occurrences)

1. **T/T47/T47n1985.xml 0497c27–0497c28 · 無事是貴人，但莫造作，秖是平常**
   - KWIC: FOUND exactly once, contiguous, verbatim including CBETA punctuation ，
     (raw span crosses only `<lb n="0497c28" ed="T"/>`). Lb re-derived: cited FromLb/ToLb correct.
   - Attribution: immediately preceded by 「師示眾云：『道流！切要求取真正見解…免被這一般精魅惑亂。』」
     — 師 = 臨濟義玄 in 鎮州臨濟慧照禪師語錄. MasterName correct.
2. **T/T47/T47n1985.xml 0499b11–0499b12 · 佛與祖師是無事人**
   - KWIC: FOUND x1, contiguous, verbatim. Lbs correct.
   - Attribution: inside Linji's continuous discourse (師云 exchange on the 三眼國土, then
     「求佛、求法，即是造地獄業…看經、看教亦是造業。佛與祖師是無事人」) — Linji speaking. Correct,
     and the context confirms the entry's point that 無事人 names the highest, not a deficiency.
3. **T/T47/T47n1985.xml 0500c10–0500c11 · 秖是平常著衣、喫飯、無事過時**
   - KWIC: FOUND x1, contiguous, verbatim including 、. Lbs correct.
   - Attribution: first-person 山僧 discourse (「約山僧見處無如許多般，秖是平常著衣、喫飯、無事過時」)
     — 山僧 = Linji himself. Correct.
4. **T/T48/T48n2012A.xml 0383b11–0383b12 · 道人是無事人。實無許多般心**
   - KWIC: FOUND x1, contiguous, verbatim. Lbs correct.
   - Attribution: directly preceded by 「上堂云百種多知。不如無求。最第一也。」 — Huangbo's 上堂 in
     黃檗山斷際禪師傳心法要. MasterName 黃檗希運 correct (matches the AttributionNote's 上堂 claim).
5. **T/T48/T48n2012A.xml 0382c26–0382c27 · 情盡都無依執。是無事人**
   - KWIC: FOUND x1, contiguous, verbatim. Lbs correct.
   - Attribution: continuous Huangbo discourse (「此語只為空爾情量知解。但銷鎔表裏。情盡都無依執。
     是無事人。」) — the master's own words, no second speaker. Correct.

## Cross-checks

- **Allowlist:** T/T47/T47n1985.xml and T/T48/T48n2012A.xml both present in
  `Assets/Data/zen-corpus.json`. No contamination.
- **Multi-source:** two independent texts by two masters (Linji 語錄; Huangbo 傳心法要) — distinct
  works, not two copies of one passage. Teacher–disciple proximity is honestly disclosed in the
  entry Note ("Hongzhou line"), so no laundering. `multi-source` upheld.
- **Explanation quotes re-attested:** 無事是貴人 (x1), 但莫造作 (x1), 佛與祖師是無事人 (x1),
  無事過時 (x1), 情盡都無依執 (x1), 實無許多般心 (x1). All in the cited files, once each.
- **Over-read / imported abstraction:** none — the entry is explicitly deflationary ("nothing to
  do", forbidding "non-action"/"transcendence"), which the Chinese supports. "Stock formula" for
  無事是貴人 is accurate, and no master-uniqueness claim is made.
- **RelatedTerms:** 無事人 is a genuine derivative compound (not a coincidental prefix); 貴人 /
  平常 come straight from the cited formulas. Acceptable.

## Issues (tagged)

None.

## Verified occurrences: 5/5 KWIC confirmed verbatim
