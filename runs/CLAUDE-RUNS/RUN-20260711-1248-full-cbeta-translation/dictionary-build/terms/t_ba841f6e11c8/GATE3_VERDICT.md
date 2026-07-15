# GATE 3 RE-AUDIT VERDICT — t_ba841f6e11c8 · 乾屎橛

VERDICT: PASS

Re-audited 2026-07-11 (Gate 3 re-audit, independent adversarial pass, Claude/Fable 5).
Method: tag-stripped (notes/rdg removed) verbatim substring search over cited TEI files,
nearest-preceding-`<lb>` (ed="X" for X-canon) + governing `cb:mulu` check, allowlist check,
head/title verification for C078n1720, term-presence check for new SourceTexts.

## Prior punch list — resolution confirmed

1. **DEFECT 1 (sense 1, occ 1, X80n1565 0227a10 MasterName → null): RESOLVED.**
   MasterName is null; AttributionNote correctly keeps the whose-case claim (raised case in
   the 定上座 section, 師遂舉臨濟上堂曰…, "the line is Linji's… so MasterName null per the
   raised-case rule"). Governing mulu re-verified: 定上座 ✓.
2. **DEFECT 2 (sense 1, occ 2, C077n1710 0762b14 MasterName → null): RESOLVED.**
   MasterName is null; note discloses the raised case inside 廣智全悟禪師語錄 (governing
   mulu re-verified ✓) with the host's comment.
3. **DEFECT 3 (sense 2, occ 2 AttributionNote wrong title): RESOLVED.** Now reads
   禪宗頌古聯珠通集 (file heads confirm 禪宗頌古聯珠集序 / 通集序); 續傳燈錄 removed.
4. **DEFECT 4 (sense 2 Note same misidentification): RESOLVED.** Note now attests
   "禪宗頌古聯珠通集 (雲門因僧問如何是佛…乾屎橛, with verse-commentary)". Multi-source still
   holds: 五燈會元 + 禪宗頌古聯珠通集 + 雲門廣錄 (primary).
5. **Minor 5 (Explanation quote truncation): RESOLVED.** Explanation now quotes the full
   釋迦老子是乾屎橛。文殊普賢是擔屎漢 (re-grepped: attested verbatim in X80n1565, count 1)
   and names the speaker Deshan Xuanjian (德山宣鑒).
6. **Note 6 (C078n1720 compiled-case MasterName 雲門文偃): unchanged, accepted** per the
   prior ruling (compiler's by-master attribution heads the case; no competing governing
   master).

## Regression spot-checks — clean

- KWIC re-grep (3): 無位真人是甚麼乾屎橛。巖頭不覺吐舌。雪峯曰。 → unique, lb X 0227a10,
  mulu 定上座 ✓. 真人是什麼乾屎橛便歸方丈 → 3 hits in C077n1710; cited FromLb 0762b14
  anchors the first (mulu 廣智全悟禪師語錄) exactly as the note discloses ✓.
  是佛師曰乾屎橛。 → unique, lb 0808c24 in C078n1720 ✓.
- Allowlist: ALL RelPaths in both senses' Occurrences AND SourceTexts in zen-corpus.json,
  including the newly added sense-1 SourceTexts B/B14/B14n0082.xml (傳燈玉英集 — the very
  alternative witness the prior verdict suggested; 乾屎橛 count 1) and D/D48/D48n8939.xml
  (乾屎橛 count 8). B/B25/B25n0145.xml (sense 2) count 7. No contamination.
- Sense split (Linji levelling epithet vs. Yunmen koan-answer) intact; sense-1 Note's
  續傳燈錄 mention is the previously-accepted named-allusion attestation (T51n2077),
  unchanged.
- No attribution regression on the untouched occurrences (T47n1988 0549b07 雲門文偃 and
  both remaining sense-2 occurrences unchanged).

All 4 blocking defects and the minor defect from the prior punch list resolved; no new
defects introduced.
