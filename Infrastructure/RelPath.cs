// Infrastructure/RelPath.cs
namespace ReadZen.App.Infrastructure;

/// <summary>
/// Relative-path key normalization, consolidated from five byte-identical private
/// copies (dead-code audit 2026-07-09, item #3): CommunityDataService,
/// TranslationMemoryService, SearchIndexService.NormalizeRelKey, GitTabViewModel,
/// TranslationAssistantBuildService.
///
/// These strings KEY INTO the shared search and translation-memory dictionaries,
/// so the exact transform is a compatibility contract — pinned by
/// <see cref="ReadZen.Tests.Infrastructure.RelPathTests"/>.
///
/// KNOWN UN-ROUTED IDENTICAL COPIES (not exhaustive: the "five copies" figure above
/// counts only the ones actually routed in that audit). The following are the SAME
/// byte-identical expression and remain un-routed for now — they key into the same
/// rel-path-keyed dictionaries, so any future edit to <see cref="Normalize"/> must be
/// mirrored here (or these should be routed in a follow-up):
/// ZenTextsService.Norm, IndexCacheService.NormalizePathKey,
/// MainWindowViewModel.NormalizeRel, plus three inline occurrences in
/// MainWindow.axaml.cs (SourceTitleResolver / SourceTitleDetailResolver / TitleLookup).
///
/// Two nearby copies deliberately DIFFER and are handled at their call sites:
/// TranslationReviewService.NormalizeRel appends a trailing <c>.Trim()</c>;
/// TranslationLicenseService.NormalizeRel is Replace-only (no null guard, no
/// TrimStart) and is intentionally NOT routed here.
/// </summary>
public static class RelPath
{
    /// <summary>
    /// Normalizes a relative path to a dictionary key: null → empty, backslashes →
    /// forward slashes, leading slashes trimmed.
    /// </summary>
    public static string Normalize(string? p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');
}
