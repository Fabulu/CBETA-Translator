namespace CbetaTranslator.App.Models;

/// <summary>
/// Carries all data needed to populate a <see cref="Views.CompareTranslationsWindow"/>:
/// the original document plus two translated renderings with labels.
/// </summary>
public sealed record CompareTranslationsRequestData(
    string Title,
    RenderedDocument OriginalDoc,
    RenderedDocument TranslationADoc,
    string TranslationALabel,
    RenderedDocument TranslationBDoc,
    string TranslationBLabel);
