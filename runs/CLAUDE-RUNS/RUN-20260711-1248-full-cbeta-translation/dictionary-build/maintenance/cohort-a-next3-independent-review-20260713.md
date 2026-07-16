# Cohort A next-three independent review — 2026-07-13

**Reviewer:** Codex subagent `/root/repair_bird_path`  
**Scope:** read-only semantic and complete-case review of `僧問` (`t_67bff0d0e5d3`), `且道` (`t_cc840e36f2da`), and `良久` (`t_6abcff898d95`) after `maintenance/cohort-a-next3-evidence-20260713.md`. No entry, `WORK.md`, status, manifest, or merged artifact was edited.

## Verdict

| Entry | Verdict | Sense/opening/depth | Exact-actor adjudication | Required action |
|---|---|---|---|---|
| 僧問 | **REVISE** | Pass | The 11 questioners are honestly unnamed, but occurrence 4 incompletely maps the named participants | Preserve all 11 null exact actors through a reviewed-unnamed schema state; revise the Flower Garland case note/prose to name Xuanting as the interjecting answerer as well as Zhiwei as the person questioned |
| 且道 | **PASS** | Pass | 11/11 exact speakers independently confirmed | No lexical revision |
| 良久 | **REVISE** | Pass | 8 rows have a named personal actor, 1 has a genuinely unnamed monk, and 2 are impersonal/narrative intervals rather than actions by the currently assigned master | Mark one duplicate Buddha transmission as support/parallel; represent the Dayang and Huanglong rows as narrative/scene attribution rather than exact personal acts |

**Cohort verdict: REVISE before integration.** All three lexical senses and openings are sound. The defects are occurrence independence, participant mapping, and the inability of the current occurrence schema/auditor to distinguish an honestly unnamed actor from an unattributed quotation or an impersonal narrative interval.

## Mechanical recheck

- `zc_batch.py verify-entries`: **33/33 exact**, zero failures.
- `audit_depth_sense.py`: zero hard failures; all three high-frequency single senses remain semantic-review flags, adjudicated below.
- `audit_attribution.py`: 29 nominal failures. Twenty-four are the expected paired `null_master` / `note_missing_speaker` reports for the twelve honest null rows; five are prose detections of the literal phrase “a monk.” These are audit-model failures, not evidence that a respondent should be put into `MasterName`.
- All 14 retained Chinese prose strings remain anchored according to the preceding evidence pass. This review found no new dangling quotation.

## 1. 僧問 — REVISE

### Lexical result

The one-sense analysis is correct. `僧問` is the subject-plus-verb event formula “a monk asked,” not a noun naming the public-interview genre. Event framing (`因僧問`), introduction (`有僧問`, `時有僧問`), biography transition (`住後，僧問`), and later case-raising (`復舉：僧問`) alter the frame, not the thing denoted. The opening clearly surfaces where Chan bends the ordinary wording: it is a recurrent hinge that publicly places a question before a named respondent and preserves what follows.

Eleven occurrences across nine source paths are sufficient coverage of the distinct framing grammars. The two Linji witnesses are separate live interviews in the same record, not duplicate transmissions. No additional sense is exposed by the harvested cases.

### The eleven anonymous questioners

The prior pass's central claim is confirmed: **11/11 exact questioners remain personally unnamed after the six-rung ladder.** The source can name the respondent, section owner, later raiser, or compiler without naming the monk who uttered `僧問`. Those people must not be substituted into `MasterName`.

| # | Case | Independent ladder result |
|---:|---|---|
| 1 | Zhaozhou dog case | The line and hundreds of parallel citations retain generic `僧問`; headings and titles identify Zhaozhou or later transmitters, never the questioner |
| 2 | Linji, first “great purport” question | The full public assembly in Linji's record says `僧問`; the parallel *Five Houses Recorded Sayings* transmission adds no personal name |
| 3 | Linji, “again there was a monk” | `又有僧問` introduces a second generic questioner; nearby named figures and chief seats are different participants |
| 4 | Flower Garland lecturer | The fullest parallel adds only “a monk from Chang'an who lectured the Flower Garland scripture” (`有長安講華嚴經僧來`), not a personal name |
| 5 | Helin Xuansu, coming from the west | Full biography has `或有僧問`; no parallel or metadata rung names him |
| 6 | Yunfeng, flower at Vulture Peak | Exact witness names Yunfeng as respondent only; the monk remains generic |
| 7 | Baizhang, extraordinary matter | Parallel transmissions consistently have generic `僧問百丈`; the later raiser is not the old questioner |
| 8 | Qingyuan, Luling rice price | Early *Patriarchs' Hall Collection* and later parallels retain a generic monk/student; `學人` in a later version is still not a personal identity |
| 9 | Budai in the street | All located parallels use `有僧問`; no personal name appears |
| 10 | Tianyi after taking up residence | Parallel records retain `住後僧問`; no personal name appears |
| 11 | Baiyan, gate of not-two | The full exact witness says `時有僧問`; no further rung names the questioner |

These are not “unnamed masters” recoverable from the title of their own recorded sayings. They are genuinely anonymous participants in records that do name other participants.

### Required participant correction in occurrence 4

The present entry says that a Flower Garland lecturer questions Oxhead Zhiwei, which is accurate as far as the target event goes, but its AttributionNote calls Zhiwei “the named respondent” and stops there. The complete case is three-party:

1. the unnamed Chang'an Flower Garland lecturer asks about the dependent arising of true nature;
2. Zhiwei (`威` / `五祖`) remains silent for a good while;
3. Xuanting, then standing in attendance, calls to the lecturer and supplies the answer (`時師侍立次乃謂曰…`).

