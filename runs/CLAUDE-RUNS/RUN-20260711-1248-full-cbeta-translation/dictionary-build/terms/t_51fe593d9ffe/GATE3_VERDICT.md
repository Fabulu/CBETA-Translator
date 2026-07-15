# Gate 3 Verdict — 作麼生 (t_51fe593d9ffe)

VERDICT: PASS

Independent adversarial re-derivation from the primary Chinese (Gate 3, fresh model, 2026-07-11).
All checks run against `C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5` and
`Assets/Data/zen-corpus.json`. Entry NOT modified.

## Per-sense findings

### Sense 1 (only sense): "how? / in what way?" — PASS

**Check 1 — KWIC exact + contiguous (4/4 verified verbatim):**
- occ 1 · `T/T47/T47n1985.xml` lb 0499b16 — file lines 524–525:
  `是爾如今與麼` (0499b16) + `聽法底人作麼生擬修他、證他、莊嚴他？渠且` (0499b17) →
  「是爾如今與麼聽法底人作麼生擬修他、證他、莊嚴他？」 exact contiguous across the lb,
  punctuation 、/？ preserved. VERIFIED.
- occ 2 · `X/X80/X80n1565.xml` lb 0071b18 — file line 5557:
  `師問溈山。併却咽喉唇吻。作麼生道。山曰。却請和尚` — KWIC 「師問溈山。併却咽喉唇吻。
  作麼生道。」 exact contiguous on one line (ed="X" numbering — correct for this dual X/R138
  file). The trim that dropped 山曰。却請和尚道 keeps the span single-speaker and remains an
  exact prefix of the source — legitimate shortening, not stitching. VERIFIED.
- occ 3 · `X/X80/X80n1565.xml` lb 0109b01 — file line 8392:
  `也不得。恁麼不恁麼總不得。子作麼生。師罔措。頭曰。` — KWIC 「恁麼不恁麼總不得。子作麼生。
  師罔措。」 exact contiguous within the line. VERIFIED.
- occ 4 · `T/T47/T47n1985.xml` lb 0503a25 — file lines 863–864:
  `後溈山問仰山：「此二尊宿` (0503a25) + `意作麼生？」仰山云：「和尚作麼生？」溈山云：`
  (0503a26) → KWIC exact contiguous across the lb, quote marks and ？ preserved. VERIFIED.
- No ellipses, no stitching, no altered punctuation. FromLb = line where each KWIC begins.

**Check 2 — RelPath real + allowlisted:** both files exist; `T/T47/T47n1985.xml` (line 253)
and `X/X80/X80n1565.xml` (line 453) are in `zen-corpus.json`. No contamination.

**Check 3 — Multi-source claim:** `multi-source` HOLDS. Two independent texts (臨濟語錄 T,
五燈會元 X), four distinct episodes, five masters (Linji sermon; Baizhang→Guishan; Shitou→
Yaoshan; Guishan↔Yangshan). No cited passage is a copy of another.

**Check 4 — Over-read:** none. The four collocations (作麼生道 / 作麼生會 / 子·汝作麼生 /
意作麼生) are presented as shared fixed shapes of the genre, not any master's signature; each
is anchored to a verified occurrence. My own greps confirm the collocations recur across
masters (e.g. 你作麼生 X80n1565 0328b05, 0418c16; 諸人作麼生會 0368a01).

**Check 5 — Imported abstraction:** none. Rendered as a bare live challenge-interrogative,
explicitly NOT "a philosophical 'why'" or "what is the ultimate." Deflationary and literal.

**Check 6 — Speaker attribution (all four correct):**
- occ 1 Linji Yixuan: continuous first-person sermon in the 臨濟語錄 discourse section
  (示眾 material around 0499b; quoting 祖師云 within his own speech); the KWIC sentence
  是爾如今…作麼生擬修他 is Linji's own. Single speaker. CORRECT.
- occ 2 Baizhang Huaihai: chapter head 洪州百丈山懷海禪師 (X80n1565 ed="X" 0071a09, file
  line 5523); immediately before the KWIC: 溈山．五峯．雲巖侍立次 (0071b17) — so 師 = Baizhang
  asks Guishan 併却咽喉唇吻。作麼生道. KWIC trimmed to his question only. CORRECT.
- occ 3 Shitou Xiqian: chapter head 澧州藥山惟儼禪師 (0109a19, file line 8385); the utterance
  opens 頭曰 at 0109a24 with 頭 = 石頭 (首造石頭之室, 0109a22; parallel 藥山問石頭…頭曰 at
  0418c13–15). 子作麼生 is the tail of Shitou's single speech; 師罔措 is narration about
  Yaoshan, not a second spoken line. CORRECT.
- occ 4 null: the hit sits under the 勘辨 head (T47n1985 lb 0503a16, file line 854); it is an
  appended two-speaker Guishan↔Yangshan exchange (溈山問仰山…仰山云…), so MasterName null with
  both named in the AttributionNote is exactly right. CORRECT.

## Issues (tagged)

None blocking. Non-blocking observations:
- INFO: Frequency claim in Note (412 allowlist files) not re-derived corpus-wide by Gate 3;
  contextual only.
- INFO: occ 3 deliberately cites the same passage as 恁麼's occ 2 (cross-link between the two
  deictics); disclosed in WORK.md and each entry quotes the span relevant to its own headword —
  acceptable, and the multi-source status of this entry does not depend on that occurrence.

## Verified occurrences: 4/4 KWIC confirmed verbatim
