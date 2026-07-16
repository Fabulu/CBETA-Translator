# Reviewed unnamed exact actors: minimal schema, audit, and website proposal

Date: 2026-07-13

Status: design only. No entries, audit scripts, models, merge code, or website files were changed.

## Problem proved by the 僧問 cohort

`maintenance/cohort-a-next3-evidence-20260713.md` establishes a legitimate state that schema v2 and `audit_attribution.py` cannot currently represent:

- In all eleven curated 僧問 cases, the exact actor encoded by the headword is the questioning monk.
- Each source calls him only `僧`; the complete six-rung attribution ladder does not recover a personal name.
- The respondent is usually named and valuable—Zhaozhou Congshen, Linji Yixuan, Oxhead Zhiwei, and others—but the respondent did not utter the headword turn.
- One 良久 control has the same structure: `僧良久` makes an unnamed monk the actor of the pause; Songshan Junji speaks only afterward.
- Putting the respondent into `MasterName` would create a false speaker link. Leaving `MasterName: null` is honest, but the current gate treats an exhausted and reviewed null exactly like unfinished attribution.

The data model therefore needs three distinguishable states:

1. exact actor named;
2. exact actor genuinely unnamed after all six rungs;
3. attribution unresolved or not yet reviewed.

Only states 1 and 2 may pass. State 3 remains a hard failure.

## Recommended additive schema

Keep `MasterName` with its current, strict meaning: **the roster-exact name of the exact speaker or actor of the stored occurrence**. Do not reinterpret it as “important master in this case.”

Add two optional occurrence fields:

```json
{
  "MasterName": null,
  "ActorAttribution": {
    "Status": "reviewed-unnamed",
    "Kind": "monk",
    "RungsChecked": [
      "line",
      "expanded-context",
      "section-header",
      "book-title",
      "tei-header",
      "parallel-passage"
    ],
    "ReviewedBy": "Codex /root/feedback_lexicography",
    "ReviewedUtc": "2026-07-13T00:00:00Z"
  },
  "ContextMasters": [
    {
      "MasterName": "Zhaozhou Congshen",
      "Roles": ["respondent"]
    },
    {
      "MasterName": "Wumen Huikai",
      "Roles": ["record-owner", "later-commentator"]
    }
  ]
}
```

### Field definitions

`ActorAttribution` is present only for the exceptional reviewed-unnamed state.

- `Status`: initially one allowed value, `reviewed-unnamed`. Do not add an accepted `unresolved` value; absence of both a name and this reviewed state is already unresolved and must fail.
- `Kind`: controlled display category for the exact actor. Initial values: `monk`, `person`, `group`. The current twelve cases use `monk`. This field does not claim a personal identity.
- `RungsChecked`: the six canonical machine-readable rung identifiers, in guide order. A free-text “we checked everything” is insufficient.
- `ReviewedBy`: required provenance string identifying the human or agent review responsible for the exception.
- `ReviewedUtc`: required ISO-8601 UTC timestamp.

`ContextMasters` preserves named people in the occurrence without confusing them with the exact actor.

- `MasterName`: roster-exact `names[0]`, suitable for `#/master/{name}`.
- `Roles`: nonempty controlled array. Initial roles: `respondent`, `record-owner`, `section-subject`, `later-raiser`, `later-commentator`, `addressee`, `person-discussed`, `quoted-speaker`.
- A person may carry multiple roles in one object. Do not duplicate the same name merely to add another role.
- These are context links only. No `ContextMasters` member may be promoted into occurrence `MasterName` unless a complete-case review separately proves that person performed the exact stored action.

This is additive schema v2, not a semantic schema rewrite. Old consumers can ignore the new optional fields. Producers that deserialize and rewrite v2 must nevertheless add matching model properties first, or they may silently strip the fields on round-trip.

## Why this is the minimal safe design

- A boolean such as `UnnamedActor: true` is too weak: it cannot prove the ladder was completed, distinguish a monk from another anonymous actor, or preserve review provenance.
- A prose-only convention in `AttributionNote` is not auditable and is exactly why the current gate cannot distinguish honest nulls from unfinished work.
- Reusing sense-level `RelatedMasters` is too coarse: it does not say which occurrence a master belongs to or whether he is respondent, source owner, or later commentator.
- Replacing `MasterName` with a broad “case master” field would destroy the exact-turn invariant and preserve the original defect under a new name.
- A fully generalized participant-and-turn graph would be powerful but unnecessary for this repair. `ActorAttribution` plus role-labelled `ContextMasters` captures the proven requirement without restructuring every named occurrence.

## Hard audit gates

### 1. Exact-actor state must be exclusive and complete

For every occurrence, exactly one passing branch is allowed:

- **Named branch:** nonempty `MasterName`, roster-exact; `ActorAttribution` absent.
- **Reviewed-unnamed branch:** `MasterName` is null/absent; `ActorAttribution.Status == "reviewed-unnamed"`; every required review field valid.

Neither branch means unresolved and is a hard failure. Both branches at once is a contradiction and a hard failure.

Suggested failure kinds:

- `unresolved_exact_actor`
- `named_and_reviewed_unnamed_conflict`
- `invalid_actor_attribution_status`
- `incomplete_unnamed_actor_review`

### 2. All six rungs are mandatory

For `reviewed-unnamed`, `RungsChecked` must equal this ordered list exactly:

```text
line
expanded-context
section-header
book-title
tei-header
parallel-passage
```

No missing rung, unknown rung, duplicate, or reordered “shortcut” passes. The reviewer must still inspect ±500, ±2,000, and ±10,000 characters inside the single `expanded-context` rung as Rule 10 requires; the `AttributionNote` should state that these windows were exhausted.

### 3. AttributionNote remains mandatory and reader-facing

For a reviewed unnamed actor, `AttributionNote` must:

- name the source text;
- say explicitly that the exact actor is unnamed in the record;
- identify the actor kind and exact action, such as “the unnamed monk asks” or “the unnamed monk pauses”;
- say that all six attribution rungs were exhausted;
- name every retained `ContextMasters` person and state the matching role in readable prose.

The existing source-title gate still applies. `note_missing_speaker` should be replaced by a state-aware `note_missing_exact_actor`: a reviewed phrase such as “the unnamed monk asks” satisfies the actor clause, while the respondent's name alone does not.

### 4. Context master links are roster-strict and role-strict

- Every `ContextMasters[].MasterName` must match roster `names[0]` exactly.
- `Roles` must be nonempty and contain only controlled values.
- Duplicate names or duplicate roles fail.
- A note/context mismatch fails: if the structured role says `respondent`, the note must not call that person the questioner or speaker of the headword.
- A contextual master does not increase the count of named exact actors.

Suggested failure kinds:

- `invalid_context_master`
- `invalid_context_master_role`
- `duplicate_context_master`
- `context_role_note_mismatch`

### 5. Do not let anonymous review become a quota shortcut

- `reviewed-unnamed` is not a waiver for exact KWIC, source, line, evidence-role, depth, or public-interview gates.
- The exact headword/action must belong to the anonymous actor in the complete case.
- If a named respondent's reply is needed as evidence, store it separately as an actor-pure supporting occurrence or keep it in the full-case KWIC while leaving `MasterName` attached only to the exact headword actor. Never assign the entire mixed-turn KWIC to the respondent.
- Prefer a named exact-actor witness where one exists and carries the same deployment, but do not delete structurally necessary anonymous evidence merely to improve an attribution percentage. 僧問 is the canonical case where anonymity is itself part of the attested formula.

### 6. Report attribution states honestly

The audit summary should expose at least:

- `named_exact_actors`
- `reviewed_unnamed_exact_actors`
- `unresolved_exact_actors`
- `context_master_links`

`reviewed_unnamed_exact_actors` passes the hard gate but must never be folded into `named_occurrences`. This preserves an honest denominator.

## Migration impact

### Known migration set

The reviewed cohort contains twelve immediately eligible occurrences:

