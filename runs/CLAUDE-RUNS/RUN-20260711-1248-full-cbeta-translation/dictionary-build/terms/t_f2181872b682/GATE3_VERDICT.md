# Gate 3 Verdict — 轉語 (t_f2181872b682)

VERDICT: PASS

Verifier: Gate 3 (independent adversarial, fresh model). All evidence re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag+whitespace-stripped exact-substring
matching with lb anchoring; WORK.md used as context only.

## Per-occurrence findings (sense 1 of 1)

1. **X/X80/X80n1565.xml @ 0071c04** — PASS. KWIC
   `今請和尚代一轉語。貴脫野狐身。師曰。汝問。老人曰。大修行人還落因果也無。師曰。不昧因果。`
   exact-contiguous immediately after `<lb ed="X" n="0071c04"/>` (X-canon: ed="X" anchor correct).
   Nearest preceding heads: `馬祖一禪師法嗣` → `洪州百丈山懷海禪師` (raw 365840/365888), so 師 = Baizhang
   as claimed. The preceding narrative verifies the whole fox setup verbatim: `某對云。不落因果。遂五百生墮野狐身`,
   and the sequel `老人於言下大悟。作禮曰。某已脫野狐身` confirms "frees him."
2. **T/T48/T48n2005.xml @ 0294c17** — PASS. KWIC
   `無門曰。且道。趙州頂草鞋意作麼生。若向者裏下得一轉語。便見南泉令不虛行。` exact-contiguous at
   `<lb n="0294c17" ed="T"/>`; the case title `南泉斬猫` and 趙州-sandals narrative immediately precede —
   Wumenguan case 14, 無門曰 self-attributes to Wumen. Correct.
3. **T/T48/T48n2003.xml @ 0154b03** — PASS. KWIC `他日老僧忌辰只舉此三轉語。報恩足矣。` exact-contiguous
   at `<lb n="0154b03" ed="T"/>`, directly preceded by `雲門云。` — the words are Yunmen's, as the entry
   says. The preceding commentary verifies the Baling claims verbatim: `更不作法嗣書。只將三轉語上雲門`
   ("in place of a dharma-heir letter") and the sequel `依雲門之囑。只舉此三轉語` confirms the memorial-day vow.
   AttributionNote honestly notes the 三轉語 themselves are Baling's. (Blue Cliff Record commentary.)
4. **T/T48/T48n2003.xml @ 0216b05** — PASS. KWIC
   `雪竇復云。若要清風再復頭角重生。請禪客各下一轉語。問云。扇子既破。還我犀牛兒來。` exact-contiguous at
   `<lb n="0216b05" ed="T"/>`; surrounding text is the 犀牛扇子 (rhinoceros-fan) case with the four elders'
   answers — BCR case 91; 雪竇復云 self-attributes to Xuedou. Correct.
5. **X/X80/X80n1565.xml @ 0116a01** — PASS. KWIC `今請闍黎別下一轉語。若愜老僧意。` exact-contiguous
   immediately after `<lb ed="X" n="0116a01"/>`. Nearest preceding head: `鄂州百巖明哲禪師` (raw 674797/674844),
   confirming 師 = Baiyan Mingzhe. The sequel `便開粥相伴過夏` verifies the "share the summer's gruel" detail,
   and the preceding `昨日老僧對闍黎一轉語不相契。一夜不安` confirms the live-examination framing.
   (Minor, non-blocking: the addressee is one of two visiting 上座 — "senior monk" would be more precise
   than the note's "head-monk," but the attribution of the words to Baiyan is correct.)

## Checks

- **KWIC exact + contiguous:** 5/5 verbatim, no ellipsis, no stitching, all anchored at the cited lb.
- **Allowlist:** all 3 RelPaths (X80n1565, T48n2005, T48n2003) present in `Assets/Data/zen-corpus.json`
  (lines 453, 278, 276). No contamination.
- **Multi-source:** HOLDS. Three independent texts (denglu / Wumenguan / BCR), five distinct masters
  (Baizhang, Wumen, Yunmen[/Baling], Xuedou, Baiyan Mingzhe), and completely unrelated cases — no shared
  passage doing double duty.
- **Explanation phrase claims adversarially checked against the corpus** (fixed-string counts across the
  462-text allowlisted corpus): 下一轉語 = 276 hits, 代一轉語 = 148, 道一轉語 = 22, 著一轉語 = 16 — all
  attested. Wumen's "repeatedly challenges": 下得一轉語 occurs 2x in T48n2005 itself and 一轉語 6x — the
  claim holds inside the Wumenguan alone.
- **Over-read:** none — "one corpus-wide technical sense, shared across houses; no single master owns it"
  is exactly what the evidence shows.
- **Imported abstraction:** none — "a turning word" is literal; the explanation is deflationary
  ("not magical speech but the exact word that pivots the checkpoint").
- **Attribution honesty:** all verified against section heads / self-attributing formulas; the layered
  Yunmen-quoting-within-BCR-commentary case (#3) is handled honestly in the AttributionNote.

## Issues (tagged)

- None blocking. (Cosmetic only: "head-monk" → "senior monk (上座)" in occurrence #5's AttributionNote;
  does not affect attribution or evidence.)

## Verified occurrences: 5/5 KWIC confirmed verbatim

PASS — merge as-is.
