# WORK — 話墮 (t_427fa502a11b)

## Concordance (Zen allowlist only, 462 texts)
- 話墮 → **552 hits / 170 files**. Broadly spread across 語錄, 廣錄, 燈錄, 會元 — every major genre and lineage.
- Top texts: X82n1571 (34), X80n1565 五燈會元 (18), X79n1559 (18), C077n1710 古尊宿 (16), X66n1296 (14), T51n2076 景德傳燈錄 (11), T47n1988 雲門廣錄 (8).

## Sense analysis
**One sense (corpus-wide, SenseKey=null).** Literally "speech-fall" (話 speech + 墮 fall). It names the
moment a speaker is caught out by his own words and thereby loses an encounter-dialogue. The dominant
form is the master's verdict on a monk: 你/汝/爾話墮也 ("you've slipped"). The caught monk then presses
什麼處是話墮處 ("where exactly is the slip?") and receives NOT an explanation but a further blow / turning-
word:
- Muzhou (睦州道明), C077n1710 0657c09: 師云你為什麼話墮進云什麼處是話墮處師云擔枷過狀萬里崖州自領出去 — "why
  have you slipped?"…"shoulder the cangue, banish yourself 10,000 li to Yazhou."
- Yunmen (雲門文偃), T47n1988 0550b06 (his own 廣錄): 話墮也。進云。什麼處是話墮。師云。七棒對十三。
- Luohan Guichen (羅漢桂琛), X80n1565 0167a22: 和尚因甚麼如此。師曰。汝話墮也。

The concept is **reciprocal / turnable**, not a one-way master-weapon:
- Baozi Xiaowu, T51n2076 0375c01: 師曰。爾話墮也。又曰。我話亦墮汝作麼生。僧無對 — "you've slipped … my speech
  slips too, what about you?"
- 彼此話墮 ("both of us have slipped"), T51n2076 0401c02 (泉州福清廣法大師).
- Dahui (大慧宗杲), X80n1565 0404a19: 老胡九年話墮。可惜當時放過 — charges even Bodhidharma's nine years of
  wall-gazing with a 話墮 (the term's furthest rhetorical reach).

Adjacent term in the same passages: **墮負** ("defeat/fault") — C077 has 有什麼墮負 right beside 話墮,
confirming the "lost the bout" reading over any doctrinal "fall."

No master bends the *referent*; the range (verdict / reciprocal / applied-to-a-patriarch) is one concept
deployed, exactly like 祖師西來意. So a single corpus-wide sense is correct.

## Attribution evidence (cb:mulu heads, checked)
- C077n1710 0657c09 → 睦州禪師語錄 = **Muzhou Daoming** (roster ✓).
- T47n1988 0550b06 → whole text is 雲門匡真禪師廣錄 = **Yunmen Wenyan** (roster ✓); section 對機三百二十則.
- T51n2076 0375c01 → 婺州金鱗報恩院寶資曉悟大師 = Baozi Xiaowu, **NOT in roster** → MasterName=null + note.
- X80n1565 0404a19 → 臨安府徑山宗杲大慧普覺禪師 = **Dahui Zonggao** (roster ✓).
- X80n1565 0167a22 → 漳州羅漢院桂琛禪師 = **Luohan Guichen** (roster ✓).

## Multi-source verdict
**multi-source** — 4 independent texts (C077 古尊宿, T47 雲門廣錄, T51 景德傳燈錄, X80 五燈會元), 4 rostered
masters + the reciprocal usage attested in two further independent passages.

## Deflationary check
Rendered "a slip of speech / being caught out in a dialogue." NOT inflated into a soteriological "fall."
Grounded in 墮負 (defeat) co-occurrence. Passes the 凡情聖見 fakeout test.

## Gate 1 self-check
All 5 KWICs re-verified as exact contiguous substrings (tags+newlines stripped); all RelPaths in
zen-corpus.json; every FromLb = nearest preceding primary-ed <lb>. Non-X texts single ed; no X dual-lb
occurrences here except X80 (ed="X" used, not ed="R").

## Gate 2 (Claude adversarial verify+repair) — verified
- All 5 KWICs re-derived EXACT-CONTIGUOUS against cited files (tag+whitespace stripped, char-for-char incl. gaiji). Muzhou KWIC contains gaiji U+2F804 (你-variant) at 0657c09 — matches the file byte-for-byte; a naive plain-你 re-type does NOT (noted so gate 3 doesn't false-flag). No ellipses/stitching.
- Contamination: 0. All 4 RelPaths (C077n1710, T47n1988, T51n2076, X80n1565) in zen-corpus.json.
- Attribution re-confirmed at governing cb:mulu heads: Muzhou (睦州禪師語錄 @L7689), Yunmen (whole text 雲門廣錄), Dahui (臨安府徑山宗杲大慧普覺禪師 @L30403), Guichen (漳州羅漢院桂琛禪師 @L12648, 師=speaker in his own section). Baozi correctly null (婺州金鱗報恩院寶資曉悟大師, not in roster).
- FromLb all = nearest preceding <lb>; X80 uses ed="X" (verified programmatically).
- Collocations verified: 墮負 present in C077n1710; 彼此話墮 contiguous in T51n2076; 七棒對十三 (Yunmen) verbatim.
- Repairs: (1) added ToLb 0657c10 to Muzhou span (crosses lb); (2) removed 話頭 from RelatedTerms — coincidental 話-prefix (hua-tou meditation ≠ losing-an-exchange), per §5b. Kept 墮負/轉語/機鋒 (genuine encounter-dialogue field).
- Deflationary rendering intact: "slip of speech / caught out (墮負)", not a soteriological fall.
- STATUS → verified.
