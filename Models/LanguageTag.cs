// Models/LanguageTag.cs
// Result of CommentaryLanguageClassifier.Classify. Carries the normalised
// BCP-47 tag, which tier of inference produced it, and a human-readable
// evidence string an admin surface can render alongside the entry.

namespace ReadZen.App.Models;

/// <summary>
/// Language inference result for a single <see cref="CommentaryEntry"/>.
/// </summary>
/// <param name="Bcp47">
/// Normalised BCP-47 language tag (e.g. <c>"ja"</c>, <c>"zh-Hant"</c>) or
/// the literal <c>"unknown"</c> when classification fell through to
/// Tier 4 (default-deny). Always lowercase primary subtag; region/script
/// subtags normalised to Title-case (e.g. <c>"zh-Hant"</c>).
/// </param>
/// <param name="Source">Which tier of the 3-tier strategy produced this tag.</param>
/// <param name="Evidence">
/// Human-readable evidence string for admin/provenance display
/// (e.g. <c>"hiragana/katakana count=147"</c>,
/// <c>"genre marker \"講話\" in title"</c>, <c>"xml:lang=\"ja\""</c>).
/// </param>
public sealed record LanguageTag(
    string Bcp47,
    LanguageInferenceSource Source,
    string Evidence);
