using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CbetaTranslator.App.Infrastructure;

public partial class AppPaths
{
    public const string OriginalFolderName = "xml-p5";
    public const string TranslatedFolderName = "xml-p5t";
    public const string TranslatedCacheFolderName = "xml-p5t-cache";

    // Default repo folder names used only for cloning. Discovery uses xml-p5/xml-p5t conventions.
    public const string DefaultOriginalRepoFolderName = "CbetaZenTexts";
    public const string DefaultTranslationRepoFolderName = "CbetaZenTranslations";

    // Cache discovery results to avoid repeated filesystem scans.
    private static readonly ConcurrentDictionary<string, (string? OriginalsRepoRoot, string? TranslationsRepoRoot)> _discoveryCache = new();

    /// <summary>
    /// Discovers the originals and translations repo roots under a parent folder.
    /// Scans immediate subfolders for xml-p5/ (originals) and xml-p5t/ (translations).
    /// A single subfolder containing BOTH xml-p5/ and xml-p5t/ is treated as a legacy
    /// single-repo layout and is NOT matched (to avoid returning the same path for both).
    /// Results are cached per parentRoot.
    /// </summary>
    public static (string? OriginalsRepoRoot, string? TranslationsRepoRoot) DiscoverRepoPaths(string parentRoot)
    {
        var key = Path.GetFullPath(parentRoot);
        if (_discoveryCache.TryGetValue(key, out var cached))
            return cached;

        var result = DiscoverRepoPathsCore(parentRoot);
        _discoveryCache[key] = result;
        return result;
    }

    /// <summary>
    /// Invalidates the cached discovery result for a parent root.
    /// Call after cloning repos or changing directory structure.
    /// </summary>
    public static void InvalidateDiscoveryCache(string? parentRoot = null)
    {
        if (parentRoot != null)
            _discoveryCache.TryRemove(Path.GetFullPath(parentRoot), out _);
        else
            _discoveryCache.Clear();
    }

    private static (string? OriginalsRepoRoot, string? TranslationsRepoRoot) DiscoverRepoPathsCore(string parentRoot)
    {
        if (!Directory.Exists(parentRoot)) return (null, null);

        string? originalsRoot = null;
        string? translationsRoot = null;

        foreach (var sub in Directory.EnumerateDirectories(parentRoot))
        {
            bool hasOriginals = Directory.Exists(Path.Combine(sub, OriginalFolderName));
            bool hasTranslations = Directory.Exists(Path.Combine(sub, TranslatedFolderName));

            // Skip subfolders that contain BOTH — that's a legacy single-repo layout,
            // not a proper split repo. Each repo should have exactly one.
            if (hasOriginals && hasTranslations)
                continue;

            if (originalsRoot == null && hasOriginals)
                originalsRoot = sub;
            if (translationsRoot == null && hasTranslations)
                translationsRoot = sub;
            if (originalsRoot != null && translationsRoot != null)
                break;
        }

        return (originalsRoot, translationsRoot);
    }

    /// <summary>
    /// Validates that both repos exist under the parent root.
    /// </summary>
    public static bool ValidateBothReposExist(string parentRoot)
    {
        var (orig, trans) = DiscoverRepoPaths(parentRoot);
        return orig != null && trans != null;
    }

    public static string? GetOriginalRepoRoot(string parentRoot)
        => DiscoverRepoPaths(parentRoot).OriginalsRepoRoot;

    public static string? GetTranslationRepoRoot(string parentRoot)
        => DiscoverRepoPaths(parentRoot).TranslationsRepoRoot;

    public static string GetOriginalDir(string parentRoot)
    {
        var repoRoot = GetOriginalRepoRoot(parentRoot);
        return repoRoot != null
            ? Path.Combine(repoRoot, OriginalFolderName)
            : Path.Combine(parentRoot, DefaultOriginalRepoFolderName, OriginalFolderName);
    }

    public static string GetTranslatedDir(string parentRoot)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        return repoRoot != null
            ? Path.Combine(repoRoot, TranslatedFolderName)
            : Path.Combine(parentRoot, DefaultTranslationRepoFolderName, TranslatedFolderName);
    }

    public static string GetTranslatedCacheDir(string parentRoot)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        return repoRoot != null
            ? Path.Combine(repoRoot, TranslatedCacheFolderName)
            : Path.Combine(parentRoot, DefaultTranslationRepoFolderName, TranslatedCacheFolderName);
    }

    public static void EnsureTranslatedDirExists(string parentRoot)
    {
        var dir = GetTranslatedDir(parentRoot);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var cacheDir = GetTranslatedCacheDir(parentRoot);
        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);
    }

    /// <summary>
    /// Sanitizes a username for use as a filesystem directory name.
    /// Only allows letters, digits, hyphens, and underscores.
    /// </summary>
    public static string SanitizeUsername(string username)
    {
        var safe = string.Concat(username.Select(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-'));
        // Collapse consecutive hyphens
        while (safe.Contains("--")) safe = safe.Replace("--", "-");
        safe = safe.Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "User" : safe;
    }

    /// <summary>
    /// Returns the per-user translation directory: {translationsRepo}/community/translations/{sanitized-username}/
    /// </summary>
    public static string GetUserTranslatedDir(string parentRoot, string username)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        var baseDir = repoRoot ?? Path.Combine(parentRoot, DefaultTranslationRepoFolderName);
        return Path.Combine(baseDir, "community", "translations", SanitizeUsername(username));
    }
}
