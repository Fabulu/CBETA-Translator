using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Pins the collections-dedup fix: <see cref="ScholarTabViewModel"/> deduplicates the owned
/// collections by Id at the single point of ingestion (LoadAsync), healing corrupted on-disk
/// data so the tree, the Collections list, and the next save all see one entry per Id.
/// </summary>
[Trait("Domain", "Scholar")]
public class ScholarCollectionsDedupTests
{
    private const BindingFlags PrivFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    private static ScholarTabViewModel MakeVm(StubScholarCollectionsService svc)
        => new ScholarTabViewModel(svc);

    private static void SetScholarContext(ScholarTabViewModel vm, string root, string username)
    {
        typeof(ScholarTabViewModel).GetField("_root", PrivFlags)!.SetValue(vm, root);
        typeof(ScholarTabViewModel).GetField("_username", PrivFlags)!.SetValue(vm, username);
        typeof(ScholarTabViewModel).GetField("_preferredUsername", PrivFlags)!.SetValue(vm, username);
        typeof(ScholarTabViewModel).GetField("_legacyUsername", PrivFlags)!.SetValue(vm, null);
        typeof(ScholarTabViewModel).GetField("_configLoadAttempted", PrivFlags)!.SetValue(vm, true);
    }

    private static async Task InvokeLoadAsync(ScholarTabViewModel vm)
    {
        var m = typeof(ScholarTabViewModel).GetMethod("LoadAsync", PrivFlags)!;
        await (Task)m.Invoke(vm, Array.Empty<object>())!;
    }

    private static ScholarCollection Coll(string id, string name)
        => new ScholarCollection { Id = id, Name = name };

    // ── the fixture "with one of each" is left fully intact (no over-dedup) ──

    [Fact]
    public async Task LoadAsync_WithOneOfEachId_KeepsEveryCollection()
    {
        var svc = new StubScholarCollectionsService
        {
            Collections =
            {
                Coll("a", "Alpha"),
                Coll("b", "Beta"),
                Coll("c", "Gamma"),
            }
        };
        var vm = MakeVm(svc);
        SetScholarContext(vm, Path.GetTempPath(), "tester");

        await InvokeLoadAsync(vm);

        Assert.Equal(3, vm.AllCollections.Count);
        Assert.Equal(new[] { "a", "b", "c" }, vm.AllCollections.Select(c => c.Id).OrderBy(x => x).ToArray());
        // No Id occurs more than once anywhere the UI consumes.
        Assert.Equal(vm.AllCollections.Count, vm.AllCollections.Select(c => c.Id).Distinct().Count());
        Assert.Equal(vm.Collections.Count, vm.Collections.Select(c => c.Id).Distinct().Count());
    }

    // ── duplicate-Id on-disk data is healed to distinct-by-Id at load ──

    [Fact]
    public async Task LoadAsync_WithDuplicateIds_DeduplicatesToDistinctById()
    {
        var svc = new StubScholarCollectionsService
        {
            Collections =
            {
                Coll("dup", "First"),
                Coll("dup", "Second"),      // duplicate Id (earlier persistence bug)
                Coll("dup", "Third"),       // duplicate Id
                Coll("solo", "Only"),
            }
        };
        var vm = MakeVm(svc);
        SetScholarContext(vm, Path.GetTempPath(), "tester");

        await InvokeLoadAsync(vm);

        Assert.Equal(2, vm.AllCollections.Count);
        Assert.Equal(vm.AllCollections.Count, vm.AllCollections.Select(c => c.Id).Distinct().Count());
        Assert.Contains(vm.AllCollections, c => c.Id == "dup");
        Assert.Contains(vm.AllCollections, c => c.Id == "solo");
    }

    [Fact]
    public async Task LoadAsync_Dedup_KeepsFirstEntryForEachId()
    {
        var svc = new StubScholarCollectionsService
        {
            Collections =
            {
                Coll("dup", "First"),
                Coll("dup", "Second"),
            }
        };
        var vm = MakeVm(svc);
        SetScholarContext(vm, Path.GetTempPath(), "tester");

        await InvokeLoadAsync(vm);

        var kept = Assert.Single(vm.AllCollections);
        Assert.Equal("First", kept.Name); // first entry per Id wins
    }

    // ── the Collections list (ComboBox source) is likewise free of duplicate Ids ──

    [Fact]
    public async Task LoadAsync_WithDuplicateIds_CollectionsListHasNoDuplicateIds()
    {
        var svc = new StubScholarCollectionsService
        {
            Collections =
            {
                Coll("x", "One"),
                Coll("x", "One-dup"),
                Coll("y", "Two"),
            }
        };
        var vm = MakeVm(svc);
        SetScholarContext(vm, Path.GetTempPath(), "tester");

        await InvokeLoadAsync(vm);

        Assert.Equal(vm.Collections.Count, vm.Collections.Select(c => c.Id).Distinct().Count());
        Assert.Equal(2, vm.Collections.Count);
    }
}
