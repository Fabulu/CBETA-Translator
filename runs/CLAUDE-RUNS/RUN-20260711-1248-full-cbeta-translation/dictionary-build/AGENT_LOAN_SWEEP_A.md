# Imported-loan sweep — Batch A

Edited prose only in the eight assigned existing entries. No sense, occurrence, source, anchor, attribution identity, roster link, or evidence inventory was changed.

| Entry | Contextual English revision |
|---|---|
| treasury of the true eye of the teaching (正法眼藏) | Removed the inherited loan from one alternate target, the literal graph account, and the title of the note on transmitting the robe and teaching. The preferred target remains derived from the corpus self-definition: the eye of the teaching transmitted from teacher to teacher, with “treasury” explained by the record's inexhaustible-eye simile. |
| true eye of the teaching (正法眼) | Replaced the inherited alternate target and removed the glossary-style claim that the eye “sees” an imported abstraction. The opening now follows the records' predicates: the eye is possessed, absent, brightened, and blinded. |
| separate transmission outside the teachings (教外別傳) | Rendered the question tail as “the teaching of,” the objection as “some further teaching left unexhausted,” and the final denial as no separate thing called a teaching outside mind. |
| transmitting mind by mind (以心傳心) | Rendered transmitted or entrusted 法 as “the teaching” in the Platform and lamp-record sentences and their attribution notes. Bodhidharma's historical name remains intact. |
| no-mind (無心) | Rendered 不取著諸法 as “does not grasp at anything.” |
| distinguishing (分別) | Rendered 諸法 as “all things” in the Vimalakirti line and 是法 as “this matter” in Dahui's question, including the corresponding attribution prose. |
| see one's nature, become Buddha (見性成佛) | Rendered 更無別法 as “there is no other teaching” and 法界眾生 as “beings throughout the whole realm.” |
| guest and host (賓主) | Replaced two unsupported religious-combat labels with the observable corpus settings: a face-to-face meeting in the record and two people meeting in an exchange. |

## Final QA

- JSON/schema: 8/8 entries parsed and retained the required PascalCase entry, sense, and occurrence fields.
- Protected-field comparison: 8/8 pre-edit fingerprints remained identical after excluding only `PreferredTarget`, `AlternateTargets`, `Explanation`, `Note`, and `AttributionNote`. This proves that sense structure, all evidence objects, KWICs, paths, line bounds, source lists, statuses, attribution identities, and roster links were preserved.
- Concordance verification: 44/44 occurrences returned `zc.verify(...).ok == True` with saved `FromLb` and `ToLb` unchanged and synchronized.
- Conformance: zero hard flags across all assigned prose under the current audit patterns.
- English-first: zero Chinese runs outside parentheses in prose.
- Roster: every non-null occurrence link and every related-master link matched an exact name in `master-dates.json`.

Only the eight assigned `entry.v2.json` files and this report were written for this sweep. No status, manifest, plan, guide, termbase, XML, other entry, or merge target was touched.
