using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Behavior pins for the consolidated filename sanitizers (audit P3.6). Strict()
/// names existing on-disk community files (per-user jsonl) — its behavior is a
/// compatibility contract, not a style choice.
/// </summary>
public class FileNameSanitizerTests
{
    [Theory]
    [InlineData("Fabulu", "Fabulu")]
    [InlineData("user name.x", "usernamex")] // dots and spaces stripped
    [InlineData("a<b>c", "abc")]
    [InlineData("...   ", "unknown")] // nothing survives → fallback
    [InlineData("", "unknown")]
    public void Strict_StripsAndFallsBack(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.Strict(input));
    }

    [Theory]
    [InlineData("My Export v1.2", "My Export v1.2")] // dots and spaces preserved
    [InlineData("a<b>c", "a_b_c")]                   // invalid → underscore
    [InlineData("", "")]
    public void Lenient_SubstitutesUnderscores(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.Lenient(input));
    }

    [Fact]
    public void Strict_MatchesTheHistoricalCommunityFilenameRule()
    {
        // The rule that named community/translations/<user>/ and reviews/<user>.jsonl:
        // strip Path.GetInvalidFileNameChars() plus '.' and ' '.
        Assert.Equal("theksepyro", FileNameSanitizer.Strict("theksepyro"));
        Assert.Equal("JohnDoe", FileNameSanitizer.Strict("John Doe"));
        Assert.Equal("nameco", FileNameSanitizer.Strict("name.co"));
    }
}
