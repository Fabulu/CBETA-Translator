# Dictionary-Build STATUS

## ⛔ LIVE STATE — 2026-07-13 (ALL OLDER COUNTS BELOW ARE HISTORICAL)

- **626 entries are done and merged:** the prior 621 plus five new gold/silver calibration entries. The accepted
  nine-entry calibration set (including linked 本來面目) verifies **111/111 exact KWIC/lb bounds, 111/111
  named-and-sourced occurrences, and 125/125 anchored Chinese prose strings**. Generated rich entries and shards
  equal source; the website suite passes **507/507**.
- **Current phase: full-entry retrospective remediation, then r003 integration.** The first-public-feedback
  calibration is merged and hash-approved. Read `FIRST_PUBLIC_FEEDBACK_FIX.md`, `ATTRIBUTION_FIX.md`, and guide §5
  items 10–19 and `REMEDIATION_MASTER.md`. Every current entry must now pass corpus-inference, plain-English image, modifier/material,
  verb-frame sense, search-recall, nested-compound, and family-propagation gates as well as Rule 10.
- `maintenance/remediation-ledger.json` is now the only whole-dictionary completion ledger. It binds approvals
  to the entry + WORK + governing-spec + dependency hash and separately inventories unbuilt r/n/s/backlog rows.
  Old `STATUS=done` and old wave approvals do not satisfy the retrospective gates. Each sense must now open
  with a short corpus-earned English interpretation before quotations (guide item 18).
- Durable forward queues: requested r003–r009 (r009 adds 心如牆壁 and 不動尊 from the wall/movement lead); curated `NEXT500_BUILD_PLAN.md` n001–n034; companion
  `NEXT100_BUILD_PLAN.md` s001–s007; then all 720 rows in `RELATED_INVESTIGATION_BACKLOG.md` must be adjudicated and
  every independent Zen-specific survivor built.
- Next-500 QA removed 27 variant/family duplicates and substituted 27 independent counted terms. Current structure:
  500 unique deterministic IDs, A=180/B=180/R=140, exact plan parity; see `NEXT500_QA_RESOLUTION.md`.
- Guide §5 item 9 is mandatory: preserve every inherited interpretation as a tested lead with explicit
  keep/revise/reject. Merge only after exact zc, depth/sense, conformance, English-first, independent semantic
  cross-check, registration, and sync gates.
- Guide §5 item 10 is mandatory for all old and new work: exhaust the six-rung speaker ladder, name the
  person and exact text in every occurrence note, anchor every Chinese evidence string, and re-test the
  definition against added witnesses. Do not resume wave merges until this gate is passing.
- The separately checked 15-entry r002 cohort is remediated: 104/104 speakers resolved, 104/104 exact
  KWIC/lb verification, 104/104 exact-title-and-speaker notes, and 26/26 detected evidence strings
  anchored. Its 59 remaining flags are named speakers absent the current roster, not anonymous evidence.
  See `ATTRIBUTION_REMEDIATION_PLAN.md`. The original 606-entry sweep is now active.
- Original-606 progress: 102 entries have cleared both attribution/quote/depth mechanics and independent
  post-enrichment semantic review: 847/847 current occurrences are named and 752/752 detected Chinese
  prose evidence strings are anchored. Those approvals remain valid for Rule 10 but are reopened for the
  new items 11–19. The stopped five-entry 錯會 cohort is parse-safe but not counted: seven vague-prose flags
  and its depth audit remain. No partial remediation merge has been run.
- Post-606 r003 progress remains separately counted: 1/15 entries (蝦蟆) is remediated and independently
  reviewed with 9/9 named witnesses and 12/12 prose strings anchored. The other 14 remain held.
- Exact cohort IDs and peer-review state are tracked in `ATTRIBUTION_PROGRESS.md`; trust that ledger on
  restart instead of rediscovering or redoing completed attribution batches.
