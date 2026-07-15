# GATE 3 RE-AUDIT VERDICT — t_0a686fa27769 · 著語

VERDICT: REVISE

**Auditor:** Gate 3 re-audit (independent adversarial), 2026-07-11. Method: tag-stripped
(note/rdg excluded) exact-substring matching against
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`, lb re-anchoring, governing cb:mulu
extraction, note-inclusion differential for the inline-note claim, TEI title check,
allowlist-wide counts.

## 1. Prior punch list — both items resolved
1. **Occ 2 inline-note defect — FIXED (option b taken).** The former T48n2006 @0308b14
   `古德著語云` occurrence (inside `<note place="inline">`) was replaced by T48n2001 @0054b10
   `雪竇著語云。今日共者漢游山。圖箇甚麼。` — grep-verified: 1 hit, verbatim, lb exact, and it
   survives in the note-STRIPPED stream, i.e. genuine main text that can re-anchor in the
   rendered document ✓. The entry's claim that all 9 古德著語云 in T48n2006 are interlinear
   notes re-verified: 9 hits with notes included, 0 in main text ✓.
2. **Occ 1 "case 1" → case 4 — FIXED.** AttributionNote now says case 4 (德山挾複子);
   governing cb:mulu at the KWIC re-extracted: level=1, content "4" ✓.

## 2. NEW defect introduced by the replacement (the reason for REVISE)
**Occ 2's text is misidentified as 從容錄.** T48n2001 is 宏智禪師廣錄 (TEI title:
"No. 2001 宏智禪師廣錄"); the actual 從容錄 is **T48n2004** (萬松老人評唱天童覺和尚頌古從容庵錄,
also in the allowlist). Consequences in the current entry:
- AttributionNote of occ 2 says "從容錄 (萬松行秀's 評唱 on 天童 頌古)" — wrong text, wrong
  genre frame, wrong commentator. The passage's governing mulu is 明州天童山覺和尚上堂語錄:
  it is a 示眾/舉 in **Hongzhi Zhengjue's own recorded-sayings collection** (師舉 the
  保福/長慶 游山 case, quotes 雪竇's cap, then 師云 = Hongzhi's own comment) — not 萬松's
  評唱 layer.
- The Note's attestation list repeats it: "從容錄 (T48n2001)".
This is the same defect class as the prior B25n0144/中峰廣錄 error: an evidence-note text
misidentification, not corpus falsification. **The KWIC, lb, main-text status, and
MasterName 雪竇重顯 (internal attribution `雪竇著語云` in the KWIC itself) all remain
correct** — only the container identification is wrong. Fix: relabel occ 2 (and the Note)
as 宏智禪師廣錄 / 天童覺和尚上堂語錄, or move the occurrence to the real 從容錄 (T48n2004)
if that framing is wanted.

## 3. Regression re-grep — otherwise clean
- KWIC 1 T48n2003 @0144a07 verbatim, 1 hit ✓; KWIC 3 X66n1296 @0069c20 ✓; KWIC 4 X66n1296
  @0074c04 ✓ (both ed="X" anchors).
- MasterName values unchanged except the new occ 2 (verified above).
- Allowlist: T48n2003, T48n2001, X66n1296, T48n2006, J27nB198, plus newly cited J39nB466
  and X71n1418 — all present ✓.

## 4. Strip+enrich pass — describe-only, attested
Prose is descriptive (device description, formula pattern, translations); the Note's
"render 'capping phrase,' not 'wrote/uttered words'" is translation guidance, not
interpretation. No intent/force/"the point is" language.

Added quotes grep-verified verbatim:
- J39nB466 @0852b24: 五臺山有一尊宿 + 設十二問 + 請師著語 co-located on one line ✓;
  印月書記十問 + 請師著語 @0853a07, same record ✓
- X71n1418 @0511c05: 請著語于後 ✓
- 古德著語云 9× in T48n2006, all inline notes ✓ (differential test above)

Counts (measured vs claimed): 著語 316/119 vs 315/119; 著語云 150/43 vs 141/42;
代語 125/67 vs 130/65; 下語 914/206 vs 878/204. Deltas ≤4%, mixed direction, all phrases
massively attested — convention sensitivity, non-blocking, but worth refreshing when
fixing the item above.

## Punch list
1. **[REQUIRED] Occ 2 + Note: relabel T48n2001 correctly** (宏智禪師廣錄, Hongzhi's 上堂/示眾
   record — governing mulu 明州天童山覺和尚上堂語錄), or swap the occurrence to the true
   從容錄 (T48n2004). Remove the "萬松行秀's 評唱 on 天童 頌古" framing from this occurrence.
2. [Optional] While editing, refresh the four counts above under the stated convention.
