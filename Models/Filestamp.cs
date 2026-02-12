using System;

namespace CbetaTranslator.App.Models;

public readonly record struct FileStamp(
    string AbsPath,
    long Length,
    long LastWriteUtcTicks
)
{
    public static FileStamp FromFile(string absPath)
    {
        var fi = new System.IO.FileInfo(absPath);
        return new FileStamp(
            absPath,
            fi.Exists ? fi.Length : 0,
            fi.Exists ? fi.LastWriteTimeUtc.Ticks : 0
        );
    }

    public bool IsSameContentAs(FileStamp other)
        => string.Equals(AbsPath, other.AbsPath, StringComparison.OrdinalIgnoreCase)
           && Length == other.Length
           && LastWriteUtcTicks == other.LastWriteUtcTicks;
}