- The quote-anchor/depth support-inflation queue is exhausted: the baseline 115 unlabelled non-headword
  occurrences in 68 merged entries has been reduced to zero. Supporting evidence remains visible but is
  explicitly marked family/contrast and cannot buy exact-headword depth. The deterministic full sweep continues.
- Roster-link matching is DEFERRED while another agent expands the master roster. Source-attested names
  absent from the current 301-person roster are preserved and counted, but do not block attribution
  batches. Re-run exact roster/link resolution after the expanded roster lands.
- First-feedback calibration result: all current term entry files remain in retrospective scope. Repairs to 鳥道,
  玄路, 金鎖, 金, 銀, 金彈子, 銀彈子, 金毬, and linked 本來面目 are merged and accepted against their current
  hashes. The machine ledger reports **9 remediation-complete / 632 remaining** and **1,400 unbuilt queue rows**.
  The earlier baseline found 391 `Literally…` openings and 28 apparent-material headwords; those remain discovery
  signals for the retrospective, not automatic defects.
  `web_index_kwic.mjs` runs the website's actual v3 bigram+unigram shards locally and filters the Zen allowlist;
  proof queries reproduced 金 37,439/452 in 16.3 ms and 銀 2,972/373 in 8.6 ms. `indexed_kwic.py` cross-checks
  the desktop v4 postings plus KWIC sidecar. Both are discovery-only because sidecars can retain apparatus;
  all saved evidence still requires `zc.verify`.

## ACTIVE — Codex final-spec rebuild (2026-07-12)

Current rich termbase: **218 entries**. Waves b001-b014 and the seven former legacy stubs are local, `done`, and merged.
The final-spec refresh, depth repair, and imported-loan sweep completed and were re-merged on 2026-07-12. Latest merged
gates: **218/218 JSON-valid**, **2,378/2,378 occurrence copies verified**, **0 expanded conformance flags**, **0 strict
English-first violations**, and **0 invalid roster links**. Reports: `maintenance/conformance-audit-20260712T144046Z.json`,
`maintenance/english-prose-audit-20260712T144046Z.json`, and `maintenance/entry-audit-20260712T144350Z-dry-run.json`.
Next wave: **b015**. Batches A/B/C are active in the three agent lanes.
The old imported-framing glosses in `WAVE_PLAN.md` were also purged; its hard imported-framing scan is now clean.
All planned waves b009-b036 have refreshed `maintenance/bNNN-preflight.json` reports and durable three-agent
`BNNN_ASSIGNMENTS.md` queues. b009 and the 13-entry depth repair are complete. Unmerged future-wave entries have no
`STATUS` and are excluded from merge until root QA.

Existing translation audit is now scoped and saved: 21 XML files (12 base + 9 community), 654 body findings under the new
framing rules. Exact counts and the report path are in `CODEX_RESUME.md`; paragraph XML IDs remain stable for later repair.
Read-only triage in `TRANSLATION_REPAIR_PLAN.md` reduced these to 403 anchored units: 545 confirmed repairs, 31 false
positives to retain, and 78 source checks; exact mirror deduplication leaves 502 logical findings. All XML currently parses
and all stable `xml:id` values are unique.

## DIRECTIVE (user 2026-07-12): FINISH THE WHOLE THING — autonomously build ALL 35 waves (b002-b036, ~517 terms),
merging each, until the dictionary is complete. Report only when DONE or a structural blocker. Quiet churn.

