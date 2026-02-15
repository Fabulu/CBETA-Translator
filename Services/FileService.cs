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
    private static string ToMarkdownRelativePath(string relativePath) => Path.ChangeExtension(relativePath, ".md");

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

    public Task WriteTranslatedAsync(string translatedDir, string relativePath, string translatedXml)
    {
        return Task.Run(() =>
        {
            var path = Path.Combine(translatedDir, relativePath);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, translatedXml ?? string.Empty, Utf8NoBom);
        });
    }

    public Task<(string OriginalXml, string MarkdownText)> ReadOriginalAndMarkdownAsync(string originalDir, string markdownDir, string relativePath)
    {
        return Task.Run(() =>
        {
            var origPath = Path.Combine(originalDir, relativePath);
            var mdPath = Path.Combine(markdownDir, ToMarkdownRelativePath(relativePath));

            string orig = File.Exists(origPath) ? File.ReadAllText(origPath, Utf8NoBom) : string.Empty;
            string md = File.Exists(mdPath) ? File.ReadAllText(mdPath, Utf8NoBom) : string.Empty;

            return (orig, md);
        });
    }

    public Task WriteMarkdownAsync(string markdownDir, string relativePath, string markdownText)
    {
        return Task.Run(() =>
        {
            var mdRelPath = ToMarkdownRelativePath(relativePath);
            var path = Path.Combine(markdownDir, mdRelPath);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, markdownText ?? string.Empty, Utf8NoBom);
        });
    }
}
