# Next-500 QA A — lane-R distinctness audit

Read-only QA of `NEXT500_TERMS.md` and `NEXT500_BUILD_PLAN.md`, with semantic emphasis on the 140 lane-R selections. No plan, entry, status, manifest, or merge file was changed.

## Structural result

- `NEXT500_TERMS.md` has exactly **500** parsed rows, ranks 1–500, **500 unique IDs**, and **500 unique headwords**.
- Lane distribution is exactly **A 180 / B 180 / R 140**.
- Every ID is exactly `t_` plus the first twelve hexadecimal characters of SHA-256 of its headword: **500/500 pass**.
- `NEXT500_BUILD_PLAN.md` has exactly **500** term rows, **500 unique IDs**, and **500 unique headwords**. Its complete `(ID, headword, hits, files)` set equals the terms file's set: no omission, addition, or count drift.
- I rescanned all **462 allowlisted files** using the same normalization as `zc.py`. All **500/500** advertised hit/file pairs equal fresh concordance counts; there are **zero count mismatches**.
- Exact collision checks found zero Next-500 ID or headword collisions with all 616 currently present `terms/*/entry.v2.json` files (606 done plus 10 draft), zero collisions with the 108 rows of `REQUESTED_BUILD_PLAN.md`, and zero headword collisions with the 100 unique candidates in `NEXT100_SAYINGS_CANDIDATES.md`. A direct bold-headword check of `REQUESTED_TERMS.md` likewise found zero exact collision.

The structural queue is therefore sound. The defect is semantic duplication in lane R: automatic harvesting of `RelatedTerms` promoted several spelling variants, object-expanded phrases, and already-covered family members into separate articles.

## Lane-R rejects and one-for-one replacements

The following **27** lane-R selections should be replaced. Counts for replacements are the allowlist-scoped `zc` counts recorded in `NEXT500_RELATED_POOL.tsv`; all proposed replacements were checked against the full selected set, current entries, the requested build plan, and NEXT100 and are not already selected.