## OVERNIGHT DIRECTIVE (user 2026-07-11 ~22:10): Carry on through the night. Session limits WILL recur (they reset on a
rolling window; last reset 22:10 Zurich). On a session-limit death of all agents: re-validate JSON, RELAUNCH the dead
stage from the ledger (atomic per-file writes = minimal waste), continue. Prefer FABLE to conserve opus; if fable pool
exhausts, use OPUS. When ALL waves done + fully de-interp+enriched+merged → seed the NEXT 500 terms (new WAVE_PLAN block)
and repeat the entire pipeline. RESILIENCE: agent success AND failure both send task-notifications that re-invoke me, so the
loop self-sustains; only if truly idle with nothing running, use ScheduleWakeup past the next reset.
CRASH-RECOVERY FACT (22:12): 6 enrich + b004-repair died at 22:10 having WRITTEN NOTHING (died in tooling/research). Fixed a
real bug: 3 b004 entries (百尺竿頭/拈古/麻三斤, from research r2) were camelCase not PascalCase → normalized via node. All
58 entry.v2.json now parse PascalCase. Relaunched enrich A-F (fable) + b004 repair (opus).

## ⛔⛔ EXPANDED GOVERNING STANDARD (user, 2026-07-12) — now guide §5 #0 + #0b + #0c. READ THE GUIDE.
 #0 DESCRIBE, DON'T INTERPRET (+ not-thin): literal gloss + attested usage + structural facts; no intent/point/force;
    no reading-menus; exemplar 乾屎橛 (terms/t_ba841f6e11c8).
 #0b ZEN ONLY. Zen = the Chinese Chan corpus; NOT Japanese Zen; **Zen has no "practice."** Purge SIX imported-framing
    families: (1) Buddhist-doctrine, (2) meditation/mindfulness — **禪/dhyāna is NEVER "meditation"** (禪床="Chan seat",
    參禪="investigate Chan"), (3) present-moment (當下="on the spot", 目前="before your eyes"), (4) dualism (分別="distinguish",
    not "dualistic"), (5) practice/method + Japanese overlay (no "huatou/koan practice, zazen, satori"; render the literal
    action), (6) Chinese-Chan-ONLY — **no Dōgen / Japanese; LITMUS: need Japanese to describe it → not Zen, drop it.**
 #0c DESCRIBE IN ENGLISH; TRANSLATE EVERYTHING (it's a dictionary): 看箇無字 = "look at the word 'no'". Chinese only in
    ()s WITH its English; Kwic field stays verbatim Chinese (evidence).
 DEFERRED (when whole dict done): auto-hyperlink in-description terms that have their own entries (overlaps RelatedTerms).

## ⚠ FABLE CREDITS EXHAUSTED (2026-07-12 ~dawn) — "out of usage credits for Fable 5". Per user: switch to OPUS.
 ALL remaining work is OPUS-only now (purge finish, b008 QA, future gates). Opus has its own session limits (hit before)
 → slower. Codex handoff (GPT-5.6, humane limits) is the scaling path — see CODEX_HANDOFF.md.

