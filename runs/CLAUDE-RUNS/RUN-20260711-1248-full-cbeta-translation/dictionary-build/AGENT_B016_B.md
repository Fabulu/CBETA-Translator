# b016 Batch B report

Completed the five assigned entries:

1. `t_9bdac4a01636` — 理事 — **principle and affairs**
2. `t_4c9320095ba1` — 見聞覺知 — **seeing, hearing, sensing, and knowing**
3. `t_734eadab549a` — 本心 — **original mind**
4. `t_5ce4bbfe682f` — 本性 — **original nature**
5. `t_462d9613abe9` — 鳥道 — **bird path**

## Research and editorial result

- Each entry was built from a full corpus inventory, required formula searches, collocation/depth searches, and selection of direct definitional or corrective witnesses.
- Each entry has one corpus-wide sense with a null sense key. None of the meanings depends on a single master, even where a speaker supplied an especially useful definition.
- The entries retain twenty-five curated occurrences in total: five per entry. Every saved KWIC contains its headword.
- The English describes what the records say and translates every quoted phrase in prose. No imported meditation, mindfulness, present-moment, dualism, doctrinal, Japanese, practice/method, paradox, or afterlife frame is used.
- Special translation controls were observed: the four functions in 見聞覺知 remain ordinary verbal nouns; 三昧 is translated “complete command”; 行 is rendered contextually as “conduct” or “carried out”; 鳥道 is not recast as a prescribed procedure or an abstract symbol.
- WORK.md in each term directory records counts, formula/collocation harvests, sense decisions, retained depth, and verification status so the batch can be reconstructed.

## Verification

- JSON parsing: **pass**, five of five.
- `zc.verify(RelPath, Kwic)`: **pass**, twenty-five of twenty-five.
- Saved `FromLb` / `ToLb` equal the verifier's returned bounds: **pass**, twenty-five of twenty-five.
- Headword present in every saved KWIC: **pass**, twenty-five of twenty-five.
- Each sense's `SourceTexts` set equals its occurrence `RelPath` set: **pass**, five of five.
- Targeted conformance scan over every prose field using the current banned-framing patterns: **0 flags**.
- Targeted English-prose scan for bare CJK outside schema-exempt fields: **0 flags**.

No manifest, status, plan, guide, termbase, corpus, or merge file was changed.
