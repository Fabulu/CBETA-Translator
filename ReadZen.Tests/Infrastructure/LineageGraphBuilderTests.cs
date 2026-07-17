// LineageGraphBuilderTests — pins the pure lineage normalization port
// (Infrastructure/LineageGraphBuilder.cs) against the SPA reference module
// (ZenLinkPage/lib/lineage-data.js). The full-roster report invariants below were
// obtained by RUNNING the JS reference over the same data\lineage-masters.json
// (a node harness printing report.* counts); they are the source of truth and a
// permanent re-sync ratchet.
//
// Expansion-safe invariants are used below: evidence-backed roster additions
// must increase masters/nodes without requiring a test rewrite after each wave.

using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Lineage")]
public class LineageGraphBuilderTests
{
    // Loads the real expandable roster via the L1 loader (asset is copied into the
    // test output by the App csproj Content include).
    private static IReadOnlyList<LineageMasterRecord> RealRoster()
        => new LineageRosterService().GetAll();

    private static LineageMasterRecord Rec(
        string primary,
        string? teacherKey = null,
        string? attestation = null,
        string? transmission = null,
        string? school = null,
        IEnumerable<string>? students = null,
        IEnumerable<string>? extraNames = null)
    {
        var names = new List<string> { primary };
        if (extraNames != null) names.AddRange(extraNames);
        return new LineageMasterRecord
        {
            Names = names,
            TeacherKey = teacherKey,
            Attestation = attestation,
            Transmission = transmission,
            School = school,
            Students = students?.ToList() ?? new List<string>(),
        };
    }

    // ── Full-roster report invariants (pinned from the JS reference run) ──

    [Fact]
    public void FullRoster_ReportCountsMatchJsReference()
    {
        var roster = RealRoster();
        // 2026-07-17 fold (RUN-20260711-1248): the roster was restored from the
        // CORRUPT 1012-record file (609 real + 403 auto-harvested hollow records,
        // which is what the old ">= 1009" bound guarded) back to the clean 609
        // baseline, then had ~356 individually-researched masters folded back in
        // (367 qualified minus dedup/merges, plus rescues/dangling-teacher nodes),
        // landing at 965. A same-day defect-fix pass then collapsed 21 duplicate-
        // person clusters the fold had shipped (22 excess records: the same man
        // merge-added a second or, in one case, third time), landing at 943 --
        // the current floor.
        Assert.True(roster.Count >= 943);

        var g = LineageGraphBuilder.Build(roster);

        Assert.Equal(roster.Count, g.Report.Masters);
        Assert.True(g.Report.Edges >= 552);
        Assert.True(g.Report.Roots >= 6);
        // Measured on the 943-record roster: Dangling=51 (2 UnresolvedTeacherKey +
        // 49 flagged teacher_dangling). "<= roster.Count" was VACUOUS -- it can only
        // ever be false if more than 100% of the roster dangles, i.e. never. Bound it
        // against FOLD_SPEC's own acceptance criterion instead: the dangling rate
        // must stay at or below 7% of the roster (documented baseline: 41/593=6.9%).
        Assert.True(g.Report.Dangling <= (int)(roster.Count * 0.07) + 5,
            $"dangling rate rose: {g.Report.Dangling}/{roster.Count}");
        Assert.Equal(3, g.Report.BookSources);
        Assert.Equal(2, g.Report.Contested);
        Assert.Empty(g.Report.BadAttestation);
        // Measured: 40/943 (4.2%). Also VACUOUS before ("<= roster.Count" can never
        // fail) -- bounded with real headroom instead.
        Assert.True(g.Report.UnknownSchool.Count <= 60,
            $"unknown-school count rose: {g.Report.UnknownSchool.Count}/{roster.Count}");
        Assert.Empty(g.Report.UnknownTransmission);
        // The same 2 unresolved keys as the clean 609 baseline (龍巖慧彦, teacher of
        // two Korean masters, himself not on the roster) -- exact, not a ceiling.
        Assert.Equal(2, g.Report.UnresolvedTeacherKey.Count);
    }

    [Fact]
    public void FullRoster_NodeAndCollectionCountsMatch()
    {
        var g = LineageGraphBuilder.Build(RealRoster());

        Assert.Equal(RealRoster().Count + 3, g.Nodes.Count);
        Assert.Equal(3, g.Sources.Count);
        Assert.Equal(g.Report.Edges, g.Edges.Count);
        Assert.All(g.Sources, s => Assert.True(s.IsSource));
        Assert.Equal(RealRoster().Count, g.Nodes.Count(n => !n.IsSource));
    }

    // ── Jinul: 3 bilingual book source pseudo-nodes, edges pointing at them ──

    [Fact]
    public void Jinul_SynthesizesThreeBilingualBookSources()
    {
        var g = LineageGraphBuilder.Build(RealRoster());

        Assert.True(g.ByName.TryGetValue("Jinul", out var jinul));
        Assert.Equal("book", jinul!.Transmission);

        var jSources = g.Nodes
            .Where(n => n.IsSource && n.Id.StartsWith("__src__" + jinul.Id + "__"))
            .ToList();
        Assert.Equal(3, jSources.Count);

        // Every book source is bilingual (EN + hanja), never hanja-only.
        foreach (var s in jSources)
        {
            Assert.Equal(2, s.Names.Count);
            Assert.All(s.Names, n => Assert.False(string.IsNullOrEmpty(n)));
            Assert.False(string.IsNullOrEmpty(s.SourceTitleEn));
            Assert.False(string.IsNullOrEmpty(s.SourceTitle)); // hanja
        }

        // Three book edges, each src -> Jinul, kind "book"; narrative parent = first book.
        Assert.NotNull(jinul.BookEdges);
        Assert.Equal(3, jinul.BookEdges!.Count);
        Assert.All(jinul.BookEdges, e =>
        {
            Assert.Equal("book", e.Kind);
            Assert.Same(jinul, e.To);
            Assert.True(e.From.IsSource);
        });
        Assert.Same(jinul.BookEdges[0], jinul.ParentEdge);
    }

