using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Per-corpus repository layout discovered under a parent root folder.
/// Each entry pairs the corpus kind with the absolute paths of its
/// originals and translations sub-repos and the actual folder names
/// found inside (xml-p5/xml-p5t for CBETA, xml-open/xml-open-t for Open).
/// </summary>
public sealed record CorpusLayout(
    CorpusKind Kind,
    string OriginalsRepoRoot,
    string TranslationsRepoRoot,
    string OriginalFolderName,
    string TranslatedFolderName,
    string TranslatedCacheFolderName)
{
    public string OriginalDir => Path.Combine(OriginalsRepoRoot, OriginalFolderName);
    public string TranslatedDir => Path.Combine(TranslationsRepoRoot, TranslatedFolderName);
    public string TranslatedCacheDir => Path.Combine(TranslationsRepoRoot, TranslatedCacheFolderName);
}

public partial class AppPaths
{
    // CBETA layout (the original corpus)
    public const string OriginalFolderName = "xml-p5";
    public const string TranslatedFolderName = "xml-p5t";
    public const string TranslatedCacheFolderName = "xml-p5t-cache";

    // OpenZenTexts layout (the parallel free-licensed corpus)
    public const string OpenOriginalFolderName = "xml-open";
    public const string OpenTranslatedFolderName = "xml-open-t";
    public const string OpenTranslatedCacheFolderName = "xml-open-t-cache";

    // Default repo folder names used only for cloning. Discovery scans both
    // CBETA (xml-p5/xml-p5t) and OpenZenTexts (xml-open/xml-open-t) conventions.
    public const string DefaultOriginalRepoFolderName = "CbetaZenTexts";
    public const string DefaultTranslationRepoFolderName = "CbetaZenTranslations";

    // Cache discovery results to avoid repeated filesystem scans.
    private static readonly ConcurrentDictionary<string, (string? OriginalsRepoRoot, string? TranslationsRepoRoot)> _discoveryCache = new();

    /// <summary>
    /// All recognized originals folder names in priority order. New layouts
    /// can be added to this list without changing any caller signatures.
    /// </summary>
    private static readonly string[] OriginalFolderCandidates = { OriginalFolderName, OpenOriginalFolderName };

    /// <summary>
    /// All recognized translations folder names in priority order, mirroring
    /// OriginalFolderCandidates by index.
    /// </summary>
    private static readonly string[] TranslatedFolderCandidates = { TranslatedFolderName, OpenTranslatedFolderName };

    /// <summary>
    /// All recognized translated-cache folder names in priority order, mirroring
    /// OriginalFolderCandidates by index.
    /// </summary>
    private static readonly string[] TranslatedCacheFolderCandidates = { TranslatedCacheFolderName, OpenTranslatedCacheFolderName };

