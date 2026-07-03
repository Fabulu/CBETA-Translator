# Run Archive

Completed runs are logged here (newest first). Working directories remain in
`runs/CLAUDE-RUNS/<RUN-ID>-<slug>/` indefinitely â€” never auto-deleted.

---

## Entry Template

```markdown
### [RUN-YYYYMMDD-HHMM] Brief Description

**Archived:** YYYY-MM-DD HH:MM zzz
**Created:** YYYY-MM-DD HH:MM zzz
**Completed:** YYYY-MM-DD HH:MM zzz (optional)
**Duration:** ~X hours/minutes (optional)
**Working Directory:** `runs/CLAUDE-RUNS/<RUN-ID>-<slug>/`
**Branch:** branch-name (optional)

**Code Duplication:** X.XX% (optional â€” project-specific metric)

**Summary:**
[Brief description of what was accomplished]

**Deliverables:**
- [List of key files created/modified]

**Notes:** (optional)

**Outcome:** [Final result and any follow-up context]

---
```

## 2026-07-03 — Archived (backfill per audit RUN-20260702-2259, decisions D1/D2)

Entries below reconstruct the May-2026 runs that were completed but never logged here
("Archive" commits f857e77/3d8349c touched only CLAUDE.md). Source: run-dir material +
git log; where SPEC/TASK_LOG were untouched templates, the narrative comes from sibling
IMPLEMENTATION_PLAN/QA_REPORT/REVIEW docs (noted per entry).

### [RUN-20260516-1028] Segment parser bug fixes (5 bugs, 100% coverage)

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-16 10:28 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260516-1028-segment-parser-fixes/`
**Commits:** `2d32b3f` (bugs 1-3), `0cfeb5a` (bug 4), `58cbf75` (bug 5)

**Summary:** Fixed five bugs in the structural segment parser `eng/tools/build-structural-segments.js`: multi-`<p>` merge (each `<p>` now its own segment), empty-text early-exit (short `<p>` without internal `<lb>` now emit), over-broad `cb:` tag skip (allowlist so `<cb:div>` children parse), lb-before-p carry-in, and table/list content in `<cell>`/`<item>`. Added module export + `require.main` guard and a test file (8 tests: fixes, caesura U+3000, head-skip, `<lg>` verse detection).

**Deliverables:**
- `eng/tools/build-structural-segments.js`
- `eng/tools/__tests__/build-structural-segments.test.js`

**Notes:** SPEC_v1.md and TASK_LOG.md are untouched init templates; the real record is IMPLEMENTATION_PLAN.md + commits. The old Active Tasks row ("4 bugs, 99.5% coverage") was stale: bug 5 landed in `58cbf75` reaching **100% segment-map coverage (4990/4990)**.

**Outcome:** All 5 parser bugs fixed; 100% corpus coverage as of 2026-05-17.

---

### [RUN-20260514-1037] Biyan Lu juan 1-5 segment extraction (spec only)

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-14 10:37 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260514-1037-segment-biyan-j1-5/`

**Summary:** Scoped extraction of structured segments from the Blue Cliff Record Chinese source `T48n2003.xml`, juans 1-5 (source lines 608-4672): unique ids, `text_zh`, placeholder `text_en`, segment type, `<lb>` count, juan number. Translation, juans 6-10, and XML repair explicitly out of scope.

**Notes:** SPEC_v1.md is real; TASK_LOG.md is an untouched template and no output was recorded — the work was likely folded into the 05-14/15 semantic-segmentation commits (`376b551`/`c9a23ca`/`cb266b5`). Sibling dir `RUN-20260514-1037-j24nb137-segmentation/` is an empty stub (only an empty `subagents/`) created by init-run.ps1's minute-granularity ID collision (audit R1-F3).

**Outcome:** Spec-only record; superseded by the corpus-wide structural pipeline.

---

### [RUN-20260513-2243] Segment-map batch pipeline design

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-13 22:43 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260513-2243-segment-map-pipeline-design/`

**Summary:** Designed a 5-stage Haiku batch pipeline (extract + TEI pre-tag, ~3K-token batch requests, Anthropic Message Batches API, merge, validate) for all 4990 files. Measured corpus ~2.1 GB XML -> ~1.0 GB body -> ~163M input tokens; est. ~$122 with Batch discount, ~1-2h wall clock; incremental via manifest SHA-256.

**Notes:** Design-only (SPEC_v1 complete; TASK_LOG untouched template).

**Outcome:** The LLM-batch approach was reframed by RUN-20260513-2238's ARCHITECT_SYNTHESIS_v3 (structural, no-LLM Layer 1); kept as reference for a possible Layer 2.

---

### [RUN-20260513-2241] Segmentation test/validation strategy

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-13 22:41 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260513-2241-segmentation-test-strategy/`

