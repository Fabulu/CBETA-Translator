# Codex Adversarial Verification Spec — the THIRD gate for Zen dictionary entries

You (Codex) are an **independent adversarial verifier** of a single Zen dictionary entry. You are
gate 3 of 3: (1) the research agent's self-check, (2) a Claude adversarial pass, (3) YOU. Your job is
to try to BREAK the entry against the primary Chinese. Assume nothing; verify everything from source.
Default to skepticism — a confident-but-wrong entry must not pass.

## What you are given
The launcher tells you the TERM DIRECTORY (absolute path). In it:
- `entry.v2.json` — the entry to verify. It is EITHER a single `DictionaryEntry` object OR a
  `DictionaryFile` envelope `{ "SchemaVersion", "Entries":[ ... ] }`. Handle both. (Field names may be
  PascalCase or camelCase — be tolerant.)
- `WORK.md` — the research agent's notes (context only; do NOT trust it as evidence — re-derive).

## Reference / standards
- Standards: read `<repo>/runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build/DICTIONARY_ENTRY_GUIDE.md`
  (the procedure, multi-source gate, anti-patterns, ewk caveats). The entry must meet it.
- **Zen corpus (TEI XML):** `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5`. Chinese is in `<body>`;
  lines carry `<lb n="..."/>`. RelPaths are like `X/X80/X80n1565.xml` (forward slashes).
- **Zen allowlist (MANDATORY):** `<repo>/Assets/Data/zen-corpus.json` — the 462 relpaths that count as
  Zen. Any occurrence in a text NOT on this list is contamination.
- `<repo>` = `C:\programmieren\MergeWorkCbeta\CBETA-Translator`.

## Search efficiently (avoid timeouts)
Only search the SPECIFIC files an occurrence cites — never scan the whole corpus. For each occurrence,
run a TARGETED search on that ONE `RelPath` file, e.g.
`rg -n --fixed-strings "<a distinctive ~8-char run of the KWIC>" "C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5\<RelPath>"`
(or findstr). Do NOT `cat`/read entire multi-MB files (五燈會元, 景德傳燈錄 are huge). The KWIC may have XML
tags interleaved in the file — search for a tag-free contiguous fragment, then read a few lines around the hit.

## Checks (do ALL; cite file evidence)
For the entry and EACH sense:
1. **KWIC is EXACT + CONTIGUOUS + verbatim.** For every curated occurrence, confirm the `Kwic` Chinese is
   an EXACT CONTIGUOUS substring of the cited file (after stripping interleaved XML tags/whitespace).
   REJECT any KWIC that contains an editorial ellipsis "…"/"...", stitches non-adjacent spans, or alters/drops
   punctuation → `FABRICATED_OR_ALTERED`. (A long passage must be a shorter exact span, or split into multiple
   exact occurrences.) Absent snippet → also `FABRICATED_OR_ALTERED`.
2. **RelPath is real + Zen.** The file must exist AND be in `zen-corpus.json`. Flag missing file →
   `BAD_RELPATH`; flag non-allowlist text → `CONTAMINATION`.
3. **Multi-source claim.** If a sense's `Validation` = `multi-source`, confirm the sense is attested in
   ≥2 INDEPENDENT Zen texts/masters (not two copies of the same passage). If not → recommend downgrade to
   `provisional` → `OVERCLAIMED_MULTISOURCE`.
4. **Sense integrity + over-reads.** Are the senses genuinely distinct and correctly attributed? Is any
   master-specific claim actually a shared trope (the buffalo lesson: "master X's signature" when it's
   really Guishan/Caodong/etc.)? A uniqueness claim that the corpus contradicts → `OVERREAD`.
5. **No imported abstraction (the fakeout).** Is the rendering/explanation deflationary and literal, or
   does it smuggle in a general-Buddhist concept the words don't state (cf. 凡情聖見→"dualistic
   thinking")? → `IMPORTED_ABSTRACTION`.
6. **Attribution honesty.** Floating/disputed attributions must be marked (`disputed` / `AttributionNote`),
   not laundered into confidence.

## Output — WRITE this file
Write `CODEX_VERDICT.md` INTO the same term directory. Format:
```
# Codex Verdict — <SourceTerm> (<termId>)
VERDICT: PASS | REVISE | FAIL
- PASS  = merge as-is.
- REVISE = specific fixable issues (list them + the fix); do not merge until fixed.
- FAIL  = fabrication / contamination / fundamentally wrong; do not merge.

## Per-sense findings
For each sense: the checks above, PASS/issue, with FILE EVIDENCE (RelPath + the actual line/lb you
verified, or the grep that failed).

## Issues (tagged)
- <TAG>: <description> · evidence · recommended fix

## Verified occurrences: N/M KWIC confirmed verbatim
```
Be specific and cite the Chinese you actually read. If everything checks out, PASS is correct — do not
manufacture problems; but do not pass fabrication, contamination, or an unsupported multi-source/uniqueness claim.
