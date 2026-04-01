using System.IO;
using System.Linq;

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

    /// <summary>
    /// Sanitizes a username for use as a filesystem directory name.
    /// Only allows letters, digits, hyphens, and underscores.
    /// </summary>
    public static string SanitizeUsername(string username)
    {
        var safe = string.Concat(username.Where(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        return string.IsNullOrWhiteSpace(safe) ? "User" : safe;
    }

    /// <summary>
    /// Returns the per-user translation directory: community/translations/{sanitized-username}/
    /// </summary>
    public static string GetUserTranslatedDir(string root, string username)
        => Path.Combine(root, "community", "translations", SanitizeUsername(username));
}
