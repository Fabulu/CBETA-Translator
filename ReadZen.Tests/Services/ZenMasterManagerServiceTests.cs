using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class ZenMasterManagerServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;
    private readonly string _baseFilePath;

    public ZenMasterManagerServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-zen-master-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(_repoRoot);
        _baseFilePath = Path.Combine(_tempDir, "master-dates.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task LoadAsync_MergesBaseAndCommunityBySharedAlias()
    {
        await File.WriteAllTextAsync(_baseFilePath, BaseJson(), Encoding.UTF8);

        var service = new ZenMasterManagerService(new FakeMasterDatesService(new Dictionary<string, List<MasterDateEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = new()
            {
                new MasterDateEntry { Names = new() { "Linji", "\u81e8\u6fdf\u7fa9\u7384" }, Floruit = 851, Death = 867 }
            },
            ["bob"] = new()
            {
                new MasterDateEntry { Names = new() { "Dongshan Liangjie", "\u6d1e\u5c71\u826f\u4ef7" }, Floruit = 807, Death = 869 }
            }
        }));

        var catalog = await service.LoadAsync(_repoRoot, _baseFilePath);

        Assert.Equal(2, catalog.Records.Count);
        Assert.Equal(1, catalog.BundledRecordCount);
        Assert.Equal(1, catalog.CommunityOnlyRecordCount);
        Assert.Equal(2, catalog.CommunityVariantCount);

        var linji = Assert.Single(catalog.Records, r => r.Aliases.Contains("\u81e8\u6fdf\u7fa9\u7384"));
        Assert.True(linji.HasBase);
        Assert.Equal(1, linji.CommunityVariantCount);
        Assert.Contains("Linji", linji.Aliases);
        Assert.Contains("alice", linji.CommunityUsers);
    }

    [Fact]
    public async Task FindLandingMatch_PrefersRequestedCommunityUser()
    {
        await File.WriteAllTextAsync(_baseFilePath, BaseJson(), Encoding.UTF8);

        var service = new ZenMasterManagerService(new FakeMasterDatesService(new Dictionary<string, List<MasterDateEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = new()
            {
                new MasterDateEntry { Names = new() { "Linji", "\u81e8\u6fdf\u7fa9\u7384" }, Floruit = 851 }
            }
        }));

        var catalog = await service.LoadAsync(_repoRoot, _baseFilePath);
        var match = service.FindLandingMatch(catalog.Records, "Linji", "alice");

        Assert.NotNull(match);
        Assert.Equal("Linji Yixuan", match!.Record.CanonicalName);
        Assert.Equal("alice", match.Variant.Username);
        Assert.True(match.UsedPreferredUser);
    }

    private static string BaseJson() => """
{
  "masters": [
    {
      "names": ["Linji Yixuan", "\u81e8\u6fdf\u7fa9\u7384"],
      "floruit": 850,
      "death": 866
    }
  ]
}
""";

    private sealed class FakeMasterDatesService : IMasterDatesService
    {
        private readonly Dictionary<string, List<MasterDateEntry>> _entries;

        public FakeMasterDatesService(Dictionary<string, List<MasterDateEntry>> entries)
        {
            _entries = entries;
        }

        public Task WriteMasterDatesJsonlAsync(string communityDir, string username, List<MasterDateEntry> entries, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<Dictionary<string, List<MasterDateEntry>>> LoadAllCommunityMasterDatesAsync(string communityDir, CancellationToken ct = default)
            => Task.FromResult(_entries);
    }
}
