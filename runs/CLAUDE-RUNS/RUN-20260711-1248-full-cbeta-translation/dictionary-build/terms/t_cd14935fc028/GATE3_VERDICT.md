# GATE 3 VERDICT — t_cd14935fc028 · 頌古

VERDICT: PASS

Auditor: Gate 3 independent adversarial pass (Claude, Fable 5). Method: tag-stripped verbatim
substring search over the cited TEI files, nearest-preceding-`<lb>` check (primary edition),
allowlist membership check against `Assets/Data/zen-corpus.json`, and grep of every Chinese
phrase quoted in Explanation/Note.

## 1. KWIC integrity — ALL VERBATIM
| Occ | RelPath | KWIC | Result |
|---|---|---|---|
| 1 | T/T48/T48n2003.xml | 雪竇頌古百則。叢林學道詮要也。 | exact contiguous, 1 hit |
| 2 | T/T48/T48n2004.xml | 頌古。猶詩壇之李杜。世謂雪竇有翰林之才。 | exact contiguous, 1 hit |
| 3 | C/C078/C078n1720.xml | 有拈古焉有頌古焉 | exact contiguous, 1 hit |
| 4 | T/T47/T47n1998A.xml | 亦來呈見解。作頌古。 | exact contiguous, 1 hit |

No ellipsis, no stitching, punctuation matches the files byte-for-byte.

## 2. lb anchors — ALL CORRECT
0224b08 (T-ed), 0226c28 (T-ed), 0622b10 (C-ed), 0869a23 (T-ed) — each is the nearest
preceding lb of the KWIC start in the primary edition.

## 3. Allowlist — ALL 4 RelPaths present in zen-corpus.json. No contamination.

## 4. Attribution — CORRECT
- Occ 1: 碧巖錄 postface — context confirms 「佛果圜悟禪師碧巖錄卷第十後序」 immediately precedes.
  MasterName null (editorial voice) is right.
- Occ 2: 從容錄 preface (移剌楚才/耶律楚材 letter-preface context 「評唱天童從容庵錄。寄湛然居士書」).
  Preceding text 「吾宗有雪竇天童。猶孔門之有游夏。二師之」 verified at 0226c27 — the
  AttributionNote's claim is exact. MasterName null right.
- Occ 3: 禪宗頌古聯珠(通集) preface — the same preface later says 「目之曰禪宗頌古聮珠」,
  confirming the text identification. MasterName null right.
- Occ 4: Dahui 普說 — 山僧 first-person voice verified at 0869a02 (「山僧向渠道」), and the
  passage sits in the 普說 fascicles of 大慧語錄 (T47n1998A, 蘊聞編). The 「雲門向他道」 in the
  continuation is Dahui's self-reference (his 雲門菴 period), consistent with the note.
  MasterName = Dahui Zonggao is correct.

## 5. Explanation honesty — ALL QUOTED CHINESE ATTESTED
「有拈古焉有頌古焉」 (C078n1720, 0622b10) · 「二師之頌古。猶詩壇之李杜」 (T48n2004, 0226c27) ·
「叢林學道詮要」 (T48n2003, 0224b08) · 「雪竇頌古百則」 (T48n2003, 3 hits) ·
「呈見解。作頌古」 (T47n1998A, 0869a23). Nothing fabricated. The gloss ("verses on old cases,"
a concrete compositional form/genre) matches attested usage; deflationary; the Fenyang-origin
remark is explicitly fenced as scholarly context not asserted from an occurrence — honest.

## 6. Multi-source — HOLDS. 4 independent texts (碧巖錄, 從容錄, 頌古聯珠, 大慧語錄), editorial
plus live-master usage. `multi-source` is justified.

## 7. Nesting / RelatedTerms — genuine semantic relations (拈古 sibling genre attested in the
same clause; 公案/古則 the object of the verses; 評唱 the lecture layer 「評唱天童從容庵錄」).
No coincidental character-overlap links.

## Punch list (non-blocking observations)
- Note claims "261 allowlist texts" attest 頌古; my raw recount (tag-stripped, note text
  included) finds 265. Within tooling tolerance (note/apparatus text); not a defect, but the
  number should be treated as approximate.

Defects: 0 blocking, 1 informational.
