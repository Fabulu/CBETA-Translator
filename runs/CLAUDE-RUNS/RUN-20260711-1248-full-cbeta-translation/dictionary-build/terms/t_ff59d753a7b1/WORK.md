# Roam at ease (逍遙) — enrichment ledger

- Allowlisted concordance: 974 hits in 245 texts; frequency floor 7, final 10 anchors across 8 texts.
- Definition harvest: retained Yongjue's “what is called” definition and warning, Tianhuang's instruction, going/staying narrative, staff question, community-rule rebuke, and direct person-question.
- Deployment inventory: lexical verb and compounds; Master Xiaoyao heading; independent succession/location notice; Mount Xiaoyao headings in two compilations.
- Item-8 retest: **revised from one sense to three.** Roaming at ease is an action/state; Master Xiaoyao is a person/title; Mount Xiaoyao is a place. Master Xiaoyao succeeds Jiashan; Huaizhong is the master attached to the mountain heading, so they are not merged.
- Roster check: Jiashan Shanhui is canonical. Master Xiaoyao and Huaizhong of Mount Xiaoyao are absent from `Assets/Data/master-dates.json`; their occurrences remain unlinked instead of receiving invented canonical names.
- Family comparison: 任性逍遙, 逍遙自在, 放曠逍遙, and 隨緣放曠 remain lexical. 逍遙山谷 means roaming through mountains and valleys, not the proper name Mount Xiaoyao.
- #0g decision: the lexical sense retains the corpus's direct definition and correction against reckless indulgence. The figure sense records Master Xiaoyao's lineage and whisk encounter; the place sense records the master-heading function without manufacturing symbolic meaning.
- Omission audit: all six earlier lexical deployments and both independent anchors for each named referent are retained.
- Verification: all 10 exact KWICs pass `zc.verify`; stored line bounds match.

## Item-8 retained-sense ledger

- `sense-target-distinguishability: KEEP` — **to roam at ease** denotes an action or state; **Master Xiaoyao** denotes a person in a lineage and encounter; **Mount Xiaoyao** denotes a place used in a master's heading. Action, person, and place are three exact referent classes, not readings or grammatical variants.
- Depth/source retest: 974 allowlisted hits in 245 files; 10 curated anchors across 8 source texts (6 action, 2 person, 2 place), exceeding the floor with distinct definitional, narrative, encounter, lineage, and heading evidence rather than repetition.
- Family/definition retest: `任性逍遙`, `逍遙自在`, `放曠逍遙`, and `隨緣放曠` remain lexical; `逍遙山谷` is ordinary roaming through terrain, not Mount Xiaoyao. The succession notice distinguishes Master Xiaoyao from Huaizhong attached to the mountain.
- #0g retest: the lexical sense retains the corpus's definition and warning against reckless indulgence; the person sense retains lineage and whisk deployment; the place sense records the heading function without inventing symbolism.

## Highest-risk exact-actor, quotation, sense, and public-reader repair (2026-07-13)

### Baseline and research method

- Read all ten prepared workbook units and reconstructed the four over-broad `head-section` extractions from the exact XML divisions. Read the current guide in full before changing the entry.
- Baseline: 10/10 stored anchors were exact, but all ten occurrences had unresolved actor state, all ten attribution notes omitted exact actor and source, seven sense-local Chinese evidence strings were dangling, and one explanation used a vague attributor.
- Used `indexed_kwic.py` for allowlist-scoped formula, family, place, name, and title discovery, then verified every retained occurrence and line bound against source XML with `zc.verify`. The optional website JavaScript index could not run because `node` is unavailable in this shell; no index-only material was saved.
- Inherited research was retained as a lead: the earlier three-way action/person/place split, Yongjue definition, Tianhuang instruction, Danxia narrative, Tianyin staff use, Shiyu correction, Lumen interview, Xiaoyao succession, and Huaizhong place headings were all rechecked rather than discarded.

### Definition, deployment, and family inventory