**Summary:** Validation strategy for 4990 segment maps: JSON Schema (14-type enum), standalone `eng/validate-segments.py` (schema/coverage/quality tiers, CI exit codes), coverage rules (no gaps/overlaps, order preserved, text within 5%), 4-tier confidence thresholds, genre-specific rules, Wumenguan 10-case/60-segment golden regression set, 6-work validation set, CI + rendering tests, quality dashboard.

**Notes:** Design-only (SPEC_v1 complete; TASK_LOG "Ready for Review").

**Outcome:** Companion design to the schema and pipeline runs; informs segmentation QA.

---

### [RUN-20260513-2240] Semantic segmentation JSONL schema + taxonomy

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-13 22:40 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260513-2240-semantic-segmentation-schema/`

**Summary:** Segment-map schema design: 7 required + 7 optional fields, `segments/<collection>/<workId>.seg.jsonl` naming, 33-type taxonomy in 3 tiers (12 TEI-derivable @ confidence 1.0, 6 TEI-hinted, 15 LLM-required). Key decisions: flat type strings (grep/jq-friendly), confidence as first-class field, `speaker` separate from `type`, `parent_unit` for flat-file nesting, `tei_source` for round-tripping. ~36% of segments derivable from TEI alone.

**Notes:** Design-only (SPEC_v1 complete; TASK_LOG Complete).

**Outcome:** Schema fed the shipped implementation (`376b551`/`c9a23ca`/`cb266b5`).

---

### [RUN-20260513-2240] Haiku segmentation capability eval

**Archived:** 2026-07-03 (backfill; archival ruled by decision D1)
**Created:** 2026-05-13 22:40 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260513-2240-haiku-segmentation-eval/`

**Summary:** Assessed whether Claude Haiku can segment the 4990-file CBETA corpus into semantic JSONL. Delivered corpus profile (180 MB, largest file 17.5 MB), prompt template with 9-type taxonomy + Wumenguan few-shot, failure-mode table, 3-tier validation strategy, cost estimate ~$1,100-1,300 (Haiku full corpus ~$900 + Sonnet escalation ~$200). Key insight: TEI markup provides ~70% of boundaries for free — never send raw XML.

**Deliverables:**
- `haiku-segmentation-assessment.md`

**Outcome:** Conclusion superseded by ARCHITECT_SYNTHESIS_v3 (RUN-20260513-2238): Layer 1 is structural parsing without LLM. Run dir retained as the reference for a future Layer-2 semantic-labeling pass (deferred per decision D10).

---

### [RUN-20260512-2214] Search index polish (load-all-snippets + content-hash cache)

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-12 22:14 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260512-2214-search-index-polish/`
**Commits:** `8f04bdf`, `3d8349c`

**Summary:** Follow-up to the Search Latency Sprint. PR A: "Load all snippets" (`ISearchIndexService.LoadSnippetsForAsync`) promoting skip-verified placeholder rows to verified snippets in one action. PR B: per-file `ContentHash` cache on `SearchIndexEntry` — steady-state startup re-hash ~100-300ms -> ~5-10ms via stat-only reuse on size+mtime match, backward-compatible legacy backfill, no BuildGuid bump.

**Deliverables:**
- `Services/SearchIndexService.cs`, `Services/ISearchIndexService.cs`, `Models/SearchModels.cs`, `ViewModels/SearchTabViewModel.cs`, `Views/SearchTabView.axaml`
- `LoadAllSnippetsTests.cs`, `HashCacheTests.cs`

**Notes:** TASK_LOG.md is an untouched template; record reconstructed from SPEC_v1 + IMPLEMENTATION_PLAN + commits.

**Outcome:** Shipped in `8f04bdf` (archived by `3d8349c`, which only edited CLAUDE.md).

---

### [RUN-20260512-1754] Faith-in-Mind commentary language filter

**Archived:** 2026-07-03 (backfill)
**Created:** 2026-05-12 17:54 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260512-1754-faith-in-mind-commentary-filter/`
**Commits:** `4728e0f`, `bb095b3`

