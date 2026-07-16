# Quick attribution owner 1 report

Status: complete.

The three owned source units were reviewed by complete case and exact turn, signed, compiled, dry-run, applied, focused-gated, and verified against CBETA:

- X80n1565: 23 rows, 20 entries, 9 overrides, 23/23 `zc.verify`.
- X82n1571: 22 rows, 20 entries, 8 overrides, 22/22 `zc.verify`.
- T51n2076: 21 rows, 16 entries, 18 overrides, 21/21 `zc.verify`.

Total: 66 real occurrences across exactly the 50 exclusively owned entries. All decision sheets match the applied occurrence fields, all 66 KWICs replay at their stored `FromLb`, every entry JSON parses, and `git diff --check` passes apart from unrelated existing CRLF conversion warnings.

## Count reconciliation

The ownership manifest declared 67 rows (X80 24 / X82 22 / T51 21), while the initial filtered sheets contained 65 (23 / 22 / 20). The discrepancy came entirely from `t_2f4b60453d19` (`承當`):

- Its real T51 occurrence at 0344b27 was omitted from the quick sheet because it was classified `full-ladder-or-parallel-needed`. It was appended, reviewed in full context, attributed to Xuansha Shibei, applied, and verified.
- Its manifest X80 count was phantom. X80 appears in the entry's broader `SourceTexts` list, but exhaustive comparison finds no stored X80 occurrence for this entry. There is therefore no 67th occurrence to review or apply.

The reconciled source counts are X80 23 / X82 22 / T51 21 = 66.

No merge, commit, or push was performed.
