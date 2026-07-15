// ViewModels/LineageDetailViewModel.cs
//
// The detail side-panel presenter (plan PR-L6) — the desktop parity port of
// ZenLinkPage/views/lineage-panel.js. It projects the SELECTED lineage node
// (a master, or a synthesized book-source pseudo-node) into display-ready,
// compiled-binding-friendly view-models: the bilingual header, the evidence
// line "in words", the two-sided dispute card for a contested edge, the bio,
// the stele "rubbings", the provenance ledger, and the footer of teacher /
// heirs / links. A book source gets a book's card instead (title / author /
// description + a reader-or-CBETA link).
//
// It is deliberately Avalonia-FREE and headless-testable, exactly like the
// chart VM that builds it (LineageChartViewModel.BuildDetail): all interaction
// (focus a node, open a URL, navigate the corpus, open a profile) is routed
// through the plain-delegate <see cref="LineageDetailContext"/>, never through a
// UI type. The panel XAML binds to these properties with x:DataType.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using LineageEdge = ReadZen.App.Infrastructure.LineageEdge;

namespace ReadZen.App.ViewModels;

/// <summary>
/// Plain-delegate interaction seam handed to <see cref="LineageDetailViewModel"/>
/// so the presenter stays headless: focusing a node, opening an external URL,
/// navigating the corpus reader, and the two profile actions are all callbacks
/// the host window wires up. Any callback may be null (a no-op).
/// </summary>
public sealed class LineageDetailContext
{
    /// <summary>Select + centre a node in the chart (teacher / heir / book link).</summary>
    public Action<LineageNode>? Focus { get; init; }
    /// <summary>Open an external URL in the system browser.</summary>
    public Action<string>? OpenUrl { get; init; }
    /// <summary>Navigate the corpus reader to a TEI path (a stele / in-corpus book).</summary>
    public Action<string>? NavigateCorpus { get; init; }
    /// <summary>Open the full master profile (List tab).</summary>
    public Action<LineageNode>? OpenProfile { get; init; }
    /// <summary>Open the corpus-appearances view for this master (Corpus tab).</summary>
    public Action<LineageNode>? OpenCorpusSearch { get; init; }
}

/// <summary>
/// One focusable node reference (teacher or heir) — a clickable link that
/// re-focuses the chart on that node.
/// </summary>
public sealed class LineageRefViewModel
{
    public string Label { get; }
    public ICommand FocusCommand { get; }

    public LineageRefViewModel(LineageNode target, LineageDetailContext ctx)
    {
        Label = !string.IsNullOrEmpty(target.Cjk) ? target.Cjk : target.Primary;
        FocusCommand = new RelayCommand(() => ctx.Focus?.Invoke(target));
    }
}

/// <summary>An external reference link (opens in the system browser).</summary>
public sealed class LineageLinkViewModel
{
    public string Label { get; }
    public ICommand OpenCommand { get; }

    public LineageLinkViewModel(LineageLink link, LineageDetailContext ctx)
    {
        var url = link.Url ?? "";
        Label = !string.IsNullOrEmpty(link.Label) ? link.Label! : url;
        OpenCommand = new RelayCommand(
            () => { if (!string.IsNullOrEmpty(url)) ctx.OpenUrl?.Invoke(url); });
    }
}

/// <summary>A "Read in context" / "Read on CBETA" action for a stele or book.</summary>
public sealed class LineageReadLinkViewModel
{
    public string Label { get; }
    public ICommand ReadCommand { get; }

    public LineageReadLinkViewModel(string label, Action run)
    {
        Label = label;
        ReadCommand = new RelayCommand(run);
    }
}

/// <summary>The two-sided "who was his teacher?" dispute card for a contested edge.</summary>
public sealed class LineageDisputeViewModel
{
    public string? KeepTeacher { get; }
    public string? KeptRung { get; }
    public string? KeptEvidence { get; }
    public string? Rival { get; }
    public string? RivalRung { get; }
    public string? RivalEvidence { get; }
    public string? Stake { get; }