- Concordance: 974 exact hits in 245 allowlisted texts; frequency floor 7.
- Definition-formula inventory: `逍遙者` 3/3; `所謂逍遙` 1/1; `謂之逍遙` 0; `名為逍遙` 0; `喚作逍遙` 0; `何謂逍遙` 0; `如何是逍遙` 7/7.
- Direct definition: Yongjue Yuanxian says what is called roaming at ease and ranging freely is remaining aloof and unattached amid every false scene; his bird-in-sky and dragon-from-sea comparisons explicitly predicate no tether and no obstruction. The expanded exact anchor now preserves the complete definition, both ordinary images, the governing physical constraints, and his sound/sight/wealth/power countertest.
- Distinct lexical deployments retained: direct definition and contrast; Tianhuang Daowu's answer to Longtan Chongxin; Danxia Tianran's going/staying biography; Tianyin Yuanxiu's staff question; Shiyu Mingfang's community-rule rebuke; an unnamed monk's public-interview question with Lumen Fadeng's answer; and Yongjia Xuanjue's ordinary motion-through-terrain syntax.
- Family counts: roaming at ease and unbound 53/40; roam at ease according to one's nature 66/45; range freely and roam at ease 9/8; roaming at ease outside things 24/22; roam through mountains and valleys 2/2; Free and Easy Wandering 6/3; Xiaoyao Heshang 17/9; Mount Xiaoyao 18/9.
- Longer-title adjudication: `逍遙遊` is the longer title *Free and Easy Wandering*. Juelang Daosheng's exact essay heading is stored as `EvidenceRole: family`; the added graph `遊` creates the title, so it does not create a fourth bare-headword sense or buy bare-headword depth.
- Nested-family adjudication: `逍遙山谷` is verb plus terrain object, not Mount Xiaoyao. Its exact Yongjia witness is stored under the lexical sense and is now the seventh independent lexical anchor.

### Sense, target, and gloss-hygiene verdicts

- `sense-target-distinguishability: KEEP` — pair 1/2: **roam at ease** names an action/state; **Master Xiaoyao** names Xiaoyao Heshang, a lineage person. Pair 1/3: **roam at ease** names an action/state; **Mount Xiaoyao** names a place. Pair 2/3: **Master Xiaoyao** and **Mount Xiaoyao** explicitly label person versus place and cannot be confused from their targets alone.
- Different favorable, critical, instructional, narrative, and interview uses of roaming at ease remain one lexical sense because they concern the same action/state. Evaluation does not make polysemy.
- Person verdict: keep. The Jingde unit names Xiaoyao Heshang and records his own seated encounter; the Patriarchs' Hall lineage notice independently names him as Jiashan Shanhui's successor at Gao'an. The title contains no further personal name, so `Xiaoyao Heshang` is preserved as the exact source-attested name pending roster expansion.
- Place verdict: keep. Two independent compilations head Huaizhong's entry with Mount Xiaoyao in Jiangxi and place him under Jiashan Shanhui. `Xiaoyao Huaizhong` is not Xiaoyao Heshang.
- Title verdict: reject as fourth sense; retain as explicitly marked longer-family evidence.
- Zen-loaded verdict: the strongest corpus bend is not a new referent but a tested deployment. The record can praise free movement, place the phrase on a teaching-seat staff, and reject indulgence or crazed delusion passed off as freedom. These tensions are reported as predicates and contrasts, not reduced to an imported doctrine.
- Opening revisions: all three senses now open with the referent and corpus-earned distinction. None begins with graph composition, frequency, or a quotation dump.

### Exact-actor ladder and quotation repair

