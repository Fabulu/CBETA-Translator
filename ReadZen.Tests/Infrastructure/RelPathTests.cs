using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Byte-compatibility contract for the consolidated <see cref="RelPath.Normalize"/>
/// (dead-code audit 2026-07-09, item #3). These keys index into the shared search
/// and translation-memory dictionaries; the five routed callers
/// ((p ?? "").Replace('\\','/').TrimStart('/')) must keep producing byte-identical
/// output. If one of these fails after a change, the change breaks a shared key
/// format - do not "fix the test".
/// </summary>
public class RelPathTests
{
    // The exact inline expression the five identical copies used.
    private static string OldInline(string? p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    // The TranslationReviewService variant: same, plus a trailing .Trim().
    private static string OldReviewInline(string? p)
        => (p ?? "").Replace('\\', '/').TrimStart('/').Trim();

    public static readonly object?[][] Samples =
    {
        new object?[] { "T/T48/T48n2005.xml" },
        new object?[] { "T\\T48\\T48n2005.xml" },
        new object?[] { "/leading/slash" },
        new object?[] { "\\leading\\backslash" },
        new object?[] { "//double/leading" },
        new object?[] { "mixed\\path/to\\file.xml" },
        new object?[] { "" },
        new object?[] { (string?)null },
        new object?[] { "  spaced  " },
        new object?[] { "\\  leading backslash then spaces  " },
        new object?[] { "no/change/needed" },
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void Normalize_IsByteIdenticalToOldInline(string? input)
    {
        Assert.Equal(OldInline(input), RelPath.Normalize(input));
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void ReviewVariant_Normalize_Then_Trim_IsByteIdenticalToOldInline(string? input)
    {
        // TranslationReviewService.NormalizeRel = RelPath.Normalize(p).Trim()
        Assert.Equal(OldReviewInline(input), RelPath.Normalize(input).Trim());
    }

    [Fact]
    public void Normalize_Null_ReturnsEmpty()
    {
        Assert.Equal("", RelPath.Normalize(null));
    }

    [Fact]
    public void Normalize_RepresentativeKeys()
    {
        Assert.Equal("T/T48/T48n2005.xml", RelPath.Normalize("T\\T48\\T48n2005.xml"));
        Assert.Equal("a/b", RelPath.Normalize("/a/b"));
        Assert.Equal("a/b", RelPath.Normalize("\\a\\b"));
    }
}
