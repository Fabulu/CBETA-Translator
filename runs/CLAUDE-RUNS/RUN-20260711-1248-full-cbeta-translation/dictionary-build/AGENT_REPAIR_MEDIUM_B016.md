# b016 medium/lower repair report

## Scope

Repaired the eight assigned b016 entries:

- 燈錄 `t_15eec715e731`
- 雲水 `t_2d92f15fa0ab`
- 理事 `t_9bdac4a01636`
- 一隻眼 `t_ccae22e8375d`
- 本心 `t_734eadab549a`
- 森羅萬象 `t_4416ef85b3a5`
- 體露 `t_94be914de45d`
- 鳥道 `t_462d9613abe9`

Added missing #0f ledgers for 燈錄, 雲水, 森羅萬象, and 體露. No status, manifest, termbase, wave-plan, merge, or corpus file was changed.

## Repairs

### 燈錄

Narrowed the definition to what the evidence directly establishes: a title family and collective reference, the headings and contents of named lamp compilations, a compiler’s enumeration of continued/linked/universal/broad lamp names, and an editorial use in checking damaged encounter wording. Removed the broader inferred “lamp-succession framework” genre claim.

### 雲水

Removed the unsupported statement that itinerants’ movement is compared to clouds and flowing water. Kept the primary institutional collective grounded in direct address, two-hall monks, reception lodgings, and the cloud-and-water hall.

The old secondary witness had ambiguous segmentation and was replaced with a clear literal landscape occurrence from `J/J37/J37nB372.xml`, `0076b02`–`0076b03`:

`笑傲乾坤雲水間，回頭處處是青山。花兩岸，柳一灣，時人不識謂偷閒。`

The surrounding green mountains, flowers on both banks, and willows around a bend establish literal “clouds and water.” The secondary sense remains provisional.

### 理事

Attributed the reciprocal relation explicitly to the Mirror Record: principle completes affairs and affairs display principle. The entry no longer generalizes that one text’s definition into the global meaning of the coordinate pair.

### 一隻眼

Removed the unsupported alternate “one discerning eye.” Retained only “one eye” and “a single eye,” matching the varied positive and adverse appraisals in the evidence.

### 本心

Removed the inferred condition “before anything additional is sought or set up.” The explanation now starts with the literal graphs and proceeds only through recorded predicates: neither joined nor separate; no form, color, root, lodging, arising, or extinction; and the correction concerning stillness.

### 森羅萬象

Changed the preferred target from “the whole array of phenomena” to the literal **“the myriad forms arrayed.”** Removed the generalized “visible or nameable multiplicity” abstraction. Preserved the Fayan moon reversal, rivers/sea comparison, illness chain, moon, medicine, and scenery deployments.

### 體露

Changed the preferred target from adjective-only “fully exposed” to **“body or substance exposed,”** retaining the noun while leaving its contextual referent open. Preserved the direct naming statement, Yunmen autumn-wind case, Blue Cliff witness, whole-body and imposing-and-evident expansions, and later direct question.

### 鳥道

The “three roads” language is explicit, not inferred. The source says, “From now on, permit him to study three roads, namely the dark road, the bird path, and extending the hands.” The curated occurrence was expanded to the exact verified frame:

`今時向去。許伊三路學。所謂玄路鳥道展手。`

Verified at `T/T47/T47n1992.xml`, `0618b29`–`0618c01`. The explanation now quotes the explicit list while retaining Dongshan’s “travel the bird path” and “not traveling the bird path” answers.

## Evidence changes

All existing evidence was preserved except the two targeted strengthening changes:

1. 雲水 secondary sense: replaced one admitted ambiguous poem with the clear literal landscape witness above; its secondary `SourceTexts` was synchronized.
2. 鳥道: expanded the existing same-source occurrence to include the immediately preceding explicit “three roads” frame and synchronized its verified bounds.

No other occurrence or source-text evidence was added, removed, or replaced.

## Final QA

- JSON files parsed: **8/8**
- occurrences checked: **43/43**
- `zc.verify(...).ok`: **43/43**
- stored bounds exactly match verifier bounds: **43/43**
- allowlist checks: **43/43**
- curated KWICs containing their headword: **43/43**
- banned imported-framing findings: **0**
- Chinese outside permitted evidence/schema fields: **0**

