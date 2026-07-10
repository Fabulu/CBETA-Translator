// Infrastructure/CjkText.cs
namespace ReadZen.App.Infrastructure;

/// <summary>
/// CJK ideograph classification, consolidated from ~18 byte-identical private
/// copies (dead-code audit 2026-07-09, item #2). The canonical set is exactly
/// three BMP ranges:
/// <list type="bullet">
/// <item>U+3400-U+4DBF - CJK Unified Ideographs Extension A</item>
/// <item>U+4E00-U+9FFF - CJK Unified Ideographs</item>
/// <item>U+F900-U+FAFF - CJK Compatibility Ideographs</item>
/// </list>
/// Note the deliberate gap U+4DC0-U+4DFF (Yijing hexagram symbols) between the
/// first two ranges, which are NOT ideographs and are excluded.
///
/// Four other copies in the codebase intentionally use DIFFERENT ranges and are
/// left un-routed (each carries a one-line comment): ScholarTabViewModel and
/// CjkMatchNormalizer (U+4E00-U+9FFF only), SearchTabView (U+3400-U+9FFF
/// contiguous - includes the Yijing gap, excludes Compatibility), TypeaheadService
/// (>= U+4E00, open-ended).
///
/// CRITICAL: three search-index callers (SearchIndexService.IsCjk / IsIndexableCjk,
/// InvertedSearchIndex.IsIndexable) feed GUID-versioned artifacts that must not
/// drift. <see cref="ReadZen.Tests.Infrastructure.CjkTextTests"/> pins
/// <see cref="IsIdeograph"/> to the historical three-range set over the entire BMP.
/// </summary>
public static class CjkText
{
    /// <summary>
    /// True iff <paramref name="c"/> is a CJK ideograph in the canonical three-range
    /// set. <c>char</c> is a 16-bit UTF-16 code unit, so supplementary-plane
    /// ideographs (>= U+20000) are handled elsewhere via surrogate pairs.
    /// </summary>
    public static bool IsIdeograph(char c)
        => (c >= '\u3400' && c <= '\u4DBF')
        || (c >= '\u4E00' && c <= '\u9FFF')
        || (c >= '\uF900' && c <= '\uFAFF');

    /// <summary>
    /// True iff any char of <paramref name="s"/> is a canonical CJK ideograph.
    /// Null/empty returns false (preserves the null-guarded callers' behavior).
    /// </summary>
    public static bool ContainsIdeograph(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        foreach (char c in s)
        {
            if (IsIdeograph(c))
                return true;
        }
        return false;
    }
}
