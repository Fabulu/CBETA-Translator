# CBETA Text Library Sorting Data

This folder contains the analysis outputs and scripts used by the CBETA Translator navigation/sorting UI.

## Files Present

### Analysis outputs
- `buddhist_metadata_analysis.json` - Canon/tradition/period/origin analysis with per-file records (`detailed_analysis`)
- `projectdesc_analysis.json` - Contributor/projectDesc analysis
- `projectdesc_report.txt` - Human-readable projectDesc report

### Scripts
- `analyze_buddhist_metadata.py`
- `analyze_projectdesc.py`
- `projectdesc_summary.py`
- `sorting_recommendations.py`

## GUI Integration (Current)

The app currently consumes:

- `buddhist_metadata_analysis.json`

Specifically, `Services/BuddhistMetadataService.cs` reads `detailed_analysis[]` and normalizes each absolute XML path into a root-relative key like:

- `T/T01/T01n0001.xml`

Those keys are joined with indexed file entries and persisted into `index.cache.json` (v3 schema).

The loader searches multiple locations for this file:
- `<selected-root>/CBETA_Sorting_Data/...`
- parent folders of the selected root (for the common `CbetaZenTexts` root layout)
- app base directory fallback

## Fallback Behavior

If `buddhist_metadata_analysis.json` is missing, unreadable, or incomplete:

- navigation still loads,
- files remain browsable/searchable,
- metadata defaults are used:
  - `Unknown Tradition`
  - `Unknown Period`
  - `Unknown Origin`

## Notes

- Many files are multi-tradition; filtering treats tradition as multi-valued.
- Unknown categories are intentionally included in UI dropdowns.
