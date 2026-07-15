# GATE 3 VERDICT — t_d35dc9e3723e · 無念

VERDICT: PASS

**Auditor:** Gate 3 independent adversarial pass (Claude, Frizzle instance), 2026-07-11.
**Method:** every KWIC re-derived by tag-stripped exact-substring search against the cited file; lb re-located from nearest preceding `<lb>`; chapter/section context re-read from raw XML; every Chinese phrase quoted in Explanation/Note grepped across the 462-text allowlist.

## Defects

None blocking.

## Observations (non-blocking, recommended polish)

1. **[Explanation · 念念相續] Recension-specific polarity worth an anchor.** The parenthetical "thoughts keep flowing (念念相續)" is attested in the claimed POSITIVE sense only in the Dunhuang recension T48n2007: 「前念、今念、後念，念念相續，無有斷絕。若一念斷絕，法身即離色身。」 In the Zongbao recension T48n2008 the same phrase is used NEGATIVELY: 「若前念今念後念，念念相續不斷，名為繫縛。」 Since both recensions are cited sources the quote is honest, but a reader grepping T48n2008 first meets the opposite polarity. Recommend tagging the parenthetical to T48n2007 (or citing 一念絕即死 for the Zongbao side).

## Verified clean

- **KWIC integrity:** 5/5 KWICs are exact contiguous verbatim substrings of their cited files (each unique in-file, count=1), punctuation included. No ellipses, no stitching.
- **lb anchors:** all 5 match the nearest preceding lb exactly (T48n2008 0353a12 / 0351b02 / 0353a24; T48n2007 0340c19; B25n0143 0229a11).
- **Allowlist:** T48n2008, T48n2007, B25n0143 all in zen-corpus.json. Note's supporting cite T48n2016 (宗鏡錄) is ALSO on the allowlist, and 無念 occurs ~95x there — "quotes it heavily" holds.
- **Attribution:** occ 1–3 are inside Huineng's sermons (師示眾云 / 善知識！…) in 般若第二 and 定慧第四 of the Zongbao 壇經 ✓; occ 4 is Huineng in the Dunhuang 壇經 (第三十一折 area) ✓; occ 5 is in B25n0143 南陽和尚問答雜徵義 (the Shenhui corpus), and the raw context reads 「8.張燕公問：禪師日常說無念法…答曰：…」 — Shenhui answering 張燕公, exactly as the AttributionNote claims ✓.
- **Explanation collocations all attested verbatim in the cited sources:** 先立無念為宗，無相為體，無住為本 (T48n2008, exact incl. punctuation); 無相者，於相而離相。無念者，於念而無念 (KWIC); 於諸境上，心不染 (T48n2008 「於諸境上，心不染，曰無念」); 若百物不思，當令念絕，即是法縛，即名邊見 (KWIC); 云何立無念為宗？只緣口說見性… (KWIC); 無念法不言有，不言無 (KWIC); 念念相續 (see observation).
- **Recension identifications correct:** T48n2008 = 六祖大師法寶壇經 (宗寶本), T48n2007 = Dunhuang 壇經, B25n0143 = 神會和尚語錄…南陽和尚問答雜徵義 (title element confirms).
- **Multi-source:** Huineng across two textually independent recensions + Shenhui (independent master/text). Gate satisfied.
- **Anti-fakeout honesty:** the entry's refusal of the "mental blankness" reading is not imported doctrine — it is the text's own 法縛/邊見 clause. Deflationary and grounded.
- **RelatedTerms:** 無相 / 無住 are the two companion pillars in the attested formula ✓; 無念為宗 attested (KWIC 3) ✓; 見性 attested in the same passage (口說見性) ✓.
