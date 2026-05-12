// Services/CommentaryPanelStateResolver.cs
// Pure decision logic for the reader-facing commentary panel. Extracted from
// ReadableTabView.PopulateCommentary so it is independently testable without
// instantiating the Avalonia control tree.
//
// Contract:
//   - Edition has not opted into commentary surfacing
//     (manifest.CommentaryReaderLanguages is null) → panel hidden entirely.
//   - Edition opts in but no Chinese entries match the filter
//     (typical FiM today: 17 Japanese entries all filtered out) → panel visible,
//     empty-state placeholder visible, entries list empty.
//   - Edition opts in and matching entries exist → panel visible, entries list
//     populated, empty-state hidden.
//
// Per SPEC v2 §"Default-deny posture", the filter at the service boundary
// already rejects null / "unknown" language tags; this resolver simply observes
// the post-filter result count to drive panel visibility.

using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Snapshot of the commentary panel's desired visual state. Consumed by
/// <see cref="ReadZen.App.Views.ReadableTabView.PopulateCommentary"/> to set
/// <c>IsVisible</c> on the panel border / empty-state placeholder and populate
/// the entry host.
/// </summary>
public sealed class CommentaryPanelState
{
    /// <summary>The right-column <c>CommentaryPanelBorder</c> visibility.</summary>
    public bool PanelVisible { get; init; }

    /// <summary>The inline empty-state placeholder visibility (only meaningful when <see cref="PanelVisible"/>).</summary>
    public bool EmptyStateVisible { get; init; }

    /// <summary>Entries to render in the panel host. Always non-null; empty when there are no matches.</summary>
    public IReadOnlyList<CommentaryEntry> Entries { get; init; } = System.Array.Empty<CommentaryEntry>();

    public static CommentaryPanelState Hidden { get; } = new CommentaryPanelState
    {
        PanelVisible = false,
        EmptyStateVisible = false,
        Entries = System.Array.Empty<CommentaryEntry>()
    };
}

/// <summary>
/// Pure helper deciding the commentary panel's visibility / contents from the
/// loaded manifest + service result. Designed so view-layer tests can exercise
/// the decision logic without an Avalonia control tree.
/// </summary>
public static class CommentaryPanelStateResolver
{
    /// <summary>
    /// Computes the panel state for the given file context.
    /// </summary>
    /// <param name="xmlAbsPath">Absolute path to the currently loaded XML. When null/empty, panel is hidden.</param>
    /// <param name="manifest">Manifest associated with the file. When null, or when
    /// <see cref="ManifestInfo.CommentaryReaderLanguages"/> is null, panel is hidden.</param>
    /// <param name="service">Resolved <see cref="ICommentaryService"/>. When null, panel is hidden
    /// (no plumbing wired — behave identically to a non-opt-in edition rather than crashing).</param>
    public static CommentaryPanelState Resolve(
        string? xmlAbsPath,
        ManifestInfo? manifest,
        ICommentaryService? service)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return CommentaryPanelState.Hidden;

        if (manifest?.CommentaryReaderLanguages is null)
            return CommentaryPanelState.Hidden;

        if (service is null)
            return CommentaryPanelState.Hidden;

        // Edition has opted into commentary surfacing — panel is visible from
        // here on, regardless of how many matching entries we surface.
        var result = service.TryLoad(xmlAbsPath, manifest.CommentaryReaderLanguages);
        var entries = result?.Entries;

        if (entries == null || entries.Count == 0)
        {
            return new CommentaryPanelState
            {
                PanelVisible = true,
                EmptyStateVisible = true,
                Entries = System.Array.Empty<CommentaryEntry>()
            };
        }

        return new CommentaryPanelState
        {
            PanelVisible = true,
            EmptyStateVisible = false,
            Entries = entries
        };
    }
}
