using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class ZenMasterManagerWindowViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;
    private readonly string _baseFilePath;

    public ZenMasterManagerWindowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cbeta-zen-master-vm-test-" + Guid.NewGuid().ToString("N")[..8]);
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
    public async Task ApplyLanding_BeforeLoad_SelectsRequestedMasterAfterLoad()
    {
        await WriteBaseFileAsync();
        var vm = CreateViewModel();

        vm.ApplyLanding("Linji", "alice");
        await vm.LoadAsync();

        Assert.NotNull(vm.SelectedMaster);
        Assert.Equal("Linji Yixuan", vm.SelectedMaster!.CanonicalName);
        Assert.Contains("alice", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FilterText_MatchesAliasAndCommunityUser()
    {
        await WriteBaseFileAsync();
        var vm = CreateViewModel();
        await vm.LoadAsync();

        vm.FilterText = "Linji";
        Assert.Single(vm.Masters);

        vm.FilterText = "alice";
        Assert.Single(vm.Masters);
        Assert.Equal("Linji Yixuan", vm.Masters[0].CanonicalName);
    }

    [Fact]
    public async Task LoadAsync_UsesSummaryStatusWhenNoLandingRequested()
    {
        await WriteBaseFileAsync();
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Contains("Loaded 2 Zen master(s)", vm.StatusText);
        Assert.Contains("1 bundled", vm.StatusText);
        Assert.Contains("1 community-only", vm.StatusText);
    }

    private ZenMasterManagerWindowViewModel CreateViewModel()
    {
        var service = new ZenMasterManagerService(new FakeMasterDatesService(new Dictionary<string, List<MasterDateEntry>>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = new()
            {
                new MasterDateEntry { Names = new() { "Linji", "\u81e8\u6fdf\u7fa9\u7384" }, Floruit = 851 }
            },
            ["bob"] = new()
            {
                new MasterDateEntry { Names = new() { "Dongshan Liangjie", "\u6d1e\u5c71\u826f\u4ef7" }, Floruit = 807 }
            }
        }));

        return new ZenMasterManagerWindowViewModel(service, _repoRoot, _baseFilePath);
    }

    private Task WriteBaseFileAsync()
    {
        return File.WriteAllTextAsync(_baseFilePath, """
{
  "masters": [
    {
      "names": ["Linji Yixuan", "\u81e8\u6fdf\u7fa9\u7384"],
      "floruit": 850,
      "death": 866
    }
  ]
}
""", Encoding.UTF8);
    }

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
