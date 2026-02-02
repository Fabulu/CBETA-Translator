using System.Collections.Generic;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public interface IFileService
{
    Task<List<string>> EnumerateXmlRelativePathsAsync(string originalDir);
    Task<(string OriginalXml, string TranslatedXml)> ReadPairAsync(string originalDir, string translatedDir, string relativePath);
}
