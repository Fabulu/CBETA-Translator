// Models/LanguageInferenceSource.cs
// Identifies which tier of the CommentaryLanguageClassifier produced a
// given LanguageTag. Used by admin/provenance surfaces to render
// inference provenance (e.g. "ja (inferred from kana in body, count=147)").

namespace ReadZen.App.Models;

public enum LanguageInferenceSource
{
    /// <summary>
    /// Tier 1: the entry's <c>language</c> field was non-null / non-empty.
    /// Trusted unconditionally — package authors are expected to set this
    /// explicitly for Chinese commentary so it can surface to readers.
    /// </summary>
    ExplicitMetadata,

    /// <summary>
    /// Tier 2: hiragana + katakana count in the entry's body crossed the
    /// threshold (>= 3) — strong signal that the prose is Japanese.
    /// </summary>
    ContentKana,

    /// <summary>
    /// Tier 3: the entry's title matched one of a curated Japanese-commentary
    /// genre keywords (講話, 拈提, 夜塘水, 和譯/和訳, 国訳/國訳).
    /// </summary>
    TitleKeyword,

    /// <summary>
    /// Tier 4: no signal triggered. Under the default-deny posture the
    /// resulting tag is <c>"unknown"</c>, not a positively-identified
    /// language. The reader filter excludes "unknown" by design.
    /// </summary>
    Default,
}
