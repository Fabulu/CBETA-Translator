# Next-500 QA resolution

The 27 lane-R queue defects reported in `NEXT500_QA_A.md` were applied one-for-one through
`compile_next500_plan.py`, then `NEXT500_TERMS.md` and `NEXT500_BUILD_PLAN.md` were regenerated.

- Removed speech-marker/object/location expansions already owned by completed entries (`示眾云`, `示眾曰`,
  `良久云`, `便棒`, `僧堂前`, `如何是和尚家風`, `顧視大眾`, and comparable forms).
- Removed orthographic, word-order, and case-label duplicates (`拈花微笑`, `金剛王寶劒`, `君臣五位`,
  `萬象森羅`, `大地山河`, `趙州喫茶`, and comparable forms).
- Removed compounds already explicitly assigned to an existing family article (`百丈清規`, `二時粥飯`,
  `涅槃妙心`, `拈花示眾`, and comparable forms).
- Added the 27 independently counted replacements named in the QA report, including `未審`, `商量`, `道得`,
  `因果`, `業識`, `堂奧`, `參問`, `祖印`, `正位`, `啐啄`, `宗匠`, and `道中人`.

Post-resolution mechanical validation:

- 500 rows, 500 unique headwords, 500 unique deterministic SHA IDs;
- lane distribution remains A=180, B=180, R=140;
- build plan contains the exact same 500 tuples;
- none of the 27 rejected terms remains selected;
- no collision with completed/draft entries, requested rows, or the normalized 100-sayings queue;
- the related-pool dispositions and 720-row investigation backlog were regenerated after the replacement.

The retained lane-R build-time controls in `NEXT500_QA_A.md` remain mandatory.
