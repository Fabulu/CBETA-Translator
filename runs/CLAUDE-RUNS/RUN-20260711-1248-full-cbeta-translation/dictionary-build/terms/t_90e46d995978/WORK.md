# WORK — 參請

Built from scratch with the b018 guide and `zc.py`.

## Corpus harvest

- Headword: 243 hits in 121 allowlisted texts.
- `隨眾參請`: 23 hits in 13 texts.
- `入室參請`: 2/2; `遊方參請`: 6 hits in 2 texts; `參請因緣`: 2 hits in 1 text.
- Negative and syntactic controls: `不參請` 8/8; `參請外` 11/11.
- Institutional evidence harvested from monastic regulations: leaving one's teacher, seeking a respected elder, asking to lodge, reporting to guest office, then hall office.
- Encounter evidence harvested: joining the assembly, no longer inquiring, inquiry apart from scripture reading, bending the knees before a teacher's address, morning/evening visits with responses, and the contrast with entering the room.

## Editorial decision

One corpus-wide verb/verbal-noun sense, “visit and inquire.” Institutional travel and a direct teacher encounter are linked deployments in the Chinese records, not separate imported systems. The entry never calls the term a practice, method, technique, or meditation.

## Verification

Seven curated KWICs across four principal source texts were passed to `zc.verify`; all return `ok == True`. `SourceTexts` also records the two supporting regulation/heading witnesses used for semantic harvest.

## 2026-07-14 semantic hard-pass ledger

- feedback-inference-verdict: PASS — the opening now defines the term as an in-person visit to a teacher or assembly for questions and requested instruction, not merely a two-verb calque.
- feedback-observations: Zhicheng travels and joins an assembly; Shuangfeng Gu abstains from formal inquiry; Fayan Wenyi treats it as an ordinary activity; the Baizhang regulations specify departure, travel, lodging, and registration; Huanyuan Fuyu bends the knee; Tianning's followers come morning and evening; Shending Yunwai Ze permits entry into his room; Ruibai Mingxue's record heads a seven-teacher itinerary as circumstances of visiting and inquiry.
- feedback-falsification-searches: Rechecked 參請 243/121, 隨眾參請 23/13, 入室參請 2/2, 遊方參請 6/2, 參請因緣 2/1, 不參請 8/8, 參請外 11/11, 晨夕參請 5/5, and 屈膝參請 1/1.
- feedback-counterexamples: Travel, residence in an assembly, registration, direct questioning, entry into a room, and a visit-history heading are linked stages or frames of one visit-for-inquiry event. Noun and verb grammar do not make separate referents.
- feedback-scope: One corpus-wide institutional and encounter action: visiting or attending in order to inquire and request instruction.
- lookup-probes: `visit and inquire`, `seek instruction in person`, `visit a teacher for questions`, `attend and inquire`, `formal inquiry visit`, `join the assembly and inquire`.
- opening-interpretation-verdict: PASS — the reader is told who is visited and why before the institutional and biographical examples begin.
- definition-and-sense-verdict: KEEP one sense. Travel, assembly attendance, room entry, and face-to-face exchange are components and deployments of the same event, not different things.
- sense-target-distinguishability: PASS — one visit-for-inquiry sense; no paraphrase split or grammar-only split exists.
- family-verdict: Join-assembly, enter-room, travel, circumstances, non-inquiry, inquiry-apart-from, morning/evening, and bent-knee families were cross-checked and retained.
- provenance-verdict: Added exact witnesses for both formerly dangling Chinese families rather than deleting them. All nine stored KWICs are source-verifiable; notes now name the translated source and accountable speaker, actor, reviewed unnamed group, or impersonal procedural scene.
- propagation-verdict: Added five natural retrieval probes, rewrote the opening around the observable visit-for-questions event, and preserved the institutional/public-interview range.
- final-gate: `semantic-r002-owner1-canqing-gate.json` hardPass=true; 9/9 exact KWICs verified, including the two newly anchored families, and zero exact or attribution failures; entry SHA-256 `1aed1a94c28de73ef47f135d51fe7663c111faad94e410264944ecf32b8509b9`.
