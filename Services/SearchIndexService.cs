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
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class SearchIndexService : ISearchIndexService
{
    public sealed class SearchIndexServiceOptions
    {
        public long MaxBloomCacheBytes { get; set; } = 128L * 1024 * 1024;
        public long MaxVerifyTextCacheChars { get; set; } = 8L * 1024 * 1024;

        // HDD + 2MB files: IO-bound verification is common.
        // Too much parallelism can *thrash* an HDD (seeks).
        // Start conservative; bump if you move corpus to SSD.
        public int MaxVerifyDegreeOfParallelism { get; set; } = Math.Min(2, Environment.ProcessorCount);

        // Bloom scan is sequential-ish over the bin file; CPU-bound-ish.
        public int MaxBloomDegreeOfParallelism { get; set; } = Math.Min(Environment.ProcessorCount, 8);

        public int ReplaceTries { get; set; } = 18;
        public int ReplaceDelayMs { get; set; } = 80;

        // If you truly need entity-decoding for search, keep this true.
        // For CBETA bodies it's often unnecessary; turning it off is faster.
        public bool HtmlDecodeIfAmpersandPresent { get; set; } = true;

        // Phase C (optional): compact-CJK bigram postings prefilter.
        // Guarded so behavior can be toggled without schema churn.
        public bool EnableCjkBigramPrefilter { get; set; } = true;
        public int CjkBigramPrefilterMinQueryLength { get; set; } = 2;
        public int CjkBigramPrefilterMaxQueryLength { get; set; } = 4;
        public double CjkBigramPrefilterMaxPassRatio { get; set; } = 0.85;
    }

    public SearchIndexServiceOptions Options { get; } = new();

    /// <summary>Inverted bigram index built alongside bloom filters. Null until first build/load.</summary>
    public InvertedSearchIndex? InvertedIndex { get; private set; }

    /// <summary>Corpus-wide CJK character frequencies (key = single char as string). Null until loaded/built.</summary>
    public IReadOnlyDictionary<string, int>? CorpusCharFreqs { get; private set; }

    /// <summary>Corpus-wide CJK bigram frequencies (key = 2-char string). Null until loaded/built.</summary>
    public IReadOnlyDictionary<string, int>? CorpusBigramFreqs { get; private set; }

    /// <summary>Total CJK characters counted across the entire corpus.</summary>
    public long CorpusTotalChars { get; private set; }

    /// <summary>True when corpus frequency data has been loaded or built.</summary>
    public bool HasCorpusFrequencies => CorpusCharFreqs != null;

    // Gate only for index file I/O (manifest/bin) so we can release before expensive verification.
    private static readonly SemaphoreSlim _indexIoGate = new(1, 1);

    private readonly Dictionary<long, LinkedListNode<(long key, ulong[] bits)>> _bloomCache = new();
    private readonly LinkedList<(long key, ulong[] bits)> _bloomLru = new();
    private long _bloomCacheBytes = 0;
    private readonly object _bloomLock = new();
    private readonly Dictionary<(string rel, SearchSide side, long ticks, long len), LinkedListNode<((string rel, SearchSide side, long ticks, long len) key, string text)>> _verifyTextCache
        = new(new VerifyTextCacheKeyComparer());
    private readonly LinkedList<((string rel, SearchSide side, long ticks, long len) key, string text)> _verifyTextLru = new();
    private long _verifyTextCacheChars = 0;
    private readonly object _verifyTextCacheLock = new();

    // Cached manifest + mmap (big real-world speed win for repeated searches)
    private readonly object _indexCacheLock = new();
    private SearchIndexManifest? _cachedManifest;
    private string? _cachedManifestPath;
    private DateTime _cachedManifestWriteUtc;
    private SearchTextManifest? _cachedTextManifest;
    private string? _cachedTextManifestPath;
    private DateTime _cachedTextManifestWriteUtc;
    private SearchCjkBigramManifest? _cachedCjk2Manifest;
    private string? _cachedCjk2ManifestPath;
    private DateTime _cachedCjk2ManifestWriteUtc;

    private MemoryMappedFile? _cachedMmf;
    private MemoryMappedViewAccessor? _cachedAccessor;
    private string? _cachedBinPath;
    private DateTime _cachedBinWriteUtc;
    private MemoryMappedFile? _cachedTextMmf;
    private string? _cachedTextBinPath;
    private DateTime _cachedTextBinWriteUtc;

    private const string ManifestFileName = "search.index.manifest.json";
    private const string BinFileName = "search.index.bin";
    // Searchable-text sidecar is versioned separately from bloom.
    // If this sidecar is missing/corrupt/mismatched, search verify falls back to XML parse.
    private const string TextManifestFileName = "search.text.manifest.json";
    private const string TextBinFileName = "search.text.bin";
    private const string Cjk2ManifestFileName = "search.cjk2.manifest.json";

    private const int BloomBits = 16384; // was 4096
    private const int BloomBytes = BloomBits / 8;
    private const int BloomUlongs = BloomBits / 64;
    private const int BloomHashCount = 5; // optional: 4 is okay too
    private const string BuildGuid = "search-v3-bloom-compact";
    private const int TextManifestVersion = 1;
    private const string TextBuildGuid = "search-v1-text-sidecar";
    private const int Cjk2ManifestVersion = 1;
    private const string Cjk2BuildGuid = "search-v1-cjk2-postings";
    private const string CorpusFreqBuildGuid = "search-v1-corpusfreq";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly HashSet<char> CooccurrenceStopChars = new()
    {
        '\u4E4B', '\u4E4E', '\u8005', '\u4E5F', '\u77E3', '\u7109', '\u800C', '\u4EE5', '\u70BA', '\u65BC',
        '\u5176', '\u6240', '\u5247', '\u4E43', '\u82E5', '\u5982', '\u96D6', '\u65E2', '\u4E14', '\u7336',
        '\u6CC1', '\u8C48', '\u84CB', '\u592B', '\u60DF', '\u552F', '\u5373', '\u9042', '\u7ADF', '\u4F46',
        '\u7136', '\u54C9', '\u4E0D', '\u662F', '\u6709', '\u7121', '\u6B64', '\u5F7C', '\u4F55'
    };

    /// <summary>Holds pre-computed results for a single work item during parallel index build (Phase 1).</summary>
    private struct ComputedEntry
    {
        public string RelKey;
        public SearchSide Side;
        public long Ticks;
        public long LenBytes;
        public ulong[]? Bits;           // null when CopiedBloom = true
        public byte[]? TextBytes;       // null when CopiedText = true
        public string SearchableText;   // always populated (needed for inverted index)
        public bool CopiedBloom;
        public bool CopiedText;
        public long OldBloomOffset;     // valid only when CopiedBloom = true
        public long OldTextOffset;      // valid only when CopiedText = true
        public int OldTextLen;          // valid only when CopiedText = true
        public Dictionary<char, int>? CharFreqs;    // per-file CJK char frequencies (build-time only)
        public Dictionary<string, int>? BigramFreqs; // per-file CJK bigram frequencies (build-time only)
    }

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
        IReadOnlyDictionary<string, int>? corpusCharFreqs = null,
        IReadOnlyDictionary<string, int>? corpusBigramFreqs = null,
        long corpusTotalChars = 0,
        int topK = 30)
    {
        query ??= "";
        string compactQuery = CompactCooccurrenceText(query);
        var queryChars = BuildQueryCharExclusions(compactQuery);

        int totalHits = 0;
        int totalWindows = 0;

        var chFreq = new Dictionary<string, int>(StringComparer.Ordinal);
        var chRange = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var chByFile = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

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

                for (int i = 0; i < window.Length; i++)
                {
                    char ch = window[i];
                    if (!ShouldKeepCoocChar(ch, queryChars)) continue;

                    string key = ch.ToString();
                    chFreq[key] = chFreq.TryGetValue(key, out var f) ? f + 1 : 1;

                    if (!chRange.TryGetValue(key, out var set))
                        chRange[key] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    set.Add(rel);

                    if (!chByFile.TryGetValue(key, out var map))
                        chByFile[key] = map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    map[rel] = map.TryGetValue(rel, out var v) ? v + 1 : 1;
                }

                char a = '\0', b = '\0';
                bool hasA = false, hasB = false;

                for (int i = 0; i < window.Length; i++)
                {
                    char ch = window[i];
                    if (!ShouldKeepNgramChar(ch)) continue;

                    if (!hasA)
                    {
                        a = ch; hasA = true;
                        continue;
                    }
                    if (!hasB)
                    {
                        b = ch; hasB = true;

                        string bg0 = string.Concat(a, b);
                        if (ShouldKeepCoocNgram(bg0, compactQuery))
                        {
                            ngFreq[bg0] = ngFreq.TryGetValue(bg0, out var f2) ? f2 + 1 : 1;

                            if (!ngRange.TryGetValue(bg0, out var set))
                                ngRange[bg0] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            set.Add(rel);

                            if (!ngByFile.TryGetValue(bg0, out var map))
                                ngByFile[bg0] = map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            map[rel] = map.TryGetValue(rel, out var v) ? v + 1 : 1;
                        }

                        continue;
                    }

                    string bg = string.Concat(b, ch);
                    if (ShouldKeepCoocNgram(bg, compactQuery))
                    {
                        ngFreq[bg] = ngFreq.TryGetValue(bg, out var fbg) ? fbg + 1 : 1;

                        if (!ngRange.TryGetValue(bg, out var setBg))
                            ngRange[bg] = setBg = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        setBg.Add(rel);

                        if (!ngByFile.TryGetValue(bg, out var mapBg))
                            ngByFile[bg] = mapBg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        mapBg[rel] = mapBg.TryGetValue(rel, out var vbg) ? vbg + 1 : 1;
                    }

                    string tg = string.Concat(a, b, ch);
                    if (ShouldKeepCoocNgram(tg, compactQuery))
                    {
                        ngFreq[tg] = ngFreq.TryGetValue(tg, out var ftg) ? ftg + 1 : 1;

                        if (!ngRange.TryGetValue(tg, out var setTg))
                            ngRange[tg] = setTg = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        setTg.Add(rel);

                        if (!ngByFile.TryGetValue(tg, out var mapTg))
                            ngByFile[tg] = mapTg = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                        mapTg[rel] = mapTg.TryGetValue(rel, out var vtg) ? vtg + 1 : 1;
                    }

                    a = b;
                    b = ch;
                }
            }
        }

        bool hasCorpusFreqs = corpusCharFreqs != null && corpusBigramFreqs != null && corpusTotalChars > 0;
        long N = corpusTotalChars;
        string fallbackNotice = "";

        // Compute query term corpus frequency (f_y) once
        long queryCorpusFreq = 0;
        if (hasCorpusFreqs && !string.IsNullOrEmpty(compactQuery))
        {
            if (compactQuery.Length == 1)
            {
                corpusCharFreqs!.TryGetValue(compactQuery, out var qf);
                queryCorpusFreq = qf;
            }
            else
            {
                long minBg = long.MaxValue;
                for (int i = 0; i < compactQuery.Length - 1; i++)
                {
                    string bg = compactQuery.Substring(i, 2);
                    corpusBigramFreqs!.TryGetValue(bg, out var bgf);
                    if (bgf < minBg) minBg = bgf;
                }
                queryCorpusFreq = minBg == long.MaxValue ? 0 : minBg;
            }
        }

        // If corpus-dependent metric selected but no freq data, fall back to frequency
        if (!hasCorpusFreqs && metric != CoocMetric.Frequency && metric != CoocMetric.Dominance)
        {
            metric = CoocMetric.Frequency;
            fallbackNotice = "Build the search index to enable association metrics.";
        }

        double DominanceShare(Dictionary<string, int>? perFile, int freq)
        {
            if (freq <= 0 || perFile == null || perFile.Count == 0) return 0;
            int max = 0;
            foreach (var kv in perFile)
                if (kv.Value > max) max = kv.Value;
            return (double)max / freq;
        }

        int LookupCollocateFreq(string key)
        {
            if (!hasCorpusFreqs) return 0;
            if (key.Length == 1)
            {
                corpusCharFreqs!.TryGetValue(key, out var f);
                return f;
            }
            else
            {
                corpusBigramFreqs!.TryGetValue(key, out var f);
                return f;
            }
        }

        static double ComputeG2(int O11, long f_x, long f_y, long NN)
        {
            if (O11 <= 0 || NN <= 0) return 0;
            long O12 = f_x - O11; if (O12 < 0) O12 = 0;
            long O21 = f_y - O11; if (O21 < 0) O21 = 0;
            long O22 = NN - f_x - f_y + O11; if (O22 < 0) O22 = 0;
            double E11 = (double)f_x * f_y / NN;
            double E12 = (double)f_x * (NN - f_y) / NN;
            double E21 = (double)(NN - f_x) * f_y / NN;
            double E22 = (double)(NN - f_x) * (NN - f_y) / NN;
            double g2 = 0;
            if (O11 > 0 && E11 > 0) g2 += O11 * Math.Log(O11 / E11);
            if (O12 > 0 && E12 > 0) g2 += O12 * Math.Log(O12 / E12);
            if (O21 > 0 && E21 > 0) g2 += O21 * Math.Log(O21 / E21);
            if (O22 > 0 && E22 > 0) g2 += O22 * Math.Log(O22 / E22);
            return 2 * g2;
        }

        double MetricValueFor(string key, int freq, int range, Dictionary<string, int>? perFile)
        {
            int f_x = LookupCollocateFreq(key);
            long f_y = queryCorpusFreq;
            double E = N > 0 ? (double)f_x * f_y / N : 0;

            return metric switch
            {
                CoocMetric.LogDice => f_x + f_y > 0 ? 14.0 + Math.Log2(2.0 * freq / (f_x + f_y)) : 0,
                CoocMetric.MI => E > 0 && freq >= 5 ? Math.Log2(freq / E) : 0,
                CoocMetric.MI3 => E > 0 && freq >= 5 ? Math.Log2((double)freq * freq * freq / E) : 0,
                CoocMetric.TScore => E > 0 ? (freq - E) / Math.Sqrt(Math.Max(1, freq)) : 0,
                CoocMetric.LogLikelihood => ComputeG2(freq, f_x, f_y, N),
                CoocMetric.Frequency => freq,
                CoocMetric.Dominance => DominanceShare(perFile, freq),
                _ => freq
            };
        }

        string metricName = metric switch
        {
            CoocMetric.LogDice => "logDice",
            CoocMetric.MI => "MI",
            CoocMetric.MI3 => "MI\u00B3",
            CoocMetric.TScore => "t-score",
            CoocMetric.LogLikelihood => "Log-likelihood",
            CoocMetric.Frequency => "Frequency",
            CoocMetric.Dominance => "Dominance (top-file share)",
            _ => "Frequency"
        };

        var left = chFreq.Select(kv =>
        {
            var key = kv.Key;
            int freq = kv.Value;
            int range = chRange.TryGetValue(key, out var s) ? s.Count : 0;
            chByFile.TryGetValue(key, out var byFile);
            double val = MetricValueFor(key, freq, range, byFile);
            return new CoocRow { Key = key, Freq = freq, Range = range, Assoc = val, Dominance = DominanceShare(byFile, freq), Bar = "" };
        }).ToList();

        var right = ngFreq.Select(kv =>
        {
            var key = kv.Key;
            int freq = kv.Value;
            int range = ngRange.TryGetValue(key, out var s) ? s.Count : 0;
            ngByFile.TryGetValue(key, out var byFile);
            double val = MetricValueFor(key, freq, range, byFile);
            return new CoocRow { Key = key, Freq = freq, Range = range, Assoc = val, Dominance = DominanceShare(byFile, freq), Bar = "" };
        }).ToList();

        // G2 significance floor: filter out statistically insignificant collocates
        if (hasCorpusFreqs && metric != CoocMetric.Frequency && metric != CoocMetric.Dominance)
        {
            int leftBefore = left.Count, rightBefore = right.Count;
            left = left.Where(r =>
            {
                int f_x = LookupCollocateFreq(r.Key);
                return ComputeG2(r.Freq, f_x, queryCorpusFreq, N) >= 6.63;
            }).ToList();
            right = right.Where(r =>
            {
                int f_x = LookupCollocateFreq(r.Key);
                return ComputeG2(r.Freq, f_x, queryCorpusFreq, N) >= 6.63;
            }).ToList();
            int removed = (leftBefore - left.Count) + (rightBefore - right.Count);
            if (removed > 0)
                fallbackNotice = (fallbackNotice ?? "") +
                    (left.Count + right.Count == 0
                        ? "No statistically significant collocates found (G2 < 6.63). Try Frequency mode."
                        : $"{removed} weak collocates filtered by significance (p < 0.01).");
        }

        left = left.OrderByDescending(r => r.Assoc).ThenByDescending(r => r.Freq).Take(topK).ToList();
        right = right.OrderByDescending(r => r.Assoc).ThenByDescending(r => r.Freq).Take(topK).ToList();

        static string MakeBar(int v, int max)
        {
            if (max <= 0) return "";
            int n = (int)Math.Round(12.0 * v / max);
            n = Math.Clamp(n, 0, 12);
            return new string('#', n);
        }

        int maxC = left.Count > 0 ? left.Max(r => r.Freq) : 0;
        int maxN = right.Count > 0 ? right.Max(r => r.Freq) : 0;
        foreach (var r in left) r.Bar = MakeBar(r.Freq, maxC);
        foreach (var r in right) r.Bar = MakeBar(r.Freq, maxN);

        // Normalize BarRatio by the selected metric (Assoc), not raw frequency
        double maxAssocL = left.Count > 0 ? left.Max(r => Math.Abs(r.Assoc)) : 0;
        double maxAssocR = right.Count > 0 ? right.Max(r => Math.Abs(r.Assoc)) : 0;
        foreach (var r in left) r.BarRatio = maxAssocL > 0 ? Math.Clamp(r.Assoc / maxAssocL, 0, 1) : 0;
        foreach (var r in right) r.BarRatio = maxAssocR > 0 ? Math.Clamp(r.Assoc / maxAssocR, 0, 1) : 0;

        var zip = right.OrderByDescending(r => r.Freq).Take(12).Select((r, i) => $"{i + 1}:{r.Freq}").ToArray();
        string zipLine = zip.Length > 0 ? ("Zipf-ish ranks (top ngrams): " + string.Join("  ", zip)) : "";

        var domTop = right.OrderByDescending(r => r.Freq).Take(10).Select(r =>
        {
            ngByFile.TryGetValue(r.Key, out var byFile);
            double share = DominanceShare(byFile, r.Freq);
            int bars = Math.Clamp((int)Math.Round(12 * share), 0, 12);
            return $"{r.Key}:{share * 100:0.#}% {new string('#', bars)}";
        }).ToArray();

        string domLine = domTop.Length > 0 ? ("Dominance (top-file share): " + string.Join("  ", domTop)) : "";

        var extra = string.Join("\n", new[] { zipLine, domLine }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new CooccurrencePanelResult
        {
            Summary = $"result-scoped metric={metricName}   hits={totalHits:n0}, windows={totalWindows:n0}, result files={Nfiles:n0}, context={contextWidth} chars",
            LeftTitle = $"Top characters within current results by {metricName}",
            RightTitle = $"Top bigrams / trigrams within current results by {metricName}",
            Left = left,
            Right = right,
            ExtraLine = string.Join("\n", new[] { "Window-scoped analytics from current search results; not corpus-wide.", fallbackNotice, extra }.Where(s => !string.IsNullOrWhiteSpace(s)))
        };
    }

    public static CooccurrencePanelResult ComputeCorpusCooccurrences(
        string originalDir,
        string translatedDir,
        IEnumerable<FileNavItem> files,
        string query,
        bool includeOriginal,
        bool includeTranslated,
        int contextWidth,
        CoocMetric metric,
        int topK = 30,
        Func<string, bool>? relPathFilter = null,
        TranslationStatus? statusFilter = null,
        IProgress<(int done, int total)>? progress = null,
        IReadOnlyDictionary<string, int>? corpusCharFreqs = null,
        IReadOnlyDictionary<string, int>? corpusBigramFreqs = null,
        long corpusTotalChars = 0,
        CancellationToken ct = default)
    {
        var selectedFiles = (files ?? Array.Empty<FileNavItem>())
            .Where(f => f != null && !string.IsNullOrWhiteSpace(f.RelPath))
            .Where(f => !statusFilter.HasValue || f.Status == statusFilter.Value)
            .Where(f => relPathFilter == null || relPathFilter(f.RelPath))
            .ToList();

        var groups = new List<SearchResultGroup>(selectedFiles.Count);
        for (int i = 0; i < selectedFiles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var file = selectedFiles[i];
            var children = BuildAlignedDisplayChildrenFromIndexedUnits(
                originalDir,
                translatedDir,
                file.RelPath,
                query,
                includeOriginal,
                includeTranslated,
                contextWidth);

            if (children.Count > 0)
            {
                var fileTooltip = string.IsNullOrWhiteSpace(file.Tooltip) ? file.RelPath : file.Tooltip;
                var fileZh = "";
                var fileNl = fileTooltip.IndexOf('\n');
                if (fileNl >= 0 && fileNl < fileTooltip.Length - 1)
                    fileZh = fileTooltip[(fileNl + 1)..];

                groups.Add(new SearchResultGroup
                {
                    RelPath = file.RelPath,
                    DisplayName = string.IsNullOrWhiteSpace(file.DisplayShort) ? file.FileName : file.DisplayShort,
                    Tooltip = fileTooltip,
                    ChineseTitle = fileZh,
                    Status = file.Status,
                    Children = children,
                    HitsOriginal = children.Count(c => c.Side == SearchSide.Original),
                    HitsTranslated = children.Count(c => c.Side == SearchSide.Translated)
                });
            }

            progress?.Report((i + 1, selectedFiles.Count));
        }

        var result = ComputeCooccurrences(groups, query, contextWidth, metric, corpusCharFreqs, corpusBigramFreqs, corpusTotalChars, topK);
        result.Summary = result.Summary.Replace("result-scoped", "corpus-scan", StringComparison.OrdinalIgnoreCase);
        result.LeftTitle = result.LeftTitle.Replace("within current results", "across filtered corpus", StringComparison.OrdinalIgnoreCase);
        result.RightTitle = result.RightTitle.Replace("within current results", "across filtered corpus", StringComparison.OrdinalIgnoreCase);
        result.ExtraLine = string.Join("\n", new[]
        {
            $"Filtered files scanned: {selectedFiles.Count:n0}",
            "Corpus scan is slower because it re-reads filtered files directly.",
            result.ExtraLine
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return result;
    }
    private static HashSet<char> BuildQueryCharExclusions(string compactQuery)
    {
        var set = new HashSet<char>();
        foreach (var ch in compactQuery)
        {
            if (ShouldKeepNgramChar(ch))
                set.Add(ch);
        }
        return set;
    }

    private static string CompactCooccurrenceText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ShouldKeepNgramChar(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static bool ShouldKeepCoocChar(char ch, HashSet<char> queryChars)
    {
        if (!IsMeaningfulCoocChar(ch))
            return false;

        return !queryChars.Contains(char.ToLowerInvariant(ch));
    }

    private static bool ShouldKeepCoocNgram(string value, string compactQuery)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        bool hasCjk = false;
        bool hasLetter = false;
        foreach (var ch in value)
        {
            if (!ShouldKeepNgramChar(ch))
                return false;

            if (IsCjk(ch))
                hasCjk = true;
            else if (char.IsLetter(ch))
                hasLetter = true;
        }

        if (!hasCjk && !hasLetter)
            return false;

        var compactValue = CompactCooccurrenceText(value);
        if (compactValue.Length < 2)
            return false;

        if (compactValue.All(IsCooccurrenceStopChar))
            return false;

        if (compactValue.Length >= 3)
        {
            bool leadingParticles = IsCooccurrenceStopChar(compactValue[0]) && IsCooccurrenceStopChar(compactValue[1]);
            bool trailingParticles = IsCooccurrenceStopChar(compactValue[^1]) && IsCooccurrenceStopChar(compactValue[^2]);
            if (leadingParticles || trailingParticles)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(compactQuery) &&
            (string.Equals(compactValue, compactQuery, StringComparison.OrdinalIgnoreCase) ||
             compactQuery.Contains(compactValue, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return compactValue switch
        {
            "the" or "and" or "ing" or "ion" or "for" or "ent" => false,
            _ => true
        };
    }

    private static bool IsCooccurrenceStopChar(char ch)
    {
        return ch switch
        {
            '\u4E4B' or '\u4E4E' or '\u8005' or '\u4E5F' or '\u77E3' or '\u7109' or '\u800C' or '\u4EE5' or '\u70BA' or '\u65BC'
            or '\u5176' or '\u6240' or '\u5247' or '\u4E43' or '\u82E5' or '\u5982' or '\u96D6' or '\u65E2' or '\u4E14' or '\u7336'
            or '\u6CC1' or '\u8C48' or '\u84CB' or '\u592B' or '\u60DF' or '\u552F' or '\u5373' or '\u9042' or '\u7ADF' or '\u4F46'
            or '\u7136' or '\u54C9' or '\u4E0D' or '\u662F' or '\u6709' or '\u7121' or '\u6B64' or '\u5F7C' or '\u4F55' => true,
            _ => false,
        };
    }

    private static bool IsMeaningfulCoocChar(char ch)
    {
        if (!ShouldKeepNgramChar(ch))
            return false;

        if (ch <= 0x7F && char.IsLetter(ch))
            return false;

        if (CooccurrenceStopChars.Contains(ch))
            return false;

        return true;
    }

    private static bool ShouldKeepNgramChar(char ch)
    {
        if (char.IsWhiteSpace(ch) || char.IsControl(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch) || char.IsDigit(ch))
            return false;

        return IsCjk(ch) || char.IsLetter(ch);
    }

    private static bool IsCjk(char ch)
        => (ch >= '\u3400' && ch <= '\u4DBF')
        || (ch >= '\u4E00' && ch <= '\u9FFF')
        || (ch >= '\uF900' && ch <= '\uFAFF');

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
            _cachedTextManifest = null;
            _cachedTextManifestPath = null;
            _cachedTextManifestWriteUtc = default;
            _cachedCjk2Manifest = null;
            _cachedCjk2ManifestPath = null;
            _cachedCjk2ManifestWriteUtc = default;

            try { _cachedAccessor?.Dispose(); } catch { }
            try { _cachedMmf?.Dispose(); } catch { }
            try { _cachedTextMmf?.Dispose(); } catch { }

            _cachedAccessor = null;
            _cachedMmf = null;
            _cachedBinPath = null;
            _cachedBinWriteUtc = default;
            _cachedTextMmf = null;
            _cachedTextBinPath = null;
            _cachedTextBinWriteUtc = default;
        }

        ClearVerifyTextCache();
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

    private MemoryMappedFile GetOrCreateTextMappedFile(string textBinPath, DateTime writeUtc)
    {
        var full = Path.GetFullPath(textBinPath);

        lock (_indexCacheLock)
        {
            if (_cachedTextMmf != null &&
                string.Equals(_cachedTextBinPath, full, StringComparison.OrdinalIgnoreCase) &&
                _cachedTextBinWriteUtc == writeUtc)
            {
                return _cachedTextMmf;
            }

            try { _cachedTextMmf?.Dispose(); } catch { }

            _cachedTextMmf = MemoryMappedFile.CreateFromFile(full, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _cachedTextBinPath = full;
            _cachedTextBinWriteUtc = writeUtc;
            return _cachedTextMmf;
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

    /// <summary>Resolve a RelPath to an absolute filesystem path, trying primary dir first then additionals.</summary>
    private static string ResolveAbsPath(string primaryDir, IReadOnlyList<string>? additionalDirs, string relKey)
    {
        var relFs = relKey.Replace('/', Path.DirectorySeparatorChar);
        var primary = Path.Combine(primaryDir, relFs);
        if (File.Exists(primary)) return primary;

        if (additionalDirs != null)
        {
            foreach (var dir in additionalDirs)
            {
                var candidate = Path.Combine(dir, relFs);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return primary; // fallback — VerifyFileAllHits handles missing files gracefully
    }

    public string GetManifestPath(string root) => Path.Combine(root, ManifestFileName);
    public string GetBinPath(string root) => Path.Combine(root, BinFileName);
    public string GetTextManifestPath(string root) => Path.Combine(root, TextManifestFileName);
    public string GetTextBinPath(string root) => Path.Combine(root, TextBinFileName);
    public string GetCjk2ManifestPath(string root) => Path.Combine(root, Cjk2ManifestFileName);

    public async Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs)
    {
        var manifestPath = GetManifestPath(root);
        if (!File.Exists(manifestPath))
            return true;

        SearchIndexManifest? manifest;
        try { manifest = await TryLoadAsync(root); }
        catch { return true; }

        if (manifest == null)
            return true;

        var manifestWriteUtc = File.GetLastWriteTimeUtc(manifestPath);

        // Only check translated dirs for changes — originals are a read-only corpus
        foreach (var tDir in translatedDirs)
        {
            if (!Directory.Exists(tDir)) continue;
            foreach (var f in Directory.EnumerateFiles(tDir, "*.xml", SearchOption.AllDirectories))
            {
                if (File.GetLastWriteTimeUtc(f) > manifestWriteUtc)
                    return true;
            }
        }

        // If manifest has zero entries but originals exist, it needs a full build
        if (manifest.Entries.Count == 0 && Directory.Exists(originalDir))
            return true;

        return false;
    }

    public void ClearBloomCache()
    {
        lock (_bloomLock)
        {
            _bloomCache.Clear();
            _bloomLru.Clear();
            _bloomCacheBytes = 0;
        }
    }

    public void ClearVerifyTextCache()
    {
        lock (_verifyTextCacheLock)
        {
            _verifyTextCache.Clear();
            _verifyTextLru.Clear();
            _verifyTextCacheChars = 0;
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

    private string GetSearchableTextCached(
        string root,
        string relPath,
        SearchSide side,
        long lastWriteUtcTicks,
        long lengthBytes,
        SearchTextEntry? textEntry,
        string absPath,
        bool htmlDecodeIfAmpersandPresent)
    {
        var key = (rel: NormalizeRelKey(relPath), side, ticks: lastWriteUtcTicks, len: lengthBytes);

        lock (_verifyTextCacheLock)
        {
            if (_verifyTextCache.TryGetValue(key, out var node))
            {
                _verifyTextLru.Remove(node);
                _verifyTextLru.AddFirst(node);
                return node.Value.text;
            }
        }

        if (!TryReadSearchableTextFromSidecar(root, key, textEntry, out string searchable))
        {
            string xml;
            try { xml = File.ReadAllText(absPath, Utf8NoBom); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Read failed for {absPath}: {ex.Message}"); return ""; }

            try { searchable = MakeSearchableTextFromXml_Fast(xml, htmlDecodeIfAmpersandPresent); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Parse failed for {absPath}: {ex.Message}"); return ""; }
        }

        lock (_verifyTextCacheLock)
        {
            if (_verifyTextCache.TryGetValue(key, out var existing))
            {
                _verifyTextLru.Remove(existing);
                _verifyTextLru.AddFirst(existing);
                return existing.Value.text;
            }

            var node = new LinkedListNode<((string rel, SearchSide side, long ticks, long len) key, string text)>((key, searchable));
            _verifyTextLru.AddFirst(node);
            _verifyTextCache[key] = node;
            _verifyTextCacheChars += searchable.Length;

            EvictVerifyTextCacheIfNeeded();
        }

        return searchable;
    }

    private bool TryReadSearchableTextFromSidecar(
        string root,
        (string rel, SearchSide side, long ticks, long len) key,
        SearchTextEntry? textEntry,
        out string searchable)
    {
        searchable = "";
        if (textEntry == null) return false;
        if (textEntry.TextOffset < 0 || textEntry.TextLengthBytes < 0) return false;
        if (!string.Equals(NormalizeRelKey(textEntry.RelPath), key.rel, StringComparison.OrdinalIgnoreCase)) return false;
        if (textEntry.Side != key.side) return false;
        if (textEntry.LastWriteUtcTicks != key.ticks || textEntry.LengthBytes != key.len) return false;
        if (textEntry.TextLengthBytes == 0) return true;

        try
        {
            string textBinPath = GetTextBinPath(root);
            var full = Path.GetFullPath(textBinPath);
            if (!File.Exists(full)) return false;

            var fileLen = new FileInfo(full).Length;
            long end = textEntry.TextOffset + textEntry.TextLengthBytes;
            if (textEntry.TextOffset < 0 || end < textEntry.TextOffset || end > fileLen)
                return false;

            using var accessor = GetOrCreateTextMappedFile(full, File.GetLastWriteTimeUtc(full))
                .CreateViewAccessor(textEntry.TextOffset, textEntry.TextLengthBytes, MemoryMappedFileAccess.Read);

            var bytes = new byte[textEntry.TextLengthBytes];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            searchable = Utf8NoBom.GetString(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EvictVerifyTextCacheIfNeeded()
    {
        long max = Math.Max(0, Options.MaxVerifyTextCacheChars);

        if (max == 0)
        {
            _verifyTextCache.Clear();
            _verifyTextLru.Clear();
            _verifyTextCacheChars = 0;
            return;
        }

        while (_verifyTextCacheChars > max && _verifyTextLru.Last != null)
        {
            var last = _verifyTextLru.Last!;
            _verifyTextLru.RemoveLast();
            _verifyTextCache.Remove(last.Value.key);
            _verifyTextCacheChars -= last.Value.text.Length;
        }
    }

    // ---------------------------
    // CJK character classification (shared with InvertedSearchIndex)
    // ---------------------------

    /// <summary>Returns true if the character is in a CJK range suitable for indexing.</summary>
    internal static bool IsIndexableCjk(char ch)
        => (ch >= '\u4E00' && ch <= '\u9FFF')
        || (ch >= '\u3400' && ch <= '\u4DBF')
        || (ch >= '\uF900' && ch <= '\uFAFF');

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
            if (sb.Capacity <= 256 * 1024) // don't hold giant buffers
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

            // Try loading inverted index alongside bloom
            if (InvertedIndex == null)
            {
                try
                {
                    var invPath = Path.Combine(root, "search.inverted.bin");
                    var inv = new InvertedSearchIndex();
                    if (await inv.TryLoadAsync(invPath, CancellationToken.None))
                    {
                        InvertedIndex = inv;
                        Dbg($"Inverted index loaded: {inv.TermCount} terms, {inv.DocCount} docs");
                    }
                }
                catch { /* inverted index is optional */ }
            }

            // Try loading corpus frequency index alongside bloom
            if (CorpusCharFreqs == null)
            {
                try { await TryLoadCorpusFrequenciesAsync(root); }
                catch { /* corpus freq index is optional */ }
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Loads the corpus frequency index from disk. Returns true on success.</summary>
    public async Task<bool> TryLoadCorpusFrequenciesAsync(string root)
    {
        var manifestPath = Path.Combine(root, "search.corpusfreq.manifest.json");
        var binPath = Path.Combine(root, "search.corpusfreq.bin");

        if (!File.Exists(manifestPath) || !File.Exists(binPath))
            return false;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return false;

            var freqManifest = JsonSerializer.Deserialize<CorpusFreqManifest>(json, JsonOpts);
            if (freqManifest == null || freqManifest.Version != 1) return false;
            if (!string.Equals(freqManifest.BuildGuid, CorpusFreqBuildGuid, StringComparison.Ordinal)) return false;

            var bytes = await File.ReadAllBytesAsync(binPath);
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms, Utf8NoBom, leaveOpen: false);

            // Validate magic "CF01"
            byte m0 = br.ReadByte(), m1 = br.ReadByte(), m2 = br.ReadByte(), m3 = br.ReadByte();
            if (m0 != (byte)'C' || m1 != (byte)'F' || m2 != (byte)'0' || m3 != (byte)'1')
                return false;

            int charCount = br.ReadInt32();
            int bigramCount = br.ReadInt32();
            long totalChars = br.ReadInt64();

            var charFreqs = new Dictionary<string, int>(charCount);
            for (int i = 0; i < charCount; i++)
            {
                char ch = br.ReadChar();
                int freq = br.ReadInt32();
                charFreqs[ch.ToString()] = freq;
            }

            var bigramFreqs = new Dictionary<string, int>(bigramCount);
            for (int i = 0; i < bigramCount; i++)
            {
                char c1 = br.ReadChar();
                char c2 = br.ReadChar();
                int freq = br.ReadInt32();
                bigramFreqs[string.Concat(c1, c2)] = freq;
            }

            CorpusCharFreqs = charFreqs;
            CorpusBigramFreqs = bigramFreqs;
            CorpusTotalChars = totalChars;

            Dbg($"Corpus freq index loaded: {charCount} chars, {bigramCount} bigrams, {totalChars} total");
            return true;
        }
        catch (Exception ex)
        {
            Dbg($"Corpus freq load failed: {ex.Message}");
            return false;
        }
    }

    public async Task<SearchTextManifest?> TryLoadTextManifestAsync(string root)
    {
        try
        {
            var mp = GetTextManifestPath(root);
            var bp = GetTextBinPath(root);

            if (!File.Exists(mp) || !File.Exists(bp))
                return null;

            var mpFull = Path.GetFullPath(mp);
            var mpWriteUtc = File.GetLastWriteTimeUtc(mpFull);

            lock (_indexCacheLock)
            {
                if (_cachedTextManifest != null &&
                    string.Equals(_cachedTextManifestPath, mpFull, StringComparison.OrdinalIgnoreCase) &&
                    _cachedTextManifestWriteUtc == mpWriteUtc)
                {
                    return _cachedTextManifest;
                }
            }

            var json = await File.ReadAllTextAsync(mp, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var man = JsonSerializer.Deserialize<SearchTextManifest>(json, JsonOpts);
            if (man == null) return null;

            if (!string.Equals(Path.GetFullPath(man.RootPath), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return null;
            if (man.Version != TextManifestVersion) return null;
            if (!string.Equals(man.BuildGuid, TextBuildGuid, StringComparison.Ordinal)) return null;
            if (man.Entries == null || man.Entries.Count == 0) return null;

            var binLen = new FileInfo(bp).Length;
            foreach (var e in man.Entries)
            {
                if (e.TextOffset < 0 || e.TextLengthBytes < 0) return null;
                long end = e.TextOffset + e.TextLengthBytes;
                if (end < e.TextOffset || end > binLen) return null;
            }

            lock (_indexCacheLock)
            {
                _cachedTextManifest = man;
                _cachedTextManifestPath = mpFull;
                _cachedTextManifestWriteUtc = mpWriteUtc;
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root)
    {
        try
        {
            var mp = GetCjk2ManifestPath(root);
            if (!File.Exists(mp))
                return null;

            var mpFull = Path.GetFullPath(mp);
            var mpWriteUtc = File.GetLastWriteTimeUtc(mpFull);

            lock (_indexCacheLock)
            {
                if (_cachedCjk2Manifest != null &&
                    string.Equals(_cachedCjk2ManifestPath, mpFull, StringComparison.OrdinalIgnoreCase) &&
                    _cachedCjk2ManifestWriteUtc == mpWriteUtc)
                {
                    return _cachedCjk2Manifest;
                }
            }

            var json = await File.ReadAllTextAsync(mp, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var man = JsonSerializer.Deserialize<SearchCjkBigramManifest>(json, JsonOpts);
            if (man == null) return null;

            if (!string.Equals(Path.GetFullPath(man.RootPath), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return null;
            if (man.Version != Cjk2ManifestVersion) return null;
            if (!string.Equals(man.BuildGuid, Cjk2BuildGuid, StringComparison.Ordinal)) return null;
            if (man.GramSize != 2) return null;
            if (man.Postings == null) return null;
            if (man.EntryCount < 0) return null;

            foreach (var p in man.Postings)
            {
                if (p == null) return null;
                if (string.IsNullOrEmpty(p.Gram) || p.Gram.Length != 2) return null;
                if (p.EntryIds == null) return null;

                for (int i = 0; i < p.EntryIds.Count; i++)
                {
                    int id = p.EntryIds[i];
                    if (id < 0 || id >= man.EntryCount) return null;
                }
            }

            lock (_indexCacheLock)
            {
                _cachedCjk2Manifest = man;
                _cachedCjk2ManifestPath = mpFull;
                _cachedCjk2ManifestWriteUtc = mpWriteUtc;
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

    private async Task SaveTextManifestAtomicAsync(string root, SearchTextManifest manifest, CancellationToken ct)
    {
        manifest.RootPath = root;
        manifest.BuiltUtc = DateTime.UtcNow;
        manifest.Version = TextManifestVersion;
        manifest.BuildGuid = TextBuildGuid;

        var final = GetTextManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct);

        ReplaceFileAtomicWithRetry(tmp, final);

        try
        {
            var full = Path.GetFullPath(final);
            var writeUtc = File.GetLastWriteTimeUtc(full);
            lock (_indexCacheLock)
            {
                _cachedTextManifest = manifest;
                _cachedTextManifestPath = full;
                _cachedTextManifestWriteUtc = writeUtc;
            }
        }
        catch
        {
            // harmless
        }
    }

    private async Task SaveCjk2ManifestAtomicAsync(string root, SearchCjkBigramManifest manifest, CancellationToken ct)
    {
        manifest.RootPath = root;
        manifest.BuiltUtc = DateTime.UtcNow;
        manifest.Version = Cjk2ManifestVersion;
        manifest.BuildGuid = Cjk2BuildGuid;

        var final = GetCjk2ManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct);

        ReplaceFileAtomicWithRetry(tmp, final);

        try
        {
            var full = Path.GetFullPath(final);
            var writeUtc = File.GetLastWriteTimeUtc(full);
            lock (_indexCacheLock)
            {
                _cachedCjk2Manifest = manifest;
                _cachedCjk2ManifestPath = full;
                _cachedCjk2ManifestWriteUtc = writeUtc;
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

        // Standard grams (spaces preserved) - needed for English phrase search.
        for (int i = 0; i < text.Length; i++)
        {
            if (i + 2 <= text.Length)
                BloomAdd(bits, text.AsSpan(i, 2));

            if (i + 3 <= text.Length)
                BloomAdd(bits, text.AsSpan(i, 3));
        }

        // Compact grams (spaces + CJK punctuation stripped) - for CJK phrase search across <lb>
        // boundaries. lb-tags introduce newlines to spaces; CBETA punctuation is a modern editorial
        // addition not present in the original text.  Stripping both lets cross-lb / cross-punct
        // phrases be found.
        string compact = CjkMatchNormalizer.Normalize(text);
        if (compact.Length != text.Length)
        {
            for (int i = 0; i < compact.Length; i++)
            {
                if (i + 2 <= compact.Length)
                    BloomAdd(bits, compact.AsSpan(i, 2));

                if (i + 3 <= compact.Length)
                    BloomAdd(bits, compact.AsSpan(i, 3));
            }
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

    private static List<string> MakeCompactQueryBigrams(string q)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(q) || q.Length < 2) return list;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i + 2 <= q.Length; i++)
        {
            string g = q.Substring(i, 2);
            if (!seen.Add(g)) continue;
            list.Add(g);
        }
        return list;
    }

    private static IEnumerable<string> EnumerateUniqueCompactBigrams(string text)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        string compact = CjkMatchNormalizer.Normalize(text);
        if (compact.Length < 2)
            yield break;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i + 2 <= compact.Length; i++)
        {
            string gram = compact.Substring(i, 2);
            if (!seen.Add(gram)) continue;
            yield return gram;
        }
    }

    private SearchCjkBigramManifest BuildCjk2ManifestFromTextSidecar(
        string root,
        SearchIndexManifest indexManifest,
        SearchTextManifest textManifest,
        CancellationToken ct)
    {
        string textBinPath = GetTextBinPath(root);
        if (!File.Exists(textBinPath))
            throw new FileNotFoundException("search.text.bin not found for cjk2 postings build.", textBinPath);

        var textById = new Dictionary<int, SearchTextEntry>();
        foreach (var t in textManifest.Entries)
            textById[t.Id] = t;

        var postings = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        using var fs = new FileStream(textBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        foreach (var e in indexManifest.Entries)
        {
            ct.ThrowIfCancellationRequested();

            if (!textById.TryGetValue(e.Id, out var textEntry))
                continue;
            if (textEntry.TextLengthBytes <= 0)
                continue;
            if (textEntry.TextOffset < 0)
                continue;

            fs.Seek(textEntry.TextOffset, SeekOrigin.Begin);
            var bytes = new byte[textEntry.TextLengthBytes];
            int read = 0;
            while (read < bytes.Length)
            {
                int r = fs.Read(bytes, read, bytes.Length - read);
                if (r <= 0) break;
                read += r;
            }
            if (read != bytes.Length)
                continue;

            string searchable = Utf8NoBom.GetString(bytes);
            foreach (var gram in EnumerateUniqueCompactBigrams(searchable))
            {
                if (!postings.TryGetValue(gram, out var ids))
                {
                    ids = new List<int>();
                    postings[gram] = ids;
                }
                ids.Add(e.Id);
            }
        }

        var manifest = new SearchCjkBigramManifest
        {
            RootPath = root,
            BuiltUtc = DateTime.UtcNow,
            BuildGuid = Cjk2BuildGuid,
            Version = Cjk2ManifestVersion,
            GramSize = 2,
            EntryCount = indexManifest.Entries.Count
        };

        foreach (var kv in postings.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            kv.Value.Sort();
            manifest.Postings.Add(new SearchCjkBigramPosting
            {
                Gram = kv.Key,
                EntryIds = kv.Value
            });
        }

        return manifest;
    }

    // ---------------------------
    // Build / Update Index (incremental)
    // ---------------------------

    public Task BuildAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
        => BuildOrUpdateAsync(root, originalDir, translatedDirs, forceRebuild: true,
            additionalOriginalDirs: null, additionalTranslatedDirs: null, progress: progress, ct: ct);

    public Task BuildOrUpdateAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        bool forceRebuild,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
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
                SearchTextManifest? oldTextMan = null;
                string oldBinPath = GetBinPath(root);
                string oldTextBinPath = GetTextBinPath(root);

                if (!forceRebuild)
                {
                    oldMan = await TryLoadAsync(root);
                    oldTextMan = await TryLoadTextManifestAsync(root);
                }

                FileStream? oldFs = null;
                if (!forceRebuild && oldMan != null && File.Exists(oldBinPath))
                {
                    try { oldFs = new FileStream(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                    catch { oldFs = null; }
                }
                FileStream? oldTextFs = null;
                if (!forceRebuild && oldTextMan != null && File.Exists(oldTextBinPath))
                {
                    try { oldTextFs = new FileStream(oldTextBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                    catch { oldTextFs = null; }
                }

                var oldMap = new Dictionary<(string rel, SearchSide side), SearchIndexEntry>(new RelSideComparer());
                if (!forceRebuild && oldMan != null)
                {
                    foreach (var e in oldMan.Entries)
                        oldMap[(e.RelPath, e.Side)] = e;
                }
                var oldTextMap = new Dictionary<(string rel, SearchSide side), SearchTextEntry>(new RelSideComparer());
                if (!forceRebuild && oldTextMan != null)
                {
                    foreach (var e in oldTextMan.Entries)
                        oldTextMap[(e.RelPath, e.Side)] = e;
                }

                progress?.Report((0, 0, "Scanning filesystem..."));

                var origFiles = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories)
                    .Select(f => (rel: NormalizeRelKey(Path.GetRelativePath(originalDir, f)), abs: f, fi: new FileInfo(f)))
                    .ToDictionary(x => x.rel, x => x, StringComparer.OrdinalIgnoreCase);

                // Scan additional original dirs (e.g., OpenZen alongside CBETA)
                if (additionalOriginalDirs != null)
                {
                    foreach (var addDir in additionalOriginalDirs)
                    {
                        if (string.IsNullOrWhiteSpace(addDir) || !Directory.Exists(addDir)) continue;
                        foreach (var f in Directory.EnumerateFiles(addDir, "*.xml", SearchOption.AllDirectories))
                        {
                            var rel = NormalizeRelKey(Path.GetRelativePath(addDir, f));
                            if (!origFiles.ContainsKey(rel))
                                origFiles[rel] = (rel, f, new FileInfo(f));
                        }
                    }
                }

                var tranFiles = new Dictionary<string, (string rel, string abs, FileInfo fi)>(StringComparer.OrdinalIgnoreCase);
                foreach (var tDir in translatedDirs)
                {
                    if (!Directory.Exists(tDir)) continue;
                    foreach (var f in Directory.EnumerateFiles(tDir, "*.xml", SearchOption.AllDirectories))
                    {
                        var rel = NormalizeRelKey(Path.GetRelativePath(tDir, f));
                        if (!tranFiles.ContainsKey(rel))
                            tranFiles[rel] = (rel, f, new FileInfo(f));
                    }
                }
                // Scan additional translated dirs (e.g., OpenZen translations)
                if (additionalTranslatedDirs != null)
                {
                    foreach (var tDir in additionalTranslatedDirs)
                    {
                        if (string.IsNullOrWhiteSpace(tDir) || !Directory.Exists(tDir)) continue;
                        foreach (var f in Directory.EnumerateFiles(tDir, "*.xml", SearchOption.AllDirectories))
                        {
                            var rel = NormalizeRelKey(Path.GetRelativePath(tDir, f));
                            if (!tranFiles.ContainsKey(rel))
                                tranFiles[rel] = (rel, f, new FileInfo(f));
                        }
                    }
                }

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
                var textManifest = new SearchTextManifest
                {
                    RootPath = root,
                    BuiltUtc = DateTime.UtcNow,
                    BuildGuid = TextBuildGuid,
                    Version = TextManifestVersion,
                };

                var finalBin = GetBinPath(root);
                var tmpBin = finalBin + ".tmp";
                var finalTextBin = GetTextBinPath(root);
                var tmpTextBin = finalTextBin + ".tmp";

                try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
                try { if (File.Exists(tmpTextBin)) File.Delete(tmpTextBin); } catch { }

                var invertedDocs = new List<(string relPath, string text)>();

                // Corpus frequency accumulators — merged during Phase 2 before entries are cleared
                var corpusCharFreqs = new Dictionary<string, int>(32768);
                var corpusBigramFreqs = new Dictionary<string, int>(65536);
                long corpusTotalChars = 0;

                try
                {
                    using (var outFs = new FileStream(tmpBin, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var outTextFs = new FileStream(tmpTextBin, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        long bloomOffset = 0;
                        long textOffset = 0;
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
                        static bool CopyTextBlock(FileStream src, long srcOffset, int len, Stream dst)
                        {
                            if (len < 0 || srcOffset < 0) return false;
                            if (len == 0) return true;

                            src.Seek(srcOffset, SeekOrigin.Begin);
                            var buf = new byte[64 * 1024];
                            int remaining = len;
                            while (remaining > 0)
                            {
                                int want = Math.Min(buf.Length, remaining);
                                int read = src.Read(buf, 0, want);
                                if (read <= 0) return false;
                                dst.Write(buf, 0, read);
                                remaining -= read;
                            }
                            return true;
                        }

                        // ── Build flat work list (preserves deterministic ordering) ──
                        var workItems = new List<(string relKey, SearchSide side, string absPath, FileInfo fi)>(total);
                        foreach (var relKey in allRel)
                        {
                            if (origFiles.TryGetValue(relKey, out var o))
                                workItems.Add((relKey, SearchSide.Original, o.abs, o.fi));
                            if (tranFiles.TryGetValue(relKey, out var t))
                                workItems.Add((relKey, SearchSide.Translated, t.abs, t.fi));
                        }

                        // ── Phase 1: Parallel compute (CPU+IO bound) ──
                        var buildSw = System.Diagnostics.Stopwatch.StartNew();
                        progress?.Report((0, total, forceRebuild ? "Rebuilding index..." : "Updating index..."));

                        var computed = new ComputedEntry[workItems.Count];
                        bool htmlDecode = Options.HtmlDecodeIfAmpersandPresent;

                        Parallel.For(0, workItems.Count, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Math.Max(1, Options.MaxBloomDegreeOfParallelism),
                            CancellationToken = ct
                        }, i =>
                        {
                            var (relKey, side, absPath, fi) = workItems[i];
                            long ticks = fi.LastWriteTimeUtc.Ticks;
                            long lenBytes = fi.Length;

                            bool copiedBloom = false;
                            bool copiedText = false;
                            long oldBloomOffset = -1;
                            long oldTextOffset = -1;
                            int oldTextLen = 0;

                            if (!forceRebuild &&
                                oldMap.TryGetValue((relKey, side), out var old) &&
                                old.LastWriteUtcTicks == ticks &&
                                old.LengthBytes == lenBytes &&
                                old.BloomOffset >= 0)
                            {
                                copiedBloom = true;
                                oldBloomOffset = old.BloomOffset;
                            }

                            if (!forceRebuild &&
                                oldTextMap.TryGetValue((relKey, side), out var oldText) &&
                                oldText.LastWriteUtcTicks == ticks &&
                                oldText.LengthBytes == lenBytes &&
                                oldText.TextOffset >= 0 &&
                                oldText.TextLengthBytes >= 0)
                            {
                                copiedText = true;
                                oldTextOffset = oldText.TextOffset;
                                oldTextLen = oldText.TextLengthBytes;
                            }

                            // Always read XML and extract searchable text — needed for inverted index
                            string xml = File.ReadAllText(absPath, Utf8NoBom);
                            string searchable = MakeSearchableTextFromXml_Fast(xml, htmlDecode);

                            // Count CJK character and bigram frequencies for corpus freq index
                            var charFreqs = new Dictionary<char, int>(256);
                            var bigramFreqs = new Dictionary<string, int>(512);
                            char prevIndexable = '\0';
                            bool hasPrev = false;

                            for (int ci = 0; ci < searchable.Length; ci++)
                            {
                                char ch = searchable[ci];
                                if (!IsIndexableCjk(ch)) { hasPrev = false; continue; }

                                charFreqs[ch] = charFreqs.TryGetValue(ch, out var cf) ? cf + 1 : 1;

                                if (hasPrev)
                                {
                                    string bg = string.Concat(prevIndexable, ch);
                                    bigramFreqs[bg] = bigramFreqs.TryGetValue(bg, out var bf) ? bf + 1 : 1;
                                }

                                prevIndexable = ch;
                                hasPrev = true;
                            }

                            ulong[]? bits = null;
                            byte[]? textBytes = null;

                            if (!copiedBloom)
                            {
                                bits = new ulong[BloomUlongs];
                                BuildBloomFromText(bits, searchable);
                            }

                            if (!copiedText)
                            {
                                textBytes = string.IsNullOrEmpty(searchable)
                                    ? Array.Empty<byte>()
                                    : Utf8NoBom.GetBytes(searchable);
                            }

                            computed[i] = new ComputedEntry
                            {
                                RelKey = relKey,
                                Side = side,
                                Ticks = ticks,
                                LenBytes = lenBytes,
                                SearchableText = searchable,
                                Bits = bits,
                                TextBytes = textBytes,
                                CopiedBloom = copiedBloom,
                                CopiedText = copiedText,
                                OldBloomOffset = oldBloomOffset,
                                OldTextOffset = oldTextOffset,
                                OldTextLen = oldTextLen,
                                CharFreqs = charFreqs,
                                BigramFreqs = bigramFreqs
                            };
                        });

                        var phase1Ms = buildSw.ElapsedMilliseconds;
                        Dbg($"Index build Phase 1 (parallel compute) done in {phase1Ms} ms for {workItems.Count} items");

                        // ── Phase 2: Sequential write (maintains exact byte ordering) ──
                        for (int i = 0; i < computed.Length; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var entry = computed[i];

                            long entryBloomOffset = bloomOffset;
                            long entryTextOffset = textOffset;
                            int textLenBytes;

                            if (entry.CopiedBloom && oldFs != null)
                            {
                                CopyBloomBlock(oldFs, entry.OldBloomOffset, outFs);
                            }
                            else if (entry.Bits != null)
                            {
                                WriteBloom(outFs, entry.Bits);
                            }

                            if (entry.CopiedText && oldTextFs != null)
                            {
                                bool ok = CopyTextBlock(oldTextFs, entry.OldTextOffset, entry.OldTextLen, outTextFs);
                                textLenBytes = ok ? entry.OldTextLen : 0;
                            }
                            else
                            {
                                var tb = entry.TextBytes ?? Array.Empty<byte>();
                                if (tb.Length > 0)
                                    outTextFs.Write(tb, 0, tb.Length);
                                textLenBytes = tb.Length;
                            }

                            invertedDocs.Add((entry.RelKey, entry.SearchableText));

                            manifest.Entries.Add(new SearchIndexEntry
                            {
                                Id = id++,
                                RelPath = entry.RelKey,
                                Side = entry.Side,
                                LastWriteUtcTicks = entry.Ticks,
                                LengthBytes = entry.LenBytes,
                                BloomOffset = entryBloomOffset
                            });

                            textManifest.Entries.Add(new SearchTextEntry
                            {
                                Id = id - 1,
                                RelPath = entry.RelKey,
                                Side = entry.Side,
                                LastWriteUtcTicks = entry.Ticks,
                                LengthBytes = entry.LenBytes,
                                TextOffset = entryTextOffset,
                                TextLengthBytes = textLenBytes
                            });

                            bloomOffset += BloomBytes;
                            textOffset += textLenBytes;
                            done++;

                            if (done % 200 == 0 || done == total)
                                progress?.Report((done, total, forceRebuild ? "Rebuilding index..." : "Updating index..."));

                            // Merge per-file frequencies into corpus-wide accumulators (must happen before default clear)
                            if (entry.CharFreqs != null)
                            {
                                foreach (var kv in entry.CharFreqs)
                                {
                                    string key = kv.Key.ToString();
                                    corpusCharFreqs[key] = corpusCharFreqs.TryGetValue(key, out var v) ? v + kv.Value : kv.Value;
                                    corpusTotalChars += kv.Value;
                                }
                            }
                            if (entry.BigramFreqs != null)
                            {
                                foreach (var kv in entry.BigramFreqs)
                                    corpusBigramFreqs[kv.Key] = corpusBigramFreqs.TryGetValue(kv.Key, out var v) ? v + kv.Value : kv.Value;
                            }

                            // Release large buffers eagerly to reduce peak memory.
                            // Must clear array slot (struct copy), not local variable.
                            computed[i] = default;
                        }

                        buildSw.Stop();
                        Dbg($"Index build total: {buildSw.ElapsedMilliseconds} ms (Phase 1: {phase1Ms} ms, Phase 2: {buildSw.ElapsedMilliseconds - phase1Ms} ms)");

                        outFs.Flush(true);
                        outTextFs.Flush(true);
                    }
                }
                catch
                {
                    try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
                    try { if (File.Exists(tmpTextBin)) File.Delete(tmpTextBin); } catch { }
                    throw;
                }
                finally
                {
                    try { oldFs?.Dispose(); } catch { }
                    try { oldTextFs?.Dispose(); } catch { }
                }

                ReplaceFileAtomicWithRetry(tmpBin, finalBin);
                ReplaceFileAtomicWithRetry(tmpTextBin, finalTextBin);
                await SaveManifestAtomicAsync(root, manifest, ct);
                await SaveTextManifestAtomicAsync(root, textManifest, ct);

                // Build and save inverted index alongside bloom
                try
                {
                    var invertedIndex = new InvertedSearchIndex();
                    var sortedDocs = invertedDocs.OrderBy(d => d.relPath, StringComparer.OrdinalIgnoreCase).ToList();
                    Dbg($"Inverted index: building from {sortedDocs.Count} docs...");
                    invertedIndex.Build(sortedDocs.Select(d => (d.relPath, d.text)).ToList());
                    sortedDocs = null; // free text strings immediately
                    invertedDocs.Clear();
                    var invertedPath = Path.Combine(root, "search.inverted.bin");
                    await invertedIndex.SaveAsync(invertedPath, ct);
                    InvertedIndex = invertedIndex;
                    GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                    Dbg($"Inverted index built: {invertedIndex.TermCount} terms, {invertedIndex.DocCount} docs");
                }
                catch (Exception ex)
                {
                    Dbg($"Inverted index build FAILED: {ex.Message}\n{ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Inverted index FAILED: {ex.Message}\n{ex.StackTrace}");
                }

                // Phase C optional accelerator: compact-CJK bigram postings.
                // If this build fails, search still works via bloom + verify fallback.
                try
                {
                    var cjk2Manifest = BuildCjk2ManifestFromTextSidecar(root, manifest, textManifest, ct);
                    await SaveCjk2ManifestAtomicAsync(root, cjk2Manifest, ct);
                }
                catch (Exception ex)
                {
                    Dbg($"CJK2 postings build skipped: {ex.Message}");
                    try
                    {
                        var oldCjk2 = GetCjk2ManifestPath(root);
                        if (File.Exists(oldCjk2)) File.Delete(oldCjk2);
                    }
                    catch { }
                }

                // Build and save corpus frequency index
                try
                {
                    Dbg($"Corpus freq index: {corpusCharFreqs.Count} unique chars, {corpusBigramFreqs.Count} unique bigrams, {corpusTotalChars} total chars");

                    var freqManifest = new CorpusFreqManifest
                    {
                        Version = 1,
                        BuildGuid = CorpusFreqBuildGuid,
                        BuiltUtc = DateTime.UtcNow,
                        TotalCharacters = corpusTotalChars,
                        UniqueCharacters = corpusCharFreqs.Count,
                        UniqueBigrams = corpusBigramFreqs.Count
                    };

                    // Write manifest
                    var freqManifestFinal = Path.Combine(root, "search.corpusfreq.manifest.json");
                    var freqManifestTmp = freqManifestFinal + ".tmp";
                    var freqManifestJson = JsonSerializer.Serialize(freqManifest, JsonOpts);
                    await File.WriteAllTextAsync(freqManifestTmp, freqManifestJson, Utf8NoBom, ct);
                    ReplaceFileAtomicWithRetry(freqManifestTmp, freqManifestFinal);

                    // Write binary: [magic 4B][charCount 4B][bigramCount 4B][totalChars 8B]
                    //   char entries: [char 2B][freq 4B] x charCount
                    //   bigram entries: [char1 2B + char2 2B][freq 4B] x bigramCount
                    var freqBinFinal = Path.Combine(root, "search.corpusfreq.bin");
                    var freqBinTmp = freqBinFinal + ".tmp";
                    using (var fs = new FileStream(freqBinTmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var bw = new BinaryWriter(fs, Utf8NoBom, leaveOpen: false))
                    {
                        // Magic: "CF01"
                        bw.Write((byte)'C'); bw.Write((byte)'F'); bw.Write((byte)'0'); bw.Write((byte)'1');
                        bw.Write(corpusCharFreqs.Count);
                        bw.Write(corpusBigramFreqs.Count);
                        bw.Write(corpusTotalChars);

                        foreach (var kv in corpusCharFreqs)
                        {
                            bw.Write(kv.Key[0]); // single char as UTF-16
                            bw.Write(kv.Value);
                        }

                        foreach (var kv in corpusBigramFreqs)
                        {
                            bw.Write(kv.Key[0]); // first char
                            bw.Write(kv.Key[1]); // second char
                            bw.Write(kv.Value);
                        }
                    }
                    ReplaceFileAtomicWithRetry(freqBinTmp, freqBinFinal);

                    // Populate in-memory properties immediately
                    CorpusCharFreqs = corpusCharFreqs;
                    CorpusBigramFreqs = corpusBigramFreqs;
                    CorpusTotalChars = corpusTotalChars;

                    Dbg($"Corpus freq index saved: {new FileInfo(freqBinFinal).Length} bytes");
                }
                catch (Exception ex)
                {
                    Dbg($"Corpus frequency build FAILED: {ex.Message}");
                }

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
        public long CandidateMs { get; set; }
        public long VerifyMs { get; set; }
        public long TotalMs { get; set; }
    }

    private static void Dbg(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [SearchIndexService] {msg}";
        try { System.Diagnostics.Debug.WriteLine(line); } catch { }
        try { Console.WriteLine(line); } catch { }
    }

    public static List<SearchResultChild> BuildResultChildren(
        string relPath,
        IReadOnlyList<SearchHit> originalHits,
        IReadOnlyList<SearchHit> translatedHits)
    {
        var children = new List<SearchResultChild>(originalHits.Count + translatedHits.Count);

        for (int i = 0; i < originalHits.Count; i++)
        {
            var primaryHit = originalHits[i];
            var secondaryHit = i < translatedHits.Count ? translatedHits[i] : null;
            children.Add(new SearchResultChild
            {
                RelPath = relPath,
                Side = SearchSide.Original,
                Hit = primaryHit,
                PrimaryIsContextOnly = IsContextOnlyHit(primaryHit),
                SecondaryHit = secondaryHit,
                SecondaryIsContextOnly = secondaryHit != null && IsContextOnlyHit(secondaryHit)
            });
        }

        for (int i = 0; i < translatedHits.Count; i++)
        {
            var primaryHit = translatedHits[i];
            var secondaryHit = i < originalHits.Count ? originalHits[i] : null;
            children.Add(new SearchResultChild
            {
                RelPath = relPath,
                Side = SearchSide.Translated,
                Hit = primaryHit,
                PrimaryIsContextOnly = IsContextOnlyHit(primaryHit),
                SecondaryHit = secondaryHit,
                SecondaryIsContextOnly = secondaryHit != null && IsContextOnlyHit(secondaryHit)
            });
        }

        return children;
    }

    public static List<SearchResultChild> BuildAlignedDisplayChildrenFromIndexedUnits(
        string originalDir,
        string translatedDir,
        string relPath,
        string query,
        bool includeOriginal,
        bool includeTranslated,
        int contextWidth)
    {
        var indexed = TryLoadIndexedTranslationForDisplay(originalDir, translatedDir, relPath);
        if (indexed == null)
            return new List<SearchResultChild>();

        bool isCjkQuery = CjkMatchNormalizer.ContainsCjk(query);
        string effectiveQuery = isCjkQuery ? CjkMatchNormalizer.Normalize(query ?? string.Empty) : (query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(effectiveQuery))
            return new List<SearchResultChild>();

        var originalChildren = new List<SearchResultChild>();
        var translatedChildren = new List<SearchResultChild>();
        for (int i = 0; i < indexed.Units.Count; i++)
        {
            var unit = indexed.Units[i];
            string zh = unit.Zh ?? string.Empty;
            string en = unit.En ?? string.Empty;

            bool zhMatch = includeOriginal && TextContainsQuery(zh, effectiveQuery, isCjkQuery);
            bool enMatch = includeTranslated && TextContainsQuery(en, effectiveQuery, false);
            if (!zhMatch && !enMatch)
                continue;

            var zhCounterpart = BuildWidenedCounterpartSnippet(indexed.Units, i, SearchSide.Original, contextWidth);
            var enCounterpart = BuildWidenedCounterpartSnippet(indexed.Units, i, SearchSide.Translated, contextWidth);

            if (zhMatch)
            {
                originalChildren.Add(new SearchResultChild
                {
                    RelPath = relPath,
                    Side = SearchSide.Original,
                    Hit = BuildSnippetForDisplay(zh, query ?? string.Empty, contextWidth, isCjkQuery),
                    PrimaryIsContextOnly = false,
                    SecondaryHit = enCounterpart,
                    SecondaryIsContextOnly = true
                });
            }

            if (enMatch)
            {
                translatedChildren.Add(new SearchResultChild
                {
                    RelPath = relPath,
                    Side = SearchSide.Translated,
                    Hit = BuildSnippetForDisplay(en, query ?? string.Empty, contextWidth, false),
                    PrimaryIsContextOnly = false,
                    SecondaryHit = zhCounterpart,
                    SecondaryIsContextOnly = true
                });
            }
        }

        var children = new List<SearchResultChild>(originalChildren.Count + translatedChildren.Count);
        children.AddRange(originalChildren);
        children.AddRange(translatedChildren);
        return children;
    }

    private static IndexedTranslationDocument? TryLoadIndexedTranslationForDisplay(string originalDir, string translatedDir, string relPath)
    {
        if (string.IsNullOrWhiteSpace(originalDir) || string.IsNullOrWhiteSpace(relPath))
            return null;

        foreach (var candidateTranslatedDir in EnumerateTranslatedCounterpartDirs(originalDir, translatedDir, SearchSide.Original))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidateTranslatedDir))
                    continue;

                string origAbs = Path.Combine(originalDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                string tranAbs = Path.Combine(candidateTranslatedDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(origAbs) || !File.Exists(tranAbs))
                    continue;

                string originalXml = File.ReadAllText(origAbs, Utf8NoBom);
                string translatedXml = File.ReadAllText(tranAbs, Utf8NoBom);
                return new IndexedTranslationService().BuildIndex(originalXml, translatedXml);
            }
            catch
            {
            }
        }

        return null;
    }

    private static SearchHit BuildSnippetForDisplay(string text, string query, int contextWidth, bool isCjkQuery)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new SearchHit();

        string collapsed = CollapseWhitespace(text);
        if (collapsed.Length == 0)
            return new SearchHit();

        if (string.IsNullOrWhiteSpace(query))
            return BuildCounterpartSnippet(collapsed, contextWidth);

        if (isCjkQuery)
        {
            string normalizedQuery = CjkMatchNormalizer.Normalize(query);
            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                var normalized = CjkMatchNormalizer.NormalizeWithMap(collapsed);
                int idx2 = normalized.Normalized.IndexOf(normalizedQuery, StringComparison.Ordinal);
                if (idx2 >= 0)
                {
                    int start = CjkMatchNormalizer.RawIndexFromNormalizedPos(normalized, idx2);
                    int end = CjkMatchNormalizer.RawIndexFromNormalizedPos(normalized, idx2 + normalizedQuery.Length);
                    return BuildSnippetFromOffsets(collapsed, start, end, contextWidth);
                }
            }
        }
        else
        {
            int idx2 = collapsed.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (idx2 >= 0)
                return BuildSnippetFromOffsets(collapsed, idx2, idx2 + query.Length, contextWidth);
        }

        return BuildCounterpartSnippet(collapsed, contextWidth);
    }

    private static readonly HashSet<char> SentenceBreaks = new() { '\u3002', '\uFF01', '\uFF1F', '\n' }; // 。！？\n

    private static SearchHit BuildSnippetFromOffsets(string text, int matchStart, int matchEnd, int contextWidth)
    {
        int safeStart = Math.Clamp(matchStart, 0, text.Length);
        int safeEnd = Math.Clamp(matchEnd, safeStart, text.Length);
        int leftStart = Math.Max(0, safeStart - contextWidth);
        int rightEnd = Math.Min(text.Length, safeEnd + contextWidth);

        // Clamp left: don't cross sentence boundary into the window
        for (int i = safeStart - 1; i >= leftStart; i--)
        {
            if (SentenceBreaks.Contains(text[i])) { leftStart = i + 1; break; }
        }

        // Clamp right: stop at nearest sentence boundary
        for (int i = safeEnd; i < rightEnd; i++)
        {
            if (SentenceBreaks.Contains(text[i])) { rightEnd = i + 1; break; }
        }

        return new SearchHit
        {
            Index = safeStart,
            Left = text.Substring(leftStart, safeStart - leftStart),
            Match = text.Substring(safeStart, safeEnd - safeStart),
            Right = text.Substring(safeEnd, rightEnd - safeEnd)
        };
    }

    public static List<SearchHit> BuildCounterpartHitsFromIndexedUnits(
        IndexedTranslationDocument doc,
        string query,
        SearchSide primarySide,
        int neededCount,
        int contextWidth)
    {
        if (doc == null || neededCount <= 0)
            return new List<SearchHit>();

        bool isCjkQuery = CjkMatchNormalizer.ContainsCjk(query);
        string effectiveQuery = isCjkQuery ? CjkMatchNormalizer.Normalize(query ?? string.Empty) : (query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(effectiveQuery))
            return new List<SearchHit>();

        var hits = new List<SearchHit>(neededCount);
        foreach (var unit in doc.Units)
        {
            string primaryText = primarySide == SearchSide.Original ? unit.Zh ?? string.Empty : unit.En ?? string.Empty;
            if (!TextContainsQuery(primaryText, effectiveQuery, isCjkQuery))
                continue;

            string counterpartText = primarySide == SearchSide.Original ? unit.En ?? string.Empty : unit.Zh ?? string.Empty;
            if (string.IsNullOrWhiteSpace(counterpartText))
                continue;

            hits.Add(BuildCounterpartSnippet(counterpartText, contextWidth));
            if (hits.Count >= neededCount)
                break;
        }

        return hits;
    }

    private static SearchHit? BuildWidenedCounterpartSnippet(IReadOnlyList<TranslationUnit> units, int centerIndex, SearchSide counterpartSide, int contextWidth)
    {
        if (units == null || centerIndex < 0 || centerIndex >= units.Count)
            return null;

        string centerText = GetCounterpartUnitText(units[centerIndex], counterpartSide);
        if (string.IsNullOrWhiteSpace(centerText))
            return null;

        int budget = Math.Max(60, contextWidth * 2);
        var pieces = new List<string>();
        AddVisiblePiece(pieces, centerText, addToFront: false);
        int visibleLength = string.Join(" ", pieces).Length;

        int left = centerIndex - 1;
        int right = centerIndex + 1;
        while (visibleLength < budget)
        {
            bool added = false;

            if (left >= 0 && CanExtendCounterpartWindow(units, left, left + 1))
            {
                int beforeCount = pieces.Count;
                AddVisiblePiece(pieces, GetCounterpartUnitText(units[left], counterpartSide), addToFront: true);
                if (pieces.Count != beforeCount)
                {
                    visibleLength = string.Join(" ", pieces).Length;
                    added = true;
                }
                left--;
            }
            else
            {
                left = -1;
            }

            if (visibleLength >= budget)
                break;

            if (right < units.Count && CanExtendCounterpartWindow(units, right - 1, right))
            {
                int beforeCount = pieces.Count;
                AddVisiblePiece(pieces, GetCounterpartUnitText(units[right], counterpartSide), addToFront: false);
                if (pieces.Count != beforeCount)
                {
                    visibleLength = string.Join(" ", pieces).Length;
                    added = true;
                }
                right++;
            }
            else
            {
                right = units.Count;
            }

            if (!added)
                break;
        }

        if (pieces.Count == 0)
            return null;

        return BuildCounterpartSnippet(string.Join(" ", pieces), contextWidth, budget);
    }

    private static string GetCounterpartUnitText(TranslationUnit unit, SearchSide counterpartSide)
        => counterpartSide == SearchSide.Original ? unit.Zh ?? string.Empty : unit.En ?? string.Empty;

    private static void AddVisiblePiece(List<string> pieces, string text, bool addToFront)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (addToFront)
            pieces.Insert(0, text.Trim());
        else
            pieces.Add(text.Trim());
    }

    private static bool CanExtendCounterpartWindow(IReadOnlyList<TranslationUnit> units, int leftIndex, int rightIndex)
    {
        if (leftIndex < 0 || rightIndex < 0 || leftIndex >= units.Count || rightIndex >= units.Count)
            return false;

        var left = units[leftIndex];
        var right = units[rightIndex];
        return string.Equals(left.ElementStableKey, right.ElementStableKey, StringComparison.Ordinal)
            && left.Kind == right.Kind
            && right.LineNumber == left.LineNumber + 1;
    }
    private static bool TextContainsQuery(string text, string effectiveQuery, bool isCjkQuery)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (isCjkQuery)
            return CjkMatchNormalizer.Normalize(text).Contains(effectiveQuery, StringComparison.Ordinal);

        return text.Contains(effectiveQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static SearchHit BuildCounterpartSnippet(string text, int contextWidth, int? maxLenOverride = null)
    {
        string collapsed = CollapseWhitespace(text);
        if (collapsed.Length == 0)
            return new SearchHit();

        int maxLen = maxLenOverride ?? Math.Max(20, contextWidth * 2);
        string snippet = collapsed.Length > maxLen ? collapsed[..maxLen] + "..." : collapsed;
        return new SearchHit { Left = snippet, Match = string.Empty, Right = string.Empty };
    }

    private static bool IsContextOnlyHit(SearchHit? hit)
    {
        return hit != null
            && string.IsNullOrEmpty(hit.Match)
            && !string.IsNullOrEmpty(hit.Left)
            && string.IsNullOrEmpty(hit.Right);
    }

    private static string CollapseWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        bool prevWs = false;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevWs)
                    sb.Append(' ');
                prevWs = true;
            }
            else
            {
                sb.Append(ch);
                prevWs = false;
            }
        }

        return sb.ToString().Trim();
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
    IReadOnlyList<string>? additionalOriginalDirs = null,
    IReadOnlyList<string>? additionalTranslatedDirs = null,
    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        query = (query ?? "").Trim();
        if (query.Length == 0)
            yield break;

        // CJK queries: strip spaces and CJK punctuation so phrases split across <lb> tag boundaries
        // (and across modern editorial punctuation) are found.  CBETA punctuation is not original.
        // English text relies on natural spaces from XML whitespace and keeps them.
        string effectiveQuery = CjkMatchNormalizer.ContainsCjk(query)
            ? CjkMatchNormalizer.Normalize(query)
            : query;
        if (effectiveQuery.Length == 0)
            yield break;

        var entries = manifest.Entries ?? new List<SearchIndexEntry>();

        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        Dbg($"SearchAllAsync START q='{query}' effectiveQ='{effectiveQuery}' len={effectiveQuery.Length} includeO={includeOriginal} includeT={includeTranslated} entries={entries.Count}");

        bool useBloom = effectiveQuery.Length >= 2;
        var grams = MakeQueryGrams(effectiveQuery);
        HashSet<int>? cjk2PrefilterIds = null;
        bool useCjk2Prefilter =
            Options.EnableCjkBigramPrefilter &&
            CjkMatchNormalizer.ContainsCjk(query) &&
            effectiveQuery.Length >= Math.Max(2, Options.CjkBigramPrefilterMinQueryLength) &&
            effectiveQuery.Length <= Math.Max(2, Options.CjkBigramPrefilterMaxQueryLength);

        if (useCjk2Prefilter)
        {
            try
            {
                var cjk2 = await TryLoadCjk2ManifestAsync(root);
                if (cjk2 != null &&
                    cjk2.EntryCount == entries.Count &&
                    cjk2.Postings != null)
                {
                    var postingMap = new Dictionary<string, List<int>>(StringComparer.Ordinal);
                    foreach (var p in cjk2.Postings)
                    {
                        if (p == null || string.IsNullOrEmpty(p.Gram) || p.EntryIds == null) continue;
                        postingMap[p.Gram] = p.EntryIds;
                    }

                    var qBigrams = MakeCompactQueryBigrams(effectiveQuery);
                    if (qBigrams.Count > 0)
                    {
                        HashSet<int>? intersect = null;
                        bool impossible = false;

                        foreach (var g in qBigrams)
                        {
                            if (!postingMap.TryGetValue(g, out var ids) || ids.Count == 0)
                            {
                                impossible = true;
                                break;
                            }

                            if (intersect == null) intersect = new HashSet<int>(ids);
                            else intersect.IntersectWith(ids);

                            if (intersect.Count == 0)
                            {
                                impossible = true;
                                break;
                            }
                        }

                        cjk2PrefilterIds = impossible ? new HashSet<int>() : (intersect ?? new HashSet<int>());
                        Dbg($"CJK2 prefilter {(cjk2PrefilterIds.Count == 0 ? "EMPTY" : "ACTIVE")} qBigrams={qBigrams.Count} passIds={cjk2PrefilterIds.Count}");

                        int entryCount = entries.Count;
                        if (cjk2PrefilterIds.Count > 0 && entryCount > 0)
                        {
                            double passRatio = (double)cjk2PrefilterIds.Count / entryCount;
                            if (passRatio > Math.Clamp(Options.CjkBigramPrefilterMaxPassRatio, 0.01, 1.0))
                            {
                                Dbg($"CJK2 prefilter disabled (passRatio={passRatio:0.###} too broad).");
                                cjk2PrefilterIds = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Dbg($"CJK2 prefilter unavailable: {ex.Message}");
            }
        }

        bool sideAllowed(SearchSide s)
            => (s == SearchSide.Original && includeOriginal) ||
               (s == SearchSide.Translated && includeTranslated);

        progress?.Report(new SearchProgress { Phase = "Building candidates..." });

        var candidates = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var swCandidate = System.Diagnostics.Stopwatch.StartNew();
        bool usedInvertedIndex = false;

        // Fast path: inverted index (0% false positives, sub-millisecond)
        if (InvertedIndex?.IsLoaded == true && effectiveQuery.Length >= 2)
        {
            var invertedHits = InvertedIndex.Search(effectiveQuery);
            if (invertedHits != null && invertedHits.Length > 0)
            {
                int sideMask = (includeOriginal ? 1 : 0) | (includeTranslated ? 2 : 0);
                foreach (var docId in invertedHits)
                {
                    var relPath = InvertedIndex.GetRelPath(docId);
                    if (relPath == null) continue;
                    if (relPathFilter != null && !relPathFilter(relPath)) continue;

                    // The inverted index doesn't track sides per doc — apply the requested side mask.
                    // The verification loop will check each side individually against actual file content.
                    candidates.AddOrUpdate(relPath, _ => sideMask, (_, v) => v | sideMask);
                }

                usedInvertedIndex = true;
                Dbg($"Candidate phase inverted index DONE hits={invertedHits.Length} candidateKeys={candidates.Count}");
            }
        }

        // Fallback: bloom filter scan (when inverted index unavailable or query too short)
        if (!usedInvertedIndex)
        {
            await _indexIoGate.WaitAsync(ct);
            try
            {
                Dbg($"Candidate phase START useBloom={useBloom}");

                if (!useBloom)
                {
                    int seen = 0;
                    foreach (var e in entries)
                    {
                        ct.ThrowIfCancellationRequested();

                        if (!sideAllowed(e.Side)) continue;
                        if (relPathFilter != null && !relPathFilter(e.RelPath)) continue;

                        candidates.AddOrUpdate(
                            e.RelPath,
                            _ => e.Side == SearchSide.Original ? 1 : 2,
                            (_, v) => v | (e.Side == SearchSide.Original ? 1 : 2));

                        seen++;
                        if (seen % 1000 == 0)
                            Dbg($"Candidate phase (no bloom): scanned={seen}, candidateKeys={candidates.Count}");
                    }

                    Dbg($"Candidate phase (no bloom) DONE candidateKeys={candidates.Count}");
                }
                else
                {
                    string binPath = GetBinPath(root);
                    var binFull = Path.GetFullPath(binPath);

                    if (!File.Exists(binFull))
                    {
                        Dbg($"Candidate phase bloom: bin missing '{binFull}'");
                    }
                    else if (cjk2PrefilterIds != null && cjk2PrefilterIds.Count == 0)
                    {
                        Dbg("Candidate phase bloom skipped by empty CJK2 prefilter set.");
                    }
                    else
                    {
                        var swBloom = System.Diagnostics.Stopwatch.StartNew();

                        // IMPORTANT FIX:
                        // - Do NOT share one MemoryMappedViewAccessor across threads.
                        // - Create one MMF for this search, and a THREAD-LOCAL accessor per worker.
                        using var mmf = MemoryMappedFile.CreateFromFile(binFull, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

                        int scannedEntries = 0;
                        int bloomPass = 0;

                        var po = new ParallelOptions
                        {
                            CancellationToken = ct,
                            MaxDegreeOfParallelism = Math.Max(1, Options.MaxBloomDegreeOfParallelism)
                        };

                        Dbg($"Candidate phase bloom: Parallel.ForEach START dop={po.MaxDegreeOfParallelism}, grams={grams.Count}");

                        Parallel.ForEach(
                            entries,
                            po,
                            localInit: () =>
                            {
                                // Thread-local state: own accessor + scratch buffers
                                var localAccessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                                return (accessor: localAccessor, arr: new byte[BloomBytes], bits: new ulong[BloomUlongs]);
                            },
                            body: (e, _, local) =>
                            {
                                po.CancellationToken.ThrowIfCancellationRequested();

                                Interlocked.Increment(ref scannedEntries);

                                if (!sideAllowed(e.Side)) return local;
                                if (e.LastWriteUtcTicks == 0 || e.LengthBytes == 0) return local;
                                if (relPathFilter != null && !relPathFilter(e.RelPath)) return local;
                                if (cjk2PrefilterIds != null && !cjk2PrefilterIds.Contains(e.Id)) return local;

                                try
                                {
                                    local.accessor.ReadArray(e.BloomOffset, local.arr, 0, BloomBytes);
                                }
                                catch (Exception ex)
                                {
                                    Dbg($"Bloom ReadArray EXCEPTION rel={e.RelPath} side={e.Side} offset={e.BloomOffset}: {ex}");
                                    throw;
                                }

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
                                    if (start + n > effectiveQuery.Length) continue;

                                    if (!BloomMightContain(local.bits, effectiveQuery.AsSpan(start, n)))
                                    {
                                        ok = false;
                                        break;
                                    }
                                }

                                if (!ok) return local;

                                int mask = (e.Side == SearchSide.Original) ? 1 : 2;
                                candidates.AddOrUpdate(e.RelPath, _ => mask, (_, v) => v | mask);
                                Interlocked.Increment(ref bloomPass);

                                if (bloomPass % 500 == 0)
                                    Dbg($"Candidate phase bloom progress: scanned={Volatile.Read(ref scannedEntries)}, bloomPass={bloomPass}, candidateKeys={candidates.Count}");

                                return local;
                            },
                            localFinally: local =>
                            {
                                try { local.accessor.Dispose(); } catch { }
                            }
                        );

                        swBloom.Stop();
                        Dbg($"Candidate phase bloom DONE in {swBloom.ElapsedMilliseconds}ms scanned={scannedEntries} bloomPass={bloomPass} candidateKeys={candidates.Count}");
                    }
                }
            }
            finally
            {
                _indexIoGate.Release();
            }
        }
        swCandidate.Stop();

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

        Dbg($"Verify phase PREP candidateKeys={candidateList.Count} docsToVerify={totalDocsToVerify}");

        progress?.Report(new SearchProgress
        {
            Phase = usedInvertedIndex ? "Candidate filtering done (inverted index)" : useBloom ? "Candidate filtering done" : "Brute candidates (1-char search)",
            Candidates = totalDocsToVerify,
            TotalDocsToVerify = totalDocsToVerify,
            CandidateMs = swCandidate.ElapsedMilliseconds,
            TotalMs = swTotal.ElapsedMilliseconds
        });

        var outGroups = new ConcurrentBag<SearchResultGroup>();

        int verifiedDocs = 0;
        int totalHits = 0;
        int emittedGroups = 0;
        var entryMap = entries.ToDictionary(e => (e.RelPath, e.Side), e => e, new RelSideComparer());
        var textEntryMap = new Dictionary<(string rel, SearchSide side), SearchTextEntry>(new RelSideComparer());

        try
        {
            var textManifest = await TryLoadTextManifestAsync(root);
            if (textManifest?.Entries != null)
            {
                foreach (var e in textManifest.Entries)
                    textEntryMap[(e.RelPath, e.Side)] = e;
            }
        }
        catch
        {
            // Sidecar is optional. Fallback path keeps search correctness.
        }

        var verifyPo = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Max(1, Options.MaxVerifyDegreeOfParallelism)
        };

        var outChannel = System.Threading.Channels.Channel.CreateUnbounded<SearchResultGroup>(
            new System.Threading.Channels.UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        var swVerify = System.Diagnostics.Stopwatch.StartNew();

        var verifyTask = Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(candidateList, verifyPo, relKey =>
                {
                    ct.ThrowIfCancellationRequested();

                    int mask = candidates[relKey];

                    var meta = fileMeta(relKey);
                    var tooltip = string.IsNullOrWhiteSpace(meta.tooltip) ? relKey : meta.tooltip;
                    // Extract Chinese title from tooltip (format: "English\nChinese")
                    var zhTitle = "";
                    var nlIdx = tooltip.IndexOf('\n');
                    if (nlIdx >= 0 && nlIdx < tooltip.Length - 1)
                        zhTitle = tooltip[(nlIdx + 1)..];

                    var group = new SearchResultGroup
                    {
                        RelPath = relKey,
                        DisplayName = string.IsNullOrWhiteSpace(meta.display) ? relKey : meta.display,
                        Tooltip = tooltip,
                        ChineseTitle = zhTitle,
                        Status = meta.status
                    };

                    int hitsO = 0;
                    int hitsT = 0;
                    var originalHits = new List<SearchHit>();
                    var translatedHits = new List<SearchHit>();

                    if ((mask & 1) != 0)
                    {
                        string abs = ResolveAbsPath(originalDir, additionalOriginalDirs, relKey);
                        entryMap.TryGetValue((relKey, SearchSide.Original), out var metaOriginal);
                        textEntryMap.TryGetValue((relKey, SearchSide.Original), out var textOriginal);
                        originalHits = VerifyFileAllHits(
                            root,
                            relKey,
                            SearchSide.Original,
                            abs,
                            metaOriginal?.LastWriteUtcTicks ?? 0,
                            metaOriginal?.LengthBytes ?? 0,
                            textOriginal,
                            effectiveQuery,
                            contextWidth,
                            htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent);
                        Interlocked.Increment(ref verifiedDocs);
                        hitsO = originalHits.Count;
                        Interlocked.Add(ref totalHits, hitsO);
                    }

                    if ((mask & 2) != 0)
                    {
                        string abs = ResolveAbsPath(translatedDir, additionalTranslatedDirs, relKey);
                        entryMap.TryGetValue((relKey, SearchSide.Translated), out var metaTranslated);
                        textEntryMap.TryGetValue((relKey, SearchSide.Translated), out var textTranslated);
                        translatedHits = VerifyFileAllHits(
                            root,
                            relKey,
                            SearchSide.Translated,
                            abs,
                            metaTranslated?.LastWriteUtcTicks ?? 0,
                            metaTranslated?.LengthBytes ?? 0,
                            textTranslated,
                            effectiveQuery,
                            contextWidth,
                            htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent);
                        Interlocked.Increment(ref verifiedDocs);
                        hitsT = translatedHits.Count;
                        Interlocked.Add(ref totalHits, hitsT);
                    }

                    group.HitsOriginal = hitsO;
                    group.HitsTranslated = hitsT;
                    group.Children.AddRange(BuildResultChildren(relKey, originalHits, translatedHits));

                    if (group.Children.Count > 0)
                    {
                        Interlocked.Increment(ref emittedGroups);
                        outChannel.Writer.TryWrite(group);
                    }

                    int v = Volatile.Read(ref verifiedDocs);
                    if (v <= 10 || v % 10 == 0)
                    {
                        int groupsNow = Volatile.Read(ref emittedGroups);
                        int hitsNow = Volatile.Read(ref totalHits);
                        Dbg($"Verify phase progress verified={v}/{totalDocsToVerify} groups={groupsNow} hits={hitsNow}");

                        progress?.Report(new SearchProgress
                        {
                            Phase = "Searching...",
                            Candidates = totalDocsToVerify,
                            VerifiedDocs = v,
                            TotalDocsToVerify = totalDocsToVerify,
                            Groups = groupsNow,
                            TotalHits = hitsNow,
                            CandidateMs = swCandidate.ElapsedMilliseconds,
                            VerifyMs = swVerify.ElapsedMilliseconds,
                            TotalMs = swTotal.ElapsedMilliseconds
                        });
                    }
                });

                swVerify.Stop();
                Dbg($"Verify phase DONE in {swVerify.ElapsedMilliseconds}ms verified={verifiedDocs}/{totalDocsToVerify} groups={emittedGroups} hits={totalHits}");

                progress?.Report(new SearchProgress
                {
                    Phase = "Done",
                    Candidates = totalDocsToVerify,
                    VerifiedDocs = verifiedDocs,
                    TotalDocsToVerify = totalDocsToVerify,
                    Groups = emittedGroups,
                    TotalHits = totalHits,
                    CandidateMs = swCandidate.ElapsedMilliseconds,
                    VerifyMs = swVerify.ElapsedMilliseconds,
                    TotalMs = swTotal.ElapsedMilliseconds
                });

                outChannel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                outChannel.Writer.TryComplete(ex);
            }
        }, ct);

        await foreach (var g in outChannel.Reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return g;
            await Task.Yield();
        }

        await verifyTask;

        swTotal.Stop();
        Dbg($"SearchAllAsync END total={swTotal.ElapsedMilliseconds}ms");

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

    private static List<SearchHit> MergeDisplayHits(IReadOnlyList<SearchHit> existingHits, IReadOnlyList<SearchHit> counterpartHits, int targetCount)
    {
        if (targetCount <= 0)
            return new List<SearchHit>();

        var merged = new List<SearchHit>(targetCount);
        foreach (var hit in existingHits)
            merged.Add(hit);

        for (int i = merged.Count; i < targetCount && i < counterpartHits.Count; i++)
            merged.Add(counterpartHits[i]);

        return merged;
    }
    internal static List<SearchHit> TryBuildCounterpartHitsForDisplay(
        string originalDir,
        string translatedDir,
        string relKey,
        string effectiveQuery,
        SearchSide primarySide,
        int neededCount,
        int contextWidth)
    {
        if (string.IsNullOrWhiteSpace(originalDir))
            return new List<SearchHit>();

        foreach (var candidateTranslatedDir in EnumerateTranslatedCounterpartDirs(originalDir, translatedDir, primarySide))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(candidateTranslatedDir))
                    continue;

                string origAbs = Path.Combine(originalDir, relKey.Replace('/', Path.DirectorySeparatorChar));
                string tranAbs = Path.Combine(candidateTranslatedDir, relKey.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(origAbs) || !File.Exists(tranAbs))
                    continue;

                string originalXml = File.ReadAllText(origAbs, Utf8NoBom);
                string translatedXml = File.ReadAllText(tranAbs, Utf8NoBom);
                var indexed = new IndexedTranslationService().BuildIndex(originalXml, translatedXml);
                var hits = BuildCounterpartHitsFromIndexedUnits(indexed, effectiveQuery, primarySide, neededCount, contextWidth);
                if (hits.Count > 0)
                    return hits;
            }
            catch
            {
                // Try the next translated candidate.
            }
        }

        return new List<SearchHit>();
    }

    internal static IEnumerable<string> EnumerateTranslatedCounterpartDirs(string originalDir, string translatedDir, SearchSide primarySide)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(translatedDir))
        {
            string fullTranslated = Path.GetFullPath(translatedDir);
            if (seen.Add(fullTranslated))
                yield return fullTranslated;
        }

        if (primarySide != SearchSide.Original)
            yield break;

        // Fallback: when the active translatedDir is a personal dir (e.g. community/translations/user),
        // also check the canonical xml-p5t sibling of the originals dir for counterpart files.
        var originalParent = Directory.GetParent(Path.GetFullPath(originalDir))?.FullName;
        if (string.IsNullOrWhiteSpace(originalParent))
            yield break;

        string canonicalTranslated = Path.Combine(originalParent, "xml-p5t");
        if (Directory.Exists(canonicalTranslated))
        {
            string fullCanonicalTranslated = Path.GetFullPath(canonicalTranslated);
            if (seen.Add(fullCanonicalTranslated))
                yield return fullCanonicalTranslated;
        }
    }
    private sealed class VerifyTextCacheKeyComparer : IEqualityComparer<(string rel, SearchSide side, long ticks, long len)>
    {
        public bool Equals((string rel, SearchSide side, long ticks, long len) x, (string rel, SearchSide side, long ticks, long len) y)
            => string.Equals(x.rel, y.rel, StringComparison.OrdinalIgnoreCase)
               && x.side == y.side
               && x.ticks == y.ticks
               && x.len == y.len;

        public int GetHashCode((string rel, SearchSide side, long ticks, long len) obj)
        {
            unchecked
            {
                int h = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.rel ?? "");
                h = (h * 397) ^ obj.side.GetHashCode();
                h = (h * 397) ^ obj.ticks.GetHashCode();
                h = (h * 397) ^ obj.len.GetHashCode();
                return h;
            }
        }
    }

    private List<SearchHit> VerifyFileAllHits(
        string root,
        string relPath,
        SearchSide side,
        string absPath,
        long lastWriteUtcTicks,
        long lengthBytes,
        SearchTextEntry? textEntry,
        string query,
        int contextWidth,
        bool htmlDecodeIfAmpersandPresent)
    {
        var hits = new List<SearchHit>();
        if (!File.Exists(absPath)) return hits;

        if (lastWriteUtcTicks <= 0 || lengthBytes <= 0)
        {
            try
            {
                var fi = new FileInfo(absPath);
                lastWriteUtcTicks = fi.LastWriteTimeUtc.Ticks;
                lengthBytes = fi.Length;
            }
            catch
            {
                return hits;
            }
        }

        string text = GetSearchableTextCached(
            root,
            relPath,
            side,
            lastWriteUtcTicks,
            lengthBytes,
            textEntry,
            absPath,
            htmlDecodeIfAmpersandPresent);

        if (string.IsNullOrEmpty(text)) return hits;

        // CJK queries search in a compact (spaces + CJK-punct stripped) version of the text so
        // that phrases split across <lb> boundaries and across editorial punctuation are found.
        // KWIC (left/match/right) is then extracted from the *original* text via position mapping
        // so that navigation and highlighting in the reader work correctly.
        // English queries use the text as-is (spaces are meaningful word separators).
        bool isCjk = CjkMatchNormalizer.ContainsCjk(query);
        var normalizedText = isCjk ? CjkMatchNormalizer.NormalizeWithMap(text) : null;
        string searchText = isCjk ? normalizedText!.Normalized : text;

        int idx = 0;
        while (true)
        {
            idx = searchText.IndexOf(query, idx, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            string left, match, right;
            int hitIndex;

            if (isCjk)
            {
                // Map compact positions back to the original text so KWIC retains original
                // spacing/punctuation and can be matched against the rendered document.
                int origStart = CjkMatchNormalizer.RawIndexFromNormalizedPos(normalizedText!, idx);
                int origEnd   = CjkMatchNormalizer.RawIndexFromNormalizedPos(normalizedText!, idx + query.Length);
                int leftStart = Math.Max(0, origStart - contextWidth);
                int rightEnd  = Math.Min(text.Length, origEnd + contextWidth);
                left     = text.Substring(leftStart, origStart - leftStart);
                match    = text.Substring(origStart, origEnd - origStart);
                right    = text.Substring(origEnd, rightEnd - origEnd);
                hitIndex = origStart;
            }
            else
            {
                int start    = idx;
                int end      = idx + query.Length;
                int leftStart = Math.Max(0, start - contextWidth);
                int rightEnd  = Math.Min(text.Length, end + contextWidth);
                left     = text.Substring(leftStart, start - leftStart);
                match    = text.Substring(start, query.Length);
                right    = text.Substring(end, rightEnd - end);
                hitIndex = start;
            }

            hits.Add(new SearchHit
            {
                Index = hitIndex,
                Left = left,
                Match = match,
                Right = right
            });

            idx = Math.Max(idx + query.Length, idx + 1);
        }

        return hits;
    }
}



















