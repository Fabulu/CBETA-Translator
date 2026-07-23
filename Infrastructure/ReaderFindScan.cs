// Infrastructure/ReaderFindScan.cs
//
// Pure find-in-page text-scanning helpers for the reader surfaces. Extracted from
// ReadableTabView.axaml.cs (MVVM renovation P10) so the match-comparison rule and the
// overlapping-occurrence scan can be unit tested and shared between the two reader
// find paths.
//
// Like RowGridBuilder, ReaderLbGeometry, and ReaderLayoutStrategy, this class is
// deliberately PURE — no Avalonia, no state, no I/O — it operates only on plain strings.
// Callers: the two-editor find path (CollectMatches over AvaloniaEdit document text) and
// the RowGrid find path (CollectGridMatches over RowVm cell text).

using System;
using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Pure helpers for the reader find-in-page scan: choosing the string-comparison mode
/// (exact for CJK queries, case-insensitive for Latin) and enumerating every
/// (overlapping) occurrence of a query within a text. Stateless and deterministic;
/// safe to call off the UI thread.
/// </summary>
public static class ReaderFindScan
{
    /// <summary>
    /// Comparison rule shared by both reader find paths: CJK queries match exactly
    /// (<see cref="StringComparison.Ordinal"/>) so ideographs are never case-folded, while
    /// Latin queries match case-insensitively (<see cref="StringComparison.OrdinalIgnoreCase"/>).
    /// </summary>
    public static StringComparison ComparisonFor(string? query)
        => CjkText.ContainsIdeograph(query)
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// Returns the start offsets of every occurrence of <paramref name="query"/> in
    /// <paramref name="text"/> using <paramref name="comparison"/>. Matches may overlap
    /// (the scan advances by one char past each hit), mirroring the reader's highlight
    /// behavior. Returns an empty list when either string is null/empty or the query is
    /// longer than the text.
    /// </summary>
    public static List<int> FindOccurrences(string? text, string? query, StringComparison comparison)
    {
        var result = new List<int>();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query) || text.Length < query.Length)
            return result;

        int pos = 0;
        while (pos <= text.Length - query.Length)
        {
            int idx = text.IndexOf(query, pos, comparison);
            if (idx < 0) break;
            result.Add(idx);
            pos = idx + 1; // allow overlapping matches
        }

        return result;
    }
}
