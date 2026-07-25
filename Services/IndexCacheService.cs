using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class IndexCacheService : IIndexCacheService
{
    private readonly INavStatusEvaluator _navEvaluator;
    private readonly ITranslationStatusService _statusService;

    public IndexCacheService(INavStatusEvaluator navEvaluator, ITranslationStatusService statusService)
    {
        _navEvaluator = navEvaluator ?? throw new ArgumentNullException(nameof(navEvaluator));
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
    }

    /// <summary>
    /// Convenience constructor (tests / legacy call sites): wraps a bare status service in
    /// a real <see cref="NavStatusEvaluator"/> so the single gated status pipeline is used
    /// even when only a status service is supplied. The DI-preferred constructor injects
    /// the shared singleton evaluator.
    /// </summary>
    public IndexCacheService(ITranslationStatusService statusService)
        : this(new NavStatusEvaluator(statusService, new IndexedTranslationService()), statusService)
    {
    }

    private const string CacheFileName = "index.cache.json";

    // On-disk schema version. v5 (NAV_CACHE_REDESIGN §2): machine-independent, bundleable —
    // source manifest, content sigs, demoted RootPath. A v4 cache is rejected in PR-NV2
    // (rebuilt) and migrated in PR-NV3.
    private const int CacheVersion = 5;

    // Logic-version gate. Bump to force a rebuild when status logic changes.
    private const string CacheBuildGuid = "nav-v6-bundleable";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    // Token for the canonical (xml-p5t / xml-open-t) translation source; community sources
    // use "user:{username}". A relative source id, resolved to an absolute path at use time.
    private const string CanonicalToken = "canonical";

    public string GetCachePath(string root)
        => Path.Combine(root, CacheFileName);

    // ---------------------------------------------------------------- load

    public async Task<IndexCache?> TryLoadAsync(string root)
    {
        var result = await LoadAsync(root);
        return result.Status == NavCacheLoadStatus.V5 ? result.Cache : null;
    }

    public Task<NavCacheLoadResult> LoadAsync(string root)
        => LoadFromFileAsync(GetCachePath(root));

    /// <summary>
    /// Root-tolerant classified load from an arbitrary cache FILE (not just the canonical
    /// <c>{root}/index.cache.json</c>) — used by <see cref="LoadAsync"/> and by
    /// <see cref="TryAdoptBundle"/> to classify a shipped bundle before adopting it.
    /// </summary>
    private async Task<NavCacheLoadResult> LoadFromFileAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
                return NavCacheLoadResult.Unusable;

            var json = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(json))
                return NavCacheLoadResult.Unusable;

            var cache = JsonSerializer.Deserialize<IndexCache>(json, JsonOpts);
            if (cache == null || cache.Entries == null || cache.Entries.Count == 0)
                return NavCacheLoadResult.Unusable;

            // v5 (NAV_CACHE_REDESIGN §2.1): RootPath is NO LONGER compared — a foreign-root
            // cache is adopted and re-homed on the next save.

            // A v4 cache is migratable (PR-NV3); in PR-NV2 the ladder still rebuilds it.
            if (cache.Version == 4)
                return new NavCacheLoadResult(NavCacheLoadStatus.V4NeedsMigration, cache);

            // Any other below-current version lacks the v5 fields ⇒ unusable.
            if (cache.Version < CacheVersion)
                return NavCacheLoadResult.Unusable;

            if (!string.Equals(cache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal))
                return NavCacheLoadResult.Unusable;

            return new NavCacheLoadResult(NavCacheLoadStatus.V5, cache);
        }
        catch
        {
            return NavCacheLoadResult.Unusable;
        }
    }

    /// <summary>
    /// Reads the current HEAD commit SHA of the git repo rooted at
    /// <paramref name="repoRoot"/>, without invoking the git binary. RETIRED for the nav
    /// path (no nav caller after v5); kept because other services may still reference it.
    /// </summary>
    public static string? TryGetGitHead(string repoRoot)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoRoot)) return null;
            var gitDir = Path.Combine(repoRoot, ".git");
            if (!Directory.Exists(gitDir)) return null;

            var headFile = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headFile)) return null;

            var head = File.ReadAllText(headFile).Trim();
            if (string.IsNullOrEmpty(head)) return null;

            if (!head.StartsWith("ref:", StringComparison.Ordinal))
                return head;

            var refPath = head.Substring(4).Trim();
            if (string.IsNullOrEmpty(refPath)) return null;

            var refFile = Path.Combine(gitDir, refPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refFile))
            {
                var sha = File.ReadAllText(refFile).Trim();
                if (!string.IsNullOrEmpty(sha)) return sha;
            }

            var packedRefs = Path.Combine(gitDir, "packed-refs");
            if (File.Exists(packedRefs))
            {
                foreach (var line in File.ReadLines(packedRefs))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("^", StringComparison.Ordinal)) continue;
                    var sp = line.IndexOf(' ');
                    if (sp <= 0 || sp >= line.Length - 1) continue;
                    var name = line.Substring(sp + 1).Trim();
                    if (string.Equals(name, refPath, StringComparison.Ordinal))
                    {
                        var sha = line.Substring(0, sp).Trim();
                        if (!string.IsNullOrEmpty(sha)) return sha;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ---------------------------------------------------------------- save

    public async Task SaveAsync(string root, IndexCache cache)
    {
        cache.RootPath = root;
        cache.BuiltUtc = DateTime.UtcNow;
        cache.Version = CacheVersion;
        cache.BuildGuid = CacheBuildGuid;
        cache.TitlesHash = ComputeTitlesHash(root);
        // NOTE (NAV_CACHE_REDESIGN §5.5): SaveAsync stamps only the always-derivable
        // metadata. CorpusKind/OriginalsSig/SourceSig are set by the caller (BuildAsync /
        // RefreshAsync) and survive by construction; SaveAsync never invents them. A bare
        // "new IndexCache { Entries = … }" save (the MWVM sweep, until PR-NV4) therefore
        // leaves those sig fields null — harmless: the next launch simply recomputes them
        // (per-entry gate, no mass recompute) rather than hitting the fast path.

        var path = GetCachePath(root);
        var json = JsonSerializer.Serialize(cache, JsonOpts);

        // Atomic write: serialize to a per-write unique temp file, then move it over the
        // target in one step. The GUID temp name stops concurrent savers from clobbering
        // each other's temp file (NAV_CACHE_REDESIGN §6).
        var tmpPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tmpPath, json, Utf8NoBom);
            File.Move(tmpPath, path, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw;
        }
    }

    // ---------------------------------------------------------------- bundle adoption (§4.2 row 3)

    /// <summary>
    /// Test seam (NavBundleAdoptionTests): a staged synthetic bundle FILE path that
    /// <see cref="TryAdoptBundle"/> uses instead of resolving the exe-adjacent asset via
    /// <see cref="Infrastructure.AppPaths.GetBundledNavCachePath"/>. Null in production.
    /// </summary>
    internal string? TestOnlyBundlePathOverride;

    /// <inheritdoc/>
    public async Task<IndexCache?> TryAdoptBundle(
        string root, Models.CorpusKind activeKind, CancellationToken ct = default)
    {
        try
        {
            var bundlePath = TestOnlyBundlePathOverride
                ?? Infrastructure.AppPaths.GetBundledNavCachePath(activeKind);
            if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
                return null; // no bundle ships (raw source build) ⇒ ladder cold-builds

            // Classify the bundle exactly like a local cache: only a usable v5 is adoptable.
            var load = await LoadFromFileAsync(bundlePath);
            if (load.Status != NavCacheLoadStatus.V5 || load.Cache == null)
                return null; // corrupt / wrong version / wrong guid ⇒ ladder cold-builds

            // Kind safety (NAV_CACHE_REDESIGN §4.3): an Open root never adopts a Cbeta bundle.
            var activeKindStr = activeKind == Models.CorpusKind.Open ? "Open" : "Cbeta";
            if (!string.Equals(load.Cache.CorpusKind, activeKindStr, StringComparison.Ordinal))
                return null;

            // Adopt via an atomic tmp+move BYTE copy into the local cache path (master
            // TryAdoptBundleAsync pattern) — no re-homing, nothing in v5 is absolute. The
            // caller then runs the gated RefreshAsync as catch-up.
            var target = GetCachePath(root);
            var tmp = target + ".adopt-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(root);
                using (var src = new FileStream(bundlePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var dst = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await src.CopyToAsync(dst, ct);
                }
                File.Move(tmp, target, overwrite: true);
            }
            catch
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return null;
            }

            return load.Cache;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// SHA256 (hex) of the shipped <c>titles.jsonl</c> bytes — the single content input
    /// (beyond the per-entry file gate) the nav display fields depend on. Returns a stable
    /// sentinel when the file is absent/unreadable so its appearance/disappearance flips
    /// the hash and re-derives display fields (NAV_CACHE_REDESIGN §3.4).
    /// </summary>
    private static string ComputeTitlesHash(string root)
    {
        try
        {
            var titlesPath = Path.Combine(root, "titles.jsonl");
            if (!File.Exists(titlesPath))
                return "no-titles";
            using var fs = File.OpenRead(titlesPath);
            var hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return "titles-error";
        }
    }

    // ---------------------------------------------------------------- titles

    private sealed class TitleInfo
    {
        public string? Zh { get; set; }
        public string? En { get; set; }
        public string? EnShort { get; set; }
    }

    private static string NormalizePathKey(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private static Dictionary<string, TitleInfo> LoadTitlesMap(string root)
    {
        var titlesPath = Path.Combine(root, "titles.jsonl");
        var dict = new Dictionary<string, TitleInfo>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(titlesPath))
            return dict;

        foreach (var line in File.ReadLines(titlesPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;

                if (!r.TryGetProperty("path", out var pathEl)) continue;
                var path = pathEl.GetString();
                if (string.IsNullOrWhiteSpace(path)) continue;

                var key = NormalizePathKey(path);

                string? zh = r.TryGetProperty("zh", out var zhEl) ? zhEl.GetString() : null;
                string? en = r.TryGetProperty("en", out var enEl) ? enEl.GetString() : null;
                string? enShort = r.TryGetProperty("enShort", out var esEl) ? esEl.GetString() : null;

                dict[key] = new TitleInfo { Zh = zh, En = en, EnShort = enShort };
            }
            catch
            {
                // ignore bad lines
            }
        }

        return dict;
    }

    public TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true)
    {
        return _statusService.ComputeStatusForPairLive(origAbs, tranAbs, rootForLogs, relKeyForLogs, verboseLog);
    }

    // ---------------------------------------------------------------- source manifest (§3.1)

    /// <summary>One translation-source candidate for a rel: its token, absolute path,
    /// content signature, and local (size, mtime) at scan time.</summary>
    private readonly record struct SourceCandidate(
        string Token, string AbsPath, string ContentSig, long SizeBytes, long MtimeTicks);

    /// <summary>The translation-source manifest: relKey → ordered candidates (canonical
    /// first), plus the SourceSig over all lines.</summary>
    private sealed class SourceManifest
    {
        public Dictionary<string, List<SourceCandidate>> ByRel { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public string SourceSig { get; set; } = "";
    }

    /// <summary>
    /// Enumerates every translation-source dir ONCE (canonical + each
    /// community/translations/{user}) producing relKey → [(Token, ContentSig)] and the
    /// SourceSig (NAV_CACHE_REDESIGN §3.1). ContentSig is reused from a matching stored
    /// hint (skip re-hashing) or freshly SHA256-16'd on a miss. Untranslated rels are
    /// simply absent — their candidate set is empty ⇒ trivially Red, zero filesystem touch.
    /// </summary>
    private static SourceManifest ScanTranslationSources(
        string translatedDir,
        string root,
        Dictionary<string, (long size, long mtime, string sig)> hintLookup)
    {
        var manifest = new SourceManifest();

        void AddDir(string dir, string token)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories); }
            catch { return; }

            foreach (var f in files)
            {
                long size, mtime;
                try { var fi = new FileInfo(f); size = fi.Length; mtime = fi.LastWriteTimeUtc.Ticks; }
                catch { continue; /* vanished between enumerate and stat */ }

                var relKey = NormalizePathKey(Path.GetRelativePath(dir, f));

                string sig;
                if (hintLookup.TryGetValue(HintKey(relKey, token), out var h)
                    && h.size == size && h.mtime == mtime)
                {
                    sig = h.sig; // hint hit — no read
                }
                else
                {
                    sig = Sha256Hex16OfFile(f); // hint miss — hash (heal happens on save)
                }

                if (!manifest.ByRel.TryGetValue(relKey, out var list))
                {
                    list = new List<SourceCandidate>(1);
                    manifest.ByRel[relKey] = list;
                }
                list.Add(new SourceCandidate(token, f, sig, size, mtime));
            }
        }

        // Canonical first so it wins the legacy read-path resolution and ordering.
        AddDir(translatedDir, CanonicalToken);

        var communityRoot = Path.Combine(root, "community", "translations");
        if (Directory.Exists(communityRoot))
        {
            IEnumerable<string> userDirs;
            try { userDirs = Directory.EnumerateDirectories(communityRoot); }
            catch { userDirs = Array.Empty<string>(); }
            foreach (var userDir in userDirs)
                AddDir(userDir, "user:" + Path.GetFileName(userDir));
        }

        manifest.SourceSig = ComputeSourceSig(manifest);
        return manifest;
    }

    // Runtime-only composite key (candidate-set gate + hint lookup). An explicit, NAMED
    // delimiter (NAV_CACHE_REDESIGN NV2 review) keeps two DISTINCT (a, b) pairs from
    // concatenating to the same string: e.g. ("canonical","ab") and ("canonicala","b")
    // would both collapse to "canonicalab" and falsely SetEqual under empty/short sigs.
    // (The value was already a U+0001 char embedded raw in the literals; hoisting it into
    // this constant removes that invisible-char fragility.) The separator cannot occur in a
    // token, relKey, or hex sig. Keys are never serialized -- pure runtime, no format bump.
    internal const string KeySeparator = "\u0001";

    internal static string ComposeKey(string a, string b) => a + KeySeparator + b;

    private static string HintKey(string relKey, string token) => ComposeKey(relKey, token);

    /// <summary>Hash over the sorted <c>"{token}|{relKey}|{contentSig}"</c> lines (§2.1).</summary>
    private static string ComputeSourceSig(SourceManifest manifest)
    {
        var lines = new List<string>();
        foreach (var kv in manifest.ByRel)
            foreach (var c in kv.Value)
                lines.Add(c.Token + "|" + kv.Key + "|" + c.ContentSig);
        lines.Sort(StringComparer.Ordinal);
        return Sha256Hex16(string.Concat(lines.Select(l => l + "\n")));
    }

    /// <summary>
    /// Builds the (relKey,token) → (size,mtime,sig) hint lookup from an existing cache so
    /// <see cref="ScanTranslationSources"/> can skip re-hashing unchanged candidates
    /// (NAV_CACHE_REDESIGN §2.2). Pairs each stored <see cref="NavSourceRecord"/> with its
    /// <see cref="NavSourceHint"/> by token.
    /// </summary>
    private static Dictionary<string, (long size, long mtime, string sig)> BuildHintLookup(IndexCache? cache)
    {
        var lookup = new Dictionary<string, (long, long, string)>(StringComparer.Ordinal);
        if (cache?.Entries == null) return lookup;

        foreach (var e in cache.Entries)
        {
            if (e.Sources == null || e.Sources.Count == 0 || e.TranLocalHints == null) continue;
            var relKey = NormalizePathKey(e.RelPath);
            foreach (var s in e.Sources)
            {
                var hint = e.TranLocalHints.FirstOrDefault(h =>
                    string.Equals(h.Token, s.Token, StringComparison.Ordinal));
                if (hint == null || string.IsNullOrEmpty(s.ContentSig)) continue;
                lookup[HintKey(relKey, s.Token)] = (hint.SizeBytes, hint.MtimeTicks, s.ContentSig);
            }
        }
        return lookup;
    }

    // ---------------------------------------------------------------- originals sig (§2.1)

    /// <summary>
    /// <c>"files={N};bytes={SUM};pathsig={P16}"</c> over the originals — the master
    /// corpus-stamp recipe (NAV_CACHE_REDESIGN §2.1). Stat-only, mtime-immune. P16 =
    /// SHA256-16 over sorted <c>"{relKey}:{size}\n"</c> lines (catches renames/moves).
    /// </summary>
    private static string ComputeOriginalsSig(IReadOnlyList<(string relKey, long size)> originals)
    {
        long totalBytes = 0;
        var pathLines = new List<string>(originals.Count);
        foreach (var (relKey, size) in originals)
        {
            totalBytes += size;
            pathLines.Add(relKey + ":" + size + "\n");
        }
        pathLines.Sort(StringComparer.Ordinal);
        var pathSig = Sha256Hex16(string.Concat(pathLines));
        return $"files={originals.Count};bytes={totalBytes};pathsig={pathSig}";
    }

    // ---------------------------------------------------------------- entry building

    private static string CorpusKindOf(string translatedDir)
    {
        var name = Path.GetFileName(translatedDir.TrimEnd('/', '\\'));
        return string.Equals(name, AppPathsOpenTranslatedFolderName, StringComparison.OrdinalIgnoreCase)
            ? "Open" : "Cbeta";
    }

    // Mirror of AppPaths.OpenTranslatedFolderName without taking a hard dependency on the
    // Infrastructure partial (kept a literal here so the derivation is self-contained).
    private const string AppPathsOpenTranslatedFolderName = "xml-open-t";

    // Green > Yellow > Red — mirrors NavStatusEvaluator / MainWindowViewModel rank.
    private static int Rank(TranslationStatus s) => s switch
    {
        TranslationStatus.Green => 2,
        TranslationStatus.Yellow => 1,
        _ => 0,
    };

    private static TranslationStatus MaxStatus(List<NavSourceRecord> sources)
    {
        var best = TranslationStatus.Red;
        foreach (var s in sources)
            if (Rank(s.Status) > Rank(best))
                best = s.Status;
        return best;
    }

    private static void ApplyDisplayFields(FileNavItem item, string rel, Dictionary<string, TitleInfo> titles)
    {
        var relKey = NormalizePathKey(rel);
        var fileName = Path.GetFileName(rel);
        titles.TryGetValue(relKey, out var ti);

        item.FileName = fileName;
        item.DisplayShort = !string.IsNullOrWhiteSpace(ti?.EnShort) ? ti!.EnShort! : fileName;

        var tooltipParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ti?.En)) tooltipParts.Add(ti!.En!);
        if (!string.IsNullOrWhiteSpace(ti?.Zh)) tooltipParts.Add(ti!.Zh!);
        if (tooltipParts.Count == 0) tooltipParts.Add(rel);
        item.Tooltip = string.Join("\n", tooltipParts);
    }

    /// <summary>
    /// Builds (or recomputes) a nav entry from the source manifest (NAV_CACHE_REDESIGN
    /// §2.2, §3.2-3.3). Per candidate: reuse the stored per-candidate verdict when its
    /// (Token, ContentSig) matches <paramref name="storedEntry"/> (no evaluator call),
    /// else evaluate. Entry status = max over candidates; Red when there are none. The
    /// legacy numeric hint fields are populated from the chosen (canonical-first)
    /// candidate for the still-running sweep; the absolute TranResolvedPath is left null
    /// to keep the cache machine-independent.
    /// </summary>
    private FileNavItem BuildEntry(
        string origAbs,
        string originalDir,
        Dictionary<string, TitleInfo> titles,
        SourceManifest manifest,
        FileNavItem? storedEntry,
        long origSize,
        long origTicks)
    {
        var rel = Path.GetRelativePath(originalDir, origAbs);
        var relKey = NormalizePathKey(rel);

        var item = new FileNavItem
        {
            RelPath = rel,
            OrigSizeBytes = origSize,
            OrigMtimeTicks = origTicks,
        };
        ApplyDisplayFields(item, rel, titles);

        var sources = new List<NavSourceRecord>();
        var hints = new List<NavSourceHint>();

        if (manifest.ByRel.TryGetValue(relKey, out var candidates))
        {
            foreach (var c in candidates)
            {
                TranslationStatus status;
                var reuse = storedEntry?.Sources?.FirstOrDefault(s =>
                    string.Equals(s.Token, c.Token, StringComparison.Ordinal)
                    && string.Equals(s.ContentSig, c.ContentSig, StringComparison.Ordinal));

                if (reuse != null)
                    status = reuse.Status; // persisted verdict — no evaluator call
                else
                    status = _navEvaluator.ComputeCandidateStatus(origAbs, c.AbsPath);

                sources.Add(new NavSourceRecord { Token = c.Token, ContentSig = c.ContentSig, Status = status });
                hints.Add(new NavSourceHint { Token = c.Token, SizeBytes = c.SizeBytes, MtimeTicks = c.MtimeTicks });
            }
        }

        item.Sources = sources;
        item.TranLocalHints = hints;
        item.Status = MaxStatus(sources);

        // Legacy numeric fields (sweep continuity) — chosen candidate is canonical-first.
        var chosen = ChooseLegacyCandidate(candidates);
        item.TranSizeBytes = chosen?.SizeBytes ?? 0;
        item.TranslatedMtimeTicks = chosen?.MtimeTicks ?? 0;
        // TranResolvedPath intentionally left null (machine-independence, §2.2).

        return item;
    }

    private static SourceCandidate? ChooseLegacyCandidate(List<SourceCandidate>? candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;
        foreach (var c in candidates)
            if (string.Equals(c.Token, CanonicalToken, StringComparison.Ordinal))
                return c;
        return candidates[0];
    }

    /// <summary>The live candidate {(Token, ContentSig)} set for a rel, for the entry gate.</summary>
    private static HashSet<string> CandidateSetKey(List<SourceCandidate>? candidates)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (candidates != null)
            foreach (var c in candidates)
                set.Add(ComposeKey(c.Token, c.ContentSig));
        return set;
    }

    private static HashSet<string> StoredSourceSetKey(FileNavItem entry)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (entry.Sources != null)
            foreach (var s in entry.Sources)
                set.Add(ComposeKey(s.Token, s.ContentSig));
        return set;
    }

    // ---------------------------------------------------------------- build

    public Task<IndexCache> BuildAsync(
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var titles = LoadTitlesMap(root);
            var manifest = ScanTranslationSources(
                translatedDir, root, new Dictionary<string, (long, long, string)>(StringComparer.Ordinal));

            var files = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();
            int total = files.Count;

            var entries = new List<FileNavItem>(capacity: total);
            var originals = new List<(string relKey, long size)>(capacity: total);

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();

                var origAbs = files[i];
                long origSize = 0, origTicks = 0;
                try { var fi = new FileInfo(origAbs); origSize = fi.Length; origTicks = fi.LastWriteTimeUtc.Ticks; }
                catch { }

                var rel = Path.GetRelativePath(originalDir, origAbs);
                originals.Add((NormalizePathKey(rel), origSize));

                entries.Add(BuildEntry(origAbs, originalDir, titles, manifest, storedEntry: null, origSize, origTicks));

                if (progress != null && (i % 50 == 0 || i == total - 1))
                    progress.Report((i + 1, total));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            return new IndexCache
            {
                Version = CacheVersion,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                TitlesHash = ComputeTitlesHash(root),
                CorpusKind = CorpusKindOf(translatedDir),
                OriginalsSig = ComputeOriginalsSig(originals),
                SourceSig = manifest.SourceSig,
                Entries = entries
            };
        }, ct);
    }

    // ---------------------------------------------------------------- refresh (§3.4)

    public Task<IndexCache> RefreshAsync(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            // 1. Structural gate — Version/BuildGuid drift or an empty cache is not
            //    stat-comparable ⇒ full build. RootPath is NOT compared (§2.1); a v4 cache
            //    (Version < 5) falls here and rebuilds in PR-NV2 (migration is PR-NV3).
            bool structurallyReusable =
                oldCache is { Entries.Count: > 0 }
                && oldCache.Version >= CacheVersion
                && string.Equals(oldCache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal);

            if (!structurallyReusable)
            {
                var full = await BuildAsync(originalDir, translatedDir, root, progress, ct);
                await SaveAsync(root, full);
                return full;
            }

            // 2. Titles gate — a titles.jsonl change re-derives DISPLAY fields only, keeping
            //    statuses (§3.4). This is no longer a full-rebuild trigger.
            var titlesHash = ComputeTitlesHash(root);
            bool titlesChanged = !string.Equals(oldCache.TitlesHash, titlesHash, StringComparison.Ordinal);
            Dictionary<string, TitleInfo>? titles = null;
            if (titlesChanged)
            {
                titles = LoadTitlesMap(root);
                foreach (var e in oldCache.Entries)
                    ApplyDisplayFields(e, e.RelPath, titles);
            }

            // 3. Live originals (stat-only) + source manifest (hint-accelerated).
            var files = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();
            var liveOriginals = new List<(string origAbs, string relKey, long size, long ticks)>(files.Count);
            var originalsForSig = new List<(string relKey, long size)>(files.Count);
            foreach (var origAbs in files)
            {
                ct.ThrowIfCancellationRequested();
                long size = 0, ticks = 0;
                try { var fi = new FileInfo(origAbs); size = fi.Length; ticks = fi.LastWriteTimeUtc.Ticks; }
                catch { }
                var relKey = NormalizePathKey(Path.GetRelativePath(originalDir, origAbs));
                liveOriginals.Add((origAbs, relKey, size, ticks));
                originalsForSig.Add((relKey, size));
            }

            var liveOriginalsSig = ComputeOriginalsSig(originalsForSig);
            var hintLookup = BuildHintLookup(oldCache);
            var manifest = ScanTranslationSources(translatedDir, root, hintLookup);
            var liveSourceSig = manifest.SourceSig;

            // 4. FAST PATH — both sigs equal and titles unchanged ⇒ nothing changed status.
            //    Return the cache verbatim with ZERO recomputes. Post-clone, the mtime
            //    hints may have drifted (content unchanged ⇒ sigs still equal); heal them
            //    and save ONCE so the next launch is a pure stat hit.
            if (!titlesChanged
                && string.Equals(liveOriginalsSig, oldCache.OriginalsSig, StringComparison.Ordinal)
                && string.Equals(liveSourceSig, oldCache.SourceSig, StringComparison.Ordinal))
            {
                bool hintsHealed = HealHints(oldCache.Entries, manifest);
                if (hintsHealed)
                {
                    oldCache.CorpusKind ??= CorpusKindOf(translatedDir);
                    oldCache.OriginalsSig = liveOriginalsSig;
                    oldCache.SourceSig = liveSourceSig;
                    await SaveAsync(root, oldCache);
                }
                return oldCache;
            }

            // 5. Per-entry gate. Reuse an entry untouched iff OrigSizeBytes is unchanged AND
            //    the live candidate {(Token, ContentSig)} set equals the stored Sources set;
            //    otherwise recompute only the changed candidates. Report progress over the
            //    recompute set only.
            titles ??= LoadTitlesMap(root);
            var oldByRel = new Dictionary<string, FileNavItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in oldCache.Entries)
                oldByRel[NormalizePathKey(e.RelPath)] = e;

            var entries = new List<FileNavItem>(capacity: liveOriginals.Count);
            var recompute = new List<(string origAbs, string relKey, long size, long ticks, FileNavItem? stored)>();
            bool changed = false;

            foreach (var (origAbs, relKey, size, ticks) in liveOriginals)
            {
                ct.ThrowIfCancellationRequested();

                manifest.ByRel.TryGetValue(relKey, out var candidates);
                oldByRel.TryGetValue(relKey, out var stored);

                if (stored != null
                    && stored.OrigSizeBytes == size
                    && CandidateSetKey(candidates).SetEquals(StoredSourceSetKey(stored)))
                {
                    // Reuse — but heal this entry's hints from the fresh scan, and re-home
                    // its display fields if titles changed.
                    if (HealEntryHints(stored, candidates)) changed = true;
                    entries.Add(stored);
                }
                else
                {
                    recompute.Add((origAbs, relKey, size, ticks, stored));
                    changed = true;
                }
            }

            // Removed originals: any old entry not seen this pass is dropped.
            if (entries.Count + recompute.Count != oldCache.Entries.Count)
                changed = true;

            for (int i = 0; i < recompute.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var r = recompute[i];
                // Per-candidate verdicts may only be reused when the ORIGINAL is unchanged:
                // status is a function of the (orig, tran) pair, so a changed original must
                // re-evaluate every candidate even if the translation bytes are identical.
                var origUnchanged = r.stored != null && r.stored.OrigSizeBytes == r.size;
                var reuseFrom = origUnchanged ? r.stored : null;
                entries.Add(BuildEntry(r.origAbs, originalDir, titles, manifest, reuseFrom, r.size, r.ticks));
                if (progress != null)
                    progress.Report((i + 1, recompute.Count));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            var refreshed = new IndexCache
            {
                Version = CacheVersion,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                TitlesHash = titlesHash,
                CorpusKind = oldCache.CorpusKind ?? CorpusKindOf(translatedDir),
                OriginalsSig = liveOriginalsSig,
                SourceSig = liveSourceSig,
                Entries = entries
            };

            // Cache-level sig drift (e.g. bare sweep-save left them null) also warrants a save.
            if (!string.Equals(oldCache.OriginalsSig, liveOriginalsSig, StringComparison.Ordinal)
                || !string.Equals(oldCache.SourceSig, liveSourceSig, StringComparison.Ordinal)
                || titlesChanged)
                changed = true;

            if (changed)
                await SaveAsync(root, refreshed);

            return refreshed;
        }, ct);
    }

    /// <summary>
    /// Heals every entry's local (size, mtime) hints from the fresh manifest scan (the
    /// post-clone write-back, NAV_CACHE_REDESIGN §2.2). Returns true if any hint drifted.
    /// </summary>
    private static bool HealHints(List<FileNavItem> entries, SourceManifest manifest)
    {
        bool any = false;
        foreach (var e in entries)
        {
            manifest.ByRel.TryGetValue(NormalizePathKey(e.RelPath), out var candidates);
            if (HealEntryHints(e, candidates)) any = true;
        }
        return any;
    }

    private static bool HealEntryHints(FileNavItem entry, List<SourceCandidate>? candidates)
    {
        var fresh = new List<NavSourceHint>();
        if (candidates != null)
            foreach (var c in candidates)
                fresh.Add(new NavSourceHint { Token = c.Token, SizeBytes = c.SizeBytes, MtimeTicks = c.MtimeTicks });

        if (HintsEqual(entry.TranLocalHints, fresh))
            return false;

        entry.TranLocalHints = fresh;
        return true;
    }

    private static bool HintsEqual(List<NavSourceHint>? a, List<NavSourceHint> b)
    {
        int ac = a?.Count ?? 0;
        if (ac != b.Count) return false;
        for (int i = 0; i < b.Count; i++)
        {
            var x = a![i];
            var y = b[i];
            if (!string.Equals(x.Token, y.Token, StringComparison.Ordinal)
                || x.SizeBytes != y.SizeBytes || x.MtimeTicks != y.MtimeTicks)
                return false;
        }
        return true;
    }

    // ---------------------------------------------------------------- migration (§4.4)

    /// <summary>
    /// One-time v4 -&gt; v5 migration (NAV_CACHE_REDESIGN §4.4). Transforms each v4 entry in
    /// place: keep <c>RelPath</c>, display fields, <c>Status</c>, <c>OrigSizeBytes</c>;
    /// convert the absolute <c>TranResolvedPath</c> into a relative <c>Token</c> by
    /// prefix-matching the canonical dir (xml-p5t / xml-open-t -&gt; "canonical") and each
    /// <c>community/translations/{user}</c> (-&gt; "user:{name}"); an unparseable/foreign
    /// prefix drops that source record. The referenced translated file (the ~21 that exist)
    /// is hashed for its <c>ContentSig</c> -- the migration's only I/O -- and v4's
    /// single-source <c>Status</c> carries over as the stored per-candidate verdict.
    ///
    /// The result is a v5-shaped cache with each entry holding AT MOST its one v4-resolved
    /// source. It is then handed to the normal gated <see cref="RefreshAsync"/>, whose
    /// per-entry gate does the rest for free: untranslated and unchanged single-source
    /// entries are reused (no evaluator call), while any rel the live source manifest shows
    /// in more than one source dir -- where v4's single value could understate the
    /// multi-source max -- fails the candidate-set gate and is recomputed (bounded by the
    /// manifest overlap set, a handful), as is any entry whose source record was dropped.
    /// RefreshAsync saves the upgraded cache as v5.
    /// </summary>
    public Task<IndexCache> MigrateV4(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            var migrated = new IndexCache
            {
                Version = CacheVersion,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                // Carried as-is; the gated refresh re-derives display fields if titles moved.
                TitlesHash = oldCache.TitlesHash,
                CorpusKind = CorpusKindOf(translatedDir),
                // OriginalsSig/SourceSig deliberately left null so RefreshAsync SKIPS the fast
                // path and runs the per-entry gate (recomputing exactly the overlap + dropped
                // set; untranslated / unchanged-single-source entries reuse with no evaluator
                // call).
                Entries = new List<FileNavItem>(oldCache.Entries?.Count ?? 0),
            };

            foreach (var e in oldCache.Entries ?? new List<FileNavItem>())
            {
                ct.ThrowIfCancellationRequested();

                var item = new FileNavItem
                {
                    RelPath = e.RelPath,
                    FileName = e.FileName,
                    DisplayShort = e.DisplayShort,
                    Tooltip = e.Tooltip,
                    Status = e.Status,
                    OrigSizeBytes = e.OrigSizeBytes,
                    OrigMtimeTicks = e.OrigMtimeTicks,
                    Sources = new List<NavSourceRecord>(),
                    TranLocalHints = new List<NavSourceHint>(),
                };

                // v4 stored a resolved path on EVERY entry — including the ~4,970 untranslated
                // texts, whose path is the (never-created) canonical xml-p5t path. Only create a
                // source record when the file ACTUALLY EXISTS: a non-existent path means no
                // translation, which is a Red entry with an empty candidate set. Emitting a bogus
                // source for the missing path would (a) hash ~5,000 phantom files here and (b) make
                // every one of them mismatch the fresh manifest's empty set in RefreshAsync ⇒ a
                // full-corpus recompute — the multi-minute migration hang this guard prevents.
                if (!string.IsNullOrWhiteSpace(e.TranResolvedPath)
                    && File.Exists(e.TranResolvedPath!)
                    && TryResolveV4Token(e.TranResolvedPath!, translatedDir, root, out var token))
                {
                    long size = 0, mtime = 0;
                    try { var fi = new FileInfo(e.TranResolvedPath!); size = fi.Length; mtime = fi.LastWriteTimeUtc.Ticks; }
                    catch { }

                    // v4's single-source Status IS this candidate's verdict.
                    var sig = Sha256Hex16OfFile(e.TranResolvedPath!);
                    item.Sources.Add(new NavSourceRecord { Token = token, ContentSig = sig, Status = e.Status });
                    item.TranLocalHints.Add(new NavSourceHint { Token = token, SizeBytes = size, MtimeTicks = mtime });

                    // Legacy numeric hints (sweep continuity, as BuildEntry does).
                    item.TranSizeBytes = size;
                    item.TranslatedMtimeTicks = mtime;
                }
                else
                {
                    // No existing translation file ⇒ no candidate ⇒ Red, empty Sources. This
                    // matches what the fresh manifest scan finds, so the per-entry gate REUSES
                    // the entry with zero evaluator calls instead of recomputing it.
                    item.Status = TranslationStatus.Red;
                }

                migrated.Entries.Add(item);
            }

            // Normal gated refresh: recompute only the changed candidates, heal hints/sigs,
            // save as v5.
            return await RefreshAsync(migrated, originalDir, translatedDir, root, progress, ct);
        }, ct);
    }

    // ---------------------------------------------------------------- single-entry refresh (§3.5.1)

    public Task<FileNavItem> RefreshEntryAsync(
        FileNavItem? storedEntry,
        string relPath,
        string originalDir,
        string translatedDir,
        string root,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var origAbs = Path.GetFullPath(Path.Combine(originalDir, relPath));
            var rel = Path.GetRelativePath(originalDir, origAbs);
            var relKey = NormalizePathKey(rel);

            long origSize = 0, origTicks = 0;
            try { var fi = new FileInfo(origAbs); origSize = fi.Length; origTicks = fi.LastWriteTimeUtc.Ticks; }
            catch { }

            // Reuse this rel's own hints so an unchanged candidate is not re-hashed.
            var hintLookup = BuildHintLookup(
                storedEntry == null ? null : new IndexCache { Entries = new List<FileNavItem> { storedEntry } });
            var manifest = ScanSourcesForRel(relKey, rel, translatedDir, root, hintLookup);

            // Display fields never change on a translation save: carry them from the stored
            // entry (or read titles.jsonl only for the brand-new-entry fallback).
            var titles = storedEntry != null
                ? new Dictionary<string, TitleInfo>()
                : LoadTitlesMap(root);

            var item = BuildEntry(origAbs, originalDir, titles, manifest, storedEntry, origSize, origTicks);
            if (storedEntry != null)
            {
                item.FileName = storedEntry.FileName;
                item.DisplayShort = storedEntry.DisplayShort;
                item.Tooltip = storedEntry.Tooltip;
            }
            return item;
        }, ct);
    }

    /// <summary>
    /// Single-rel analogue of <see cref="ScanTranslationSources"/> (NAV_CACHE_REDESIGN
    /// §3.5.1): probes the canonical dir plus each <c>community/translations/{user}</c> dir
    /// for exactly ONE rel — a canonical stat + one community loop — never the corpus sweep.
    /// Reuses the (size, mtime)-matched hint's sig, else hashes the single candidate.
    /// </summary>
    private static SourceManifest ScanSourcesForRel(
        string relKey,
        string rel,
        string translatedDir,
        string root,
        Dictionary<string, (long size, long mtime, string sig)> hintLookup)
    {
        var manifest = new SourceManifest();
        // Normalize to OS-native separators so Path.Combine locates the candidate on any host.
        var relOs = rel.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

        void TryAdd(string dir, string token)
        {
            if (string.IsNullOrEmpty(dir)) return;
            var f = Path.Combine(dir, relOs);
            long size, mtime;
            try
            {
                if (!File.Exists(f)) return;
                var fi = new FileInfo(f);
                size = fi.Length;
                mtime = fi.LastWriteTimeUtc.Ticks;
            }
            catch { return; }

            string sig;
            if (hintLookup.TryGetValue(HintKey(relKey, token), out var h) && h.size == size && h.mtime == mtime)
                sig = h.sig;             // hint hit — no read
            else
                sig = Sha256Hex16OfFile(f); // hint miss — hash

            if (!manifest.ByRel.TryGetValue(relKey, out var list))
            {
                list = new List<SourceCandidate>(1);
                manifest.ByRel[relKey] = list;
            }
            list.Add(new SourceCandidate(token, f, sig, size, mtime));
        }

        // Canonical first (ordering parity with the full scan).
        TryAdd(translatedDir, CanonicalToken);

        var communityRoot = Path.Combine(root, "community", "translations");
        if (Directory.Exists(communityRoot))
        {
            IEnumerable<string> userDirs;
            try { userDirs = Directory.EnumerateDirectories(communityRoot); }
            catch { userDirs = Array.Empty<string>(); }
            foreach (var userDir in userDirs)
                TryAdd(userDir, "user:" + Path.GetFileName(userDir));
        }

        manifest.SourceSig = ComputeSourceSig(manifest);
        return manifest;
    }

    /// <summary>
    /// Maps a v4 absolute <c>TranResolvedPath</c> to a relative v5 source <c>Token</c>:
    /// "canonical" when the path is under the canonical translated dir, "user:{name}" when
    /// under <c>community/translations/{name}</c>. Returns false for an unparseable/foreign
    /// prefix (a stale machine path, a retired dir) so the migration drops that source record
    /// and the gate recomputes the entry.
    /// </summary>
    private static bool TryResolveV4Token(string tranResolvedPath, string translatedDir, string root, out string token)
    {
        token = "";
        try
        {
            // Canonical (xml-p5t / xml-open-t) — checked first so it wins.
            if (IsUnderDir(tranResolvedPath, translatedDir, out _))
            {
                token = CanonicalToken;
                return true;
            }

            // community/translations/{user}/… — first segment is the username.
            var communityRoot = Path.Combine(root, "community", "translations");
            if (IsUnderDir(tranResolvedPath, communityRoot, out var relFromCommunity))
            {
                var norm = relFromCommunity.Replace('\\', '/').TrimStart('/');
                var slash = norm.IndexOf('/');
                var user = slash > 0 ? norm.Substring(0, slash) : norm;
                if (!string.IsNullOrEmpty(user))
                {
                    token = "user:" + user;
                    return true;
                }
            }
        }
        catch { /* any path parse failure ⇒ drop the record (recompute) */ }
        return false;
    }

    /// <summary>True iff <paramref name="path"/> lies inside <paramref name="dir"/>; outputs
    /// the relative sub-path when so. A "climb-out" ("..") or a rooted result (a different
    /// Windows drive) means the path is elsewhere.</summary>
    private static bool IsUnderDir(string path, string dir, out string relative)
    {
        relative = "";
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(path))
            return false;
        var rel = Path.GetRelativePath(dir, path);
        if (string.Equals(rel, ".", StringComparison.Ordinal)) return false;   // path == dir
        if (rel.StartsWith("..", StringComparison.Ordinal)) return false;       // outside dir
        if (Path.IsPathRooted(rel)) return false;                               // different root/drive
        relative = rel;
        return true;
    }

    // ---------------------------------------------------------------- hashing

    private static string Sha256Hex16(string s)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static string Sha256Hex16OfFile(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash).ToLowerInvariant()[..16];
        }
        catch
        {
            // Unreadable ⇒ a stable sentinel so its state still participates in the sig.
            return "unreadable000000";
        }
    }

    // ---------------------------------------------------------------- headless bake (§4.1)

    /// <summary>
    /// Headless <c>--build-nav-cache &lt;parentRoot&gt; &lt;outFile&gt;</c> bake mode
    /// (NAV_CACHE_REDESIGN §4.1, PR-NV5), the nav analogue of the
    /// <c>--build-search-index</c> path. Resolves the CBETA corpus under
    /// <paramref name="args"/>[1] via <see cref="Infrastructure.AppPaths.DiscoverAllCorpora"/>,
    /// runs the same v5 <see cref="BuildAsync"/> pipeline (originals scan + translation-source
    /// manifest) over it, and writes the machine-independent v5 <c>nav-cache.cbeta.json</c> to
    /// <paramref name="args"/>[2] (atomic tmp+move). Machine-independent by construction:
    /// relative source tokens, content sigs, an informational RootPath, and CI-machine
    /// TranLocalHints that self-heal on the first local launch. No GUI, no side effects beyond
    /// the output file. Exit codes: 0 success, 1 bad args / missing dirs / no CBETA corpus,
    /// 2 the build threw, 3 an empty cache was produced. CI stages this in prebuild-index;
    /// the committed asset + CI guard are PR-NV6.
    /// </summary>
    public static int RunHeadlessBuild(string[] args, TextWriter log)
    {
        if (args.Length < 3)
        {
            log.WriteLine("usage: --build-nav-cache <parentRoot> <outFile>");
            return 1;
        }

        var parentRoot = args[1];
        var outFile = args[2];

        if (!Directory.Exists(parentRoot))
        {
            log.WriteLine($"error: parentRoot does not exist: {parentRoot}");
            return 1;
        }

        try
        {
            var corpora = Infrastructure.AppPaths.DiscoverAllCorpora(parentRoot);
            var cbeta = corpora.FirstOrDefault(c => c.Kind == Models.CorpusKind.Cbeta);
            if (cbeta == null)
            {
                log.WriteLine($"error: no CBETA corpus (xml-p5 + xml-p5t) discovered under {parentRoot}");
                return 1;
            }

            var originalDir = cbeta.OriginalDir;
            var translatedDir = cbeta.TranslatedDir;
            var root = cbeta.TranslationsRepoRoot;

            log.WriteLine("Baking bundled nav cache (v5) ...");
            log.WriteLine($"  Originals:  {originalDir}");
            log.WriteLine($"  Translated: {translatedDir}");
            log.WriteLine($"  Root:       {root}");
            log.WriteLine($"  Out:        {outFile}");

            // Convenience ctor wires a real NavStatusEvaluator (no DI needed headlessly).
            var svc = new IndexCacheService(new TranslationStatusService());
            var progress = new Progress<(int done, int total)>(t =>
            {
                if (t.done == t.total || t.done % 500 == 0)
                    log.WriteLine($"  indexed {t.done}/{t.total}");
            });

            var cache = svc.BuildAsync(originalDir, translatedDir, root, progress)
                           .GetAwaiter().GetResult();

            if (cache.Entries == null || cache.Entries.Count == 0)
            {
                log.WriteLine("error: build produced an empty cache");
                return 3;
            }

            var outDir = Path.GetDirectoryName(Path.GetFullPath(outFile));
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);

            var json = JsonSerializer.Serialize(cache, JsonOpts);
            var tmp = outFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, json, Utf8NoBom);
            File.Move(tmp, outFile, overwrite: true);

            log.WriteLine($"OK: bundled nav cache written to {outFile} " +
                          $"({cache.Entries.Count} entries, kind={cache.CorpusKind})");
            return 0;
        }
        catch (Exception ex)
        {
            log.WriteLine($"error: nav cache bake failed: {ex}");
            return 2;
        }
    }
}
