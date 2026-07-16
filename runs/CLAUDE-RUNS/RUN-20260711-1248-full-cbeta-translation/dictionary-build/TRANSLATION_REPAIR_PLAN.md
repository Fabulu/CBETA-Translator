# Existing translation repair plan

## Scope and verdict

This is read-only triage of all 21 translated XML files covered by `maintenance/translation-framing-audit-20260712T102705Z.json`. No XML was changed. The scanner produced **654 line/kind findings**, which collapse to **403 file-local anchored units** under the stable-anchor grouping below. Two base/community pairs are byte-identical, so there are **502 logical findings after mirror de-duplication**.

- Confirmed hard-gate repairs: **545 findings** in **349 file-local units**; **417 logical findings** after exact-mirror de-duplication.
- False positives / retain as written: **31 findings** in **23 file-local units**; **25 logical findings** after de-duplication.
- Chinese-source checks, not automatic violations: **78 findings** in **31 file-local units**; **60 logical findings** after de-duplication.
- Three scanned files have no findings: `xml-p5t/X/X69/X69n1333.xml`, `community/translations/Fabulu/A/A091/A091n1057.xml`, and `community/translations/Fabulu/X/X70/X70n1394.xml`.

An *anchored unit* is normally the enclosing paragraph `xml:id`. Text outside a `<p>` is anchored to its own stable `xml:id` where present; only where the XML supplies neither does this plan use an `lb:` fallback. Those fallback locations should receive or inherit a stable paragraph ID before prose editing, without changing existing IDs or links.

## Classification rules

- **Confirmed repair:** `koan` → public case/case; meditation furniture and rooms → Chan seat/bench/hall as the Chinese warrants; verbs such as sitting/investigating Chan translated literally; `practice`, `method`, and `technique` re-derived from the exact Chinese action; present-moment wording returned to on-the-spot/right-there/immediate expressions; duality/nonduality returned to literal two/not-two or the actual contrast; mindfulness wording removed from Chan prose.
- **False positive:** every standalone `Mu` hit is a name (Prefect Mu, King/Duke Mu, Chang Mu), never the graph 無; the three `Japanese` hits are factual source or compiler descriptions; the B07 catalog and T01 scripture faithfully name non-Chan source material (including the title *Mindfulness of Breathing*); “Mindful of it here” in T48n2004 is ordinary remembrance. These are not claims about Zen and should remain.
- **Source check:** `enlightenment` and `rebirth` are review terms, not mechanical replacements. Compare the aligned Chinese. Retain a direct quotation only when the Chinese literally states birth/heaven/awakening; otherwise use the exact event or verb (awoke, understood, attained the way, became buddha, birth, born again, and so on). Never turn the occurrence into a definition of Zen.
- **Direct Buddhist quotations inside Chan books:** wording such as the Vimalakirti “gate of nonduality” is still a confirmed repair under this project’s English rule: preserve the quotation but translate 不二 literally as “not two,” marking it as quoted source language rather than adopting the abstraction as Zen’s frame.

## Safe batching order

1. Repair the two exact base files T48n2004 and T48n2005 once, verify XML and anchors, then copy only the changed English text to their byte-identical community mirrors while preserving all IDs and markup.
2. Repair T48n2003 next (largest independent file), followed by T48n2012A, J27nB196, X73n1454, T47n1987B, and J24nB137.
3. Repair the smaller independent files T47n1987A, X63n1217, T16n0663, T47n1985, and the two non-identical T48n2010 versions.
4. Run source checks for enlightenment/rebirth only after hard replacements, because many share paragraphs with hard flags.
5. Leave B07 and T01 untouched for these flags. Re-run the body scanner, then manually inspect any remaining hits. XML parse, all pre-existing `xml:id` values, paragraph count, and link targets must be identical before/after.

## File summary