**Summary:** Greenfield commentary infrastructure with a default-deny language filter so only positively-identified Chinese commentary reaches ordinary readers (the 17 Japanese FiM entries drop at the service boundary). Four PRs: models + `CommentaryService` (mtime cache); 3-tier `CommentaryLanguageClassifier` (Tier 4 defaults to `unknown`); right-column commentary panel with empty-state placeholder; PowerShell ingestion emitting `commentary.json`. Default-deny enforced at three independent layers (classifier/filter/view). FiM test facts 7 -> 43.

**Deliverables:**
- `Services/CommentaryService.cs`, `CommentaryLanguageClassifier.cs`, `CommentaryPanelStateResolver.cs`
- `Models/ManifestInfo.cs` (`commentary_file`, `commentary_reader_languages`)
- `Views/ReadableTabView.axaml(.cs)`, `eng/tools/build-faith-in-mind-commentary.ps1`

**Notes:** TASK_LOG.md is an untouched template; QA_REPORT (10 scenarios), REVIEW, and TEST_GAPS are rich. Reviewer verdict: MERGE WITH FOLLOW-UP TICKETS (M1 manifest foot-gun, M2 side-map contamination, M4 whitespace-trim left as follow-ups).

**Outcome:** Shipped in `4728e0f`.

---

### [RUN-20260509-1059] Faith-in-Mind critical edition migration

**Archived:** 2026-07-03 (backfill; archival ruled by decision D1)
**Created:** 2026-05-09 11:00 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260509-1059-faith-in-mind-migration/`
**Commit:** `4091c1f`

**Summary:** Migrated the completed Faith-in-Mind poem-first critical edition into the desktop app: `AnchorModels.cs` + `AnchorService.cs` (JSONL anchor loaders), bilingual synchronized time-travel via `CorrectionTimelineBar.SetAnchorEvents`, FootnotesPanel auto-populated from apparatus, ToggleButton stage strip replacing the `CmbTimelineStage` ComboBox. Built Chinese (71 lines) + English TEI XML and 9 OpenZen data files. Two QA rounds fixed critical issues (TeiRenderer lacked `<l>` handling; namespace bug; 3 wiring gaps). Final: build 0 errors, 1454 tests passing (1436 + 18 new).

**Deliverables:**
- `Models/AnchorModels.cs`, `Services/AnchorService.cs`
- `EditionProcessDialog.axaml(.cs)`, `ReadableTabView.axaml(.cs)`, `CorrectionTimelineBar.axaml.cs`
- OpenZen `ce/faith-in-mind/` data files; `PROCESS_CHANGES.md` (process guide for future editions)

**Notes:** Remaining niceties (TEI schema validation — checklist 33/34 — and recording QA rounds 2-3) tracked as plan item P4.2; they do not block archival per D1.

**Outcome:** Shipped in `4091c1f` (2026-05-10); process learnings captured in PROCESS_CHANGES.md.

---

## 2026-05-12 — Archived

### [RUN-20260511-0624] Search Latency Sprint (SPA→desktop backflow)

**Archived:** 2026-05-12
**Created:** 2026-05-11 06:24 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260511-0624-search-latency-sprint/`
**Commit:** `9ffe02a`

**Summary:** Ported four high-leverage findings discovered during the SPA bigram-search port back to the desktop. Five-wave pipeline: 5 recons → architect → 4 parallel implementers → reviewer + QA + test-writer trio → Wave 5 fixes. Wave 4 caught two real blockers in Wave 3's work that Wave 5 fixed: PR3's hash basis still included mtime (so `git pull` would still bust the cache — defeating the spec), and PR4's expansion helper saw master/title pseudo-paths as "previously known" by rebuild time so they never auto-expanded. Wave 5 also surfaced a cross-PR landmine — existing users' indexes were built against the buggy `<app/>` extractor and the hash-based check correctly skips rebuild on unchanged content, so PR1's fix would have been invisible without bumping `BuildGuid`.

