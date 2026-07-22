// LineageDetailViewModelTests — pins the L6 detail-panel presenter (the desktop parity
// port of ZenLinkPage/views/lineage-panel.js). The VM is deliberately Avalonia-free and
// pure: it projects a selected LineageNode into display-ready strings + Has* flags, and
// routes every interaction through the plain-delegate LineageDetailContext. That makes it
// fully headless-testable — no mocking library needed, just hand-built nodes and recording
// delegates.
//
// Coverage: header (cjk/alt/dates/school/sub-branch), the evidence sentence + D-warning,
// the transmission sentence switch (root / stub / book-via-source / book-via-edges / the
// 遙嗣/代囑/disputed/book/direct tokens), the dispute-card precedence, stele Take(3) + the
// work-id-gated read link, the flattened provenance ledger, the footer teacher/heirs/links
// with grade dots + fail-safe, the command wiring, and the book-source card's
// in-corpus vs CBETA link. Plus the internal HasWorkId / RungLabel statics.

using System;
using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;
using LineageEdge = ReadZen.App.Infrastructure.LineageEdge;

namespace ReadZen.Tests.ViewModels;

[Trait("Domain", "Lineage")]
public class LineageDetailViewModelTests
{
    private static LineageDetailContext Noop() => new LineageDetailContext();

    // ── header ──

    [Fact]
    public void Header_ProjectsCjkAltNamesAndDatesLine()
    {
        var n = new LineageNode
        {
            Primary = "Linji Yixuan",
            Cjk = "臨濟義玄",
            Aliases = new[] { "臨濟義玄", "義玄", "Linji" },   // the one equal to Cjk is dropped
            DatesText = "d. 866",
            Region = "Hebei",
            DatesConjectural = true,
            DateNote = "date disputed",
        };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.True(vm.IsMaster);
        Assert.False(vm.IsSource);
        Assert.Equal("臨濟義玄", vm.Cjk);
        Assert.True(vm.HasCjk);
        Assert.Equal("義玄 · Linji", vm.AltNames);   // Cjk-duplicate filtered, joined with " · "
        Assert.True(vm.HasAlt);
        Assert.Equal("d. 866 · Hebei", vm.DatesLine);
        Assert.True(vm.HasDates);
        Assert.True(vm.DatesUncertain);
        Assert.False(vm.DatesConflict);
        Assert.Equal("date disputed", vm.DateNote);
        Assert.True(vm.HasDateNote);
    }

