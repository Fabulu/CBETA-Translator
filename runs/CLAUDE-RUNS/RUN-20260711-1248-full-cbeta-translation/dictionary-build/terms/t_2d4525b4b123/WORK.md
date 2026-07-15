# WORK — 教外別傳 (t_2d4525b4b123)

## Concordance (Zen allowlist only, 462 texts)
- 教外別傳 → **542 hits / 194 files**. Every genre/lineage.
- Top texts: X68n1319 (19), J34nB311 (19), J26nB178 (16), D48n8939 (14), B25n0145 (14), X82n1571 (13),
  C077n1710 古尊宿 (12), X80n1565 五燈會元 (9), T51n2076 景德傳燈錄 (6), T48n2003 碧巖錄.

## Sense analysis
**One sense (corpus-wide, SenseKey=null).** Chan's self-description. Parsing corpus-internally:
教 = the doctrinal/scriptural teachings — glossed IN the corpus as 三乘十二分教 (the three vehicles and
the twelvefold canon); 外 = outside/apart from; 別傳 = a separate transmission.

Charter text = the **Flower Sermon** (X80n1565 0031a08): 世尊 holds up a flower, Mahākāśyapa smiles, and
正法眼藏。涅槃妙心。實相無相。微妙法門。不立文字。教外別傳。付囑摩訶迦葉 — "not set up on words, a separate
transmission outside the teachings, entrusted to Mahākāśyapa." This is the origin of the whole phrase.

The phrase does **double duty**:
(a) the four-phrase doctrinal slogan (with 不立文字 / 直指人心 / 見性成佛; Blue Cliff Record swaps in 單傳心印):
   - Yuanwu, T48n2003 0154c04: 謂之教外別傳。單傳心印。直指人心。見性成佛。
(b) a fixed **test-question** 如何是教外別傳(一句/底事/底法), deflected with a turning-word EXACTLY like
   祖師西來意:
   - Gushan Shenyan, T51n2076 0351b29: 如何是教外別傳底事。師曰。喫茶去 ("go drink tea").
   - Shishuang (raised case 舉石霜, C077n1710 0732b03): 如何是教外別傳一句霜云非句 ("a non-phrase").

And the tradition **turns the slogan on itself** (deflationary self-critique):
   - Yunmen, T51n2076 0356c20: 三乘十二分教豈是無言語。因什麼更道教外別傳 — "the canon is hardly wordless, so
     why go on to speak of a 'separate transmission outside the teachings'?"

One concept, several deployments → single corpus-wide sense (cf. 祖師西來意).

## Attribution evidence (cb:mulu heads, checked)
- X80n1565 0031a08 → 釋迦牟尼佛 (the Flower-Sermon legend) → MasterName=null (Śākyamuni; not a roster master).
- T48n2003 0154c04 → Blue Cliff Record 垂示 = **Yuanwu Keqin** (roster ✓).
- T51n2076 0356c20 → 韶州雲門山文偃禪師 = **Yunmen Wenyan** (roster ✓).
- T51n2076 0351b29 → 福州鼓山興聖國師 = **Gushan Shenyan** (roster ✓).
- C077n1710 0732b03 → raised case 舉石霜 → MasterName=null (rule 4); answer 非句 is Shishuang Qingzhu's.

## Multi-source verdict
**multi-source** — 4 independent texts (X80 五燈會元, T48 碧巖錄, T51 景德傳燈錄, C077 古尊宿), 3 rostered
masters + the founding legend + a raised Shishuang case. Both facets (slogan / test-question) are
independently multi-attested.

## Deflationary check / ewk (IMPORTANT)
Rendered literally "a separate transmission outside the teachings." **ewk's "outside the historical
records" is REJECTED** — the Chinese 教 is explicitly the doctrinal/scriptural teachings (the corpus
itself opposes it to 三乘十二分教), not "records." This is exactly the "verify, don't adopt ewk blindly"
caveat: grounded in the Chinese, his rendering fails.

## Nesting (§5b)
RelatedTerms 不立文字 · 直指人心 · 見性成佛 · 正法眼藏 · 祖師西來意 are genuine: the first three are the
co-slogan constituents; 正法眼藏 is the adjacent Flower-Sermon phrase; 祖師西來意 shares the deflected-
test-question behaviour (functional link). No coincidental prefixes.

## Gate 1 self-check
All 5 KWICs verified exact-contiguous, in-allowlist, FromLb = nearest preceding primary-ed <lb>
(X80 uses ed="X", not the co-located ed="R").

## Gate 2 (Claude adversarial verify+repair) — verified
- All 5 KWICs re-derived EXACT-CONTIGUOUS against cited files. No ellipses/stitching.
- Contamination: 0. All 4 RelPaths (X80n1565, T48n2003, T51n2076, C077n1710) in zen-corpus.json.
- Attribution re-confirmed at cb:mulu heads: X80 0031a08 correctly null (Śākyamuni Flower Sermon); Yuanwu (碧巖錄 pointer); Yunmen (韶州雲門山文偃禪師 @L15072); Gushan (福州鼓山興聖國師 @L14566, 師=speaker); C077 0732b03 correctly null (raised case 舉石霜, answer is Shishuang's).
- FromLb all = nearest preceding <lb>; X80 uses ed="X" (verified programmatically).
- Collocations verified: 單傳心印 (Blue Cliff variant) verbatim in T48n2003; 三乘十二分教 gloss (Yunmen) verbatim in T51n2076; 喫茶去 (Gushan) and 非句 (Shishuang) verbatim.
- Deflationary rendering intact: "a separate transmission outside the teachings"; ewk's "outside the historical records" correctly rejected (教 = 三乘十二分教, doctrinal teachings). No content repairs; RelatedTerms genuine.
- STATUS → verified.
