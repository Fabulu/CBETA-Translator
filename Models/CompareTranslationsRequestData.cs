namespace ReadZen.App.Models;

public enum ComparePaneTarget
{
    Original,
    TranslationA,
    TranslationB
}

/// <summary>
/// Carries all data needed to populate a <see cref="Views.CompareTranslationsWindow"/>:
/// the original document plus two translated renderings with labels and optional landing navigation.
/// </summary>
public sealed record CompareTranslationsRequestData(
    string Title,
    string RelPath,
    string SourceAKey,
    string SourceBKey,
    RenderedDocument OriginalDoc,
    RenderedDocument TranslationADoc,
    string TranslationALabel,
    RenderedDocument TranslationBDoc,
    string TranslationBLabel,
    ComparePaneTarget? LandingPane = null,
    NavigationRequest? LandingNavigation = null);