    [Fact]
    public void Header_EmptyFields_YieldNullsAndFalseFlags()
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "Solo" }, null, Noop());

        Assert.Null(vm.Cjk);
        Assert.False(vm.HasCjk);
        Assert.Null(vm.AltNames);
        Assert.False(vm.HasAlt);
        Assert.Null(vm.DatesLine);
        Assert.False(vm.HasDates);
        Assert.Null(vm.DateNote);
        Assert.False(vm.HasDateNote);
        Assert.Equal("Solo", vm.Primary);
    }

    [Fact]
    public void School_KnownKey_UsesSchoolLabelsMap_AndDetectsSubBranch()
    {
        var n = new LineageNode { Primary = "a", SchoolKey = "linji", SchoolRaw = "臨濟宗楊岐派" };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.Equal("Linji 臨濟", vm.SchoolLabel);
        Assert.True(vm.HasSchool);
        Assert.Equal("楊岐", vm.SubBranch);      // SubBranchRe picks the branch token out of the raw
        Assert.True(vm.HasSubBranch);
    }

    [Fact]
    public void School_KoreanSeon_DropsChanSuffix()
    {
        // The panel drops the 禪 suffix that the chart legend keeps ("Korean Seon 禪").
        var n = new LineageNode { Primary = "a", SchoolKey = "korean-seon", SchoolRaw = "曹溪宗" };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.Equal("Korean Seon", vm.SchoolLabel);
        Assert.Null(vm.SubBranch);
    }

    [Fact]
    public void School_OtherKeyWithLongRaw_TruncatesToTwentyFour()
    {
        var raw = new string('華', 30);
        var n = new LineageNode { Primary = "a", SchoolKey = "other", SchoolRaw = raw };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.Equal(24, vm.SchoolLabel!.Length);   // key "other" bypasses the map, raw is clipped
    }

    [Fact]
    public void School_SourceNode_HasNoSchoolLabel()
    {
        var n = new LineageNode { Primary = "Book", IsSource = true, SchoolKey = "linji" };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.Null(vm.SchoolLabel);
        Assert.False(vm.HasSchool);
    }

    // ── evidence ──

    [Theory]
    [InlineData("A", "Attested by his own words, or his stone.", false)]
    [InlineData("B", "Attested by a contemporary witness.", false)]
    [InlineData("C", "Listed in a lineage index.", false)]
    [InlineData("D", "Known only from the lamp records.", true)]
    public void Attestation_MapsToSentence_AndDWarningOnlyForD(string att, string sentence, bool warn)
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "x", Attestation = att }, null, Noop());

        Assert.Equal(sentence, vm.AttestationSentence);
        Assert.True(vm.HasAttestation);
        Assert.Equal(warn, vm.ShowDWarning);
        Assert.True(vm.HasEvidence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("E")]   // an unknown grade the sentence map has no entry for
    public void Attestation_NullOrUnknown_NoSentence_NoEvidence(string? att)
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "x", Attestation = att }, null, Noop());

        Assert.Null(vm.AttestationSentence);
        Assert.False(vm.HasAttestation);
        Assert.False(vm.ShowDWarning);
        Assert.False(vm.HasEvidence);          // no attestation and no parent edge
        Assert.Null(vm.TransmissionSentence);
    }

    // ── transmission sentence ──

    [Fact]
    public void Transmission_Root_NothingAbove()
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "Bodhidharma", Attestation = "A" }, null, Noop());

        Assert.True(vm.HasEvidence);
        Assert.Equal("A root of the tradition — nothing stands above him on this chart.", vm.TransmissionSentence);
    }

    [Theory]
    [InlineData("遙嗣", "Posthumous (遙嗣) heir of 老師 — a transmission acknowledged across a gap.")]
    [InlineData("代囑", "Heir of 老師 by proxy (代囑) — an intermediary hand.")]
    [InlineData("disputed", "Disputed heir of 老師.")]
    [InlineData("book", "Awakened through the writings of 老師 — a transmission by book, not by meeting.")]
    [InlineData("direct", "Dharma heir of 老師.")]
    public void Transmission_SwitchOnLivingTeacherEdge(string transmission, string expected)
    {
        var teacher = new LineageNode { Primary = "Teacher", Cjk = "老師" };
        var child = new LineageNode { Primary = "Student", Attestation = "B", Transmission = transmission };
        child.ParentEdge = new LineageEdge { From = teacher, To = child };

        var vm = new LineageDetailViewModel(child, null, Noop());
        Assert.Equal(expected, vm.TransmissionSentence);
    }

    [Fact]
    public void Transmission_BookViaSourceParent()
    {
        var src = new LineageNode { Primary = "src", IsSource = true, SourceTitleEn = "Platform Sutra", SourceTitle = "壇經" };
        var child = new LineageNode { Primary = "Huineng", Attestation = "A", Transmission = "book" };
        child.ParentEdge = new LineageEdge { From = src, To = child };

        var vm = new LineageDetailViewModel(child, null, Noop());
        Assert.Equal("No living teacher — awakened through Platform Sutra 壇經. His record says so.", vm.TransmissionSentence);
        Assert.False(vm.HasTeacher);   // a source parent is not shown as a clickable teacher ref
    }

    [Fact]
    public void Transmission_BookViaBookEdges_JoinsAllBooks()
    {
        var b1 = new LineageNode { Primary = "b1", IsSource = true, SourceTitleEn = "Diamond Sutra", SourceTitle = "金剛經" };
        var b2 = new LineageNode { Primary = "b2", IsSource = true, SourceTitleEn = "Platform Sutra", SourceTitle = "壇經" };
        var child = new LineageNode { Primary = "X", Attestation = "A", Transmission = "book" };
        child.BookEdges = new List<LineageEdge>
        {
            new LineageEdge { From = b1, To = child },
            new LineageEdge { From = b2, To = child },
        };

        var vm = new LineageDetailViewModel(child, null, Noop());
        Assert.Equal("No living teacher — awakened through Diamond Sutra 金剛經, Platform Sutra 壇經. His record says so.",
            vm.TransmissionSentence);
    }

    [Fact]
    public void Transmission_Stub_NamedNotYetInCorpus()
    {
        var n = new LineageNode { Primary = "X", Attestation = "C", Stub = true, StubLabel = "某師" };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.Equal("Dharma heir of 某師 — named in the record, not yet in this corpus.", vm.TransmissionSentence);
        Assert.Equal("某師", vm.StubTeacherLabel);
        Assert.True(vm.HasStubTeacher);
        Assert.False(vm.HasTeacher);
    }

    // ── dispute card ──

    [Fact]
    public void Dispute_FromSelectedEdge_TakesPriorityOverNode()
    {
        var edgeCb = new LineageContestedBy { KeepTeacher = "石頭", Rival = "馬祖道一", RivalRung = "stele", KeptRung = "index", Stake = "who taught him" };
        var nodeCb = new LineageContestedBy { Rival = "NODE-RIVAL" };
        var n = new LineageNode { Primary = "Yaoshan", Contested = true, ContestedBy = nodeCb };
        var edge = new LineageEdge { From = new LineageNode { Primary = "t" }, To = n, Contested = edgeCb };

        var vm = new LineageDetailViewModel(n, edge, Noop());

        Assert.True(vm.HasDispute);
        Assert.Equal("石頭", vm.Dispute!.KeepTeacher);
        Assert.Equal("馬祖道一", vm.Dispute.Rival);        // the edge's rival, not the node's
        Assert.True(vm.Dispute.HasKeepTeacher);
        Assert.True(vm.Dispute.HasRival);
        Assert.True(vm.Dispute.HasStake);
    }

    [Fact]
    public void Dispute_FallsBackToNodeContestedBy()
    {
        var nodeCb = new LineageContestedBy { Rival = "天王道悟", RivalRung = "lamp" };
        var n = new LineageNode { Primary = "Longtan", Contested = true, ContestedBy = nodeCb };

        var vm = new LineageDetailViewModel(n, null, Noop());
        Assert.True(vm.HasDispute);
        Assert.Equal("天王道悟", vm.Dispute!.Rival);
        Assert.False(vm.Dispute.HasStake);
    }

    [Fact]
    public void Dispute_None_WhenNotContested()
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "x", Contested = false }, null, Noop());
        Assert.False(vm.HasDispute);
        Assert.Null(vm.Dispute);
    }

    // ── steles ──

    [Fact]
    public void Steles_TakeThree_AndReadLinkGatedOnWorkId()
    {
        var n = new LineageNode
        {
            Primary = "x",
            Steles = new List<LineageStele>
            {
                new LineageStele { Kind = "pagoda", Title = "Stele 1", Author = "Pei Xiu", Quote = "…", Note = "n", Path = "xml-p5/T/T48n2003.xml", Lb = "0526c25" },
                new LineageStele { Title = "Stele 2", Path = "not-a-work-path.txt" },
                new LineageStele { Title = "Stele 3" },   // null path
                new LineageStele { Title = "Stele 4 (dropped by Take(3))" },
            },
        };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.True(vm.HasSteles);
        Assert.Equal(3, vm.Steles.Count);          // fourth stele is dropped

        var s0 = vm.Steles[0];
        Assert.Equal("pagoda", s0.Kind);
        Assert.True(s0.HasKind);
        Assert.Equal("Stele 1", s0.Title);
        Assert.Equal("Pei Xiu", s0.Author);
        Assert.True(s0.HasQuote);
        Assert.True(s0.HasNote);
        Assert.True(s0.HasRead);                    // path carries a TEI work id
        Assert.False(vm.Steles[1].HasRead);         // ".txt" path has no work id
        Assert.False(vm.Steles[2].HasRead);         // null path
    }

    [Fact]
    public void Stele_ReadCommand_NavigatesWithPathAndLineAnchor()
    {
        string? gotPath = null, gotLb = "SENTINEL";
        var ctx = new LineageDetailContext { NavigateCorpus = (p, l) => { gotPath = p; gotLb = l; } };
        var n = new LineageNode
        {
            Primary = "x",
            Steles = new List<LineageStele>
            {
                new LineageStele { Title = "S", Path = "xml-p5/T/T48n2003.xml", Lb = "0526c25" },
            },
        };
        var vm = new LineageDetailViewModel(n, null, ctx);

        vm.Steles[0].Read!.ReadCommand.Execute(null);
        Assert.Equal("xml-p5/T/T48n2003.xml", gotPath);
        Assert.Equal("0526c25", gotLb);             // the stele's lb anchor is carried through
    }

    // ── provenance ledger ──

    [Fact]
    public void Provenance_FlattensClaimsInOrder_AndCountsHeader()
    {
        var prov = new LineageProvenance();
        prov.Teacher.Add(new LineageProvenanceItem { Rung = "stele", Source = "Tang stele", Quote = "q" });
        prov.Dates.Add(new LineageProvenanceItem { Rung = "index", Source = "lamp record" });
        prov.School.Add(new LineageProvenanceItem { Rung = "unknown-rung", Source = "note", Note = "hm" });
        var n = new LineageNode { Primary = "x", Provenance = prov };

        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.True(vm.HasProvenance);
        Assert.Equal(3, vm.Provenance.Count);
        Assert.Equal("Sources (3)", vm.ProvenanceHeader);

        Assert.Equal("teacher", vm.Provenance[0].Claim);
        Assert.Equal("stele", vm.Provenance[0].Rung);
        Assert.Equal("Tang stele", vm.Provenance[0].Source);
        Assert.True(vm.Provenance[0].HasQuote);

        Assert.Equal("dates", vm.Provenance[1].Claim);
        Assert.False(vm.Provenance[1].HasQuote);

        Assert.Equal("school", vm.Provenance[2].Claim);
        Assert.Equal("unknown-rung", vm.Provenance[2].Rung);   // unmapped rung passes through
        Assert.True(vm.Provenance[2].HasNote);
    }

    [Fact]
    public void Provenance_Empty_WhenNull()
    {
        var vm = new LineageDetailViewModel(new LineageNode { Primary = "x" }, null, Noop());
        Assert.False(vm.HasProvenance);
        Assert.Empty(vm.Provenance);
        Assert.Equal("Sources (0)", vm.ProvenanceHeader);
    }

    // ── footer: teacher / heirs / links ──

    [Fact]
    public void Footer_HeirsFromChildEdges_WithGradeDotsAndFailSafe()
    {
        var heirA = new LineageNode { Primary = "HeirA", Cjk = "甲", Attestation = "A" };
        var heirBad = new LineageNode { Primary = "HeirZ", Attestation = "Z" };   // invalid grade → D
        var n = new LineageNode { Primary = "Master" };
        n.ChildEdges.Add(new LineageEdge { From = n, To = heirA });
        n.ChildEdges.Add(new LineageEdge { From = n, To = heirBad });

        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.True(vm.HasHeirs);
        Assert.Equal(2, vm.Heirs.Count);
        Assert.Equal("Heirs (2)", vm.HeirsHeader);

        Assert.Equal("甲", vm.Heirs[0].Label);        // cjk preferred over primary
        Assert.Equal("A", vm.Heirs[0].Grade);
        Assert.Equal(0.85, vm.Heirs[0].DotOpacity);
        Assert.False(vm.Heirs[0].DotFaint);
        Assert.True(vm.Heirs[0].DotSolid);

        Assert.Equal("HeirZ", vm.Heirs[1].Label);     // falls back to primary
        Assert.Equal("D", vm.Heirs[1].Grade);         // fail-safe for the invalid grade
        Assert.Equal(0.40, vm.Heirs[1].DotOpacity);
        Assert.True(vm.Heirs[1].DotFaint);
        Assert.False(vm.Heirs[1].DotSolid);
    }

    [Fact]
    public void Footer_SourceNode_HeirsHeaderSaysAwakened()
    {
        var n = new LineageNode { Primary = "Book", IsSource = true };
        n.ChildEdges.Add(new LineageEdge { From = n, To = new LineageNode { Primary = "disciple" } });

        var vm = new LineageDetailViewModel(n, null, Noop());
        Assert.Equal("Awakened (1)", vm.HeirsHeader);
    }

    [Fact]
    public void Footer_TeacherRef_FocusCommandRoutesTargetToContext()
    {
        var teacher = new LineageNode { Primary = "Teacher", Cjk = "師" };
        var child = new LineageNode { Primary = "Student" };
        child.ParentEdge = new LineageEdge { From = teacher, To = child };

        LineageNode? focused = null;
        var ctx = new LineageDetailContext { Focus = t => focused = t };
        var vm = new LineageDetailViewModel(child, null, ctx);

        Assert.True(vm.HasTeacher);
        Assert.Equal("師", vm.Teacher!.Label);
        vm.Teacher.FocusCommand.Execute(null);
        Assert.Same(teacher, focused);
    }

    [Fact]
    public void Links_FilterEmptyUrls_AndOpenCommandRoutes()
    {
        var n = new LineageNode
        {
            Primary = "x",
            Links = new List<LineageLink>
            {
                new LineageLink { Label = "DILA", Url = "https://example.org" },
                new LineageLink { Label = "Empty", Url = "" },
                new LineageLink { Label = null, Url = null },
            },
        };
        string? opened = null;
        var ctx = new LineageDetailContext { OpenUrl = u => opened = u };
        var vm = new LineageDetailViewModel(n, null, ctx);

        Assert.True(vm.HasLinks);
        Assert.Single(vm.Links);                       // only the one with a non-empty url survives
        Assert.Equal("DILA", vm.Links[0].Label);
        vm.Links[0].OpenCommand.Execute(null);
        Assert.Equal("https://example.org", opened);
    }

    [Fact]
    public void ProfileCommands_RouteNodeToContext()
    {
        LineageNode? profile = null, corpus = null;
        var ctx = new LineageDetailContext { OpenProfile = x => profile = x, OpenCorpusSearch = x => corpus = x };
        var n = new LineageNode { Primary = "x" };
        var vm = new LineageDetailViewModel(n, null, ctx);

        vm.OpenProfileCommand.Execute(null);
        vm.OpenCorpusSearchCommand.Execute(null);
        Assert.Same(n, profile);
        Assert.Same(n, corpus);
    }

    // ── book-source card ──

    [Fact]
    public void BookSource_InCorpus_ReadInContext_NavigatesWithNullAnchor()
    {
        var n = new LineageNode
        {
            Primary = "src",
            IsSource = true,
            SourceTitle = "壇經",
            SourceTitleEn = "Platform Sutra",
            SourceAuthor = "Huineng",
            SourceDesc = "A sutra",
            SourcePath = "xml-p5/T/T48n2008.xml",
            SourceInCorpus = true,
        };
        string? navPath = null, navLb = "SENTINEL";
        var ctx = new LineageDetailContext { NavigateCorpus = (p, l) => { navPath = p; navLb = l; } };
        var vm = new LineageDetailViewModel(n, null, ctx);

        Assert.True(vm.IsSource);
        Assert.Equal("壇經", vm.SourceTitle);
        Assert.Equal("Platform Sutra", vm.SourceTitleEn);
        Assert.Equal("Huineng", vm.SourceAuthor);
        Assert.Equal("A sutra", vm.SourceDesc);
        Assert.True(vm.HasBookLink);
        Assert.Equal("Read in context →", vm.BookLink!.Label);

        vm.BookLink.ReadCommand.Execute(null);
        Assert.Equal("xml-p5/T/T48n2008.xml", navPath);
        Assert.Null(navLb);                            // a book lands at the top, not a stele line
    }

    [Fact]
    public void BookSource_NotInCorpus_ReadOnCbeta_OpensDilaUrl()
    {
        var n = new LineageNode { Primary = "src", IsSource = true, SourcePath = "foo/T48n2003.xml", SourceInCorpus = false };
        string? opened = null;
        var ctx = new LineageDetailContext { OpenUrl = u => opened = u };
        var vm = new LineageDetailViewModel(n, null, ctx);

        Assert.Equal("Read on CBETA →", vm.BookLink!.Label);
        vm.BookLink.ReadCommand.Execute(null);
        Assert.Equal("https://cbetaonline.dila.edu.tw/zh/T48n2003", opened);
    }

    [Fact]
    public void BookSource_NoWorkIdInPath_NoBookLink()
    {
        var n = new LineageNode { Primary = "src", IsSource = true, SourcePath = "no-id-here.xml" };
        var vm = new LineageDetailViewModel(n, null, Noop());

        Assert.False(vm.HasBookLink);
        Assert.Null(vm.BookLink);
    }

    // ── internal pure helpers ──

    [Theory]
    [InlineData("xml-p5/T/T48n2003.xml", true)]
    [InlineData("X01n0001.xml", true)]
    [InlineData("T48n2003", true)]
    [InlineData("plain.txt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasWorkId_DetectsTeiWorkId(string? path, bool expected)
        => Assert.Equal(expected, LineageDetailViewModel.HasWorkId(path));

    [Theory]
    [InlineData("first-person", "first-person")]
    [InlineData("stele", "stele")]
    [InlineData("lamp", "lamp")]
    [InlineData("mystery", "mystery")]   // unmapped rung passes through unchanged
    [InlineData("", "")]
    [InlineData(null, "")]
    public void RungLabel_MapsKnownOrPassesThrough(string? rung, string expected)
        => Assert.Equal(expected, LineageDetailViewModel.RungLabel(rung));
}
