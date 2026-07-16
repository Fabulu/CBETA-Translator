# Cohort A evidence pass — 上堂, 示眾, 法嗣 (2026-07-13)

## Scope and disposition

- Entries: 上堂 (`t_4f7bd98ad40f`), 示眾 (`t_1a7e251bda53`), 法嗣 (`t_8a06e7d99b19`).
- Re-read the current guide, including items 10–19, and `ATTRIBUTION_FIX.md` before editing.
- Reconstructed every saved occurrence as a complete address, encounter, embedded case, code procedure, biography, memorial, or catalogue unit. No title or header packet wrote a name automatically.
- Re-tested definitions, sense splits, openings, search aliases, source equality, family relations, and every quoted Chinese string.
- No merge was run.

## Baseline and result

| Metric | Before | After |
|---|---:|---:|
| entries | 3 | 3 |
| occurrences | 34 | 37 |
| named `MasterName` | 5 | 24 |
| reviewed null `MasterName` | 29 | 13 |
| Chinese prose strings | 19 | 18 |
| anchored Chinese strings | 16 | 18 |
| dangling Chinese strings | 3 | 0 |
| source-note failures | 26 | 0 for named rows; reviewed non-personal rows remain auditor-null flags |
| vague prose attributors | 8 | 0 |

The three added occurrence records are supporting family anchors: one 上堂-family anchor containing 小參, and the same 嗣法於 family witness attached to both 法嗣 senses because prose in each sense invokes that related construction. All are marked `EvidenceRole: family`; none counts toward headword depth.

## Complete-case attribution decisions

### 上堂

- Named exact address actors: Yunmen Wenyan, Hongzhi Zhengjue (two rows), Miyun Yuanwu, Yulin Tongxiu, Foyan Qingyuan, Feiyin Tongrong, Yinyuan Longqi, and Yongjue Yuanxian. Linji Yixuan remains the exact actor for the physical-ascent sense.
- High-value veto: `舉前堂隱元首座秉拂上堂` occurs in Feiyin Tongrong's record, but the line explicitly assigns the address to Yinyuan Longqi. The title owner was rejected.
- Foyan Qingyuan owns the hall-address event in the D48/C077 parallel, while the stored opening question is spoken by an unnamed monk. The note preserves both facts rather than assigning the monk's words to Foyan.
- Reviewed nulls: two procedural passages in the *Imperially Revised Baizhang Monastic Code* and one editorial family taxonomy in *Essentials from the Patriarchs' Addresses*. None is personal speech.

### 示眾

- Named exact actors: Yuanwu Keqin (two rows), Huangbo Xiyun, Yuejiang Zhengyin, Luopu Yuanan, Vasumitra, Ruibai Mingxue, Shakyamuni Buddha, Yaoshan Weiyan, and Yangshan Huiji.
- Embedded-case vetoes: Gutting's commentary quotes Luopu; the Mengxi section embeds Yaoshan's garment case; the compilation embeds Yangshan's mirror case. Inline speakers override container owners in all three.
- Reviewed null: the editorial rules section of *Essentials from the Patriarchs' Addresses* is source classification, not a personal utterance.
- Deferred roster spellings, pending the separate roster project: Yuejiang Zhengyin, Vasumitra, Ruibai Mingxue, and Shakyamuni Buddha. The entry records the exact source identity rather than erasing it.

### 法嗣

- Named exact actors: Prajnatara recognizes the heir; Michaka predicts his heir; Chuiwan Guangzhen speaks the Muzhou retrospective; Wuzu Fayan receives and raises the succession document.
- Exact-turn vetoes: Shunzhi Emperor—not Muchen Daomin—asks whose heir Wunian was. An unnamed questioner—not Yexian Guisheng—asks from whom Yexian received succession. Because `MasterName` is a master-link field, the emperor and anonymous questioner remain null while the notes name or classify them exactly.
- Reviewed nulls also include catalogue headings, a memorial narrator, an editorial catalogue note, and a pagoda-inscription family witness. Assigning the nearby lineage master would turn genealogy/source narration into a false personal utterance.
- Deferred roster spellings: Prajnatara, Michaka, and Chuiwan Guangzhen.

## Definition, sense, opening, and search retest

- 上堂: kept `formal teaching-hall address` separate from `ascend the teaching hall`. The first is a completed public institutional event; the second is the physical action. Search aliases now cover natural hall-address and high-seat queries.
- 示眾: kept `public-address format` separate from `physical display to the assembly`. Written verse remains a delivery medium of the first; flower, whisk, garment, and mirror are instances of the second. Search aliases cover address/instruction and show/display/raise phrasing.
- 法嗣: kept `lineage heir` (person) separate from `lineage affiliation` (relation/status). Counted heirs and a succession document make the referent distinction direct. Search aliases cover natural heir/successor and affiliation/succession phrasing.
- All six openings now state the referent and Chan bend before frequency or evidence detail.
- All three `WORK.md` files now carry the item-11/public-feedback ledger, lookup probes, opening verdict, and pairwise sense-target verdict.

## Mechanical verification

- `zc.verify`: **37/37 pass**, with exact stored `FromLb` and `ToLb`; `PYTHONIOENCODING=utf-8` used.
- Source equality: **6/6 senses pass** (`SourceTexts` exactly equals occurrence `RelPath` set).
- `audit_depth_sense.py`: **3 audited, 0 hard failures, 0 review flags**; all three entries have 10+ occurrences and remain multi-sense.
- `audit_public_feedback.py`: **3/3 pass, 0 flags**.
- `audit_attribution.py`: 24 named, 13 null, 18/18 Chinese strings anchored, 0 vague attributors, 0 missing source notes on named rows. It still reports each reviewed null twice (`null_master` plus `note_missing_speaker`) because its schema assumes every occurrence is a personal master utterance.
- Forbidden reader labels: depth gate found none.

## Structural finding

The remaining attribution failures are not all unresolved attribution. They divide into:

1. institutional/editorial/catalogue narration with no personal turn;
2. a named non-master speaker (Shunzhi Emperor);
3. a genuinely unnamed questioner after the complete unit is reconstructed;
4. source narration centered on a named master but not spoken by that master.

Forcing any of these into `MasterName` creates the exact-turn error the current guide forbids. The data model needs an explicit reviewed-null/source-voice state, or separate `ActorName`/`SpeakerName`/`SourceVoice` fields, before the mechanical target can honestly be universal. Until then, the entries preserve null plus an explicit reader-facing note.

## Merge status

Not merged, per assignment.
