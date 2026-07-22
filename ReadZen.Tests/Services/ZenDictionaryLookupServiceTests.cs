using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class ZenDictionaryLookupServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DictionaryStore _store;

    public ZenDictionaryLookupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-zendict-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        // The store reads/writes under the per-test temp root passed to Save/LoadAsync, so
        // it never touches any file next to the test host exe.
        _store = new DictionaryStore();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // 無門 (single sense) and 無門關 (multi-sense) share a prefix so we can prove longest-match wins.
    private static DictionaryFile SampleFile() => new()
    {
        SchemaVersion = 2,
        Entries = new List<DictionaryEntry>
        {
            new()
            {
                Id = DictionaryStore.ComputeId("無門"),
                SourceTerm = "無門",
                Senses = new List<DictionarySense>
                {
                    new()
                    {
                        SenseKey = null,
                        PreferredTarget = "no gate",
                        SearchAliases = new List<string> { "gateless" },
                        Status = "preferred",
                        Validation = "multi-source"
                    }
                }
            },
            new()
            {
                Id = DictionaryStore.ComputeId("無門關"),
                SourceTerm = "無門關",
                Senses = new List<DictionarySense>
                {
                    new()
                    {
                        SenseKey = null,
                        PreferredTarget = "The Gateless Barrier",
                        SearchAliases = new List<string> { "gateless barrier", "wumenguan" },
                        Status = "preferred",
                        Validation = "multi-source"
                    },
                    new()
                    {
                        SenseKey = "Wumen Huikai",
                        MasterName = "Wumen Huikai",
                        PreferredTarget = "Wumen's collection of forty-eight cases",
                        SearchAliases = new List<string> { "gateless gate" },
                        Status = "preferred",
                        Validation = "multi-source"
                    }
                }
            }
        }
    };

    private async Task<IZenDictionaryLookup> LoadedServiceAsync()
    {
        await _store.SaveAsync(_tempDir, SampleFile());
        var svc = new ZenDictionaryLookupService(_store);
        await svc.EnsureLoadedAsync(_tempDir);
        return svc;
    }

    [Fact]
    public void NotLoaded_ReturnsFalse()
    {
        var svc = new ZenDictionaryLookupService(_store);
        Assert.False(svc.IsLoaded);
        Assert.False(svc.TryLookupExact("無門", out _));
        Assert.False(svc.TryLookupLongest("無門", 0, out _));
    }

    [Fact]
    public async Task ExactLookup_ByHeadTerm_Hits()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.IsLoaded);
        Assert.True(svc.TryLookupExact("無門", out var entry));
        Assert.Equal("無門", entry.SourceTerm);
        Assert.Equal("no gate", entry.Senses[0].PreferredTarget);
    }

    [Fact]
    public async Task ExactLookup_ByAlias_ResolvesToOwningEntry()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.TryLookupExact("wumenguan", out var entry));
        Assert.Equal("無門關", entry.SourceTerm);
    }

    [Fact]
    public async Task LongestMatch_PrefersLongerTerm()
    {
        var svc = await LoadedServiceAsync();

        // "無門關" contains the shorter head term "無門" as a prefix; the longer must win.
        Assert.True(svc.TryLookupLongest("無門關是第一則", 0, out var match));
        Assert.Equal("無門關", match.Headword);
        Assert.Equal(3, match.Length);
        Assert.Equal(0, match.StartIndex);
        Assert.Equal("無門關", match.Entry.SourceTerm);
    }

    [Fact]
    public async Task LongestMatch_FallsBackToShorterTerm_WhenLongerAbsent()
    {
        var svc = await LoadedServiceAsync();

        // "無門" followed by a non-continuing char yields the shorter head term only.
        Assert.True(svc.TryLookupLongest("無門也", 0, out var match));
        Assert.Equal("無門", match.Headword);
        Assert.Equal(2, match.Length);
    }

    [Fact]
    public async Task LongestMatch_Miss_ReturnsFalse()
    {
        var svc = await LoadedServiceAsync();

        Assert.False(svc.TryLookupLongest("東西南北", 0, out _));
        Assert.False(svc.TryLookupExact("不存在的詞", out _));
    }

    [Fact]
    public async Task MultiSenseEntry_ReturnsWholeEntry_AllSenses()
    {
        var svc = await LoadedServiceAsync();

        Assert.True(svc.TryLookupLongest("無門關", 0, out var match));
        Assert.Equal(2, match.Entry.Senses.Count);
        Assert.Null(match.Entry.Senses[0].SenseKey);           // corpus-wide
        Assert.Equal("Wumen Huikai", match.Entry.Senses[1].MasterName); // master-specific
    }

    [Fact]
    public async Task EmptyStore_LoadsGracefully_AndMisses()
    {
        // No termbase files written to the temp dir at all.
        var svc = new ZenDictionaryLookupService(_store);
        await svc.EnsureLoadedAsync(_tempDir);

        Assert.True(svc.IsLoaded);
        Assert.False(svc.TryLookupExact("無門", out _));
        Assert.False(svc.TryLookupLongest("無門關", 0, out _));
    }
}
