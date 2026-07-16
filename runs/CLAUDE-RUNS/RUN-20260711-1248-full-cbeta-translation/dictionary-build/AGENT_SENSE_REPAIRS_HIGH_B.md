# Agent report — confirmed high-confidence sense repairs B

Implemented the three confirmed #0f.8 splits assigned from `SENSE_AUDIT_0_4.md`. No `STATUS`, manifest, or merged termbase file was touched.

## 善知識

- Split the role noun **“a good teacher”** from the assembly vocative **“good friends!”**.
- Reassigned the existing Huineng address to the vocative and added an independent public-address witness from Huiyue Xu's record.
- Both senses are `multi-source`; seven total curated witnesses verify exactly.
- Cross-checked `參善知識`, definition formulas, role families, and public-address contexts. The split is based on different referents and grammatical functions, not different readings of one role.

## 三昧

- Preserved the primary lexical **“complete command”** sense and added the proper-person sense **“Master Sanmei.”**
- Anchored the person in three independent allowlisted records: two portrait/title uses and one ordination biography.
- The person sense is `multi-source`. No roster-backed canonical identity was found, so no `MasterName` or master-specific `SenseKey` was invented.
- Cross-checked the lexical compound family (`一行三昧`, `海印三昧`, `法性三昧`, `自受用三昧`) against the title/person and ordination family.

## 師子

- Preserved the primary animal **“lion”** sense and added **“Patriarch Siṃha.”**
- Anchored the named person in the *Patriarchs' Hall Collection* as the Twenty-fourth Patriarch and in Qianyan's record through the Kashmir beheading case.
- The person sense is `multi-source`; its Chan definition foregrounds lineage position and later case deployment rather than general hagiography.
- Cross-checked lion compounds (`師子兒`, `師子吼`, `師子座`) against the patriarch/title family. No exact local roster identity was found, so link fields remain unset rather than guessed.

## QA

- JSON parse: 3/3.
- Curated occurrences: 22/22 `zc.verify` exact and headword-bearing.
- Stored `FromLb`/`ToLb`: synchronized 22/22 after repairing three newly exposed end-line bounds.
- Validation: every new sense is supported by at least two independent allowlisted texts.
- English-first: Chinese outside `Kwic` is paired with an English rendering; no untranslated target or explanation fragment was introduced.
