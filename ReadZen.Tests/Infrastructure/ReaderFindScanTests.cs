using System;
using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="ReaderFindScan"/>, the pure find-in-page scan helpers
/// extracted from ReadableTabView (MVVM renovation P10). Pins the CJK/Latin comparison
/// rule and the overlapping-occurrence scan shared by the two-editor and RowGrid find paths.
/// </summary>
public class ReaderFindScanTests
{
    // ---- ComparisonFor ----

    [Fact]
    public void ComparisonFor_LatinQuery_IsCaseInsensitive()
    {
        Assert.Equal(StringComparison.OrdinalIgnoreCase, ReaderFindScan.ComparisonFor("world"));
    }

    [Fact]
    public void ComparisonFor_CjkQuery_IsOrdinalExact()
    {
        Assert.Equal(StringComparison.Ordinal, ReaderFindScan.ComparisonFor("佛法"));
    }

    [Fact]
    public void ComparisonFor_MixedQueryWithCjk_IsOrdinalExact()
    {
        // Any ideograph forces exact matching.
        Assert.Equal(StringComparison.Ordinal, ReaderFindScan.ComparisonFor("the 佛"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ComparisonFor_NullOrEmpty_IsCaseInsensitive(string? query)
    {
        Assert.Equal(StringComparison.OrdinalIgnoreCase, ReaderFindScan.ComparisonFor(query));
    }

    // ---- FindOccurrences: basic ----

    [Fact]
    public void FindOccurrences_ExactMatch_ReturnsAllOffsets()
    {
        var hits = ReaderFindScan.FindOccurrences("Hello World, World!", "World", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new List<int> { 6, 13 }, hits);
    }

    [Fact]
    public void FindOccurrences_CaseInsensitive_MatchesDifferentCase()
    {
        var hits = ReaderFindScan.FindOccurrences("aAaA", "a", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new List<int> { 0, 1, 2, 3 }, hits);
    }

    [Fact]
    public void FindOccurrences_OrdinalCaseSensitive_RespectsCase()
    {
        var hits = ReaderFindScan.FindOccurrences("aAaA", "A", StringComparison.Ordinal);
        Assert.Equal(new List<int> { 1, 3 }, hits);
    }

    // ---- FindOccurrences: overlapping ----

    [Fact]
    public void FindOccurrences_OverlappingMatches_AdvancesByOne()
    {
        // "aa" occurs at 0,1,2 in "aaaa" because the scan advances by a single char.
        var hits = ReaderFindScan.FindOccurrences("aaaa", "aa", StringComparison.Ordinal);
        Assert.Equal(new List<int> { 0, 1, 2 }, hits);
    }

    // ---- FindOccurrences: CJK ----

    [Fact]
    public void FindOccurrences_CjkExact_FindsIdeograph()
    {
        var hits = ReaderFindScan.FindOccurrences("佛法僧佛", "佛", StringComparison.Ordinal);
        Assert.Equal(new List<int> { 0, 3 }, hits);
    }

    // ---- FindOccurrences: edge cases ----

    [Fact]
    public void FindOccurrences_NoMatch_ReturnsEmpty()
    {
        Assert.Empty(ReaderFindScan.FindOccurrences("abcdef", "xyz", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, "a")]
    [InlineData("", "a")]
    [InlineData("abc", null)]
    [InlineData("abc", "")]
    public void FindOccurrences_NullOrEmptyInputs_ReturnEmpty(string? text, string? query)
    {
        Assert.Empty(ReaderFindScan.FindOccurrences(text, query, StringComparison.Ordinal));
    }

    [Fact]
    public void FindOccurrences_QueryLongerThanText_ReturnsEmpty()
    {
        Assert.Empty(ReaderFindScan.FindOccurrences("ab", "abc", StringComparison.Ordinal));
    }

    [Fact]
    public void FindOccurrences_QueryEqualsText_ReturnsSingleZero()
    {
        Assert.Equal(new List<int> { 0 }, ReaderFindScan.FindOccurrences("abc", "abc", StringComparison.Ordinal));
    }
}
