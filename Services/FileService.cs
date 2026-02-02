using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class FileService : IFileService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public Task<List<string>> EnumerateXmlRelativePathsAsync(string originalDir)
    {
        return Task.Run(() =>
        {
            return Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(originalDir, f))
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public Task<(string OriginalXml, string TranslatedXml)> ReadPairAsync(string originalDir, string translatedDir, string relativePath)
    {
        return Task.Run(() =>
        {
            var origPath = Path.Combine(originalDir, relativePath);
            var tranPath = Path.Combine(translatedDir, relativePath);

            string orig = File.Exists(origPath) ? File.ReadAllText(origPath, Utf8NoBom) : string.Empty;
            string tran = File.Exists(tranPath) ? File.ReadAllText(tranPath, Utf8NoBom) : string.Empty;

            return (orig, tran);
        });
    }
}
