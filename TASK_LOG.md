
## 2026-07-17 17:10 — user said GO. Wave 1 dispatched (3 collision-free implementers)

Repo states checked first:
- ZenLinkPage: clean @ bd0408f5 except 4 WIP dict files → Phase A/B in WORKTREES off HEAD
  (structurally can't see/touch the WIP).
- read-zen: only 2 commits, 23 uncommitted files (whole search stack in working tree) →
  worktree-hostile; read-zen work must run in main tree, serialized by file ownership.

**Wave 1 (launched, parallel, no file collisions):**
- Phase A (a5086ac5) — ZenLinkPage builder, worktree. Scope filter, EN word-terms,
  english-normalize.js, SHARD_COUNT=1024, manifest v4, LFS guard, text/ non-ship,
  README fix, ADD env knobs. Forbidden: 4 WIP dict files.
- Phase B (a7d41285) — ZenLinkPage SPA runtime, worktree. Delete XOR, word-run
  extraction, density ranking, capability gate. Router+ranking only. Forbidden: WIP + highlight.js.
- G1 (a3b68988) — read-zen src/shared/overlay.ts + test ONLY (2 new files, no collision
  with the 23 dirty). Compose-never-suppress, data-term reserved, UTF-16 offsets.

**Wave 2 (HELD — read-zen, shared files):** Phase C (engine), D (KWIC/seg/deep-link),
G2 (adapter+endpoints), G3 (client UX), G4 (ingestion). These share api.ts/server.ts/
post-card.ts → must be sequenced by file ownership after Wave 1, NOT swarmed. D+G3 both
touch post-card.ts render seam → likely ONE implementer owns the overlay integration
there (serves both KWIC-mark + dict-underline via overlay.ts).
Deep-link FIX from Phase 0: path form workId/startLb-endLb/en/Translator (query form
doesn't round-trip). Nothing pushed/deployed.

## 2026-07-17 17:25 — read-zen checkpointed; Wave 2a launched

**read-zen checkpoint (enables worktrees):**
- 430fd4a — search stack + cjk-normalize (user's shipped-to-playtest work, was uncommitted)
- 2518f8e — shared overlay compositor (G1). Tree clean. git user bammf1.

**Wave 2a (worktrees off 2518f8e, code against frozen contract):**
- Phase C (a638f6fd) — Devvit engine: manifest-driven modulo, word runs, density rank,
  work-level join, scope.ts + cases-in-scope guard, work-grouped hit shape in api.ts
  (the D integration point), delete stale commit_sha comments. Flagged index-groundtruth
  stale-fixture hazard (don't green against retired artifact).
- G2 (adba3aa3) — dict adapter (termbase-data.ts, SchemaVersion@2 gate, case-tolerant,
  RelPath→workId) + index/entry endpoints (Redis-ceiling-safe), negative pin on v2.json.

**Wave 2b (HELD until C+G2 land + I verify api.ts/server.ts shapes):**
- Phase D + G3 render seam (ONE owner: kwic.ts, segments.ts, github.ts, render.ts,
  post-card.ts, reader.ts — KWIC-mark + dict-underline + panel, all via overlay.ts;
  deep-link PATH form per Phase 0). Needs C's SearchHit + G2's dict endpoints verified real.
- G4 ingestion (dict.ts, resolve.ts) — after G2.

Still running: Phase A (a5086ac5), Phase B (a7d41285) in ZenLinkPage worktrees.
Nothing pushed/deployed.

## 2026-07-17 17:50 — worktree-isolation scare → resolved, work is safe

**What happened:** `isolation:worktree` makes a worktree of the SESSION's primary repo
(CBETA-Translator), NOT of the cross-repo target (ZenLinkPage/read-zen) the agents edit.
Those harness worktrees are empty CBETA checkouts (junk, to prune).

**But the agents self-isolated correctly:** each created its OWN sibling worktree+branch
in the actual target repo. Verified on disk:
- ZenLinkPage-phaseA [phaseA-bilingual-index] — Phase A work: build/*.js edits (24 scope/
  env markers), lib/english-normalize.js, SHARD_COUNT=1024. Terse report was just a
  verification build left running; work is complete. → committed 521cfc08.
- ZenLinkPage-phaseB [phase-b-spa-search] — Phase B: bigram-search.js, search.js,
  views/search.js + tests, 604 pass. → committed 6cead79f.
- read-zen-phase-c [phase-c-devvit-search] — Phase C, STILL RUNNING.
- read-zen-g2 [g2-termbase-data] — G2, STILL RUNNING.

**No collisions occurred** — read-zen main tree clean (dirty=0), each agent in its own
worktree. My earlier "C and G2 colliding in main tree" worry was WRONG (verified).

**Correction to my mental model:** for cross-repo agent work, worktree isolation of the
primary repo is useless; agents making their own target-repo branches is what actually
worked. Future: launch cross-repo agents WITHOUT isolation flag and have them branch in-target.

**Next:** await C+G2 → integrate A+B into ZenLinkPage (disjoint, clean) and C+G2 into
read-zen (share api.ts/server.ts, resolve) → Wave 2b (D+G3, G4) → reviewer/QA/test-writer.
Nothing merged to main, nothing pushed/deployed. Phase B updated 2 existing tests + added 10.

## 2026-07-17 17:55 — Phase A final report verified

phaseA-bilingual-index @ 521cfc08, 1 commit, clean worktree, 597 tests pass.
CJK byte-identical check PASSED (scope/reorder doesn't alter Chinese indexing).
Real scoped smoke build: docCount 483 (464 zh + 19 en real, pre-MT-pass), index
shards 66.9 MB, no text/, EN word-terms present, X70n1394 the sole EN exclusion.

**GOOD AGENT OVERRIDE (recorded):** kept SHARD_COUNT/TEXT_SHARD_COUNT default = 4096
(not my briefed 1024) so the live SPA full-corpus `npm run build:search` stays
byte-identical. Clients read manifest.shardCount. Scoped Devvit publish MUST pass env:
  SCOPE_FILE=<scope-zen-translated.json> OPENZEN_COMMUNITY_DIR=<...> \
  SHARD_COUNT=1024 SKIP_TEXT_SHARDS=1
  → 🔴 CUTOVER-CRITICAL: capture this in the Phase E publish command.
Also needs package.json to add SHARD_COUNT=4096 to the SPA build script if the default
ever changes (out of Phase A scope — noted).

Awaiting Phase C + G2. Then integrate A+B (ZenLinkPage) and C+G2 (read-zen).

## 2026-07-17 18:05 — G2 done; cross-agent catch fixed at base

**G2 (dict adapter+endpoints):** g2-termbase-data @ c78c0f7, 25/25 tests. Frozen for G3/G4:
- Types in api.ts: DictIndex/DictEntry/DictSense/DictOccurrence + DictIndexRsp/DictEntryRsp.
- Endpoints: api/dict/index (200 DictIndexRsp | 503 unavailable), api/dict/entry?term=
  (200 DictEntryRsp | 404). Server fns getDictIndex()/getDictEntry(term).
- SchemaVersion@99→undefined→503 (never mis-render); negative pin v2.json never fetched; real
  entry 木人/木佛 parsed from termbase/040.json fixture.
- Sensible deviations flagged: exported MAX_CACHE_BYTES in tei-cache.ts (contract said import
  it, it wasn't exported — additive); interface→type (writeJson needs index sig); added cacheRead
  robustness; SourceTexts RelPath→workId.

**🔴 G2 caught a real bug in G1 (cross-agent verification working):** overlay.test.ts had 6
tsc errors under noUncheckedIndexedAccess (shuffle swap + map[i]/frags[0] index reads). G1's
runtime tests were green (17/17) but it never ran tsc --build on the TEST file, so test:types
(which the repo's `test` runs first) was RED — blocking every downstream branch. Verified myself,
fixed with non-null assertions (no semantic change), tsc now 0 errors, 17/17 still pass.
Committed to read-zen main fd7a868. C's branch (off old 2518f8e) merges clean (disjoint).

**Status:** A ✅ B ✅ G1 ✅(+fix) G2 ✅ · Phase C still running.
Next: await C → integrate A+B (ZenLinkPage) + C+G2 (read-zen, resolve api.ts/server.ts) →
Wave 2b (D+G3, G4) → reviewer/QA/test-writer. Nothing pushed/deployed/merged-to-main.

## 2026-07-17 18:20 — Phase C done; read-zen INTEGRATED

Phase C (phase-c-devvit-search 7a7edc2): bigram-client manifest-driven modulo + word runs +
joinAndRank density; engine XOR removed, scope-gated, groupRanked; verify-text per-side;
NEW scope.ts (+cases-in-scope guard) + english-normalize.ts; routeForSide PATH form (Phase-0
deep-link fix). WorkSubHit/WorkGroupHit frozen in api.ts (D's integration point). Tests:
bigram-client 25, engine 15, scope 3, english-normalize 4 — green.

**Integration onto read-zen main (all local):**
- merge G2 (clean) → merge C (clean auto-merge; api.ts/server.ts additive, both survived:
  WorkGroupHit×6 + DictEntry×6, dict endpoints×15 + works×3, verified).
- **2 NEW tsc errors caught post-merge:** G2's test files used `import ...with{type:'json'}`
  (passes esbuild, fails repo tsc --build TS2823). Same blind-spot class as G1. Fixed to the
  repo's readFileSync+import.meta.url idiom (91c9855); fixture loads verified (2 entries 木人/木佛).
- Type gate: **0 new errors.** 3 pre-existing red tests remain (index-groundtruth ×2,
  spa-parity ×1) — confirmed red at HEAD by C, need Phase-E cutover fixture regeneration.

**Node-20 shell limit (honest):** cannot run the repo's real `test:unit` (needs Node 22.6+
for --experimental-strip-types); esbuild-bundle proxy breaks import.meta.url fixtures. Full
read-zen suite must be run on Node 22 in the QA/test-writer phase.

**read-zen main = integrated base** (overlay+G1fix+G2+C). Launching Wave 2b-i off it.
ZenLinkPage A+B not yet integrated (doesn't gate 2b; disjoint, will do onto integration branch).

## 2026-07-17 18:35 — G4 integrated

G4 (g4-dict-ingestion 793d90e): dict.ts provider (resolves via G2's getDictEntry, term card
+ old-Reddit textFallback + rzLink outbound), registered in resolve.ts between master/sample,
provider.ts name-union widened (3rd file, type-only, flagged). dict.test 6/6, render 6/6,
ingest 21/21. Merged clean into main → c28cd57. tsc --build 0 errors.

Clarification: the 3 "pre-existing red" tests (index-groundtruth ×2, spa-parity ×1) are
test:unit RUNTIME failures (docCount/fixture drift), NOT tsc type errors — so tsc --build is
genuinely clean; runtime reds need Phase-E fixture regeneration.

read-zen main = base + overlay(+fix) + G2 + C + G4. Still running: D1 (KWIC/seg/deep-link
server). After D1: D2/G3 client render seam (underlines + panel + seg styling + KWIC highlight
via overlay). Then integrate ZenLinkPage A+B, then reviewer/QA/test-writer. Nothing pushed.

## 2026-07-17 18:50 — D1 integrated; ZenLinkPage A+B integrated & GREEN

**read-zen:** D1 merged clean → 40afb5c (all api.ts contribs present: WorkGroupHit/DictEntry/
KwicRow/SegLabel; tsc 0). Removed stray bash.exe.stackdump (b3533c8) + gitignored it.
read-zen main = base+overlay(+fix)+G2+C+G4+D1, type-clean.
**D2/G3 (a44bf668) LAUNCHED** off integrated main — client render seam: dict underlines
(computeZenMarks port, CJK-only), click→responsive panel (dock≥720/sheet<720, stays-open/
dismissible), seg-v1 block styling, KWIC highlight — ALL composited via shared/overlay.ts.
Link mapping = recommended mix. Own worktree read-zen-d2 [d2g3-client-render].

**ZenLinkPage:** integration worktree ZenLinkPage-integ [bilingual-search-spa] off main.
Merged Phase A (11a60ba1) + Phase B (6ff0b8eb), 0 conflicts. **node --test 607/607 GREEN**
(real suite, plain JS — no Node-22 caveat). SPA half fully verified. WIP dict files not on
this branch (off bd0408f5), untouched.

**Remaining:** D2/G3 (running) → integrate it → reviewer → opus QA (runs full read-zen
test:unit on Node 22 + regenerates the 3 drifted fixtures) → test writer. Nothing pushed/deployed.

## 2026-07-17 19:05 — ALL IMPLEMENTERS DONE + INTEGRATED

D2/G3 (a556299) merged clean → c668aea. read-zen main = FULL feature:
overlay+G1fix + G2(dict adapter/endpoints) + C(bilingual engine) + G4(ingestion) +
D1(KWIC/seg/deep-link) + D2/G3(client seam: underlines, panel, seg styling, KWIC via overlay).
tsc --build 0 errors; client+server esbuild bundles clean.

ZenLinkPage: A+B on bilingual-search-spa, 607/607 green (real suite).

**Known-remaining (not breakage):**
- read-zen 3 pre-existing runtime reds (index-groundtruth ×2, spa-parity ×1) — fixture drift,
  need regeneration in QA.
- Full read-zen test:unit needs Node 22 (shell is 20) — QA/user runs it.
- Panel "not annoying" feel at both widths = playtest, headless can't verify (D2/G3 flagged).

**Launching review pipeline (user-requested):** Fable reviewer → opus QA → opus test writer.
Nothing pushed/deployed/merged-to-ship-branch. read-zen main advanced past the user's original
2-commit state by design (all reversible; checkpoint 430fd4a is pre-feature).

## 2026-07-17 19:20 — Fable reviewer surfaced real issues (report was incomplete)

Reviewer (a17208e0) delivered a PARTIAL report — named 2 findings without detail:
- "blocker-level KWIC integration gap" (read-zen)
- "confirmed mixed-query verification bug on the SPA side" (ZenLinkPage)
Also referenced a dict-track sub-review that never reported to main.

Resumed the reviewer for the COMPLETE ranked list (severity, file:line, confirmed-vs-suspected,
failing scenario per finding; verified-clean per seam). HOLDING QA + fixes until findings land —
not acting on named-but-undetailed findings. The review is doing its job: real issues exist
pre-ship, which is why the pipeline runs. Nothing merged to ship / pushed / deployed.

## 2026-07-17 19:35 — Fable review COMPLETE (full ranked findings)

**Verdict: architecture sound (all 8 seams VERIFIED CLEAN), but 1 BLOCKER + 2 majors before ship.**

**BLOCKER #1 — KWIC drill-down not integrated (read-zen). CONFIRMED.** `api/kwic` has ZERO
client callers. D1 built the server endpoint, D2 built the render seam (`ReaderMarks.hits`),
but NOBODY wired the client drill-down: no `fetchKwic`, compose-pick wires sub-hits straight
to the whole-work line picker (caps at first 400 lines via sliceIds). → the user's core flow
"search → KWIC → zoom to matched passage" is UNREACHABLE; a match past line 400 (routine in
Blue Cliff Record) can't be reached at all. **This is my decomposition gap: D1=server, D2=render
seam, but the KWIC drill-down UI (fetchKwic + compose-pick KWIC rows + hit-range wiring) fell
between them, unowned.** Needs a D3.

**MAJOR #2 — SPA mixed-query verify falsely kills matching EN rows (ZenLinkPage). CONFIRMED.**
`bigram-search.js:239-250` + views/search.js. Query `祖師西來意 gate`: an en doc that matched
CJK via inline Chinese gets verified against word needles only → 0 → removed + docId marked dead.
**MAJOR #3 — SPA verifyDocPhrase hardcodes TEXT_SHARD_COUNT=4096 (ZenLinkPage). CONFIRMED latent.**
Ignores manifest.textShards (incl null). CONTRACT §1 violation. SPA prod (4096) unaffected today.

**#4 — read-zen ZERO tests for new ranking/join/capability core.** joinAndRank/densityOf/
rankScript/groupRanked/hasWordTermsCapability untested; SPA twin has them → asymmetric pin.
Test-writer's job.

MINORS 6-13 (scope leak pre-cutover [plan-blessed], KWIC row mislabel when translation absent,
overlay SPA-parity fixture missing, english-normalize no drift guard/3 copies, round-trip doesn't
check SPA grammar, dict client caches transient failures page-lifetime, partial-translation note
missing, v2-shard fallback drops word runs silently). NITS 14-20 (incl #20: ReaderMarks.hits
carries no side — EN offsets would tint wrong ZH chars if KWIC wired naively → RELEVANT to fixing #1).

**8 seams all clean; WIP contamination ZERO; no Node-22 landmines found.**

HOLDING for user steer on the fix plan (D3 KWIC drill-down is real new work).

## 2026-07-18 — user steered: build D3 + fold 2 SPA majors + minors + test-gap

User decisions on review: BUILD the KWIC drill-down (D3) now; fold in the 2 SPA majors,
worth-it minors (#11 dict cache, #7 KWIC mislabel, #12 partial-translation note), and the
test-coverage gap (#4 → test writer).

**Launched (parallel, different repos, no collision):**
- D3 (a93d90d9) — read-zen client KWIC drill-down: fetchKwic + compose-pick concordance UI +
  passage-precise navigation + SIDE-AWARE hit layer (fixes nit #20: EN hits highlight EN
  column). Folds in #7 (kwic.ts honest side labeling), #11 (fetch.ts don't permanently cache
  failures), #12 (partial-translation note). Worktree read-zen-d3 off main c668aea.
- SPA-fix (a4309c9a) — ZenLinkPage #2 (mixed-query verify: union CJK+word needles for en docs,
  don't kill inline-CJK matches) + #3 (read manifest.textShards, skip on null, no hardcoded
  4096). Worktree ZenLinkPage-spafix off bilingual-search-spa. Must keep 607/607 + new tests.

**HELD:** test writer (#4 read-zen ranking/join/capability coverage) — until D3 lands so it
tests final code. Then opus QA (full test:unit on Node 22 = user's env + fixture regen).
Nothing pushed/deployed/merged-to-ship.

## 2026-07-18 — SPA majors integrated & GREEN

spa-major-fixes (4cfc913e) merged into bilingual-search-spa clean. node --test 613/613
(was 607, +6). #2 mixed-query verify: en docs verify against UNION of word+CJK needles
(OR-presence), don't drop inline-CJK matches, honest counts. #3: textShardCountFromManifest
reads manifest.textShards.count; null→skip-before-fetch; absent→legacy 4096 (production
byte-identical — well-reasoned deviation, flagged). ZenLinkPage SPA side COMPLETE + verified.

Still running: D3 (read-zen KWIC drill-down + #7/#11/#12 + side-aware hit layer). Then integrate
D3 → test writer (#4) → opus QA (Node-22 full suite + fixture regen). Nothing pushed/deployed.
