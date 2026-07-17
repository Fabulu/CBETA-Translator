using System;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Fidelity tests for the rich lineage roster loader (PR-L1). Reads the bundled
/// <c>Assets/Data/lineage-masters.json</c> from the test output directory (the
/// app project's Content propagates there, same as master-dates.json).
/// </summary>
public class LineageRosterServiceTests
{
    private static LineageMasterRecord ByName(System.Collections.Generic.IReadOnlyList<LineageMasterRecord> all, string name)
    {
        var rec = all.FirstOrDefault(r => r.Names.Any(n => string.Equals(n, name, StringComparison.Ordinal)));
        Assert.NotNull(rec);
        return rec!;
    }

    [Fact]
    public void GetAll_LoadsExpandedEvidenceRoster()
    {
        var svc = new LineageRosterService();
        var all = svc.GetAll();
        // 2026-07-17 fold (RUN-20260711-1248): the old ">=1009" bound guarded the
        // CORRUPT 1012-record file (609 real + 403 auto-harvested hollow records).
        // That file was restored to the clean 609 baseline and had ~356
        // individually-researched masters folded back in, landing at 965. A
        // same-day defect-fix pass then collapsed 21 duplicate-person clusters (22
        // excess records) the fold had shipped, landing at 943 -- the current floor.
        Assert.True(all.Count >= 943, $"Expected the expanded evidence roster (>=943), got {all.Count}.");
    }

    [Fact]
    public void GetAll_IsCachedAcrossCalls()
    {
        var svc = new LineageRosterService();
        var first = svc.GetAll();
        var second = svc.GetAll();
        Assert.Same(first, second);
    }

    [Fact]
    public void BookTransmissionMaster_Jinul_HasThreeBooksWithFidelity()
    {
        var all = new LineageRosterService().GetAll();
        var jinul = ByName(all, "Jinul");

        Assert.Equal(3, jinul.BookTransmissions.Count);
        Assert.Contains("知訥", jinul.Names);

        var first = jinul.BookTransmissions[0];
        Assert.Equal("book:T48n2008", first.Id);
        Assert.Equal("Platform Sutra of the Sixth Patriarch", first.TitleEn);
        Assert.True(first.InCorpus);

        // Fidelity of a non-corpus entry (in_corpus=false must round-trip).
        Assert.Contains(jinul.BookTransmissions, b => b.InCorpus == false);
    }

    [Fact]
    public void ContestedMaster_LongtanChongxin_HasRivalHypothesis()
    {
        var all = new LineageRosterService().GetAll();
        var longtan = ByName(all, "龍潭崇信");

        Assert.True(longtan.Contested);
        Assert.NotNull(longtan.ContestedBy);
        Assert.Equal("Tianhuang Daowu 天皇道悟", longtan.ContestedBy!.KeepTeacher);
        Assert.Equal("天王道悟", longtan.ContestedBy!.Rival);
    }

    [Fact]
    public void ConjecturalDateMaster_Hyeeun_FlaggedConjectural()
    {
        var all = new LineageRosterService().GetAll();
        var hyeeun = ByName(all, "慧隱");

        Assert.True(hyeeun.DatesConjectural);
        Assert.Equal(800, hyeeun.Birth);
        Assert.Equal(850, hyeeun.Death);
    }

    [Fact]
    public void AllAttestations_AreParsed_AndProvenanceNestingRoundTrips()
    {
        var all = new LineageRosterService().GetAll();

        // Attestation grades present in the roster are A/B/C/D only.
        var grades = all.Select(r => r.Attestation)
                        .Where(a => !string.IsNullOrEmpty(a))
                        .Distinct()
                        .OrderBy(a => a, StringComparer.Ordinal)
                        .ToList();
        Assert.Equal(new[] { "A", "B", "C", "D" }, grades);

        // At least one record carries nested provenance evidence with a rung/quote,
        // proving the nested object graph deserializes.
        var withProv = all.FirstOrDefault(r =>
            r.Provenance != null &&
            (r.Provenance.Teacher.Count + r.Provenance.Dates.Count +
             r.Provenance.School.Count + r.Provenance.Bio.Count) > 0);
        Assert.NotNull(withProv);
    }
}