| File | Raw findings | Confirmed | False | Source-check | Anchored units | Action |
|---|---:|---:|---:|---:|---:|---|
| `xml-p5t/B/B07/B07na005.xml` | 6 | 0 | 6 | 0 | 2 | No automatic repair; retain false positives and perform source checks |
| `xml-p5t/J/J24/J24nB137.xml` | 21 | 21 | 0 | 0 | 15 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T01/T01n0001.xml` | 10 | 0 | 10 | 0 | 10 | No automatic repair; retain false positives and perform source checks |
| `xml-p5t/T/T47/T47n1987A.xml` | 17 | 16 | 0 | 1 | 9 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T47/T47n1987B.xml` | 27 | 20 | 1 | 6 | 14 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T48/T48n2003.xml` | 125 | 118 | 2 | 5 | 79 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T48/T48n2004.xml` | 143 | 120 | 5 | 18 | 96 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T48/T48n2005.xml` | 9 | 8 | 1 | 0 | 6 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T48/T48n2010.xml` | 2 | 2 | 0 | 0 | 2 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/T/T48/T48n2012A.xml` | 40 | 23 | 0 | 17 | 9 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/X/X63/X63n1217.xml` | 17 | 17 | 0 | 0 | 7 | Repair confirmed units; source-check review; preserve false positives |
| `xml-p5t/X/X69/X69n1333.xml` | 0 | 0 | 0 | 0 | 0 | No flagged prose; XML-parse regression check only |
| `community/translations/Fabulu/A/A091/A091n1057.xml` | 0 | 0 | 0 | 0 | 0 | No flagged prose; XML-parse regression check only |
| `community/translations/Fabulu/T/T16/T16n0663.xml` | 6 | 2 | 0 | 4 | 4 | Repair confirmed units; source-check review; preserve false positives |
| `community/translations/Fabulu/T/T47/T47n1985.xml` | 6 | 6 | 0 | 0 | 2 | Repair confirmed units; source-check review; preserve false positives |
| `community/translations/Fabulu/T/T48/T48n2004.xml` | 143 | 120 | 5 | 18 | 96 | Exact mirror of `xml-p5t/T/T48/T48n2004.xml`; synchronize after base repair |
| `community/translations/Fabulu/T/T48/T48n2005.xml` | 9 | 8 | 1 | 0 | 6 | Exact mirror of `xml-p5t/T/T48/T48n2005.xml`; synchronize after base repair |
| `community/translations/Fabulu/T/T48/T48n2010.xml` | 3 | 3 | 0 | 0 | 2 | Repair confirmed units; source-check review; preserve false positives |
| `community/translations/Fabulu/X/X70/X70n1394.xml` | 0 | 0 | 0 | 0 | 0 | No flagged prose; XML-parse regression check only |
| `community/translations/theksepyro/J/J27/J27nB196.xml` | 38 | 33 | 0 | 5 | 24 | Repair confirmed units; source-check review; preserve false positives |
| `community/translations/theksepyro/X/X73/X73n1454.xml` | 32 | 28 | 0 | 4 | 20 | Repair confirmed units; source-check review; preserve false positives |

## Exact anchored worklist

### `xml-p5t/B/B07/B07na005.xml`

- **FALSE POSITIVE / RETAIN — japanese-overlay:** `pB07pa001a1301`

- **FALSE POSITIVE / RETAIN — mindfulness:** `pB07pa001a0401`

### `xml-p5t/J/J24/J24nB137.xml`

- **CONFIRMED REPAIR — meditation:** `lgJ24p0357c0701`, `pJ24p0359b0601`, `pJ24p0368a0201`, `pJ24p0369a0501`

- **CONFIRMED REPAIR — meditation, practice:** `lgJ24p0371a1901`

- **CONFIRMED REPAIR — practice:** `lgJ24p0371a0101`, `pJ24p0359a2101`, `pJ24p0359c1801`, `pJ24p0359c2201`, `pJ24p0360a2501`, `pJ24p0361a1601`, `pJ24p0361c1101`, `pJ24p0362a2101`, `pJ24p0363c2301`

- **CONFIRMED REPAIR — present-moment:** `pJ24p0367a0601`

### `xml-p5t/T/T01/T01n0001.xml`

