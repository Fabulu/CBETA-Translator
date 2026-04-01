using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

/// <summary>
/// Carries all data needed to populate a <see cref="Views.CompareTagsWindow"/>:
/// the rendered document, both users' tags, and their vocabularies.
/// </summary>
public sealed record CompareTagsRequestData(
    string Title,
    RenderedDocument Doc,
    string MyUsername,
    List<DocumentTag> MyTags,
    TagVocabulary? MyVocab,
    string OtherUsername,
    List<DocumentTag> OtherTags,
    TagVocabulary? OtherVocab);