## RETRO PURGE (43 flagged entries, #0b+#0c): FABLE agents A✅ C✅ done (18 entries English-conformed). B/D/E DIED on
 fable-credit exhaustion (B did 著語✅+more; D died on 父母未生前; E died early on 頌古). RE-RUN on OPUS in flight:
  opus-1 a59a8c08 (B's 8: 打成一片/教外別傳/本地風光/百尺竿頭/大死/一大事因緣/直指人心/不立文字)
  opus-2 a854a1bf (D's 8: 下語/君臣/本分事/佛性/正法眼藏[DEL Dōgen note]/父母未生前/公案/無事)
  opus-3 aa80ad1f (E's 8: 頌古/棒喝/參禪[meditate→investigate Chan]/無心/祖師西來意/葛藤/無字/勘破)
 (idempotent — already-conformed entries just verified.) After they return → re-merge (still 113 count; content-only changes).
 THEN comb-through: the ~63 grep-clean entries still need #0c English review (untranslated Chinese in Explanation/Note).
 OPEN QUESTIONS for user's comb-through (boundary of "what is Zen"):
   (a) Korean Seon texts in allowlist (Chinul 修心訣 T48n2020, cited in 頓悟) — count as Zen or exclude?
   (b) Buddhist-doctrinal-ORIGIN terms that Chan repurposes: 一大事因緣 (Lotus Sūtra origin), 佛性, 一大事 — keep (Chan usage described) or drop as not-natively-Zen?
   (c) verify 禪源諸詮集都序 (Zongmi) prose-citation in 一大事因緣 is on the 462 allowlist (borderline if not; not a curated Occurrence so evidence OK).
 PURGE re-run DONE: opus-1 B✅ opus-2 D✅ (+公案 JSON-syntax-error fixed) opus-3 E✅. + fixed residual "koan"→"case" in
 正法眼/棒/喫茶去 (fable-A hadn't grepped "koan"=Japanese reading of 公案). ALL 43 flagged entries: JSON-valid, zero banned
 tokens, #0b+#0c conformed. RE-MERGED → 113 (content-only).
 STILL TODO (comb-through): the ~63 grep-CLEAN entries are framing-clean but NOT #0c-English-conformed (they predate #0c;
 untranslated Chinese remains in Explanation/Note). A follow-up English-conformance sweep is needed for full compliance.
 b008: 15 drafted (framing-correct), needs 1 OPUS quick-QA under #0b/#0c → merge (→128).
## b008: all 15 DRAFTED (r1 clean + r2/r3 reframed — 當下="on the spot" not present-moment; 惺惺 shows corpus MOCKING the
 meditation formula; 知解 pejorative). Needs 1 OPUS quick-QA under #0b/#0c, then merge (expect 128).
==> DE-INTERPRETATION is now a PERMANENT GATE: every wave gets a de-interp pass (prose-only surgery, preserve all
gate-verified facts) BEFORE merge. Fold it into the per-wave pipeline between gate-3 and merge.

## BALANCE (user, 2026-07-11): de-interp must NOT hollow entries — "get as much out of the text as possible."
Definition emerges from attested usage. Enrichment mines in-corpus self-definitions (X者…也/謂之X/名為X), deployment
range, textual contrasts, collocation counts — all grep-verified, describe-only. Written into guide §5 #0 "BUT NOT thin".

## PROGRESS: b001-b006 ✅ MERGED → termbase.v2.json = 98 entries. ALL describe-only + enriched.
b006 CLOSED via LEAN v2: 1 fable quick-QA (a56e6618) verified 61/61 KWICs w/ zc.py + fixed lbs/counts/2 attributions → 15 done → merged → 98.
CODEX_HANDOFF.md written (self-contained plan for Codex to continue @ 4-agent cap, GPT-5.6 generously).
CURRENT: b007 "Encounter-dialogue mechanics" (WAVE_PLAN line 103). 15 seeded. LEAN v2 RESEARCH RUNNING (3 opus×5, zc.py mandatory):
 r1 acde2cdb(鼻孔 t_ea138c7335d3/會麼 t_0e7b683790e8/便喝 t_4e30d47a452c/擬議 t_ef39bdc0eb99/目前 t_937f63a4fb51)
 r2 aec26634(拄杖子 t_87cc840b8f33/珍重 t_ada407625f42/承當 t_2f4b60453d19/分別 t_15026800437e/意旨如何 t_8f41e0da5a71)
 r3 ab5f9bac(思量 t_8a016f49e5b8/宗旨 t_cf0513be4012/正法眼 t_970c3f191929/宗風 t_7c1991e9eabb/枯木 t_326be1e9c98a)
 NEXT: research→1 quick-QA (fable/opus, zc.py)→merge(expect 113)→seed b008 (WAVE_PLAN line 120). Report at milestone/blocker.

## (older detail)
## PROGRESS: b001-b005 ✅ MERGED → termbase.v2.json = 83 entries. ALL describe-only + enriched.
b005 CLOSED: 11 gate-3 PASS + 4 REVISE repaired (圓通法秀 named / 刀-劍 qualified / X72n1437=永覺元賢廣錄) → merged → 83.
Guide note added (§ schema): two null SenseKeys OK for corpus-wide polysemy (末後句/隨波逐浪); Senses[0]=primary; gates don't flag it.
CURRENT: b006 "House devices & teaching implements" (WAVE_PLAN 86). 15 DRAFTED describe-only (喝 2-sense, 四喝 self-def main-text;
 一喝分賓主=谷隱 not Linji; 四照用 four-fold only in 卍-footnote→sourced C077n1710; 棒 三十棒 = 德山緣密 not emblem-Deshan).
 [old gate-2 fable army died at session limit — replaced by lean v2.] All 15 still drafted (deaths wrote nothing).
 QUICK-QA (lean v2) RUNNING: 1 FABLE agent a56e6618 over all 15 (zc.verify every KWIC + spot-check attribution/counts +
 describe-only scan; fixes 喫茶去 headword). Sets STATUS=done (or verified+flag). 
 NEXT: quick-QA returns → targeted opus fix on any flagged term → merge (expect 98) → seed b007 (WAVE_PLAN line 103, 3 opus research).

## (older progress detail below)
## PROGRESS: b001✅ b002✅ b003✅ b004✅ MERGED → termbase.v2.json = 68 entries. ALL describe-only + enriched.
b004 CLOSED: strip+enrich (3 fable) done → re-gate3 3 repaired (直指人心 PASS/百尺竿頭 PASS/著語 fixed 從容錄→宏智廣錄 T48n2001)
→ 15 STATUS=done → merged → 68. b001-b003 retro (strip+enrich+2 lb fixups) done + merged earlier.

## ⚡ LEAN PER-WAVE PIPELINE v2 (user 2026-07-12 ~05:50 — the old 16-agent/wave flow hit session limits every ~5h =
## ~1 wave/window; too slow. NO MORE heavy fable gate armies. Shared toolkit + ~4 agents/wave.):
 SHARED TOOLKIT: `dictionary-build/zc.py` (import it; tested vs 乾屎橛 exemplar). zc.verify(rel,kwic)->{ok,fromLb,toLb},
  zc.count(term)->{hits,files,per_file}, zc.find, zc.title, zc.head(rough), zc.is_allowed. Excludes <note>/<app>/<rdg>
  apparatus, ed="X" primary lbs. Run python with PYTHONIOENCODING=utf-8. Agents MUST use it, not hand-roll grep scripts.
 1. SEED (main thread: IDs/dirs/manifest).
 2. RESEARCH — 3 opus agents × 5 terms, describe-only + maximal, KWICs self-verified via zc → drafted.
 3. QUICK-QA — 1 agent (FABLE first, switch to OPUS when fable pool exhausts), uses zc to verify EVERY KWIC mechanically
    + spot-check attribution/counts/titles + scan describe-only + fix obvious → STATUS=done (or `verified`+flag if deep issue).
 4. MERGE (main thread) → seed next wave.
 Model policy: research=opus; QA=fable-while-available-then-opus; targeted-fixes=opus. ~4 agents/wave (was ~16).
 Retro de-interp+enrich of b001-b004 is DONE (one-time) — not part of the per-wave flow.

## IN FLIGHT:
 b005 "House systems" (WAVE_PLAN line 69). 15 VERIFIED (gate-2 fable done — good catches: 四賓主 apparatus-footnote KWIC
 re-anchored to main-text 客看主; 隨波逐浪 non-matching KWIC fixed; 真淨克文/玉林通琇 named; T48n2007 pruned from 本來無一物).
 GATE-3 DONE: 11 PASS, 4 REVISE, 0 FAIL. REVISE all label/note/attribution-level. REPAIR RUNNING (opus ab23d77a):
  隨波逐浪(圓通法秀 roster-name, reversed-lookup miss → set MasterName; 五參提綱 note) 活人劍(qualify 刀/劍 complement — 活人刀/殺人劍 both exist)
  正中來+兼中到(X72n1437 = 永覺元賢廣錄 not 無異元來; 無異元來=X72n1435). PASS(11): 正中偏/偏中正/函蓋乾坤/平常心是道/四賓主/
  呵佛罵祖/兼中至/本來無一物/打成一片/大機大用/生死事大.
  NEXT: repair returns (grep-verified) → set 15 STATUS=done → merge (expect 83) → seed b006 (WAVE_PLAN line ~85).
 b005 term ids: 正中偏 t_d4661c1b4dbb 隨波逐浪 t_2852a9ae231c 偏中正 t_dc02eefd07f5 生死事大 t_78f95517a347 正中來 t_ccd48e1c9145
  兼中到 t_61c90d3a8edd 打成一片 t_1d3706324b0c 活人劍 t_e6eb14b6c1ca 大機大用 t_d03aa9267f79 呵佛罵祖 t_1da939bf1267
  兼中至 t_8650004bb9d7 本來無一物 t_93ab42fecdca 函蓋乾坤 t_49829f59faac 平常心是道 t_9a5dc768cbc5 四賓主 t_ed962dfd1158
 NEXT: gate-2→gate-3 fable→self-correct→set done→merge (expect 83)→seed b006 (WAVE_PLAN line ~85). Report only at b036 or blocker.
 BUDGET MODEL: research=opus, gate-2=fable, gate-3=fable, REVISE-repair=opus. If a pool exhausts, adapt per overnight directive.
NOTE: every future wave now includes a de-interp+enrich stage between gate-3 and merge (permanent pipeline change).

## PROGRESS: b001 ✅ + b002 ✅ + b003 ✅ MERGED → termbase.v2.json = 53 entries (46 dict + 7 legacy). CURRENT: b004 RESEARCH.
b003 CLOSED: 15/15 PASS (5 clean + 10 repaired-then-re-gate3-PASS, 0 FAIL). Merged via merge-dict-entries.js → 53.
b004 "Core idioms & signature koans I" (WAVE_PLAN line 52). 15 terms DRAFTED (research done, high quality;
catches: 麻三斤 洞山守初≠洞山良价, 百尺竿頭 石霜-null, 截斷眾流 三句 = Deshan Yuanming, 一大事因緣 rejected 奏劄/塔銘 front-matter).
GATE-2 verify+repair RUNNING (opus, 5×3):
 g1 ae150e61(話墮 t_427fa502a11b/直指人心 t_b8063e3d60b4/教外別傳 t_2d4525b4b123)
 g2 aa7ae91b(百尺竿頭 t_53da4e346a6f/拈古 t_66792ea088de/麻三斤 t_ce2a5ef71afe)
 g3 af078166(三玄三要 t_52391cba2cdf/殺人刀 t_d7167b5f3236/著語 t_0a686fa27769)
 g4 a951186e(本地風光 t_831f84399d0b/不立文字 t_46c30c5d57d4/一大事因緣 t_223c2f6ade25)
 g5 a380c6a6(大死 t_fd1759947989/截斷眾流 t_f7bdd2def0ec/庭前柏樹子 t_097f38f58678)
FLAGGED CALLS for gate-2/3 to resolve: 大死 K1 Touzi two-speaker (name vs null); 著語 defining occ 圜悟 評唱 voice; 百尺竿頭 石霜 roster ambiguity.
NEXT: gate-2 returns → set STATUS=verified → 5 Fable gate-3 → self-correct REVISE → merge → seed b005 (WAVE_PLAN line 69).
LESSONS baked into b004 prompts: NO collocation hints (terms+gloss only, grep-derive); NEVER write "speaker not identified"
without reading the governing cb:mulu head; X-canon ed="X" not ed="R"; exact-contiguous KWIC.
NEXT after b004: b005 (line 69), … b036. Merge each. Report to user only when b036 merged (or structural blocker).

## THE AUTONOMOUS LOOP (a compacted / fresh session: follow THIS to resume)
Gate 3 = Fable-5 agents (model:fable; codex RETIRED — cmd "App auswählen" dialogs blocked it). Merge =
`node eng/tools/merge-dict-entries.js` (keys on terms/<id>/STATUS == "done"). Orchestrator (me) OWNS STATUS files
(agents unreliable at setting them) — set drafted→verified→done from agent REPORTS.
Per wave bNN (WAVE_PLAN.md has the 15 terms/wave, b002-b036):
 1. SEED: for each term compute id = "t_"+sha256(term)[:12] (bash: `printf '%s' "$t" | sha256sum | cut -c1-12`);
    mkdir terms/<id>; append 15 todo lines to MANIFEST.jsonl (write temp jsonl via Write tool for CJK safety, cat >>).
 2. RESEARCH: 5 opus agents × 3 terms → write terms/<id>/{entry.v2.json (single DictionaryEntry, PascalCase),
    WORK.md, STATUS=drafted}. PROMPT MUST stress: EXACT-CONTIGUOUS KWIC (no ellipsis/stitch/added-punct; GREP the
    file), VERIFY SPEAKER at chapter head, allowlist-only, FromLb=nearest preceding <lb>.
 3. GATE 2 (repair): 5 opus agents × 3 → fix KWICs exact, strip contamination, tighten over-reads/abstraction,
    verify attributions → STATUS=verified. (Reference DICTIONARY_ENTRY_GUIDE.md §5/§5b.)
 4. GATE 3 (Fable): 5 fable agents × 3 → write terms/<id>/GATE3_VERDICT.md (PASS|REVISE|FAIL); do NOT edit entries.
 5. SELF-CORRECT: PASS→STATUS=done. REVISE/FAIL→spawn an opus repair agent with the verdict's punch list (fix
    prose/attribution/KWIC, GREP-verify) → re-run Fable gate-3 on those. Loop; after ~3 rounds on one term, ACCEPT
    it (mark done) if substantively sound (KWICs/attributions/multi-source hold) — don't loop forever on hair-splits.
 6. MERGE: all 15 STATUS=done → `node eng/tools/merge-dict-entries.js` → verify termbase.v2.json + legacy grew.
 7. Update this STATUS + TASK_LOG; go to next wave. Report to user only when b036 merged (or blocker).
GOTCHAS: never 2 gate-3 on same term at once; Fable agents parallel-safe; keep ledger current (compaction survival).

## CURRENT b002 research agents: r1 a6571a65(上堂/恁麼/作麼生)✓ r2 a4db0168(示眾/小參/無心)✓ r3 a7d2b0f8(公案/無事/
參禪)✓ r4 a9a3f680(作家/大悟/葛藤)✓ r5 ac44cd8f(勘破/轉語/正法眼藏)RUNNING. b002 ids in MANIFEST.jsonl.
NEXT: r5 done → launch 5 gate-2 repair agents on b002's 15 → Fable gate-3 → merge → b003.