    // ── Book-master with NO book_transmissions falls back to one bilingual sutra node ──

    [Fact]
    public void BookMaster_NoTransmissions_FallsBackToBilingualSutraNode()
    {
        var roster = new List<LineageMasterRecord> { Rec("Bookless", transmission: "book") };
        var g = LineageGraphBuilder.Build(roster);

        Assert.Equal(1, g.Report.BookSources);
        var src = Assert.Single(g.Sources);
        Assert.Equal(new[] { "Sutra", "經" }, src.Names.ToArray()); // bilingual, never hanja-only
    }

    // ── A student back-edge NEVER overrides an existing teacher-key parent ──

    [Fact]
    public void StudentBackEdge_DoesNotOverrideExistingTeacherKeyParent()
    {
        // Child's teacher_key resolves to "Master"; "Rival" also claims Child as a student.
        var roster = new List<LineageMasterRecord>
        {
            Rec("Master"),
            Rec("Child", teacherKey: "Master"),
            Rec("Rival", students: new[] { "Child" }),
        };

        var g = LineageGraphBuilder.Build(roster);

        Assert.True(g.ByName.TryGetValue("Child", out var child));
        Assert.NotNull(child!.ParentEdge);
        Assert.Equal("Master", child.ParentEdge!.From.Primary); // teacher-key parent survives
        // The rival back-edge was refused: no edge Rival -> Child exists.
        Assert.DoesNotContain(g.Edges, e => e.From.Primary == "Rival" && e.To.Primary == "Child");
    }

    [Fact]
    public void StudentBackEdge_RecoversParentForAnOtherwiseRootChild()
    {
        // Sanity counterpart: with no teacher_key on Child, the back-edge IS added.
        var roster = new List<LineageMasterRecord>
        {
            Rec("Master", students: new[] { "Child" }),
            Rec("Child"),
        };

        var g = LineageGraphBuilder.Build(roster);

        Assert.True(g.ByName.TryGetValue("Child", out var child));
        Assert.NotNull(child!.ParentEdge);
        Assert.Equal("Master", child.ParentEdge!.From.Primary);
        Assert.False(child.IsRoot);
    }

    // ── A bad attestation value is quarantined to null and recorded in the report ──

    [Fact]
    public void BadAttestation_QuarantinedToNull_AndReported()
    {
        var roster = new List<LineageMasterRecord>
        {
            Rec("Legit", attestation: "A"),
            Rec("Bogus", attestation: "X"),   // invalid grade
            Rec("Lower", attestation: "a"),   // lowercase is invalid (JS test is case-sensitive)
        };

        var g = LineageGraphBuilder.Build(roster);

        Assert.Equal("A", g.ByName["Legit"].Attestation);
        Assert.Null(g.ByName["Bogus"].Attestation);
        Assert.Null(g.ByName["Lower"].Attestation);
        Assert.Contains("Bogus:X", g.Report.BadAttestation);
        Assert.Contains("Lower:a", g.Report.BadAttestation);
        Assert.Equal(2, g.Report.BadAttestation.Count);
    }

    // ── Determinism: two builds of the same input yield identical node/edge sets ──

    [Fact]
    public void Determinism_TwoBuilds_ProduceIdenticalNodeAndEdgeSequences()
    {
        var roster = RealRoster();
        var a = LineageGraphBuilder.Build(roster);
        var b = LineageGraphBuilder.Build(roster);

        Assert.Equal(a.Nodes.Select(n => n.Id), b.Nodes.Select(n => n.Id));
        Assert.Equal(
            a.Edges.Select(e => $"{e.From.Id}->{e.To.Id}:{e.Kind}"),
            b.Edges.Select(e => $"{e.From.Id}->{e.To.Id}:{e.Kind}"));
    }

    // ── normalizeSchool: canonical-key mapping (spot checks incl. CJK + fallbacks) ──

    [Theory]
    [InlineData("Linji", "linji")]
    [InlineData("臨濟", "linji")]
    [InlineData("雲門", "yunmen")]
    [InlineData("曹洞", "caodong")]
    [InlineData("Korean Seon", "korean-seon")]
    [InlineData("조계", "korean-seon")]
    [InlineData("Kumārajīva", "pre-chan")]
    [InlineData("chan", "early-chan")]      // ^chan$ anchored
    [InlineData("", "other")]
    [InlineData("   ", "other")]
    [InlineData("something unrelated", "other")]
    public void NormalizeSchool_MapsToCanonicalKeys(string raw, string expected)
        => Assert.Equal(expected, LineageGraphBuilder.NormalizeSchool(raw));

    // ── repYear is never invented: death, else birth+65, else floruit, else null ──

    [Fact]
    public void RepYear_NeverInvented_And_ZeroIsMissing()
    {
        var g = LineageGraphBuilder.Build(new List<LineageMasterRecord>
        {
            new() { Names = new() { "D" }, Death = 800 },
            new() { Names = new() { "B" }, Birth = 700 },
            new() { Names = new() { "F" }, Floruit = 900 },
            new() { Names = new() { "Zero" }, Death = 0, Birth = 0, Floruit = 0 }, // all missing
            new() { Names = new() { "None" } },
        });

        Assert.Equal(800, g.ByName["D"].Year);
        Assert.Equal(765, g.ByName["B"].Year);   // birth + 65
        Assert.Equal(900, g.ByName["F"].Year);
        Assert.Null(g.ByName["Zero"].Year);
        Assert.Null(g.ByName["None"].Year);
    }
}
