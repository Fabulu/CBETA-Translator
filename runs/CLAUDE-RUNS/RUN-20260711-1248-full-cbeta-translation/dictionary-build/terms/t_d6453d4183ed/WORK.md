# 面門 — the face

## Concordance and sense decision

- Allowlist count: **939 hits in 264 texts**.
- One corpus-wide sense: the face or face-front, literally ‘face’ (面) + ‘gate/opening’ (門). Physical actions, impact collocations, the Linji case, and the compact phrase ‘a dynamic at the face’ (面門一機) retain the same spatial referent; no second sense was created.

## Depth inventory

- Definition formulas searched: ‘face + one who’ (面門者), ‘the so-called face’ (所謂面門), ‘called the face’ (謂之面門), ‘named the face’ (名為面門), ‘called the face’ (喚作面門), ‘what is meant by the face?’ (何謂面門), and ‘what is the face?’ (如何是面門).
- The single apparent ‘face + one who’ hit is the agent nominalizer in ‘how many people have split open the face?’ (󳮢破面門者幾人), not a definition. The single ‘so-called face’ hit enumerates the face, nostrils, ears, eyes, heels, and brains (所謂面門、鼻孔、耳朵、眼睛、腳跟、頭腦); it is included in the entry note as an enumeration, not promoted to a gloss. No true self-definition was found.
- Distinct deployments included: Baozhi's physical action; scorching and pressing through the face; Linji's ‘entering and leaving through the faces’ case; Yuanwu's ‘dynamic at the face.’
- Collocations: ‘entering and leaving through the face’ (面門出入), 223 hits / 141 texts; ‘press through the face’ (拶破面門), 38 / 30; ‘scorch the face’ (燎却面門), 53 / 34; ‘touch the face’ (觸著面門), 1 / 1.
- Period/genre spread represented by an early named biography and Tang case in the Wudeng huiyuan, Xuedou's direct section, and Yuanwu's own discourse record.
- Omission audit: the late statement ‘eyes at the face’ (眼在面門), 2 hits / 2 texts, repeats the physical location and adds no distinct deployment beyond the selected bodily witnesses. The ‘so-called’ enumeration is recorded in the note. No high-value finding remains unaccounted for.

## Verification

- Five curated KWICs selected; **5/5 returned `zc.verify(...).ok == True`**.
- Governing heads read for all five. The Linji passage is a raised case under Bensong and is therefore unkeyed; Xuedou and Yuanwu passages occur under their own governing sections.
# 2026-07-14 fresh-build provenance and quote gate

- Corrected the central attribution: the no-rank true-person line is Linji Yixuan's quoted utterance, not Vinaya Master Bensong's. Bensong remains the later raiser and section subject.
- Added Chaozong Tongren's body-part enumeration and Wuyi Yuanlai's actor-marking question as exact headword occurrences, anchoring every previously dangling Chinese string without treating either as a definition.
- Current depth is seven exact occurrences across five source files for 1,040 hits / 287 files / 283 independent works. Every row passes `zc.verify` at exact bounds.
- Added source-exact titles and closed-role `utterer` contexts throughout; `audit_attribution.py --json` reports zero hard failures and all nineteen Chinese prose strings are anchored.
- Definition/sense retest: bodily splitting, scorching, pressing, entering/leaving, and the face-dynamic collocation preserve one face/face-front referent. KEEP one sense.
- Forbidden-prose scan includes `Bodhiteaching`, `Buddhism`, and `meditation`; no hit remains.