- **FALSE POSITIVE / RETAIN — mindfulness:** `pT01p0003c1305`, `pT01p0003c2501`, `pT01p0004a1001`, `pT01p0004a2001`, `pT01p0004a2801`, `pT01p0004b1401`, `pT01p0004b2101`, `pT01p0004b2801`, `pT01p0004c1201`

- **FALSE POSITIVE / RETAIN — practice:** `pT01p0001c1609`

### `xml-p5t/T/T47/T47n1987A.xml`

- **CONFIRMED REPAIR — enlightenment-review, practice:** `pT47p0530a1301`

- **CONFIRMED REPAIR — practice:** `pT47p0530c0601`, `pT47p0532a2801`, `pT47p0534b2001`, `pT47p0534b2801`, `pT47p0534c0901`, `pT47p0534c2901`, `pT47p0535b0301`, `pT47p0535b0801`

### `xml-p5t/T/T47/T47n1987B.xml`

- **CONFIRMED REPAIR — dualism, enlightenment-review:** `pT47p0536c1801`

- **CONFIRMED REPAIR — enlightenment-review, practice, reincarnation-afterlife:** `pT47p0539c2801`

- **CONFIRMED REPAIR — method-technique:** `pT47p0541a2001`

- **CONFIRMED REPAIR — practice:** `pT47p0536c2401`, `pT47p0538c2601`, `pT47p0540c0501`, `pT47p0542a2301`, `pT47p0543b2401`, `pT47p0543c2501`, `pT47p0544a0801`, `pT47p0544b0901`

- **SOURCE CHECK — enlightenment-review:** `pT47p0535c2401`, `pT47p0536a0301`

- **FALSE POSITIVE / RETAIN — japanese-overlay:** `pT47p0540b2101`

### `xml-p5t/T/T48/T48n2003.xml`

- **CONFIRMED REPAIR — dualism:** `lgT48p0210a0201`, `pT48p0209b2101`, `pT48p0209c0401`

- **CONFIRMED REPAIR — dualism, enlightenment-review, practice, reincarnation-afterlife:** `pT48p0210a1001`

- **CONFIRMED REPAIR — enlightenment-review, meditation, practice:** `pT48p0170b0601`

- **CONFIRMED REPAIR — enlightenment-review, practice:** `pT48p0205a0701`

- **CONFIRMED REPAIR — koan:** `lgT48p0144b1101`, `lgT48p0195a0101`, `pT48p0141a0201`, `pT48p0142a0501`, `pT48p0143b0401`, `pT48p0173a2701`, `pT48p0180a2101`, `pT48p0181c2101`, `pT48p0182b2601`, `pT48p0190a2301`, `pT48p0191c1701`, `pT48p0199c1601`, `pT48p0213c2801`

- **CONFIRMED REPAIR — koan, meditation:** `pT48p0175c0901`

- **CONFIRMED REPAIR — koan, practice:** `pT48p0181a0201`

- **CONFIRMED REPAIR — meditation:** `lgT48p0161a2901`, `lgT48p0188c0901`, `lgT48p0206a0601`, `pT48p0143b2101`, `pT48p0146b2001`, `pT48p0148c1701`, `pT48p0150b0101`, `pT48p0157c1801`, `pT48p0160b0301`, `pT48p0161b0401`, `pT48p0170a2501`, `pT48p0171b2601`, `pT48p0179a2401`, `pT48p0179b1101`, `pT48p0185c2801`, `pT48p0201b1201`, `pT48p0203b2001`, `pT48p0212c2401`, `pT48p0216c1101`

- **CONFIRMED REPAIR — meditation, method-technique:** `pT48p0159a1901`, `pT48p0198a0501`

- **CONFIRMED REPAIR — meditation, practice:** `pT48p0139b2301`, `pT48p0162c1101`, `pT48p0216a1523`, `pT48p0216a2401`

- **CONFIRMED REPAIR — method-technique:** `lgT48p0167a2501`, `pT48p0139a1901`, `pT48p0144b1601`, `pT48p0148b0501`, `pT48p0157a2201`, `pT48p0168c1401`, `pT48p0171c0301`, `pT48p0196c0401`, `pT48p0207b2001`, `pT48p0222b0801`

