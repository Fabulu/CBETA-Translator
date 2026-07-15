# GATE 3 RE-AUDIT VERDICT — t_7efdfe4296c6 · 父母未生前

VERDICT: PASS

Re-audited 2026-07-11 (Gate 3 re-audit, independent adversarial pass, Claude/Fable 5).
Method: tag-stripped (notes/rdg removed) verbatim substring search over cited TEI files,
nearest-preceding-`<lb>` (correct edition) + governing `cb:mulu` check, in-context voice
verification for both re-attributed occurrences, allowlist check, roster check against
master-dates.json.

## Prior punch list — resolution confirmed

1. **DEFECT 1 (occ 4 → Xueyan Zuqin): RESOLVED.** MasterName is now "Xueyan Zuqin"; the
   false "Speaker not identified at the section head" sentence is gone; the rewritten
   AttributionNote states the governing head and the in-text corroboration. Independently
   re-verified: KWIC 上堂：父母未生前，畢竟是什麼？ unique in X82n1571, nearest ed="X" lb =
   0152c06 ✓, governing cb:mulu = 袁州仰山雪巖祖欽禪師 ✓, and the immediately preceding
   上堂 in the same section contains 莫道仰山今日無為人方便 ✓. It is the master's OWN 上堂
   (hall discourse) — own voice, not a raised case. "Xueyan Zuqin" IS in master-dates.json ✓.
2. **DEFECT 2 (occ 5 → Tianyin Yuanxiu, "Narration" label removed): RESOLVED.** MasterName
   is now "Tianyin Yuanxiu"; note relabeled as the master's own 晚參 address raising his
   teacher 幻有正傳's practice. Independently re-verified: KWIC
   慣教人參如何是我父母未生前本來面目。 unique in J25nB171, lb J 0526b15 ✓, governing
   cb:mulu = 荊溪磬山語錄之一 (磬山 = Tianyin Yuanxiu) ✓; full context confirms
   陽山靜室眾請晚參，師云：「…昔我禹門堂上幻有老人，慣教人參如何是我父母未生前本來面目。
   汝等須是行也參…」 — the sentence is 師's own utterance describing 幻有's habitual
   teaching (his own words ABOUT the practice, not a quoted utterance OF 幻有), so direct
   attribution to Tianyin Yuanxiu is sound (the prior verdict allowed either this or null).
   "Tianyin Yuanxiu" IS in master-dates.json ✓.
3. **Minor 3 (count understatement): RESOLVED.** Note now states "771 raw occurrences
   across 201 allowlist texts, tags/notes/rdg stripped" — exactly the auditor's
   independent count, with the counting method stated.

## Regression spot-checks — clean

- KWIC re-grep (3 of 5): occ 4 and occ 5 above, plus occ 1
  智問：父母未生前，那箇是你本來面目？ → unique in X82n1571, lb X 0217a01 ✓ (governing mulu
  明州天童佛朗湛然自性禪師 — consistent with the note's null-MasterName narrated question).
  All verbatim, correct editions, no drift.
- Allowlist: all 5 occurrence RelPaths + 5 SourceTexts unchanged and in zen-corpus.json.
- Untouched occurrences 1–3 (all MasterName null): unchanged, no regression.
- Occ 5 is marked Curated: false — consistent with multi-source resting on the curated
  witnesses across X82n1571 / J26nB177 / J25nB171 (occ 3), which still holds.

Informational (non-blocking): RelatedMasters remains empty although two occurrences now
carry MasterNames (Xueyan Zuqin, Tianyin Yuanxiu) — not required by the prior punch list;
flagged only as an optional enrichment.

All defects from the prior punch list resolved; no new defects introduced.
