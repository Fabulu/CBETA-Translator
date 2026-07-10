// Infrastructure/TranslationTextNormalizer.cs
using System;
using System.Text;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Text normalization shared by the twin services TranslationAssistantBuildService
/// and TranslationReviewService (dead-code audit 2026-07-09, item #4). Both had a
/// byte-identical private NormalizeLine used to build hash/dedup keys, so the exact
/// transform is a compatibility contract - pinned by
/// <see cref="ReadZen.Tests.Infrastructure.TranslationTextNormalizerTests"/>.
/// </summary>
public static class TranslationTextNormalizer
{
    /// <summary>
    /// Canonicalizes a line for keying/hashing: NFKC, ideographic space and all
    /// whitespace collapsed to single ASCII spaces, then trimmed. Null/whitespace
    /// returns "".
    /// </summary>
    public static string NormalizeLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.Normalize(NormalizationForm.FormKC);
        s = s.Replace("\u3000", " ");
        s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ");

        return s.Trim();
    }
}