- Yongjue Yuanxian, Tianhuang Daowu, Danxia Tianran, Tianyin Yuanxiu, Shiyu Mingfang, Yongjia Xuanjue, Juelang Daosheng, and Xiaoyao Heshang are exact named speakers/actors after complete-unit review.
- Lumen interview: the headword is in the unnamed monk's question, not Lumen Fadeng's answer. All six rungs, including three allowlisted parallel recensions, leave the non-master questioner unnamed; the row uses the reviewed-unnamed branch and lists Lumen Fadeng only as respondent.
- The Patriarchs' Hall succession notice and both Mount Xiaoyao headings are impersonal editorial/narrative source statements, not master speech. Each stores concrete grammar evidence and separately names the masters in context.
- Expanded Yongjue's anchor instead of deleting the useful bird, dragon, tether, obstruction, and free-ranging evidence. Added Yongjia's exact motion-through-terrain anchor. Result: all 24 Chinese evidence strings in reader prose are anchored; none was deleted to make the gate pass.

### Public-feedback inference ledger

- feedback-inference-verdict: licensed
- feedback-observations: Yongjue directly equates the phrase with remaining unattached amid encountered scenes and predicates no tether/no obstruction through bird and dragon images; Danxia is described as free in going and staying; Tianhuang gives the stock formula; Yongjue and Shiyu explicitly reject indulgence and crazed delusion as substitutes.
- feedback-falsification-searches: all standard definition formulas; favorable and hostile predicates; direct questions; motion through terrain; Xiaoyao Heshang; Mount Xiaoyao; Free and Easy Wandering; and family phrases for nature, freedom, ranging, and outside-things were searched separately.
- feedback-counterexamples: Lumen Fadeng's answer is hostile to a neat idealized picture; Yongjue rejects being tied by sound, sight, wealth, or power; Shiyu rejects crazed delusion. These narrow the opening to free movement without tether/obstruction and prevent approval from becoming part of the definition.
- feedback-scope: corpus-wide lexical sense, with person, place, and longer-title families explicitly separated.
- lookup-probes: roam freely; wander at ease; wander freely; range freely; free roaming; Master Xiaoyao; Xiaoyao Heshang; Mount Xiaoyao; Xiaoyao Mountain; Xiaoyao Shan
- opening-interpretation-verdict: licensed
- observation: occurrence s1/o1 directly defines and physically constrains the lexical image; s1/o2 and s1/o3 establish nature/conditions and going/staying frames; s1/o5 and s1/o6 establish corrective and public-interview tension; s2 and s3 exact headings establish person/place referents.
- minimal-inference: the lexical sense can be described as going or staying freely without being tied or obstructed by what one meets; the record both uses and tests that wording. The two proper-name senses can be named as person and place because the headings and lineage relations say so.
- ordinary-bridge: a bird in open sky leaves no tether, a dragon leaving the sea meets no stated obstruction, and `going or staying` plus motion through mountains and valleys supplies ordinary movement rather than a static inner condition.
- falsification-searches: literal motion, favorable and adverse evaluation, definition/counterdefinition, direct interviews, title/name/place strings, and longer nested compounds.
- counterexamples: the Lumen answer, Yongjue's sound/sight/wealth/power tethering, Shiyu's rule, and the longer `Free and Easy Wandering` title all block a broader or vaguer gloss.
- scope: corpus-wide for the lexical sense; title/person-specific for Xiaoyao Heshang; place-specific for Mount Xiaoyao; family-only for Free and Easy Wandering.
- verdict: licensed; three referents are retained, while appraisal differences and the longer title do not create additional senses.

### Search and final depth decisions

- Lexical aliases: roam freely; wander at ease; wander freely; range freely; free roaming.
- Person aliases: Master Xiaoyao; Xiaoyao; Xiaoyao Heshang; Xiaoyao master.
- Place aliases: Mount Xiaoyao; Xiaoyao Mountain; Xiaoyao Shan; Xiaoyao mount.
- Final stored depth: 12 occurrences across ten source texts; seven exact lexical witnesses after the longer-title family exclusion, two person witnesses, two place witnesses, and one title-family control. Every sense has exact-headword evidence and the lexical sense meets its frequency-scaled floor without family inflation.
- No merge, commit, or push was performed.