- **CONFIRMED REPAIR — method-technique, mu-loan:** `pT48p0175c2501`

- **CONFIRMED REPAIR — method-technique, practice:** `pT48p0145c1601`

- **CONFIRMED REPAIR — practice:** `lgT48p0180b2601`, `lgT48p0192a1601`, `pT48p0140a1201`, `pT48p0140a2801`, `pT48p0147a2801`, `pT48p0152b0501`, `pT48p0152c2101`, `pT48p0155a2401`, `pT48p0157c0101`, `pT48p0158a0501`, `pT48p0165a0901`, `pT48p0169c0701`, `pT48p0172c0301`, `pT48p0177b1401`, `pT48p0180c2101`, `pT48p0192c2201`, `pT48p0204b1301`, `pT48p0206b0901`, `pT48p0220c2401`, `pT48p0222a1001`

- **SOURCE CHECK — enlightenment-review:** `pT48p0205a0201`

### `xml-p5t/T/T48/T48n2004.xml`

- **CONFIRMED REPAIR — dualism:** `lgT48p0257c0801`, `pT48p0227b1701`, `pT48p0257a1301`, `pT48p0257b0501`, `pT48p0257b1201`, `pT48p0265c0801`

- **CONFIRMED REPAIR — dualism, method-technique, practice:** `pT48p0231a1501`

- **CONFIRMED REPAIR — dualism, mu-loan, practice:** `pT48p0259b2001`

- **CONFIRMED REPAIR — dualism, practice:** `pT48p0263c0501`

- **CONFIRMED REPAIR — enlightenment-review, koan, practice:** `pT48p0248b2001`, `pT48p0287b1201`

- **CONFIRMED REPAIR — enlightenment-review, meditation:** `pT48p0236c2001`

- **CONFIRMED REPAIR — enlightenment-review, meditation, practice:** `pT48p0237b0801`

- **CONFIRMED REPAIR — enlightenment-review, method-technique:** `pT48p0234b1201`

- **CONFIRMED REPAIR — koan:** `pT48p0230a1701`, `pT48p0231b1501`, `pT48p0234a0901`, `pT48p0239b0601`, `pT48p0253c0901`, `pT48p0256a0301`, `pT48p0267a0401`, `pT48p0267b1201`, `pT48p0272a0801`, `pT48p0284b0101`

- **CONFIRMED REPAIR — koan, method-technique:** `pT48p0235c0601`

- **CONFIRMED REPAIR — koan, practice:** `pT48p0268a2201`

- **CONFIRMED REPAIR — meditation:** `lgT48p0253b0501`, `lgT48p0278c2101`, `pT48p0227b0401`, `pT48p0236c1401`, `pT48p0242c0301`, `pT48p0252c0301`, `pT48p0254b0401`, `pT48p0277c1001`, `pT48p0278b2301`, `pT48p0278c0401`, `pT48p0278c2701`

- **CONFIRMED REPAIR — method-technique:** `lgT48p0234b0701`, `lgT48p0244c2301`, `lgT48p0281b1401`, `pT48p0229b1001`, `pT48p0244c2901`, `pT48p0258b2401`, `pT48p0263b1501`, `pT48p0264b0501`, `pT48p0267a1401`, `pT48p0280c1201`, `pT48p0281c1501`

- **CONFIRMED REPAIR — method-technique, practice:** `pT48p0246a2901`, `pT48p0289b1601`

- **CONFIRMED REPAIR — method-technique, present-moment:** `pT48p0275c0101`

- **CONFIRMED REPAIR — practice:** `lgT48p0235a0701`, `lgT48p0240b0201`, `lgT48p0268b2901`, `lgT48p0269b1001`, `lgT48p0271a1301`, `pT48p0226b1201`, `pT48p0230b0201`, `pT48p0230b2901`, `pT48p0230c0801`, `pT48p0231c2901`, `pT48p0232b1101`, `pT48p0234c1601`, `pT48p0235a1201`, `pT48p0240a1201`, `pT48p0241c2701`, `pT48p0250c0201`, `pT48p0254a0401`, `pT48p0254c2301`, `pT48p0256b1501`, `pT48p0259a0901`, `pT48p0269a1501`, `pT48p0269c2101`, `pT48p0270b0801`, `pT48p0271a1701`, `pT48p0274a2401`, `pT48p0274b2701`, `pT48p0275a1501`, `pT48p0279b1701`, `pT48p0279c1501`, `pT48p0282c1401`, `pT48p0285a1201`, `pT48p0288a0701`

