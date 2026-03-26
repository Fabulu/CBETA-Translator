# Translation Assistant Multi-Agent Workflows

## Goal
Fix a connected bug cluster in Translation Assistant and Search:
- AI baseline returns noisy matches (including same-book/self matches that should be excluded).
- Shared highlighting is often empty, so the UI shows a hit without telling what matched.
- Matching should ignore editorial punctuation (`。`, `，`, etc.) and line breaks.
- Matching must support phrases spanning line boundaries, but without exploding false positives.
- Translation tab and Search tab should use one consistent matching policy.

## Current Code Hotspots
- `Views/TranslationTabView.axaml.cs`
  - `PublishCurrentSegment()` builds `ZhContext = prev + current + next` with no separator.
  - `UpdateTmSharedHighlights(...)` and `BuildTmHighlightRanges(...)` handle UI highlight ranges.
- `Services/TranslationMemoryService.cs`
  - `FindReferenceMatchesAsync(...)` scores against both single line and widened context.
  - `Normalize(...)` strips whitespace/CJK punctuation for matching.
  - `IsExactCurrentSegment(...)` currently misses some same-book exclusions for reference rows.
- `Services/TranslationAssistantBuildService.cs`
  - Builds single-line and 2-line reference rows (`AI baseline`) into `translation-memory.reference.jsonl`.
- `Services/SearchIndexService.cs`
  - Also strips spacing/punctuation for CJK compact matching; behavior should be aligned.

---

## Workflow Option A: Forensic + Minimal-Risk Patch
Best when you want the fastest safe fix with minimal refactor.

### Team
1. Agent A1 - Matcher Forensics
   - Inspect scoring and self-exclusion rules in `TranslationMemoryService`.
   - Produce concrete false-positive repros from current code paths.
2. Agent A2 - Highlight Forensics
   - Trace why `ReferenceMatches` can exist while highlight ranges are empty.
   - Validate `FindSharedChineseRanges(...)` behavior against punctuation-stripped matching.
3. Agent A3 - Patch Implementer
   - Implement focused fixes in TM filtering/scoring + highlight fallback.
4. Agent A4 - Search Alignment Verifier
   - Ensure Search tab matching policy uses same normalization assumptions.
   - Add/update shared helper if needed, but keep changes small.

### Expected Changes
- Tighten same-book exclusion for `AiReference` in `TranslationMemoryService`:
  - Exclude rows where `row.RelPath == current.RelPath` for reference suggestions (not only exact text equality).
- Rebalance score computation so widened context helps recall but cannot dominate unrelated matches.
- Add highlight fallback behavior:
  - If phrase-based overlap is empty, show a deterministic "best overlap" marker or suppress card-level confidence.
- Ensure Search and TM matching use the same punctuation/line-break policy.

### Deliverables
- Code patch with minimal touched surface.
- Repro notes with before/after examples.
- Quick manual validation checklist for both tabs.

### Tradeoff
- Fast and low risk.
- Leaves some technical debt in duplicated normalization/highlight logic.

---

## Workflow Option B: Shared Normalization Core (Recommended)
Best balance: fixes current bugs and prevents divergence between Translation tab and Search tab.

### Team
1. Agent B1 - Domain Rule Spec
   - Define one canonical "CJK match normalization" spec:
     - ignore whitespace/line breaks
     - ignore editorial punctuation
     - preserve mapping back to raw text for highlight
2. Agent B2 - Core Utility Implementer
   - Create shared utility for normalization + index map (normalized index -> raw index).
   - Migrate TM matching to this utility.
3. Agent B3 - UI Highlight Integrator
   - Update Translation Assistant highlighting to use normalized overlap mapped back to raw positions.
   - Ensure both Approved TM and Reference/AI cards highlight deterministically.
4. Agent B4 - Search Integrator + Regression Guard
   - Align Search compact-matching path with the same utility/spec.
   - Add targeted tests or deterministic assertions for:
     - cross-line phrase match
     - punctuation-insensitive match
     - same-book exclusion

### Expected Changes
- Add a shared normalization/mapping helper in `Services` or `Infrastructure`.
- Remove policy drift between `TranslationMemoryService` and `SearchIndexService`.
- Replace ad-hoc phrase overlap highlight with normalized-overlap-to-raw mapping.
- Explicit same-book exclusion rule for AI reference rows.

### Deliverables
- Unified matching policy in code and comments.
- Validation artifacts covering Translation tab and Search tab.
- Short migration notes for future contributors.

### Tradeoff
- Slightly more code movement.
- Much better long-term consistency and fewer regressions.

---

## Workflow Option C: Data-Pipeline Reset + Ranker Tuning
Best when baseline dataset quality is suspected as a primary issue.

### Team
1. Agent C1 - Reference TM Dataset Auditor
   - Audit generated `translation-memory.reference.jsonl`.
   - Quantify same-book contamination, duplicate patterns, and noisy 2-line pairs.
2. Agent C2 - Builder Rule Rewriter
   - Adjust `TranslationAssistantBuildService` row-generation rules and dedupe keys.
   - Introduce metadata needed for safer retrieval filtering.
3. Agent C3 - Retrieval/Ranker Tuner
   - Tune score thresholds and weighting for single-line vs context matches.
   - Add penalties for weak shared phrase evidence.
4. Agent C4 - Highlight/UI Reconciler
   - Ensure every shown hit has explainable highlight.
   - Hide or de-prioritize hits that cannot produce meaningful overlap.

### Expected Changes
- Better upstream reference TM rows.
- Stronger retrieval ranking.
- Better explainability in UI highlights.

### Tradeoff
- Highest effort and scope.
- Most robust if data quality is root cause.

---

## Recommendation
Pick **Option B**.

Reason:
- Your issue is a policy mismatch problem (retrieval says "match", UI cannot explain it).
- You need one solution across Translation and Search tabs.
- Option B fixes the immediate bug and enforces shared behavior going forward.

---

## Acceptance Criteria (All Options)
1. Same-book exclusion:
   - For current file context, AI baseline must not suggest rows from the same `RelPath`.
2. Punctuation/line-break policy:
   - Matching is insensitive to editorial punctuation and line breaks.
3. Cross-line recall:
   - A phrase split across neighboring lines can still match.
4. Highlight explainability:
   - Every displayed TM/AI hit has visible corresponding highlight.
5. Cross-tab consistency:
   - Translation tab and Search tab behave with the same matching semantics.
6. Repro case sanity:
   - The reported `<223>` example no longer surfaces irrelevant same-book result.

---

## Suggested Execution Order
1. Reproduce and capture current bad behavior.
2. Implement same-book exclusion hard rule.
3. Introduce/align shared normalization policy.
4. Fix highlight mapping to normalized overlap.
5. Validate in Translation tab.
6. Validate in Search tab.
7. Document final rules in code comments and `CLAUDE.md` task log for the run.
