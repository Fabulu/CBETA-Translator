# Reusable wave-agent prompt

Replace `{WAVE}`, `{BATCH}`, and the assigned ID/term list before dispatch.

Read `DICTIONARY_ENTRY_GUIDE.md`, `CODEX_HANDOFF.md`, `CODEX_RESUME.md`, and `{WAVE}_ASSIGNMENTS.md` in full before acting. Build only `{WAVE}` Batch `{BATCH}` and touch only the assigned term directories' `entry.v2.json` and `WORK.md` files plus `AGENT_{WAVE}_{BATCH}.md`. Do not touch `STATUS`, `MANIFEST.jsonl`, `STATUS.md`, `WAVE_PLAN.md`, the guide, termbase files, other term directories, or translated XML.

Use `zc.py` count/find/title/head/verify throughout. Treat planning glosses only as search leads. Build each entry from the Chinese Chan corpus itself. Harvest every relevant direct self-definition, contrast, equivalence, variant, morphology, question-answer definition, corrective witness, and independently useful deployment shape; do not submit thin entries. Preserve exact contiguous KWIC text and determine the governing speaker from the nearest applicable head and section. Every saved occurrence must return `zc.verify(...).ok == True` with matching `FromLb`/`ToLb`; set `PYTHONIOENCODING=utf-8`.

For speed, batch searches and final verification in one Python process whenever possible so the `zc.py` corpus cache is reused; avoid launching one slow shell per occurrence.

Write literal English-first dictionary prose. Translate every Chinese phrase used in prose and put its Chinese only parenthetically beside the English. Bare Chinese is permitted only in `Kwic`. Chinese Chan only: no imported Buddhist-doctrine, meditation/mindfulness, present-moment, dualism, practice/method/technique, huatou/koan/zazen/Mu, or Japanese/Korean framing. Public cases are records of real people, not paradoxes, riddles, parables, or devices. Translate 話頭 occurrences as the actual word, saying, remark, question, or exchange. Translate 無 as “no,” 禪 as “Chan,” 參禪 as “investigate Chan,” 禪床 as “Chan seat,” and 坐禪 from its Chinese contexts as “sitting Chan.” SenseKey is null unless the meaning itself is genuinely master-specific; historical origin alone is not enough.

Validate JSON and schema, exact roster links, source-text attestation, strict English-first prose, banned framing, and all occurrences. The batch report must give per-term corpus counts, senses, occurrence totals, sources, harvested self-definitions/contrasts/variants, verification totals, and any unresolved ambiguity. Do not merge and do not mark status.
