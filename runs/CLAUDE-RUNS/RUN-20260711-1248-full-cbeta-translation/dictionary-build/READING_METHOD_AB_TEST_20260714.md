# Passage-reading versus full-method A/B test — 2026-07-14

## Design

Two clean read-only agents independently examined the same three untouched
entries: 毘尼, 活句, and 爛柯. The reading-only arm received the entries, stored
KWICs, and surrounding source XML, but none of the guide, audit, indices, or
research machinery. The full-method arm read the governing method and used the
concordance, counts, definition/collocation/countersearches, `zc.find`, and
`zc.verify`. Neither edited the dictionary.

## Timing

- Reading-only: 3.7 minutes total (2.3 + 0.7 + 0.7).
- Full method: 12 minutes total, including about 3 minutes of shared
  guide/tool setup and about 3 minutes per entry.

On this tiny trial, direct reading was about 3.2 times faster.

## What reading alone did unusually well

- It caught the decisive 爛柯 attribution error: the enclosing passage names
  保寧草堂離指方示禪師, not the stored Yongjue Yuanxian.
- It distinguished Tiantong's verse from Wansong's commentary.
- It reconstructed the 活句 cases well enough to see that the classification
  depends on use rather than a permanently “living” wording.
- It contextualized the major 毘尼 witnesses without mistaking container owner
  for exact turn merely because of the title.

## What reading alone missed or risked

- It used the forbidden umbrella label in its 毘尼 prose.
- It overreached in places on 活句 and inherited part of the familiar 爛柯 story
  without proving those details from stored corpus evidence.
- It could not measure missing evidence families. The full method found the
  recurrent 不守毘尼 question (20 hits/17 files), the unanchored 不死不活句 family,
  more contrasting 活句 public answers, and additional Mount Lanke witnesses.
- It did not expose schema defects, alias gaps, depth-floor balance, invalid
  SenseKeys/Validation, or English-first AttributionNote failures.

## Verdict

Reading is the semantic core and must decide the actor, turn structure,
referent, and case meaning. It is both faster and more accurate than attempting
to infer those facts from metadata. The broader method remains necessary as a
guardrail and falsification/coverage layer: it catches forbidden framing,
outside-story leakage, missing counterevidence, shallow sense coverage, and
mechanical schema/anchor defects.

The optimized process is therefore **read first, mechanically challenge
second**:

1. package the complete passage plus minimal source identity;
2. read it and record the exact headword utterer and minimal corpus inference;
3. run targeted concordance/countersearch only against the claims and missing
   deployment classes the reading produced;
4. apply schema, vocabulary, depth, forbidden-English, and `zc.verify` gates;
5. have an independent reader re-read the passage rather than approve a
   classifier's result.

Do not return to title/regex attribution. Do not discard the method's gates.
Remove machinery from the act of semantic decision and retain it around the
decision as adversarial QA.