- **SOURCE CHECK — enlightenment-review:** `pT48p0237c2201`, `pT48p0240c1901`, `pT48p0252c2701`, `pT48p0255c1201`, `pT48p0256a0601`, `pT48p0256a0901`, `pT48p0256a2101`, `pT48p0285c1901`

- **SOURCE CHECK — reincarnation-afterlife:** `pT48p0264a0201`

- **FALSE POSITIVE / RETAIN — mindfulness:** `lgT48p0254c0501`

- **FALSE POSITIVE / RETAIN — mu-loan:** `pT48p0233c0301`, `pT48p0263a2801`, `pT48p0283a1601`

### `xml-p5t/T/T48/T48n2005.xml`

- **CONFIRMED REPAIR — dualism:** `pT48p0299c0901`

- **CONFIRMED REPAIR — meditation:** `pT48p0292c1301`

- **CONFIRMED REPAIR — meditation, practice:** `lgT48p0299a2901`

- **CONFIRMED REPAIR — practice:** `pT48p0293a1601`, `pT48p0297b0501`

- **FALSE POSITIVE / RETAIN — mu-loan:** `pT48p0299c2501`

### `xml-p5t/T/T48/T48n2010.xml`

- **CONFIRMED REPAIR — dualism:** `lgT48p0376b2001`, `nkr_note_orig_0377003`

### `xml-p5t/T/T48/T48n2012A.xml`

- **CONFIRMED REPAIR — enlightenment-review, method-technique, practice:** `pT48p0380c2101`

- **CONFIRMED REPAIR — enlightenment-review, method-technique, practice, reincarnation-afterlife:** `pT48p0379c1801`

- **CONFIRMED REPAIR — enlightenment-review, practice:** `pT48p0381c1301`, `pT48p0383b1401`

- **CONFIRMED REPAIR — meditation, practice:** `pT48p0382b1001`

- **CONFIRMED REPAIR — method-technique, practice:** `pT48p0382b2801`

- **CONFIRMED REPAIR — practice:** `pT48p0383c1901`

- **SOURCE CHECK — enlightenment-review:** `pT48p0381b1701`, `pT48p0383a0301`

### `xml-p5t/X/X63/X63n1217.xml`

- **CONFIRMED REPAIR — practice:** `lb:0807a01`, `pX63p0001a0601`, `pX63p0001a2001`, `pX63p0001b0301`, `pX63p0001b0801`, `pX63p0001b1201`, `pX63p0001b1801`

### `xml-p5t/X/X69/X69n1333.xml`

No scanner findings. No prose repair scheduled; include in XML/ID regression validation.

### `community/translations/Fabulu/A/A091/A091n1057.xml`

No scanner findings. No prose repair scheduled; include in XML/ID regression validation.

### `community/translations/Fabulu/T/T16/T16n0663.xml`

- **CONFIRMED REPAIR — enlightenment-review, practice:** `pT16p0343c1902`, `pT16p0345b1401`

- **SOURCE CHECK — enlightenment-review:** `nkr_note_orig_0344001`, `pT16p0345a1405`

### `community/translations/Fabulu/T/T47/T47n1985.xml`

- **CONFIRMED REPAIR — meditation, method-technique, practice:** `pT47p0499a2801`

- **CONFIRMED REPAIR — practice:** `pT47p0499c1401`

### `community/translations/Fabulu/T/T48/T48n2004.xml`

Byte-identical to `xml-p5t/T/T48/T48n2004.xml` (SHA-256 `bae779a82ad9411e7486987f67e7042384bccf5d9886e69290998830f9b043db`). Do not translate independently: apply the reviewed base-file English changes at the identical anchors and keep the files synchronized.