**Deliverables:**
- `Services/SearchIndexService.cs` — `<app/>` self-close fix, content-based hash, `IsStaleAsync` hybrid + legacy fallback, skip-verify hybrid for 2-char CJK, `BuildGuid` bump to `search-v4-app-self-close-fix`
- `Models/SearchModels.cs` — `IsSkippedVerify`, `InputHash`, Display-property suppressions
- `ViewModels/SearchTabViewModel.cs` — streaming threshold 4→1, 60ms `DispatcherTimer` coalescer, in-place `ResultGroups` mutation, `ApplyDefaultExpansionForNewGroupsOnly`, master-card init expansion, enrichment skip
- `Views/SearchTabView.axaml` — `IsSkippedVerify` placeholder template
- 3 new test files (53 facts) + 8 added to `IndexStalenessTests.cs`. 1395 → 1529 (+134), 0 fail.

**Notes:**
- One sprint commit rather than 4 PR-boundary commits — `SearchIndexService.cs` was touched by 3 PRs at scattered hunks; hunk-level surgery from a non-interactive shell was unreliable. Body lists PR boundaries explicitly; can be split via `git rebase -i` if needed.
- Deferred: PR4 H1 (default-expansion vs final-sort order — polish), per-file content-hash caching (steady-state perf), `MasterCorpusSearchService` freshness check, xxHash alternative to SHA256.

**Outcome:** All 4 sprint items shipped, reviewed, tested. Cold `無門` first-paint per QA: 150–250ms (was 2–4s). `git pull` mtime bumps no longer trigger reindex when content unchanged. Existing users get one-time forced rebuild via GUID bump to recover from the `<app/>` bug.

---

## 2026-04-25 — Archived (v6.0.0 marathon)

### [RUN-20260421-2127] Search Parity + Insights Overhaul (5-phase plan)

**Archived:** 2026-04-25
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260421-2127-search-parity-and-insights/`

**Summary:** Shipped Waves 3-5 as v6.0.0. Wave 3: post-search filter, lazy-expand TreeView, multi-master intersection filter, typeahead polish. Wave 4: hit count badges, search history, cooc suggestions, toasts, command palette (Ctrl+Shift+P), context menus, open-in-new-window. Wave 5: parallel index build, children cap, fuzzy command palette, context menu parity. 50 new tests (1199 total). Full pipeline per wave: recons → architect → implementers → reviewer → QA → test writer.

**Outcome:** All 5 phases shipped, reviewed, tested. Version 5.9.0 → 6.0.0.

---

### [RUN-20260421-2042] Unified Search Redesign (4-wave plan)

**Archived:** 2026-04-25
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260421-2042-unified-search-redesign/`

**Summary:** Original 4-wave search redesign plan. Superseded by RUN-20260421-2127 which expanded the scope to 5 phases and shipped everything.

**Outcome:** Merged into RUN-20260421-2127. All deliverables shipped in v6.0.0.

---

### [RUN-20260420-2353] Pagefind Full-Text Search + Master Corpus Sharding

