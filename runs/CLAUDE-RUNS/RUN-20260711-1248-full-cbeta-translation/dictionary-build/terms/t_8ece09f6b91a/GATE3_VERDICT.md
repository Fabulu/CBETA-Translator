# Gate 3 Verdict — 正法眼藏 (t_8ece09f6b91a)

VERDICT: PASS

Verifier: Gate 3 (independent adversarial, fresh model). All evidence re-derived from
`C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` via tag+whitespace-stripped exact-substring
matching with lb anchoring; WORK.md used as context only.

## Special check (per launcher): Dahui's 正法眼藏 collection + Dogen

- **CONFIRMED CLEAN.** The five Occurrences cite only X80n1565, T48n2005, T51n2076, T47n1985 — Dahui's
  正法眼藏 koan collection (X67n1309) and Dogen's Shobogenzo appear ONLY in the sense `Note` as reception
  history, explicitly flagged "noted only as reception, not cited as evidence."
- The Note's factual claim that these "fall outside the Zen-record allowlist" is itself true: I checked
  `Assets/Data/zen-corpus.json` — the X67 entries present are X67n1299/1303/1304/1307; **X67n1309 is NOT
  on the allowlist**, and Dogen's Japanese work is not in the corpus at all. The entry handles this
  exactly as required.

## Per-occurrence findings (sense 1 of 1)

1. **X/X80/X80n1565.xml @ 0031a08** — PASS. KWIC
   `世尊曰。吾有正法眼藏。涅槃妙心。實相無相。微妙法門。不立文字。教外別傳。付囑摩訶迦葉。`
   exact-contiguous immediately after `<lb ed="X" n="0031a08"/>` (X-canon: ed="X" anchor correct).
   Nearest preceding heads: `迦葉佛（賢劫第三尊）` → `釋迦牟尼佛（賢劫第四尊）` (raw 75002/75046) — the WDHY
   opening Buddhas section, as the AttributionNote says. The flower narrative immediately precedes:
   `世尊在靈山會上。拈華示眾。是時眾皆默然。唯迦葉尊者破顏微笑。` Locus classicus confirmed.
2. **T/T48/T48n2005.xml @ 0293c19** — PASS. KWIC
   `正法眼藏作麼生傳。設使迦葉不笑。正法眼藏又作麼生傳。若道正法眼藏有傳授。黃面老子誑謼閭閻。`
   exact-contiguous at `<lb n="0293c19" ed="T"/>`, inside Wumenguan case 6 (`世尊拈花` title precedes;
   `無門曰` introduces the comment). The sequel `若道無傳授。為甚麼獨許迦葉` verifies the two-horn argument
   exactly as the Explanation renders it. Attribution to Wumen correct.
3. **T/T51/T51n2076.xml @ 0208c21** — PASS. KWIC `我今以如來正法眼藏付囑於汝勿令斷絕。` exact-contiguous
   at `<lb n="0208c21" ed="T"/>`. Context: 伏馱蜜多 walks seven steps, is ordained (`尊者尋授具戒。復告之曰`),
   and afterward `爾時尊者佛陀難提。即現神變却復本坐儼然寂滅` — the speaker is Buddhanandi (8th patriarch,
   correct in the traditional numbering) entrusting Buddhamitra, exactly as the AttributionNote states.
   MasterName=null with an explanatory note (Indian ancestor, not in the Chan-master index) is honest handling.
4. **T/T47/T47n1985.xml @ 0506c03** — PASS. The long deathbed KWIC (師臨遷化時據坐云：「吾滅後不得滅却吾
   正法眼藏。」… 言訖端然示寂。) is exact-contiguous BYTE-FOR-BYTE at `<lb n="0506c03" ed="T"/>` — including
   the modern punctuation ：「」？，, which this CBETA Linji-lu file genuinely carries (verified, not an
   editorial artifact). The passage is immediately followed by the memorial notice `師諱義玄，曹州南華人也`,
   confirming 師 = Linji Yixuan; 三聖 (Sansheng) is the interlocutor as claimed.
5. **X/X80/X80n1565.xml @ 0389c05** — PASS. KWIC
   `開堂示眾云。昔日靈山會上。世尊拈華。迦葉微笑。世尊道。吾有正法眼藏。分付摩訶大迦葉。次第流傳。無令斷絕。至于今日。`
   exact-contiguous immediately after `<lb ed="X" n="0389c05"/>`. Nearest preceding heads:
   `楊歧會禪師法嗣` → `舒州白雲守端禪師` (raw 2587917/2587964) — Baiyun Shouduan, a Yangqi heir, as claimed.
   The sermon continues `況諸人分上。各各自有正法眼藏` — the living-lineage application described in the note.

## Checks

- **KWIC exact + contiguous:** 5/5 verbatim, no ellipsis, no stitching, all anchored at the cited lb.
  (The Explanation's `吾有正法眼藏，涅槃妙心…付囑摩訶迦葉` uses an ellipsis, but that is prose gloss, not a
  KWIC — the actual Occurrence carries the full uncut span. Acceptable.)
- **Allowlist:** all 4 RelPaths present in `Assets/Data/zen-corpus.json` (lines 453, 278, 301, 253).
  No contamination.
- **Multi-source:** HOLDS. Four independent texts (WDHY denglu, Wumenguan, Jingde lamp record, Linji yulu),
  and the uses are structurally distinct (origin narrative / commentarial interrogation / patriarch
  handover formula / a master's self-application) — far beyond one copied passage.
- **Over-read:** none. "One corpus-wide sense — the lineage's transmitted dharma-eye" matches what I read;
  no master-uniqueness claim.
- **Imported abstraction:** none — "the treasury of the true dharma eye" is strictly morpheme-literal
  (正法/眼/藏), and the entry even documents the tradition's own deflationary pressure (Wumen) instead of
  mystifying the term.
- **Attribution honesty:** all 5 verified against section heads / narrative frames (details above);
  the null-MasterName for the Indian patriarch is the honest choice, with the reasoning recorded.

## Issues (tagged)

- None blocking. Two cosmetic observations: (a) occurrence #1 sets MasterName "Sakyamuni (World-Honored
  One)" while #3 uses null for a comparable non-Chan-index figure — a consistency choice for the editor,
  not an error (both notes explain themselves); (b) RelatedMasters includes Dahui Zonggao solely on
  reception grounds — defensible navigation given the Note, but flagging for awareness.

## Verified occurrences: 5/5 KWIC confirmed verbatim

PASS — merge as-is.
