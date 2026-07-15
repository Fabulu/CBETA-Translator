# GATE 3 VERDICT — t_ce2a5ef71afe · 麻三斤

VERDICT: PASS

Independent adversarial audit, 2026-07-11. Method: tag-stripped exact-substring match with
raw-offset mapping back to `<lb>` (ed="X" for X-canon) and governing `cb:mulu` chain; phrase
counts over the full 462-file allowlist (notes/rdg removed).

## What PASSED

1. **KWIC integrity — 6/6 verbatim**, every FromLb correct:
   - X80n1565 @0310a16 `問。如何是佛。師曰。麻三斤。` ✓
   - T51n2076 @0386c21 `僧問洞山。如何是佛。洞山云。麻三斤。` ✓
   - T48n2005 @0295b11 `突出麻三斤言親意更親` ✓
   - T48n2003 @0152c24 `及至洞山。却道麻三斤。` ✓
   - B25n0145 @0897a15 `惟雲門乾屎橛。洞山麻三斤。却較些子。` ✓
   - X80n1565 @0318a20 `洞山麻三斤。意旨如何。` ✓
2. **Attribution — the mandated checks all hold:**
   - The ONLY named occurrence (X80n1565 @0310a16) sits under governing mulu level=4 head
     襄州洞山守初宗慧禪師 = Dongshan Shouchu ✓ — NOT 洞山良价. MasterName string matches the
     roster exactly (master-dates.json: Dongshan Shouchu / 洞山守初, 910–990 — the dates in the
     explanation match the roster too).
   - 景德傳燈錄 T51n2076 @0386c21: raised case (僧問洞山…) inside the entry of
     隋州雙泉山師寬明教大師 (governing mulu confirmed), with 師聞之乃曰 following — null ✓, and
     the attributionNote's framing is exactly what the file shows.
   - 無門關 T48n2005 @0295b11: under mulu 洞山三斤 (sequence 洞山三頓→鐘聲七條→國師三喚→洞山三斤
     = Case 18 ✓), a raised case + Wumen's verse — null ✓.
   - 碧巖錄 T48n2003 @0152c24: under mulu "12" (Case 12 ✓), Yuanwu's pointer listing stock
     answers — null ✓.
   - B25n0145 @0897a15 (discussion) and X80n1565 @0318a20 (monk probing the huatou under head
     自巖上座, confirmed by governing mulu) — null ✓.
3. **Allowlist.** All 5 RelPaths in zen-corpus.json ✓.
4. **Explanation/Note honesty — every quoted Chinese string attested:**
   - Origin line 問。如何是佛。師曰。麻三斤 ✓ (X80n1565, Shouchu's section).
   - Pairing 惟雲門乾屎橛。洞山麻三斤。却較些子 ✓ (B25n0145 @0897a15); the 無義路 tie is also
     independently real in the same text: 如庭前栢樹子麻三斤乾屎橛之類。略無義路與人穿鑿
     (B25n0145 @0798a16) ✓.
   - Blue Cliff folk-reading rejection: 洞山是時在庫下。秤麻 (@0152c25 area) and
     只這麻三斤便是佛。且得沒交涉 (@0152c28–29) both verbatim in T48n2003, in order, same
     passage — the explanation's ellipsis is transparent ✓.
   - Wumen's verse 突出麻三斤 ✓; huatou probe 洞山麻三斤。意旨如何 ✓.
   - Variant claim 麻三觔: stated ~107×, measured 116 in 47 files ✓ (and it indeed does not
     substring-match 麻三斤).
   - Disambiguation forms all attested in the allowlist: 洞山初 69×, 洞山初和尚 19×,
     襄州洞山守初宗慧禪師 7× ✓.
5. **Multi-source.** 5 independent witnesses (五燈會元, 景德傳燈錄, 無門關, 碧巖錄, 中峰廣錄)
   spanning X/T/B ✓.
6. **RelatedTerms.** 乾屎橛 and 庭前柏樹子 are co-listed with 麻三斤 in a single corpus line
   (B25n0145 @0798a16) as the same class of no-reasoning-path answers; 如何是佛 is the eliciting
   question in the origin line itself. Genuine semantic relations, no coincidental character
   overlap ✓.

## Nits (non-blocking)

- 庭前柏樹子 appears as 栢 (variant of 柏) in the B25n0145 co-listing line; the standard 柏 form
  is itself attested 401× in the allowlist, so the RelatedTerm form is fine — just be aware the
  variant exists if that line is ever cited as a KWIC.

No defects. Exemplary attribution discipline on a known trap term. PASS.
