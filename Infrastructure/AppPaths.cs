using System.IO;

namespace CbetaTranslator.App.Infrastructure;

public partial class AppPaths
{
    public const string OriginalFolderName = "xml-p5";
    public const string TranslatedFolderName = "xml-p5t";
    public const string MarkdownFolderName = "md-p5t";

    public static string GetOriginalDir(string root) => Path.Combine(root, OriginalFolderName);
    public static string GetTranslatedDir(string root) => Path.Combine(root, TranslatedFolderName);
    public static string GetMarkdownDir(string root) => Path.Combine(root, MarkdownFolderName);

    public static void EnsureTranslatedDirExists(string root)
    {
        var dir = GetTranslatedDir(root);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public static void EnsureMarkdownDirExists(string root)
    {
        var dir = GetMarkdownDir(root);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
