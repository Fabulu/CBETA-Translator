# Read-only Semantic and Depth Audit — b016–b018

## Scope and checkpoint state

This is the durable **b016 checkpoint** requested before reassignment. The current guide and handoff were reread in full. All fifteen b016 entries and every available b016 `WORK.md` were read. No entry, status, manifest, termbase, wave-plan, or corpus file was changed.

b017 and b018 have **not** been audited in this checkpoint.

Current `zc.count` totals were refreshed read-only:

| Term | Hits | Files |
|---|---:|---:|
| 一隻眼 | 992 | 233 |
| 燈錄 | 789 | 91 |
| 理事 | 850 | 198 |
| 雲水 | 789 | 238 |
| 印可 | 761 | 173 |
| 本心 | 748 | 157 |
| 王老師 | 760 | 193 |
| 見聞覺知 | 787 | 162 |
| 單傳 | 692 | 211 |
| 本性 | 667 | 152 |
| 木佛 | 663 | 181 |
| 鳥道 | 627 | 168 |
| 付法 | 608 | 143 |
| 森羅萬象 | 685 | 213 |
| 體露 | 646 | 222 |

A mechanical prose scan found no literal banned-word or Chinese-outside-parentheses flags in the fifteen entries. The semantic findings below go beyond that shallow scan.

## Priority findings

### HIGH — 印可 (`t_fb23e0284d73`)

The entry imports a generalized theory not established by the curated evidence: an official authenticating seal is said to be “repurposed” as a teacher's stamp certifying a student's “realization as authentic,” necessarily tied to lineage succession. The passages directly show approval, named appraisals, receipt of approval from one or more teachers, and later questioning after approval. They do not establish that every occurrence certifies “realization,” nor that approval itself assigns lineage.

Repair should keep the literal “seal/approve” components, describe the recorded approval events, and foreground the entry's own useful structural caution that approving teacher and lineage teacher can differ. Delete “Zen bend,” “repurposed,” and the global realization-authentication claim unless a direct corpus definition supports each.

### HIGH — 王老師 (`t_2202e37854d4`)

This is explicitly Nanquan Puyuan's self-designation and later label. A null corpus-wide `SenseKey`, null sense `MasterName`, and empty `RelatedMasters` conflict with the schema rule for a genuinely master-specific meaning. Historical origin alone would not justify a key, but here the referent itself is Nanquan.

Repair should confirm the exact roster spelling and use the Nanquan master key/link unless corpus review establishes a separate generic “Teacher Wang” sense. The prose and occurrences otherwise preserve the self-name, selling-himself case, buffalo saying, cat-case wording, Mazu contrast, and later comments well.

### HIGH — 本性 (`t_5ce4bbfe682f`)

The `WORK.md` inventory reports forty occurrences in thirty files of the construction translated literally as “original nature is empty,” but the entry silently omits the entire finding. Rule #0f requires every high-value collocation or direct description to be included, explicitly excluded with a reason, or marked unresolved. Avoiding imported emptiness doctrine is correct; silently discarding the corpus phrase is not.

Repair should inspect representative Chinese Chan contexts, translate the actual predicate in English, and describe who says it and in what deployment. If the matches are syntactically misleading or contaminated by nesting, record that reason explicitly. The retained Platform Record and Mirror Record descriptions are strong.

## Medium findings

### 燈錄 (`t_15eec715e731`)

No `WORK.md` exists. Most curated evidence consists of title or contents metadata rather than passages demonstrating the asserted genre structure. The editorial preface and compiler's rules are stronger, but the statement that the genre arranges masters, successors, sayings, and encounters through a “lamp-succession framework” needs to be tied more explicitly to those contents or replaced by a narrower observable description.

Add the required #0f inventory and retain the valuable title-family evidence: transmission, continued, linked, universal, and broad lamp records.

### 雲水 (`t_2d92f15fa0ab`)

No `WORK.md` exists. The primary institutional sense is well evidenced by monks of the two halls, reception lodgings, and the cloud-and-water hall. The etymological statement that their movement “is compared to clouds and flowing water” is not directly supplied by the curated passages and should be qualified or removed.

The second sense rests on one poem whose compact syntax is explicitly admitted to allow multiple segmentations. That is too weak to establish a separate lexical sense without clearer prose witnesses; either find unambiguous literal “clouds and water” evidence or leave the reading unresolved rather than preserve a speculative sense bucket.

