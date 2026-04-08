using System;
using System.IO;

namespace ReadZen.App.Infrastructure;

public static partial class AppPaths
{
    public static string GetCedictPath()
    {
        // Try both casings — Windows outputs "Assets/Dict", code historically used "assets/dict"
        // macOS with case-sensitive filesystem needs the exact match
        var lower = Path.Combine(AppContext.BaseDirectory, "assets", "dict", "cedict_ts.u8");
        if (File.Exists(lower)) return lower;

        var upper = Path.Combine(AppContext.BaseDirectory, "Assets", "Dict", "cedict_ts.u8");
        if (File.Exists(upper)) return upper;

        // Return lowercase as default (will trigger "not found" error with instructions)
        return lower;
    }

    public static void EnsureCedictFolderExists()
    {
        var path = GetCedictPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}
