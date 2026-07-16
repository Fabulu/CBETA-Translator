# Agent depth repairs — first 128 entries

Date: 2026-07-12  
Scope: the 8 high-priority and 5 medium-priority findings in `DEPTH_AUDIT_128.md`  
Result: complete; no merge performed

## Edited entries

| Priority | ID | Term | Repair | Final senses / occurrences |
|---|---|---|---|---:|
| H1 | `t_6f47a97d45b0` | 序 | Rebuilt the incomplete one-sense inventory as three corpus-grounded senses: Chan-institutional rank/division first, bibliographic preface, and ordinary sequence/order. The noisy one-graph count is no longer presented as a sense count. | 3 / 7 |
| H2 | `t_c13928184189` | 見性 | Added Tiantai Yunju Zhi's direct lamp-record definition and noted its parallel recension spread. | 1 / 6 |
| H3 | `t_1a7e251bda53` | 示眾 | Added Yuejiang Zhengyin's explicit small-gathering/address-to-assembly comparison and equivalence. | 1 / 5 |
| H4 | `t_16140def874d` | 主人公 | Added the mind/oneself/master-in-charge equivalence. Corrected the audit's attribution: X70n1403 is Tianru Weize's Recorded Sayings (天如惟則禪師語錄), not Hanyue's. | 1 / 6 |
| H5 | `t_c1af3ecba987` | 機鋒 | Added Dahui's corrective witness, which first applies “swift pivotal edge” and then calls it identity-consciousness playing tricks. Removed the article's unsupported trigger-point interpretation while preserving its attested collocations and deployment inventory. | 1 / 6 |
| H6 | `t_1d3706324b0c` | 打成一片 | Added the Blue Cliff Record's one-color-side equivalence and Zhongfeng Mingben's explicit rejection of the silver-mountain/iron-wall equation. | 1 / 8 |
| H7 | `t_326be1e9c98a` | 枯木 | Added the “withered-tree assembly” institutional epithet and distinguished it from withered-tree hall, withered-tree Chan, and the base image. | 1 / 5 |
| H8 | `t_d11d5f0c78a5` | 以心傳心 | Added the Chan Gate Treasury Record's explicit attribution to Bodhidharma as that text's historical claim, including Master Ke's question and the recorded answer. | 1 / 6 |
| M1 | `t_970c3f191929` | 正法眼 | Added the standalone “what is the true Dharma eye?” / “a broken sand-pot” case and distinguished it from the longer treasury compound. | 1 / 6 |
| M2 | `t_81147ad4e8bf` | 四料揀 | Added Dahui's compact formula naming the requested public case “the Four Selections,” without projecting the later label into Linji's own record. | 1 / 7 |
| M3 | `t_6edb551acb53` | 知解 | Added the explicit “intellectual understanding not digested” formula and the text's own food/poison comparison. | 1 / 7 |
| M4 | `t_fd1759947989` | 大死 | Added the late preface's equivalence between passing the heavy barrier and greatly dying/greatly living, explicitly weighted as a single provisional witness inside the broader corpus-wide article. | 1 / 5 |
| M5 | `t_87cc840b8f33` | 拄杖子 | Independently verified and added a positive/negative metalinguistic pair: “only call it a staff” and “must not call it a staff / must not fail to call it a staff.” | 1 / 6 |

## Evidence and validation

- Added 18 curated occurrences; the 13 articles now contain 80 occurrences across 15 senses.
- Re-verified every existing and added occurrence with `zc.verify`: 80/80 returned `ok == True`.
- Every saved `FromLb` and `ToLb` exactly matches the verifier's primary-edition anchors.
- All cited paths are allowlisted because `zc.verify` rejects non-allowlist paths.
- JSON and required schema fields passed for all 13 entries.
- Status and validation enums passed.
- All 21 distinct master-link values used by these entries are valid roster names; zero missing roster names.
- Targeted project conformance scan found zero imported-framing flags and zero English-first/bare-Chinese violations.
- No `WORK.md`, `STATUS`, `MANIFEST.jsonl`, guide, wave-plan, termbase, or translation file was changed.
- The merge script was not run.

