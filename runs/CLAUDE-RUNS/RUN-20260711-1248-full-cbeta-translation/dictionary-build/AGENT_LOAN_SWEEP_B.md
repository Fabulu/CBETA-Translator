# Imported-loan sweep — Batch B

Edited prose only in the eight assigned entries. Sense structure, evidence inventory, KWIC text, source paths, line bounds, attribution links, roster links, and term status files were preserved. No manifest, plan, guide, termbase, other entry, or translated XML was touched.

| Term | Chinese construction | Previous loan rendering | Revised contextual English |
|---|---|---|---|
| 打成一片 | 佛法世法 | “Buddha's teaching and worldly dharma” | “Buddha's teaching and worldly affairs” |
| 綱宗 | 本無實法 | “originally there is no actual dharma” | “originally there is no actual thing” |
| 思量 | 是法非思量分別之所能解 | “this dharma is not what pondering-and-distinguishing can grasp” | “this matter is not what pondering and distinguishing can understand” |
| 目前 | 目前無法 / 不是目前法 | “no dharma before your eyes” / “the dharma before your eyes” | “no thing before your eyes” / “the thing before your eyes” |
| 宗旨 | 建法幢 | “raise the dharma-banner” | “raise the teaching-banner” |
| 擔荷 | 擔荷大法 | “shoulder the great Dharma” | “shoulder the great teaching” |
| 露地白牛 | 露地是所證之法故 | “the open ground is the dharma realized” | “the open ground is what is realized” |
| 祖師西來意 | 二祖得法 | “the Second Patriarch got the Dharma” | “the Second Patriarch obtained the teaching” |

## Verification

- 8/8 JSON files parse and retain their top-level schema.
- Protected-structure SHA-256 digests match the pre-edit values for all eight files. The protected projection excludes only `PreferredTarget`, `AlternateTargets`, `Explanation`, `Note`, and `AttributionNote`; everything else is unchanged.
- 47/47 occurrences return `zc.verify(...).ok == true`, and every stored `FromLb`/`ToLb` pair still matches the verifier exactly.
- All 18 distinct master-link values used by the entries remain present in the roster.
- Strict English-first and imported-framing scans report zero violations in the eight assigned entries.
- No standalone English `Dharma`/`dharma` or unexplained `samādhi` remains in their prose.

## Judgment notes

- The replacements were sentence-specific, not global graph substitution. Bare 法 was rendered as “thing” or “matter” where the construction points to an object under discussion, as “teaching” where the sentence concerns transmission or a banner/charge, and as “what is realized” where English does not require a separate noun.
- The Linji exchange retains the repeated obtaining/not-obtaining wording: “obtain the teaching” preserves 得法 while allowing the following “getting is not-getting” response to remain intact.
