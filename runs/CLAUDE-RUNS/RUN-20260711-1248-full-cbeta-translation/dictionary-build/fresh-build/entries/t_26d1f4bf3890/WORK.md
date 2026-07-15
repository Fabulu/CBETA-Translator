# WORK — 殺活 (t_26d1f4bf3890)


## Public-feedback reconstruction ledger

- feedback-inference-verdict: PASS — For 殺活, the displayed senses are killing and giving life; the definitions state only relations observed in the stored turns, contrasts, grammatical frames, and self-descriptions, without promoting an answer or symbolic association into the headword's meaning.
- feedback-observations: 7 exact headword/declared-variant occurrences across 7 source files support 1 different-thing sense(s); actor and source notes remain attached to every evidence row.
- feedback-falsification-searches: Re-tested literal versus Chan-loaded use, word versus title/person, corpus-wide versus master-specific scope, incompatible subject/event frames, and response diversity; only different referents or events justify the 1 retained sense(s).
- feedback-counterexamples: Negative, critical, quoted, narrated, and question-form witnesses were checked against the definition; differences of stance, answer, speaker, or grammar remain visible in evidence rather than being collapsed into an interpretive rule or inflated into polysemy.
- feedback-scope: multi-source; no master-specific sense. Corpus storage counts are concordance context, while the sense claims are limited to the exact witnesses and independent-work spread stored here.
- lookup-probes: killing and giving life; kill and revive; killing and reviving; take life and give life; death and life function; killing sword life-giving sword; to kill and to give life; killing and bringing to life; the killing-and-reviving function. These probes cover ordinary English synonyms, word-order variants, and the principal Chan-facing retrieval wording without changing the displayed definition.
- opening-interpretation-verdict: PASS — A reader can identify killing and giving life from the PreferredTarget and opening sentence before counts, graph analysis, named examples, or source discussion.
- material-claim-verdict: PASS — The earlier English phrase ‘made from’ described graph composition, not physical substance. The entry now says ‘the paired words’; no material claim is asserted.
**Gloss:** "killing and giving life" — the two paired functions of a Chan teacher.

## Concordance (zc, Zen-allowlist scoped)
- 殺活: 1275 hits / 280 files. 殺活自在 45, 殺活同時 21, 具殺活 (機/手段) common.
- Neighbours: 殺人刀 435, 活人劍 282, 殺人刀活人劍 25.

## Sense analysis
ONE corpus-wide sense (SenseKey=null). 殺活 is technical vocabulary of the Linji-lineage
paired functions, strung together in the record with 縱奪 / 權實 / 照用. Not a distinctive
coinage of any speaker — occurrences are stock/formulaic exposition → MasterName null on all.

## Key describe-only findings (grep-verified)
- Corpus itself equates 殺 / 活 with the two swords: 一轉語是殺人刀、一轉語是活人劍、
  一轉語殺活同時、一轉語殺活不同時 (J26nB187).
- Family clustering: 殺活縱奪、權實照用 (X66n1296).
- Fixed forms: 殺活自在, 殺活臨時 (如王寶劍，殺活臨時 — X64n1260), 具殺活機 (J28nB208).
- Staged question 如何是殺 answered by raising a foot (J26nB183).

## Validation
multi-source. 6 curated occurrences, all zc.verify ok=True, lb-exact, allowlisted.
RelatedTerms: 殺人刀, 活人劍, 照用, 縱奪.

## 2026-07-14 fresh-build actor and retrieval gate

- Added English retrieval aliases for kill/revive, take/give life, and the paired swords.
- Every named exact actor now carries a closed-vocabulary `utterer` context link. The unnamed monk remains a complete six-rung reviewed questioner, with Shiqi Tongyun separately identified as respondent and record owner.
- Re-tested all six exact occurrences with `zc.verify`; every stored range remains exact. All sixteen Chinese prose strings are anchored, and `audit_attribution.py --json` reports zero hard failures.
- Definition/sense verdict remains one sense: noun and verb packaging, simultaneous versus different-time operation, and favorable versus critical deployments all refer to the same kill/give-life pair.
