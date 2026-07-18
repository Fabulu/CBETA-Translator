# Iriya sayings — semantic adjudication guide

**Audience:** the agent adjudicating the `IRIYA_SAYINGS_QUEUE.md` candidates (the `z*` final wave).
**Read `DICTIONARY_ENTRY_GUIDE.md` first.** This file adds only the traps specific to the Iriya sayings.
It does NOT change scope, priority, or the provenance firewall.

## 0. The one rule this whole step exists to enforce

**Mechanical clean ≠ acceptance. Mechanical absence ≠ rejection.** The validator's flags are *plumbing
signals* about strings. Whether a phrase becomes an entry is a *semantic* decision about **observable Chan
deployment**. Every disposition below must be grounded in what the corpus actually does with the phrase,
checked with `zc` (`zc.count`, `zc.find`, `zc.verify`) against the frozen corpus. Never decide from the
validator flag alone, and never from the Japanese source.

## 1. THE ADJUDICATION CRITERION (write this into every decision)

For each candidate, answer one question from the corpus, not from intuition:

> **Does the corpus DEPLOY this phrase with an observable Chan job — as an answer, a verdict, a capping
> phrase (著語), a test-question, an epithet, a raised case — or does it merely CONTAIN the phrase as
> quoted verse / ordinary running text?**

- **Deployed with a job → in scope** (even if the phrase originated as secular Tang poetry). This is the
  point of a Chan-usage dictionary: it records how the records *use* language.
- **Only contained as quoted verse, or generic phrasing with no Chan job → out of scope.** This is the
  Japanese-reception / 禅林句 layer that belongs to Kroll / Anderl, not here.

Worked example — **`薫風自南來、殿閣生微涼`** ("the fragrant wind comes from the south; the palace hall
grows faintly cool"): a Táng palace-poem couplet. It is out of scope *as poetry*. It is IN scope **only if**
`zc.find` shows a master wielding it as a 著語 / answer (deployment), with the exchange quoted in the entry.
If it appears only as recited verse, reject it and say so.

## 2. TRAP A — "exact form absent" is a SEGMENTATION signal, NOT a reject signal

The validator flags ~163 candidates whose exact contiguous string is absent from the corpus (`Pair` = 0,
only the `Anchor` component is attested). **Do NOT route these to reject.** The corpus writes couplets split
across a dialogue or under different interpunction, so the exact string legitimately fails to match while the
phrase is real and canonical.

Verified specimens now sitting in the exact-absent bucket that are genuine Chan formulas:

| candidate (pair absent) | what it is |
|---|---|
| `隨處作主、立處皆眞` | canonical Linji formula |
| `藏頭白、海頭黒` | stock Chan case-phrase |
| `炙脂帽子、鶻臭布衫` | Linji |
| `佛眞法身、猶若虚空、應物現形、如水中月` | stock verse the records deploy; anchor `如水中月` |

**Procedure for every exact-absent candidate:**
1. `zc.find` each clause and the whole phrase (try `。`, `，`, `、`, and no-separator joins, and search with
   short intervening spans). Confirm whether the couplet occurs as a unit at all, even when split.
2. Decide the **lexical unit**:
   - If masters deploy the **whole couplet** as a formula (across ≥2 independent works): **KEEP as couplet**;
     build the couplet headword; curate occurrences even where the two clauses are separated in the source.
   - If only a **clause** carries the Chan job (e.g. `如水中月` is the live phrase, the rest is framing):
     **KEEP the component**, drop the couplet headword, and record the couplet only in the component's Note.
   - If neither the couplet nor a clause is Chan-deployed: **REJECT** with the reason.
3. Never reject an exact-absent candidate solely because `Pair = 0`. State, per candidate, what `zc.find`
   showed.

## 3. TRAP B — anchor inflation: trust `Pair`, distrust `Anchor`

The validator flags ~753 candidates where the `Anchor` count is a generic component, not the saying. **The
`Anchor` number is an upper bound / recurrence signal only. Use the `Pair` (exact-form) count as the real
attestation.**

Verified specimen — **`三世諸佛、口掛壁上`** anchors on `三世諸佛` at ~2,830 hits, but `三世諸佛` ("the
buddhas of the three times") is generic Buddhist vocabulary, not the saying. The saying's real attestation is
its exact-pair count, which is far smaller. Never treat an anchor count as evidence the saying is common.
When you cite counts in an entry, cite the exact-form count, never the anchor.

## 4. TRAP C — two-work failures are `provisional`, not reject

~383 candidates hold in fewer than two **independent works** (count works, not files — split volumes and
reprints of one work are ONE source). A candidate that is genuinely Chan-deployed but single-source is
**`Validation = provisional`**, built and flagged, not rejected. Reject only for absence of Chan deployment,
not for thin multi-source support.

## 5. Disposition set (assign exactly one, with a corpus-grounded reason)

- **KEEP (couplet)** — whole multi-clause phrase is a Chan-deployed lexical unit; ≥2 independent works.
- **KEEP (component)** — only a clause is the real unit; build/route the clause, drop the couplet.
- **PROVISIONAL** — Chan-deployed but single independent work; build with `Validation = provisional`.
- **REJECT** — not Chan-deployed (quoted verse only / generic phrase with no job), or a substring better
  housed in an existing article. State which, with the `zc.find` evidence.

## 6. Non-negotiables (unchanged)

- **Provenance firewall:** no gloss, definition, sense, or example from Iriya & Koga's *Zengo jiten*. The
  headword list is a *selection signal only*. Every sense/gloss/occurrence/KWIC is derived independently
  from the corpus and passes `zc.verify`. (See `IRIYA_SAYINGS_QUEUE.md` header.)
- **Full `DICTIONARY_ENTRY_GUIDE.md` §5 #0–#0g applies** to any entry actually built: describe, don't
  interpret; primary Zen-technical sense first; honest validation state.
- **Ordering:** this adjudication runs at the `z*` (last) wave. Do NOT move any Iriya candidate ahead of an
  already-assigned higher-priority ordinal.
- **A negative result is a valid result.** Rejecting a candidate with a stated corpus reason is a correct
  outcome, not a failure.

## 7. Output

For each candidate adjudicated: record `{term, disposition, unit (couplet|component|—), validation, reason,
zc-evidence (the KWIC/deployment you found or the confirmed absence)}`. Write a running
`IRIYA_ADJUDICATION_LOG.md`. Do not build entries for REJECT/decompose-to-component couplets; hand
KEEP/PROVISIONAL forward to the normal build step under the guide.

### 7a. Row-association gate before independent review

Every author ledger must pass both:

```
python validate_iriya_author_ledger.py maintenance/<author-ledger>.json
python validate_iriya_author_ledger.py maintenance/<author-ledger>.json --full
```

The cheap pass binds each row's queue number, canonical index, ID, printed term,
and corpus query directly to the authoritative queue and requires its own query
in its own evidence windows. The full pass additionally reproduces the exact
hit/file/work totals, both line anchors, source titles, `zc.verify`, and canonical
`work_id` values. A failure stays with the author and is never dispatched to an
independent semantic reviewer. This gate exists because a batch once retained
the correct selected IDs while copying the previous offset's queries, counts,
reasons, and evidence into nine rows; identity-only preflight could not detect
that cross-row template association defect. The validator makes no semantic
decision and never substitutes for full-case independent reading.
