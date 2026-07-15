# Gate 3 Verdict — 祖師西來意 (t_49efe4fed8d4)

VERDICT: PASS

Independent adversarial re-derivation (Gate 3, fresh model). Method: each cited file
tag-stripped (all `<...>` removed, whitespace collapsed) and the KWIC checked as an
EXACT CONTIGUOUS substring; RelPaths checked against `Assets/Data/zen-corpus.json`;
attribution context read around each hit in the source XML.

## Per-sense findings

### Sense 0 — "the meaning of the Patriarch's coming from the West" (multi-source)

1. **KWIC exact + contiguous: PASS (5/5).**
   - `J/J37/J37nB370.xml` lb 0003b01 — 「僧問趙州：『如何是祖師西來意？』州云：『庭前柏樹子。』僧云：『和尚莫將境示人。』」 verbatim contiguous after tag-strip. No ellipsis, no stitching.
   - `X/X73/X73n1447.xml` lb 0446b12 — 「不見水潦和尚問馬大師云：如何是祖師西來意？馬大師欄胸一踏倒，水潦從地起來，忽然大省」 verbatim contiguous.
   - `J/J32/J32nB273.xml` lb 0222a01 — 「香林遠禪師。僧問：「如何是祖師西來意？」遠云：「坐久成勞。」」 verbatim contiguous.
   - `J/J32/J32nB272.xml` lb 0188c22 — 「僧問龍牙：『如何是祖師西來意？』牙云：『待石烏龜解語即向汝道。』」 verbatim contiguous (tail 「解語即向汝道」 read at lb 0188c23; span begins at 0188c22 as cited).
   - `J/J28/J28nB202.xml` lb 0006c03 — 「仰山問溈山云：「如何是祖師西來意？」溈云：「大好燈籠。」」 verbatim contiguous.

2. **Allowlist: PASS.** All 5 RelPaths present in `zen-corpus.json`. All files exist; every cited FromLb string present in its file.

3. **Multi-source: PASS.** Five separate texts, five separate masters (Zhaozhou, Mazu, Xianglin, Longya, Guishan), five materially different answers — clearly ≥2 independent witnesses; not copies of one passage. (Side observation: J32nB272 lb 0188c23 also carries the Xianglin 坐久成勞 exchange, independently corroborating occ2.)

4. **Over-read: none found.** Every master-specific claim in the Explanation (Zhaozhou 庭前柏樹子, Mazu's kick, Xianglin 坐久成勞, Longya's stone tortoise, Guishan 大好燈籠) is attested verbatim IN the cited KWICs, and all attributions are self-attesting inside the quoted Chinese (the master is named in the snippet itself). The superlative "single most common test-question" is a standard characterization and is supported by the Note's raw counts; the raw counts (1736/239) were NOT re-derived here (corpus-wide scan out of scope per spec) but nothing rests on them.

5. **Imported abstraction: none.** The rendering is deflationary and literal ("the meaning of the Patriarch's coming from the West"); the Explanation explicitly warns against reading a mystical essence into it. No smuggled general-Buddhist concept.

6. **Attribution honesty: PASS.** No floating attributions among the curated five; each exchange is attributed by the source text itself.

## Issues (tagged)

None.

## Verified occurrences: 5/5 KWIC confirmed verbatim
