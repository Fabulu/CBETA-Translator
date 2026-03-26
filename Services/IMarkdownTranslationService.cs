using System.Collections.Generic;

namespace CbetaTranslator.App.Services;

public interface IMarkdownTranslationService
{
    string ConvertTeiToMarkdown(string originalXml, string? sourceFileName);
    string MergeMarkdownIntoTei(string originalXml, string markdown, out int updatedCount);
    string CreateReadableInlineEnglishXml(string mergedXml);
    bool IsCurrentMarkdownFormat(string markdown);
    bool TryExtractPdfSectionsFromMarkdown(
        string markdown,
        out List<string> chineseSections,
        out List<string> englishSections,
        out string? error);
}
