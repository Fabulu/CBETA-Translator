# Queue deduplication audit — 2026-07-13

Scope: `NEXT500_BUILD_PLAN.md` (500 rows), `NEXT100_BUILD_PLAN.md` (100 rows), and
`RELATED_INVESTIGATION_BACKLOG.md` (720 rows).

## Mechanical result

- 1,320 rows parsed.
- Zero duplicate exact headwords.
- Zero duplicate deterministic IDs.
- Zero duplicates after NFKC, punctuation/spacing removal, and the first-pass common-variant map.
- A broader traditional-graph map found two definite duplicate pairs inside the backlog.
- Cross-queue containment produced 74 review candidates. Most are legitimate component/compound families, not
  duplicates; containment never authorizes automatic deletion.

## Definite duplicate variants

These must be researched and built once. The retained article must preserve both written forms as attested variants
or search aliases and inherit both backlog leads.

| Preferred investigation row | Duplicate row | Why |
|---|---|---|
| `拈槌豎拂` (backlog 323) | `拈槌竪拂` (backlog 340) | `豎/竪` orthographic variants; otherwise identical phrase. |
| `畫餅充飢` (backlog 707) | `畫餅充饑` (backlog 588) | `飢/饑` orthographic variants in this phrase. |
| `有甚麼交涉` (next-500) | `有甚交涉` (backlog 269) | Same question formula with optional `麼`; combine concordances before choosing display form. |
| `百尺竿頭須進步` (next-500) | `百尺竿頭進步` (backlog 460) | Same saying with optional `須`; one saying article should anchor both forms. |
| `龜毛拂` (sayings-100) | `龜毛拂子` (backlog 343) | Same impossible tortoise-hair whisk with the ordinary object suffix `子`; one family article unless full-case research proves a second lexical object. |
| `空劫已前` (next-500) | `空劫以前` (backlog 405) | `已前/以前` are variant “before” forms for the same compound frame; combine evidence and retain the attested alternate. |

This yields **six duplicate rows** at minimum, so the naive 1,320-row sum overstates independent work by at least
six. The queues remain unchanged until their inherited leads are folded into a canonical row; no evidence should be
lost merely to improve a count.

## Strong merge candidates requiring corpus adjudication

| Planned row | Backlog row | Required test |
|---|---|---|
| `出身之路` | `出身一路` | Test whether `之路` and `一路` are variants of one named route or genuinely different constructions. |
| `趙州茶` | `趙州喫茶` | Test named-case shorthand versus the headword-bearing action formula; likely one article/family, but do not infer from shared story alone. |
| `須彌山` | `須彌盧` | Same referent is likely, but determine whether the corpus treats these as alternate names worth one article or separately searchable lexical forms. |
| `洗鉢盂` | `洗鉢` | Apply the nested-compound gate: the specified bowl phrase cannot automatically erase an independently used bare washing action. |
| `續燈錄` | `續燈` | Separate a particular book title from title shorthand and the productive “continue the lamp” phrase. |
| `頂門正眼` | `頂門具眼` | Similar wording is not enough; test whether these are formula variants or distinct predicates. |

## High-volume false positives retained as distinct

Examples among the 74 containment candidates that are not duplicates merely because they overlap include
`佛法`/`佛法大意`, `祖師`/`祖師意`, `一棒`/`一棒一條痕`, `泥牛`/`泥牛吼月`,
`末後一句`/`末後一著`/`末後一關`, and `古尊宿`/`古尊宿語錄`. Component, saying, title, and
institutional referents must pass the ordinary sense and nested-compound gates independently.

## Build gate

Before claiming a row from any of these three queues, check this audit plus all earlier/later queue rows under:

1. exact deterministic ID and exact headword;
2. punctuation/spacing normalization;
3. attested traditional graph variants;
4. optional particles and common suffixes (`子`, `盂`, `錄`, etc.);
5. longest lexical object and nested-compound status;
6. same referent versus genuinely different word/thing;
7. inherited-lead preservation.

A duplicate disposition redirects the row to the canonical article and records its variant, anchors, counts, and
inherited interpretation there. It never silently deletes the row or its research lead.
