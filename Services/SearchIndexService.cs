// Services/SearchIndexService.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class SearchIndexService : IDisposable
{
    public sealed class SearchIndexServiceOptions
    {
        public long MaxBloomCacheBytes { get; set; } = 128L * 1024 * 1024;

        // HDD + 2MB files: IO-bound verification is common.
        // Too much parallelism can *thrash* an HDD (seeks).
        // Start conservative; bump if you move corpus to SSD.
        public int MaxVerifyDegreeOfParallelism { get; set; } = Math.Min(2, Environment.ProcessorCount);

        // Bloom scan is sequential-ish over the bin file; CPU-bound-ish.
        public int MaxBloomDegreeOfParallelism { get; set; } = Math.Min(Environment.ProcessorCount, 8);

        public int ReplaceTries { get; set; } = 18;
        public int ReplaceDelayMs { get; set; } = 80;

        // If you truly need entity-decoding for search, keep this true.
        // For CBETA bodies it’s often unnecessary; turning it off is faster.
        public bool HtmlDecodeIfAmpersandPresent { get; set; } = true;
    }

    public SearchIndexServiceOptions Options { get; } = new();

    // Gate only for index file I/O (manifest/bin) so we can release before expensive verification.
    private static readonly SemaphoreSlim _indexIoGate = new(1, 1);

    private readonly Dictionary<long, LinkedListNode<(long key, ulong[] bits)>> _bloomCache = new();
    private readonly LinkedList<(long key, ulong[] bits)> _bloomLru = new();
    private long _bloomCacheBytes = 0;
    private readonly object _bloomLock = new();

    // Cached manifest + mmap (big real-world speed win for repeated searches)
    private readonly object _indexCacheLock = new();
    private SearchIndexManifest? _cachedManifest;
    private string? _cachedManifestPath;
    private DateTime _cachedManifestWriteUtc;

    private MemoryMappedFile? _cachedMmf;
    private MemoryMappedViewAccessor? _cachedAccessor;
    private string? _cachedBinPath;
    private DateTime _cachedBinWriteUtc;

    private const string ManifestFileName = "search.index.manifest.json";
    private const string BinFileName = "search.index.bin";

    private const int BloomBits = 16384; // was 4096
    private const int BloomBytes = BloomBits / 8;
    private const int BloomUlongs = BloomBits / 64;
    private const int BloomHashCount = 5; // optional: 4 is okay too
    private const string BuildGuid = "search-v2-bloom-16384";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    // ==========================================================
    // CO-OCCURRENCE METRICS (dropdown controls what panel shows)
    // ==========================================================

    public sealed class CooccurrencePanelResult
    {
        public string Summary { get; set; } = "";
        public string LeftTitle { get; set; } = "Top characters";
        public string RightTitle { get; set; } = "Top bigrams / trigrams";
        public List<CoocRow> Left { get; set; } = new();
        public List<CoocRow> Right { get; set; } = new();
        public string ExtraLine { get; set; } = "";
    }

    public static CooccurrencePanelResult ComputeCooccurrences(
        IEnumerable<SearchResultGroup> groups,
        string query,
        int contextWidth,
        CoocMetric metric,
        int topK = 30)
    {
        query ??= "";

        int totalHits = 0;
        int totalWindows = 0;

        // Characters
        var chFreq = new Dictionary<string, int>(StringComparer.Ordinal);
        var chRange = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var chByFile = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

        // Ngrams (bigrams+trigrams together)
        var ngFreq = new Dictionary<string, int>(StringComparer.Ordinal);
        var ngRange = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var ngByFile = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

        var groupList = groups?.ToList() ?? new List<SearchResultGroup>();
        int Nfiles = groupList.Count;

        foreach (var g in groupList)
        {
            string rel = g.RelPath ?? "";
            if (string.IsNullOrWhiteSpace(rel)) rel = "(unknown)";

            foreach (var c in g.Children)
            {
                totalHits++;
                totalWindows++;

                string window = (c.Hit.Left ?? "") + (c.Hit.Match ?? "") + (c.Hit.Right ?? "");
                window = window.Replace("\r", "").Replace("\n", " ").Trim();
                if (window.Length == 0) continue;

                // Characters (skip whitespace)
                for (int i = 0; i < window.Length; i++)
                {
                    char ch = window[i];
                    if (char.IsWhiteSpace(ch)) continue;

                    string key = ch.ToString();
                    chFreq[key] = chFreq.TryGetValue(key, out var f) ? f + 1 : 1;

                    if (!chRange.TryGetValue(key, out var set))
                        chRange[key] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    set.Add(rel);

                    if (!chByFile.TryGetValue(key, out var map))
                        chByFile[key] = map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    map[rel] = map.TryGetValue(rel, out var v) ? v + 1 : 1;
                }

                // Ngrams: avoid LINQ allocations by doing a compact rolling window over non-whitespace
                char a = '\0', b = '\0';
                bool hasA = false, hasB = false;

                for (int i = 0; i < window.Length; i++)
                {
                    char ch = window[i];
                    if (char.IsWhiteSpace(ch)) continue;

                    // shift rolling buffer: (a,b,ch)
                    if (!hasA)
                    {
                        a = ch; hasA = true;
                        continue;
                    }
                    if (!hasB)
                    {
                        b = ch; hasB = true;

                        // bigram (a,b)
                        string bg0 = string.Concat(a, b);
                        ngFreq[bg0] = ngFreq.TryGetValue(bg0, out var f2) ? f2 + 1 : 1;

                        if (!ngRange.TryGetValue(bg0, out var set))
                            ngRange[bg0] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        set.Add(rel);

                        if (!ngByFile.TryGetValue(bg0, out var map))
                            ngByFile[bg0] = map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        map[rel] = map.TryGetValue(rel, out var v) ? v + 1 : 1;

                        continue;
                    }

                    // Now we have a,b and new ch => bigram (b,ch) and trigram (a,b,ch)
                    string bg = string.Concat(b, ch);
                    ngFreq[bg] = ngFreq.TryGetValue(bg, out var fbg) ? fbg + 1 : 1;

                    if (!ngRange.TryGetValue(bg, out var setBg))
                        ngRange[bg] = setBg = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    setBg.Add(rel);

                    if (!ngByFile.TryGetValue(bg, out var mapBg))
                        ngByFile[bg] = mapBg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    mapBg[rel] = mapBg.TryGetValue(rel, out var vbg) ? vbg + 1 : 1;

                    string tg = string.Concat(a, b, ch);
                    ngFreq[tg] = ngFreq.TryGetValue(tg, out var ftg) ? ftg + 1 : 1;

                    if (!ngRange.TryGetValue(tg, out var setTg))
                        ngRange[tg] = setTg = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    setTg.Add(rel);

                    if (!ngByFile.TryGetValue(tg, out var mapTg))
                        ngByFile[tg] = mapTg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    mapTg[rel] = mapTg.TryGetValue(rel, out var vtg) ? vtg + 1 : 1;

                    // shift
                    a = b;
                    b = ch;
                }
            }
        }

        double DispersionScore(int freq, int range)
            => (freq / Math.Sqrt(1.0 + totalWindows)) * Math.Log(1.0 + range);

        double DominanceShare(Dictionary<string, int>? perFile, int freq)
        {
            if (freq <= 0 || perFile == null || perFile.Count == 0) return 0;
            int max = 0;
            foreach (var kv in perFile)
                if (kv.Value > max) max = kv.Value;
            return (double)max / freq; // 0..1
        }

        double LogDiceApprox(int f_xq, int f_x, int f_q)
            => 14.0 + Math.Log((2.0 * f_xq) / (f_x + (double)f_q + 1e-9), 2.0);

        double PmiSurrogate(int freq, int range)
            => (Math.Log(1.0 + freq, 2.0) - Math.Log(1.0 + range, 2.0));

        double TScoreSurrogate(int freq, int range)
            => Math.Sqrt(Math.Max(0, freq)) * Math.Log(1.0 + Math.Max(0, range));

        double MetricValueFor(int freq, int range, Dictionary<string, int>? perFile)
        {
            return metric switch
            {
                CoocMetric.TopCooccurrences => DispersionScore(freq, range),
                CoocMetric.DispersionScore => DispersionScore(freq, range),
                CoocMetric.Frequency => freq,
                CoocMetric.Range => range,
                CoocMetric.Dominance => DominanceShare(perFile, freq),
                CoocMetric.PMI => PmiSurrogate(freq, range),
                CoocMetric.LogDice => LogDiceApprox(freq, freq, Math.Max(1, totalWindows)),
                CoocMetric.TScore => TScoreSurrogate(freq, range),
                _ => DispersionScore(freq, range)
            };
        }

        string metricName = metric switch
        {
            CoocMetric.TopCooccurrences => "Top co-occurrences",
            CoocMetric.DispersionScore => "Dispersion score",
            CoocMetric.Frequency => "Frequency",
            CoocMetric.Range => "Range",
            CoocMetric.Dominance => "Dominance (top-file share)",
            CoocMetric.PMI => "PMI (window-based)",
            CoocMetric.LogDice => "logDice",
            CoocMetric.TScore => "t-score",
            _ => "Dispersion score"
        };

        var left = chFreq.Select(kv =>
        {
            var key = kv.Key;
            int freq = kv.Value;
            int range = chRange.TryGetValue(key, out var s) ? s.Count : 0;
            chByFile.TryGetValue(key, out var byFile);
            double val = MetricValueFor(freq, range, byFile);

            return new CoocRow { Key = key, Freq = freq, Range = range, Assoc = val, Bar = "" };
        }).ToList();

        var right = ngFreq.Select(kv =>
        {
            var key = kv.Key;
            int freq = kv.Value;
            int range = ngRange.TryGetValue(key, out var s) ? s.Count : 0;
            ngByFile.TryGetValue(key, out var byFile);
            double val = MetricValueFor(freq, range, byFile);

            return new CoocRow { Key = key, Freq = freq, Range = range, Assoc = val, Bar = "" };
        }).ToList();

        left = left.OrderByDescending(r => r.Assoc).ThenByDescending(r => r.Freq).Take(topK).ToList();
        right = right.OrderByDescending(r => r.Assoc).ThenByDescending(r => r.Freq).Take(topK).ToList();

        static string MakeBar(int v, int max)
        {
            if (max <= 0) return "";
            int n = (int)Math.Round(12.0 * v / max);
            n = Math.Clamp(n, 0, 12);
            return new string('█', n);
        }

        int maxC = left.Count > 0 ? left.Max(r => r.Freq) : 0;
        int maxN = right.Count > 0 ? right.Max(r => r.Freq) : 0;
        foreach (var r in left) r.Bar = MakeBar(r.Freq, maxC);
        foreach (var r in right) r.Bar = MakeBar(r.Freq, maxN);

        var zip = right.OrderByDescending(r => r.Freq).Take(12).Select((r, i) => $"{i + 1}:{r.Freq}").ToArray();
        string zipLine = zip.Length > 0 ? ("Zipf-ish ranks (top ngrams): " + string.Join("  ", zip)) : "";

        var domTop = right.OrderByDescending(r => r.Freq).Take(10).Select(r =>
        {
            ngByFile.TryGetValue(r.Key, out var byFile);
            double share = DominanceShare(byFile, r.Freq);
            int bars = Math.Clamp((int)Math.Round(12 * share), 0, 12);
            return $"{r.Key}:{share * 100:0.#}% {new string('█', bars)}";
        }).ToArray();

        string domLine = domTop.Length > 0 ? ("Dominance (top-file share): " + string.Join("  ", domTop)) : "";

        var extra = string.Join("\n", new[] { zipLine, domLine }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CooccurrencePanelResult
        {
            Summary = $"metric={metricName}   hits={totalHits:n0}, windows={totalWindows:n0}, files={Nfiles:n0}, context={contextWidth} chars",
            LeftTitle = $"Top characters by {metricName}",
            RightTitle = $"Top bigrams / trigrams by {metricName}",
            Left = left,
            Right = right,
            ExtraLine = extra
        };
    }

    // ---------------------------
    // Helpers (index caches)
    // ---------------------------

    public void Dispose()
    {
        InvalidateIndexCaches();
        GC.SuppressFinalize(this);
    }

    private void InvalidateIndexCaches()
    {
        lock (_indexCacheLock)
        {
            _cachedManifest = null;
            _cachedManifestPath = null;
            _cachedManifestWriteUtc = default;

            try { _cachedAccessor?.Dispose(); } catch { }
            try { _cachedMmf?.Dispose(); } catch { }

            _cachedAccessor = null;
            _cachedMmf = null;
            _cachedBinPath = null;
            _cachedBinWriteUtc = default;
        }
    }

    private MemoryMappedViewAccessor GetOrCreateMappedAccessor(string binPath)
    {
        var full = Path.GetFullPath(binPath);
        var writeUtc = File.GetLastWriteTimeUtc(full);

        lock (_indexCacheLock)
        {
            if (_cachedAccessor != null &&
                _cachedMmf != null &&
                string.Equals(_cachedBinPath, full, StringComparison.OrdinalIgnoreCase) &&
                _cachedBinWriteUtc == writeUtc)
            {
                return _cachedAccessor;
            }

            try { _cachedAccessor?.Dispose(); } catch { }
            try { _cachedMmf?.Dispose(); } catch { }

            _cachedMmf = MemoryMappedFile.CreateFromFile(full, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _cachedAccessor = _cachedMmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            _cachedBinPath = full;
            _cachedBinWriteUtc = writeUtc;

            return _cachedAccessor;
        }
    }

    // ---------------------------
    // Helpers (file replace retry)
    // ---------------------------

    private static FileStream OpenFileWithRetry(
        string path,
        FileMode mode,
        FileAccess access,
        FileShare share,
        int tries = 12,
        int delayMs = 80)
    {
        Exception? last = null;

        for (int i = 0; i < tries; i++)
        {
            try { return new FileStream(path, mode, access, share); }
            catch (IOException ex) { last = ex; Thread.Sleep(delayMs); delayMs = Math.Min(500, (int)(delayMs * 1.4)); }
            catch (UnauthorizedAccessException ex) { last = ex; Thread.Sleep(delayMs); delayMs = Math.Min(500, (int)(delayMs * 1.4)); }
        }

        throw new IOException($"Could not open '{path}' after {tries} attempts. Still locked by another process.", last);
    }

    private void ReplaceFileAtomicWithRetry(string tmp, string final)
    {
        Exception? last = null;

        int tries = Math.Max(1, Options.ReplaceTries);
        int delayMs = Math.Max(10, Options.ReplaceDelayMs);

        for (int i = 0; i < tries; i++)
        {
            try
            {
                if (File.Exists(final))
                {
                    var bak = final + ".bak";
                    try { if (File.Exists(bak)) File.Delete(bak); } catch { }

                    File.Replace(tmp, final, bak, ignoreMetadataErrors: true);

                    try { if (File.Exists(bak)) File.Delete(bak); } catch { }
                }
                else
                {
                    File.Move(tmp, final);
                }

                return;
            }
            catch (IOException ex) { last = ex; }
            catch (UnauthorizedAccessException ex) { last = ex; }

            Thread.Sleep(delayMs);
            delayMs = Math.Min(500, (int)(delayMs * 1.4));
        }

        throw new IOException($"Failed to replace '{final}' after {tries} attempts.", last);
    }

    private static string NormalizeRelKey(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    public string GetManifestPath(string root) => Path.Combine(root, ManifestFileName);
    public string GetBinPath(string root) => Path.Combine(root, BinFileName);

    public void ClearBloomCache()
    {
        lock (_bloomLock)
        {
            _bloomCache.Clear();
            _bloomLru.Clear();
            _bloomCacheBytes = 0;
        }
    }

    private ulong[] GetBloomCached(FileStream fs, long offset)
    {
        lock (_bloomLock)
        {
            if (_bloomCache.TryGetValue(offset, out var node))
            {
                _bloomLru.Remove(node);
                _bloomLru.AddFirst(node);
                return node.Value.bits;
            }
        }

        var bits = ReadBloom(fs, offset);

        lock (_bloomLock)
        {
            if (_bloomCache.TryGetValue(offset, out var existing))
            {
                _bloomLru.Remove(existing);
                _bloomLru.AddFirst(existing);
                return existing.Value.bits;
            }

            var node = new LinkedListNode<(long key, ulong[] bits)>((offset, bits));
            _bloomLru.AddFirst(node);
            _bloomCache[offset] = node;
            _bloomCacheBytes += BloomBytes;

            EvictBloomCacheIfNeeded();
        }

        return bits;
    }

    private void EvictBloomCacheIfNeeded()
    {
        long max = Math.Max(0, Options.MaxBloomCacheBytes);

        if (max == 0)
        {
            _bloomCache.Clear();
            _bloomLru.Clear();
            _bloomCacheBytes = 0;
            return;
        }

        while (_bloomCacheBytes > max && _bloomLru.Last != null)
        {
            var last = _bloomLru.Last!;
            _bloomLru.RemoveLast();
            _bloomCache.Remove(last.Value.key);
            _bloomCacheBytes -= BloomBytes;
        }
    }

    // ---------------------------
    // FAST body extraction / normalization (NO REGEX)
    // ---------------------------

    private static string MakeSearchableTextFromXml_Fast(string xml, bool htmlDecodeIfAmpersandPresent)
    {
        if (string.IsNullOrEmpty(xml)) return "";

        int iBody = xml.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (iBody < 0) return "";

        int iStart = xml.IndexOf('>', iBody);
        if (iStart < 0) return "";

        int iEnd = xml.IndexOf("</body>", iStart + 1, StringComparison.OrdinalIgnoreCase);
        if (iEnd < 0) return "";

        int bodyLen = iEnd - (iStart + 1);
        if (bodyLen <= 0) return "";

        var sb = StringBuilderCache.Acquire(bodyLen);

        bool inTag = false;
        bool prevSpace = true; // trim-leading
        bool sawAmp = false;

        for (int i = iStart + 1; i < iEnd; i++)
        {
            char ch = xml[i];

            if (inTag)
            {
                if (ch == '>') inTag = false;
                continue;
            }

            if (ch == '<')
            {
                inTag = true;
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
                continue;
            }

            if (ch == '\r') continue;

            if (ch == '\n' || ch == '\t' || ch == ' ' || ch == '\f' || ch == '\v')
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
                continue;
            }

            if (ch == '&') sawAmp = true;

            sb.Append(ch);
            prevSpace = false;
        }

        if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
            sb.Length--;

        string text = StringBuilderCache.GetStringAndRelease(sb);

        if (htmlDecodeIfAmpersandPresent && sawAmp)
        {
            try { text = WebUtility.HtmlDecode(text); }
            catch { /* ignore */ }
        }

        return text;
    }

    private static class StringBuilderCache
    {
        [ThreadStatic] private static StringBuilder? _cached;

        public static StringBuilder Acquire(int capacity)
        {
            var sb = _cached;
            if (sb == null)
                return new StringBuilder(capacity);

            _cached = null;
            sb.Clear();
            if (sb.Capacity < capacity) sb.Capacity = capacity;
            return sb;
        }

        public static string GetStringAndRelease(StringBuilder sb)
        {
            string s = sb.ToString();
            if (sb.Capacity <= 256 * 1024) // don’t hold giant buffers
                _cached = sb;
            return s;
        }
    }

    // ---------------------------
    // Manifest I/O
    // ---------------------------

    public async Task<SearchIndexManifest?> TryLoadAsync(string root)
    {
        try
        {
            var mp = GetManifestPath(root);
            var bp = GetBinPath(root);

            if (!File.Exists(mp) || !File.Exists(bp))
                return null;

            var mpFull = Path.GetFullPath(mp);
            var mpWriteUtc = File.GetLastWriteTimeUtc(mpFull);

            // Fast path: cached manifest still matches file timestamp
            lock (_indexCacheLock)
            {
                if (_cachedManifest != null &&
                    string.Equals(_cachedManifestPath, mpFull, StringComparison.OrdinalIgnoreCase) &&
                    _cachedManifestWriteUtc == mpWriteUtc)
                {
                    return _cachedManifest;
                }
            }

            var json = await File.ReadAllTextAsync(mp, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            if (man == null) return null;

            if (!string.Equals(Path.GetFullPath(man.RootPath), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return null;

            if (man.Version != 1) return null;
            if (!string.Equals(man.BuildGuid, BuildGuid, StringComparison.Ordinal)) return null;

            if (man.BloomBits != BloomBits || man.BloomHashCount != BloomHashCount)
                return null;

            if (man.Entries == null || man.Entries.Count == 0)
                return null;

            var binLen = new FileInfo(bp).Length;
            foreach (var e in man.Entries)
            {
                if (e.BloomOffset < 0 || e.BloomOffset + BloomBytes > binLen)
                    return null;
            }

            lock (_indexCacheLock)
            {
                _cachedManifest = man;
                _cachedManifestPath = mpFull;
                _cachedManifestWriteUtc = mpWriteUtc;
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveManifestAtomicAsync(string root, SearchIndexManifest manifest, CancellationToken ct)
    {
        manifest.RootPath = root;
        manifest.BuiltUtc = DateTime.UtcNow;
        manifest.Version = 1;
        manifest.BloomBits = BloomBits;
        manifest.BloomHashCount = BloomHashCount;
        manifest.BuildGuid = BuildGuid;

        var final = GetManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct);

        ReplaceFileAtomicWithRetry(tmp, final);

        // Refresh manifest cache immediately so next search avoids JSON reload
        try
        {
            var full = Path.GetFullPath(final);
            var writeUtc = File.GetLastWriteTimeUtc(full);
            lock (_indexCacheLock)
            {
                _cachedManifest = manifest;
                _cachedManifestPath = full;
                _cachedManifestWriteUtc = writeUtc;
            }
        }
        catch
        {
            // harmless
        }
    }

    // ---------------------------
    // Bloom implementation
    // ---------------------------

    private static uint Fnv1a32(ReadOnlySpan<char> s, uint seed)
    {
        uint hash = 2166136261u ^ seed;
        for (int i = 0; i < s.Length; i++)
        {
            hash ^= s[i];
            hash *= 16777619u;
        }
        return hash;
    }

    private static void BloomAdd(ulong[] bits, ReadOnlySpan<char> gram)
    {
        uint h1 = Fnv1a32(gram, 0xA5A5A5A5);
        uint h2 = Fnv1a32(gram, 0xC3C3C3C3);

        for (int i = 0; i < BloomHashCount; i++)
        {
            uint mix = (uint)(h1 + (uint)i * 0x9E3779B9u) ^ (uint)(h2 + (uint)i * 0x7F4A7C15u);
            int bit = (int)(mix % (uint)BloomBits);
            int idx = bit / 64;
            int off = bit % 64;
            bits[idx] |= (1UL << off);
        }
    }

    private static bool BloomMightContain(ulong[] bits, ReadOnlySpan<char> gram)
    {
        uint h1 = Fnv1a32(gram, 0xA5A5A5A5);
        uint h2 = Fnv1a32(gram, 0xC3C3C3C3);

        for (int i = 0; i < BloomHashCount; i++)
        {
            uint mix = (uint)(h1 + (uint)i * 0x9E3779B9u) ^ (uint)(h2 + (uint)i * 0x7F4A7C15u);
            int bit = (int)(mix % (uint)BloomBits);
            int idx = bit / 64;
            int off = bit % 64;

            if ((bits[idx] & (1UL << off)) == 0)
                return false;
        }

        return true;
    }

    private static void WriteBloom(Stream fs, ulong[] bits)
    {
        Span<byte> buf = stackalloc byte[BloomBytes];
        buf.Clear();

        for (int i = 0; i < BloomUlongs; i++)
        {
            ulong v = bits[i];
            int baseOff = i * 8;
            buf[baseOff + 0] = (byte)(v & 0xFF);
            buf[baseOff + 1] = (byte)((v >> 8) & 0xFF);
            buf[baseOff + 2] = (byte)((v >> 16) & 0xFF);
            buf[baseOff + 3] = (byte)((v >> 24) & 0xFF);
            buf[baseOff + 4] = (byte)((v >> 32) & 0xFF);
            buf[baseOff + 5] = (byte)((v >> 40) & 0xFF);
            buf[baseOff + 6] = (byte)((v >> 48) & 0xFF);
            buf[baseOff + 7] = (byte)((v >> 56) & 0xFF);
        }

        fs.Write(buf);
    }

    private static ulong[] ReadBloom(FileStream fs, long offset)
    {
        fs.Seek(offset, SeekOrigin.Begin);
        byte[] buf = new byte[BloomBytes];
        int read = 0;
        while (read < buf.Length)
        {
            int r = fs.Read(buf, read, buf.Length - read);
            if (r <= 0) break;
            read += r;
        }

        var bits = new ulong[BloomUlongs];
        for (int i = 0; i < BloomUlongs; i++)
        {
            int o = i * 8;
            ulong v =
                ((ulong)buf[o + 0]) |
                ((ulong)buf[o + 1] << 8) |
                ((ulong)buf[o + 2] << 16) |
                ((ulong)buf[o + 3] << 24) |
                ((ulong)buf[o + 4] << 32) |
                ((ulong)buf[o + 5] << 40) |
                ((ulong)buf[o + 6] << 48) |
                ((ulong)buf[o + 7] << 56);

            bits[i] = v;
        }

        return bits;
    }

    private static void BuildBloomFromText(ulong[] bits, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        // IMPORTANT: text already has normalized spaces (single ' ').
        // We still include grams crossing spaces so bloom remains permissive.
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 2 <= text.Length)
                BloomAdd(bits, text.AsSpan(i, 2));

            if (i + 3 <= text.Length)
                BloomAdd(bits, text.AsSpan(i, 3));
        }
    }

    private static List<(int n, int start)> MakeQueryGrams(string q)
    {
        q = (q ?? "").Trim();
        var grams = new List<(int n, int start)>();

        if (q.Length >= 3)
        {
            for (int i = 0; i + 3 <= q.Length; i++)
                grams.Add((3, i));
            return grams;
        }

        if (q.Length == 2)
        {
            grams.Add((2, 0));
            return grams;
        }

        return grams;
    }

    // ---------------------------
    // Build / Update Index (incremental)
    // ---------------------------

    public Task BuildAsync(
        string root,
        string originalDir,
        string translatedDir,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
        => BuildOrUpdateAsync(root, originalDir, translatedDir, forceRebuild: true, progress, ct);

    public Task BuildOrUpdateAsync(
        string root,
        string originalDir,
        string translatedDir,
        bool forceRebuild,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested();

            await _indexIoGate.WaitAsync(ct);
            try
            {
                // Make sure stale mmap/manifest caches don't point at files being replaced
                InvalidateIndexCaches();

                SearchIndexManifest? oldMan = null;
                string oldBinPath = GetBinPath(root);

                if (!forceRebuild)
                    oldMan = await TryLoadAsync(root);

                FileStream? oldFs = null;
                if (!forceRebuild && oldMan != null && File.Exists(oldBinPath))
                {
                    try { oldFs = new FileStream(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                    catch { oldFs = null; }
                }

                var oldMap = new Dictionary<(string rel, SearchSide side), SearchIndexEntry>(new RelSideComparer());
                if (!forceRebuild && oldMan != null)
                {
                    foreach (var e in oldMan.Entries)
                        oldMap[(e.RelPath, e.Side)] = e;
                }

                progress?.Report((0, 0, "Scanning filesystem..."));

                var origFiles = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories)
                    .Select(f => (rel: NormalizeRelKey(Path.GetRelativePath(originalDir, f)), abs: f, fi: new FileInfo(f)))
                    .ToDictionary(x => x.rel, x => x, StringComparer.OrdinalIgnoreCase);

                var tranFiles = Directory.EnumerateFiles(translatedDir, "*.xml", SearchOption.AllDirectories)
                    .Select(f => (rel: NormalizeRelKey(Path.GetRelativePath(translatedDir, f)), abs: f, fi: new FileInfo(f)))
                    .ToDictionary(x => x.rel, x => x, StringComparer.OrdinalIgnoreCase);

                var allRel = origFiles.Keys.Union(tranFiles.Keys, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int total = 0;
                foreach (var rel in allRel)
                {
                    if (origFiles.ContainsKey(rel)) total++;
                    if (tranFiles.ContainsKey(rel)) total++;
                }

                var manifest = new SearchIndexManifest
                {
                    RootPath = root,
                    BuiltUtc = DateTime.UtcNow,
                    BuildGuid = BuildGuid,
                    BloomBits = BloomBits,
                    BloomHashCount = BloomHashCount,
                    Version = 1,
                };

                var finalBin = GetBinPath(root);
                var tmpBin = finalBin + ".tmp";

                try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }

                try
                {
                    using (var outFs = new FileStream(tmpBin, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        long offset = 0;
                        int id = 0;
                        int done = 0;

                        void CopyBloomBlock(FileStream src, long srcOffset, Stream dst)
                        {
                            src.Seek(srcOffset, SeekOrigin.Begin);
                            Span<byte> buf = stackalloc byte[BloomBytes];
                            int r = src.Read(buf);
                            if (r == BloomBytes) dst.Write(buf);
                            else
                            {
                                buf.Clear();
                                dst.Write(buf);
                            }
                        }

                        void IndexOne(string relKey, SearchSide side, string absPath, FileInfo fi)
                        {
                            ct.ThrowIfCancellationRequested();

                            long ticks = fi.LastWriteTimeUtc.Ticks;
                            long lenBytes = fi.Length;

                            bool copied = false;

                            if (!forceRebuild && oldFs != null &&
                                oldMap.TryGetValue((relKey, side), out var old) &&
                                old.LastWriteUtcTicks == ticks &&
                                old.LengthBytes == lenBytes &&
                                old.BloomOffset >= 0)
                            {
                                CopyBloomBlock(oldFs, old.BloomOffset, outFs);
                                copied = true;
                            }

                            if (!copied)
                            {
                                var bits = new ulong[BloomUlongs];

                                // FAST extraction: no regex; optional html decode only when '&' appears
                                string xml = File.ReadAllText(absPath, Utf8NoBom);
                                string searchable = MakeSearchableTextFromXml_Fast(xml, Options.HtmlDecodeIfAmpersandPresent);
                                BuildBloomFromText(bits, searchable);

                                WriteBloom(outFs, bits);
                            }

                            manifest.Entries.Add(new SearchIndexEntry
                            {
                                Id = id++,
                                RelPath = relKey,
                                Side = side,
                                LastWriteUtcTicks = ticks,
                                LengthBytes = lenBytes,
                                BloomOffset = offset
                            });

                            offset += BloomBytes;
                            done++;

                            if (done % 200 == 0 || done == total)
                                progress?.Report((done, total, forceRebuild ? "Rebuilding index..." : "Updating index..."));
                        }

                        progress?.Report((0, total, forceRebuild ? "Rebuilding index..." : "Updating index..."));

                        foreach (var relKey in allRel)
                        {
                            ct.ThrowIfCancellationRequested();

                            if (origFiles.TryGetValue(relKey, out var o))
                                IndexOne(relKey, SearchSide.Original, o.abs, o.fi);

                            if (tranFiles.TryGetValue(relKey, out var t))
                                IndexOne(relKey, SearchSide.Translated, t.abs, t.fi);
                        }

                        outFs.Flush(true);
                    }
                }
                catch
                {
                    try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
                    throw;
                }
                finally
                {
                    try { oldFs?.Dispose(); } catch { }
                }

                ReplaceFileAtomicWithRetry(tmpBin, finalBin);
                await SaveManifestAtomicAsync(root, manifest, ct);

                // Warm mmap cache after rebuild so next search click is faster
                try { _ = GetOrCreateMappedAccessor(finalBin); } catch { }

                progress?.Report((total, total, "Done"));
            }
            finally
            {
                _indexIoGate.Release();
            }
        }, ct);
    }

    // ---------------------------
    // Search
    // ---------------------------

    public sealed class SearchProgress
    {
        public int Candidates { get; set; }
        public int VerifiedDocs { get; set; }
        public int TotalDocsToVerify { get; set; }
        public int Groups { get; set; }
        public int TotalHits { get; set; }
        public string Phase { get; set; } = "";
    }

    public async IAsyncEnumerable<SearchResultGroup> SearchAllAsync(
        string root,
        string originalDir,
        string translatedDir,
        SearchIndexManifest manifest,
        string query,
        bool includeOriginal,
        bool includeTranslated,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
        int contextWidth,
        IProgress<SearchProgress>? progress = null,
        Func<string, bool>? relPathFilter = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        query = (query ?? "").Trim();
        if (query.Length == 0)
            yield break;

        bool useBloom = query.Length >= 2;
        var grams = MakeQueryGrams(query);

        bool sideAllowed(SearchSide s)
            => (s == SearchSide.Original && includeOriginal) ||
               (s == SearchSide.Translated && includeTranslated);

        progress?.Report(new SearchProgress { Phase = "Building candidates..." });

        var candidates = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Hold _indexIoGate only during candidate scan (bin access).
        // Release before expensive verification (file reads) so searches don’t serialize behind index I/O.
        await _indexIoGate.WaitAsync(ct);
        try
        {
            if (!useBloom)
            {
                foreach (var e in manifest.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!sideAllowed(e.Side)) continue;

                    if (relPathFilter != null && !relPathFilter(e.RelPath))
                        continue;

                    candidates.AddOrUpdate(
                        e.RelPath,
                        _ => e.Side == SearchSide.Original ? 1 : 2,
                        (_, v) => v | (e.Side == SearchSide.Original ? 1 : 2));
                }
            }
            else
            {
                string binPath = GetBinPath(root);
                var accessor = GetOrCreateMappedAccessor(binPath);

                var po = new ParallelOptions
                {
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Math.Max(1, Options.MaxBloomDegreeOfParallelism)
                };

                Parallel.ForEach(
                    manifest.Entries,
                    po,
                    () => (arr: new byte[BloomBytes], bits: new ulong[BloomUlongs]),
                    (e, _, local) =>
                    {
                        if (!sideAllowed(e.Side)) return local;
                        if (e.LastWriteUtcTicks == 0 || e.LengthBytes == 0) return local;

                        if (relPathFilter != null && !relPathFilter(e.RelPath))
                            return local;

                        // Concurrent reads from the shared accessor are read-only.
                        accessor.ReadArray(e.BloomOffset, local.arr, 0, BloomBytes);

                        for (int i = 0; i < BloomUlongs; i++)
                        {
                            int o = i * 8;
                            ulong v =
                                ((ulong)local.arr[o + 0]) |
                                ((ulong)local.arr[o + 1] << 8) |
                                ((ulong)local.arr[o + 2] << 16) |
                                ((ulong)local.arr[o + 3] << 24) |
                                ((ulong)local.arr[o + 4] << 32) |
                                ((ulong)local.arr[o + 5] << 40) |
                                ((ulong)local.arr[o + 6] << 48) |
                                ((ulong)local.arr[o + 7] << 56);

                            local.bits[i] = v;
                        }

                        bool ok = true;
                        for (int i = 0; i < grams.Count; i++)
                        {
                            var (n, start) = grams[i];
                            if (start + n > query.Length) continue;

                            if (!BloomMightContain(local.bits, query.AsSpan(start, n)))
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (!ok) return local;

                        int mask = (e.Side == SearchSide.Original) ? 1 : 2;
                        candidates.AddOrUpdate(e.RelPath, _ => mask, (_, v) => v | mask);

                        return local;
                    },
                    _ => { }
                );
            }
        }
        finally
        {
            _indexIoGate.Release();
        }

        var candidateList = candidates.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int totalDocsToVerify = 0;
        foreach (var rel in candidateList)
        {
            int mask = candidates[rel];
            if ((mask & 1) != 0) totalDocsToVerify++;
            if ((mask & 2) != 0) totalDocsToVerify++;
        }

        progress?.Report(new SearchProgress
        {
            Phase = useBloom ? "Candidate filtering done" : "Brute candidates (1-char search)",
            Candidates = totalDocsToVerify,
            TotalDocsToVerify = totalDocsToVerify
        });

        var outGroups = new ConcurrentBag<SearchResultGroup>();
        int verifiedDocs = 0;
        int totalHits = 0;

        var verifyPo = new ParallelOptions
        {
            CancellationToken = ct,
            // HDD: keep modest parallelism to reduce seek thrash
            MaxDegreeOfParallelism = Math.Max(1, Options.MaxVerifyDegreeOfParallelism)
        };

        Parallel.ForEach(candidateList, verifyPo, relKey =>
        {
            ct.ThrowIfCancellationRequested();

            int mask = candidates[relKey];

            var meta = fileMeta(relKey);
            var group = new SearchResultGroup
            {
                RelPath = relKey,
                DisplayName = string.IsNullOrWhiteSpace(meta.display) ? relKey : meta.display,
                Tooltip = string.IsNullOrWhiteSpace(meta.tooltip) ? relKey : meta.tooltip,
                Status = meta.status
            };

            int hitsO = 0;
            int hitsT = 0;

            if ((mask & 1) != 0)
            {
                string abs = Path.Combine(originalDir, relKey.Replace('/', Path.DirectorySeparatorChar));
                var hits = VerifyFileAllHits(abs, query, contextWidth, htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent);
                Interlocked.Increment(ref verifiedDocs);

                foreach (var h in hits)
                {
                    hitsO++;
                    Interlocked.Increment(ref totalHits);

                    group.Children.Add(new SearchResultChild
                    {
                        RelPath = relKey,
                        Side = SearchSide.Original,
                        Hit = h
                    });
                }
            }

            if ((mask & 2) != 0)
            {
                string abs = Path.Combine(translatedDir, relKey.Replace('/', Path.DirectorySeparatorChar));
                var hits = VerifyFileAllHits(abs, query, contextWidth, htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent);
                Interlocked.Increment(ref verifiedDocs);

                foreach (var h in hits)
                {
                    hitsT++;
                    Interlocked.Increment(ref totalHits);

                    group.Children.Add(new SearchResultChild
                    {
                        RelPath = relKey,
                        Side = SearchSide.Translated,
                        Hit = h
                    });
                }
            }

            group.HitsOriginal = hitsO;
            group.HitsTranslated = hitsT;

            if (group.Children.Count > 0)
                outGroups.Add(group);

            int v = Volatile.Read(ref verifiedDocs);
            if (v % 50 == 0)
            {
                progress?.Report(new SearchProgress
                {
                    Phase = "Searching...",
                    Candidates = totalDocsToVerify,
                    VerifiedDocs = v,
                    TotalDocsToVerify = totalDocsToVerify,
                    Groups = outGroups.Count,
                    TotalHits = Volatile.Read(ref totalHits)
                });
            }
        });

        var ordered = outGroups
            .OrderBy(g => g.RelPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        progress?.Report(new SearchProgress
        {
            Phase = "Done",
            Candidates = totalDocsToVerify,
            VerifiedDocs = verifiedDocs,
            TotalDocsToVerify = totalDocsToVerify,
            Groups = ordered.Count,
            TotalHits = totalHits
        });

        foreach (var g in ordered)
        {
            ct.ThrowIfCancellationRequested();
            yield return g;
            await Task.Yield();
        }
    }

    private sealed class RelSideComparer : IEqualityComparer<(string rel, SearchSide side)>
    {
        public bool Equals((string rel, SearchSide side) x, (string rel, SearchSide side) y)
            => string.Equals(x.rel, y.rel, StringComparison.OrdinalIgnoreCase) && x.side == y.side;

        public int GetHashCode((string rel, SearchSide side) obj)
        {
            unchecked
            {
                int h = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.rel ?? "");
                h = (h * 397) ^ obj.side.GetHashCode();
                return h;
            }
        }
    }

    private static List<SearchHit> VerifyFileAllHits(
        string absPath,
        string query,
        int contextWidth,
        bool htmlDecodeIfAmpersandPresent)
    {
        var hits = new List<SearchHit>();
        if (!File.Exists(absPath)) return hits;

        string xml;
        try { xml = File.ReadAllText(absPath, Utf8NoBom); }
        catch { return hits; }

        string text;
        try { text = MakeSearchableTextFromXml_Fast(xml, htmlDecodeIfAmpersandPresent); }
        catch { return hits; }

        if (string.IsNullOrEmpty(text)) return hits;

        int idx = 0;
        while (true)
        {
            idx = text.IndexOf(query, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            int start = idx;
            int end = idx + query.Length;

            int leftStart = Math.Max(0, start - contextWidth);
            int rightEnd = Math.Min(text.Length, end + contextWidth);

            // Text is already whitespace-normalized: no Replace/Trim per-hit
            string left = text.Substring(leftStart, start - leftStart);
            string match = text.Substring(start, query.Length);
            string right = text.Substring(end, rightEnd - end);

            hits.Add(new SearchHit
            {
                Index = start,
                Left = left,
                Match = match,
                Right = right
            });

            idx = Math.Max(end, idx + 1);
        }

        return hits;
    }
}