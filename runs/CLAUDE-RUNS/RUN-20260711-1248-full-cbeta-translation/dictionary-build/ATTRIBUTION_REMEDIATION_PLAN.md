# Attribution remediation plan — rule 10

Current work is governed by `ATTRIBUTION_FIX.md` and `DICTIONARY_ENTRY_GUIDE.md` item 10. New-wave
integration is paused until the same attribution gate is applied to its drafts. Edit only
`terms/<id>/entry.v2.json`; never hand-edit generated termbase artifacts.

## Hard requirements

- Work every occurrence through the six-rung speaker ladder. `MasterName` is never left null merely
  because the KWIC says only 師. Use roster `names[0]` exactly when present. For a demonstrably named
  speaker absent from the roster, retain a stable pinyin name and flag roster expansion; never invent,
  substitute, or erase the speaker.
- **Roster-link validation is temporarily deferred** while a separate agent expands the master roster
  (user, 2026-07-13). Continue preserving and counting source-attested non-roster pinyin names, but do
  not treat them as attribution failures and do not spend this pass reconciling roster pages. Re-run the
  exact `names[0]`/website-link audit after that roster work is integrated.
- Every `AttributionNote` names the exact TEI title returned by `zc.title(RelPath)` and the speaker.
- Replace vague prose attribution with the identified person and source.
- Anchor every Chinese evidence string with a new `zc.verify`-confirmed occurrence. Do not delete
  useful evidence to pass the gate. If a string is not in the allowlisted corpus, stop and report it as
  possible paraphrase, outside-source evidence, or transcription error.
- Re-test the entry's definition and different-things sense split against every newly added witness.
- A quote anchor whose KWIC lacks the exact headword is retained with `EvidenceRole: "family"` or
  `"contrast"`, but it does not count toward depth or source spread. `audit_depth_sense.py` now counts
  exact-headword witnesses separately from total stored support.
- Run `audit_attribution.py` on the edited entry paths plus the existing mechanical/semantic gates.

`zc.context(rel, lb, chars, kwic=kwic)` implements rung 2 for 500/2,000/10,000-character windows;
`zc.heads(rel, lb, kwic=kwic)` and `zc.title(rel)` support rungs 3 and 4. **Always pass the KWIC:**
CBETA line numbers repeat across fascicles, so bare-lb context can land on the wrong identically numbered line.

## Frozen before-state

The original spec snapshot is **606 entries / 3,562 occurrences**:

- named: 749; null: 2,813; non-roster strings requiring reconciliation: 38;
- AttributionNotes: 3,470; missing notes: 92;
- vague-attributor regex findings: 805;
- the deliberately broad Chinese-evidence detector finds 6,594 distinct per-sense strings, of which
  2,283 do not yet match a stored KWIC. This detector is broader than the spec author's historical
  3,883-quote/1,201-dangling measurement; findings must be adjudicated as evidence strings rather than
  silently treated as equivalent counts.

The quote-anchor/depth split gate added during cross-check finds **115 unlabelled non-headword
occurrences across 68 merged entries** (plus six already adjudicated `family` anchors). These 115 must
be individually classified as `family` or `contrast`, and each affected entry must independently clear
its exact-headword frequency/source floor. Do not bulk-label them and do not delete them.

The **15 post-snapshot r002 entries / 104 occurrences** are a separate cohort and must not be assumed
covered merely because the combined total is 621 / 3,666:

- named: 12; null: 92;
- all 104 have notes, but none yet names the exact TEI title and 102 do not name the canonical speaker;
- vague-attributor findings: 18;
- Chinese-evidence detector: 26 strings, 2 dangling.

The three r002 batches are being remediated independently before the 606-entry sweep. The final report
must preserve separate before/after counts for both cohorts and then give the combined 621-entry result.

### r002 remediation result (integrated 2026-07-13)

- 104/104 occurrences now have a six-rung-resolved `MasterName` (before: 12 named / 92 null).
- Independent `zc.verify` pass: 104/104 KWICs exact with matching `FromLb` and `ToLb`.
- 104/104 AttributionNotes name the exact TEI title and the speaker; vague/source/speaker/dangling
  failures are zero; the broad evidence detector is 26/26 anchored.
- 59 occurrence-level roster flags remain. They are named speakers (including minor masters, lay
  speakers, and the compilers Jing and Yun) absent from the current 301-name roster, not unresolved
  attribution. Their pinyin names and source evidence are preserved in the entries and `WORK.md` files.
  Reconcile them separately; never turn these 59 names back into nulls to make a roster audit quiet.
- Material corrections found by the pass include Fenyang rather than Shitou for the Herdboy Song,
  Zaitian Yu for a duplicated wooden-horse line, Gaofeng inside Duanya's biography, Fang'an Xian for
  the Lanke passage, and Jing and Yun rather than Xuefeng for Zutang compiler narration.

## Work order

1. Remediate and independently verify r002-A/B/C (five non-overlapping entries each).
2. Re-run the separate r002 audit; resolve roster gaps without losing attested names.
3. Partition the original 606 by entry ID into non-overlapping batches; remediate, cross-check, and
   close each batch before reusing a worker.
4. Apply rule 10 to all unmerged r003 drafts, then finish r003's outstanding semantic corrections.
5. Only after the all-entry gate passes: register/merge r003 and resume requested waves.
