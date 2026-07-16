# 賓主 sense repair report

Repaired `terms/t_6da91f8ce284/entry.v2.json` under depth gate #0f.8.

- Merged the two paraphrastic corpus-wide senses: `guest and host` and `guest and host
  (interchanging)` refer to the same paired roles. Caodong interchange is a distinct deployment
  of those roles, not a different thing.
- Preserved every curated witness and source, the Caodong positional/interchange evidence,
  Dongshan-Caoshan links, related terms, counts, and the caveat that the four `中` position-names
  occur across houses with different glosses.
- Retained Linji Yixuan's genuinely distinct `four guest-and-host relations` as a separate
  master-specific sense. Its `看` ("examine") taxonomy, witnesses, graph-recension history,
  links, and validation remain intact.
- Final structure: 2 senses, 8 occurrences. JSON parses. With `PYTHONIOENCODING=utf-8`, all
  8/8 KWICs pass `zc.verify` and all stored primary-edition bounds match exactly. Six KWICs
  contain the compound `賓主`; two deliberately contain named constituent formations
  (`賓中主` / `主中主` and `賓看主`) used as evidence for the corresponding families.
- Reviewed the revised corpus-wide prose and occurrence notes for describe-only and
  English-first conformance. No status, manifest, wave-plan, guide, or merged termbase file
  was changed.

Follow-up English-first gate: replaced the one bare headword in `Senses[0].Note` with "the
term 'guest and host' (賓主)." The two-sense structure and all evidence remain unchanged;
JSON and all 8 saved occurrence anchors were reverified.
