// CommentaryLanguageClassifierTests — pure-static classifier unit tests.
// Locks in the 3-tier strategy + default-deny posture (Tier 4 = "unknown",
// not "zh-Hant"). Each fact maps to a Recon-D scenario plus the default-deny
// reframe (SPEC v2 §"Default-deny posture", user reinforced 2026-05-12).

using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "FiM")]
public class CommentaryLanguageClassifierTests
{
    [Fact]
    public void Classify_ExplicitXmlLangJa_TrustsAlways()
    {
        // Tier 1: explicit metadata wins even when body is empty
        // and title gives no signal.
        var entry = new CommentaryEntry
        {
            CommentaryId = "C1",
            Language = "ja",
            Title = "信心銘", // pure CJK — no Japanese signals here
            Body = null,
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("ja", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.ExplicitMetadata, tag.Source);
        Assert.Contains("ja", tag.Evidence);
    }

    [Fact]
    public void Classify_ExplicitXmlLangZhHant_TrustsOverHiraganaBody()
    {
        // Tier 1 must trump Tier 2: an admin who tags a quoted-Japanese
        // Chinese commentary as zh-Hant gets the explicit tag back,
        // not a Tier-2 reclassification to ja.
        var entry = new CommentaryEntry
        {
            CommentaryId = "C2",
            Language = "zh-Hant",
            Title = "信心銘解",
            Body = "本書は信心銘の解釈である。これとは、これ。", // many hiragana
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("zh-Hant", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.ExplicitMetadata, tag.Source);
    }

    [Fact]
    public void Classify_BodyWithThreeHiragana_ReturnsJaViaContentKana()
    {
        // Tier 2: body crosses the kana threshold (>= 3).
        var entry = new CommentaryEntry
        {
            CommentaryId = "C3",
            Language = null,
            Title = "信心銘", // pure CJK
            Body = "信心銘これはあり。", // 3 hiragana: こ, れ, は (and あ, り — total 5)
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("ja", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.ContentKana, tag.Source);
        Assert.Contains("count=", tag.Evidence);
    }

    [Fact]
    public void Classify_BodyWithKatakanaOnly_ReturnsJaViaContentKana()
    {
        // Tier 2: katakana also counts. Mid-dot U+30FB excluded so it
        // doesn't false-positive on Chinese names.
        var entry = new CommentaryEntry
        {
            CommentaryId = "C5",
            Language = null,
            Title = "信心銘解",
            Body = "カタカナ", // 4 katakana
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("ja", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.ContentKana, tag.Source);
    }

    [Fact]
    public void Classify_BodyWithTwoHiraganaQuote_StaysUnknown_ThresholdBelowThree()
    {
        // Tier 2 floor: a Chinese commentary that quotes a short Japanese
        // phrase 「これ」 (exactly 2 hiragana) must NOT be reclassified
        // as Japanese. Under the default-deny reframe sub-threshold
        // input falls through Tier 4 to "unknown" (NOT "zh-Hant" as in
        // the original Recon-D recommendation).
        var entry = new CommentaryEntry
        {
            CommentaryId = "C6",
            Language = null,
            Title = "信心銘", // pure CJK
            Body = "中文論文引用「これ」一詞。", // exactly 2 hiragana: こ, れ; everything else CJK + punctuation
        };

        // Sanity-check the fixture: exactly 2 kana under our definition.
        Assert.Equal(2, CjkMatchNormalizer.CountKana(entry.Body));

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("unknown", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.Default, tag.Source);
    }

    [Fact]
    public void Classify_TitleHasKowa講話_NoBody_ReturnsJaViaTitleKeyword()
    {
        // Tier 3: most FiM C* items have no OCR'd body. The genre
        // keyword 講話 in the title is the only signal we have.
        var entry = new CommentaryEntry
        {
            CommentaryId = "C7",
            Language = null,
            Title = "信心銘講話",
            Body = null,
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("ja", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.TitleKeyword, tag.Source);
        Assert.Contains("講話", tag.Evidence);
    }

    [Fact]
    public void Classify_TitleHasWayaku和譯_NoBody_ReturnsJaViaTitleKeyword()
    {
        // Tier 3: 和譯 / 和訳 marker family (Japanese translation).
        var entry = new CommentaryEntry
        {
            CommentaryId = "C8",
            Language = null,
            Title = "信心銘和譯",
            Body = null,
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("ja", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.TitleKeyword, tag.Source);
        Assert.Contains("和譯", tag.Evidence);
    }

    [Fact]
    public void Classify_PureCjkTitleAndBody_DefaultsToUnknown()
    {
        // Default-deny: no signals → "unknown", NOT "zh-Hant".
        // This is the SPEC v2 reframe — a Japanese commentary that fell
        // through every tier must NOT be silently surfaced as Chinese
        // to the reader. Chinese commentary must be Tier-1-tagged.
        var entry = new CommentaryEntry
        {
            CommentaryId = "C9",
            Language = null,
            Title = "信心銘", // pure CJK, no genre keyword
            Body = "信心銘是禪宗三祖僧璨大師所作。", // pure CJK
        };

        var tag = CommentaryLanguageClassifier.Classify(entry);

        Assert.Equal("unknown", tag.Bcp47);
        Assert.Equal(LanguageInferenceSource.Default, tag.Source);
        Assert.Equal("no Japanese signals", tag.Evidence);
    }

    [Fact]
    public void Classify_EmitsEvidenceString_HumanReadable()
    {
        // Admin inspectability: each Source produces an Evidence string
        // that's renderable in a tooltip / panel. No raw enums leak.
        var explicitEntry = new CommentaryEntry { CommentaryId = "E1", Language = "ja" };
        var kanaEntry = new CommentaryEntry { CommentaryId = "E2", Body = "これは日本語です。", Title = "" };
        var titleEntry = new CommentaryEntry { CommentaryId = "E3", Title = "信心銘講話" };
        var defaultEntry = new CommentaryEntry { CommentaryId = "E4", Title = "信心銘", Body = "純粹中文" };

        var e1 = CommentaryLanguageClassifier.Classify(explicitEntry);
        var e2 = CommentaryLanguageClassifier.Classify(kanaEntry);
        var e3 = CommentaryLanguageClassifier.Classify(titleEntry);
        var e4 = CommentaryLanguageClassifier.Classify(defaultEntry);

        Assert.False(string.IsNullOrWhiteSpace(e1.Evidence));
        Assert.False(string.IsNullOrWhiteSpace(e2.Evidence));
        Assert.False(string.IsNullOrWhiteSpace(e3.Evidence));
        Assert.False(string.IsNullOrWhiteSpace(e4.Evidence));

        // Per-source format expectations:
        Assert.Contains("language=", e1.Evidence);
        Assert.Contains("count=", e2.Evidence);
        Assert.Contains("genre marker", e3.Evidence);
        Assert.Contains("no Japanese signals", e4.Evidence);
    }

    [Fact]
    public void Classify_BcpTagNormalized_jaJpStaysJa()
    {
        // BCP-47 hygiene: classifier normalises primary subtag to
        // lowercase, script subtag (length 4) to Title-case.
        var upperJa = new CommentaryEntry { CommentaryId = "B1", Language = "JA" };
        var mixedZh = new CommentaryEntry { CommentaryId = "B2", Language = "zh-hant" };
        var regionTag = new CommentaryEntry { CommentaryId = "B3", Language = "ja-JP" };

        Assert.Equal("ja", CommentaryLanguageClassifier.Classify(upperJa).Bcp47);
        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.Classify(mixedZh).Bcp47);
        Assert.Equal("ja-JP", CommentaryLanguageClassifier.Classify(regionTag).Bcp47);
    }

    // ─────────────────────────────────────────────────────────────
    // Gap-fill facts (Wave 4 test-writer pass — RUN-20260512-1754).
    // Lock in BCP-47 normalization edges + GetInferenceTag's null-id
    // guard that none of the first 13 facts asserted directly.
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_BcpTagNormalized_AllUppercaseZhHantInputNormalizesToZhHant()
    {
        // The user-suggested mixed-case edge: an admin writes "ZH-HANT"
        // in the language field. The classifier (Tier 1 trusts the value
        // but normalises casing) must emit "zh-Hant" — both subtags
        // re-cased — so the downstream filter's case-insensitive prefix
        // match against the whitelist ["zh-Hant"] still matches even on
        // case-sensitive comparators future code might use.
        var allCaps = new CommentaryEntry { CommentaryId = "N1", Language = "ZH-HANT" };
        var allLower = new CommentaryEntry { CommentaryId = "N2", Language = "zh-hant" };
        var camel = new CommentaryEntry { CommentaryId = "N3", Language = "Zh-Hant" };

        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.Classify(allCaps).Bcp47);
        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.Classify(allLower).Bcp47);
        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.Classify(camel).Bcp47);

        // Sanity: NormalizeBcp47 is idempotent (applying it twice is a no-op).
        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.NormalizeBcp47("zh-Hant"));
        Assert.Equal("zh-Hant", CommentaryLanguageClassifier.NormalizeBcp47("ZH-HANT"));
        // FIXME: bug — NormalizeBcp47(null) returns "" (empty string) rather than null.
        // The docstring claims "Returns the input unchanged if null/empty" but the
        // implementation coerces null → empty via `tag ?? string.Empty`. Documenting
        // current behaviour here rather than flipping the contract.
        Assert.Equal("", CommentaryLanguageClassifier.NormalizeBcp47(null));
    }
}
