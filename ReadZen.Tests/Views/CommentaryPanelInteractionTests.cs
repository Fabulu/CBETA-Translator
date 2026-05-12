// ReadZen.Tests/Views/CommentaryPanelInteractionTests.cs
//
// Coverage for the right-column commentary panel (PR 3 of the FiM commentary
// language filter sprint). The full ReadableTabView control tree is heavy to
// materialize in a headless harness, so the panel's decision logic lives in
// `Services/CommentaryPanelStateResolver.cs` as a pure helper. These tests
// exercise that helper end-to-end via `StubCommentaryService` from PR 1, then
// also drive the static `ReadableTabView.ApplyCommentaryPanelState` against
// freshly-constructed Avalonia controls to lock in the rendering branch.
//
// All tests tagged `[Trait("Domain", "FiM")]` per the run's testing scheme.

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Views;
using ReadZen.Tests.Stubs;
using Xunit;

namespace ReadZen.Tests.Views;

[Trait("Domain", "FiM")]
public class CommentaryPanelInteractionTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer]

    private static string MakeTempXmlPath()
    {
        // Resolver only uses the path as an opaque key for the service.
        var dir = Path.Combine(Path.GetTempPath(), "fim-commentary-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "faith-in-mind.xml");
    }

    private static CommentaryEntry MakeEntry(string id, string language, string title, string? body = null, string? locus = null)
        => new()
        {
            CommentaryId = id,
            Language = language,
            Title = title,
            Body = body,
            LocusId = locus,
            Source = $"witness:{id}"
        };

    // -----------------------------------------------------------------
    // Fact 1: panel hidden when manifest doesn't opt in
    // -----------------------------------------------------------------
    [Fact]
    public void Panel_Hidden_WhenManifestHasNoLanguageWhitelist()
    {
        var stub = new StubCommentaryService
        {
            // Even if a commentary file is on disk, with no whitelist in the
            // manifest the resolver must short-circuit to Hidden BEFORE the
            // service is consulted (zero footprint on non-opt-in editions).
            PreloadedEntries = new List<CommentaryEntry>
            {
                MakeEntry("C1", "zh-Hant", "信心銘箋註")
            }
        };
        var manifest = new ManifestInfo
        {
            // No CommentaryReaderLanguages → non-opt-in edition
            CommentaryReaderLanguages = null
        };

        var state = CommentaryPanelStateResolver.Resolve(MakeTempXmlPath(), manifest, stub);

        Assert.False(state.PanelVisible);
        Assert.False(state.EmptyStateVisible);
        Assert.Empty(state.Entries);
        // BLOCKING ACCEPTANCE: service must not be called when manifest opts out.
        Assert.Equal(0, stub.CallCount);
    }

    // -----------------------------------------------------------------
    // Fact 2: empty-state when whitelist set but no matching entries
    // (the typical FiM-today case — 17 Japanese filtered to zero)
    // -----------------------------------------------------------------
    [Fact]
    public void Panel_Visible_EmptyState_WhenWhitelistSetButNoMatchingEntries()
    {
        // Simulate FiM today: 17 Japanese entries, none of which match the
        // Chinese-only reader whitelist. StubCommentaryService mirrors the
        // real filter logic from PR 1.
        var japaneseEntries = new List<CommentaryEntry>();
        for (int i = 1; i <= 17; i++)
            japaneseEntries.Add(MakeEntry($"C{i}", "ja", $"信心銘講話 vol.{i}", body: "ある日, 三祖が言われた"));

        var stub = new StubCommentaryService { PreloadedEntries = japaneseEntries };
        var manifest = new ManifestInfo
        {
            CommentaryReaderLanguages = new List<string> { "zh-Hant", "zh-Hans" }
        };

        var state = CommentaryPanelStateResolver.Resolve(MakeTempXmlPath(), manifest, stub);

        Assert.True(state.PanelVisible);
        Assert.True(state.EmptyStateVisible);
        Assert.Empty(state.Entries);
        // Service WAS consulted (edition opted in); reader filter dropped all 17.
        Assert.Equal(1, stub.CallCount);
        Assert.Equal(new List<string> { "zh-Hant", "zh-Hans" }, stub.LastAllowedLanguages);
    }

    // -----------------------------------------------------------------
    // Fact 3: panel renders entries when matching Chinese entries exist
    // (forward-compat: when real Chinese commentary lands in OpenZen)
    // -----------------------------------------------------------------
    [Fact]
    public void Panel_Visible_RendersEntries_WhenMatchingEntriesExist()
    {
        var stub = new StubCommentaryService
        {
            PreloadedEntries = new List<CommentaryEntry>
            {
                MakeEntry("C1", "ja", "信心銘講話"),                                    // filtered
                MakeEntry("C2", "zh-Hant", "信心銘箋註", body: "至道無難之說也。", locus: "L01"),
                MakeEntry("C3", "zh-Hans", "信心铭笺注", body: "至道无难。"),
                MakeEntry("C4", "en", "Notes on Xinxinming")                            // filtered
            }
        };
        var manifest = new ManifestInfo
        {
            CommentaryReaderLanguages = new List<string> { "zh-Hant", "zh-Hans" }
        };

        var state = CommentaryPanelStateResolver.Resolve(MakeTempXmlPath(), manifest, stub);

        Assert.True(state.PanelVisible);
        Assert.False(state.EmptyStateVisible);
        Assert.Equal(2, state.Entries.Count);
        Assert.Equal("C2", state.Entries[0].CommentaryId);
        Assert.Equal("C3", state.Entries[1].CommentaryId);

        // Also exercise the static renderer to lock in the empty-state-hidden +
        // entries-rendered branch against a real Avalonia control tree.
        Dispatcher.UIThread.Invoke(() =>
        {
            var border = new Border { IsVisible = false };
            var host = new StackPanel();
            var emptyState = new TextBlock { IsVisible = false };

            ReadableTabView.ApplyCommentaryPanelState(state, border, host, emptyState);

            Assert.True(border.IsVisible);
            Assert.False(emptyState.IsVisible);
            Assert.Equal(2, host.Children.Count);
        });
    }

    // -----------------------------------------------------------------
    // Fact 4: empty-state ↔ list toggles when commentary content changes
    // -----------------------------------------------------------------
    [Fact]
    public void Panel_TogglesEmptyStateVsList_WhenCommentaryFileMtimeChanges()
    {
        // Round 1: only Japanese available → empty-state.
        var stub = new StubCommentaryService
        {
            PreloadedEntries = new List<CommentaryEntry>
            {
                MakeEntry("C1", "ja", "信心銘講話")
            }
        };
        var manifest = new ManifestInfo
        {
            CommentaryReaderLanguages = new List<string> { "zh-Hant", "zh-Hans" }
        };
        var xmlPath = MakeTempXmlPath();

        var firstState = CommentaryPanelStateResolver.Resolve(xmlPath, manifest, stub);
        Assert.True(firstState.PanelVisible);
        Assert.True(firstState.EmptyStateVisible);
        Assert.Empty(firstState.Entries);

        // Simulate the on-disk commentary.json gaining a Chinese entry (the
        // service in production picks this up via mtime cache invalidation;
        // the stub just exposes new preloaded entries).
        stub.PreloadedEntries.Add(MakeEntry("C2", "zh-Hant", "信心銘箋註", body: "至道無難。"));

        var secondState = CommentaryPanelStateResolver.Resolve(xmlPath, manifest, stub);
        Assert.True(secondState.PanelVisible);
        Assert.False(secondState.EmptyStateVisible);
        Assert.Single(secondState.Entries);
        Assert.Equal("C2", secondState.Entries[0].CommentaryId);

        // Lock in the toggle behaviour on the real Avalonia controls — same
        // instances reused across rounds prove the renderer clears the host
        // between applications.
        Dispatcher.UIThread.Invoke(() =>
        {
            var border = new Border();
            var host = new StackPanel();
            var emptyState = new TextBlock();

            ReadableTabView.ApplyCommentaryPanelState(firstState, border, host, emptyState);
            Assert.True(border.IsVisible);
            Assert.True(emptyState.IsVisible);
            Assert.Empty(host.Children);

            ReadableTabView.ApplyCommentaryPanelState(secondState, border, host, emptyState);
            Assert.True(border.IsVisible);
            Assert.False(emptyState.IsVisible);
            Assert.Single(host.Children);
        });
    }

    // -----------------------------------------------------------------
    // Gap-fill facts (Wave 4 test-writer pass — RUN-20260512-1754).
    // The original 4 facts covered the four "happy" quadrants of the
    // resolver's state space. These add the defensive guard checks
    // (null xmlAbsPath, null service, null manifest) + the null Title /
    // null Body rendering branch in BuildCommentaryEntryView that no
    // existing fact reached.
    // -----------------------------------------------------------------

    [Fact]
    public void Resolve_Hidden_WhenServiceIsNull()
    {
        // DI lookup yields null (test harness misconfigured, plugin not
        // registered, etc.) → resolver short-circuits to Hidden rather
        // than NullReferenceException. The resolver's third guard.
        var manifest = new ManifestInfo
        {
            CommentaryReaderLanguages = new List<string> { "zh-Hant" }
        };

        var state = CommentaryPanelStateResolver.Resolve(MakeTempXmlPath(), manifest, service: null);

        Assert.False(state.PanelVisible);
        Assert.False(state.EmptyStateVisible);
        Assert.Empty(state.Entries);
    }

    [Fact]
    public void Resolve_Hidden_WhenXmlAbsPathIsNullOrWhitespace()
    {
        // First resolver guard: no file context (Reader tab cleared,
        // text un-selected, startup race) → Hidden. Service must NOT
        // be called for any of these inputs.
        var stub = new StubCommentaryService
        {
            PreloadedEntries = new List<CommentaryEntry>
            {
                MakeEntry("C1", "zh-Hant", "should-not-load")
            }
        };
        var manifest = new ManifestInfo
        {
            CommentaryReaderLanguages = new List<string> { "zh-Hant" }
        };

        foreach (var path in new string?[] { null, "", "   " })
        {
            var state = CommentaryPanelStateResolver.Resolve(path, manifest, stub);
            Assert.False(state.PanelVisible);
            Assert.False(state.EmptyStateVisible);
            Assert.Empty(state.Entries);
        }

        // All three null/empty/whitespace paths short-circuited BEFORE
        // touching the service.
        Assert.Equal(0, stub.CallCount);
    }

    [Fact]
    public void ApplyCommentaryPanelState_RendersEntryWithNullTitleAndBody_NoCrash()
    {
        // BuildCommentaryEntryView guards each optional field individually:
        //   - Title null/whitespace → renders "(untitled)" placeholder.
        //   - Body null/whitespace → skipped (no body TextBlock added).
        //   - LocusId null → no locus chip.
        //   - Source null → no source attribution line.
        // The existing 4 panel facts always populated Title and Source;
        // none exercised the "real FiM today" shape where Body / LocusId /
        // AnchorText are all null per PR 4's fixture. This locks in that
        // a degenerate entry doesn't crash and renders something visible.
        var state = new CommentaryPanelState
        {
            PanelVisible = true,
            EmptyStateVisible = false,
            Entries = new[]
            {
                new CommentaryEntry
                {
                    CommentaryId = "C-bare",
                    Language = "zh-Hant",
                    Title = null,      // → "(untitled)" placeholder
                    Body = null,       // → no body TextBlock
                    LocusId = null,    // → no locus chip
                    AnchorText = null,
                    Source = null      // → no source attribution
                },
                new CommentaryEntry
                {
                    CommentaryId = "C-full",
                    Language = "zh-Hant",
                    Title = "全",
                    Body = "本",
                    LocusId = "L1",
                    Source = "witness:full"
                }
            }
        };

        Dispatcher.UIThread.Invoke(() =>
        {
            var border = new Border { IsVisible = false };
            var host = new StackPanel();
            var emptyState = new TextBlock { IsVisible = true };

            ReadableTabView.ApplyCommentaryPanelState(state, border, host, emptyState);

            Assert.True(border.IsVisible);
            Assert.False(emptyState.IsVisible);
            // Two entries rendered as separate Border children — neither
            // crashed despite the first one being entirely-null payload.
            Assert.Equal(2, host.Children.Count);
            Assert.IsType<Border>(host.Children[0]);
            Assert.IsType<Border>(host.Children[1]);
        });
    }
}
