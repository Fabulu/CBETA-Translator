using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

/// <summary>
/// Cross-app parity tests for the dictionary-entry term-node feature. These pin the
/// SHARED CONTRACT that the desktop <see cref="ResearchGraphViewModel.RebuildGraph"/> and the
/// SPA (views/scholar-graph.js) must both honor over the SAME hand-authored fixture
/// (TestData/dictnodes/dict-entry-nodes.jsonl, byte-identical to the SPA copy at
/// ZenLinkPage-spafix/test/fixtures/dict-entry-nodes.jsonl):
///   * a term node id is "term:" + SourceTerm (raw CJK, never slugified), NodeType == TermbaseEntry (3);
///   * a manual ref whose id is in SuppressedAutoNodeIds is NOT materialized, and its edges do not survive;
///   * typed term-endpoint edges (uses-term / defines-term) survive when both endpoints resolve;
///   * GraphLayout.NodePositions for a term node are honored;
///   * the JSONL carries only the ref snapshot (Id / SourceTerm / PreferredTarget) — never a dict body.
/// </summary>
public class DictionaryEntryNodesTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "dictnodes", "dict-entry-nodes.jsonl");

    private static readonly JsonSerializerOptions ReadOpts = new() { PropertyNameCaseInsensitive = true };

    private static string FixtureLine() => File.ReadAllText(FixturePath, Encoding.UTF8).Trim();

    private static ScholarCollection LoadFixtureCollection()
    {
        var c = JsonSerializer.Deserialize<ScholarCollection>(FixtureLine(), ReadOpts);
        Assert.NotNull(c);
        return c!;
    }

    // Shared-contract expectations (kept in one place so a drift shows up as one failing assert).
    private const string TermPublished = "水牯牛";      // has a published Zen entry
    private const string TermUnpublished = "未收之詞";  // no published entry — still becomes a node
    private const string TermSuppressed = "隱藏詞";     // suppressed via SuppressedAutoNodeIds
    private static readonly string[] ExpectedNodeIds =
    {
        "p1", "c1", "master:南泉普願", "term:水牯牛", "term:未收之詞"
    };
    // Surviving term-endpoint edges, identified by from|to|relationType (the app-agnostic key).
    private static readonly string[] ExpectedSurvivingTermEdgeKeys =
    {
        "p1|term:水牯牛|uses-term",
        "p1|term:未收之詞|defines-term"
    };

    // ---------------------------------------------------------------------------------------------
    // (a) ScholarCollectionsService round-trip preserves DictionaryEntries; legacy line -> empty list.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Fixture_ServiceRoundTrip_PreservesDictionaryEntries()
    {
        var svc = new ScholarCollectionsService();
        var tempRoot = Path.Combine(Path.GetTempPath(), "readzen-dictnodes-" + Guid.NewGuid().ToString("N")[..8]);
        var communityDir = Path.Combine(tempRoot, "community");
        Directory.CreateDirectory(communityDir);
        try
        {
            // Seed the community dir with the raw fixture line, then load via the real service path.
            File.WriteAllText(Path.Combine(communityDir, "tester.jsonl"), FixtureLine() + "\n", new UTF8Encoding(false));
            var loaded = await svc.LoadAllCommunityJsonlAsync(communityDir);

            Assert.True(loaded.ContainsKey("tester"));
            var c = Assert.Single(loaded["tester"]);
            AssertDictionaryEntriesPreserved(c);

            // Write back out through the service and reload — the field must survive a full round-trip.
            var communityDir2 = Path.Combine(tempRoot, "community2");
            await svc.WriteUserJsonlAsync(communityDir2, "tester", loaded["tester"]);
            var reloaded = await svc.LoadAllCommunityJsonlAsync(communityDir2);
            AssertDictionaryEntriesPreserved(Assert.Single(reloaded["tester"]));
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static void AssertDictionaryEntriesPreserved(ScholarCollection c)
    {
        Assert.Equal(3, c.DictionaryEntries.Count);
        var byTerm = c.DictionaryEntries.ToDictionary(e => e.SourceTerm, StringComparer.Ordinal);

        Assert.Equal("t_36aa29eb1287", byTerm[TermPublished].Id);
        Assert.Equal("water buffalo", byTerm[TermPublished].PreferredTarget);
        Assert.Equal("t_8ce98de02f17", byTerm[TermUnpublished].Id);
        Assert.Equal("t_c40040b029c6", byTerm[TermSuppressed].Id);

        // Contract: the JSONL never carries a dict body — only reference fields exist on the type.
        Assert.DoesNotContain("Senses", FixtureLine());
        Assert.DoesNotContain("Occurrences", FixtureLine());
    }

    [Fact]
    public void LegacyLine_WithoutDictionaryEntriesField_LoadsAsEmptyList()
    {
        // A pre-feature JSONL line has no DictionaryEntries property at all.
        const string legacy = "{\"Id\":\"old\",\"Name\":\"Legacy\",\"SchemaVersion\":2,\"Passages\":[{\"Id\":\"p1\"}]}";
        var c = JsonSerializer.Deserialize<ScholarCollection>(legacy, ReadOpts);

        Assert.NotNull(c);
        Assert.NotNull(c!.DictionaryEntries);
        Assert.Empty(c.DictionaryEntries);
    }

    // ---------------------------------------------------------------------------------------------
    // (b) RebuildGraph yields the exact node-id set (incl. term: nodes), types, labels, positions,
    //     and the surviving term-endpoint edges — honoring SuppressedAutoNodeIds.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void RebuildGraph_ProducesExactNodeIdSet_HonoringSuppression()
    {
        var c = LoadFixtureCollection();
        var vm = new ResearchGraphViewModel(c, new List<ScholarCollection> { c });

        var actualIds = vm.Nodes.Select(n => n.NodeId).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(ExpectedNodeIds.ToHashSet(StringComparer.Ordinal), actualIds);

        // Suppressed term node must NOT be materialized.
        Assert.DoesNotContain("term:" + TermSuppressed, actualIds);
    }

    [Fact]
    public void RebuildGraph_TermNodes_HaveTermbaseTypeAndRawCjkLabel()
    {
        var c = LoadFixtureCollection();
        var vm = new ResearchGraphViewModel(c, new List<ScholarCollection> { c });

        foreach (var term in new[] { TermPublished, TermUnpublished })
        {
            var node = vm.Nodes.Single(n => n.NodeId == "term:" + term);
            Assert.Equal(ScholarNodeType.TermbaseEntry, node.NodeType);
            Assert.Equal(3, (int)node.NodeType);
            Assert.Equal(term, node.Label); // raw CJK headword, never slugified
        }
    }

    [Fact]
    public void RebuildGraph_SurvivingTermEdges_MatchSharedContract()
    {
        var c = LoadFixtureCollection();
        var vm = new ResearchGraphViewModel(c, new List<ScholarCollection> { c });

        // Typed edges anchored on a term node that survive the rebuild.
        var survivingKeys = vm.Edges
            .Where(e => e.From.NodeId.StartsWith("term:", StringComparison.Ordinal)
                     || e.To.NodeId.StartsWith("term:", StringComparison.Ordinal))
            .Select(e => $"{e.From.NodeId}|{e.To.NodeId}|{e.RelationType}")
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ExpectedSurvivingTermEdgeKeys.ToHashSet(StringComparer.Ordinal), survivingKeys);

        // By EdgeId: the two term edges survive; the edge into the suppressed node does not.
        var edgeIds = vm.Edges.Select(e => e.EdgeId).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("e-uses", edgeIds);
        Assert.Contains("e-defines", edgeIds);
        Assert.DoesNotContain("e-suppressed", edgeIds);
    }

    [Fact]
    public void RebuildGraph_HonorsSavedTermNodePosition()
    {
        var c = LoadFixtureCollection();
        var vm = new ResearchGraphViewModel(c, new List<ScholarCollection> { c });

        var node = vm.Nodes.Single(n => n.NodeId == "term:" + TermPublished);
        Assert.Equal(123.5, node.X);
        Assert.Equal(456.25, node.Y);
    }

    // ---------------------------------------------------------------------------------------------
    // (c) OnAddTerm-style persistence: ComputeId fallback when the picker supplied no Id.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void OnAddTerm_WhenPickerIdAbsent_UsesComputeIdFallback()
    {
        // Mirrors Views/ResearchGraphWindow.OnAddTerm: fall back to ComputeId if the picker lacked an Id.
        string termName = TermPublished;
        string? pickerId = null; // picker supplied no Id

        var entryRef = new DictionaryEntryRef
        {
            Id = !string.IsNullOrEmpty(pickerId) ? pickerId! : DictionaryStore.ComputeId(termName),
            SourceTerm = termName,
            PreferredTarget = "water buffalo"
        };

        // Deterministic id derived from the raw head term, matching the fixture's persisted Id.
        Assert.Equal("t_36aa29eb1287", entryRef.Id);
        Assert.Equal(DictionaryStore.ComputeId(termName), entryRef.Id);
        // Node id contract: "term:" + raw SourceTerm.
        Assert.Equal("term:" + termName, "term:" + entryRef.SourceTerm);
    }

    [Fact]
    public void OnAddTerm_WhenPickerIdPresent_UsesPickerId()
    {
        string termName = TermPublished;
        string? pickerId = "t_picked000000";

        var entryRef = new DictionaryEntryRef
        {
            Id = !string.IsNullOrEmpty(pickerId) ? pickerId! : DictionaryStore.ComputeId(termName),
            SourceTerm = termName
        };

        Assert.Equal("t_picked000000", entryRef.Id);
    }

    [Fact]
    public void ComputeId_IsDeterministicAndMatchesFixtureIds()
    {
        Assert.Equal("t_36aa29eb1287", DictionaryStore.ComputeId(TermPublished));
        Assert.Equal("t_8ce98de02f17", DictionaryStore.ComputeId(TermUnpublished));
        Assert.Equal("t_c40040b029c6", DictionaryStore.ComputeId(TermSuppressed));
    }
}
