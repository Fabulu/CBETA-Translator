# Frozen historical reference

The pre-rebuild `terms/`, `maintenance/`, plans, prompts, and reports remain in
place and are not production output for the fresh build. Production workers may
consult them only after independent corpus research, as leads to falsify or
improve. They must never edit them. All new work lives under `fresh-build/`.

The full accumulated queue source files are copied and checksummed under
`fresh-build/queue-sources/`. This non-destructive layout is the primary backup:
the rebuild does not replace, delete, rename, or merge over historical entries.

