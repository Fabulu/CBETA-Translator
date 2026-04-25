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
