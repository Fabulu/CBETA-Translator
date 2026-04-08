using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class SearchExportServiceTests : IDisposable
{
    private readonly SearchExportService _svc = new();
    private readonly string _tempDir;

    public SearchExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "search-export-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private string TempFile(string name) => Path.Combine(_tempDir, name);

    private static SearchExportSnapshot MakeSnapshot()
    {
        var group = new SearchResultGroup
        {
            RelPath = "T/T48/T48n2005.xml",
            DisplayName = "Blue Cliff",
            Tooltip = "T48n2005",
            Status = TranslationStatus.Yellow,
            HitsOriginal = 1,
            HitsTranslated = 1,
            Children = new List<SearchResultChild>
            {
                new()
                {
                    RelPath = "T/T48/T48n2005.xml",
                    Side = SearchSide.Original,
                    Hit = new SearchHit { Index = 12, Left = "左文", Match = "中", Right = "右文" },
                    SecondaryHit = new SearchHit { Index = 3, Left = "left", Match = "match", Right = "right" }
                }
            }
        };

        return new SearchExportSnapshot
        {
            Query = "dharma",
            SearchOriginal = true,
            SearchTranslated = true,
            ZenOnly = true,
            StatusFilter = "Green (done)",
            TagFilter = "Practice",
            ContextLabel = "40 chars",
            ExportedUtc = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
            Groups = new[] { group }
        };
    }

    [Fact]
    public async Task HtmlExport_IncludesMetadataAndBilingualRows()
    {
        var path = TempFile("search.html");

        await _svc.ExportAsync(path, MakeSnapshot(), SearchExportFormat.Html);

        var html = await File.ReadAllTextAsync(path);
        Assert.Contains("Search Results", html);
        Assert.Contains("Query", html);
        Assert.Contains("dharma", html);
        Assert.Contains("Blue Cliff", html);
        Assert.Contains("左文", html);
        Assert.Contains("left", html);
        Assert.Contains("match", html);
        Assert.Contains("right", html);
    }

    [Fact]
    public async Task DelimitedExport_ProducesHeaderAndFlattenedRows()
    {
        var path = TempFile("search.tsv");

        await _svc.ExportAsync(path, MakeSnapshot(), SearchExportFormat.Tsv);

        var tsv = await File.ReadAllTextAsync(path);
        Assert.StartsWith("query\tsearch_original\tsearch_translated\tzen_only", tsv);
        Assert.Contains("dharma", tsv);
        Assert.Contains("Blue Cliff", tsv);
        Assert.Contains("match", tsv);
    }

    [Fact]
    public async Task JsonExport_ProducesStructuredPayload()
    {
        var path = TempFile("search.json");

        await _svc.ExportAsync(path, MakeSnapshot(), SearchExportFormat.Json);

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var root = doc.RootElement;
        Assert.Equal("search-results/v1", root.GetProperty("format").GetString());
        Assert.Equal("dharma", root.GetProperty("state").GetProperty("query").GetString());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("group_count").GetInt32());
        Assert.Single(root.GetProperty("groups").EnumerateArray());
    }
}