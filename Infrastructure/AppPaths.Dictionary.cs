using System;
using System.IO;

namespace CbetaTranslator.App.Infrastructure;

public static partial class AppPaths
{
    public static string GetCedictPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "Dict", "cedict_ts.u8"),
            Path.Combine(baseDir, "assets", "dict", "cedict_ts.u8"),
            Path.Combine(baseDir, "Assets", "dict", "cedict_ts.u8"),
            Path.Combine(baseDir, "assets", "Dict", "cedict_ts.u8")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return candidates[0];
    }

    public static void EnsureCedictFolderExists()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Dict", "cedict_ts.u8");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}
