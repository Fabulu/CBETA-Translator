# Codex Verdict - 五位 (t_ff50c6974a36)
VERDICT: PASS
- PASS = merge as-is.
- REVISE = specific fixable issues (list them + the fix); do not merge until fixed.
- FAIL = fabrication / contamination / fundamentally wrong; do not merge.

## Per-sense findings

### Sense 1 - Dongshan Liangjie / the Five Ranks

PASS. The curated KWICs are exact contiguous substrings after stripping XML tags/whitespace; no editorial ellipsis or stitched span remains. All cited occurrence RelPaths exist and are in `Assets/Data/zen-corpus.json`.

- `J/J10/J10nA158.xml`, lb `0040b17`-`0040b20`: confirmed `然後所立君臣、偏正、王子、功勳各五位者，若我臨濟大師要且不然，但曰：「赤肉團上有無位真人嘗在面門出入，未證據者看看。」`
- `J/J26/J26nB188.xml`, lb `0756a04`-`0756a08`: confirmed the full contiguous five-rank exchange beginning `五位君臣事若何？` and continuing through `正中偏`, `偏中正`, `正中來`, `兼中至`, `兼中到`.
- `J/J25/J25nB163.xml`, lb `0256a14` onward: confirmed `曹山寂禪師。僧問：「五位對賓時如何？」...「某甲從偏位中來，請師正位中接。」`
- `C/C078/C078n1720.xml`, lb `0787b02`-`0787b03`: confirmed `曹山因僧問五位對賔時如何師曰汝即今問那箇位曰某甲從偏位中來請師向正位中接師曰不接`
- `J/J25/J25nB156.xml`, lb `0062c07`: confirmed `洞山五位悟非悟，臨濟三玄然未然`

Multi-source check: PASS. The sense is attested in at least five Zen texts, not merely repeated from one witness: `J10nA158` lists the four fivefold schemata, `J26nB188` gives an independent interview through the five ranks, `J25nB163` and `C078n1720` witness the Caoshan 對賓 exchange, and `J25nB156` independently pairs 洞山五位 with 臨濟三玄. `Validation: multi-source` is supported.

Sense integrity / over-read check: PASS. The entry marks the Dongshan/Caoshan division of labour instead of laundering Caoshan material into a sole Dongshan attribution. The explanation's "pedagogical device" framing is deflationary and tied to the cited 正/偏, 君臣, 對賓 evidence; I found no unsupported uniqueness claim or imported abstraction requiring revision.

### Sense 2 - generic five positions / five stages

PASS. The curated KWICs are exact contiguous substrings after stripping XML tags/whitespace; no ellipsis, stitching, or altered punctuation found. Both occurrence RelPaths exist and are Zen-allowlisted.

- `B/B25/B25n0144.xml`, lb `0676a05`: confirmed `寄因五位，乃至果位。雖寄此位，不住此位。`
- `J/J26/J26nB180.xml`, lb `0295b04`: confirmed `第五位聖人又是那箇？`

Multi-source / validation check: PASS. The entry does not overclaim this as a unified multi-source Zen technical sense; it correctly uses `Validation: provisional` and explains the bucket as heterogeneous generic residue. Sense split is justified: these occurrences lack 正/偏, 君臣, 洞山/曹山, and 臨濟三玄 signals.

## Issues (tagged)

- None.

## Verified occurrences: 7/7 KWIC confirmed verbatim

Additional allowlist check: all `SourceTexts` listed in both senses also exist under `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` and are present in `Assets/Data/zen-corpus.json`, including the non-curated support paths `D/D51/D51n8948.xml`, `J/J26/J26nB185.xml`, `J/J27/J27nB189.xml`, and `B/B27/B27n0152.xml`.