- eleven in `t_67bff0d0e5d3` 僧問;
- one in `t_6abcff898d95` 良久 (`僧良久` before Songshan Junji's question).

For these twelve only, migrate the already completed ladder work into `ActorAttribution`, and translate each note's named respondent/source/later-raiser into `ContextMasters` with exact roles.

Do **not** automatically convert every existing `MasterName: null`. First scan all nulls, reopen the complete case, and require an explicit six-rung review. A bulk “null means reviewed unnamed” migration would turn old omissions into approved exceptions.

### Model and artifact changes required when implementation is authorized

1. Add optional `ActorAttribution` and `ContextMasters` properties to the canonical `DictOccurrence` model and serializer.
2. Ensure the merge/sharding path preserves both fields in `termbase.v2.json` and `termbase/NNN.json`.
3. Keep the legacy downgrade backward-compatible; it may omit these fields, but v2 remains authoritative. Document that the legacy artifact cannot express reviewed anonymity.
4. Update `audit_attribution.py` and cohort gates before marking the twelve entries done, so reviewed nulls pass only through the strict branch above.
5. Add migration/audit tests covering named, reviewed unnamed, unresolved, contradictory, incomplete-rung, invalid-role, and roster-mismatch cases.

No schema-version bump is required for this additive change if all v2 producers preserve unknown/new fields. If any canonical producer rewrites occurrences through a closed model and cannot safely preserve them, update that model before migration; silent field loss is a release blocker.

## Minimal website behavior

The current website normalizer reads only occurrence `MasterName`, `AttributionNote`, and `EvidenceRole`; the evidence card displays an empty `MasterName` as “Speaker unresolved.” That incorrectly conflates reviewed anonymity with unfinished attribution.

### Normalize

Extend `occurrenceView` to carry:

- `actorAttribution`
- `contextMasters`

Do not synthesize `masterName` from `contextMasters`, the sense master, the title owner, or the respondent.

### Render

Change the byline label from `Speaker:` to **`Exact actor:`**, because actions such as `僧良久` are not speech.

- Named branch: `Exact actor: Zhaozhou Congshen` as the existing master link.
- Reviewed-unnamed branch: `Exact actor: Unnamed monk` plus a small non-link badge, `reviewed unnamed`.
- Unresolved branch: `Exact actor: Attribution incomplete` in the existing missing/error style.

When `ContextMasters` is nonempty, render a separate line:

```text
Named context: Zhaozhou Congshen — respondent · Wumen Huikai — record owner, later commentator
```

Each name links to its master page; role labels do not. This preserves useful navigation without implying that the linked respondent asked the question.

The source link and full `AttributionNote` remain visible as they are now.

### Evidence-reference accessibility

The superscript evidence button's accessible label must use the exact actor state first:

```text
Show evidence 1: unnamed monk — T48n2005 0292c23–0292c24; respondent Zhaozhou Congshen
```

It must not collapse to `Zhaozhou Congshen — T48n2005...`, which would recreate the false speaker attribution outside the visible card.

### Website tests

Add tests proving:

1. a named actor renders one linked exact-actor name;
2. reviewed unnamed renders “Unnamed monk,” not “Speaker unresolved”;
3. contextual respondents/source masters render as separate linked context;
4. contextual masters never populate the exact-actor link or evidence provenance;
5. unresolved null still renders an incomplete-attribution warning;
6. all dynamic labels, names, and notes remain escaped.

## Examples

### 僧問: anonymous questioner, named respondent and source commentator

```json
{
  "RelPath": "T/T48/T48n2005.xml",
  "FromLb": "0292c23",
  "ToLb": "0292c24",
  "Kwic": "趙州和尚因僧問。狗子還有佛性。也無。州云無。",
  "MasterName": null,
  "ActorAttribution": {
    "Status": "reviewed-unnamed",
    "Kind": "monk",
    "RungsChecked": [
      "line",
      "expanded-context",
      "section-header",
      "book-title",
      "tei-header",
      "parallel-passage"
    ],
    "ReviewedBy": "Codex /root/feedback_lexicography",
    "ReviewedUtc": "2026-07-13T00:00:00Z"
  },
  "ContextMasters": [
    {
      "MasterName": "Zhaozhou Congshen",
      "Roles": ["respondent"]
    },
    {
      "MasterName": "Wumen Huikai",
      "Roles": ["record-owner", "later-commentator"]
    }
  ],
  "Curated": true,
  "AttributionNote": "The Gateless Checkpoint opens its first case. The exact questioner is an unnamed monk; all six attribution rungs, including all three expanded-context windows and parallel passages, leave him unnamed. Zhaozhou Congshen is the respondent and Wumen Huikai is the record owner and later commentator."
}
```

### 良久: anonymous actor of a pause, named respondent

```json
{
  "RelPath": "X/X80/X80n1565.xml",
  "FromLb": "0054c09",
  "ToLb": "0054c09",
  "Kwic": "僧良久。師曰。會麼僧。曰。不會",
  "MasterName": null,
  "ActorAttribution": {
    "Status": "reviewed-unnamed",
    "Kind": "monk",
    "RungsChecked": [
      "line",
      "expanded-context",
      "section-header",
      "book-title",
      "tei-header",
      "parallel-passage"
    ],
    "ReviewedBy": "Codex /root/feedback_lexicography",
    "ReviewedUtc": "2026-07-13T00:00:00Z"
  },
  "ContextMasters": [
    {
      "MasterName": "Songshan Junji",
      "Roles": ["respondent", "section-subject"]
    }
  ],
  "Curated": true,
  "AttributionNote": "Five Lamps Meeting the Source, in Songshan Junji's explicit section, records an unnamed monk as the exact actor of the pause; all six attribution rungs leave him unnamed. Songshan Junji is the named respondent who asks whether the monk understands."
}
```

The key invariant is unchanged: Songshan remains linked context and never becomes the `MasterName` for `僧良久`.

## Decision

Adopt optional `ActorAttribution` plus occurrence-level `ContextMasters`. Keep `MasterName` exact-actor-only. Accept a null only through the fully audited `reviewed-unnamed` branch. Render contextual masters as separately role-labelled links. This resolves the proven 僧問 / 良久 structure without weakening Rule 10 or manufacturing a speaker.