**Archived:** 2026-04-25
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260420-2353-pagefind-and-master-shards/`

**Summary:** Pagefind deployed to SPA (local build + wrangler deploy). Master corpus sharded into per-master JSON files with _index.json. Dict shards bundled from 12K → 201 bucket files to fit Cloudflare 20K file limit. Hover dictionary added to SPA.

**Outcome:** SPA full-text search live. Dict bucketing shipped. Hover dict working.

---

## 2026-04-18 to 2026-04-19 — Archived (SPA + Korean Seon marathon, v4.9.3 → v5.0.1)

### SPA Overhaul + Korean Seon Lineage + Translation Stars

**Archived:** 2026-04-19
**Created:** 2026-04-18
**Duration:** ~30 hours across 2 days
**Versions:** Desktop v4.9.3 → v5.0.1, SPA continuous deploy

**Summary:**
Three major workstreams in one marathon session: (1) transformed the SPA from a basic preview into a full reading/browsing tool, (2) built the translation star system across desktop + SPA + data repos, (3) researched and implemented Korean Seon Buddhism on the lineage graph with 42 masters, 4-tier attestation, and scholarly verification.

**SPA (readzen.pages.dev):**
- Search: browse all texts, translated/untranslated/Zen filters, paginated results
- Passage reader: full text pagination (no more TOC-only), multi-edition TEI fix, bilingual toggle
- Dictionary: 11,959 CC-CEDICT shards deployed (were gitignored, never on Cloudflare)
- Lineage graph: Korean Seon color, dashed node borders, attestation edge styles, compact legend
- Masters: pagination, school colors, Korean Seon entries
- UX: font size buttons, scroll-to-top, home link, dismissible continue-reading, bookmark deep links
- Install instructions: WinGet, Setup.exe, bundled Git documented
- Feature showcase: web vs desktop distinction, desktop features section
- OpenZen license corrected (CC0/CC BY 4.0), Start Here text IDs fixed

**Desktop App:**
- Translation star system: star/unstar toggle, per-user JSONL sync, aggregated star-counts.json, ranking integration, star button in Reader + Translate tabs
- Critical onboarding fix: mandatory setup can't be bypassed, Open Folder always works as escape hatch
- Korean Seon: 42 masters with attestation tiers, dashed node borders, attestation-based edge rendering (solid/dashed/dotted/faint), legend with tier explanations, rightward layout push
- Author-field concept-name matching (fixes Yongming/Zongjing Lu detection)
- Focus behavior: direct connections only (teacher + students)
- Schools: Heze removed, normalized to 9 Chinese + Korean Seon
- 99 broken student reverse-links fixed
- 250 total masters (up from 205)

**Korean Seon Research (20+ recon waves):**
- 5-language web research (EN, ZH, KO, DE, ES)
- 7 of 9 Mountain School founders confirmed in Zutangji (952 CE)
- Legitimacy assessment: 4-tier system (A=Chinese source, B=Korean stele, C=textual, D=retroactive)
- Scholarly verification: Buswell, Ahn, Jorgensen, Van Cutsem, Sorensen cited
- Complete lineage chains: 9 Mountain Schools → Suseonsa → Dark Centuries → Hyujeong
- Bridge figures: Shiwu Qinggong, Pingshan Chulin, Ji'an Zongxin, Gunabhadra
- Musang/Wuxiang identified as early teacher of Mazu (Buswell 1989)

**Data (all repos synced):**
- master-dates.json / masters.json: 250 masters, 42 Korean Seon, 45 attestation fields
- master-corpus.json: rebuilt with author-field fix, 217 reliable masters, 726 KB
- zen_texts.json: curated list moved to translations repo
- .gitattributes: star merge rules added
- auto-merge workflow: star-counts.json added to allowlist

**Key files modified (desktop):**
- Models/StarEntry.cs, Services/ITranslationStarService.cs, Services/TranslationStarService.cs (NEW)
- Models/LineageGraphNode.cs, Models/MasterDateEntry.cs, Models/ZenMasterModels.cs
- Services/MasterCorpusSearchService.cs, Services/ZenMasterManagerService.cs
- ViewModels/MainWindowViewModel.cs, ViewModels/GitTabViewModel.cs, ViewModels/LineageGraphViewModel.cs
- Views/MainWindow.axaml.cs, Views/LineageWebControl.cs
- Views/ReadableTabView.axaml + .cs, Views/TranslationTabView.axaml + .cs
- Views/GitTabView.axaml.cs, Services/ServiceCollectionExtensions.cs

**Key files modified (SPA):**
- views/lineage-graph.js, views/masters-browse.js, views/passage.js, views/search.js
- views/landing.js, views/master.js, views/shell.js
- lib/github.js, lib/format.js, lib/tei.js, lib/reading-lists.js, lib/inline-dict.js
- style.css, app.js, .gitignore

**Outcome:** v5.0.1 released. SPA is now a standalone useful tool. Korean Seon lineage is the most thoroughly sourced visualization of Korean Buddhist transmission that exists online. Translation star system ready for community use.

---

## 2026-04-16 to 2026-04-18 — Archived (marathon CE session, v4.5.0 → v4.9.2)

| Run ID | Description | Outcome |
|--------|-------------|---------|
| RUN-20260416-1711 | Ewk-friendly install (6 phases) | Completed: tour split, Velopack, WinGet, 50 label renames, SPA landing, README rewrite |
| RUN-20260416-2302 | Lineage focus + edition dates + Find button + Copy | Completed: click-to-focus, 4 date types, Find buttons, right-click Copy |
| RUN-20260417-0420 | Selection-based TM + History rendering + concordance + naming audit + bug fixes | Completed: selection TM in all 3 assistants, Translation History XML→readable, concordance toggle, 50 label renames, Zen filter fix, ghost translation fix, status fix, freeze fix, Git sync fix |
| RUN-20260418-0925 | CE Time-Travel Translation (5 phases) | Completed: correction scrubber, bilingual reconstruction, drift detection, confidence heatmap, variant-aware TM, translation diff protocol |
| RUN-20260418-1247 | CE Forensic Provenance | Completed: Pdfium PDF viewer, 4 forensic log parsers, OCR consensus/rejected readings/translation reasoning/character provenance, edition agent master instructions |
| RUN-20260418-1403 | Witness Evidence Viewer | Completed: PNG page images, character provenance flyout, fuzzy text matching, OCR readings panel, witness selector |
| RUN-20260418-1536 | Classical Apparatus Features | Completed: collation table, Leiden notation toggle, stemma visualization, char-level diff |
| RUN-20260418-1613 | CE Export Formats + UX Polish | Completed: TEI/PDF/HTML/LaTeX/CSV/plain-text exports, export UI dialog, time-travel mode indicator, 24 of 25 UX audit fixes, flaky test fix |

**Combined stats:** v4.4.0 → v4.9.2, tests 1044 → 1132 (+88), ~60 agent swarm waves, 3 protocol documents, ~50 new files, QuestPDF + Docnet.Core dependencies added.

---

### [RUN-20260416-1109] Witness Variant Viewer (v4.4.0, Task #33)

**Archived:** 2026-04-16 12:30 CET
**Created:** 2026-04-16 11:09 CET
**Duration:** ~1.5 hours
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260416-1109-witness-variant-viewer/`

