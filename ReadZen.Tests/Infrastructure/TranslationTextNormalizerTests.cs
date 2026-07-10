using System.Text;
using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Behavior pin for the consolidated <see cref="TranslationTextNormalizer.NormalizeLine"/>
/// (dead-code audit 2026-07-09, item #4). The twin services used its output to
/// build hash/dedup keys, so it must stay byte-identical to the old inline body.
/// </summary>
public class TranslationTextNormalizerTests
{
    // The exact private body both twin services shared.
    private static string OldInline(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        s = s.Normalize(NormalizationForm.FormKC);
        s = s.Replace("\u3000", " ");
        s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        while (s.Contains("  ", System.StringComparison.Ordinal))
            s = s.Replace("  ", " ");
        return s.Trim();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("hello world")]
    [InlineData("  leading  and   trailing   ")]
    [InlineData("tab\tand\nnewline\rreturn")]
    [InlineData("full\u3000width\u3000space")]
    [InlineData("\uFF21\uFF22\uFF23")] // fullwidth ABC -> NFKC ASCII
    [InlineData("\u5E2B\u3000\u793A\u773E")] // CJK with ideographic space
    public void NormalizeLine_IsByteIdenticalToOldInline(string? input)
    {
        Assert.Equal(OldInline(input), TranslationTextNormalizer.NormalizeLine(input));
    }
}