**Phase:** AUTONOMOUS CHURN (user away). Finish b001 → merge → then ~500 terms in waves (b002…), slowly.
**Current batch:** b001 (15 terms) — in the 3-gate pipeline, step 3 (gate-2 verify+repair running).
**Roadmap:** WAVE_PLAN.md being generated (~500 terms, ~33 waves). termbase.v2.json = merge target
(C:\temp\NewTranslationrepos\CbetaZenTranslations\termbase.v2.json).

## THE PER-WAVE PIPELINE (each wave bNN)
1. Seed MANIFEST with the wave's ~15 terms (todo) + `mkdir terms/<id>` (Id = "t_"+sha256(term)[:12]).
2. Research: 5 agents × 3 terms → write terms/<id>/{entry.v2.json,WORK.md,STATUS=drafted}.
3. GATE 2 (Claude verify+repair): 5 agents → fix KWICs exact-contiguous, strip contamination, tighten
   over-reads/abstraction, nesting discipline → STATUS=verified.
4. GATE 3 (Codex): `pwsh eng/tools/codex-verify-dict-entry.ps1 -TermId <id>` per entry → CODEX_VERDICT.md
   (PASS|REVISE|FAIL). Run in small parallel batches (timeouts tuned via targeted search in CODEX_VERIFY_SPEC).
