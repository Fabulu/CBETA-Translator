# WORK — 開悟 (t_33d49f4710be) · batch b003

## Gloss target
"to open awakening / become enlightened"

## Evidence (Zen-scoped allowlist only)
- 566 raw occurrences across 147 allowlist texts. Top: X78n1556 (25), X80n1565 (23), B25n0145 (21), X85n1593 (21), T48n2016 (18), X82n1571 (18).

## Sense analysis
ONE corpus-wide sense. 開 'open' + 悟 'awaken' = the event of realization opening.
- Dominant use is **intransitive**: 豁然開悟 / 心忽開悟 / 遇百丈開悟 / 其僧於是開悟 — a person opens into awakening, usually sudden, often triggered by a teacher's word/gesture.
- A **transitive-causative valence** ('open others to awakening': 令汝等開悟, 開悟群生, 開悟法界眾生) appears esp. in 宗鏡錄 (T48n2016) and the Bodhidharma cycle. SAME lexical meaning, only the subject differs — so it is documented within the one sense, not split off.
- No master bends the word → single null sense is honest. Not master-specific.

## Multi-source gate
PASS (multi-source): curated witnesses in 3 independent texts — 景德傳燈錄 (T51n2076), 五燈會元 (X80n1565), 宗鏡錄 (T48n2016).

## Distinctions recorded in Explanation
- 大悟 (great awakening, stock 言下大悟) and 頓悟 (sudden awakening) name the same event under different emphasis — separate entries.
- 見性成佛 names the content/result. Yongming's 若開悟時，不隔剎那，便成佛果 anchors the non-gradual, deflationary reading.

## Curated occurrences (5; 4 curated + 1 huatou/false)
1. T51n2076 0268a10 — 古靈神贊 遇百丈開悟 (narration, null)
2. X80n1565 0066a12 — Aṅgulimāla 心忽開悟 (ed=X lb, null)
3. T48n2016 0554a19 — Yongming: 若開悟時…便成佛果 (Yongming Yanshou)
4. X80n1565 0070b03 — Bodhidharma 令汝等開悟 (causative valence, null)
5. T51n2076 0324c09 — cicada gesture 其僧於是開悟 (curated=false)

## X-canon note
X80n1565 lbs use ed="X" (verified). The co-located ed="R138" reprint numbers were explicitly NOT used (tool bug caught and fixed mid-research).

## Verification
verify.py: 5/5 OK — every KWIC exact-contiguous (tag-stripped, whitespace-collapsed), all RelPaths in allowlist, all FromLb match nearest preceding ed=X/T lb, term present in every KWIC. JSON valid.

## GATE 2 (Claude adversarial verify-and-repair)
- Re-derived all 5 KWICs by targeted grep of the cited file: 5/5 EXACT contiguous, zero ellipses.
- FromLb re-checked against nearest preceding lb PER EDITION: all 5 use the primary ed (X/T) number correctly; the co-located ed="R138" X-canon reprint numbers were NOT used. OK.
- Allowlist: all 5 RelPaths in zen-corpus.json. Zero contamination.
- Attribution: fixed occ4 note — passage is 馬祖道一 (Mazu) addressing the assembly and DESCRIBING Bodhidharma, NOT a "Bodhidharma cycle." MasterName stays null (correct). occ3 Yongming = expository voice of 宗鏡錄 author, OK. Others null (narration/two-speaker), OK.
- UNVERIFIED-CLAIM REMOVED: 開悟群生 (cited in Explanation + Note) has 0 occurrences in the Zen allowlist (18 in the full canon — non-Zen only). Replaced with the attested Zen collocation 開悟眾生 (5 hits / 4 allowlist texts). 令汝等開悟 (occ4) and 開悟法界眾生 (T48n2016) also confirmed attested.
- Multi-source gate: 3 independent texts (T51n2076 / X80n1565 / T48n2016) — PASS. Over-read check: reading kept literal ("open into awakening"), no imported mystical abstraction. RelatedTerms genuine.
- STATUS → verified.
