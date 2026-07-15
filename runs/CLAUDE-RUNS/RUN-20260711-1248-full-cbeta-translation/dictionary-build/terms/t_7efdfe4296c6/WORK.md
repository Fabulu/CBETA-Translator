# WORK — 父母未生前 (t_7efdfe4296c6) · batch b003

## Gloss target
"before your father and mother were born" — the original/pre-natal state pointer.

## Evidence (Zen-scoped allowlist only)
- 638 raw occurrences across 185 allowlist texts. Top: X82n1571 (52), J36nB369 (19), J26nB177 (17), J34nB311 (16).

## Sense analysis
ONE corpus-wide sense: a stock huatou clause naming the time before one's birth/individuation, used to ask after the original self/face. Dominant form is the pairing with 本來面目 → 父母未生前本來面目; also 那箇是你本來面目 / bare 畢竟是什麼. Uniform across the corpus; no master bends it → single null sense.

## Deflationary reading (anti-fakeout)
Not an assertion of a pre-existent soul. It is a concrete "before you existed" framing that throws the student off the born, named, conditioned self — proven by the surname pun (萬居士: 父母未生前姓甚麼) and by awakening-verses that answer it with 本來無一物 ("originally not a single thing"), not with a metaphysical essence.

## Nested / cross-ref (§5b)
父母未生前 is a constituent of the koan 父母未生前本來面目 (not itself a separate entry). Genuine semantic partner 本來面目 (t_1c7d25824f85, done) placed FIRST in RelatedTerms — noted its koan form as instructed. 見性, 主人公 added as looser relations.

## Multi-source gate
PASS (multi-source): 3 independent curated texts — 五燈全書 (X82n1571), 天童密雲禪師語錄-family Jiaxing 語錄 (J26nB177), 幻有正傳/圓悟-line Jiaxing 語錄 (J25nB171).

## Curated occurrences (5; 4 curated + 1 huatou/false)
1. X82n1571 0217a01 — 慧海智 asks disciple: 父母未生前，那箇是你本來面目 (koan pairing; ed=X lb, null)
2. J26nB177 0029a07 — 萬居士: 未審父母未生前姓甚麼 (literal before-birth surname pun, null)
3. J25nB171 0568c27 — 印乾 verse: 父母未生前，本來無一物 (awakening-verse opening, null)
4. X82n1571 0152c06 — 上堂: 父母未生前，畢竟是什麼 (bare pointer; ed=X lb, null)
5. J25nB171 0526b15 — 幻有正傳 慣教人參如何是我父母未生前本來面目 (formal huatou; curated=false)

## Speaker verification
All occurrences are questions/verses/narration (two-speaker or student voice) → MasterName=null throughout, per the null-for-quoted rule. RelatedMasters left empty (慧海智 / 幻有正傳 not confirmed canonical names).

## X-canon note
X82n1571 FromLb values use ed="X" (verified), not ed="R" reprint. One off-by-one (b14→b15) on occ 5 caught by verify.py and corrected.

## Verification
verify.py: 5/5 OK — KWICs exact-contiguous, allowlist-clean, FromLb matches, term present. JSON valid.

## GATE 2 (Claude adversarial verify-and-repair)
- 5/5 KWICs re-derived by targeted grep of the cited file: all EXACT contiguous, zero ellipses.
- FromLb per-edition check: X82n1571 occ1/occ4 use ed="X" (0217a01 / 0152c06) correctly, NOT the co-located ed="R141" reprint. J occs use ed="J". All match nearest preceding lb.
- Allowlist: all RelPaths (X82n1571, J26nB177, J25nB171 + SourceTexts J37nB399, X84n1583) in zen-corpus.json. Zero contamination.
- Attribution confirmed against source: occ1 慧海智 questioning his disciple (narrated 智問, null OK); occ2 萬居士 surname pun in two-speaker exchange (null OK); occ3 印乾 awakening-verse (null OK); occ4 上堂 pointer, speaker not resolvable at head (null OK); occ5 幻有正傳 narration (null OK).
- Explanation quotes grep-verified: 父母未生前本來面目 (92 hits / 57 texts — "dominant form" claim holds), 那箇是你本來面目 / 畢竟是什麼 / 本來無一物 / surname pun all attested. Reading kept literal ("before you existed"), no pre-existent-soul over-read.
- Multi-source PASS (X82n1571 / J26nB177 / J25nB171). RelatedTerms (本來面目, 見性, 主人公) genuine, no coincidental prefixes.
- STATUS → verified. No repairs needed.