    public bool HasKeepTeacher => !string.IsNullOrEmpty(KeepTeacher);
    public bool HasKeptRung => !string.IsNullOrEmpty(KeptRung);
    public bool HasKeptEvidence => !string.IsNullOrEmpty(KeptEvidence);
    public bool HasRival => !string.IsNullOrEmpty(Rival);
    public bool HasRivalRung => !string.IsNullOrEmpty(RivalRung);
    public bool HasRivalEvidence => !string.IsNullOrEmpty(RivalEvidence);
    public bool HasStake => !string.IsNullOrEmpty(Stake);

    public LineageDisputeViewModel(LineageContestedBy cb)
    {
        KeepTeacher = cb.KeepTeacher;
        KeptRung = cb.KeptRung;
        KeptEvidence = cb.KeptEvidence;
        Rival = cb.Rival;
        RivalRung = cb.RivalRung;
        RivalEvidence = cb.RivalEvidence;
        Stake = cb.Stake;
    }
}

/// <summary>A stele / inscription rendered as a "rubbing" block.</summary>
public sealed class LineageSteleViewModel
{
    public string? Kind { get; }
    public string? Title { get; }
    public string? Author { get; }
    public string? Quote { get; }
    public string? Note { get; }
    public LineageReadLinkViewModel? Read { get; }

    public bool HasKind => !string.IsNullOrEmpty(Kind);
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    public bool HasAuthor => !string.IsNullOrEmpty(Author);
    public bool HasQuote => !string.IsNullOrEmpty(Quote);
    public bool HasNote => !string.IsNullOrEmpty(Note);
    public bool HasRead => Read != null;

    public LineageSteleViewModel(LineageStele s, LineageDetailContext ctx)
    {
        Kind = s.Kind;
        Title = s.Title;
        Author = s.Author;
        Quote = s.Quote;
        Note = s.Note;
        var path = s.Path;
        if (LineageDetailViewModel.HasWorkId(path))
            Read = new LineageReadLinkViewModel("Read in context →",
                () => ctx.NavigateCorpus?.Invoke(path!));
    }
}

/// <summary>One provenance ledger row: an evidence rung, the claim it backs, the source and quote.</summary>
public sealed class LineageProvRowViewModel
{
    public string Rung { get; }
    public string Claim { get; }
    public string Source { get; }
    public string? Quote { get; }
    public string? Note { get; }

    public bool HasQuote => !string.IsNullOrEmpty(Quote);
    public bool HasNote => !string.IsNullOrEmpty(Note);

    public LineageProvRowViewModel(string claim, LineageProvenanceItem p)
    {
        Claim = claim;
        Rung = LineageDetailViewModel.RungLabel(p.Rung);
        Source = p.Source ?? "";
        Quote = p.Quote;
        Note = p.Note;
    }
}