### `community/translations/Fabulu/T/T48/T48n2005.xml`

Byte-identical to `xml-p5t/T/T48/T48n2005.xml` (SHA-256 `e2d7da8f6a542a1ed414b0b35576dbb2a22c50a1129da63edd4ba2441d538daf`). Do not translate independently: apply the reviewed base-file English changes at the identical anchors and keep the files synchronized.

### `community/translations/Fabulu/T/T48/T48n2010.xml`

- **CONFIRMED REPAIR — dualism:** `lgT48p0376b2001`, `nkr_note_orig_0377003`

### `community/translations/Fabulu/X/X70/X70n1394.xml`

No scanner findings. No prose repair scheduled; include in XML/ID regression validation.

### `community/translations/theksepyro/J/J27/J27nB196.xml`

- **CONFIRMED REPAIR — enlightenment-review, practice:** `pJ27p0382c3001`, `pJ27p0387b2701`

- **CONFIRMED REPAIR — meditation:** `lgJ27p0384a1501`, `lgJ27p0386a0701`, `lgJ27p0386a1601`, `pJ27p0388b0101`

- **CONFIRMED REPAIR — meditation, mindfulness, practice:** `pJ27p0389b1901`

- **CONFIRMED REPAIR — meditation, practice:** `lgJ27p0385c2101`, `pJ27p0383b2001`

- **CONFIRMED REPAIR — method-technique:** `pJ27p0379b0101`

- **CONFIRMED REPAIR — mindfulness:** `pJ27p0380c1601`

- **CONFIRMED REPAIR — practice:** `lgJ27p0387a1501`, `lgJ27p0387b0101`, `lgJ27p0387b0401`, `pJ27p0380c2501`, `pJ27p0382a0601`, `pJ27p0382a2101`, `pJ27p0382c2301`, `pJ27p0383a2501`, `pJ27p0389b0301`

- **CONFIRMED REPAIR — present-moment:** `pJ27p0378c2301`

- **SOURCE CHECK — enlightenment-review:** `pJ27p0382c0701`, `pJ27p0387b2201`, `pJ27p0388a0601`

### `community/translations/theksepyro/X/X73/X73n1454.xml`

- **CONFIRMED REPAIR — enlightenment-review, koan, meditation, practice:** `pX73p0441a2201`

- **CONFIRMED REPAIR — meditation:** `pX73p0435c0401`, `pX73p0437a0201`, `pX73p0437b1101`, `pX73p0437b1801`, `pX73p0439a1801`, `pX73p0440b1501`, `pX73p0440c1601`

- **CONFIRMED REPAIR — meditation, practice:** `pX73p0437c1001`

- **CONFIRMED REPAIR — method-technique:** `pX73p0434c0501`, `pX73p0444a0301`

- **CONFIRMED REPAIR — practice:** `pX73p0435b2301`, `pX73p0437c0601`, `pX73p0438a2201`, `pX73p0440a1801`, `pX73p0440b2301`, `pX73p0443a2401`

- **SOURCE CHECK — enlightenment-review:** `pX73p0437c0101`, `pX73p0442a1601`, `pX73p0442b2301`

## Per-paragraph repair protocol

For each confirmed or source-check anchor: open the translated paragraph and the CBETA Chinese paragraph with the same stable ID; identify the exact Chinese span behind each flagged English word; retranslate that span under #0/#0b/#0c without changing speaker turns, names, notes, markup, paragraph boundaries, `xml:id`, `lb`, or links. A global word substitution is unsafe except `koan` → `case/public case` after confirming 公案 in that span. Furniture compounds require their noun (`seat`, `bench`, `platform`, `hall`) and must not be flattened into an activity. Where `practice/method` translates several different graphs, record the graph-specific English choice in the repair ledger.

Validation for each batch: XML parses; the sorted set of every `xml:id` is byte-for-byte identical; every prior link target remains present; paragraph count is unchanged; scanner hard categories are zero except documented literal false positives; and a reviewer compares every changed sentence to the aligned Chinese before merge.
