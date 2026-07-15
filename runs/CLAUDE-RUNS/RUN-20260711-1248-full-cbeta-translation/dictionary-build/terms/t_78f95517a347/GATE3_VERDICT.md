# GATE 3 VERDICT — t_78f95517a347 · 生死事大

**Auditor:** Gate 3 independent adversarial pass (Claude, from-scratch corpus re-derivation)
**Date:** 2026-07-12 01:10 +02:00
**Method:** tag-stripped extraction of every cited TEI file (note/rdg/orig excluded; cb:mulu tracked separately), exact-substring KWIC matching, lb-milestone position mapping per edition, full 462-file allowlist recount of every numeric claim, cb:mulu governing-head check at each occurrence offset, roster lookup.

VERDICT: PASS

## 1. KWIC integrity — 6/6 verbatim, all lbs exact
| Occurrence | File | Verbatim? | lb check |
|---|---|---|---|
| 覺曰：「生死事大，無常迅速。」祖曰：「何不體取無生， | J27nB192 | EXACT (×1) | J:0197a17 ✓ |
| 問生死事大請師相救 | C077n1710 | EXACT (×2) | 0808a20 + 0896c08 — entry's two-occurrence claim exact; cited one is 0896c08 ✓; the answer 三家村人失却火 sits at 0896c08 ✓ |
| 每書生死事大四字於案頭。 | X84n1583 | EXACT (×1) | ed=X 0645a08 ✓; co-located R144:0974b17 exactly as AttributionNote states ✓ |
| 堂門書「生死事大」，壁書 | J26nB180 | EXACT (×1, incl. 「」) | J:0301c02 ✓ |
| 又是九月半也，生死事大，無常迅速，討甚 | J32nB273 | EXACT (×1) | J:0204b18 ✓ |
| 天如和尚示眾云：『生死事大。諸禪德！須是將生死 | J25nB171 | EXACT (×1, incl. 『) | J:0549c10 ✓ |

No ellipsis, no stitching, no added punctuation in any Kwic field. X-canon lb uses ed=X ✓.

## 2. Attribution — correct
- All six MasterName = null. Verified: J27nB192 hit sits in a 頌古 section (governing mulu 頌古) — a raised/narrated old case ✓. C077 is a questioner's plea ✓. X84n1583 governing mulu = 杭州府雲棲蓮池袾宏大師 — biographical notice, matches AttributionNote ✓. J26nB180 governing mulu = 上所居師蕘堂門… (narrated notice; the phrase 上所居師蕘 is verbatim in text at 0301c02) ✓. J25nB171: the 舉：「天如和尚示眾云 framing is verbatim at 0549c02 — a raised case, null correct even though the text names 天如 ✓ (天如惟則 in roster).
- OBSERVATION (not a defect): the J32nB273 hit opens an 上堂 in 千巖和尚語錄 — i.e., 千巖元長 (in roster) speaking in his own compiled record. Null is the conservative call and never a wrong-speaker; a future editor could defensibly attribute it. Also nit: entry calls the text 千巖禪師語錄; canonical title is 千巖和尚語錄.

## 3. Allowlist — 9/9 IN
All six occurrence RelPaths + B25n0145, J28nB208, X82n1571 are in zen-corpus.json. Every SourceText attests the headword (B25n0145: 11 hits; J28nB208, X82n1571 verified via quoted phrases).

## 4. Explanation honesty — every quote and every number grep-verified
- 生死事大，無常迅速 pair: **84 in 56 texts — exact** ✓. 無常迅速 total **160** ✓, outside-pair 76 = 160−84 ✓.
- 念生死事大 **19** ✓; 為生死事大 **39** ✓; 大事因緣 **699** ✓; 一大事因緣 **415** ✓.
- 生死事大 total: claimed 321/135 — replicates **exactly** (321/135) under the entry's stated method (note/rdg/orig excluded); a strict body-text count excluding cb:mulu TOC text gives 320/135 (one hit is a table-of-contents duplicate). The method is stated in the Note, so this is a methodological nit, not a misstatement.
- Canon spread claim "B, C, D, J, T and X" — verified: B18/C8/D5/J111/T36/X142 ✓.
- 兩句現話: occurs exactly once corpus-wide, in J28nB208 — "One text" claim exact ✓; full string 生死事大，無常迅速，兩句現話 verbatim ✓.
- X82n1571: 念生死事大，奮志尋師 ✓ (X:0249a18) and 念生死事大，乃薙染完具 ✓ (X:0617b11) — both verbatim.
- Yongjia–Huineng exchange: full quote 覺曰：「…」祖曰：「何不體取無生，了無速乎？」 verbatim in J27nB192 ✓; "narrated in many texts" supported — 何不體取無生 occurs 26× in 23 allowlist texts.
- J25nB171 continuation (只將不生不死四字貼在額頭上; 遂拈拄杖趁出) verbatim ✓. J26nB180 continuation (莫道老來方學道，孤墳盡是少年人; 每對此輒萬緣寢削) verbatim ✓.
- MINOR FLAG: the X84n1583 AttributionNote's extended quote ends 「…失手碎茶甌，有省。」 — the source reads 有省**，**作七筆勾… (comma, not full stop). Quote-final punctuation altered inside a note-level quotation. The Kwic itself and the Explanation's quote (ends at 案頭) are verbatim. Cosmetic; fix at leisure.

## 5. Multi-source — holds overwhelmingly
135 allowlist texts, 6 canons, 6 independent curated witnesses (J×4, C×1, X×1 plus 3 more SourceTexts). `multi-source` ✓.

## 6. Describe-only — clean
Deployment-range labels (reason-for-leaving-home, plea, inscription, sermon opener, raised 示眾) are all observable genre facts anchored to verbatim quotes. Closing formula present ("The texts assign 生死事大 no gloss beyond the literal reading of its graphs, and neither does this entry"). No banned vocabulary, no reading-menu. 大事因緣 contrast is stated as a separate stock phrase, not a gloss ✓.

## 7. Nesting / RelatedTerms — genuine
無常迅速 = dominant collocate (84 paired hits) ✓; 大事因緣 / 一大事因緣 = explicitly-contrasted stock phrases with verified counts ✓. RelatedMasters 永嘉玄覺, 慧能 both in roster ✓. No coincidental-prefix relations.

## Punch list (non-blocking)
1. AttributionNote (X84n1583): change quote-final 有省。 to 有省， or end the quote at 有省 (source: 有省，作七筆勾).
2. AttributionNote (J32nB273): title is 千巖和尚語錄, not 千巖禪師語錄; optionally reconsider null vs 千巖元長 for an 上堂 in the master's own record.
3. Note (optional): the 321 count includes 1 cb:mulu (TOC) duplicate; strict body-text = 320.
