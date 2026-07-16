# Bundle C consolidated attribution report

All six conflict-owned sources were processed sequentially through full-case review, signed compilation, strict dry-run, atomic apply, focused stored-field comparison, JSON parse, and exact source replay.

| Source | Term / line | Exact actor | Result | Context figures | Gate |
|---|---|---|---|---|---|
| T48n2016 | `漸修` / `0626b28` | Yongming Yanshou | default retained | — | 1/1 |
| X63n1220 | `觀心` / `0008c09` | Bodhidharma | default retained | — | 1/1 |
| X68n1319 | `戒定慧` / `0530a23` | Yongzheng Emperor | default retained | — | 1/1 |
| X70n1384 | `大用` / `0289a17` | Xuansha Shibei | override | Xuefeng Yicun; Jue'an Kexiang | 1/1 |
| X72n1432 | `放下著` / `0186c07` | Zhaozhou Congshen | override | Wuming Huijing | 1/1 |
| X73n1451 | `莊周` / `0121b10` | Cishou Huaishen | default retained | — | 1/1 |

Totals: six named exact actors, four retained defaults, two overrides, zero unnamed/impersonal decisions, zero dry-run/apply failures, six of six focused gates, and six of six exact `zc.verify` successes. All 30 expected JSON parses passed (four artifacts plus one entry per source). Touched-entry audit hard failures fell by 17 overall (98→81); remaining findings are outside the six exact occurrences.

Triage reconciliation confirmed every workbook row is present in current triage. T48n2016, X63n1220, and X68n1319 have respectively 18, 1, and 27 additional source-triage rows outside these conflict-owned workbook scopes; X70n1384, X72n1432, and X73n1451 have no additional row.

Crash-resume state and exact hashes are recorded in `maintenance/bundle-ledgers/quick-bundle-C.json`; the human-readable checkpoint trail is `quick-bundle-C.md`. No unit remains failed or deferred, and `nextUnit` is null.

No merge, commit, or push was performed.