5. Self-correct: REVISE/FAIL → re-run gate-2 repair on those term(s) → re-gate-3. Loop until PASS, OR after
   ~3 tries mark the offending sense `disputed`/drop it (never merge fabrication/contamination).
6. MERGE: collect PASS entries → assemble DictionaryFile → write termbase.v2.json (rich) + downgraded
   legacy termbase.json → mark `done` in MANIFEST. (Merge script: build when first wave clears — see NEXT.)
7. Next wave.

## LOOP DIRECTIVE (user 2026-07-11)
"keep going, report back when b005 is merged." → churn b001→b005 through the full 3-gate pipeline, MERGE each,
stay quiet until b005 merged (or a structural blocker). Gate-3 model = gpt-5.6-sol (windowless codex.ps1; script
rm's stale verdict; NEVER 2 gate-3 on one term at once). Merge = `node eng/tools/merge-dict-entries.js` (STATUS=done).
Big-file entries (cite 五燈會元/景德傳燈錄 multi-MB) may no-verdict on codex timeouts → re-run, or pre-extract
cited lines to a small per-term evidence file if persistent.

## b001 gate-3 progress
done: 五位, 佛性. running: 異類中行 bly9qpfq6, 賓主 bsdki1jfm, buffalo bfmqkutcu(5.6-sol), 話頭 bzwaq6n23, 家風 bk3z8xw87.
verified-not-yet-gate3 (9): 本來面目 t_1c7d25824f85, 平常心 t_4ccf8aed47d3, 即心是佛 t_adde034233ba, 祖師西來意
t_49efe4fed8d4, 露地白牛 t_5d6035b1e800, 見性 t_c13928184189, 棒喝 t_0f97bfab265c, 機鋒 t_c1af3ecba987, 末後句 t_ab6276be6e08.

## NEXT ACTION (resume here)
b001 is at STEP 3: 6 gate-2 repair agents running (v1-v5 on the 15 terms + a dedicated buffalo repair).
WHEN THEY RETURN:
1. Confirm each terms/<id>/entry.v2.json rewritten + STATUS=verified.
2. BUILD the merge script (eng/tools/merge-dict-entries.ps1 or a node script): read verified single-
   DictionaryEntry files → assemble {SchemaVersion:2, Entries:[...]} → write termbase.v2.json + legacy.
3. Run GATE 3 (codex) on all 16 corrected entries (buffalo + 15) in parallel batches.
4. Self-correct any REVISE/FAIL (re-run gate-2 on those → re-gate-3).
5. MERGE all PASS → termbase.v2.json → mark done.
6. Read WAVE_PLAN.md → seed b002 → run the pipeline. Repeat, ~1 wave in flight at a time ("slow churn").

## b001 assignments (termId · term)
a1/v1: 異類中行 t_b4a4ae6874d0 · 本來面目 t_1c7d25824f85 · 平常心 t_4ccf8aed47d3
a2/v2: 即心是佛 t_adde034233ba · 佛性 t_ad0a8e5aac3d · 話頭 t_d190cf45c531
a3/v3: 祖師西來意 t_49efe4fed8d4 · 露地白牛 t_5d6035b1e800 · 見性 t_c13928184189
a4/v4: 家風 t_c728f3a8e02b · 五位 t_ff50c6974a36 · 棒喝 t_0f97bfab265c
a5/v5: 賓主 t_6da91f8ce284 · 機鋒 t_c1af3ecba987 · 末後句 t_ab6276be6e08
pilot: 水牯牛 t_36aa29eb1287 (buffalo — gate-3 FAILed, repair running; import as first done)