/// <summary>
/// The full projection of a selected node into the detail panel. Built by
/// <see cref="LineageChartViewModel.BuildDetail"/>; every string here is
/// display-ready so the XAML stays declarative.
/// </summary>
public sealed class LineageDetailViewModel
{
    // Evidence attestation → a sentence (parity with lineage-panel.js ATT_SENTENCE).
    private static readonly IReadOnlyDictionary<string, string> AttSentence =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["A"] = "Attested by his own words, or his stone.",
            ["B"] = "Attested by a contemporary witness.",
            ["C"] = "Listed in a lineage index.",
            ["D"] = "Known only from the lamp records.",
        };

    private static readonly IReadOnlyDictionary<string, string> RungLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["first-person"] = "first-person", ["stele"] = "stele",
            ["contemporary"] = "contemporary", ["index"] = "index",
            ["lamp"] = "lamp", ["external"] = "external",
        };

    // A TEI work id inside a path (e.g. T48n2003) — parity with the SPA regexes.
    private static readonly Regex WorkIdRe =
        new(@"([A-Z]{1,2}\d+n\d+[A-Za-z]?)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SubBranchRe =
        new(@"Yangqi|楊岐|Huanglong|黃龍|Sanfeng|三峰|Songyuan|聚雲|Juyun|Jogye|Shouchang|壽昌",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public LineageNode Node { get; }
    public bool IsSource { get; }
    public bool IsMaster => !IsSource;

    // ── header ──
    public string? Cjk { get; }
    public bool HasCjk => !string.IsNullOrEmpty(Cjk);
    public string Primary { get; }
    public string? AltNames { get; }
    public bool HasAlt => !string.IsNullOrEmpty(AltNames);
    public string? DatesLine { get; }
    public bool HasDates => !string.IsNullOrEmpty(DatesLine);
    public bool DatesUncertain { get; }
    public bool DatesConflict { get; }
    public string? DateNote { get; }
    public bool HasDateNote => !string.IsNullOrEmpty(DateNote);
    public string? SchoolLabel { get; }
    public bool HasSchool => !string.IsNullOrEmpty(SchoolLabel);
    public string? SubBranch { get; }
    public bool HasSubBranch => !string.IsNullOrEmpty(SubBranch);

    // ── evidence ──
    public string? AttestationSentence { get; }
    public bool HasAttestation => !string.IsNullOrEmpty(AttestationSentence);
    public bool ShowDWarning { get; }
    public string? TransmissionSentence { get; }
    public bool HasEvidence { get; }

    // ── dispute ──
    public LineageDisputeViewModel? Dispute { get; }
    public bool HasDispute => Dispute != null;

    // ── bio ──
    public string? Bio { get; }
    public bool HasBio => !string.IsNullOrEmpty(Bio);

    // ── steles ──
    public IReadOnlyList<LineageSteleViewModel> Steles { get; }
    public bool HasSteles => Steles.Count > 0;

    // ── edge note ──
    public string? EdgeNote { get; }
    public bool HasEdgeNote => !string.IsNullOrEmpty(EdgeNote);

    // ── provenance ──
    public IReadOnlyList<LineageProvRowViewModel> Provenance { get; }
    public bool HasProvenance => Provenance.Count > 0;
    public string ProvenanceHeader => "Sources (" + Provenance.Count.ToString(CultureInfo.InvariantCulture) + ")";

    // ── footer ──
    public LineageRefViewModel? Teacher { get; }
    public bool HasTeacher => Teacher != null;
    public string? StubTeacherLabel { get; }
    public bool HasStubTeacher => !string.IsNullOrEmpty(StubTeacherLabel);
    public IReadOnlyList<LineageRefViewModel> Heirs { get; }
    public bool HasHeirs => Heirs.Count > 0;
    public string HeirsHeader =>
        (IsSource ? "Awakened (" : "Heirs (") + Heirs.Count.ToString(CultureInfo.InvariantCulture) + ")";
    public IReadOnlyList<LineageLinkViewModel> Links { get; }
    public bool HasLinks => Links.Count > 0;

    // ── master profile actions ──
    public ICommand OpenProfileCommand { get; }
    public ICommand OpenCorpusSearchCommand { get; }

    // ── book source ──
    public string? SourceTitle { get; }
    public bool HasSourceTitle => !string.IsNullOrEmpty(SourceTitle);
    public string? SourceTitleEn { get; }
    public bool HasSourceTitleEn => !string.IsNullOrEmpty(SourceTitleEn);
    public string? SourceAuthor { get; }
    public bool HasSourceAuthor => !string.IsNullOrEmpty(SourceAuthor);
    public string? SourceDesc { get; }
    public bool HasSourceDesc => !string.IsNullOrEmpty(SourceDesc);
    public LineageReadLinkViewModel? BookLink { get; }
    public bool HasBookLink => BookLink != null;

    public LineageDetailViewModel(LineageNode n, LineageEdge? selectedEdge, LineageDetailContext ctx)
    {
        Node = n;
        IsSource = n.IsSource;

        // ── header ──
        Cjk = string.IsNullOrEmpty(n.Cjk) ? null : n.Cjk;
        Primary = n.Primary;
        var alt = n.Aliases.Where(a => !string.IsNullOrEmpty(a) && !string.Equals(a, n.Cjk, StringComparison.Ordinal)).ToList();
        AltNames = alt.Count > 0 ? string.Join(" · ", alt) : null;

        var dline = new List<string>();
        if (!string.IsNullOrEmpty(n.DatesText)) dline.Add(n.DatesText);
        if (!string.IsNullOrEmpty(n.Region)) dline.Add(n.Region);
        DatesLine = dline.Count > 0 ? string.Join(" · ", dline) : null;
        DatesUncertain = n.DatesConjectural;
        DatesConflict = n.DatesConflict;
        DateNote = string.IsNullOrEmpty(n.DateNote) ? null : n.DateNote;

        if (!IsSource && !string.IsNullOrEmpty(n.SchoolKey))
        {
            SchoolLabel = SchoolLabelFor(n);
            SubBranch = SubBranchFor(n.SchoolRaw);
        }

        // ── evidence line (attestation in words) + transmission ──
        var att = n.Attestation; // already validated to A/B/C/D or null by the builder
        if (att != null && AttSentence.TryGetValue(att, out var sentence))
        {
            AttestationSentence = sentence;
            ShowDWarning = att == "D";
        }
        HasEvidence = AttestationSentence != null || n.ParentEdge != null;
        if (HasEvidence)
            TransmissionSentence = TransmissionSentenceFor(n);

        // ── dispute card ──
        var cb = selectedEdge?.Contested ?? (n.Contested ? n.ContestedBy : null);
        if (cb != null) Dispute = new LineageDisputeViewModel(cb);

        // ── bio ──
        Bio = string.IsNullOrEmpty(n.Bio) ? null : n.Bio;

        // ── steles (up to three, the crown jewels) ──
        Steles = n.Steles.Take(3).Select(s => new LineageSteleViewModel(s, ctx)).ToList();

        // ── edge note ──
        EdgeNote = string.IsNullOrEmpty(n.EdgeNote) ? null : n.EdgeNote;

        // ── provenance ledger ──
        Provenance = BuildProvenance(n.Provenance);

        // ── footer: teacher / heirs / links ──
        if (n.ParentEdge != null && !n.ParentEdge.From.IsSource)
            Teacher = new LineageRefViewModel(n.ParentEdge.From, ctx);
        else if (n.Stub)
            StubTeacherLabel = n.StubLabel;
        Heirs = n.ChildEdges.Select(e => new LineageRefViewModel(e.To, ctx)).ToList();
        Links = n.Links.Where(l => !string.IsNullOrEmpty(l.Url)).Select(l => new LineageLinkViewModel(l, ctx)).ToList();

        OpenProfileCommand = new RelayCommand(() => ctx.OpenProfile?.Invoke(n));
        OpenCorpusSearchCommand = new RelayCommand(() => ctx.OpenCorpusSearch?.Invoke(n));

        // ── book source card ──
        if (IsSource)
        {
            SourceTitle = string.IsNullOrEmpty(n.SourceTitle) ? null : n.SourceTitle;
            SourceTitleEn = string.IsNullOrEmpty(n.SourceTitleEn) ? null : n.SourceTitleEn;
            SourceAuthor = string.IsNullOrEmpty(n.SourceAuthor) ? null : n.SourceAuthor;
            SourceDesc = string.IsNullOrEmpty(n.SourceDesc) ? null : n.SourceDesc;
            BookLink = BuildBookLink(n, ctx);
        }
    }

    // ── projection helpers (pure) ──

    private static IReadOnlyList<LineageProvRowViewModel> BuildProvenance(LineageProvenance? prov)
    {
        var rows = new List<LineageProvRowViewModel>();
        if (prov == null) return rows;
        void Add(string claim, IReadOnlyList<LineageProvenanceItem>? items)
        {
            if (items == null) return;
            foreach (var p in items) rows.Add(new LineageProvRowViewModel(claim, p));
        }
        Add("teacher", prov.Teacher);
        Add("dates", prov.Dates);
        Add("school", prov.School);
        Add("bio", prov.Bio);
        return rows;
    }

    private static LineageReadLinkViewModel? BuildBookLink(LineageNode n, LineageDetailContext ctx)
    {
        var path = n.SourcePath;
        var m = string.IsNullOrEmpty(path) ? Match.Empty : WorkIdRe.Match(path!);
        if (!m.Success) return null;
        var workId = m.Groups[1].Value;
        if (n.SourceInCorpus)
            return new LineageReadLinkViewModel("Read in context →", () => ctx.NavigateCorpus?.Invoke(path!));
        var url = "https://cbetaonline.dila.edu.tw/zh/" + workId;
        return new LineageReadLinkViewModel("Read on CBETA →", () => ctx.OpenUrl?.Invoke(url));
    }

    private static string TransmissionSentenceFor(LineageNode n)
    {
        // Book-with-no-teacher: awakened through one or more book sources.
        if (n.Transmission == "book" && n.BookEdges is { Count: > 0 })
        {
            var books = string.Join(", ", n.BookEdges.Select(e => BookLabel(e.From)));
            return "No living teacher — awakened through " + books + ". His record says so.";
        }
        if (n.Transmission == "book" && n.ParentEdge != null && n.ParentEdge.From.IsSource)
            return "No living teacher — awakened through " + BookLabel(n.ParentEdge.From) + ". His record says so.";
        if (n.Stub)
            return "Dharma heir of " + (n.StubLabel ?? "") + " — named in the record, not yet in this corpus.";
        if (n.ParentEdge == null)
            return "A root of the tradition — nothing stands above him on this chart.";

        var who = TeacherLabel(n.ParentEdge.From);
        return n.Transmission switch
        {
            "遙嗣" => "Posthumous (遙嗣) heir of " + who + " — a transmission acknowledged across a gap.",
            "代囑" => "Heir of " + who + " by proxy (代囑) — an intermediary hand.",
            "disputed" => "Disputed heir of " + who + ".",
            "book" => "Awakened through the writings of " + who + " — a transmission by book, not by meeting.",
            _ => "Dharma heir of " + who + ".",
        };
    }

    private static string BookLabel(LineageNode source)
    {
        var parts = new[] { source.SourceTitleEn, source.SourceTitle }.Where(s => !string.IsNullOrEmpty(s));
        return string.Join(" ", parts);
    }

    private static string TeacherLabel(LineageNode t)
        => !string.IsNullOrEmpty(t.Cjk) ? t.Cjk : t.Primary;

    private static string SchoolLabelFor(LineageNode n)
    {
        if (n.SchoolKey != "other" && LineageGraphBuilder.SchoolLabels.TryGetValue(n.SchoolKey, out var label))
            return label;
        var raw = n.SchoolRaw ?? "";
        return raw.Length > 24 ? raw.Substring(0, 24) : (raw.Length > 0 ? raw : "Other");
    }

    private static string? SubBranchFor(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var m = SubBranchRe.Match(raw!);
        return m.Success ? m.Value : null;
    }

    internal static bool HasWorkId(string? path)
        => !string.IsNullOrEmpty(path) && WorkIdRe.IsMatch(path!);

    internal static string RungLabel(string? rung)
    {
        if (string.IsNullOrEmpty(rung)) return "";
        return RungLabels.TryGetValue(rung!, out var l) ? l : rung!;
    }
}
