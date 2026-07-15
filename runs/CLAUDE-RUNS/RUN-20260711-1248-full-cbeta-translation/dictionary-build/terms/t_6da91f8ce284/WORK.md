# 賓主 (t_6da91f8ce284) — guest and host

## Concordance (allowlist-scoped, counts per text; top texts)
賓主 is one of the highest-frequency technical terms in the Zen corpus. Sampled counts
(allowlist only): X82n1571 (92), J34nB299 (62), J25nB171 (46), X66n1296 (45),
X80n1565 (40), X64n1260 (40), X79n1557 (38)… hundreds of texts total. Related strings:
四賓主 (Linji's four), 賓看主/主看賓/主看主/賓看賓 (the four categories),
賓中主/主中賓/主中主/賓中賓 (the Caodong four positions), 賓主歷然, 賓主互換.

## Sense analysis (3 senses)
1. **Corpus-wide "guest and host"** (SenseKey null). The two roles in a Dharma meeting;
   who holds the initiative/true eye (host) vs. who is tested (guest). Stock verdict
   賓主歷然. Locus: Linji lu (T47n1985) 賓主歷然 / 臨濟賓主句.
2. **Linji's 四賓主** (SenseKey Linji Yixuan). Four EXAMINING relations —
   賓看主 / 主看賓 / 主看主 / 賓看賓 — a capacity-test taxonomy. Locus: T47n1985;
   reproduced J26nB183, J23nB134; catechized J25nB171; headed B27n0152. Listed beside
   三玄三要 / 四料揀 / 四照用.
3. **Caodong 回互 interchange** (SenseKey Caoshan Benji). 賓主 as a Five-Ranks pair that
   TRADES PLACES: 賓主互換, 彼此回互, expanded to 賓中主/主中賓/… Locus: Dongshan's
   賓中主/主中主 series (B25n0144); formulas in J25nB163, J28nB212.

## Multi-source verdict
- Sense 1: **multi-source** — pervasive; Linji lu + all major collections.
- Sense 2: **multi-source** — T47n1985 + J25nB171 + J26nB183 + J23nB134 + B27n0152.
- Sense 3: **multi-source** — B25n0144 (Dongshan) + J25nB163 + J28nB212 + T47n1987A/B.

## Reconciliation with existing termbase
The task flagged an existing 回互 Caoshan sense of "guests/hosts changing places." That
is exactly Sense 3 here (賓主互換 / 彼此回互), now grounded in Chinese and keyed to Caoshan
Benji — with the honest caveat that it is a Dongshan-Caoshan SHARED usage (Dongshan asks
the 賓中主 series), not Caoshan's private coinage.

## Honest thin spots
- The 賓看主 four-name gloss in J25nB171/J26nB183 reproduces the Linji lu passage; treated
  as faithful transmission, not independent invention (still counts as multi-witness).
- B27n0152's four appear as interleaved song-case headings, so only the first
  (舉臨濟大師四賓主 / 賓看主) is quoted verbatim contiguously; the other three are cited by lb.
- Boundary between Sense 1 and Sense 2 is graded: many "who is host?" lines are the
  general sense edging toward the formal four. Assigned by presence of the 看-taxonomy.

---
## Gate 2 (Claude adversarial verify-and-repair) — VERIFIED
- All 8 curated KWICs across all 3 senses confirmed EXACT-CONTIGUOUS substrings of their
  cited files (XML tags + inline <note> apparatus stripped; no ellipsis, no stitching).
- All 3 SourceTexts sets + all occurrence RelPaths are in zen-corpus.json allowlist — zero contamination.
- All FromLb anchors correct: they use the primary `<lb ed="X">` CBETA numbering (verified the
  cited lb directly precedes each KWIC; the file also carries a secondary `ed="R138"` reprint lb —
  the author correctly took the ed="X" value).
- Multi-source gate holds for all 3 senses (Sense 1 pervasive; Sense 2 T47n1985+J25nB171+J26nB183+
  J23nB134+B27n0152; Sense 3 B25n0144 Dongshan + J25nB163 + J28nB212). Caodong Sense 3 keeps the
  honest "Dongshan-Caoshan shared, not Caoshan's private coinage" caveat (buffalo-style honesty).
- Renderings deflationary/literal ("guest and host"); no imported abstraction.
- RelatedTerms are genuine constituents/deliberate semantic links (賓主歷然/四賓主/回互 etc.) — no
  coincidental-prefix traps.
- No repairs required to entry.v2.json. STATUS -> verified.

---
## Gate 3 REVISE — repairs applied (2026-07-11 18:01 +02:00)
Gate 3 (Fable) flagged REVISE. Every claim below re-verified against the raw TEI (tags +
whitespace stripped, note apparatus excluded) before editing. Four fixes:

1. **Two fabricated ellipses in sense-3 KWICs → replaced with exact-contiguous spans.**
   - `J/J25/J25nB163.xml` lb 0256a22: was `賓主互換，偏正不拘，至……` → now
     `賓主互換，偏正不拘，至位融和，所謂無礙。` (source line 2998-2999:
     `…賓主互換，偏正不拘，至位融和，所謂無礙。山不接意…` — exact, contiguous across the lb).
   - `J/J28/J28nB212.xml` lb 0475c25: was `未免賓主相待，彼此回互。直須賓不是賓、賓……` → now
     `未免賓主相待，彼此回互。直須賓不是賓、賓中有主，主不是主、主中有賓，賓主交參、互換無位`
     (source lines 668-669: `…直須賓不是賓、賓中有主，主不是主、主中有賓，賓主交參、互換無位，到這裏…`
     — the fuller run is STRONGER interchange evidence).

2. **Misattribution of 二俱是瞎漢 (sense-2 Explanation) → reattributed.** Verified
   `grep -c '二俱是瞎漢' T/T47/T47n1985.xml` = 0 (absent from the Linji lu). The Linji lu's own
   fourth-case gloss is 彼此不辨, main text lb 0501a15: `人歡喜。彼此不辨，呼為客看客。` (written with
   the 客 graph). 二俱是瞎漢 belongs to J25nB171 lb 0534c15 (`賓看賓？」師云：「二俱是瞎漢。」`), the
   later Linji-house catechism. Explanation now names 彼此不辨 as Linji's wording and flags
   二俱是瞎漢 as J25nB171's. The occurrence itself was already correctly RelPath=J25nB171 with the
   exact KWIC (option b already satisfied) — only the prose laundered it.

3. **OVERREAD (house-level) → caveat added to sense-3 Explanation.** Verified in T48n2006
   (人天眼目): 臨濟門庭 lb 0311b16-18 defines a LINJI 四賓主 in the same 中-names
   (`四賓主者。師家有鼻孔。名主中主。學人有鼻孔。名賓中主。師家無鼻孔。名主中賓。學人無鼻孔。名賓中賓。與曹洞賓主不同。`)
   and 曹洞門庭 lb 0320c12-13 answers (`四賓主。不同臨濟。主中賓。體中用也。賓中主。用中體也。`). Caveat now
   states the 中-position names are shared cross-house vocabulary with different glosses; the
   Caodong hallmark is the 偏正/回互 pairing, not the mere 中-names. Also folded in the minor
   Dongshan-as-answerer clarification (Dongshan ASKS in B25n0144; 白雲蓋青山／長年不出戶 are the
   interlocutor's replies).

4. **J23nB134 note fix.** Verified `grep '客看主\|主看客'` → 客看主 lb 0525c08, 主看客 lb 0525c10;
   `grep -c '賓看主'`=0, `grep -c '四賓主'`=0. Sense-2 Note now states J23nB134 reproduces the
   passage with the 客 graph throughout (not the 賓-names, no 四賓主 label, no 二俱是瞎漢) — a
   variant witness kept in SourceTexts for the reproduction only.

**Final KWIC verification: 8/8 exact-contiguous verbatim in the reading flow** (Python
tag+whitespace strip, then a second pass with `<note>…</note>` apparatus removed — all 8 still
PASS in-flow, not notes-only). Validation states unchanged: all 3 senses retain ≥2 independent
allowlisted witnesses → multi-source held for all three. STATUS -> verified.

---

## Gate 3 round-2 residual fix (2026-07-11) — sense-2 graph attribution

Gate 3 (fresh verifier) confirmed all four round-1 fixes and all 8 KWICs, but caught ONE
residual factual error in sense-2 prose about which graph the Linji lu writes. Fixed prose
only (Explanation + Note); no KWIC/occurrence field touched.

**Client re-grep of T47n1985.xml (whole file):** 客看主=1, 主看客=1, 主看主=2, 客看客=1,
賓看主=1, 主看賓=1, 賓看賓=1, 四賓主=0. The 客-forms sit in the BODY (lb 0501a08–a15:
`喚作客看主…此是主看客…此喚作主看主…彼此不辨，呼為客看客`). The 賓看主/賓看賓 forms occur ONLY on
line 1349 — inside back-matter apparatus note n=0505003 (type="orig", Ming/宮 recension:
`喚作賓看主…此是主看賓…此喚作主看主…彼此不辨喚作賓看賓`). Body 賓看主/主看賓/賓看賓 = 0.

**Correction made:**
- Explanation: the Linji lu MAIN TEXT writes the guest with 客 in all four
  (客看主／主看客／主看主／客看客, lb 0500c26–0501a15); the 賓-graph names are the later/standard
  form and appear in T47n1985 only via the Ming apparatus note n=0505003. Removed the false
  "only the fourth case uses 客" implication.
- Note: J23nB134 (客看主 0525c08 / 主看客 0525c10) reproduces the SAME 客 graph as the Taishō
  main text — it MATCHES the main-text recension, not a "variant witness"; it merely lacks the
  四賓主 label and the later 二俱是瞎漢 tag.

**Re-verified after edit:** JSON re-parses (3 senses); all 8 KWICs still exact-contiguous
verbatim (8 OK / 0 MISS). STATUS unchanged -> verified.

---
## #0f.8 sense repair — merge of paraphrastic corpus-wide senses (2026-07-13)

The two former null-key senses were re-tested against the guide's different-THING test:

- `guest and host` named the paired roles in meetings and encounter records.
- `guest and host (interchanging)` named those same roles in the Caodong formulas
  `賓主互換` ("guest and host exchange places") and `彼此回互` ("the two mutually
  turn through each other"). Interchange changes their positions; it does not create a
  different referent, word class, or object.

They are therefore one corpus-wide sense. The repair preserves all five curated witnesses,
all source texts, the Dongshan and Caoshan links, the Caodong `偏正` ("bent and upright") /
`回互` ("mutual interchange") deployment, the cross-house position-name caveat, and the
counts. The primary target remains literal `guest and host`; interchange is now an alternate
and a fully described deployment rather than a second sense.

Linji's `四賓主` ("four guest-and-host relations") remains separate and master-specific.
It is not a paraphrase of the pair: it is a named fourfold taxonomy whose members are formed
with `看` ("examine") and independently transmitted as Linji's four. Its three curated
witnesses and recension caveats are retained.

Family cross-check: the ordinary meeting pair, Zhaozhou's named no-guest-no-host talk,
Linji's four examining relations, and Caodong positional interchange can all stand together
under this structure without assigning one witness incompatible referents. Result: 3 senses
became 2 (one corpus-wide pair + one genuinely master-specific Linji taxonomy).

Mechanical recheck after repair: JSON parses; 8/8 curated KWICs return `zc.verify(...).ok`
with saved `FromLb`/`ToLb` bounds unchanged and exact. Six witnesses contain the exact compound;
the two constituent-form witnesses intentionally anchor `賓中主` / `主中主` and `賓看主`, the
attested families discussed by their senses. No `STATUS`, manifest, or merged termbase file was
touched.

English-first follow-up: the corpus-wide note's bare headword was changed to "the term
'guest and host' (賓主)." The sense structure and evidence are unchanged; JSON parsing and all
8 KWIC anchors were rechecked successfully.

## L002-B item-8 merge, family, definition, and #0g retest

- sense-target-distinguishability: MERGE. The former `the four guest-and-host relations` sense names 四賓主, a larger compound and dedicated dictionary entry built from the same guest/host pair; it is not a second referent of bare 賓主. Interchange and positional use likewise alter the relation or compound, not the pair named by the headword.
- Merge result: one clean target, `guest and host`. Two exact 四賓主 witnesses remain as family evidence; the prior 賓看主-only witness was not retained as a headword anchor because it does not contain 賓主 contiguously.
- Definition/family retest: 賓主歷然, 賓主句, 一喝分賓主, 無賓主話, 賓主互換, 四賓主, 賓中主, 偏正, and 君臣 all preserve one base pair while organizing it in different formulas or systems.
- #0g retest: ordinary meeting roles become a repeated public-interview vocabulary for distinguishing, exchanging, and negating positions. No claim is made that every school organizes the pair identically.
- Strengthened-depth follow-up: added Zhaozhou's named no-guest-no-host story from an additional source, a distinct negated-case deployment that brings the merged high-frequency entry to its eight-occurrence floor.

## 2026-07-13 full remediation
- Kept the required merge: `guest and host` and `guest and host (interchanging)` remain one sense under item 8.
- Reclassified the positional witness and both 四賓主 witnesses as family evidence, then rebuilt to 11 total / 8 exact across 7 exact sources with a public Juelang exchange, the one-shout catechism, and Yuanwu's mutual guest-host deployment.
- Every prose claim is anchored, including `one shout distinguishes guest and host`; all KWICs/bounds verified. Attribution and depth/sense gates pass; broad-single-sense review was consciously adjudicated as one pair.

## semantic-r001 public-feedback remediation (2026-07-14)

- feedback-inference-verdict: KEEP one base-pair sense. Distinction, opposition, interpenetration, exchange, negation, and role-testing alter the relation between guest and host; they do not create different referents. The fourfold Linji taxonomy and positional compounds are larger family terms, not senses of the bare pair.
- feedback-observations: Linji Yixuan says the roles stand out distinctly; Guting Shanjian says they exchange places; Eryin Mi describes opposition, interpenetration, and positionless exchange; Zhaozhou Congshen names a no-guest-no-host story; Juelang Daosheng and Xuean Congjin publicly test presence, distance, speech, and distinction; Yuanwu Keqin requires mutual guest-host action.
- feedback-falsification-searches: Rechecked distinct guest-and-host, guest-host phrase, no-guest-no-host, exchange, mutual turning, one shout distinguishes the roles, whether they are present, their distance, their speech, the fourfold taxonomy, guest-within-host positions, bent/upright, and ruler/minister families. Tested whether interchanging roles create a second thing; the predicates keep the same pair.
- feedback-counterexamples: The former “interchanging” sense paraphrased the base pair and therefore fails item 8. Cross-house use of positional names prevents assigning all guest-host vocabulary to one school. Family-only positional and fourfold witnesses do not buy exact-headword depth.
- feedback-scope: One corpus-wide role pair used in face-to-face encounters and teaching-seat explanations. Individual taxonomies organize the pair differently but do not own the base sense.
- lookup-probes: Reader probes covered “host and guest,” “guest-host roles,” “interchanging guest and host,” “guest and host positions,” and “no guest no host.” These are now approved SearchAliases.
- opening-interpretation-verdict: KEEP the ordinary-scene opening and Chan bend. It first names the paired roles in a face-to-face encounter, then shows how records use them to distinguish, exchange, negate, and publicly test positions.
- definition-formula-audit: Direct questions ask whether guest and host exist, how far apart they are, whether their meeting has speech, and how one shout distinguishes them. These object- and relation-testing frames preserve one pair rather than defining rival substances.
- nested-family-audit: The four guest-and-host relations, guest-within-host positions, bent/upright, ruler/minister, and no-guest-no-host story were rechecked. Larger compounds remain labeled family evidence and do not create bare-headword senses.
- modifier-and-provenance-audit: No feedback modifier is at issue. All eleven witnesses were re-read; eight exact-headword anchors and three labeled family witnesses retain named sources and speakers.
- semantic-propagation: Preserve the merged base pair in the fourfold taxonomy, mutual-interchange, positional, bent/upright, and ruler/minister entries. Search aliases should recover the entry from English role, position, interchange, and negation language.
- final-cohort-gate: `run_cohort_gate.py` hardPass=true; exact KWIC 11/11, attribution hard failures 0, public-feedback flags 0, depth/sense hard failures 0, review flags 0, and forbidden-English matches 0. Report: `maintenance/semantic-cohorts/semantic-r001-owner1-binzhu-gate.json`.
