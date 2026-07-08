using System.Text.Json;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Byte-compatibility contract for the consolidated <see cref="TmRow"/> (audit
/// P3.6). The TM jsonl files are community-shared: every writer's output must
/// stay byte-identical to what its private row class produced. Fixture lines
/// are captured from real on-disk data / the exact serializer options of each
/// service. If one of these fails after a TmRow change, the change breaks the
/// shared file format — do not "fix the test".
/// </summary>
public class TmRowSerializationCompatTests
{
    // TranslationReviewService / CommunityDataService writer options
    private static readonly JsonSerializerOptions RelaxedOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // TranslationAssistantBuildService options (default encoder → CJK escaped)
    private static readonly JsonSerializerOptions DefaultEncoderOpts = new()
    {
        WriteIndented = false
    };

    // Real line from translation-memory.approved.jsonl (legacy writer emitted
    // BlockNumber:0 and an explicit null WrittenUtc; raw CJK via relaxed encoder).
    private const string RealApprovedLine =
        "{\"SourceText\":\"師示眾曰。凡情聖見。是金鎖玄路。直須回互。\",\"TargetText\":\"The Master instructed the assembly: Dualistic thinking - whether ordinary or enlightened - is golden shackles on the mysterious path. You must directly realize guest and host freely changing places.\",\"RelPath\":\"T/T47/T47n1987B.xml\",\"BlockNumber\":0,\"ReviewStatus\":\"Approved\",\"Translator\":\"User\",\"WrittenUtc\":null}";

    [Fact]
    public void RealApprovedLine_RoundTripsByteIdentically()
    {
        var row = JsonSerializer.Deserialize<TmRow>(RealApprovedLine, RelaxedOpts)!;
        Assert.Equal(0, row.BlockNumber); // explicit 0 stays 0, not null

        var rewritten = JsonSerializer.Serialize(row, RelaxedOpts);

        Assert.Equal(RealApprovedLine, rewritten);
    }

    [Fact]
    public void ReviewWriterShape_NoBlockNumber_OmitsTheKey()
    {
        // What TranslationReviewService's 6-property row wrote: no BlockNumber key.
        var row = new TmRow
        {
            SourceText = "無門曰",
            TargetText = "Wumen says",
            RelPath = "T/T48/T48n2005.xml",
            ReviewStatus = "Approved",
            Translator = "User",
            WrittenUtc = null
        };

        var json = JsonSerializer.Serialize(row, RelaxedOpts);

        Assert.Equal(
            "{\"SourceText\":\"無門曰\",\"TargetText\":\"Wumen says\",\"RelPath\":\"T/T48/T48n2005.xml\",\"ReviewStatus\":\"Approved\",\"Translator\":\"User\",\"WrittenUtc\":null}",
            json);
    }

    [Fact]
    public void AssistantWriterShape_DefaultEncoder_EscapesCjkExactlyAsBefore()
    {
        // TranslationAssistantBuildService uses the default encoder: CJK → \uXXXX.
        var row = new TmRow
        {
            SourceText = "師",
            TargetText = "master",
            RelPath = "r",
            ReviewStatus = "AI baseline",
            Translator = "AutoImport"
        };

        var json = JsonSerializer.Serialize(row, DefaultEncoderOpts);

        Assert.Equal(
            "{\"SourceText\":\"\\u5E2B\",\"TargetText\":\"master\",\"RelPath\":\"r\",\"ReviewStatus\":\"AI baseline\",\"Translator\":\"AutoImport\",\"WrittenUtc\":null}",
            json);
    }

    [Fact]
    public void LineWithoutBlockNumber_ReadsAsNull_AndNormalizesToZeroWhereRequired()
    {
        // Rows written by the 6-property writers have no BlockNumber key.
        var line = "{\"SourceText\":\"a\",\"TargetText\":\"b\",\"RelPath\":\"r\",\"ReviewStatus\":\"Approved\",\"Translator\":\"t\",\"WrittenUtc\":null}";
        var row = JsonSerializer.Deserialize<TmRow>(line, RelaxedOpts)!;

        Assert.Null(row.BlockNumber);

        // CommunityDataService's rewrite path normalizes null→0 so its merged
        // output keeps the historical "BlockNumber":0 for such rows:
        row.BlockNumber ??= 0;
        var rewritten = JsonSerializer.Serialize(row, RelaxedOpts);
        Assert.Contains("\"BlockNumber\":0", rewritten);
    }
}