**Summary:**
Completed the witness comparison feature per PROGRAMMER_WITNESS_DELIVERY_AND_VIEWER_BRIEF_2026-04-15.md. Foundation (models, service, panel) was already shipped in earlier sessions; this run wired it into the UI, published reference data, added the missing model field, built the full text viewer, and added tests.

**Deliverables:**
- Models/WitnessTextRegistry.cs — added `alignment_statuses_supported`
- Views/EditionProcessDialog.axaml.cs — Sources tab enrichment + Apparatus tab "Compare witnesses" + empty-apparatus fallback listing
- Views/WitnessComparisonPanel.axaml.cs — clickable sigla, OpenWitnessFullTextRequested event
- Views/WitnessTextViewerWindow.axaml + .cs (NEW) — full witness text viewer with copy, source-open, status banner
- Views/MainWindow.axaml.cs — wires WitnessTextService into EditionProcessDialog
- README.md — added Witness Comparison feature description
- ReadZen.Tests/Services/WitnessTextServiceTests.cs (NEW) — 5 tests, 1044/1044 passing
- C:/Programmieren/OpenZenTexts/xml-open/pd/wumenguan-1632/witnesses.json (NEW)
- v4.4.0 tag pushed

**Outcome:** All 7 spec acceptance criteria pass. Users can: open Edition Details → see delivery banner per witness → click Compare on any apparatus locus → see differing-first comparison popup → click any siglum to open full witness text. Reference data shipped for wumenguan-1632.

---

### [RUN-20260415-0049] Witness Delivery Viewer (superseded stub)

**Archived:** 2026-04-16 12:30 CET
**Created:** 2026-04-15 00:49 CET
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260415-0049-witness-delivery-viewer/`

**Summary:**
Run was initialized but the SPEC was never filled in. The actual witness delivery work began in commits e410f9f and 0d7840d (witness alignment contract + comparison panel models/service/UI), then was completed in RUN-20260416-1109.

**Outcome:** Superseded by RUN-20260416-1109. This run directory is preserved for context but had no concrete deliverables of its own.

---

### [RUN-20260416-0421] Corpus Snippets, Reader Nav, Auto-export (v4.3.0)

**Archived:** 2026-04-16 11:00 CET
**Created:** 2026-04-16 04:21 CET
**Duration:** ~6 hours
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260416-0421-corpus-snippets-and-master-nav/`

**Summary:**
Four-wave run fixing corpus search quality and adding navigation:
1. Snippet XML cruft eliminated (300-char window + thorough tag cleanup)
2. Concept-name filter (法眼, 無門, 大慧, 國師, 六祖): require longer name corroboration to drop ~5150 false positives
3. Reader "View Master →" button in study panel's master bio section
4. Auto-export masters.json + master-corpus.json to translations repo during auto-index

