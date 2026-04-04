# Scholar Export Roadmap

## Current user flow

1. A user finds a passage in `Reader`, `Translate`, or `Search`.
2. They add it to a Scholar collection.
3. In Scholar they refine it with notes, tags, master names, and facet categories.
4. They compare passages, link them, and navigate back to source texts.
5. They export the collection for writing, sharing, or further analysis.

Scholar is functioning as a research notebook and quotation bank, not just a bookmark list.

## Current export state

Current rich exports:

- `HTML`
- `Markdown`
- `PlainText`

Current fallback export:

- `JSON` for all collections

Current rich exports include:

- collection name and description
- Chinese and English passage text
- tags
- master names
- notes
- facet categories
- cross-references
- source title derived from `SourceRelPath`

## Current export gaps

Current exports are not yet academically solid enough. They do not reliably emit:

- `CreatedBy` attribution
- raw `SourceRelPath`
- `FromLb` / `ToLb`
- `StartBlockNumber` / `EndBlockNumber`
- added / modified timestamps
- stable per-passage deep links

Where links are emitted or recommended, they must use the current `Zen://` / `zen://` scheme, not `cbeta://`.

## Required hardening

Strengthen existing `HTML`, `Markdown`, and `PlainText` exports so each passage can include:

- `CreatedBy`
- raw source path
- line anchors (`FromLb`, `ToLb`)
- block anchors (`StartBlockNumber`, `EndBlockNumber`) when present
- stable `Zen://` deep link or share URL
- added / modified timestamps

Collection-level attribution should also be exported.

## Recommended next formats

### 1. CSV / TSV

One row per passage with provenance and facet fields.

Use cases:

- spreadsheets
- coding and sorting
- import into external research tools

### 2. BibTeX

Add a per-passage `@misc` export.

Recommended fields:

- `title`
- `author` when meaningful
- `howpublished`
- `note`
- `keywords`
- `url`

Use `Zen://` or share URLs for the source link.

### 3. CSL-JSON

Add CSL-JSON soon after BibTeX.

Reason:

- better interoperability with Zotero and modern citation workflows

### 4. Paper draft export

Generate a structured `Markdown` or `HTML` draft, not a fake polished paper.

Recommended shape:

- grouped by tag, master, or facet
- quotations
- translation
- notes
- citation block under each passage

This should function as a research draft scaffold.

## Recommended implementation order

1. Strengthen attribution and provenance in current exports.
2. Add `CSV` / `TSV`.
3. Add `BibTeX`.
4. Add `CSL-JSON`.
5. Add `paper draft` export.

## Notes

- Do not regress current `HTML`, `Markdown`, and `PlainText` export behavior.
- Keep selected-collection export and all-collections JSON export distinct.
- Export must preserve passages and their attributions, not just readable text.

## Step 6: Reader Tagging Compatibility

CSV and TSV should not be treated as automatically compatible with the Reader tagging engine.

What is feasible:
- A dedicated structured export/import contract for Reader tags.
- A `document-tags.tsv` export with `rel_path`, `from_lb`, `to_lb`, `tag_id`, `tag_name`, `created_by`, `created_utc`, and `source_passage_id`.
- A vocabulary sidecar such as `tag-vocabulary.tsv` or JSON carrying `tag_id`, `name`, `parent_id`, `color`, `description`, and `sort_order`.
- An optional Scholar-oriented TSV with passage text, notes, categories, and collection metadata for spreadsheet work.

Constraints:
- Reader tagging is anchored by lb ranges and vocabulary `tag_id`s.
- Scholar passage tags are currently free-text names, so converting them back into Reader `DocumentTag` records is lossy unless we carry `tag_id` and vocabulary data explicitly.
- Block-only anchors are not sufficient for Reader highlighting.

Recommendation:
- Keep general Scholar CSV/TSV export separate from Reader-tag interchange.
- If we add a Reader-compatible export/import path, label it explicitly as structured tag interchange and make `tag_id` mandatory.
- Use `Zen://` links in all exports and examples.
