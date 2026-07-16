# Consolidated source-batched attribution report: quick bundle A

Scope: ten regenerated-triage rows, one in each assigned source, reviewed and applied sequentially. This was attribution-only remediation. No sense, gloss, KWIC, evidence inventory, or source range was changed, and this report does not claim whole-entry remediation.

Durable crash-resume state is recorded after every completed source in `maintenance/bundle-ledgers/quick-bundle-A.json` and `quick-bundle-A.md`. The final ledger records ten completed units, no failed/deferred units, no remaining units, and `nextUnit: null`; its stored artifact hashes match the files on disk.

## Regenerated-triage reconciliation

Every prepared row was matched against the current `maintenance/attribution-triage-all.json` before review. For all ten, entry ID, source term, source path, line anchor, exact KWIC, case-cluster ID, and source title match the regenerated triage.

## Bundle result

- Rows reviewed and applied: 10/10.
- Named exact actors: 8.
- Reviewed unnamed non-master actors: 1.
- Impersonal textual actors: 1.
- Exact `zc.verify`: 10/10.
- Strict compile/dry-run/apply: 10/10, zero final failures.
- Focused audit reduction: every scoped row removed its unresolved-actor, missing-speaker, and missing-source failures; each source's focused entry audit fell by three hard failures. Remaining failures are untouched occurrences/prose outside the assigned rows.
- No merge, commit, or push was performed.

## Exact decisions and per-source gates

| Source | Entry | Exact actor decision | Candidate adjudication | Focused hard failures | `zc.verify` |
|---|---|---|---|---:|---|
| T47n1986A | `空劫` `t_ff25afe69d14` | Reviewed unnamed monk, exact questioner | Rejected Dongshan as actor; he answers the monk | 12 → 9 | `0511c14`–`0511c15` |
| T47n1986B | `空劫` `t_ff25afe69d14` | Dongshan Liangjie, exact questioner | Confirmed after mapping Shushan Kuangren's replies | 9 → 6 | `0522b25`–`0522b26` |
| X69n1354 | `空劫` `t_ff25afe69d14` | Yuelin Shiguan, exact hall speaker | Confirmed from record, section, and complete turn | 6 → 3 | `0346c14`–`0346c15` |
| J39nB463 | `君臣` `t_2069b9c33315` | Gumei Dinglie, exact whisk-address speaker | Rejected Guishan Lingyou; Guishan is the monastery | 15 → 12 | `0800c13`–`0800c14` |
| T48n2023 | `客塵` `t_57fd70bfc9ec` | Foyan Qingyuan, exact inscription author | Confirmed by the explicit inscription heading | 9 → 6 | `1048b16`–`1048b17` |
| X67n1303 | `丹霞燒佛` `t_76cff76a9bd3` | Impersonal narratorial case heading | Rejected Danxia as actor; he is the case subject | 12 → 9 | `0282b13`–`0282b14` |
| X69n1357 | `騰騰任運` `t_6a1ea4df00ce` | Yuanwu Keqin, exact instruction author | Confirmed under the `示禪人` heading | 10 → 7 | `0491a07`–`0491a08` |
| X70n1402 | `提起` `t_18b083a026ba` | Zhongfeng Mingben, exact instruction author | Confirmed under `結夏示順心庵眾` | 20 → 17 | `0714c20`–`0714c22` |
| X72n1440 | `話頭` `t_d190cf45c531` | Weilin Daopei, exact general-address speaker | Dahui is discussed afterward, not quoted here | 39 → 36 | `0653a16`–`0653a17` |
| X81n1568 | `立雪` `t_d2892b1eaae0` | Bailu Xianduan, exact hall speaker | Rejected Bodhidharma; he and Huike are discussed | 13 → 10 | `0054c12`–`0054c13` |

## Notable exact-turn corrections

- `T47n1986A`: the headword is inside `僧問`; the unnamed monk speaks it, while Dongshan Liangjie answers `白馬入蘆華`.
- `J39nB463`: `溈山` identifies Gumei Dinglie's monastery. The title/byline and complete whisk-address section identify Gumei, not the much earlier Guishan Lingyou.
- `X67n1303`: the duplicated headword belongs to the numbered XML heading `第二十五則丹霞燒佛`. Danxia Tianran is the old-case subject; Linquan Conglun supplies the following instruction/commentary. The stored actor is therefore impersonal, with both masters preserved as named context.
- `X81n1568`: the explicit section header `福州白鹿山顯端禪師` governs the hall address. Bailu Xianduan criticizes Bodhidharma's wall-facing and Huike's standing in snow; neither discussed figure is the speaker.

## Mechanical checks

- Every override sheet is signed and every row carries a full decision.
- Every source passed strict compile, dry-run, and atomic apply with one prepared/applied row and zero final failures.
- The X67 impersonal row was not marked complete until its actor label and narratorial-note wording passed both the compiler and focused audit.
- The X81 row was not marked complete until its attribution note contained the exact full source title `五燈嚴統(第10卷-第25卷)` and the complete gate reran cleanly.
- All touched entry, decision, dry-run, applied, audit, override, and ledger JSON files parse.
- All ten stored KWICs pass source-backed exact `zc.verify` at their recorded primary-edition line ranges.
- Ledger artifact hashes match the final files.
- `git diff --check` is clean for the bundle's touched entries and artifacts.