Later extended with: Wansong Xingxiu + full Caodong northern chain (9 new masters totaling 204), "196 of 204" status clarity, README overhaul, version bump to v4.3.0.

**Deliverables:**
- Services/MasterCorpusSearchService.cs (ExtractSnippet, ConceptNames, Export methods)
- Views/ReadableTabView.axaml + .cs (View Master button, OpenMasterRequested event)
- ViewModels/MainWindowViewModel.cs (auto-export call, status text fix)
- Assets/Data/master-dates.json (204 masters)
- README.md (expanded master database + search sections)
- v4.3.0 tag pushed

**Outcome:** Full corpus quality pipeline shipping with clean snippets, accurate appearance counts, and rich lineage chains.

---

### [RUN-20260416-0030] Zen Master Deep Links + Web Profile Pages (v4.2.0)

**Archived:** 2026-04-16 11:00 CET
**Created:** 2026-04-16 00:30 CET
**Duration:** ~4 hours
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260416-0030-zen-master-deeplinks/`

**Summary:**
Made zen master data shareable on the web. Right-click on master list items offers Copy Link / Copy Reddit Link. Shareable URLs became path-routed underscore-slug form (`readzen.pages.dev/master/Linji_Yixuan` instead of `%20`-encoded hash form). Published canonical masters.json + master-corpus.json to CbetaZenTranslations repo so the readzen-page SPA can serve rich profile pages showing bio, lineage (clickable teacher/students), references, and text appearances. SPA `views/master.js` rewritten to consume the canonical data instead of per-user JSONL.

**Deliverables:**
- Services/ZenUriParser.cs (BuildShareableMasterUrl, underscore URLs, path-routed)
- Views/ZenMasterManagerWindow.axaml + .cs (context menu, clickable teacher/student links)
- Views/MainWindow.axaml.cs (CorpusNavigationRequested wiring)
- CbetaZenTranslations/masters.json + master-corpus.json (web data)
- readzen-page/views/master.js + style.css (rich profile pages)
- readzen-page/lib/route.js (underscore parsing)
- v4.2.0 tag pushed

**Outcome:** Any Zen master now has a shareable public profile page. Reddit/Twitter/email friendly URLs.

---

### [RUN-20260415-0123] Master Corpus Search + Masters Tab (v4.1.0)

**Archived:** 2026-04-16 11:00 CET
**Created:** 2026-04-15 01:23 CET
**Duration:** ~18 hours across two days (data recon + implementation)
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260415-0123-master-corpus-search/`

**Summary:**
Built the Masters tab in MainWindow as a first-class work area. Corpus scanner
indexes all XML files for master name mentions, classifies primary vs secondary
appearances, computes co-occurrence stats. Auto-indexes at startup alongside
search index and TM. Lineage Web visualization with temporal Y-axis, viewport
culling, orphan layout, double-click navigation, and now a zoom slider for users
without mouse wheels. Massive data expansion: 156 → 204 masters with full lineage
chains, 400+ reference links, rationalized school taxonomy per McRae/Jia/Poceski.

**Deliverables:**
- Models/MasterCorpusIndex.cs
- Services/MasterCorpusSearchService.cs
- Views/ZenMasterManagerWindow.axaml + .cs (Corpus tab, Lineage Web zoom)
- Views/LineageWebControl.cs (Zoom property, SetZoom method)
- Views/MainWindow.axaml (Masters tab)
- Assets/Data/master-dates.json (204 masters, concept-filter, corrected lineages)
- v4.1.0 + v4.2.0 + v4.3.0 tags

**Outcome:** Full Chan/Zen master database with searchable corpus appearances, lineage graph, and web profile pages. Foundation for all later master work.

---

### [RUN-20260408-1903] Repo Split: Separate CBETA Originals from Translations

