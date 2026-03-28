using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for MasterDatesService: JSONL read/write, filename sanitization,
/// base overlap detection, and name-sharing logic.
/// </summary>
public class MasterDatesServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MasterDatesService _svc = new();

    public MasterDatesServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cbeta-md-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ---- 1. WriteMasterDatesJsonlAsync — correct JSONL format ----

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_WritesCorrectJsonlFormat()
    {
        var entries = new List<MasterDateEntry>
        {
            new() { Names = new List<string> { "Linji Yixuan", "臨濟義玄" }, Floruit = 850, Death = 866 },
            new() { Names = new List<string> { "Zhaozhou Congshen", "趙州從諗" }, Floruit = 778, Death = 897 }
        };

        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "testuser", entries);

        var path = Path.Combine(_tempDir, "testuser.jsonl");
        Assert.True(File.Exists(path));

        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
        Assert.Equal(2, lines.Length);

        // Each line must be valid JSON
        var first = JsonSerializer.Deserialize<MasterDateEntry>(lines[0],
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(first);
        Assert.Equal(850, first!.Floruit);
        Assert.Equal(866, first.Death);
        Assert.Contains("Linji Yixuan", first.Names);
        Assert.Contains("臨濟義玄", first.Names);

        var second = JsonSerializer.Deserialize<MasterDateEntry>(lines[1],
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(second);
        Assert.Equal(778, second!.Floruit);
    }

    // ---- 2. WriteMasterDatesJsonlAsync — sanitizes filename ----

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_SanitizesFilename()
    {
        var entries = new List<MasterDateEntry>
        {
            new() { Names = new List<string> { "TestMaster" }, Floruit = 800 }
        };

        // Username with path-traversal attempt
        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "user name", entries);

        // Spaces are stripped by SanitizeFilename, so file should be "username.jsonl"
        var path = Path.Combine(_tempDir, "username.jsonl");
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_RejectsPathTraversal()
    {
        var entries = new List<MasterDateEntry>
        {
            new() { Names = new List<string> { "TestMaster" }, Floruit = 800 }
        };

        // After sanitization of "../evil", dots and slashes are stripped.
        // But if somehow a path outside the dir is produced, the fullPath check catches it.
        // With the current sanitizer, "../evil" becomes "evil" which is safe.
        // Let's verify that the file ends up inside the community dir.
        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "../evil", entries);

        // The sanitizer strips '.' and '/', so the filename becomes "evil.jsonl"
        var expectedPath = Path.Combine(_tempDir, "evil.jsonl");
        Assert.True(File.Exists(expectedPath));

        // Verify no file was created outside the temp dir
        var parentDir = Path.GetDirectoryName(_tempDir)!;
        Assert.False(File.Exists(Path.Combine(parentDir, "evil.jsonl")));
    }

    // ---- 3. LoadAllCommunityMasterDatesAsync — loads multiple users ----

    [Fact]
    public async Task LoadAllCommunityMasterDatesAsync_LoadsMultipleUsers()
    {
        // Write two user files
        var entriesAlice = new List<MasterDateEntry>
        {
            new() { Names = new List<string> { "Master A", "大師A" }, Floruit = 700 }
        };
        var entriesBob = new List<MasterDateEntry>
        {
            new() { Names = new List<string> { "Master B", "大師B" }, Floruit = 800 },
            new() { Names = new List<string> { "Master C" }, Floruit = 900, Death = 950 }
        };

        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "alice", entriesAlice);
        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "bob", entriesBob);

        var result = await _svc.LoadAllCommunityMasterDatesAsync(_tempDir);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("alice"));
        Assert.True(result.ContainsKey("bob"));
        Assert.Single(result["alice"]);
        Assert.Equal(2, result["bob"].Count);
    }

    // ---- 4. LoadAllCommunityMasterDatesAsync — empty dir returns empty ----

    [Fact]
    public async Task LoadAllCommunityMasterDatesAsync_EmptyDir_ReturnsEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var result = await _svc.LoadAllCommunityMasterDatesAsync(emptyDir);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAllCommunityMasterDatesAsync_NonExistentDir_ReturnsEmpty()
    {
        var result = await _svc.LoadAllCommunityMasterDatesAsync(
            Path.Combine(_tempDir, "does-not-exist"));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ---- 5. OverlapsWithBase — detects Chinese name overlap ----

    [Fact]
    public void OverlapsWithBase_DetectsChineseNameOverlap()
    {
        var baseNames = new HashSet<string>(StringComparer.Ordinal) { "臨濟義玄", "Linji Yixuan" };
        var entry = new MasterDateEntry
        {
            Names = new List<string> { "Linji", "臨濟義玄" },
            Floruit = 850
        };

        Assert.True(MasterDatesService.OverlapsWithBase(entry, baseNames));
    }

    [Fact]
    public void OverlapsWithBase_DetectsPinyinOverlapCaseInsensitive()
    {
        var baseNames = new HashSet<string>(StringComparer.Ordinal) { "Linji Yixuan" };
        var entry = new MasterDateEntry
        {
            Names = new List<string> { "linji yixuan" },
            Floruit = 850
        };

        // Pinyin comparison is case-insensitive
        Assert.True(MasterDatesService.OverlapsWithBase(entry, baseNames));
    }

    // ---- 6. OverlapsWithBase — no false positives for unrelated names ----

    [Fact]
    public void OverlapsWithBase_NoFalsePositivesForUnrelatedNames()
    {
        var baseNames = new HashSet<string>(StringComparer.Ordinal) { "臨濟義玄", "Linji Yixuan" };
        var entry = new MasterDateEntry
        {
            Names = new List<string> { "Dongshan Liangjie", "洞山良价" },
            Floruit = 807,
            Death = 869
        };

        Assert.False(MasterDatesService.OverlapsWithBase(entry, baseNames));
    }

    [Fact]
    public void OverlapsWithBase_SkipsSingleCjkCharNames()
    {
        // A single CJK character should not trigger an overlap
        var baseNames = new HashSet<string>(StringComparer.Ordinal) { "佛" };
        var entry = new MasterDateEntry
        {
            Names = new List<string> { "佛" },
            Floruit = 500
        };

        // Single CJK char (cjkCount < 2) is skipped
        Assert.False(MasterDatesService.OverlapsWithBase(entry, baseNames));
    }

    // ---- 7. SharesAnyName — detects same master across entries ----

    [Fact]
    public void SharesAnyName_DetectsSameMasterByChineseName()
    {
        var a = new MasterDateEntry
        {
            Names = new List<string> { "Linji Yixuan", "臨濟義玄" },
            Floruit = 850
        };
        var b = new MasterDateEntry
        {
            Names = new List<string> { "臨濟義玄", "臨濟" },
            Floruit = 851
        };

        Assert.True(MasterDatesService.SharesAnyName(a, b));
    }

    [Fact]
    public void SharesAnyName_DetectsSameMasterByPinyinCaseInsensitive()
    {
        var a = new MasterDateEntry
        {
            Names = new List<string> { "Linji Yixuan" },
            Floruit = 850
        };
        var b = new MasterDateEntry
        {
            Names = new List<string> { "linji yixuan" },
            Floruit = 851
        };

        Assert.True(MasterDatesService.SharesAnyName(a, b));
    }

    [Fact]
    public void SharesAnyName_ReturnsFalseForDifferentMasters()
    {
        var a = new MasterDateEntry
        {
            Names = new List<string> { "Linji Yixuan", "臨濟義玄" },
            Floruit = 850
        };
        var b = new MasterDateEntry
        {
            Names = new List<string> { "Dongshan Liangjie", "洞山良价" },
            Floruit = 807
        };

        Assert.False(MasterDatesService.SharesAnyName(a, b));
    }

    // ---- Edge cases ----

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_EmptyEntries_WritesEmptyFile()
    {
        await _svc.WriteMasterDatesJsonlAsync(_tempDir, "emptyuser", new List<MasterDateEntry>());

        var path = Path.Combine(_tempDir, "emptyuser.jsonl");
        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Equal("", content);
    }

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_NullEntries_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _svc.WriteMasterDatesJsonlAsync(_tempDir, "user", null!));
    }

    [Fact]
    public async Task WriteMasterDatesJsonlAsync_EmptyUsername_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.WriteMasterDatesJsonlAsync(_tempDir, "", new List<MasterDateEntry>()));
    }

    [Fact]
    public async Task LoadAllCommunityMasterDatesAsync_SkipsMalformedLines()
    {
        var path = Path.Combine(_tempDir, "baduser.jsonl");
        await File.WriteAllTextAsync(path, "not-json\n{\"Names\":[\"Valid\"],\"Floruit\":800}\n");

        var result = await _svc.LoadAllCommunityMasterDatesAsync(_tempDir);

        Assert.Single(result);
        Assert.Single(result["baduser"]);
        Assert.Equal(800, result["baduser"][0].Floruit);
    }

    [Fact]
    public void SharesAnyName_SkipsSingleCjkCharInBothEntries()
    {
        var a = new MasterDateEntry { Names = new List<string> { "佛" } };
        var b = new MasterDateEntry { Names = new List<string> { "佛" } };

        // Single CJK char entries should be skipped (cjkCount < 2)
        Assert.False(MasterDatesService.SharesAnyName(a, b));
    }
}