### 理事 (`t_9bdac4a01636`)

The Mirror Record directly states a reciprocal relation between principle and affairs, but the opening generalizes that relation to the entire corpus: affairs are what display principle. Attribute that relation to the Mirror Record rather than making it the headword's global meaning. The target “principle and affairs” is appropriately literal and avoids the older imported “noumenal/phenomenal” pair.

### 一隻眼 (`t_ccae22e8375d`)

The alternate target “one discerning eye” adds a quality the headword does not state and the entry's own examples complicate: one eye may be possessed, lost, exchanged, placed, or opened, and Yunmen's possessing only one eye is an adverse appraisal. Remove “discerning” unless a particular occurrence explicitly warrants it. The accumulated deployment range is otherwise strong.

### 本心 (`t_734eadab549a`)

The opening claim that original mind is one's own “before anything additional is sought or set up” is an annotator inference, not one of the quoted descriptions. Begin with the literal “mind from the root/outset,” then let the retained descriptions do the work: neither joined nor separate; no form, color, root, lodging, arising, or extinction; paired with original nature; and not first requiring stillness.

### 森羅萬象 (`t_4416ef85b3a5`)

No `WORK.md` exists. “The whole array of phenomena” risks importing a familiar religious abstraction where the graphs give “the myriad forms arrayed.” The claims that the phrase gathers “visible or nameable multiplicity” and that the corpus collectively defines it through scenery, people, objects, and “all visible forms” are broader than the cited passages.

Prefer the literal target, retain the excellent Fayan first-moon/second-moon reversal and the medicine/illness, moon, space, and scenery deployments, and add a complete #0f inventory.

### 體露 (`t_94be914de45d`)

No `WORK.md` exists. The entry is admirably cautious about not inventing one referent for `體`, but its preferred target “fully exposed” drops the noun entirely. Consider “body/substance exposed” as the literal head target while preserving context-sensitive alternatives. The direct “called true constancy fully exposed” occurrence and Yunmen autumn-wind case are strong and should remain central.

## Lower-risk / currently strong entries

### 見聞覺知 (`t_4c9320095ba1`)

Strong English-first entry. It translates the four functions rather than importing “awareness,” retains the cause-of-birth-and-death/root-of-release contrast, the no-separate-entity correction, and the not-bound statement. Any repair should preserve all five high-value witnesses.

### 單傳 (`t_643fab6ecc1b`)

Strong depth: two explicit Zhongfeng descriptions, the immediate “what is transmitted?” question, mind-seal and outside-teachings compounds, and verse deployment. It avoids Japanese overlays and does not turn transmission into a technique. A repair should mainly check that each generalized “lineage wording” sentence stays explicitly tied to the cited records.

### 木佛 (`t_338c380e905a`)

Strong describe-only reconstruction of the Danxia case, relic exchange, gold/clay/wood sequence, later eyebrow-and-whisker questions, and divergent recorded answers. It does not convert the wooden image into an abstract symbol.

### 鳥道 (`t_462d9613abe9`)

Strong because it preserves both Dongshan's “travel the bird path” exchange and his “not traveling the bird path” answer, preventing a procedure overlay. Verify during repair whether “one of three roads” is explicitly stated by the governing text or only inferred from the three-item list; otherwise say simply that the record lists the dark road, bird path, and extending the hands.

### 付法 (`t_77774b8724f1`)

Strong contextual translation of `法` as “teaching,” with predecessor-successor entrustment, verse labels, narrative closure, the has-teaching/no-teaching verse, and the explicit ranking of recipients. It avoids leaving “Dharma” unexplained.

## Missing depth ledgers

The following b016 entries have no `WORK.md`, so they cannot yet pass the guide's required #0f reconciliation even where their prose is rich:

- 燈錄
- 雲水
- 王老師
- 森羅萬象
- 體露

## Recommended repair order

1. 印可 — remove imported realization/authentication theory.
2. 王老師 — correct master-specific schema/link semantics.
3. 本性 — reconcile the omitted high-frequency direct predicate.
4. 燈錄, 雲水, 森羅萬象, 體露 — create/reconstruct depth inventories and repair the specific semantic issues above.
5. 理事, 一隻眼, 本心 — bounded prose corrections.
6. Preserve the evidence depth of 見聞覺知, 單傳, 木佛, 鳥道, and 付法 while doing only targeted cleanup.