    /// <summary>
    /// Discovers the originals and translations repo roots under a parent folder.
    /// Scans immediate subfolders for any recognized originals layout
    /// (xml-p5/ for CBETA, xml-open/ for OpenZenTexts) and the matching
    /// translations layout (xml-p5t/ or xml-open-t/). A single subfolder
    /// containing BOTH originals AND translations folders is treated as a
    /// legacy single-repo layout and is NOT matched. Results are cached
    /// per parentRoot.
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
        {
            var key = Path.GetFullPath(parentRoot);
            _discoveryCache.TryRemove(key, out _);
            _allCorporaCache.TryRemove(key, out _);
        }
        else
        {
            _discoveryCache.Clear();
            _allCorporaCache.Clear();
        }
    }

    private static readonly ConcurrentDictionary<string, IReadOnlyList<CorpusLayout>> _allCorporaCache = new();

    /// <summary>
    /// Discovers ALL corpus repository layouts present under a parent folder.
    /// Unlike <see cref="DiscoverRepoPaths"/> which returns the first matched
    /// pair (legacy single-corpus model), this enumerates every distinct
    /// (originals, translations) pair it can find — so a parent folder
    /// containing both <c>CbetaZenTexts/CbetaZenTranslations</c> and
    /// <c>OpenZenTexts/OpenZenTranslations</c> as siblings yields two entries.
    ///
    /// The corpus kind is inferred from the folder names that were actually
    /// found inside each repo root (xml-p5 → CBETA, xml-open → Open).
    /// Results are cached per parentRoot.
    /// </summary>
    public static IReadOnlyList<CorpusLayout> DiscoverAllCorpora(string parentRoot)
    {
        var key = Path.GetFullPath(parentRoot);
        if (_allCorporaCache.TryGetValue(key, out var cached))
            return cached;

        var result = DiscoverAllCorporaCore(parentRoot);
        _allCorporaCache[key] = result;
        return result;
    }

    private static IReadOnlyList<CorpusLayout> DiscoverAllCorporaCore(string parentRoot)
    {
        if (!Directory.Exists(parentRoot)) return System.Array.Empty<CorpusLayout>();

        // For each layout-index (0=CBETA, 1=Open), scan immediate subdirs of
        // parentRoot for an originals folder of that layout, and another
        // subdir for the matching translations folder. Pair them up.
        var found = new List<CorpusLayout>();

        for (int i = 0; i < OriginalFolderCandidates.Length; i++)
        {
            string origFolder = OriginalFolderCandidates[i];
            string transFolder = TranslatedFolderCandidates[i];
            string cacheFolder = TranslatedCacheFolderCandidates[i];
            CorpusKind kind = i == 0 ? CorpusKind.Cbeta : CorpusKind.Open;

            string? originalsRoot = null;
            string? translationsRoot = null;
            try
            {
                foreach (var sub in Directory.EnumerateDirectories(parentRoot))
                {
                    bool hasOrig = Directory.Exists(Path.Combine(sub, origFolder));
                    bool hasTrans = Directory.Exists(Path.Combine(sub, transFolder));

                    // Skip legacy single-repo layout (both folders in one subdir)
                    if (hasOrig && hasTrans) continue;

                    if (originalsRoot == null && hasOrig) originalsRoot = sub;
                    if (translationsRoot == null && hasTrans) translationsRoot = sub;
                    if (originalsRoot != null && translationsRoot != null) break;
                }
            }
            catch { }

            if (originalsRoot != null && translationsRoot != null)
            {
                found.Add(new CorpusLayout(
                    kind,
                    originalsRoot,
                    translationsRoot,
                    origFolder,
                    transFolder,
                    cacheFolder));
            }
        }

        return found;
    }

    private static (string? OriginalsRepoRoot, string? TranslationsRepoRoot) DiscoverRepoPathsCore(string parentRoot)
    {
        if (!Directory.Exists(parentRoot)) return (null, null);

        string? originalsRoot = null;
        string? translationsRoot = null;

        foreach (var sub in Directory.EnumerateDirectories(parentRoot))
        {
            bool hasOriginals = ContainsAnyOriginalsFolder(sub);
            bool hasTranslations = ContainsAnyTranslatedFolder(sub);

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

    /// <summary>True if the given directory contains any recognized originals folder.</summary>
    private static bool ContainsAnyOriginalsFolder(string dir)
    {
        foreach (var name in OriginalFolderCandidates)
            if (Directory.Exists(Path.Combine(dir, name))) return true;
        return false;
    }

    /// <summary>True if the given directory contains any recognized translations folder.</summary>
    private static bool ContainsAnyTranslatedFolder(string dir)
    {
        foreach (var name in TranslatedFolderCandidates)
            if (Directory.Exists(Path.Combine(dir, name))) return true;
        return false;
    }

    /// <summary>
    /// Returns the actual originals folder name found inside the given repo
    /// root, or the default (xml-p5) if none of the candidates exist yet.
    /// </summary>
    private static string ResolveOriginalFolderName(string repoRoot)
    {
        foreach (var name in OriginalFolderCandidates)
            if (Directory.Exists(Path.Combine(repoRoot, name))) return name;
        return OriginalFolderName;
    }

    /// <summary>
    /// Returns the actual translations folder name found inside the given
    /// repo root, or the default (xml-p5t) if none of the candidates exist
    /// yet (e.g. an empty translations repo waiting for the first commit).
    /// </summary>
    private static string ResolveTranslatedFolderName(string repoRoot)
    {
        foreach (var name in TranslatedFolderCandidates)
            if (Directory.Exists(Path.Combine(repoRoot, name))) return name;
        return TranslatedFolderName;
    }

    /// <summary>
    /// Returns the actual translated-cache folder name to use for the given
    /// repo root, paired with whichever translations folder name is in use.
    /// </summary>
    private static string ResolveTranslatedCacheFolderName(string repoRoot)
    {
        for (int i = 0; i < TranslatedFolderCandidates.Length; i++)
        {
            if (Directory.Exists(Path.Combine(repoRoot, TranslatedFolderCandidates[i])))
                return TranslatedCacheFolderCandidates[i];
        }
        return TranslatedCacheFolderName;
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
            ? Path.Combine(repoRoot, ResolveOriginalFolderName(repoRoot))
            : Path.Combine(parentRoot, DefaultOriginalRepoFolderName, OriginalFolderName);
    }

    public static string GetTranslatedDir(string parentRoot)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        return repoRoot != null
            ? Path.Combine(repoRoot, ResolveTranslatedFolderName(repoRoot))
            : Path.Combine(parentRoot, DefaultTranslationRepoFolderName, TranslatedFolderName);
    }

    public static string GetTranslatedCacheDir(string parentRoot)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        return repoRoot != null
            ? Path.Combine(repoRoot, ResolveTranslatedCacheFolderName(repoRoot))
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
    /// Looks up the translations repo by scanning the parent root, which
    /// always picks the FIRST discovered translation repo. In multi-corpus
    /// setups (CBETA + OpenZenTexts coexisting), this is wrong because it
    /// always picks CBETA. Use <see cref="GetUserTranslatedDirForRepo"/>
    /// instead when you already know which translations repo is active.
    /// </summary>
    public static string GetUserTranslatedDir(string parentRoot, string username)
    {
        var repoRoot = GetTranslationRepoRoot(parentRoot);
        var baseDir = repoRoot ?? Path.Combine(parentRoot, DefaultTranslationRepoFolderName);
        return Path.Combine(baseDir, "community", "translations", SanitizeUsername(username));
    }

    /// <summary>
    /// Returns the per-user translation directory inside a SPECIFIC
    /// translations repo: {translationsRepoRoot}/community/translations/{sanitized-username}/.
    /// Use this in multi-corpus setups where the caller knows which
    /// translations repo (CBETA vs OpenZen) the user dir should live in,
    /// to avoid the silent "first translation repo wins" trap of
    /// <see cref="GetUserTranslatedDir"/>.
    /// </summary>
    public static string GetUserTranslatedDirForRepo(string translationsRepoRoot, string username)
    {
        return Path.Combine(translationsRepoRoot, "community", "translations", SanitizeUsername(username));
    }
}
