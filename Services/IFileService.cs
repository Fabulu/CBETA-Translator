using System.Collections.Generic;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public interface IFileService
{
    Task<List<string>> EnumerateXmlRelativePathsAsync(string originalDir);
    Task<(string OriginalXml, string TranslatedXml)> ReadPairAsync(string originalDir, string translatedDir, string relativePath);
    Task<(string OriginalXml, string MarkdownText)> ReadOriginalAndMarkdownAsync(string originalDir, string markdownDir, string relativePath);

    Task WriteTranslatedAsync(string translatedDir, string relativePath, string translatedXml);
    Task WriteMarkdownAsync(string markdownDir, string relativePath, string markdownText);

    Task<string?> ReadOriginalAsync(string originalDir, string relPath);
    Task<string?> ReadTranslatedAsync(string translatedDir, string relPath);


}
