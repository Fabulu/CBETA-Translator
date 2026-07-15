using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class DictionaryEditorWindowViewModelTests : IDisposable
{
    private readonly string _tempDir;

    public DictionaryEditorWindowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dict-editor-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    /// <summary>Minimal evidence stub: returns a fixed DictionaryEvidence, no index required.</summary>
    private sealed class FakeEvidenceService : IDictionaryEvidenceService
    {
        public DictionaryEvidence Result { get; set; } = new();

        public Task<DictionaryEvidence> GetEvidenceAsync(
            string term, string originalDir, string translatedDir,
            string? masterCacheDir = null,
            IReadOnlyCollection<string>? restrictToRelPaths = null,
            int maxTexts = 50, int samplesPerText = 3,
            CancellationToken ct = default)
            => Task.FromResult(Result);
    }

    private DictionaryEditorWindowViewModel MakeVm(FakeEvidenceService? evidence = null)
    {
        var vm = new DictionaryEditorWindowViewModel(new DictionaryStore(), _tempDir);
        vm.SetContext(evidence ?? new FakeEvidenceService(), "orig", "trans", masterCacheDir: null);
        vm.SetUsername("tester");
        return vm;
    }

    [Fact]
    public async Task Load_Edit_Save_RoundTrips_Through_Store()
    {
        var vm = MakeVm();
        await vm.LoadCommand.ExecuteAsync(null); // empty repo

        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "祖師"; // 祖師
        Assert.NotNull(vm.SelectedSense);
        vm.SelectedSense!.PreferredTarget = "ancestral master";
        vm.SelectedSense.Explanation = "the founding teacher";
        vm.SelectedSense.SelectedStatusIndex = 1;       // allowed
        vm.SelectedSense.SelectedValidationIndex = 1;   // multi-source
        vm.SelectedSense.AlternatesText = "patriarch\nfounder";
        vm.SelectedSense.SearchAliasesText = "ancestral teacher\nlineage founder";

        vm.SearchQuery = "lineage founder";
        Assert.Single(vm.FilteredEntries);
        vm.SearchQuery = "";

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.Saved);

        // Reload with a fresh store to prove it persisted.
        var reloaded = await new DictionaryStore().LoadAsync(_tempDir);
        var entry = Assert.Single(reloaded.Entries);
        Assert.Equal("祖師", entry.SourceTerm);
        Assert.Equal(DictionaryStore.ComputeId("祖師"), entry.Id);

        var sense = Assert.Single(entry.Senses);
        Assert.Equal("ancestral master", sense.PreferredTarget);
        Assert.Equal("the founding teacher", sense.Explanation);
        Assert.Equal("allowed", sense.Status);
        Assert.Equal("multi-source", sense.Validation);
        Assert.Equal(new[] { "patriarch", "founder" }, sense.AlternateTargets);
        Assert.Equal(new[] { "ancestral teacher", "lineage founder" }, sense.SearchAliases);
    }

    [Fact]
    public async Task NewEntry_And_DeleteEntry_Adjust_Collection()
    {
        var vm = MakeVm();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "佛"; // 佛
        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "法"; // 法
        Assert.Equal(2, vm.Entries.Count);

        var toDelete = vm.SelectedEntry;
        vm.DeleteEntryCommand.Execute(null);
        Assert.Single(vm.Entries);
        Assert.DoesNotContain(toDelete, vm.Entries);
    }

    [Fact]
    public async Task AddSense_And_RemoveSense_Sync_Entry_And_Editors()
    {
        var vm = MakeVm();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "水爹牛"; // 水牯牛
        Assert.Single(vm.Senses);                    // one corpus-wide sense from New
        Assert.Single(vm.SelectedEntry!.Senses);

        vm.AddSenseCommand.Execute(null);
        Assert.Equal(2, vm.Senses.Count);
        Assert.Equal(2, vm.SelectedEntry!.Senses.Count);

        // The newly added sense is selected; give it a master, then remove it.
        vm.SelectedSense!.MasterName = "Nanquan Puyuan";
        Assert.Equal("Nanquan Puyuan", vm.SelectedSense.Model.MasterName);
        Assert.Equal("Nanquan Puyuan", vm.SelectedSense.Model.SenseKey);

        vm.RemoveSenseCommand.Execute(null);
        Assert.Single(vm.Senses);
        Assert.Single(vm.SelectedEntry!.Senses);
    }

    [Fact]
    public async Task Curate_And_Uncurate_Toggle_Occurrence_On_Sense()
    {
        var vm = MakeVm();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "無"; // 無
        var sense = vm.SelectedSense!;
        Assert.Empty(sense.CuratedOccurrences);

        var occ = new DictOccurrence { RelPath = "T/T48/T48n2005.xml", Kwic = "無門關", Curated = false };

        vm.CurateOccurrenceCommand.Execute(occ);
        var curated = Assert.Single(sense.CuratedOccurrences);
        Assert.True(curated.Curated);
        Assert.Single(sense.Model.Occurrences);
        Assert.True(sense.Model.Occurrences[0].Curated);

        // Curating the same occurrence again is a no-op (dedup by RelPath + Kwic).
        vm.CurateOccurrenceCommand.Execute(occ);
        Assert.Single(sense.CuratedOccurrences);

        vm.UncurateOccurrenceCommand.Execute(curated);
        Assert.Empty(sense.CuratedOccurrences);
        Assert.Empty(sense.Model.Occurrences);
    }

    [Fact]
    public async Task Save_Persists_Only_Curated_Occurrences_On_Sense()
    {
        var vm = MakeVm();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.NewEntryCommand.Execute(null);
        vm.SourceTerm = "道"; // 道
        vm.SelectedSense!.PreferredTarget = "the Way";
        vm.CurateOccurrenceCommand.Execute(new DictOccurrence
        {
            RelPath = "T/T48/T48n2003.xml",
            Kwic = "平常心是道",
            Curated = false,
        });

        await vm.SaveCommand.ExecuteAsync(null);

        var reloaded = await new DictionaryStore().LoadAsync(_tempDir);
        var sense = Assert.Single(Assert.Single(reloaded.Entries).Senses);
        var persisted = Assert.Single(sense.Occurrences);
        Assert.True(persisted.Curated);
        Assert.Equal("T/T48/T48n2003.xml", persisted.RelPath);
    }
}
