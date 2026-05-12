// Services/CommentaryLanguageClassifier.cs
// Pure, stateless classifier that assigns a LanguageTag to a CommentaryEntry
// via a 3-tier strategy. Designed for the Faith-in-Mind critical edition
// where all 17 Japanese commentary titles are pure CJK and thus
// indistinguishable from Chinese by script alone.
//
// Default-deny posture (SPEC v2 §"Default-deny posture", user reinforced
// 2026-05-12): when no signal triggers, the tag defaults to "unknown" —
// NOT "zh-Hant". The reader-side filter excludes "unknown" so any item
// that fell through the classifier is silently dropped at the reader call
// site. Chinese commentary must be positively identified (Tier 1 explicit
// metadata is the recommended path) to surface to readers.
//
// Classifier is pure: no I/O, no instance state, idempotent. The owning
// CommentaryService is responsible for mutating CommentaryEntry.Language
// and stashing the full LanguageTag in a side map for admin inspection.

using System;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class CommentaryLanguageClassifier
{
    /// <summary>
    /// Threshold for Tier 2 (kana count in body). Three kana characters
    /// is a deliberate floor: a Chinese commentary that quotes 「これ」
    /// or similar two-character Japanese phrases stays classified as
    /// not-Japanese (sub-threshold), while any prose-length Japanese
    /// body comfortably exceeds it.
    /// </summary>
    public const int KanaThreshold = 3;

    /// <summary>
    /// Curated Japanese-commentary-genre keywords (Recon D). When present
    /// in a commentary entry's title and Tiers 1+2 didn't fire, this
    /// strongly suggests a Japanese-language scholarly genre:
    /// 講話 (kōwa, "lecture"), 拈提 (nentei), 夜塘水 (yatōsui), and
    /// the wayaku / kokuyaku family meaning "Japanese translation".
    /// </summary>
    internal static readonly string[] JapaneseGenreMarkers =
    {
        "講話",
        "拈提",
        "夜塘水",
        "和譯",
        "和訳",
        "国訳",
        "國訳",
    };

    /// <summary>
    /// Classifies <paramref name="entry"/> using:
    /// (1) explicit <c>entry.Language</c> (non-null, non-empty) → trusted;
    /// (2) kana count in <c>entry.Body</c> &gt;= <see cref="KanaThreshold"/> → <c>"ja"</c>;
    /// (3) Japanese-genre keyword in <c>entry.Title</c> → <c>"ja"</c>;
    /// (4) default → <c>"unknown"</c> (default-deny — see file header).
    /// The resulting tag's <c>Bcp47</c> is normalised: primary subtag
    /// lowercased, region/script subtags Title-cased. Non-null
    /// <paramref name="entry"/> required; pass-through entries with no
    /// signals get <c>("unknown", Default, "no Japanese signals")</c>.
    /// </summary>
    public static LanguageTag Classify(CommentaryEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        // Tier 1: explicit metadata wins unconditionally.
        if (!string.IsNullOrWhiteSpace(entry.Language))
        {
            var raw = entry.Language!;
            return new LanguageTag(
                NormalizeBcp47(raw),
                LanguageInferenceSource.ExplicitMetadata,
                $"language=\"{raw}\"");
        }

        // Tier 2: kana-in-body. Strong signal for Japanese prose; Chinese
        // commentary quoting a few hiragana stays under the threshold.
        int kana = CjkMatchNormalizer.CountKana(entry.Body);
        if (kana >= KanaThreshold)
        {
            return new LanguageTag(
                "ja",
                LanguageInferenceSource.ContentKana,
                $"hiragana/katakana count={kana}");
        }

        // Tier 3: curated title-keyword whitelist. Handles the FiM-typical
        // case where a Japanese-genre commentary has no body (most C*
        // items have no OCR'd text) but its title contains 講話 / 和譯 etc.
        if (!string.IsNullOrEmpty(entry.Title))
        {
            foreach (var marker in JapaneseGenreMarkers)
            {
                if (entry.Title!.Contains(marker, StringComparison.Ordinal))
                {
                    return new LanguageTag(
                        "ja",
                        LanguageInferenceSource.TitleKeyword,
                        $"genre marker \"{marker}\" in title");
                }
            }
        }

        // Tier 4: default-deny. NOT "zh-Hant" — items must be positively
        // identified as Chinese (Tier 1) to surface to readers.
        return new LanguageTag(
            "unknown",
            LanguageInferenceSource.Default,
            "no Japanese signals");
    }

    /// <summary>
    /// Normalises a BCP-47 tag to conventional casing: primary subtag
    /// lowercase, region subtags (length 2) uppercase, script subtags
    /// (length 4) Title-case. Whitespace trimmed. Returns the empty
    /// string for null or whitespace-only input (so callers can compare
    /// against a non-null value). Examples:
    /// <c>"JA"</c> → <c>"ja"</c>,
    /// <c>"zh-hant"</c> → <c>"zh-Hant"</c>,
    /// <c>"ja-JP"</c> → <c>"ja-JP"</c>,
    /// <c>null</c> / <c>"  "</c> → <c>""</c>.
    /// </summary>
    public static string NormalizeBcp47(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return tag ?? string.Empty;

        var trimmed = tag.Trim();
        var parts = trimmed.Split('-');
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
                continue;

            if (i == 0)
            {
                parts[i] = part.ToLowerInvariant();
            }
            else if (part.Length == 4)
            {
                // Script subtag — Title-case (e.g. "Hant", "Hans", "Latn").
                parts[i] = char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
            }
            else if (part.Length == 2 || part.Length == 3)
            {
                // Region subtag (length 2-3) or extlang — uppercase regions.
                parts[i] = part.ToUpperInvariant();
            }
            else
            {
                // Variant or extension — leave as lowercase.
                parts[i] = part.ToLowerInvariant();
            }
        }
        return string.Join('-', parts);
    }
}