| Reject | Why it fails queue-level distinctness | Replacement from related pool |
|---|---|---|
| `示眾云` (3,928/299) | `云` merely adds the speech marker to existing `示眾`; the completed article already documents `示眾云/曰` as forms of the public address. Selecting both speech-graph forms compounds the duplication. | `未審` (6,176/364) — stock public-interview question preface |
| `示眾曰` (1,073/115) | Same family defect as `示眾云`; `曰` versus `云` is a reading/form choice, not a different thing. | `商量` (4,330/361) — recurrent discussion/adjudication of sayings and cases |
| `良久云` (1,403/169) | Graphic/speech-verb variant of existing `良久曰`, while existing `良久` already owns the pause family. | `道得` (3,477/341) — recurrent ability/demand to say it in an interview |
| `百丈清規` (50/17) | Existing `清規` explicitly assigns generic references to Baizhang's code and its title-bearing witnesses to that article's code/book sense audit. This is a family member, not a new referent. | `因果` (1,534/243) — keystone fox/precept-family term with explicit Chan assertion and denial controls |
| `便棒` (832/221) | A narrative segmentation accident: `便` “then” + the already split object/blow word `棒`. The source lead itself compares it to existing `便打`; no stable new lexical object is established. | `業識` (993/239) — high-frequency named family requiring the already mandated karma controls |
| `坐臥經行` (125/74) | A four-posture/list form better handled with retained `行住坐臥` (746/230) and existing `經行`; it does not name a second activity. | `喫飯` (3,049/356) — concrete everyday action repeatedly used in public answers and ordinary-function language |
| `二時粥飯` (117/78) | Existing `粥飯` explicitly treats “two gruel-and-rice meals” as an institutional family deployment of the same meal system. | `闍黎` (2,900/274) — recurrent Chan address/title in interviews |
| `僧堂前` (488/155) | Pure locative expansion of existing `僧堂`; the completed entry already inventories its front, inside, door, bell, and places as parts of the same building. | `薦得` (2,012/279) — recurrent public-case uptake/recognition formula |
| `如何是和尚家風` (819/113) | Existing `家風` already anchors and explains this exact interview question. It is an occurrence family, not a new lexical object. | `法語` (1,492/278) — a Chan discourse/document genre with observable institutional use |
| `德山棒臨濟喝` (27/10) | Coordinated citation of two existing articles, `德山棒` and `臨濟喝`; it adds no third referent. | `曹洞` (871/197) — independently attested Chan house name |
| `大事因緣` (699/204) | Shortened form of existing `一大事因緣`, not a different thing. | `衲僧家` (859/201) — the recurrent assembly/household constituency of patch-robed monks |
| `具一隻眼` (381/141) | Predicate expansion “possess” + existing `一隻眼`; selected `隻眼` also covers the noun family. | `提綱` (844/137) — observable teaching-seat act of raising the guiding line |
| `顧視大眾` (257/82) | Object-expanded occurrence of existing `顧視`; the completed entry already defines looking around at the assembly as its teaching-seat deployment. | `臨機` (760/236) — encounter-position term repeatedly governing responses and great function |
| `卓一下` (1,740/187) | Elliptical count/action frame whose omitted object is supplied by existing `卓拄杖`; it is not a stable object independent of what is struck/planted. | `投機` (725/204) — attested meeting/accord at the encounter, not generic machinery |
| `門庭施設` (180/92) | Existing `門庭` already anchors this exact compound and contrasts the outer arrangements with `堂奧` and the upward road. | `堂奧` (298/112) — the independently attested inner-hall side of that explicit contrast |
| `一句合頭語` (87/71) | Classifier expansion of existing `合頭語`: “one fitting phrase” is an occurrence of the same phrase-type, not another thing. | `祖師意` (694/192) — stable public-question object and controlled shorter family of `祖師西來意` |
| `臨濟三玄三要` (36/29) | Master-name prefix on existing `三玄三要`; the configuration does not change referent when attributed to Linji. | `第二句` (563/151) — independently contrasted technical position alongside selected `第一句` |
| `涅槃妙心` (323/152) | A segment of the transmission formula already treated by existing `正法眼藏` and `拈華微笑`; no independent referent is established by the lane lead. | `祖印` (542/170) — recurrent lineage/public-authority object with its own grammar |
| `拈花微笑` (163/102) | Exact graphic-family duplicate of existing `拈華微笑` (`花/華`), prohibited by the different-readings rule. | `頭首` (514/99) — specific monastery officer class distinct from the individual offices queued elsewhere |
| `君臣五位` (49/31) | Word-order duplicate of retained `五位君臣` (218/108), additionally covered by existing `五位` and `君臣`. | `正位` (393/109) — independently attested Caodong positional term |
| `趙州喫茶` (125/71) | Same Zhaozhou tea case already represented by existing `喫茶去` and selected `趙州茶` (318/161); this is navigational case-label variation. | `參問` (263/89) — explicit consult/question action in the public-interview family |
| `萬象森羅` (730/231) | Word-order variant of existing `森羅萬象`, exactly the kind of different reading that must remain one entry. | `心要` (477/131) — independently named essential/recorded object in Chan discourse |
| `金剛王寶劒` (103/37) | Exact orthographic duplicate of existing `金剛王寶劍` (`劒/劍`). | `擬心` (396/132) — recurrent attempted mental approach in encounter warnings |
| `破顏微笑` (173/118) | Alternate wording for the same flower-sermon smile already owned by `拈華微笑` and `拈花`; it is not another event or thing. | `豐干` (393/127) — non-master invoked figure in the Hanshan–Shide family |
| `拈花示眾` (118/80) | Clause-level retelling of the event already owned by existing `拈花`; adding “showed the assembly” does not create a second event. | `啐啄` (357/93) — independently used hatching-response pair, shorter than but not identical to `啐啄同時` |
| `大地山河` (562/211) | Word-order variant of existing `山河大地`; same totality, different reading order. | `宗匠` (351/154) — recurrent institutional appraisal/title, distinct from a roster identity |
| `衲僧巴鼻` (398/150) | Compositional family witness for selected base `巴鼻` (1,244/264) plus existing `衲僧`; the base article should anchor this compound. | `道中人` (330/61) — stable “person within the Way” interview/appraisal expression |

After these substitutions the queue would remain exactly 500 terms with lane R still at 140, while removing the clearest queue-level duplicates.

## Retained lane-R terms that need explicit build-time controls

The other **113** lane-R selections are provisionally retainable, not pre-approved interpretations. Several are close family expansions but do name a distinguishable object, fixed case, office, or technical position. Their authors should record the contrast explicitly:

- Keep `洗鉢盂` separate from existing complete command `洗鉢盂去`, because the completed command article itself says the shorter washing action is distinct.
- Keep `鉢盂` separate from `衣鉢`: eating bowl versus succession token.
- Keep `堂頭和尚` as an institutional title, not merely any `和尚`; keep direct address within the `和尚` article.
- Keep `三玄` and `三要` only if independent questions/enumerations anchor each component; do not use `臨濟三玄三要` as a third copy of the combined configuration.
- Retain only canonical `五位君臣`; do not reintroduce `君臣五位` as another reading.
- `舍利` must subtract/filter `舍利弗` substring hits and split relic/object uses only where genuinely different things are attested.
- `護生`, `殺生`, `大戒`, and `無生法忍` remain in scope under the precepts carve-out, but their entries need Chan-record deployments rather than generic Buddhist definitions.
- `十方世界`, `微塵`, `清淨法身`, `心地法門`, and `接物利生` are broad Buddhist/Chinese strings. Retention is conditional on anchoring observable master deployment across sources; raw frequency alone is insufficient.
- Fixed compounds such as `世尊良久`, `頂門正眼`, `佛向上事`, `法身向上事`, `十方世界是全身`, and `體露金風` should state exactly what makes the compound a named case/technical object rather than merely a longer occurrence of their base article.

## Verdict

**Structural QA: pass. Semantic lane-R QA: fail pending 27 substitutions.** The remaining 113 lane-R selections are suitable for full §5 research with the controls above; the 27 rejects should not be drafted as separate entries.