The explicit Xuanting section in *The Record of the Transmission of the Lamp* and *Five Lamps Meeting the Source* establishes this. Revise the note to distinguish **questioner: reviewed-unnamed lecturer; person questioned: Oxhead Zhiwei; interjecting answerer: Xuanting**. Add Xuanting to `RelatedMasters`. Do not populate occurrence `MasterName` with either named man because neither uttered the headword question.

## 2. 且道 — PASS

The single imperative sense “now say!” / “tell me then” holds across direct dialogue, hall addresses, ceremonial capping, and commentary. Noun/verb or genre splits are unwarranted. The opening states both the ordinary command and its Chan deployment as a recurrent demand for an answer before an audience. The search aliases cover natural English formulations.

All exact speakers survive full-context review:

- Yuanwu Keqin: occurrences 1, 2, 3, and 7, in his *Blue Cliff Record* commentary.
- Guanghui Yuanlian: occurrence 4. Although a nearest-head helper can return a stale adjacent heading, parallel texts and the raw section hierarchy explicitly place the address under `汝州廣慧院元璉禪師`.
- Wuzu Fayan: occurrences 5 and 6, in his explicit recorded-address sections.
- Zhaozhou Congshen: occurrence 8, direct speech in his section.
- Guyin Yuncong: occurrence 9, direct hall speech in his biography.
- Ying'an Tanhua: occurrence 10, an explicitly signed address.
- Puhua: occurrence 11, explicitly marked as the speaker returning Linji's question.

The eleven witnesses cover materially distinct continuation grammars and public positions without creating multiple senses. No revision is recommended.

## 3. 良久 — REVISE

### Lexical result

The single temporal sense “a good while” is correct. Silence, waiting, an ensuing shout, a raised fly-whisk, a staff action, and ordinary narrative delay are deployments of the same duration, not separate things. The opening appropriately refuses to assign an unstated spiritual meaning to the interval.

### Independence correction

Occurrences 1 and 2 are two transmissions of the same Buddha/non-Buddhist case, not independent deployments. Retain both if parallel textual evidence is useful, but mark one as **support/parallel** so it does not buy depth. The entry still has ten depth-bearing rows after that correction and therefore continues to satisfy the high-frequency floor.

### Exact-actor inventory

| Rows | Result |
|---|---|
| 1–2 | The Buddha is explicitly the actor who remains for a good while, but the rows are parallel transmissions of one case |
| 3 | Shoushan Xingnian is the actor |
| 4 | Zongxian Minghui is the actor |
| 5 | Yunfeng Wenyue is the actor |
| 6 | `僧良久` makes an unnamed monk the actor; the complete Songshan Junji case and parallel search never name him |
| 7 | Yinyuan Longqi is the actor |
| 8 | Yulin Tongxiu is the actor |
| 9 | Mahakasyapa is the actor |
| 10 | `良久乃召師` is a narrator's elapsed-time bridge before Dayang Jingxuan summons Bai Ma Guixi; Dayang is the following actor, not a grammatical actor of `良久` |
| 11 | `良久無人問` records an interval in which nobody in Huanglong Huinan's assembly asks; Huanglong is the governing hall speaker and leaves afterward, not the personal actor of the headword interval |

Row 6's null is honest and should remain reviewed-unnamed. Rows 10 and 11 should not use a linked `MasterName` in a UI field presented as “who performed/spoke this occurrence.” Their notes already describe the scenes substantially correctly, but the structured assignments overclaim exact actorhood. Represent row 10 as narrated elapsed time with Dayang and Bai Ma Guixi as context participants; represent row 11 as an impersonal/assembly nonresponse within Huanglong's address, with Huanglong as context speaker.

The distinctive Dayang wording occurs in three parallel texts, all with the same subjectless bridge. The Huanglong wording occurs once in his own record and explicitly reads “after a good while, no one asked.” Neither wider context nor a parallel supplies a missing personal actor because these are not anonymous-person cases.

## Schema recommendation

Do not weaken Rule 10 and do not fill `MasterName` with a respondent, addressee, biography owner, or following actor. Add an occurrence-level participant and evidence-voice model while preserving `MasterName` for backward compatibility:

```json
{
  "MasterName": null,
  "Attribution": {
    "Mode": "direct-speech",
    "Status": "reviewed-unnamed",
    "ActorRole": "questioner",
    "ActorLabel": "an unnamed monk"
  },
  "Participants": [
    {"Role": "respondent", "MasterName": "Zhaozhou Congshen"}
  ]
}
```

Recommended controlled values:

- `Attribution.Mode`: `direct-speech`, `action`, or `narration`.
- `Attribution.Status`: `named`, `reviewed-unnamed`, or `impersonal`.
- `ActorRole`: e.g. `questioner`, `speaker`, `pausing participant`, `assembly`, or `narrator`.
- `Participants`: every independently nameable contextual person with an exact role (`respondent`, `person-questioned`, `interjecting-answerer`, `later-raiser`, `governing-address-speaker`, `addressee`).

Audit behavior should be strict:

1. A null `MasterName` passes only with `Status: reviewed-unnamed` or `Status: impersonal`, a nonempty role/label, and an AttributionNote documenting the ladder or the impersonal grammar.
2. `reviewed-unnamed` is for a real but unnamed participant; `impersonal` is for narrator-governed duration or scene state. They are not interchangeable.
3. Named contextual participants must never satisfy the exact-actor requirement, but the website may link them under their explicit roles.
4. A parallel transmission may be displayed and cited but must be marked `EvidenceRole: support` (or equivalent) so depth audits count the underlying case once.
5. The website should render “Questioner: an unnamed monk (identity not preserved)” without a dead link, then render named respondents/other participants as role-labelled master links.

This addition resolves all twelve honest nulls in this cohort without creating false clickable attributions and also handles the two impersonal `良久` rows that the present binary `MasterName` field cannot express.
