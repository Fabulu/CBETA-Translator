# Dictionary worker instructions

- The active dictionary root is
  `runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build`.
- Do not run repository-wide `find`, `rg --files`, `git status`, or searches for instruction files.
  Worker prompts provide exact paths. If a named file is relative, resolve it against the active
  dictionary root above.
- Mandatory dictionary instructions are `DICTIONARY_ENTRY_GUIDE.md`, `ATTRIBUTION_FIX.md`,
  `ACTOR_AUDIT.md`, and, for Iriya construction, `IRIYA_CONSTRUCTION_SPEED_MODE.md`.
- The lineage roster is read-only at `Assets/Data/lineage-masters.json`. Dictionary workers must never
  modify it.
- Fresh construction output belongs only under `dictionary-build/fresh-build/entries/<id>/` until a
  root-reviewed atomic installation.
- Use exact-path, bounded commands. The scan guard terminates broad searches after twenty seconds.
