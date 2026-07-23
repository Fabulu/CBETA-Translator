// ReadableGridPureTests — headless coverage for the PURE grid-surface logic added when the
// deferred reader modes were finished (RUN-20260710-0605, Wave B/C):
//   * ResolvePairedRow — the hit-test index mapping that turns a clicked row into its
//     selection-mirror partner (same row two-column; adjacent EN/ZH row single-column).
//   * ApplyGridFindHighlights / CollectGridMatches — the find-in-page Hspan computation over
//     RowVm text (grouping matches per row/side, marking the current one, clearing stale rows).
//   * ResumeCaptureKeyStillValid / ResumeRestoreShouldWait / ReadableRenderCompletedForNav —
//     the reused pure resume guards, now shared by the grid resume path.
// The instance methods are exercised through a field-injected ReadableTabView shell (no visual
// tree needed): they read only the grid model / find bookkeeping fields, never any control.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ReadZen.App.Infrastructure;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

[Trait("Domain", "Reader")]
public class ReadableGridPureTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    private static ReadableTabView Shell()
        => (ReadableTabView)RuntimeHelpers.GetUninitializedObject(typeof(ReadableTabView));

    private static void SetField(object target, string name, object? value)
    {
        var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name}");
        f.SetValue(target, value);
    }

    private static object? InvokePrivate(object target, string name, params object?[] args)
    {
        var m = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {name}");
        return m.Invoke(target, args);
    }

    private static RowVm Zh(int index, string lb, string text = "zh")
        => new() { Index = index, Lb = lb, Shape = RowShape.SingleColumn, Side = RowSide.Zh, ZhText = text };

    private static RowVm En(int index, string lb, string text = "en")
        => new() { Index = index, Lb = lb, Shape = RowShape.SingleColumn, Side = RowSide.En, EnText = text };

    private static RowVm Two(int index, string lb, string zh = "zh", string en = "en")
        => new() { Index = index, Lb = lb, Shape = RowShape.TwoColumn, Side = RowSide.Zh, ZhText = zh, EnText = en };

    private static void InstallModel(ReadableTabView view, IReadOnlyList<RowVm> rows)
    {
        SetField(view, "_rowGridRows", rows);
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var r in rows)
            if (!string.IsNullOrEmpty(r.Lb) && !map.ContainsKey(r.Lb)) map[r.Lb] = r.Index; // first row per lb
        SetField(view, "_rowGridLbToRow", map);
    }

    private static RowVm? ResolvePaired(ReadableTabView view, RowVm row)
        => (RowVm?)InvokePrivate(view, "ResolvePairedRow", row);

    // ---- Hit-test index mapping: ResolvePairedRow ----

    [Fact]
    public void ResolvePairedRow_TwoColumnRow_IsItsOwnPair()
    {
        var view = Shell();
        var rows = new List<RowVm> { Two(0, "0001a01"), Two(1, "0001a02") };
        InstallModel(view, rows);

        Assert.Same(rows[0], ResolvePaired(view, rows[0]));
    }

    [Fact]
    public void ResolvePairedRow_SingleColumnZh_MapsToAdjacentEnRowUnderSameLb()
    {
        // [A-zh, A-en, B-zh] — a ZH click looks one row forward for its EN partner.
        var view = Shell();
        var rows = new List<RowVm> { Zh(0, "0001a01"), En(1, "0001a01"), Zh(2, "0001a02") };
        InstallModel(view, rows);

        Assert.Same(rows[1], ResolvePaired(view, rows[0]));
    }

    [Fact]
    public void ResolvePairedRow_SingleColumnZh_NoEnPartner_ReturnsNull()
    {
        // B-zh has no following EN row (untranslated line / standalone heading) → no mirror.
        var view = Shell();
        var rows = new List<RowVm> { Zh(0, "0001a01"), En(1, "0001a01"), Zh(2, "0001a02") };
        InstallModel(view, rows);

        Assert.Null(ResolvePaired(view, rows[2]));
    }

    [Fact]
    public void ResolvePairedRow_SingleColumnEn_MapsBackToItsLbZhRow()
    {
        var view = Shell();
        var rows = new List<RowVm> { Zh(0, "0001a01"), En(1, "0001a01"), Zh(2, "0001a02") };
        InstallModel(view, rows);

        // The EN row mirrors back to the lb's ZH row (LbToRow's first-row-per-lb).
        Assert.Same(rows[0], ResolvePaired(view, rows[1]));
    }

    [Fact]
    public void ResolvePairedRow_MergedStackedEn_ResolvesToUnitZhRow_EvenWhenNotImmediatelyAdjacent()
    {
        // In MergedStacked every lb in a unit maps to the unit's single ZH paragraph row, so an
        // EN row whose lb is the unit's first lb still resolves back to that ZH row via LbToRow.
        var view = Shell();
        var zh = Zh(0, "0001a01", "甲一甲二");
        var en = En(1, "0001a01", "One A One B");
        var rows = new List<RowVm> { zh, en };
        InstallModel(view, rows);

        Assert.Same(zh, ResolvePaired(view, en));
    }

    // ---- Find Hspan computation: CollectGridMatches + ApplyGridFindHighlights ----

    private static void PrimeFind(ReadableTabView view, IReadOnlyList<RowVm> rows,
        List<(int, RowSide, int, int)> matches, int current)
    {
        SetField(view, "_rowGridRows", rows);
        SetField(view, "_gridFindMatches", matches);
        SetField(view, "_gridFindTouchedRows", new List<int>());
        SetField(view, "_gridFindCurrentIndex", current);
    }

    [Fact]
    public void CollectGridMatches_FindsAllOccurrences_IncludingOverlapping()
    {
        var view = Shell();
        var matches = new List<(int, RowSide, int, int)>();
        SetField(view, "_gridFindMatches", matches);

        // "aaa" contains "aa" at offsets 0 and 1 (overlapping matches are kept).
        InvokePrivate(view, "CollectGridMatches", "aaa", "aa", StringComparison.OrdinalIgnoreCase, 7, RowSide.Zh);

        Assert.Equal(2, matches.Count);
        Assert.Equal((7, RowSide.Zh, 0, 2), matches[0]);
        Assert.Equal((7, RowSide.Zh, 1, 2), matches[1]);
    }

    [Fact]
    public void CollectGridMatches_NoMatch_AddsNothing()
    {
        var view = Shell();
        var matches = new List<(int, RowSide, int, int)>();
        SetField(view, "_gridFindMatches", matches);

        InvokePrivate(view, "CollectGridMatches", "hello", "zz", StringComparison.OrdinalIgnoreCase, 0, RowSide.En);

        Assert.Empty(matches);
    }

    [Fact]
    public void ApplyGridFindHighlights_WritesSpansOntoRows_AndMarksTheCurrentMatch()
    {
        var view = Shell();
        var rows = new List<RowVm> { Two(0, "0001a01", "狗子佛性"), Two(1, "0001a02", "佛性") };
        // Two matches for "佛性": row 0 @ offset 2, row 1 @ offset 0. Current = the second.
        var matches = new List<(int, RowSide, int, int)>
        {
            (0, RowSide.Zh, 2, 2),
            (1, RowSide.Zh, 0, 2),
        };
        PrimeFind(view, rows, matches, current: 1);

        InvokePrivate(view, "ApplyGridFindHighlights");

        var r0 = Assert.Single(rows[0].ZhHighlights);
        Assert.Equal(new Hspan(2, 2, false), r0);   // not the current match
        var r1 = Assert.Single(rows[1].ZhHighlights);
        Assert.Equal(new Hspan(0, 2, true), r1);    // current match flagged

        // EN side had no matches → stays empty on both rows.
        Assert.Empty(rows[0].EnHighlights);
        Assert.Empty(rows[1].EnHighlights);
    }

    [Fact]
    public void ApplyGridFindHighlights_RoutesEnMatchesToEnHighlights_NotZh()
    {
        var view = Shell();
        var rows = new List<RowVm> { Two(0, "0001a01", "佛性", "nature nature") };
        var matches = new List<(int, RowSide, int, int)>
        {
            (0, RowSide.En, 0, 6),
            (0, RowSide.En, 7, 6),
        };
        PrimeFind(view, rows, matches, current: 0);

        InvokePrivate(view, "ApplyGridFindHighlights");

        Assert.Empty(rows[0].ZhHighlights);
        Assert.Equal(2, rows[0].EnHighlights.Count);
        Assert.True(rows[0].EnHighlights[0].IsCurrent);   // first is current
        Assert.False(rows[0].EnHighlights[1].IsCurrent);
    }

    [Fact]
    public void ApplyGridFindHighlights_ClearsRowsThatNoLongerMatch_OnReapply()
    {
        var view = Shell();
        var rows = new List<RowVm> { Two(0, "0001a01", "佛性"), Two(1, "0001a02", "佛性") };
        SetField(view, "_rowGridRows", rows);
        SetField(view, "_gridFindTouchedRows", new List<int>());

        // First pass: both rows match.
        SetField(view, "_gridFindMatches", new List<(int, RowSide, int, int)>
        {
            (0, RowSide.Zh, 0, 2), (1, RowSide.Zh, 0, 2),
        });
        SetField(view, "_gridFindCurrentIndex", 0);
        InvokePrivate(view, "ApplyGridFindHighlights");
        Assert.Single(rows[0].ZhHighlights);
        Assert.Single(rows[1].ZhHighlights);

        // Second pass: query narrowed to row 1 only — row 0's stale span must be cleared.
        SetField(view, "_gridFindMatches", new List<(int, RowSide, int, int)>
        {
            (1, RowSide.Zh, 0, 2),
        });
        SetField(view, "_gridFindCurrentIndex", 0);
        InvokePrivate(view, "ApplyGridFindHighlights");

        Assert.Empty(rows[0].ZhHighlights);   // cleared
        Assert.Single(rows[1].ZhHighlights);  // still lit
    }

    // ---- Resume guard reuse (pure statics shared by the grid resume path) ----

    [Theory]
    [InlineData("T/a.xml", "T/a.xml", true)]
    [InlineData("T/a.xml", "T/b.xml", false)] // navigated away between schedule and tick
    [InlineData(null, "T/a.xml", false)]
    [InlineData("T/a.xml", null, false)]
    [InlineData("", "", false)]               // empty key is never "still valid"
    public void ResumeCaptureKeyStillValid_MatchesScheduledAgainstLiveKey(string? scheduled, string? live, bool expected)
        => Assert.Equal(expected, ReadableTabView.ResumeCaptureKeyStillValid(scheduled, live));

    [Theory]
    [InlineData(5, 4, true)]   // gen bumped since last provenance → render completed
    [InlineData(4, 4, false)]  // unchanged → panes still hold the previous file's docs
    public void ReadableRenderCompletedForNav_ChecksGenerationBump(int renderGen, int lastProv, bool expected)
        => Assert.Equal(expected, ReadableTabView.ReadableRenderCompletedForNav(renderGen, lastProv));

    [Theory]
    [InlineData(false, true, true, false)]   // not gated, docs loaded, reapply settled → proceed
    [InlineData(false, false, true, true)]   // docs not loaded → wait
    [InlineData(true, true, true, true)]     // gated (time-travel / pending refresh) → wait
    [InlineData(false, true, false, true)]   // sticky reapply not yet settled → wait
    public void ResumeRestoreShouldWait_WaitsUntilLoadedUngatedAndSettled(
        bool gated, bool docsLoaded, bool stickyResolved, bool expected)
        => Assert.Equal(expected, ReadableTabView.ResumeRestoreShouldWait(gated, docsLoaded, stickyResolved));
}
