using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Behavior tests for <see cref="CedictDictionaryService"/> — the trie-backed
/// hover-dictionary lookup. Exercises longest-match walking, single-char lookup,
/// traditional/simplified resolution, CJK gating (routed to CjkText.IsIdeograph
/// since v8.0.0), bounds/cap handling, and load-failure surfacing.
///
/// A tiny CC-CEDICT-format fixture is written to a temp file and the service is
/// pointed at it via the constructor's dictionaryPath override, so the tests are
/// hermetic and do not touch the shipped Assets/Dict/cedict_ts.u8.
/// </summary>
public sealed class CedictDictionaryServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly string _dictPath;

    public CedictDictionaryServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-cedict-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _dictPath = Path.Combine(_dir, "cedict-fixture.u8");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private const string Fixture =
        "# CC-CEDICT fixture header comment\n" +
        "\n" +
        "中 中 [zhong1] /middle/center/China (abbr.)/\n" +
        "中國 中国 [Zhong1 guo2] /China/Middle Kingdom/\n" +
        "中華人民共和國 中华人民共和国 [Zhong1 hua2 ren2 min2 gong4 he2 guo2] /People's Republic of China (PRC)/\n" +
        "國 国 [guo2] /country/nation/\n" +
        "this is not a valid cedict line\n";

    private async Task<CedictDictionaryService> LoadedServiceAsync()
    {
        await File.WriteAllTextAsync(_dictPath, Fixture, new UTF8Encoding(false));
        var svc = new CedictDictionaryService(_dictPath);
        await svc.EnsureLoadedAsync(CancellationToken.None);
        Assert.True(svc.IsLoaded);
        Assert.Null(svc.LastLoadError);
        return svc;
    }

    [Fact]
    public async Task LongestMatch_PrefersLongerHeadword()
    {
        var svc = await LoadedServiceAsync();

        // "中國歷史" — both "中" and "中國" are in the trie; longest must win.
        Assert.True(svc.TryLookupLongest("中國歷史", 0, out var match));
        Assert.Equal("中國", match.Headword);
        Assert.Equal(0, match.StartIndex);
        Assert.Equal(2, match.Length);
        Assert.Contains(match.Entries, e => e.Traditional == "中國");
    }

    [Fact]
    public async Task LongestMatch_FullPhrase_WalksDeepChain()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.TryLookupLongest("中華人民共和國萬歲", 0, out var match));
        Assert.Equal("中華人民共和國", match.Headword);
        Assert.Equal(7, match.Length);
    }

    [Fact]
    public async Task LongestMatch_HonorsStartIndex()
    {
        var svc = await LoadedServiceAsync();

        // Latin prefix, phrase begins at index 1.
        Assert.True(svc.TryLookupLongest("x中國", 1, out var match));
        Assert.Equal("中國", match.Headword);
        Assert.Equal(1, match.StartIndex);
    }

    [Fact]
    public async Task LongestMatch_MaxLenCap_LimitsWalkDepth()
    {
        var svc = await LoadedServiceAsync();

        // maxLen=1 caps the walk to a single char, so only "中" can match.
        Assert.True(svc.TryLookupLongest("中國", 0, out var match, maxLen: 1));
        Assert.Equal("中", match.Headword);
        Assert.Equal(1, match.Length);
    }

    [Fact]
    public async Task LongestMatch_SimplifiedText_ResolvesViaSimplifiedTrie()
    {
        var svc = await LoadedServiceAsync();

        // Simplified "中国" only lives in the simplified trie.
        Assert.True(svc.TryLookupLongest("中国人", 0, out var match));
        Assert.Equal("中国", match.Headword);
        Assert.Equal(2, match.Length);
    }

    [Fact]
    public async Task LongestMatch_NonCjkFirstChar_ReturnsFalse()
    {
        var svc = await LoadedServiceAsync();
        Assert.False(svc.TryLookupLongest("abc", 0, out _));
    }

    [Fact]
    public async Task LongestMatch_UnknownCjk_ReturnsFalse()
    {
        var svc = await LoadedServiceAsync();
        // U+9F98 is a CJK ideograph but absent from the fixture.
        Assert.False(svc.TryLookupLongest("龘龘", 0, out _));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]   // == length
    [InlineData(99)]  // > length
    public async Task LongestMatch_OutOfRangeStartIndex_ReturnsFalse(int startIndex)
    {
        var svc = await LoadedServiceAsync();
        Assert.False(svc.TryLookupLongest("中國歷史", startIndex, out _));
    }

    [Fact]
    public async Task LookupChar_KnownIdeograph_ReturnsEntries()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.TryLookupChar('國', out var entries));
        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.Simplified == "国");
        Assert.Contains(entries, e => e.Pinyin == "guo2");
    }

    [Fact]
    public async Task LookupChar_NonCjk_ReturnsFalseAndEmpty()
    {
        var svc = await LoadedServiceAsync();

        Assert.False(svc.TryLookupChar('Z', out var entries));
        Assert.Empty(entries);
    }

    [Fact]
    public async Task LookupChar_MultiCharHeadwordFirstChar_MatchesSingleCharEntry()
    {
        var svc = await LoadedServiceAsync();

        // '中' is itself a headword; single-char lookup must resolve the '中' terminal,
        // not the multi-char "中國" node.
        Assert.True(svc.TryLookupChar('中', out var entries));
        Assert.Contains(entries, e => e.Traditional == "中");
    }

    [Fact]
    public void TryLookupLongest_BeforeLoad_ReturnsFalse()
    {
        var svc = new CedictDictionaryService(_dictPath);
        Assert.False(svc.IsLoaded);
        Assert.False(svc.TryLookupLongest("中國", 0, out _));
        Assert.False(svc.TryLookupChar('中', out _));
    }

    [Fact]
    public async Task ParsedEntry_ExposesAllSenses()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.TryLookupChar('中', out var entries));
        var mid = Assert.Single(entries, e => e.Traditional == "中");
        // "/middle/center/China (abbr.)/" → three senses.
        Assert.Equal(3, mid.Senses.Length);
        Assert.Equal("middle", mid.Senses[0]);
    }

    [Fact]
    public async Task EnsureLoaded_MissingFile_SurfacesErrorAndStaysUnloaded()
    {
        var svc = new CedictDictionaryService(Path.Combine(_dir, "does-not-exist.u8"));

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => svc.EnsureLoadedAsync(CancellationToken.None));

        Assert.False(svc.IsLoaded);
        Assert.NotNull(svc.LastLoadError);
    }

    [Fact]
    public async Task EnsureLoaded_IsIdempotent_SecondCallIsNoOp()
    {
        var svc = await LoadedServiceAsync();
        // Second call should short-circuit (already loaded) and not throw.
        await svc.EnsureLoadedAsync(CancellationToken.None);
        Assert.True(svc.TryLookupLongest("中國", 0, out _));
    }
}
