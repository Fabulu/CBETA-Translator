# Prompt for Next AI (Translation Assistant + Search Shared Fix)

Use this prompt as-is with the next AI.

```text
You are working in C:\programmieren\MergeWorkCbeta\CBETA-Translator.

Read first:
1) docs/coding_agents/TRANSLATION_ASSISTANT_MULTI_AGENT_WORKFLOWS.md
2) CLAUDE.md

Use Workflow Option B ("Shared Normalization Core").

Problem to solve:
- Translation Assistant "Reference / AI baseline" shows noisy hits and often no highlight.
- Matching should ignore editorial punctuation (e.g. 。 ，) and line breaks.
- A sentence may span neighboring lines, so cross-line matching is required.
- Same-book/self matches should be excluded for AI baseline suggestions.
- Translation tab and Search tab must share the same matching semantics.

Known repro symptom:
- Current block around:
  ZH: 有來，一宿一食急走過，且趁軟煖處去也。」
  EN: one does come, after one night and one meal he hurries off, chasing after some soft warm place.
- Wrong AI baseline hit appears from the same book (should not appear).
- UI often shows score but no highlighted overlap.

Mandatory constraints:
1. Implement one shared normalization policy for TM and Search behavior.
2. Keep edits minimal and focused; no unrelated refactors.
3. Preserve existing architecture patterns and naming style.
4. Fix both retrieval quality and highlight explainability.
5. Do not rely on Claude hooks; work from code directly.

Target files (likely):
- Services/TranslationMemoryService.cs
- Services/SearchIndexService.cs
- Views/TranslationTabView.axaml.cs
- (optional shared helper) Services/* or Infrastructure/*

Expected implementation outcomes:
1) Same-book exclusion for reference matches:
   - Exclude rows where row.RelPath == current context RelPath for AI baseline suggestions.
2) Shared normalization helper/spec:
   - Ignore whitespace, line breaks, and editorial CJK punctuation for matching.
   - Provide mapping back to raw text indexes for highlight.
3) Highlight reliability:
   - If a hit is shown, a visible overlap highlight must exist.
   - Prefer normalized-overlap mapping back to raw ZH positions.
4) Cross-line support without overmatching:
   - Keep prev/current/next context support, but prevent context-only noise from outranking unrelated rows.
5) Search alignment:
   - Ensure Search compact matching follows same normalization policy.

Validation required before final response:
- Re-run the repro and show before/after behavior summary.
- Confirm no irrelevant same-book AI baseline hit for the repro case.
- Confirm at least one visible, correct highlight for valid AI/reference hits.
- Confirm Search tab behavior matches the same normalization assumptions.

Deliverables:
1. Code changes.
2. Short explanation of root cause(s).
3. File-by-file change summary.
4. Residual risks and next test suggestions.
```
