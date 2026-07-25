using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Pins the graph-add-dict fix's functional core: the research graph's "add" surface
/// exposes a dictionary-entry (term) path. The window-side menu item ("Dictionary entry
/// (term)") persists a <see cref="DictionaryEntryRef"/> into the collection; this test
/// exercises the data path that feeds — a collection carrying a DictionaryEntryRef
/// materializes a <see cref="ScholarNodeType.TermbaseEntry"/> node when the VM rebuilds,
/// so the added dictionary entry survives reload. (The menu label itself lives in
/// window-only code that needs a windowing platform + App.Services and is not unit-testable;
/// see the returned notes.)
/// </summary>
[Trait("Domain", "Scholar")]
public class ResearchGraphDictionaryEntryTests
{
    private static ScholarCollection MakeCollection(string id = "col-1")
        => new ScholarCollection { Id = id, Name = "Test Collection" };

    private static ResearchGraphViewModel MakeVm(ScholarCollection col)
        => new ResearchGraphViewModel(col, new List<ScholarCollection> { col });

    [Fact]
    public void RebuildGraph_WithDictionaryEntryRef_MaterializesTermbaseEntryNode()
    {
        var col = MakeCollection();
        col.DictionaryEntries.Add(new DictionaryEntryRef
        {
            Id = "id-wu",
            SourceTerm = "無",
            PreferredTarget = "nothing"
        });

        var vm = MakeVm(col);

        var termNode = vm.Nodes.SingleOrDefault(n => n.NodeType == ScholarNodeType.TermbaseEntry);
        Assert.NotNull(termNode);
        Assert.Equal("term:無", termNode!.NodeId);
        Assert.Equal("無", termNode.Label);
        Assert.Equal("nothing", termNode.SecondaryLabel);
        Assert.IsType<DictionaryEntryRef>(termNode.SourceData);
    }

    [Fact]
    public void RebuildGraph_WithoutDictionaryEntries_HasNoTermbaseEntryNode()
    {
        var col = MakeCollection();

        var vm = MakeVm(col);

        Assert.DoesNotContain(vm.Nodes, n => n.NodeType == ScholarNodeType.TermbaseEntry);
    }

    [Fact]
    public void RebuildGraph_SuppressedTermNode_IsNotRecreated()
    {
        var col = MakeCollection();
        col.DictionaryEntries.Add(new DictionaryEntryRef { Id = "id-wu", SourceTerm = "無" });
        col.SuppressedAutoNodeIds.Add("term:無"); // user removed it — must stay gone across reload

        var vm = MakeVm(col);

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "term:無");
    }
}
