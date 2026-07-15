# GATE3 VERDICT — t_fd1759947989 · 大死

VERDICT: PASS

**Auditor:** Gate 3 (independent adversarial, Claude/Fable) · **Date:** 2026-07-11
**Method:** All KWICs re-derived from raw TEI XML (tag-stripped, whitespace-normalized) with raw-offset mapping back to `<lb>` / `<cb:mulu>`; explanation phrases re-grepped across the 462-file allowlist.

## 1. KWIC integrity — 4/4 PASS (verbatim, contiguous, no ellipsis/stitching)
- K1 `X/X80/X80n1565.xml` — found; lb `ed="X" 0122a23` matches claim exactly (correct X edition, not R138 0189a14).
- K2 `T/T48/T48n2003.xml` — found; lb `0179a04` matches; `cb:mulu = 41` confirms 碧巖錄 case 41 as claimed.
- K3 `T/T47/T47n1998A.xml` — found; lb `0892a21` matches; mulu `示東峯居士` matches AttributionNote.
- K4 `J/J39/J39nB466.xml` — found; lb `0861c13` matches; mulu `與太谷眾居士` matches; full quotation marks 「」 verbatim in file.

## 2. Attribution — PASS
- **K1 (the specific re-check requested):** mulu head = `舒州投子山大同禪師` (Touzi's 五燈會元 section). Immediate context: `一日趙州和尚至桐城縣…州曰。如何是投子。師提起油缾曰。油。油。州問。大死底人。却活時如何。師曰…` — 州 is demonstrably Zhaozhou (named 趙州和尚 lines earlier), 師 is Touzi. Explicit two-speaker exchange → **MasterName=null is CORRECT** per the two-speaker rule, and the AttributionNote's account (Zhaozhou asks, Touzi answers, Touzi kept only in RelatedMasters) is accurate.
- K2: Yuanwu's Biyan Lu commentary on the raised case → null, correct. (T48n2003 title verified 佛果圜悟禪師碧巖錄.)
- K3: Dahui's own 示東峯居士 in 大慧普覺禪師語錄 → MasterName "Dahui Zonggao", correct and in roster.
- K4: J39nB466 title verified 山西柏山楷禪師語錄; his own letter; 柏山楷 confirmed NOT in master-dates.json roster → null with note, correct.

## 3. Allowlist — PASS. All 4 RelPaths present in zen-corpus.json.

## 4. Explanation honesty — PASS (every quoted phrase grep-verified)
- `大死底人。都無佛法道理玄妙得失是非長短` = K2 verbatim.
- `直須絕名利、忘人我，大死一番方堪湊泊` — found EXACT including punctuation in allowlist file `J/J36/J36nB359.xml`.
- `大死一番，不怕不活` — found EXACT in `J/J10/J10nA158.xml` (`秪管大死一番，不怕不活也`).
- `絕後再甦` — 120 allowlist files (incl. K4's own KWIC); "stock" claim justified.
- Anti-quietism guard verified in situ: K3 context is Dahui's polemic (`坐在黑山下鬼窟裏。喚作默而常照。又喚作如大死底人…`); literal 默照 occurs 15x in T47n1998A. The explanation's quietism framing is corpus-grounded, not imported.
- `大死大活` — 2 allowlist files (J26nB178, X68n1319), both in the death-revival sense claimed.

## 5. Multi-source — PASS. Four independent texts/masters (五燈會元 / 碧巖錄-圜悟 / 大慧語錄 / 柏山楷語錄), spanning Song→Ming. `multi-source` justified.

## 6. Nesting / RelatedTerms — PASS
- 大死大活 — genuine (grep-verified, semantically the death-then-revival pairing the sense asserts).
- 絕後再甦 — genuine (appears inside K4's own KWIC; 120-file stock phrase).

## Punch list
None. No defects found. (Cosmetic, non-defect: the last `<head>` before K2 is a front-matter leftover `夾山無礙禪師降魔表`; the governing `cb:mulu`=41 is correct, so nothing to fix.)

Defect count: 0
