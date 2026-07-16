# First public-reader feedback — corpus inference, usable glosses, and search recall

This is a hard remediation specification, created 2026-07-13 from the first substantive feedback on
the published dictionary. It governs all existing and future entries together with
`DICTIONARY_ENTRY_GUIDE.md` §5 items 11–19. Production-wave work remains paused until the calibration
entries, gates, and durable queue below are integrated.

## The three reports and adjudicated defects

1. `鳥道` was published as **“bird path.”** That is a graph calque, not a usable definition. The
   allowlisted corpus repeatedly says the bird course has no tracks/trace or lodging and separately
   describes a bird flying through space without leaving a trace. The licensed minimal inference is a
   **bird's trackless flight-course through open air**. “Cannot be traveled” is too strong because the
   corpus repeatedly says `行鳥道`. Full concordance also exposes a likely second thing: a precipitous
   mountain path fit only for birds. This requires item-8 adjudication and an anchor under each retained
   sense. With 627 hits/168 files, the former five-anchor article also fails the current depth floor.
2. `玄路` already has hidden/dark/mysterious road translations, but the live website uses literal
   substring search. `dark path`, `dark way`, `hidden path`, and similar natural queries return nothing.
   This is a retrieval-model defect. Add per-sense non-display `SearchAliases`, ranked exact-first search,
   and regression probes. Do not turn search metadata into an indiscriminate translation menu.
3. `金鎖` was published with **“Literally, a lock made of gold.”** The graphs and saved evidence do not
   establish material manufacture. The same article then invented “valuable” and “prized” symbolism.
   Exact equations and governing verbs do establish figurative obstruction in central Chan uses. A
   separate lock/barrier versus chain/fetter split is likely and must be tested by incompatible verb
   frames. Nested `金鎖骨` families do not buy standalone depth.

## Governing distinction

Prevent interpretation from outside; require inference from what is there. Every semantic bridge uses
the item-11 ledger: anchored observations, minimal inference, ordinary bridge, falsification searches,
counterexamples, narrowed scope, and `direct | licensed | uncertain | reject`. “The corpus did not say
what gold means in the first few anchors” is not a stopping rule.

## `金鎖` special research gate

Before drafting, inventory `金` across the allowlisted corpus deeply enough to test the modifier relation:

- `金` definition formulas, equations, and explicit material predicates;
- demonstrably material controls (`金佛`, furnace, melting, casting, manufacture);
- color/appearance uses, conventional names/epithets, and figurative comparisons;
- every standalone `金鎖` and `黃金鎖` lock/barrier predicate;
- every chain/fetter predicate (`掣斷`, `脫`, binding, holding, throat, elephant imagery);
- nested `金鎖骨 / 金鎖子骨 / 黃金鎖子` contamination;
- closest modifier parallels (`金毛師子`, `金翅鳥`, `金鱗`, `金身`, `金牛`, `金剛` families).

`WORK.md` records counts, representative exact anchors, rejected candidate inferences, and why the final
claim about `金` survives. A bare English “golden X” is not a safe fallback when readers will infer solid-gold
construction: the displayed target itself must name the established referent without that false implication, and
the explanation must record the unresolved or conventional modifier relation. If the corpus supports a narrower
ornamental, appearance, or conventional-register inference, state it at exactly that scope.

Inherited user lead to test, not assume: **`金` may mark enlightenment, and `銀` may supply a contrast or
parallel control.** Search explicit gold/silver equations, paired 金/銀 images, master appraisals, color/material
controls, and the same predicates across both modifiers. Record keep/revise/reject. Frequency or a familiar
religious association alone does not establish the lead; convergent corpus predicates may.

## Schema and search requirements

- Add `SearchAliases: string[]` to each dictionary sense. Preserve it through the C# model, merge
  normalization, rich shards, website normalization, and browse search. It is non-display metadata.
- Search ranking: exact Chinese/headword > exact PreferredTarget > exact AlternateTarget > exact
  SearchAlias > controlled synonym expansion > prose/KWIC mention.
- Normalize case, punctuation, and safe hyphen/spacing variation. Synonym clusters are curated and
  sense-approved, not global semantic equations.
- Each sense records 3–5 lookup probes in `WORK.md`; tests must return the intended entry. Calibration
  for `玄路`: hidden/dark/mysterious × road/path/way/route.

## Retrospective scope and baseline

Scope is every current `terms/*/entry.v2.json` (636 files at creation), not only the 621 currently merged,
plus every future entry. Previous attribution/depth approval does not waive this gate.

- 391 current explanations begin with `Literally…`: queue for the plain-English referent/image gate.
- 28 current headwords contain one of `金銀玉鐵銅木石泥`: queue for modifier-relation adjudication.
- Every such headword also records `display-modifier-verdict:`. An unresolved modifier plus a material-looking
  English PreferredTarget is a hard failure; the visible gloss cannot make a claim that its note retracts.
- Every sense receives search probes/aliases, even when the result is “preferred target already covers the
  natural probes.”
- Every entry receives nested-compound and verb-frame review; detectors queue candidates and never rewrite.
- Component corrections propagate to dependent families.

## Three-role acceptance flow

For every flagged entry:

1. Evidence reviewer builds the graph/ordinary-scene/Chan-deployment layers and inference ledger.
2. Independent falsification reviewer searches literal controls, counterexamples, incompatible verb frames,
   nested compounds, family conflicts, and alternative senses.
3. Root adjudicates, verifies every added occurrence with `zc.verify`, runs attribution/depth/English/search
   gates, and records keep/revise/reject. No auto-rewrite and no quota drafting.

Calibration starts with `鳥道`, `玄路`, and `金鎖`, then propagates to `金鎖玄路`, `三路`, `玄關`,
`凡情聖見`, and `無事人`. The full 636-entry pass follows deterministically. Existing Rule-10 remediation
may be combined with this pass, but the first 102 already-cleared original entries must be reopened for these
new gates. No termbase merge until a coherent accepted cohort is ready; never hand-edit generated artifacts.

Standalone `金` and `銀` are now requested keystone entries. The `金鎖` calibration research is their inherited
evidence seed, not a substitute for their own full concordances and sense audits. Build them before resuming the
ordinary requested-term queue.

## Second calibration finding — components, roles, comparisons, and compounds

The first standalone `金`/`銀` drafts passed mechanics but failed independent semantic review. This finding applies
retroactively to every entry:

- A different role is not a different thing. Gold stolen, demanded, priced, or exchanged is still the same gold
  commodity unless the corpus establishes a distinct lexical object; do not split “gold” from “gold as wealth.”
- A comparison standard is not automatically a new bare-word sense. `黃如金` says “yellow like gold”; it does not
  by itself make bare `金` mean “gold-colored.” The same applies to `白如銀`.
- Compounds cannot lend their referent, deployment, or lookup aliases to a component. `金毬`, `金球`, `金彈子`,
  and `銀彈子` require their own entries/adjudication; bare `金` must not advertise “gold ball” or “gold pellet.”
- Do not project one common modifier theory across unrelated families. `銀盌盛雪`, `銀山鐵壁`, `銀籠`, and
  `白銀世界` keep their own predicates and modifier verdicts unless convergent evidence proves a shared relation.
- Reader-ready openings picture the ordinary object before editorial abstraction. For `銀彈子`, say that the
  image is a small rounded pellet/ball and immediately state that material/color/conventional naming is unresolved;
  only then explain Nanquan's local value contrast and the iron-palm counterfamily.

These are hard applications of guide items 8, 15, 16, and 18, not term-specific preferences. Mechanical pass
cannot override an independent different-things or nested-family failure.