**Archived:** 2026-04-08 22:43 +02:00
**Created:** 2026-04-08 19:03 +02:00
**Duration:** ~4 hours
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260408-1903-repo-split-migration/`

**Summary:**
Split single CbetaZenTexts repo into two: CbetaZenTexts (originals only) and
CbetaZenTranslations (12 translated files + community + metadata). App updated
for two-repo model with discovery by convention (xml-p5/xml-p5t). Automatic
migration for existing users. Full-app audit across all tabs and features.

**Deliverables (11 commits):**
- Phase 1: Repo surgery — both repos pushed to GitHub
- Phase 2: App code — AppPaths discovery, MainWindowViewModel two-root, GitTabViewModel two-repo clone/sync/commit
- Phase 3: LegacyRepoMigration.cs — automatic, idempotent migration with marker file
- Phase 4: QA — 5+ recon waves, found and fixed bugs in scholar, search, tagging, zen master
- Search: multi-dir indexing (community + all personal dirs), IsStaleAsync perf optimization
- Fixed pre-existing git commit staging bug for personal translations
- Removed dead md-p5t code
- 808/808 tests pass, 0 errors, 0 warnings

**Outcome:** All pushed to origin/main. Both repos live on GitHub (Fabulu/CbetaZenTexts, Fabulu/CbetaZenTranslations).

---

### [RUN-20260402-1757] Reader Assistant + Dictionary Integration

**Archived:** 2026-04-08 22:43 +02:00
**Created:** 2026-04-02 17:57 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260402-1757-reader-assistant-dictionary/`

**Summary:**
Reader assistant and dictionary integration work. Superseded by broader work in subsequent sessions.

**Outcome:** Work incorporated into later runs. Archived as stale.

---

### [RUN-20260402-2301] Scholar + Bugfixes + Enhancements (4 Phases)

**Archived:** 2026-04-08 22:43 +02:00
**Created:** 2026-04-02 23:01 +02:00
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260402-2301-scholar-bugfixes-enhancements/`

**Summary:**
Scholar tab enhancements and bugfixes across 4 phases. Superseded by broader work in subsequent sessions.

**Outcome:** Work incorporated into later runs. Archived as stale.

---

### [RUN-20260330-2200] Performance + Tagging + Per-User Data + Phase 1-3

**Archived:** 2026-04-01 21:37 CET
**Created:** 2026-03-30 22:00 CET
**Duration:** ~24 hours across 2 sessions
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260330-2200-perf-improvements/`

**Summary:**
Massive feature sprint covering performance optimizations, complete tagging/coding
system overhaul, per-user data model (translations, termbases, scholar collections),
Git sync for all user data types, compare windows (tags + translations), and
scholar-tag cross-referencing.

**Deliverables (27 commits):**
- Performance: binary search colorizers, mtime status refresh, hover pooling, async assistant
- Tagging: English-side tagging, snap-to-tag, dual-editor highlights, color picker, drag-drop slots
- Phase 1: tag sync via Git, tag filter in Search, CompareTagsWindow
- Phase 2: community termbases in assistant, user pickers, per-user translation dirs
- Phase 3: CompareTranslationsWindow, git sync translations, scholar-tag cross-ref (N key)
- 23 new tests + 5 pre-existing test fixes (553/553 green)

**Outcome:** All pushed to origin/main. Ready for testing.

---

### [RUN-20260331-2005] Assistant + Coding Fixes

**Archived:** 2026-04-01 21:37 CET
**Created:** 2026-03-31 20:05 CET
**Working Directory:** `runs/CLAUDE-RUNS/RUN-20260331-2005-assistant-and-coding-fixes/`

**Summary:**
Fixed assistant UI lock-up and coding/tagging bugs. Merged into main run above.

**Outcome:** Merged into RUN-20260330-2200 pipeline.

---

<!-- Entries go above this line, newest first -->


## 2026-04-12 — Archived (marathon session)

| Run ID | Description | Outcome |
|--------|-------------|---------|
| RUN-20260408-2246 | Rebrand: CbetaTranslator → ReadZen | Completed prior session |
| RUN-20260411-0054 | ZenLink rich preview page (8 link types) | Superseded by SPA OpenZen upgrade |
| RUN-20260412-0052 | zenlinkpage SPA OpenZen upgrade (full pipeline) | Completed: 9 commits, 125 tests, reviewed, all findings fixed |
| RUN-20260412-0110 | Wumenguan 1632 OpenZen import | Completed: TEI + converter + regen from updated source |
| RUN-20260412-0300 | OpenZen clone misconfig investigation | Completed: external cause + code hardened (858de5e) |
| RUN-20260412-0400 | Tutorial OpenZen update | Completed: 6 new steps (43→49) |
| RUN-20260412-0942 | Provenance browser | Completed: panel + manifest service + markdown renderer |
