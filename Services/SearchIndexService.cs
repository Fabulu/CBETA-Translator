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

        // PR2 (skip-verify hybrid): for 2-char pure-CJK queries, the bigram inverted index
        // already proves contiguous adjacency, so VerifyFileAllHits is redundant for hit-count
        // accuracy. We still want snippets for the *first N candidates* (sorted by likely-hit-count
        // proxy = entry.LengthBytes desc), so verify is run for top N and skipped for the long tail.
        // Set to 0 to disable the hybrid (every candidate verified, original behaviour).
        public int SkipVerifySnippetTopN { get; set; } = 20;

        // Instant search (v4 tf). When true, any query whose bigrams resolve through the
        // inverted index RANKS candidates by index tf (highest-frequency docs verified
        // first). The skip-verify hybrid (verify + snippet only the top-N; the long tail
        // shows a tf count + loads snippets on demand) is engaged ONLY for single-bigram
        // (2-char CJK) queries, where the inverted index proves the query is contiguous so
        // the exact tf equals the match count. Multi-bigram phrases are still tf-ranked but
        // every candidate is VERIFIED — the index proves bigram co-occurrence, not phrase
        // adjacency, so emitting an unverified tail would surface scattered-bigram false
        // positives. When false, behaviour is unchanged from v6: only 2-char-CJK queries
        // use the hybrid, everything else eagerly verifies. Default FALSE at the service
        // layer so existing service-level tests keep their eager semantics; the app turns
        // it ON via AppConfig.InstantSearch (default true), pushed onto this option by
        // SearchTabViewModel.
        public bool InstantSearch { get; set; } = false;

        // If you truly need entity-decoding for search, keep this true.
        // For CBETA bodies it's often unnecessary; turning it off is faster.
        public bool HtmlDecodeIfAmpersandPresent { get; set; } = true;
    }

    public SearchIndexServiceOptions Options { get; } = new();

    // FL1 (frozen/live index split, design §2.4/§4.3): the inverted handle and the
    // corpus-frequency partial no longer live in standalone service fields — they live
    // in the <see cref="SearchFamily"/> instances held by <see cref="_families"/>.
    //
    // FL2 (design §2.3 consumer #5, "additive decomposition"): the corpus-global char/bigram
    // frequencies and total-char count are ADDITIVE across families —
    //   Freq_global[x] = Σ_family Freq_family[x],  TotalChars_global = Σ_family TotalChars_family
    // — so these getters no longer read a single family's partial; they expose an in-memory
    // additive fold over EVERY family's partial, computed once per (re)load and cached
    // (see <see cref="EnsureMergedCorpusFreqs"/>). With the sole combined family present
    // today the fold collapses to that family's partial by the SAME reference (the one-loaded
    // fast path in <see cref="MergeCorpusFreqPartials"/>), so ComputeCooccurrences and every
    // consumer at :373-527 observe byte-identical values to the pre-FL2 code. FL4 turns the
    // inverted getter's consumers into a disjoint-union merge.

    /// <summary>Inverted bigram index built alongside bloom filters. Null until first build/load.</summary>
    public InvertedSearchIndex? InvertedIndex => Combined.Inverted;

    /// <summary>
    /// Corpus-wide CJK character frequencies (key = single char as string), summed across
    /// all index families (FL2 additive fold). Null until at least one family is loaded/built.
    /// </summary>
    public IReadOnlyDictionary<string, int>? CorpusCharFreqs
    {
        get { EnsureMergedCorpusFreqs(); return _mergedCharFreqs; }
    }

    /// <summary>
    /// Corpus-wide CJK bigram frequencies (key = 2-char string), summed across all index
    /// families (FL2 additive fold). Null until at least one family is loaded/built.
    /// </summary>
    public IReadOnlyDictionary<string, int>? CorpusBigramFreqs
    {
        get { EnsureMergedCorpusFreqs(); return _mergedBigramFreqs; }
    }

    /// <summary>Total CJK characters counted across the entire corpus (FL2 additive fold over all families).</summary>
    public long CorpusTotalChars
    {
        get { EnsureMergedCorpusFreqs(); return _mergedTotalChars; }
    }

    /// <summary>True when corpus frequency data has been loaded or built.</summary>
    public bool HasCorpusFrequencies => CorpusCharFreqs != null;

    // FL2: cached additive fold of every family's corpusfreq partial. Rebuilt lazily on the
    // first getter access after any family's partial changes; invalidated by
    // InvalidateMergedCorpusFreqs() at each family-partial assignment site and by
    // InvalidateIndexCaches(). Guarded by its own lock (no nesting with _indexCacheLock, so
    // no lock-ordering hazard). Recompute cost is a single dictionary sum over ~32K chars +
    // ~65K bigrams (single-digit ms), paid once per (re)load.
    private readonly object _corpusFreqMergeLock = new();
    private IReadOnlyDictionary<string, int>? _mergedCharFreqs;
    private IReadOnlyDictionary<string, int>? _mergedBigramFreqs;
    private long _mergedTotalChars;
    private bool _mergedCorpusFreqsValid;

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

    // Cached manifest + mmap (big real-world speed win for repeated searches).
    // FL1 (frozen/live split, design §2.4/§4.3): the per-family cache slots + inverted
    // handle + corpusfreq partial that used to be single service fields now live in
    // SearchFamily instances held by _families. Today the list has exactly ONE member —
    // the combined `search.*` family (guid `search-v8-full-df`) — so every per-family
    // accessor resolves to Combined and output is byte-identical to the pre-FL1 code.
    // FL4 grows _families to {origin, overlay}. The lock still guards all cache-slot access.
    private readonly object _indexCacheLock = new();

    // PR2 (skip-verify hybrid) test-observable counters. Updated atomically at end of
    // each SearchAllAsync verify phase. Internal so ReadZen.Tests can assert on them.
    private int _lastSearchSkippedVerifyGroups;
    private int _lastSearchVerifiedGroups;
    internal int LastSearchSkippedVerifyGroups => Volatile.Read(ref _lastSearchSkippedVerifyGroups);
    internal int LastSearchVerifiedGroups => Volatile.Read(ref _lastSearchVerifiedGroups);

    // PR B (content-hash cache) — guards the opportunistic backfill write in IsStaleAsync
    // against concurrent callers. 0 = idle, 1 = a backfill is in flight. CompareExchange
    // ensures only one writer at a time; the second caller silently skips the backfill
    // (the cache will still get populated on a subsequent call). Test-observable count
    // of how many backfills actually fired this process — used in the concurrency test.
    private int _contentHashBackfillFlag;
    private int _contentHashBackfillCount;
    internal int LastContentHashBackfillCount => Volatile.Read(ref _contentHashBackfillCount);

    // INC-2A test-observable build counters. _lastBuildXmlReadCount is reset at the start
    // of every BuildOrUpdateCoreAsync run and incremented once per XML file actually read
    // in Phase 1 — the incremental skip-read tests assert it equals the changed/added
    // entry count (O(delta) XML reads). _lastBuildFallbackCount is reset per public
    // BuildOrUpdateAsync call and incremented when the incremental attempt failed and the
    // build was retried as a full rebuild inside the same gate acquisition.
    private int _lastBuildXmlReadCount;
    private int _lastBuildFallbackCount;
    internal int LastBuildXmlReadCount => Volatile.Read(ref _lastBuildXmlReadCount);
    internal int LastBuildFallbackCount => Volatile.Read(ref _lastBuildFallbackCount);

    // PERF (E) test-observable: 1 when the last public BuildOrUpdateAsync call's incremental
    // attempt bailed on the >20% delta guard (IncrementalFullRebuildDeltaThreshold) and was
    // completed as a clean full rebuild; 0 otherwise. Reset per public call — NOT per core
    // run — so it survives the full-rebuild retry that the guard triggers. Distinct from
    // _lastBuildFallbackCount (the S5 fault-retry path), which stays 0 on a guard trip.
    private int _lastBuildDeltaGuardTripped;
    internal int LastBuildDeltaGuardTripped => Volatile.Read(ref _lastBuildDeltaGuardTripped);

    // INC-3A test-observable: 1 when the last BuildOrUpdateCoreAsync run produced the
    // corpusfreq artifact via the algebraic delta (old counts - removed/changed old
    // texts + added/changed new texts), 0 when the full text.bin recount ran (fresh
    // build, delta preconditions unmet, or an inconsistency fallback). Reset per core run.
    private int _lastBuildFreqDeltaApplied;
    internal int LastBuildFreqDeltaApplied => Volatile.Read(ref _lastBuildFreqDeltaApplied);

    // INC-4A test-observable: number of entries whose inverted-alphabet gram sets
    // were COMPUTED from text during the last BuildOrUpdateCoreAsync run,
    // rather than read from the gramsets sidecar. On a warm sidecar an N-entry delta
    // computes exactly N; a full/fallback build computes every entry. Reset per core run.
    private int _lastBuildGramComputeCount;
    internal int LastBuildGramComputeCount => Volatile.Read(ref _lastBuildGramComputeCount);

    // FL7 (design §5.3) test-observable: incremented once per verify/snippet read that PARSED
    // XML from disk (the sidecar-miss fallback in GetSearchableTextCached). With the origin text
    // sidecar present, origin-side verify reads route through the mmap and this stays 0; absent,
    // every origin verify parses XML (the perf hole FL7's materialization job closes). Reset by
    // tests via ResetVerifyXmlReadCount before a query to assert sidecar-vs-XML routing.
    private int _lastVerifyXmlReadCount;
    internal int LastVerifyXmlReadCount => Volatile.Read(ref _lastVerifyXmlReadCount);
    internal void ResetVerifyXmlReadCount() => Interlocked.Exchange(ref _lastVerifyXmlReadCount, 0);

    // FL7 (design §5.3): the origin-text materialization job. Best-effort, background, idle-
    // priority, single-writer. _originTextMaterializeFlag gates concurrent spawns (only one job
    // in flight per service); _originTextMaterializeTask exposes the last-fired task so tests can
    // deterministically await it; _originTextMaterializeCount counts completed materializations.
    // _backgroundCts cancels any in-flight job on Dispose so shutdown never blocks on it.
    private int _originTextMaterializeFlag;
    private int _originTextMaterializeCount;
    private Task? _originTextMaterializeTask;
    private readonly CancellationTokenSource _backgroundCts = new();
    internal int LastOriginTextMaterializeCount => Volatile.Read(ref _originTextMaterializeCount);
    /// <summary>FL7: awaits the last-fired origin-text materialization job (or completes
    /// immediately when none has fired). Test hook for deterministic assertions.</summary>
    internal Task WhenOriginTextMaterializedAsync() => Volatile.Read(ref _originTextMaterializeTask) ?? Task.CompletedTask;

    /// <summary>
    /// Test instrumentation ONLY: invoked at the top of the incremental build path
    /// (never on the full path) so tests can inject a fault and prove the
    /// retry-as-full-rebuild fallback produces a complete, equivalent artifact family.
    /// </summary>
    internal Action? TestOnlyIncrementalFault;

    // FL4: the COMBINED family is a stable, dedicated instance (never reassigned): the
    // build/save path and the combined load/serve path always route through it. `_families`
    // (below) is the QUERY-path family list — [combined] for a combined root, [origin, overlay]
    // for a split root (design §4.3). Query/invalidation sites iterate `_families`; keeping
    // Combined separate from _families[0] means loading a split root never repoints the build
    // path's family at the origin layer. Cache slots are guarded by _indexCacheLock; the list
    // reference is reassigned (never mutated in place) under the same lock, so a lock-free
    // reader that captured the old reference stays self-consistent.
    private readonly SearchFamily _combinedFamily = new SearchFamily(
        binFileName: BinFileName,
        invertedBinFileName: InvertedBinFileName,
        textManifestFileName: TextManifestFileName,
        textBinFileName: TextBinFileName,
        corpusFreqManifestFileName: CorpusFreqManifestFileName,
        corpusFreqBinFileName: CorpusFreqBinFileName);

    // FL1/FL4: the ordered query-path family list. Starts as [combined]. FL4's split load
    // (TryLoadSplitAsync) replaces it with [origin, overlay]; the combined load resets it to
    // [combined]. Reassigned (never mutated in place) under _indexCacheLock so a concurrent
    // reader that captured the old reference stays consistent.
    private List<SearchFamily> _families;

    /// <summary>The stable combined family (build/save + combined serve). Split roots serve
    /// through the two members of <see cref="_families"/>, leaving this dormant.</summary>
    private SearchFamily Combined => _combinedFamily;

    // FL4 split-root load cache (design §4.3 "in-memory merged view"). Guarded by
    // _indexCacheLock. Mirrors the combined manifest fast path: a repeat TryLoadAsync on the
    // same split root (both layer manifests unchanged on disk) returns the cached merged view
    // and leaves the already-loaded origin/overlay families (mmaps, inverted, corpusfreq) warm.
    private SearchIndexManifest? _cachedMergedManifest;
    private string? _cachedMergedOriginPath;
    private DateTime _cachedMergedOriginWriteUtc;
    private string? _cachedMergedOverlayPath;
    private DateTime _cachedMergedOverlayWriteUtc;
    // True while _families is the [origin, overlay] split set. Read (under lock) by the text
    // route + verify paths so combined roots stay on the exact pre-FL4 single-sidecar path.
    private bool _splitLoaded;

    public SearchIndexService()
    {
        _families = new List<SearchFamily> { _combinedFamily };
        _defaultTextRoute = new TextRoute { Entry = null, TextBinFileName = TextBinFileName, Family = _combinedFamily };
    }

    // FL4: shared fallback route for a (rel, side) with no text sidecar entry — entry == null
    // forces the XML-parse fallback in TryReadSearchableTextFromSidecar, so the bin/family are
    // inert. Immutable in practice; safe to share across parallel verify workers.
    private readonly TextRoute _defaultTextRoute;

    /// <summary>
    /// FL1 (frozen/live split, design §2.4/§4.3): bundles the per-family index state that
    /// was previously held as single service-level slots — the bloom bin + inverted file
    /// names (identity), the cached manifest + mmap cache slots, the inverted-index handle,
    /// and the corpus-frequency partial. Today exactly one instance exists (the combined
    /// `search.*` family). FL4 grows the service's family list to {origin, overlay}; the
    /// query path already iterates it. Cache-slot fields are guarded by the owning service's
    /// _indexCacheLock (they were before this refactor and remain so).
    /// </summary>
    private sealed class SearchFamily
    {
        // ── identity (file names; the combined family == today's constants) ──
        // FL4: a family is now fully self-describing — it carries the bloom, inverted, text
        // and corpusfreq file names it owns. The combined family carries the legacy names
        // (byte-identical); the origin/overlay families carry search.origin.* / search.overlay.*.
        // This lets the two-bin bloom sweep + text/corpusfreq routing pick the right file per
        // (rel, side) with no external lookup table (design §2.4/§2.5/§2.3).
        public readonly string BinFileName;
        public readonly string InvertedBinFileName;
        public readonly string TextManifestFileName;
        public readonly string TextBinFileName;
        public readonly string CorpusFreqManifestFileName;
        public readonly string CorpusFreqBinFileName;

        // ── cached manifest + mmap slots (guarded by SearchIndexService._indexCacheLock) ──
        public SearchIndexManifest? CachedManifest;
        public string? CachedManifestPath;
        public DateTime CachedManifestWriteUtc;
        public SearchTextManifest? CachedTextManifest;
        public string? CachedTextManifestPath;
        public DateTime CachedTextManifestWriteUtc;

        public MemoryMappedFile? CachedMmf;
        public MemoryMappedViewAccessor? CachedAccessor;
        public string? CachedBinPath;
        public DateTime CachedBinWriteUtc;

        public MemoryMappedFile? CachedTextMmf;
        public string? CachedTextBinPath;
        public DateTime CachedTextBinWriteUtc;

        // ── handles ──
        public InvertedSearchIndex? Inverted;
        public IReadOnlyDictionary<string, int>? CharFreqs;
        public IReadOnlyDictionary<string, int>? BigramFreqs;
        public long TotalChars;

        public SearchFamily(
            string binFileName,
            string invertedBinFileName,
            string textManifestFileName,
            string textBinFileName,
            string corpusFreqManifestFileName,
            string corpusFreqBinFileName)
        {
            BinFileName = binFileName;
            InvertedBinFileName = invertedBinFileName;
            TextManifestFileName = textManifestFileName;
            TextBinFileName = textBinFileName;
            CorpusFreqManifestFileName = corpusFreqManifestFileName;
            CorpusFreqBinFileName = corpusFreqBinFileName;
        }

        /// <summary>
        /// Dispose + null this family's mmap/manifest cache slots. Mirrors the pre-FL1
        /// InvalidateIndexCaches body EXACTLY: it clears only the manifest + mmap caches
        /// and does NOT touch the Inverted handle or corpusfreq partial (those were never
        /// cleared by InvalidateIndexCaches). Caller holds _indexCacheLock.
        /// </summary>
        public void InvalidateCaches()
        {
            CachedManifest = null;
            CachedManifestPath = null;
            CachedManifestWriteUtc = default;
            CachedTextManifest = null;
            CachedTextManifestPath = null;
            CachedTextManifestWriteUtc = default;

            try { CachedAccessor?.Dispose(); } catch { }
            try { CachedMmf?.Dispose(); } catch { }
            try { CachedTextMmf?.Dispose(); } catch { }

            CachedAccessor = null;
            CachedMmf = null;
            CachedBinPath = null;
            CachedBinWriteUtc = default;
            CachedTextMmf = null;
            CachedTextBinPath = null;
            CachedTextBinWriteUtc = default;
        }
    }

    private const string ManifestFileName = "search.index.manifest.json";
    private const string BinFileName = "search.index.bin";
    // Searchable-text sidecar is versioned separately from bloom.
    // If this sidecar is missing/corrupt/mismatched, search verify falls back to XML parse.
    private const string TextManifestFileName = "search.text.manifest.json";
    private const string TextBinFileName = "search.text.bin";
    // Corpus-frequency sibling (ranking input). Its BuildGuid participates in the
    // family-guid gate (§2.2a): a corpusfreq guid mismatch while bloom is current is
    // treated as a family mismatch (Branch B reseed/rebuild), never fresh-with-degraded-ranking.
    private const string CorpusFreqManifestFileName = "search.corpusfreq.manifest.json";
    private const string CorpusFreqBinFileName = "search.corpusfreq.bin";
    // Exact-match inverted index (IIDX v4). Named here so the combined SearchFamily can
    // carry it as identity (FL1); load/build sites read it from Combined.InvertedBinFileName.
    private const string InvertedBinFileName = "search.inverted.bin";

    // ── FL3 (frozen/live split, design §1.3/§1.4): split-family file names + v9 guids ──
    // The build core is parametrized by a LayerSpec carrying the file-name set for the
    // layer being written. The COMBINED spec reuses the legacy names above (byte-identical
    // output); the ORIGIN/OVERLAY specs use these. On-disk BINARY formats are unchanged —
    // only file names + manifest fields differ. Nothing in the app loads these yet (FL4/FL5);
    // FL3 only makes origin/overlay builds directly (test/CI) invokable.
    private const string OriginManifestFileName = "search.origin.manifest.json";
    private const string OriginBinFileName = "search.origin.bin";
    private const string OriginTextManifestFileName = "search.origin.text.manifest.json";
    private const string OriginTextBinFileName = "search.origin.text.bin";
    private const string OriginCorpusFreqManifestFileName = "search.origin.corpusfreq.manifest.json";
    private const string OriginCorpusFreqBinFileName = "search.origin.corpusfreq.bin";
    private const string OriginInvertedBinFileName = "search.origin.inverted.bin";
    // Origin family format guid. A bump here (v9→v10) invalidates only the origin family
    // (design §5.1); FL3 introduces it but the serving path still gates on the combined
    // BuildGuid (search-v8-full-df) — origin/overlay are not yet loaded.
    private const string OriginBuildGuid = "search-v9-origin";

    private const string OverlayManifestFileName = "search.overlay.manifest.json";
    private const string OverlayBinFileName = "search.overlay.bin";
    private const string OverlayTextManifestFileName = "search.overlay.text.manifest.json";
    private const string OverlayTextBinFileName = "search.overlay.text.bin";
    private const string OverlayCorpusFreqManifestFileName = "search.overlay.corpusfreq.manifest.json";
    private const string OverlayCorpusFreqBinFileName = "search.overlay.corpusfreq.bin";
    private const string OverlayInvertedBinFileName = "search.overlay.inverted.bin";
    private const string OverlayBuildGuid = "search-v9-overlay";

    private const int BloomBits = 16384; // was 4096
    private const int BloomBytes = BloomBits / 8;
    private const int BloomUlongs = BloomBits / 64;
    private const int BloomHashCount = 5; // optional: 4 is okay too
    // Bumped 2026-07-11 from "search-v7-postings-tf" (AUTHORIZED): the inverted index DF cut
    // was raised 0.8 → 1.0 (full CJK-bigram coverage — see InvertedSearchIndex.MaxDocFrequencyRatio).
    // The on-disk FORMAT is unchanged (still v4), so a format check alone wouldn't rebuild; the
    // GUID bump forces the ONE full rebuild that writes the now-uncut postings, so existing
    // 80%-coverage indexes are refreshed and common-phrase CJK queries get the instant path
    // instead of the bloom fallback. Do NOT bump again lightly.
    // (Previous bump 2026-07-10: inverted tf postings v3 → v4, instant-search sprint.
    //  Previous bump 2026-07-08: cjk2 + corpusfreq IndexStamp binding, D3 item 5.
    //  Previous bump 2026-07-04: inverted index integrity contract, audit P1.1.)
    private const string BuildGuid = "search-v8-full-df";
    // PERF (E): an incremental build whose changed+removed set exceeds this fraction of
    // the corpus abandons the incremental path and runs a clean full rebuild instead —
    // near a wholesale change the per-entry incremental overhead (old-artifact reads,
    // gram-set transpose, algebraic freq delta) buys little over recomputing once. Checked
    // BEFORE any artifact is written, so no BuildGuid bump and no on-disk format change.
    private const double IncrementalFullRebuildDeltaThreshold = 0.20;
    private const int TextManifestVersion = 1;
    private const string TextBuildGuid = "search-v1-text-sidecar";
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
        public byte[] TextBytes;        // always populated: fresh extraction bytes, or the
                                        // validated old text.bin block for unchanged entries
        public string SearchableText;   // always populated (needed for inverted index)
        public bool CopiedBloom;
        public long OldBloomOffset;     // valid only when CopiedBloom = true
        public string? ContentHash;     // lowercase-hex SHA256 of the raw XML bytes: fresh for
                                        // changed/added entries, carried from the old manifest
                                        // for unchanged entries (null on legacy carry-forward)
        public string? WalkKey;         // AppendDirRows walk key ("orig/..." / "tran{i}/...")
                                        // when the file is physically under originalDir or
                                        // translatedDirs[i]; null for additional-dir files
    }

    // ==========================================================
    // CO-OCCURRENCE METRICS (dropdown controls what panel shows)
    // ==========================================================

    public sealed class CooccurrencePanelResult
    {
        public string Summary { get; set; } = "";
        public string LeftTitle { get; set; } = "Character pairs";
        public string RightTitle { get; set; } = "Recurring phrases";
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
            else if (key.Length == 2)
            {
                corpusBigramFreqs!.TryGetValue(key, out var f);
                return f;
            }
            else
            {
                // Trigram corpus frequencies are not indexed in v1.
                // Association metrics (logDice, MI, t-score, G2) return 0 for trigrams,
                // which causes the G2 floor to filter them out. Trigrams still appear
                // under Frequency ranking. This is correct — we simply lack the data.
                return 0;
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
            CoocMetric.LogDice => "Typicality",
            CoocMetric.MI => "Distinctive",
            CoocMetric.MI3 => "Balanced MI",
            CoocMetric.TScore => "Common patterns",
            CoocMetric.LogLikelihood => "Significance",
            CoocMetric.Frequency => "Frequency",
            CoocMetric.Dominance => "Concentration",
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
            LeftTitle = $"Character pairs by {metricName}",
            RightTitle = $"Recurring phrases by {metricName}",
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
        // Corpus-scan titles: extract metric name suffix from result-scoped title and prepend "(corpus)"
        var metricSuffix = result.LeftTitle.IndexOf(" by ", StringComparison.Ordinal) is int idx and >= 0
            ? result.LeftTitle[(idx + 4)..]
            : "Typicality";
        result.LeftTitle = $"Character pairs (corpus) by {metricSuffix}";
        result.RightTitle = $"Recurring phrases (corpus) by {metricSuffix}";
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

    // Canonical 3-range CJK set (U+3400-4DBF, U+4E00-9FFF, U+F900-FAFF).
    // Routed to the shared classifier; CjkTextTests pins it to the historical
    // set over the full BMP so the GUID-versioned search artifacts cannot drift.
    private static bool IsCjk(char ch) => ReadZen.App.Infrastructure.CjkText.IsIdeograph(ch);

    public void Dispose()
    {
        // FL7: cancel any in-flight origin-text materialization so disposal never blocks on it
        // (the job is best-effort; a cancelled run just leaves the sidecar absent → XML fallback).
        try { _backgroundCts.Cancel(); } catch { }
        InvalidateIndexCaches();
        GC.SuppressFinalize(this);
    }

    public void InvalidateIndexCaches()
    {
        lock (_indexCacheLock)
        {
            // FL1/FL4: invalidate the combined family AND every query-path family's cache
            // slots. For a combined root _families == [_combinedFamily] so this is one clear;
            // for a split root the combined family (dormant) plus origin+overlay are all
            // cleared. InvalidateCaches is idempotent, so the combined-root double-touch is
            // harmless.
            _combinedFamily.InvalidateCaches();
            foreach (var fam in _families)
                fam.InvalidateCaches();

            // FL4: drop the split merged-view cache so the next load re-detects + re-folds.
            _cachedMergedManifest = null;
            _cachedMergedOriginPath = null;
            _cachedMergedOriginWriteUtc = default;
            _cachedMergedOverlayPath = null;
            _cachedMergedOverlayWriteUtc = default;
        }

        // FL2: the cached corpusfreq fold is derived from family partials; invalidate it
        // alongside the family caches. Family partials themselves are NOT cleared here
        // (InvalidateCaches never touched them), so the re-fold on next access recomputes
        // the same value — behaviour is unchanged with today's single family.
        InvalidateMergedCorpusFreqs();

        ClearVerifyTextCache();
    }

    // ── FL2: corpusfreq additive fold (design §2.3 consumer #5) ──

    /// <summary>
    /// Pure additive fold of per-family corpusfreq partials into a corpus-global view.
    /// Char counts, bigram counts, and totalChars are all sums over per-document texts, so
    /// the global value is the family-wise sum (<c>Freq_global[x] = Σ_family Freq_family[x]</c>,
    /// <c>N_global = Σ_family N_family</c>). Integer addition is associative/commutative and
    /// the family partition is a partition of the same text blocks, so the fold equals a
    /// direct global recount EXACTLY.
    /// <para/>
    /// A family with a null char/bigram partial (never loaded/built) does not participate.
    /// When no family has a loaded partial the result is <c>(null, null, 0)</c> — preserving
    /// the pre-FL2 null semantics of <see cref="CorpusCharFreqs"/> et al. When exactly one
    /// family has a loaded partial the SAME references are returned (no copy), so the
    /// single-family case is byte/reference-identical to reading that family directly.
    /// Extracted as a static pure helper so FL4's two-family merge reuses it unchanged.
    /// </summary>
    internal static (IReadOnlyDictionary<string, int>? charFreqs,
                     IReadOnlyDictionary<string, int>? bigramFreqs,
                     long totalChars)
        MergeCorpusFreqPartials(
            IEnumerable<(IReadOnlyDictionary<string, int>? charFreqs,
                         IReadOnlyDictionary<string, int>? bigramFreqs,
                         long totalChars)> partials)
    {
        var loaded = partials
            .Where(p => p.charFreqs != null && p.bigramFreqs != null)
            .ToList();

        if (loaded.Count == 0)
            return (null, null, 0);

        // One loaded partial → return it verbatim (same references, no allocation). This is
        // the standing case with today's sole family and keeps the fold reference-identical
        // to the pre-FL2 getters.
        if (loaded.Count == 1)
            return (loaded[0].charFreqs, loaded[0].bigramFreqs, loaded[0].totalChars);

        var mergedChars = new Dictionary<string, int>(loaded[0].charFreqs!.Count);
        var mergedBigrams = new Dictionary<string, int>(loaded[0].bigramFreqs!.Count);
        long mergedTotal = 0;

        foreach (var p in loaded)
        {
            foreach (var kv in p.charFreqs!)
            {
                mergedChars.TryGetValue(kv.Key, out var existing);
                mergedChars[kv.Key] = existing + kv.Value;
            }
            foreach (var kv in p.bigramFreqs!)
            {
                mergedBigrams.TryGetValue(kv.Key, out var existing);
                mergedBigrams[kv.Key] = existing + kv.Value;
            }
            mergedTotal += p.totalChars;
        }

        return (mergedChars, mergedBigrams, mergedTotal);
    }

    /// <summary>
    /// FL2: build (once, lazily) and cache the additive fold over every family's corpusfreq
    /// partial, exposed through <see cref="CorpusCharFreqs"/>/<see cref="CorpusBigramFreqs"/>/
    /// <see cref="CorpusTotalChars"/>. Cheap (single dictionary sum) and re-run only after an
    /// invalidation, so hot query-path reads pay nothing after the first.
    /// </summary>
    private void EnsureMergedCorpusFreqs()
    {
        lock (_corpusFreqMergeLock)
        {
            if (_mergedCorpusFreqsValid)
                return;

            var (chars, bigrams, total) = MergeCorpusFreqPartials(
                _families.Select(f => ((IReadOnlyDictionary<string, int>?)f.CharFreqs,
                                       (IReadOnlyDictionary<string, int>?)f.BigramFreqs,
                                       f.TotalChars)));

            _mergedCharFreqs = chars;
            _mergedBigramFreqs = bigrams;
            _mergedTotalChars = total;
            _mergedCorpusFreqsValid = true;
        }
    }

    /// <summary>
    /// FL2: mark the cached corpusfreq fold stale. Called whenever a family's corpusfreq
    /// partial changes (load/build) and from <see cref="InvalidateIndexCaches"/>. The next
    /// getter access re-folds.
    /// </summary>
    private void InvalidateMergedCorpusFreqs()
    {
        lock (_corpusFreqMergeLock)
        {
            _mergedCorpusFreqsValid = false;
            _mergedCharFreqs = null;
            _mergedBigramFreqs = null;
            _mergedTotalChars = 0;
        }
    }

    // FL1: bloom/text mmap caches are per-family slots. Callers pass the owning family
    // (Combined today). Behaviour is identical to the pre-FL1 single-slot helpers.
    private MemoryMappedViewAccessor GetOrCreateMappedAccessor(string binPath, SearchFamily fam)
    {
        var full = Path.GetFullPath(binPath);
        var writeUtc = File.GetLastWriteTimeUtc(full);

        lock (_indexCacheLock)
        {
            if (fam.CachedAccessor != null &&
                fam.CachedMmf != null &&
                string.Equals(fam.CachedBinPath, full, StringComparison.OrdinalIgnoreCase) &&
                fam.CachedBinWriteUtc == writeUtc)
            {
                return fam.CachedAccessor;
            }

            try { fam.CachedAccessor?.Dispose(); } catch { }
            try { fam.CachedMmf?.Dispose(); } catch { }

            fam.CachedMmf = MemoryMappedFile.CreateFromFile(full, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            fam.CachedAccessor = fam.CachedMmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            fam.CachedBinPath = full;
            fam.CachedBinWriteUtc = writeUtc;

            return fam.CachedAccessor;
        }
    }

    private MemoryMappedFile GetOrCreateTextMappedFile(string textBinPath, DateTime writeUtc, SearchFamily fam)
    {
        var full = Path.GetFullPath(textBinPath);

        lock (_indexCacheLock)
        {
            if (fam.CachedTextMmf != null &&
                string.Equals(fam.CachedTextBinPath, full, StringComparison.OrdinalIgnoreCase) &&
                fam.CachedTextBinWriteUtc == writeUtc)
            {
                return fam.CachedTextMmf;
            }

            try { fam.CachedTextMmf?.Dispose(); } catch { }

            fam.CachedTextMmf = MemoryMappedFile.CreateFromFile(full, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            fam.CachedTextBinPath = full;
            fam.CachedTextBinWriteUtc = writeUtc;
            return fam.CachedTextMmf;
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

    private static string NormalizeRelKey(string p) => ReadZen.App.Infrastructure.RelPath.Normalize(p);

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

    // ==========================================================
    // S7: BUNDLED PREBUILT INDEX — seed-from-bundle on first run
    // ==========================================================
    // A release ships a prebuilt copy of the index family next to the exe
    // (Assets/PrebuiltIndex, produced by CI via --build-search-index). On a virgin
    // index root the seed copies that family in ONCE so first run skips the cold
    // full build; the normal IsStaleAsync + incremental catch-up then reindexes only
    // the files that drifted since the bundle was cut. Nothing here bumps a BuildGuid
    // or changes an on-disk format — it is pure file plumbing.

    /// <summary>Exposes the current bloom/inverted-family BuildGuid so the seed decision
    /// (and its tests) can reject a bundle cut for an older index format.</summary>
    internal static string CurrentSearchBuildGuid => BuildGuid;

    /// <summary>FL6: the current frozen-ORIGIN family guid — the value CI bakes into the shipped
    /// bundle's <c>search.origin.manifest.json</c> and the guid-bundle-guard compares.</summary>
    internal static string CurrentOriginBuildGuid => OriginBuildGuid;

    /// <summary>FL6: the current live-OVERLAY family guid.</summary>
    internal static string CurrentOverlayBuildGuid => OverlayBuildGuid;

    /// <summary>FL6: the current corpusfreq sibling guid (§2.2a family gate).</summary>
    internal static string CurrentCorpusFreqBuildGuid => CorpusFreqBuildGuid;

    /// <summary>Outcome of <see cref="EvaluateBundleAdoption"/>: what a launch probe should do
    /// with the prebuilt bundle given the local index state and (when known) the live InputHash.
    /// Maps onto the §2.1 decision table.</summary>
    internal enum BundleAdoptionDecision
    {
        /// <summary>A loadable local index stays authoritative — do not touch the bundle
        /// (row 1 fresh; or row 3 when the bundle does not match live / would lose local
        /// postings).</summary>
        KeepLocal,
        /// <summary>A loadable local index is stale and the bundle provably matches live →
        /// replace the local family with the bundle (row 2, zero build).</summary>
        AdoptOverLocal,
        /// <summary>No usable local index (absent / corrupt / family-guid mismatch) → copy the
        /// bundle in (rows 4/5). In the pre-hash virgin branch this is returned unconditionally
        /// for a usable bundle; the not-stale-vs-catch-up verdict is decided post-seed.</summary>
        SeedVirgin,
        /// <summary>No usable bundle (absent, missing core manifest+bin, or family-guid
        /// mismatch) → the caller runs its normal build/keep-local path (rows 3/6).</summary>
        NoBundle,
    }

    /// <summary>
    /// Pure decision (no filesystem mutation): what should the launch probe do with
    /// <paramref name="bundleDir"/> for <paramref name="indexRoot"/>? Guard order (§2.2):
    /// (1) bundle presence + core bloom manifest+bin; (2) the bundle must stamp the CURRENT
    /// FAMILY guid — bloom <c>BuildGuid</c> AND <c>CorpusFreqBuildGuid</c> (§2.2a); (3) stamp
    /// comparison against <paramref name="liveInputHashOrNull"/>. When the caller has no live
    /// hash yet (<c>null</c> — the pre-hash virgin/Branch-B path) the stamp comparison is
    /// skipped and a usable bundle yields <see cref="BundleAdoptionDecision.SeedVirgin"/>
    /// unconditionally (the ordering invariant: seed before hashing). With a live hash and an
    /// existing local index, adoption fires only when the bundle's baked InputHash equals live.
    /// </summary>
    internal static BundleAdoptionDecision EvaluateBundleAdoption(
        string indexRoot, string bundleDir, string? liveInputHashOrNull)
    {
        // Guard 1: the bundle must ship at least the core bloom manifest + bin.
        if (string.IsNullOrEmpty(bundleDir))
            return BundleAdoptionDecision.NoBundle;
        var bundleManifest = Path.Combine(bundleDir, ManifestFileName);
        var bundleBin = Path.Combine(bundleDir, BinFileName);
        if (!File.Exists(bundleManifest) || !File.Exists(bundleBin))
            return BundleAdoptionDecision.NoBundle;

        // Guard 2: family-guid — bloom AND corpusfreq must both be current. Gating on the
        // bloom guid alone (as the S7 seed did) would let a bundle whose corpusfreq loader
        // then refuses its file serve degraded ranking silently, forever (§2.2a).
        if (!BundleFamilyGuidCurrent(bundleDir))
            return BundleAdoptionDecision.NoBundle;

        // Guard 3: stamp comparison. Skipped in the virgin branch (no live hash yet) —
        // there the bundle is seeded unconditionally and the verdict is decided after the
        // post-seed hash pass (Branch B, §2.2). A present local bloom bin means "adopt over";
        // its absence means "seed onto nothing".
        bool localPresent = File.Exists(Path.Combine(indexRoot, BinFileName));
        if (liveInputHashOrNull == null || !localPresent)
            return BundleAdoptionDecision.SeedVirgin;

        // Local present + live hash known: adopt only when the bundle proves it equals live;
        // otherwise keep local (it may hold user entries / additional-corpus postings the
        // bundle lacks — adopting would lose them, §2.1a safety invariant).
        var bundleHash = TryReadBundleInputHash(bundleManifest);
        bool bundleMatchesLive = bundleHash != null &&
            string.Equals(bundleHash, liveInputHashOrNull, StringComparison.Ordinal);
        return bundleMatchesLive ? BundleAdoptionDecision.AdoptOverLocal : BundleAdoptionDecision.KeepLocal;
    }

    /// <summary>True when the bundle at <paramref name="bundleDir"/> stamps the CURRENT family
    /// guid: bloom <c>BuildGuid</c> on its main manifest AND <c>CorpusFreqBuildGuid</c> on its
    /// corpusfreq manifest (which must be present). The optional text sidecar is NOT part of the
    /// family gate — a mismatched/absent sidecar is ignored (safe, §5).</summary>
    private static bool BundleFamilyGuidCurrent(string bundleDir)
    {
        var mainGuid = TryReadBundleBuildGuid(Path.Combine(bundleDir, ManifestFileName));
        if (!string.Equals(mainGuid, BuildGuid, StringComparison.Ordinal))
            return false;
        return CorpusFreqGuidCurrentAt(bundleDir);
    }

    /// <summary>True when a <c>search.corpusfreq.manifest.json</c> is present at
    /// <paramref name="dir"/> and stamps the current <see cref="CorpusFreqBuildGuid"/>.
    /// Absent or mismatched ⇒ false (family-guid mismatch → Branch B reseed/rebuild).</summary>
    private static bool CorpusFreqGuidCurrentAt(string dir)
        => CorpusFreqGuidCurrentAt(dir, CorpusFreqManifestFileName);

    /// <summary>FL5: as <see cref="CorpusFreqGuidCurrentAt(string)"/> but over an arbitrary
    /// corpusfreq manifest file name — used to gate the ORIGIN family's corpusfreq sibling
    /// (<c>search.origin.corpusfreq.manifest.json</c>) with the same §2.2a discipline.</summary>
    private static bool CorpusFreqGuidCurrentAt(string dir, string corpusFreqManifestFileName)
    {
        var mp = Path.Combine(dir, corpusFreqManifestFileName);
        if (!File.Exists(mp)) return false;
        try
        {
            var json = File.ReadAllText(mp, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return false;
            var man = JsonSerializer.Deserialize<CorpusFreqManifest>(json, JsonOpts);
            return man != null && string.Equals(man.BuildGuid, CorpusFreqBuildGuid, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    private static string? TryReadBundleBuildGuid(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            return man?.BuildGuid;
        }
        catch { return null; }
    }

    private static string? TryReadBundleInputHash(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            return string.IsNullOrEmpty(man?.InputHash) ? null : man!.InputHash;
        }
        catch { return null; }
    }

    /// <summary>
    /// Copies the whole shipped <c>search.*</c> family from <paramref name="bundleDir"/> into
    /// <paramref name="indexRoot"/>, re-homes the RootPath the three path-bound manifests embed,
    /// and DELETES every pre-existing local <c>search.*</c> file NOT present in the bundle so the
    /// root is canonical by construction (the trimmed bundle has no <c>search.text.*</c> /
    /// <c>search.gramsets.*</c> — a surviving stale-IndexStamp text/gramsets manifest would
    /// otherwise be re-homed while pointing at bins that no longer exist, §2.2 leftover-delete).
    /// UNCONDITIONAL: the caller (via <see cref="EvaluateBundleAdoption"/>) decides whether to
    /// call this — there is no user-index guard here. All-or-nothing: any failure rolls back the
    /// files just copied so a clean full build runs instead. Returns true only when the family
    /// was actually copied in.
    /// </summary>
    internal static bool CopyBundleFamilyIntoRoot(string indexRoot, string bundleDir, TextWriter? log = null)
    {
        if (string.IsNullOrEmpty(bundleDir) || !Directory.Exists(bundleDir))
            return false;

        var copied = new List<string>();
        try
        {
            Directory.CreateDirectory(indexRoot);

            // Copy the entire search.* family. Globbing keeps this correct if the family
            // grows a future artifact; half-written CI .tmp scratch files are excluded.
            // Track the exact set of names the bundle contributes for the leftover-delete.
            var bundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var src in Directory.EnumerateFiles(bundleDir, "search.*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(src);
                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                var dst = Path.Combine(indexRoot, name);
                File.Copy(src, dst, overwrite: true);
                copied.Add(dst);
                bundleNames.Add(name);
            }

            if (copied.Count == 0)
                return false; // nothing to adopt (empty/invalid bundle) — treat as no-op

            // Re-home the absolute RootPath the two path-bound manifests embed. Without
            // this, TryLoad{,Text}ManifestAsync reject a manifest whose RootPath != the
            // load root and the freshly copied index would be silently ignored (full rebuild
            // anyway). IndexStamp — which binds the inverted/corpusfreq/gramsets siblings
            // to this build — is left untouched, so the family stays internally consistent.
            RehomeManifestRootPath(Path.Combine(indexRoot, ManifestFileName), indexRoot);
            RehomeManifestRootPath(Path.Combine(indexRoot, TextManifestFileName), indexRoot);
            // The gramsets sidecar also embeds RootPath and GramSetsStore.TryLoadAsync rejects
            // it unless full-path-equal to the load root; without this re-home the copied
            // sidecar is dead weight and every entry's gram sets are recomputed on the first
            // incremental catch-up.
            RehomeManifestRootPath(Path.Combine(indexRoot, GramSetsStore.ManifestFileName), indexRoot);

            // Leftover delete (§2.2): every local search.* file the bundle did NOT bring —
            // bins AND manifests (search.text.bin, search.text.manifest.json,
            // search.gramsets.bin, search.gramsets.manifest.json, plus any other stragglers).
            // A trimmed bundle ships no text/gramsets, so an old-IndexStamp text/gramsets
            // manifest left behind (and just re-homed above) would point at stamp-mismatched
            // or absent bins; deleting it keeps the root canonical rather than merely
            // safe-by-loader-rejection.
            foreach (var local in Directory.EnumerateFiles(indexRoot, "search.*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(local);
                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                if (bundleNames.Contains(name)) continue; // part of the freshly copied family
                try { File.Delete(local); } catch { /* best-effort; loader gate is the backstop */ }
            }

            log?.WriteLine($"Adopted search index from bundle: {copied.Count} files -> {indexRoot}");
            return true;
        }
        catch (Exception ex)
        {
            log?.WriteLine($"Bundle adopt failed ({ex.Message}); rolling back so a full build runs.");
            foreach (var f in copied)
            {
                try { File.Delete(f); } catch { }
            }
            return false;
        }
    }

    /// <summary>
    /// Rewrites ONLY the <c>RootPath</c> field of a seeded manifest to the new index root,
    /// preserving every other field byte-for-byte (JsonNode edit, not a model round-trip, so
    /// no default-value drift). No-op when the manifest is absent or unparseable.
    /// </summary>
    private static void RehomeManifestRootPath(string manifestPath, string indexRoot)
    {
        if (!File.Exists(manifestPath)) return;
        var json = File.ReadAllText(manifestPath, Utf8NoBom);
        if (string.IsNullOrWhiteSpace(json)) return;
        var node = System.Text.Json.Nodes.JsonNode.Parse(json);
        if (node == null) return;
        node["RootPath"] = indexRoot;
        // Temp-file + rename (matching this file's atomic-write pattern) so a crash mid-write
        // never leaves a truncated manifest at the index root, which would force a full cold
        // build on the next start — exactly the cost the bundle seed was meant to avoid.
        var tmp = manifestPath + ".tmp";
        File.WriteAllText(tmp, node.ToJsonString(JsonOpts), Utf8NoBom);
        File.Move(tmp, manifestPath, overwrite: true);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // FL5 (design §4.1/§4.2): ORIGIN-family bundle adoption. Mirrors the combined
    // EvaluateBundleAdoption / CopyBundleFamilyIntoRoot pair but scoped to the frozen
    // origin family (search.origin.*). Adopt-over-local is now UNCONDITIONALLY safe (the
    // origin family holds only origin postings; all user-local data lives in the overlay,
    // which adoption never touches) — so the ScopeComplete rider is gone.
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>FL5: pure decision for the ORIGIN family — should the launch probe adopt/seed the
    /// origin bundle at <paramref name="bundleDir"/> into <paramref name="indexRoot"/>? Guards
    /// mirror <see cref="EvaluateBundleAdoption"/>: (1) origin bloom manifest+bin present;
    /// (2) origin family guid current — origin bloom <c>BuildGuid</c>==<see cref="OriginBuildGuid"/>,
    /// <c>LayerRole=="origin"</c>, AND the origin corpusfreq sibling stamps the current guid;
    /// (3) stamp comparison against <paramref name="liveOriginHashOrNull"/> (null ⇒ pre-hash
    /// virgin ⇒ <see cref="BundleAdoptionDecision.SeedVirgin"/>). With a live hash and a present
    /// local origin bin, adoption fires only when the bundle's baked <c>OriginHash</c> == live.</summary>
    internal static BundleAdoptionDecision EvaluateOriginBundleAdoption(
        string indexRoot, string bundleDir, string? liveOriginHashOrNull)
    {
        if (string.IsNullOrEmpty(bundleDir))
            return BundleAdoptionDecision.NoBundle;
        var bundleManifest = Path.Combine(bundleDir, OriginManifestFileName);
        var bundleBin = Path.Combine(bundleDir, OriginBinFileName);
        if (!File.Exists(bundleManifest) || !File.Exists(bundleBin))
            return BundleAdoptionDecision.NoBundle;

        // Guard 2: origin family guid — bloom guid + role + corpusfreq sibling guid.
        if (!OriginBundleFamilyGuidCurrent(bundleDir))
            return BundleAdoptionDecision.NoBundle;

        bool localPresent = File.Exists(Path.Combine(indexRoot, OriginBinFileName));
        if (liveOriginHashOrNull == null || !localPresent)
            return BundleAdoptionDecision.SeedVirgin;

        var bundleHash = TryReadBundleOriginHash(bundleManifest);
        bool bundleMatchesLive = bundleHash != null &&
            string.Equals(bundleHash, liveOriginHashOrNull, StringComparison.Ordinal);
        return bundleMatchesLive ? BundleAdoptionDecision.AdoptOverLocal : BundleAdoptionDecision.KeepLocal;
    }

    /// <summary>FL5: the origin bundle stamps the current family — origin bloom manifest
    /// <c>BuildGuid</c>==<see cref="OriginBuildGuid"/> AND <c>LayerRole=="origin"</c> AND the
    /// origin corpusfreq sibling (<c>search.origin.corpusfreq.manifest.json</c>) is guid-current.</summary>
    private static bool OriginBundleFamilyGuidCurrent(string bundleDir)
    {
        SearchIndexManifest? man = null;
        try
        {
            var json = File.ReadAllText(Path.Combine(bundleDir, OriginManifestFileName), Utf8NoBom);
            if (!string.IsNullOrWhiteSpace(json))
                man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
        }
        catch { return false; }
        if (man == null) return false;
        if (!string.Equals(man.BuildGuid, OriginBuildGuid, StringComparison.Ordinal)) return false;
        if (!string.Equals(man.LayerRole, "origin", StringComparison.Ordinal)) return false;
        return CorpusFreqGuidCurrentAt(bundleDir, OriginCorpusFreqManifestFileName);
    }

    private static string? TryReadBundleOriginHash(string manifestPath)
    {
        try
        {
            var json = File.ReadAllText(manifestPath, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            return string.IsNullOrEmpty(man?.OriginHash) ? null : man!.OriginHash;
        }
        catch { return null; }
    }

    /// <summary>FL5: copies ONLY the frozen <c>search.origin.*</c> family from
    /// <paramref name="bundleDir"/> into <paramref name="indexRoot"/>, re-homes the path-bound
    /// origin manifests, and deletes pre-existing local <c>search.origin.*</c> files the bundle
    /// did not bring (leftover-delete, restricted to the origin glob). The <c>search.overlay.*</c>
    /// and legacy <c>search.index.*</c> families are NEVER touched — origin adoption is layer-local.
    /// All-or-nothing: any failure rolls back the just-copied files. Returns true only when the
    /// origin family was actually copied in.</summary>
    internal static bool CopyOriginBundleFamilyIntoRoot(string indexRoot, string bundleDir, TextWriter? log = null)
    {
        if (string.IsNullOrEmpty(bundleDir) || !Directory.Exists(bundleDir))
            return false;

        var copied = new List<string>();
        try
        {
            Directory.CreateDirectory(indexRoot);

            var bundleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var src in Directory.EnumerateFiles(bundleDir, "search.origin.*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(src);
                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                var dst = Path.Combine(indexRoot, name);
                File.Copy(src, dst, overwrite: true);
                copied.Add(dst);
                bundleNames.Add(name);
            }

            if (copied.Count == 0)
                return false;

            // Re-home the RootPath the path-bound origin manifests embed (main + optional text
            // sidecar). The corpusfreq sibling is bound by IndexStamp, not RootPath (RehomeManifest
            // is a no-op when the field is absent), so re-homing it is harmless.
            RehomeManifestRootPath(Path.Combine(indexRoot, OriginManifestFileName), indexRoot);
            RehomeManifestRootPath(Path.Combine(indexRoot, OriginTextManifestFileName), indexRoot);
            RehomeManifestRootPath(Path.Combine(indexRoot, OriginCorpusFreqManifestFileName), indexRoot);

            // Leftover-delete, restricted to the origin glob (never overlay/legacy families).
            foreach (var local in Directory.EnumerateFiles(indexRoot, "search.origin.*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(local);
                if (name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                if (bundleNames.Contains(name)) continue;
                try { File.Delete(local); } catch { }
            }

            log?.WriteLine($"Adopted origin family from bundle: {copied.Count} files -> {indexRoot}");
            return true;
        }
        catch (Exception ex)
        {
            log?.WriteLine($"Origin bundle adopt failed ({ex.Message}); rolling back.");
            foreach (var f in copied)
            {
                try { File.Delete(f); } catch { }
            }
            return false;
        }
    }

    /// <summary>Test-only override for the exe-adjacent prebuilt bundle directory. When non-null,
    /// <see cref="ResolveBundleDir"/> returns this instead of <c>AppPaths.GetPrebuiltIndexDir</c>,
    /// letting tests stage a bundle and exercise the full adopt/seed flow through
    /// <see cref="IsStaleAsync"/> (which resolves the bundle dir internally rather than by param).</summary>
    internal string? TestOnlyBundleDirOverride;

    /// <summary>Resolves the prebuilt bundle directory the launch probe adopts/seeds from:
    /// the test override when set, else the exe-adjacent Assets/PrebuiltIndex. Never throws —
    /// bundle resolution is an optimization, never a hard dependency of the staleness probe.</summary>
    private string? ResolveBundleDir()
    {
        if (TestOnlyBundleDirOverride != null) return TestOnlyBundleDirOverride;
        try { return ReadZen.App.Infrastructure.AppPaths.GetPrebuiltIndexDir(); }
        catch { return null; }
    }

    /// <summary>Best-effort content-hash backfill (shared by the fresh + adopted not-stale
    /// verdicts): if any entries had null/stale ContentHash or CI-machine stat stamps and we
    /// just computed fresh ones, patch the manifest on disk so the NEXT probe hits the fast
    /// stat-only cache path. Single-writer guarded — a concurrent caller skips (the cache still
    /// gets populated on a later call). Never throws — a write hiccup must not fail the probe.</summary>
    private async Task MaybeBackfillContentHashesAsync(
        string root,
        SearchIndexManifest manifest,
        IReadOnlyDictionary<string, string> writeBack,
        IReadOnlyDictionary<string, (long Ticks, long Length)> stampWriteBack)
    {
        if (writeBack.Count == 0) return;
        if (Interlocked.CompareExchange(ref _contentHashBackfillFlag, 1, 0) != 0) return;
        try
        {
            ApplyContentHashWriteBack(manifest, writeBack, stampWriteBack);
            await SaveContentHashBackfillAsync(root, manifest, CancellationToken.None).ConfigureAwait(false);
            Interlocked.Increment(ref _contentHashBackfillCount);
        }
        catch
        {
            // Best-effort: a failed backfill just means the next call will retry.
        }
        finally
        {
            Interlocked.Exchange(ref _contentHashBackfillFlag, 0);
        }
    }

    /// <summary>
    /// Headless CLI entry (Program.Main, <c>--build-search-index</c>): full-rebuild a shippable
    /// search index for a corpus into an output dir. Mirrors the <c>--build-segments</c> tool.
    /// Exit codes: 0 = success, 1 = bad args / missing dirs, 2 = build threw, 3 = build produced
    /// no bin. This is what CI runs to generate the bundle staged into Assets/PrebuiltIndex.
    /// </summary>
    public static int RunHeadlessBuild(string[] args, TextWriter log)
    {
        string? Arg(string name)
        {
            var i = Array.IndexOf(args, "--" + name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        var sourceDir = Arg("source-dir");
        var transDir = Arg("trans-dir");
        var outDir = Arg("out-dir");
        // FL3 (design §3.1/§6.3): which family/families to build.
        //   combined (default) — legacy search.index.* family, byte-identical to before
        //                        (the release pipeline + BundleSeedTests still rely on this).
        //   origin             — frozen search.origin.* family (originalDir only).
        //   overlay            — live search.overlay.* family (translations; needs an origin
        //                        family present in outDir first for §1.2/§3.2).
        //   both               — origin then overlay.
        var layer = (Arg("layer") ?? "combined").Trim().ToLowerInvariant();

        if (sourceDir == null || transDir == null || outDir == null)
        {
            log.WriteLine("usage: --build-search-index --source-dir <xml-p5 dir> --trans-dir <xml-p5t dir> --out-dir <index dir> [--layer combined|origin|overlay|both]");
            return 1;
        }
        if (!Directory.Exists(sourceDir) || !Directory.Exists(transDir))
        {
            log.WriteLine("error: --source-dir or --trans-dir does not exist");
            return 1;
        }
        if (layer != "combined" && layer != "origin" && layer != "overlay" && layer != "both")
        {
            log.WriteLine($"error: unknown --layer '{layer}' (expected combined|origin|overlay|both)");
            return 1;
        }

        try
        {
            Directory.CreateDirectory(outDir);
            var svc = new SearchIndexService();
            var progress = new Progress<(int done, int total, string phase)>(t =>
                log.WriteLine($"  {t.phase}: {t.done}/{t.total}"));

            log.WriteLine($"Building bundled search index (layer={layer}, forceRebuild) ...");
            log.WriteLine($"  Source: {sourceDir}");
            log.WriteLine($"  Trans:  {transDir}");
            log.WriteLine($"  Out:    {outDir}");

            string expectedBin;
            switch (layer)
            {
                case "origin":
                    svc.BuildOriginLayerAsync(outDir, sourceDir, progress).GetAwaiter().GetResult();
                    expectedBin = OriginBinFileName;
                    break;
                case "overlay":
                    svc.BuildOverlayLayerAsync(outDir, new[] { transDir }, progress: progress).GetAwaiter().GetResult();
                    expectedBin = OverlayBinFileName;
                    break;
                case "both":
                    svc.BuildOriginLayerAsync(outDir, sourceDir, progress).GetAwaiter().GetResult();
                    svc.BuildOverlayLayerAsync(outDir, new[] { transDir }, progress: progress).GetAwaiter().GetResult();
                    expectedBin = OverlayBinFileName; // origin bin is verified implicitly below
                    break;
                default: // "combined"
                    svc.BuildOrUpdateAsync(outDir, sourceDir, new[] { transDir },
                            forceRebuild: true, progress: progress)
                       .GetAwaiter().GetResult();
                    expectedBin = BinFileName;
                    break;
            }

            if (!File.Exists(Path.Combine(outDir, expectedBin)) ||
                (layer == "both" && !File.Exists(Path.Combine(outDir, OriginBinFileName))))
            {
                log.WriteLine($"error: build completed but {expectedBin} was not produced");
                return 3;
            }

            log.WriteLine($"OK: bundled search index (layer={layer}) written to {outDir}");
            return 0;
        }
        catch (Exception ex)
        {
            log.WriteLine($"error: index build failed: {ex}");
            return 2;
        }
    }

    /// <summary>
    /// Headless CLI entry (Program.Main, <c>--print-build-guid</c>): prints the current
    /// search index family GUIDs to <paramref name="log"/> in a machine-parseable
    /// <c>KEY=VALUE</c> form, one per line, then returns 0. CI's release workflow uses this
    /// to assert that the freshly staged bundle's <c>search.index.manifest.json</c> BuildGuid
    /// equals the guid the release binary itself would write (guarding against a stale
    /// artifact-restore serving a prior binary), and to power the release-blocking
    /// guid-bundle-guard (a guid bump that ships without a matching-guid bundle fails the tag).
    /// Emits the family guid as <c>SearchBuildGuid=</c> and the corpusfreq sibling as
    /// <c>CorpusFreqBuildGuid=</c>. No side effects; safe on a display-less runner.
    /// </summary>
    public static int RunPrintBuildGuid(TextWriter log)
    {
        // FL6 (§6.3): the release ships the frozen ORIGIN family as its bundle, so the
        // release-blocking guid-bundle-guard now compares the ORIGIN guid against the staged
        // search.origin.manifest.json. Emit the origin/overlay/corpusfreq guids; the legacy
        // SearchBuildGuid line is kept for older tooling (harmless — combined serving is
        // retired in FL8, and nothing new parses it).
        log.WriteLine($"SearchBuildGuid={BuildGuid}");
        log.WriteLine($"SearchOriginBuildGuid={OriginBuildGuid}");
        log.WriteLine($"SearchOverlayBuildGuid={OverlayBuildGuid}");
        log.WriteLine($"CorpusFreqBuildGuid={CorpusFreqBuildGuid}");
        return 0;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // FL5 (design §4): two-layer staleness / adoption for SPLIT roots. A root uses the split
    // path when a split family already exists on disk OR a guid-current split bundle is present;
    // otherwise (incl. an existing LEGACY COMBINED root with no split bundle) it stays on the
    // combined path verbatim — combined→split migration is FL6, so an existing combined user is
    // NEVER mass-rebuilt by FL5. This guard is the #1 FL5 safety constraint.
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>Per-layer probe verdict. <c>OriginNeedsBuild</c> = origin is stale/absent AND
    /// could not be adopted from a bundle (a rare local origin build is owed).
    /// <c>OverlayNeedsBuild</c> = overlay stale/absent OR its <c>BasedOnOriginStamp</c> no longer
    /// matches the current origin stamp (§1.5).</summary>
    private readonly record struct SplitProbeResult(bool OriginNeedsBuild, bool OverlayNeedsBuild);

    /// <summary>FL5 routing: is <paramref name="root"/> served by the split (origin/overlay)
    /// path? True iff a split manifest is already on disk, OR — for a root with no legacy
    /// combined family — a guid-current split bundle is available to seed from. A legacy
    /// combined root with no split-on-disk stays combined until FL6 (migration).</summary>
    private bool UseSplitPath(string root, string? bundleDir)
    {
        bool splitOnDisk = File.Exists(Path.Combine(root, OriginManifestFileName)) ||
                           File.Exists(Path.Combine(root, OverlayManifestFileName));
        if (splitOnDisk) return true;

        // A legacy combined family with no split-on-disk stays on the COMBINED path for
        // serving/staleness (its serving code is retired in FL8, not FL6). FL6's combined→split
        // MIGRATION is driven separately by the build orchestration (BuildOrUpdateAsync →
        // MigrateCombinedToSplitAsync, gated by NeedsMigration), which converts the family to split
        // via Path A (adopt origin bundle + fresh overlay) or Path B (carve from the legacy
        // artifacts) — never a mass rebuild. Once migrated, splitOnDisk above routes it to split.
        bool legacyCombinedOnDisk = File.Exists(Path.Combine(root, ManifestFileName));
        if (legacyCombinedOnDisk) return false;

        // Virgin root: prefer a split bundle when one ships (guid-current). Absent → combined
        // cold build (today's behaviour); the real origin bundle ships in FL6.
        return SplitBundleAvailable(bundleDir);
    }

    /// <summary>FL5: a usable, guid-current origin bundle is present (origin bloom manifest+bin,
    /// current guid + role, origin corpusfreq sibling guid-current).</summary>
    private static bool SplitBundleAvailable(string? bundleDir)
    {
        if (string.IsNullOrEmpty(bundleDir)) return false;
        if (!File.Exists(Path.Combine(bundleDir, OriginManifestFileName)) ||
            !File.Exists(Path.Combine(bundleDir, OriginBinFileName)))
            return false;
        return OriginBundleFamilyGuidCurrent(bundleDir);
    }

    /// <summary>FL5 (§4.1): run both per-layer probes. The ORIGIN probe is side-effecting (it
    /// adopts/seeds the origin bundle when appropriate, preserving the seed-before-hash ordering
    /// invariant); the OVERLAY probe then reads the possibly-updated origin stamp for its
    /// <c>BasedOnOriginStamp</c> comparison, so an origin adopt automatically flags the overlay
    /// stale. Returns the per-layer build verdict for both <see cref="IsStaleAsync"/> and the
    /// build orchestrator.</summary>
    private async Task<SplitProbeResult> ProbeLayersAsync(
        string root, string originalDir, IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs, IReadOnlyList<string>? additionalTranslatedDirs,
        string? bundleDir)
    {
        bool originNeedsBuild = await ProbeOriginAsync(root, originalDir, bundleDir).ConfigureAwait(false);
        bool overlayNeedsBuild = await ProbeOverlayAsync(
            root, translatedDirs, additionalOriginalDirs, additionalTranslatedDirs).ConfigureAwait(false);
        return new SplitProbeResult(originNeedsBuild, overlayNeedsBuild);
    }

    /// <summary>FL5 (§4.1 origin probe): freshness of the frozen origin family, adopt-preferred.
    /// Fresh (OriginHash == live origin hash, guid-current) → false (zero work). Stale/absent →
    /// adopt the origin bundle when it provably matches live (or seed a virgin root before
    /// hashing); returns true ONLY when neither fresh nor adopted, i.e. a local origin build is
    /// owed (the rare corpus-diverged last resort). Adopt/seed happen as a side effect here.</summary>
    private async Task<bool> ProbeOriginAsync(string root, string originalDir, string? bundleDir)
    {
        var originMp = Path.Combine(root, OriginManifestFileName);
        var originBp = Path.Combine(root, OriginBinFileName);
        bool localPresent = File.Exists(originMp) && File.Exists(originBp);

        SearchIndexManifest? localOrigin = localPresent
            ? await LoadLayerManifestAsync(originMp, originBp, root, OriginBuildGuid, "origin").ConfigureAwait(false)
            : null;
        bool originFamilyCurrent = localOrigin != null &&
            CorpusFreqGuidCurrentAt(root, OriginCorpusFreqManifestFileName);

        if (originFamilyCurrent && !string.IsNullOrEmpty(localOrigin!.OriginHash))
        {
            try
            {
                // §5.2 heal: a bundle-adopted/seeded origin manifest carries the CI machine's
                // (ticks,len) stamps, so the first local probe cache-MISSES on every origin entry
                // (one content-hash pass over ~4,990 files). Capture the fresh hashes + local
                // stamps and — on the fresh verdict — write them back to search.origin.manifest.json
                // so EVERY subsequent probe is stat-only. Mirrors the combined heal at row 1.
                var writeBack = new Dictionary<string, string>(StringComparer.Ordinal);
                var stampWriteBack = new Dictionary<string, (long Ticks, long Length)>(StringComparer.Ordinal);
                var currentHash = await ComputeOriginHashAsync(originalDir, localOrigin, writeBack, stampWriteBack).ConfigureAwait(false);
                bool fresh = string.Equals(localOrigin.OriginHash, currentHash, StringComparison.Ordinal) &&
                    !(localOrigin.Entries.Count == 0 && Directory.Exists(originalDir));
                if (fresh)
                {
                    await MaybeBackfillOriginContentHashesAsync(root, localOrigin, writeBack, stampWriteBack).ConfigureAwait(false);
                    return false; // origin fresh — zero work (stat-only after the one-time heal)
                }

                // Stale: adopt the bundle over local ONLY when it provably matches live origin.
                var decision = EvaluateOriginBundleAdoption(root, bundleDir ?? "", currentHash);
                if (decision == BundleAdoptionDecision.AdoptOverLocal &&
                    CopyOriginBundleFamilyIntoRoot(root, bundleDir!))
                    return false; // adopted → fresh
                return true; // local origin build owed (last resort)
            }
            catch (IOException)
            {
                return true; // fs race → treat as build-owed (origin has no legacy mtime path)
            }
        }

        // ── origin absent / corrupt / family-guid mismatch → seed FIRST (ordering invariant) ──
        bool seeded = false;
        if (EvaluateOriginBundleAdoption(root, bundleDir ?? "", null) == BundleAdoptionDecision.SeedVirgin)
            seeded = CopyOriginBundleFamilyIntoRoot(root, bundleDir!);

        if (!File.Exists(originMp))
            return true; // nothing seeded, no origin → local build owed

        var man = await LoadLayerManifestAsync(originMp, originBp, root, OriginBuildGuid, "origin").ConfigureAwait(false);
        if (man == null || string.IsNullOrEmpty(man.OriginHash))
            return true;
        try
        {
            // §5.2 heal (seeded branch): the just-seeded origin manifest carries CI-machine stamps;
            // capture fresh local stamps so a seeded-and-fresh origin heals on THIS probe and every
            // later probe is stat-only. Mirrors the combined seeded row-4 heal.
            var writeBack = new Dictionary<string, string>(StringComparer.Ordinal);
            var stampWriteBack = new Dictionary<string, (long Ticks, long Length)>(StringComparer.Ordinal);
            var currentHash = await ComputeOriginHashAsync(originalDir, man, writeBack, stampWriteBack).ConfigureAwait(false);
            bool matches = string.Equals(man.OriginHash, currentHash, StringComparison.Ordinal) &&
                !(man.Entries.Count == 0 && Directory.Exists(originalDir));
            // A seeded origin that matches live is fresh (zero build). A pre-existing origin that
            // reached here only via a corpusfreq-guid mismatch (seeded==false) rebuilds so the
            // partial re-materialises (§2.2a), mirroring the combined Branch-B discipline.
            if (seeded && matches)
            {
                await MaybeBackfillOriginContentHashesAsync(root, man, writeBack, stampWriteBack).ConfigureAwait(false);
                return false;
            }
            return true;
        }
        catch (IOException)
        {
            return true;
        }
    }

    /// <summary>FL5: live origin-corpus hash over <paramref name="originalDir"/> only, reusing the
    /// origin manifest's per-entry <c>(len,ticks)→ContentHash</c> stat cache. When
    /// <paramref name="writeBack"/>/<paramref name="stampWriteBack"/> are supplied, cache-miss
    /// entries (a bundle-adopted manifest's CI stamps) deposit their fresh hash + local stamp so
    /// the caller can heal the manifest (§5.2). Same value recipe as
    /// <see cref="ComputeInputHashAsync"/>.</summary>
    private static Task<string> ComputeOriginHashAsync(
        string originalDir, SearchIndexManifest originManifest,
        IDictionary<string, string>? writeBack = null,
        IDictionary<string, (long Ticks, long Length)>? stampWriteBack = null)
    {
        var cache = BuildContentHashCache(originManifest); // origin entries are all Original → "orig/…"
        return ComputeInputHashAsync(originalDir, Array.Empty<string>(), cache,
            writeBack, CancellationToken.None, stampWriteBack);
    }

    /// <summary>FL5 (§5.2): origin-scoped stat-cache heal — mirrors
    /// <see cref="MaybeBackfillContentHashesAsync"/> but persists to
    /// <c>search.origin.manifest.json</c> (never the combined manifest / Combined cache). A
    /// bundle-adopted origin heals its CI-machine stamps to local <c>(ticks,len)</c> on the first
    /// probe; every later probe is stat-only. Single-writer guarded (shares the backfill flag +
    /// count), never throws.</summary>
    private async Task MaybeBackfillOriginContentHashesAsync(
        string root,
        SearchIndexManifest manifest,
        IReadOnlyDictionary<string, string> writeBack,
        IReadOnlyDictionary<string, (long Ticks, long Length)> stampWriteBack)
    {
        if (writeBack.Count == 0) return;
        if (Interlocked.CompareExchange(ref _contentHashBackfillFlag, 1, 0) != 0) return;
        try
        {
            ApplyContentHashWriteBack(manifest, writeBack, stampWriteBack);
            await SaveOriginContentHashBackfillAsync(root, manifest, CancellationToken.None).ConfigureAwait(false);
            Interlocked.Increment(ref _contentHashBackfillCount);
        }
        catch
        {
            // Best-effort: a failed heal just means the next probe retries.
        }
        finally
        {
            Interlocked.Exchange(ref _contentHashBackfillFlag, 0);
        }
    }

    /// <summary>FL5: atomically rewrites the healed origin manifest (patched per-entry
    /// ContentHash + local stamps) to <c>search.origin.manifest.json</c> via temp-file + rename.
    /// Does NOT touch the Combined manifest cache (an origin manifest is not the served combined
    /// manifest); the split load path re-reads on the file's mtime change.</summary>
    private async Task SaveOriginContentHashBackfillAsync(string root, SearchIndexManifest manifest, CancellationToken ct)
    {
        var final = Path.Combine(root, OriginManifestFileName);
        var tmp = final + ".tmp";
        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct).ConfigureAwait(false);
        ReplaceFileAtomicWithRetry(tmp, final);
    }

    /// <summary>FL5 (§4.1 overlay probe / §1.5): the live overlay family is stale iff its
    /// <c>OverlayHash</c> differs from the live hash over the overlay dir set (translated +
    /// additional) OR its <c>BasedOnOriginStamp</c> no longer equals the current origin
    /// <c>IndexStamp</c>. Absent/guid-stale overlay ⇒ stale. There is no overlay bundle, ever.</summary>
    private async Task<bool> ProbeOverlayAsync(
        string root, IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs, IReadOnlyList<string>? additionalTranslatedDirs)
    {
        var overlayMp = Path.Combine(root, OverlayManifestFileName);
        var overlayBp = Path.Combine(root, OverlayBinFileName);
        if (!File.Exists(overlayMp) || !File.Exists(overlayBp))
            return true; // absent → overlay build owed

        var overlayMan = await LoadLayerManifestAsync(overlayMp, overlayBp, root, OverlayBuildGuid, "overlay").ConfigureAwait(false);
        if (overlayMan == null || !CorpusFreqGuidCurrentAt(root, OverlayCorpusFreqManifestFileName))
            return true; // guid-stale / corrupt / corpusfreq-stale → rebuild

        // §1.5 cross-layer binding: an overlay built against a superseded origin stamp is stale
        // (the origin rel-set it excluded may have changed) — rebuild the overlay, never origin.
        var currentOriginStamp = await ReadOriginIndexStampAsync(root, CancellationToken.None).ConfigureAwait(false);
        if (!string.Equals(overlayMan.BasedOnOriginStamp, currentOriginStamp, StringComparison.Ordinal))
            return true;

        if (string.IsNullOrEmpty(overlayMan.OverlayHash))
            return true; // no stamp to compare against → rebuild

        try
        {
            // Plain recount over the (tiny) overlay dir set — no stat cache. Basis + ordering
            // MUST match the build stamp (BuildLayerCoreAsync overlay basis: translatedDirs, then
            // additionalOriginalDirs, then additionalTranslatedDirs, hashed with an empty origin slot).
            var basis = new List<string>(translatedDirs);
            if (additionalOriginalDirs != null) basis.AddRange(additionalOriginalDirs);
            if (additionalTranslatedDirs != null) basis.AddRange(additionalTranslatedDirs);
            var currentOverlayHash = await ComputeInputHashAsync("", basis, CancellationToken.None).ConfigureAwait(false);
            return !string.Equals(overlayMan.OverlayHash, currentOverlayHash, StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // FL6 (design §6): one-time migration of an existing LEGACY COMBINED root (search.index.* /
    // search.text.* / …, guid search-v8-full-df) to the two-layer split. Path A (bundle-first):
    // adopt the frozen origin bundle by file copy + build the overlay fresh. Path B (carve): a
    // one-time split build that reads ONLY the legacy artifacts (byte-copy bloom, carry text +
    // ContentHashes, recompute grams/tf + corpusfreq from the carried text) — the SAME cost class
    // as one incremental catch-up, NO XML parse for stat-unchanged entries. NEITHER path mass-
    // rebuilds. The legacy family is deleted only after the split family commits; a torn carve
    // rolls back to the (still-servable) legacy family — never a torn/hybrid split root.
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>FL6: the carry-forward source for a Path-B carve — the legacy combined manifest +
    /// text manifest and their bin paths, fed to <see cref="BuildLayerCoreAsync"/> as the "old"
    /// artifacts so the skip-read path carries every stat-unchanged entry with no XML read.</summary>
    private sealed record LayerCarrySource(
        SearchIndexManifest Manifest, SearchTextManifest? TextManifest, string BinPath, string TextBinPath);

    /// <summary>FL6: a migration is owed iff a legacy combined bloom manifest is present AND neither
    /// split-layer manifest exists yet. Pure/cheap (three File.Exists) — safe on the staleness and
    /// build hot paths.</summary>
    private static bool NeedsMigration(string root)
        => File.Exists(Path.Combine(root, ManifestFileName)) &&
           !File.Exists(Path.Combine(root, OriginManifestFileName)) &&
           !File.Exists(Path.Combine(root, OverlayManifestFileName));

    /// <summary>FL6 (§6): the deliberate migration reader — v9 <see cref="TryLoadAsync"/> refuses a
    /// combined manifest for SERVING, but the migration reads it here to carve from. Loads the
    /// legacy combined bloom manifest (guid <see cref="BuildGuid"/>) and its text sidecar manifest
    /// (may be null on a trimmed root — then the carve re-extracts text from XML for those entries).
    /// Returns null when the combined manifest is absent/guid-stale/corrupt.</summary>
    private async Task<LayerCarrySource?> ReadCombinedManifestForMigrationAsync(string root, CancellationToken ct)
    {
        var mp = GetManifestPath(root);
        var bp = GetBinPath(root);
        if (!File.Exists(mp) || !File.Exists(bp)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(mp, Utf8NoBom, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            if (man == null) return null;
            if (man.Version != 1) return null;
            if (!string.Equals(man.BuildGuid, BuildGuid, StringComparison.Ordinal)) return null;
            if (man.Entries == null || man.Entries.Count == 0) return null;

            var textMan = await TryLoadTextManifestAsync(root).ConfigureAwait(false);
            return new LayerCarrySource(man, textMan,
                Path.Combine(root, BinFileName), Path.Combine(root, TextBinFileName));
        }
        catch { return null; }
    }

    /// <summary>FL6: the live origin-corpus hash computed CHEAPLY from the legacy combined manifest's
    /// per-entry ContentHashes (its Original-side entries seed the stat cache — stats, not reads),
    /// so Path A can decide "does the shipped origin bundle match this user's corpus?" without a
    /// read. Same value recipe as the origin build's OriginHash. Null on a filesystem race.</summary>
    private static async Task<string?> ComputeOriginHashFromLegacyAsync(
        string originalDir, SearchIndexManifest legacyCombined, CancellationToken ct)
    {
        try
        {
            var cache = BuildContentHashCache(legacyCombined); // Original entries → "orig/…"
            return await ComputeInputHashAsync(originalDir, Array.Empty<string>(),
                cache: cache, writeBack: null, ct).ConfigureAwait(false);
        }
        catch (IOException) { return null; }
    }

    /// <summary>FL6 (§6): run the combined→split migration inside the caller's _indexIoGate. Path A
    /// when the shipped origin bundle provably matches the live origin corpus (adopt origin family
    /// by file copy, build the overlay fresh); else Path B carve (one-time split build over the
    /// legacy artifacts only). On success the legacy combined family is deleted. On a torn carve the
    /// partial split families are removed and the legacy family is kept (still servable) — never a
    /// torn split root, never a mass rebuild.</summary>
    private async Task MigrateCombinedToSplitAsync(
        string root, string originalDir, IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs, IReadOnlyList<string>? additionalTranslatedDirs,
        IProgress<(int done, int total, string phase)>? progress, CancellationToken ct)
    {
        Interlocked.Exchange(ref _lastBuildFallbackCount, 0);
        Interlocked.Exchange(ref _lastBuildDeltaGuardTripped, 0);
        Interlocked.Exchange(ref _lastBuildXmlReadCount, 0);

        var legacy = await ReadCombinedManifestForMigrationAsync(root, ct).ConfigureAwait(false);
        if (legacy == null)
        {
            // No readable legacy manifest (shouldn't happen — NeedsMigration checked the file):
            // fall back to a normal split build (cold path).
            await BuildSplitAsync(root, originalDir, translatedDirs,
                additionalOriginalDirs, additionalTranslatedDirs, forceRebuild: false, progress, ct).ConfigureAwait(false);
            return;
        }

        progress?.Report((0, 0, "Migrating search index to split format..."));

        try
        {
            // ── Path A: bundle-first. Live origin hash computed cheaply from the legacy manifest's
            //    ContentHashes (stats). If a v9 origin bundle ships and its baked OriginHash ==
            //    live, adopt it (file copy) and build the overlay fresh — zero origin XML reads.
            var bundleDir = ResolveBundleDir();
            var liveOriginHash = await ComputeOriginHashFromLegacyAsync(originalDir, legacy.Manifest, ct).ConfigureAwait(false);
            if (liveOriginHash != null && SplitBundleAvailable(bundleDir))
            {
                var bundleHash = TryReadBundleOriginHash(Path.Combine(bundleDir!, OriginManifestFileName));
                if (bundleHash != null &&
                    string.Equals(bundleHash, liveOriginHash, StringComparison.Ordinal) &&
                    CopyOriginBundleFamilyIntoRoot(root, bundleDir!, null))
                {
                    await BuildOverlayLayerAsync(root, translatedDirs,
                        additionalOriginalDirs, additionalTranslatedDirs, progress, ct).ConfigureAwait(false);
                    // Release any mmap this instance held on the legacy family BEFORE deleting it.
                    InvalidateIndexCaches();
                    DeleteLegacyCombinedFamily(root);
                    return;
                }
            }

            // ── Path B: carve. One-time split build reading ONLY the legacy artifacts. Origin
            //    first (the overlay reads the origin rel-set for §1.2 shadowing + §3.2 inverted
            //    exclusion), then overlay. The delta guard is bypassed inside BuildLayerCoreAsync.
            await BuildLayerCoreAsync(OriginSpec(), root, originalDir, Array.Empty<string>(),
                allowIncremental: false, additionalOriginalDirs: null, additionalTranslatedDirs: null,
                progress, ct, carrySource: legacy).ConfigureAwait(false);

            var originRels = await LoadOriginRelSetAsync(root, ct).ConfigureAwait(false);
            await BuildLayerCoreAsync(OverlaySpec(originRels), root, originalDir: "", translatedDirs,
                allowIncremental: false, additionalOriginalDirs, additionalTranslatedDirs,
                progress, ct, carrySource: legacy).ConfigureAwait(false);

            // Release any mmap this instance held on the legacy family BEFORE deleting it.
            InvalidateIndexCaches();
            DeleteLegacyCombinedFamily(root);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // S5-style torn-carve fallback: remove any partial split families so no torn/hybrid
            // split root can load, and KEEP the legacy combined family (still servable — TryLoadAsync
            // serves it — and a later launch retries the migration). Never a mass rebuild.
            Dbg($"Combined→split migration failed ({ex.Message}); rolling back partial split families, keeping legacy combined.");
            DeleteFamilyFiles(root, "search.origin.*");
            DeleteFamilyFiles(root, "search.overlay.*");
            DeleteStrayFamilyTmpFiles(root);
            InvalidateIndexCaches();
        }
    }

    /// <summary>FL6: delete the legacy combined family (search.index.* / search.text.* /
    /// search.corpusfreq.* / search.inverted.* / search.gramsets.*) after a successful migration.
    /// None of these globs match the origin/overlay names, so the freshly-committed split family is
    /// untouched.</summary>
    private static void DeleteLegacyCombinedFamily(string root)
    {
        foreach (var glob in new[]
        {
            "search.index.*", "search.text.*", "search.corpusfreq.*",
            "search.inverted.*", "search.gramsets.*",
        })
            DeleteFamilyFiles(root, glob);
    }

    /// <summary>FL6: best-effort delete of every non-.tmp file matching <paramref name="glob"/>
    /// directly under <paramref name="root"/>.</summary>
    private static void DeleteFamilyFiles(string root, string glob)
    {
        try
        {
            foreach (var f in Directory.EnumerateFiles(root, glob, SearchOption.TopDirectoryOnly))
            {
                if (f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(f); } catch { }
            }
        }
        catch { }
    }

    public async Task<bool> IsStaleAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null)
    {
        // FL5 (finding B): a split root is judged by the per-layer Origin/Overlay probes, NOT the
        // merged view's null InputHash (which would fall to IsStaleByMtime and hunt the absent
        // combined corpusfreq manifest → churn). The origin probe adopts/seeds as a side effect;
        // the return is simply "does either layer still owe a build?".
        {
            // FL6 note: a legacy combined root is NOT reported migration-stale here — IsStaleAsync
            // keeps its combined verdict so the combined path stays alive (its serving code is
            // retired in FL8, not FL6). The combined→split MIGRATION runs in the build orchestration
            // (BuildOrUpdateAsync → MigrateCombinedToSplitAsync, gated by NeedsMigration): a real
            // user's next rebuild carves/adopts to split with no mass rebuild. A SPLIT root here is
            // judged by the per-layer Origin/Overlay probes.
            var splitBundle = ResolveBundleDir();
            if (UseSplitPath(root, splitBundle))
            {
                var probe = await ProbeLayersAsync(root, originalDir, translatedDirs,
                    additionalOriginalDirs, additionalTranslatedDirs, splitBundle).ConfigureAwait(false);
                return probe.OriginNeedsBuild || probe.OverlayNeedsBuild;
            }
        }

        // ScopeComplete (§2.1a): the search InputHash covers only the ACTIVE corpus
        // (originalDir + translatedDirs), while a build with additional dirs also indexes
        // other corpora whose postings the stamp does not cover. Stamp equality therefore
        // proves full coverage ONLY when no additional dirs are present. Zero-build verdicts
        // (adopt-over-local / seeded-not-stale) are gated on this; with additional corpora
        // present, row 2 degrades to keep-local+catch-up and row 4 degrades to seed+catch-up.
        bool scopeComplete =
            (additionalOriginalDirs == null || additionalOriginalDirs.Count == 0) &&
            (additionalTranslatedDirs == null || additionalTranslatedDirs.Count == 0);

        var bundleDir = ResolveBundleDir();
        var manifestPath = GetManifestPath(root);

        // Determine local usability up front (before any hashing) so we can pick the branch.
        // "Usable" = the bloom manifest loads (guid/version/root gated in TryLoadAsync) AND the
        // corpusfreq sibling stamps the current family guid (§2.2a). A current-bloom root whose
        // corpusfreq is absent/guid-mismatched is treated as a family mismatch → Branch B, so
        // the reseed/rebuild re-materializes corpusfreq rather than serving degraded ranking.
        SearchIndexManifest? localManifest;
        try { localManifest = await TryLoadAsync(root); }
        catch { localManifest = null; }
        bool localFamilyCurrent = localManifest != null && CorpusFreqGuidCurrentAt(root);

        if (localFamilyCurrent)
        {
            // ===== Branch A — loadable local index (serves queries throughout the wait) =====
            // Legacy manifests written before InputHash keep the mtime semantics (no forced
            // rebuild on upgrade). Adoption cannot apply without a comparable live hash.
            if (string.IsNullOrEmpty(localManifest!.InputHash))
                return IsStaleByMtime(manifestPath, originalDir, translatedDirs, localManifest);

            try
            {
                // Hash-aware path: bust only on real content/structure changes, ignoring
                // spurious mtime bumps from git pull / git checkout. The manifest's per-file
                // entries seed a content-hash cache: (LengthBytes, LastWriteUtcTicks) hit →
                // reuse stored ContentHash (no read); miss → fresh SHA256 + write-back.
                var cache = BuildContentHashCache(localManifest);
                var writeBack = new Dictionary<string, string>(StringComparer.Ordinal);
                var stampWriteBack = new Dictionary<string, (long Ticks, long Length)>(StringComparer.Ordinal);

                var currentHash = await ComputeInputHashAsync(originalDir, translatedDirs, cache, writeBack, CancellationToken.None, stampWriteBack).ConfigureAwait(false);

                bool localFresh =
                    string.Equals(localManifest.InputHash, currentHash, StringComparison.Ordinal) &&
                    !(localManifest.Entries.Count == 0 && Directory.Exists(originalDir));

                if (localFresh)
                {
                    // Row 1 — local matches live; bundle untouched. Heal any stale stat stamps.
                    await MaybeBackfillContentHashesAsync(root, localManifest, writeBack, stampWriteBack).ConfigureAwait(false);
                    return false;
                }

                // Local is stale. Adopt the bundle over it ONLY when ScopeComplete AND the
                // bundle provably matches live (row 2); otherwise keep local authoritative and
                // let the incremental catch-up compute the delta (row 3). Adoption over a
                // multi-corpus local index is refused because the bundle lacks corpus B's
                // postings (§2.1a).
                if (scopeComplete)
                {
                    var decision = EvaluateBundleAdoption(root, bundleDir ?? "", currentHash);
                    if (decision == BundleAdoptionDecision.AdoptOverLocal &&
                        CopyBundleFamilyIntoRoot(root, bundleDir!))
                    {
                        // Row 2 — zero build. The seeded manifest's CI ticks heal on the next
                        // probe via the existing stampWriteBack path.
                        return false;
                    }
                    // Adoption declined or the copy rolled back → fall through to keep-local.
                }
                return true; // Row 3 — keep local, incremental catch-up.
            }
            catch (IOException)
            {
                // Filesystem race — fall back to mtime so a startup hiccup never crashes the probe.
                return IsStaleByMtime(manifestPath, originalDir, translatedDirs, localManifest);
            }
        }

        // ===== Branch B — local absent / corrupt / family-guid mismatch =====
        // Seed the bundle FIRST, before any hashing (ordering invariant §2.2): search becomes
        // instant from the seeded family (the query path loads it independently of this probe);
        // the hash pass below only decides whether a catch-up is owed. CopyBundleFamilyIntoRoot
        // is unconditional here — the decision gate is EvaluateBundleAdoption with a null live
        // hash, which yields SeedVirgin for a usable current-family bundle (and deletes any
        // corrupt/leftover local search.* files that the bundle does not replace).
        bool seeded = false;
        if (EvaluateBundleAdoption(root, bundleDir ?? "", null) == BundleAdoptionDecision.SeedVirgin)
            seeded = CopyBundleFamilyIntoRoot(root, bundleDir!);

        // Nothing on disk and nothing seeded → cold full build (row 6, today's behavior).
        if (!File.Exists(manifestPath))
            return true;

        SearchIndexManifest? manifest;
        try { manifest = await TryLoadAsync(root); }
        catch { return true; }
        if (manifest == null)
            return true; // pre-existing corrupt/guid-mismatch manifest, no usable bundle → rebuild.

        try
        {
            if (string.IsNullOrEmpty(manifest.InputHash))
                return IsStaleByMtime(manifestPath, originalDir, translatedDirs, manifest);

            var cache = BuildContentHashCache(manifest);
            var writeBack = new Dictionary<string, string>(StringComparer.Ordinal);
            var stampWriteBack = new Dictionary<string, (long Ticks, long Length)>(StringComparer.Ordinal);

            var currentHash = await ComputeInputHashAsync(originalDir, translatedDirs, cache, writeBack, CancellationToken.None, stampWriteBack).ConfigureAwait(false);

            bool matchesLive =
                string.Equals(manifest.InputHash, currentHash, StringComparison.Ordinal) &&
                !(manifest.Entries.Count == 0 && Directory.Exists(originalDir));

            // Row 4 — the seeded family matches live and the corpus is single-scope: zero build.
            // A pre-existing (not-seeded) manifest can only reach here when it is bloom-current
            // but corpusfreq-stale (Branch B via family gate); seeded is false there, so it
            // correctly falls through to stale → rebuild, re-materializing corpusfreq (§2.2a).
            if (seeded && matchesLive && scopeComplete)
            {
                await MaybeBackfillContentHashesAsync(root, manifest, writeBack, stampWriteBack).ConfigureAwait(false);
                return false;
            }

            // Rows 5 / 6 — catch-up build folds in the delta and/or the additional corpora.
            return true;
        }
        catch (IOException)
        {
            return IsStaleByMtime(manifestPath, originalDir, translatedDirs, manifest);
        }
    }

    /// <summary>
    /// Builds the (namespaced relPath → SearchIndexEntry) lookup used by the hash worker
    /// to skip re-hashing unchanged files. Original entries get the <c>orig/</c> prefix;
    /// translated entries get <c>tran0/</c>. Matches the namespacing in <see cref="AppendDirRows"/>.
    ///
    /// <para><b>Limitation:</b> additional translated dirs (index ≥ 1) are not represented
    /// here because the existing manifest schema only distinguishes Side, not which translated
    /// dir the entry came from. Those files take the cache-miss path on every check — a tiny
    /// regression on multi-dir setups, acceptable until the schema grows a NamespaceIndex
    /// field. Most installs have a single translated dir.</para>
    /// </summary>
    private static IReadOnlyDictionary<string, SearchIndexEntry> BuildContentHashCache(SearchIndexManifest manifest)
    {
        var cache = new Dictionary<string, SearchIndexEntry>(manifest.Entries.Count, StringComparer.Ordinal);
        foreach (var e in manifest.Entries)
        {
            // Convert raw RelPath to namespaced form expected by ComputeInputHashCore.
            var ns = e.Side == SearchSide.Original ? "orig" : "tran0";
            var rel = ns + "/" + e.RelPath.Replace('\\', '/');
            // Last writer wins on duplicates (shouldn't happen with valid manifests).
            cache[rel] = e;
        }
        return cache;
    }

    /// <summary>
    /// Copies fresh per-file hashes from the write-back dict onto the manifest's entries.
    /// Mirrors the namespacing logic in <see cref="BuildContentHashCache"/>.
    /// </summary>
    private static void ApplyContentHashWriteBack(
        SearchIndexManifest manifest,
        IReadOnlyDictionary<string, string> writeBack,
        IReadOnlyDictionary<string, (long Ticks, long Length)>? stampWriteBack = null)
    {
        foreach (var e in manifest.Entries)
        {
            var ns = e.Side == SearchSide.Original ? "orig" : "tran0";
            var rel = ns + "/" + e.RelPath.Replace('\\', '/');
            if (writeBack.TryGetValue(rel, out var freshHash))
            {
                e.ContentHash = freshHash;
            }
            // Refresh the stat stamps too, so a seeded manifest (whose entries carry the CI
            // machine's ticks) heals after the first probe instead of re-reading + re-hashing
            // the whole corpus on every subsequent staleness check.
            if (stampWriteBack != null && stampWriteBack.TryGetValue(rel, out var stamp))
            {
                e.LastWriteUtcTicks = stamp.Ticks;
                e.LengthBytes = stamp.Length;
            }
        }
    }

    /// <summary>
    /// Atomically writes the patched manifest (with newly-populated ContentHash fields) to
    /// disk via the temp-file + rename pattern. Preserves the existing InputHash + BuildGuid +
    /// Entries shape — only ContentHash fields change. Refreshes the in-memory manifest cache
    /// on success so subsequent searches see the patched entries without re-reading.
    /// </summary>
    private async Task SaveContentHashBackfillAsync(string root, SearchIndexManifest manifest, CancellationToken ct)
    {
        var final = GetManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct).ConfigureAwait(false);

        ReplaceFileAtomicWithRetry(tmp, final);

        try
        {
            var full = Path.GetFullPath(final);
            var writeUtc = File.GetLastWriteTimeUtc(full);
            lock (_indexCacheLock)
            {
                Combined.CachedManifest = manifest;
                Combined.CachedManifestPath = full;
                Combined.CachedManifestWriteUtc = writeUtc;
            }
        }
        catch
        {
            // harmless — cache will refresh on next load
        }
    }

    private static bool IsStaleByMtime(
        string manifestPath,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        SearchIndexManifest manifest)
    {
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

    /// <summary>
    /// Content-based hash of every <c>*.xml</c> file in <paramref name="originalDir"/> and each
    /// <paramref name="translatedDirs"/>. Each file's bytes are SHA256'd; the per-file digests are
    /// combined with their namespace-prefixed relative paths and SHA256'd again into a root hash.
    /// Sorted by relPath (OrdinalIgnoreCase) so enumeration order is irrelevant.
    ///
    /// <para>Designed to ignore filesystem operations that preserve file content (git pull, git
    /// checkout, branch switches that bump mtime without changing bytes) and to detect every real
    /// content edit, regardless of whether file length stayed the same. Reading every file makes
    /// this O(corpus-bytes); expect ~100–300ms on commodity hardware for a 50–200MB corpus.</para>
    /// </summary>
    internal static async Task<string> ComputeInputHashAsync(
        string originalDir,
        IEnumerable<string> translatedDirs,
        CancellationToken ct)
        => await ComputeInputHashAsync(originalDir, translatedDirs, cache: null, writeBack: null, ct).ConfigureAwait(false);

    /// <summary>
    /// Cache-aware overload: when <paramref name="cache"/> is provided (keyed by namespaced
    /// relPath, e.g. <c>"orig/foo/bar.xml"</c>), per-file content hashes are reused without
    /// re-reading the file as long as the on-disk <c>(LengthBytes, LastWriteUtcTicks)</c> still
    /// match the cached entry. Cache misses (mtime / length changed, file new, or cached
    /// <see cref="SearchIndexEntry.ContentHash"/> is null) fall back to a fresh SHA256 over the
    /// file bytes; if <paramref name="writeBack"/> is non-null those fresh hashes are deposited
    /// into it so the caller can persist them back to the manifest.
    ///
    /// <para><b>Invariant:</b> The returned root hash is byte-identical between a fully populated
    /// cache (no file reads) and an empty cache (every file read) for the same corpus content.
    /// The cache is purely an optimization.</para>
    /// </summary>
    internal static async Task<string> ComputeInputHashAsync(
        string originalDir,
        IEnumerable<string> translatedDirs,
        IReadOnlyDictionary<string, SearchIndexEntry>? cache,
        IDictionary<string, string>? writeBack,
        CancellationToken ct,
        IDictionary<string, (long Ticks, long Length)>? stampWriteBack = null)
    {
        // Enumerate file metadata off the caller's thread — directory walks can be
        // multi-second on cold-disk corpora.
        return await Task.Run(() => ComputeInputHashCore(originalDir, translatedDirs, cache, writeBack, stampWriteBack, ct), ct).ConfigureAwait(false);
    }

    private static string ComputeInputHashCore(
        string originalDir,
        IEnumerable<string> translatedDirs,
        IReadOnlyDictionary<string, SearchIndexEntry>? cache,
        IDictionary<string, string>? writeBack,
        IDictionary<string, (long Ticks, long Length)>? stampWriteBack,
        CancellationToken ct)
    {
        // Content-hash basis. Per-file SHA256 of the file's bytes, combined with the
        // namespace-prefixed relative path into a final SHA256 root. Mtime is NOT in
        // the basis — that was the bug: a git pull / git checkout that bumps mtime
        // without changing content would otherwise bust the cache and trigger a
        // multi-minute reindex. Hashing content directly makes the check robust to
        // any filesystem operation that preserves file bytes.
        var rows = new List<(string rel, byte[] contentHash)>();

        AppendDirRows(rows, originalDir, "orig", cache, writeBack, stampWriteBack, ct);
        if (translatedDirs != null)
        {
            int i = 0;
            foreach (var tDir in translatedDirs)
            {
                AppendDirRows(rows, tDir, "tran" + i, cache, writeBack, stampWriteBack, ct);
                i++;
            }
        }

        rows.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.rel, b.rel));

        // Canonical binary form per row: <utf8(rel) length as int32 LE><utf8(rel) bytes><32-byte SHA256>.
        // Length-prefixed paths avoid any ambiguity from special characters (newlines, pipes)
        // in legal POSIX filenames.
        using var stream = new MemoryStream(rows.Count * 80);
        foreach (var (rel, contentHash) in rows)
        {
            var relBytes = Encoding.UTF8.GetBytes(rel);
            stream.Write(BitConverter.GetBytes(relBytes.Length));
            stream.Write(relBytes);
            stream.Write(contentHash);
        }
        stream.Position = 0;
        var rootBytes = System.Security.Cryptography.SHA256.HashData(stream.ToArray());
        return Convert.ToHexString(rootBytes).ToLowerInvariant();
    }

    private static void AppendDirRows(
        List<(string rel, byte[] contentHash)> rows,
        string dir,
        string namespacePrefix,
        IReadOnlyDictionary<string, SearchIndexEntry>? cache,
        IDictionary<string, string>? writeBack,
        IDictionary<string, (long Ticks, long Length)>? stampWriteBack,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
        foreach (var f in Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            // Namespace-prefix the relative path so a file with the same relative name in
            // originalDir and translatedDirs[0] hashes to distinct rows.
            var rel = namespacePrefix + "/" + Path.GetRelativePath(dir, f).Replace('\\', '/');

            // Cache lookup: only reuse the stored hash when (length, mtime) match. Any
            // mismatch — including a legacy entry whose ContentHash is null — falls
            // through to a fresh read+hash.
            if (cache != null && cache.TryGetValue(rel, out var cached) && !string.IsNullOrEmpty(cached.ContentHash))
            {
                try
                {
                    var fi = new FileInfo(f);
                    if (fi.Exists &&
                        fi.Length == cached.LengthBytes &&
                        fi.LastWriteTimeUtc.Ticks == cached.LastWriteUtcTicks)
                    {
                        // Cache hit — no file read needed.
                        rows.Add((rel, Convert.FromHexString(cached.ContentHash)));
                        continue;
                    }
                }
                catch (IOException) { /* fall through to fresh hash */ }
                catch (UnauthorizedAccessException) { /* fall through to fresh hash */ }
                catch (FormatException) { /* malformed hex — re-hash */ }
            }

            byte[] contentHash;
            long lengthBytes;
            try
            {
                using var fs = File.OpenRead(f);
                lengthBytes = fs.Length;
                contentHash = System.Security.Cryptography.SHA256.HashData(fs);
            }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            rows.Add((rel, contentHash));

            // Cache-miss path: capture the fresh hash for write-back so the next stale
            // check can take the fast path. Hex-encoded to match SearchIndexEntry.ContentHash.
            if (writeBack != null)
            {
                writeBack[rel] = Convert.ToHexString(contentHash).ToLowerInvariant();
            }

            // Also capture the on-disk (mtime ticks, length) so a caller backfilling a
            // seeded manifest can heal the stat cache: seeded entries carry the CI build
            // machine's ticks, which never match the local clone, so every file misses the
            // stat check on every probe until these local stamps are written back.
            if (stampWriteBack != null)
            {
                long ticks = 0;
                try { ticks = File.GetLastWriteTimeUtc(f).Ticks; } catch { /* leave 0; a 0 stamp just re-misses, never wrong */ }
                stampWriteBack[rel] = (ticks, lengthBytes);
            }
        }
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
        bool htmlDecodeIfAmpersandPresent,
        string textBinFileName,
        SearchFamily textFamily)
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

        if (!TryReadSearchableTextFromSidecar(root, key, textEntry, textBinFileName, textFamily, out string searchable))
        {
            string xml;
            // FL7 (§5.3): sidecar miss → parse XML from disk. Count it so tests can prove the
            // origin text sidecar (once materialized) actually serves origin verify/snippet reads
            // (this stays 0) vs. the absent-sidecar fallback (this fires per origin verify).
            try { xml = File.ReadAllText(absPath, Utf8NoBom); Interlocked.Increment(ref _lastVerifyXmlReadCount); }
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
        string textBinFileName,
        SearchFamily textFamily,
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
            // FL4: route to the OWNING layer's text sidecar (combined → search.text.bin;
            // origin → search.origin.text.bin; overlay → search.overlay.text.bin) and use that
            // family's mmap cache slot so combined + split roots never thrash one slot.
            string textBinPath = Path.Combine(root, textBinFileName);
            var full = Path.GetFullPath(textBinPath);
            if (!File.Exists(full)) return false;

            var fileLen = new FileInfo(full).Length;
            long end = textEntry.TextOffset + textEntry.TextLengthBytes;
            if (textEntry.TextOffset < 0 || end < textEntry.TextOffset || end > fileLen)
                return false;

            using var accessor = GetOrCreateTextMappedFile(full, File.GetLastWriteTimeUtc(full), textFamily)
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
    internal static bool IsIndexableCjk(char ch) => ReadZen.App.Infrastructure.CjkText.IsIdeograph(ch);

    /// <summary>
    /// PR2 (skip-verify hybrid): returns true iff the query is exactly 2 characters
    /// and both are indexable CJK code points. Used to gate the hybrid path where
    /// the bigram inverted index already proves adjacency, so VerifyFileAllHits is
    /// run only for snippet collection (top-N) and skipped for the long tail.
    /// </summary>
    /// <remarks>
    /// Length is measured in <see cref="char"/> (UTF-16 code units), so a single
    /// astral CJK Ext-B/C/... character occupies two chars (a surrogate pair) and
    /// would *coincidentally* match Length==2 \u2014 but neither surrogate half satisfies
    /// <see cref="IsIndexableCjk(char)"/> (which only covers BMP CJK + Ext-A + Compat),
    /// so the predicate correctly returns false for a surrogate pair. The corpus
    /// uses BMP CJK exclusively, so this is the practical case.
    /// </remarks>
    internal static bool IsTwoCharCjk(string? q)
    {
        if (q == null || q.Length != 2) return false;
        return IsIndexableCjk(q[0]) && IsIndexableCjk(q[1]);
    }

    // ---------------------------
    // FAST body extraction / normalization (NO REGEX)
    // ---------------------------

    internal static string MakeSearchableTextFromXml_Fast(string xml, bool htmlDecodeIfAmpersandPresent)
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
        int appSkipDepth = 0; // when >0, skip text inside <app> (critical apparatus variants)
        int tagStart = -1; // start of tag content (after '<')

        for (int i = iStart + 1; i < iEnd; i++)
        {
            char ch = xml[i];

            if (inTag)
            {
                if (ch == '>')
                {
                    // Check tag name for <app> / </app>
                    if (tagStart >= 0)
                    {
                        int tagContentLen = i - tagStart;
                        if (tagContentLen >= 3)
                        {
                            bool isClose = xml[tagStart] == '/';
                            bool isSelfClose = i > 0 && xml[i - 1] == '/';
                            int nameStart = isClose ? tagStart + 1 : tagStart;
                            // Check if tag name starts with "app"
                            if (i - nameStart >= 3 &&
                                xml[nameStart] == 'a' && xml[nameStart + 1] == 'p' && xml[nameStart + 2] == 'p' &&
                                (i - nameStart == 3 || nameStart + 3 >= i || xml[nameStart + 3] == ' ' || xml[nameStart + 3] == '>' || xml[nameStart + 3] == '/' || xml[nameStart + 3] == '\t' || xml[nameStart + 3] == '\n'))
                            {
                                if (isSelfClose)
                                {
                                    // <app/> is a no-op for skip-depth (both open and close in one tag).
                                    // Without this guard, appSkipDepth++ would fire unbalanced and
                                    // silently suppress all text after the self-closing apparatus anchor.
                                }
                                else if (isClose)
                                {
                                    appSkipDepth = Math.Max(0, appSkipDepth - 1);
                                }
                                else
                                {
                                    appSkipDepth++;
                                }
                            }
                        }
                    }
                    inTag = false;
                    tagStart = -1;
                }
                continue;
            }

            if (ch == '<')
            {
                inTag = true;
                tagStart = i + 1;
                if (!prevSpace && appSkipDepth == 0)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
                continue;
            }

            if (appSkipDepth > 0) continue;

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
        // FL4 (design §4.3): if the root holds a split family — search.origin.manifest.json
        // (LayerRole=="origin", guid search-v9-origin) AND search.overlay.manifest.json — load
        // BOTH layers and return an in-memory MERGED view (Entries = origin ∪ overlay). Only a
        // fully-formed, guid-current split pair engages this path; anything short of it falls
        // through to the combined loader below, which behaves EXACTLY as pre-FL4.
        if (File.Exists(Path.Combine(root, OriginManifestFileName)) &&
            File.Exists(Path.Combine(root, OverlayManifestFileName)))
        {
            var merged = await TryLoadSplitAsync(root).ConfigureAwait(false);
            if (merged != null)
                return merged;
            // Split pair present but not loadable (guid mismatch / torn) → do not silently serve
            // a combined family for a split root; refuse (mirrors the guid gate discipline).
            return null;
        }

        try
        {
            var mp = GetManifestPath(root);
            var bp = GetBinPath(root);

            if (!File.Exists(mp) || !File.Exists(bp))
                return null;

            // A combined root: make sure the query-path family list is the combined singleton
            // (a prior split load on this instance may have left it as [origin, overlay]).
            ResetToCombinedFamilies();

            var mpFull = Path.GetFullPath(mp);
            var mpWriteUtc = File.GetLastWriteTimeUtc(mpFull);

            // Fast path: cached manifest still matches file timestamp
            lock (_indexCacheLock)
            {
                if (Combined.CachedManifest != null &&
                    string.Equals(Combined.CachedManifestPath, mpFull, StringComparison.OrdinalIgnoreCase) &&
                    Combined.CachedManifestWriteUtc == mpWriteUtc)
                {
                    return Combined.CachedManifest;
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
                Combined.CachedManifest = man;
                Combined.CachedManifestPath = mpFull;
                Combined.CachedManifestWriteUtc = mpWriteUtc;
            }

            // Try loading inverted index alongside bloom. Only a file stamped with THIS
            // manifest's IndexStamp may load: a stale file (rebuild failed after the
            // manifest committed) or a torn save would otherwise be trusted as the
            // "0% false positive" candidate source and silently drop documents from
            // search results. Old manifests without a stamp get no inverted index
            // until their next rebuild (bloom + verify covers them).
            if (InvertedIndex == null && !string.IsNullOrEmpty(man.IndexStamp))
            {
                try
                {
                    var invPath = Path.Combine(root, Combined.InvertedBinFileName);
                    var inv = new InvertedSearchIndex();
                    if (await inv.TryLoadAsync(invPath, man.IndexStamp, CancellationToken.None))
                    {
                        Combined.Inverted = inv;
                        Dbg($"Inverted index loaded: {inv.TermCount} terms, {inv.DocCount} docs");
                    }
                    else if (File.Exists(invPath))
                    {
                        Dbg("Inverted index present but refused (stamp/checksum mismatch or old format) — using bloom + verify");
                    }
                }
                catch { /* inverted index is optional */ }
            }

            // Try loading corpus frequency index alongside bloom. Only an artifact
            // stamped with THIS manifest's IndexStamp may load (same-build binding).
            if (CorpusCharFreqs == null)
            {
                try { await TryLoadCorpusFrequenciesAsync(root, man.IndexStamp); }
                catch { /* corpus freq index is optional */ }
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>FL4: reset the query-path family list to the combined singleton if a prior
    /// split load left it as [origin, overlay]. Disposes the split families' mmap slots and
    /// drops the merged-view cache. No-op for an already-combined instance.</summary>
    private void ResetToCombinedFamilies()
    {
        lock (_indexCacheLock)
        {
            if (!_splitLoaded && _families.Count == 1 && ReferenceEquals(_families[0], _combinedFamily))
                return;

            foreach (var fam in _families)
                if (!ReferenceEquals(fam, _combinedFamily))
                    fam.InvalidateCaches();

            _families = new List<SearchFamily> { _combinedFamily };
            _splitLoaded = false;
            _cachedMergedManifest = null;
            _cachedMergedOriginPath = null;
            _cachedMergedOriginWriteUtc = default;
            _cachedMergedOverlayPath = null;
            _cachedMergedOverlayWriteUtc = default;
        }
        InvalidateMergedCorpusFreqs();
    }

    /// <summary>
    /// FL4 (design §4.3): load a SPLIT root — the frozen origin family (search.origin.*) and
    /// the live overlay family (search.overlay.*) — into two <see cref="SearchFamily"/> query
    /// members and return an in-memory MERGED manifest view (Entries = origin ∪ overlay, each
    /// entry kept under its owning family; IndexStamp = origin stamp for diagnostics). The
    /// per-layer inverted indexes + corpusfreq partials are loaded onto their families so the
    /// query path's disjoint-union merge (§2.2) and additive corpusfreq fold (§2.3) engage.
    /// Returns null (caller refuses the root) if either layer manifest is absent, guid-stale,
    /// mis-roled, or fails bloom-offset validation.
    /// </summary>
    private async Task<SearchIndexManifest?> TryLoadSplitAsync(string root)
    {
        try
        {
            var originMp = Path.Combine(root, OriginManifestFileName);
            var originBp = Path.Combine(root, OriginBinFileName);
            var overlayMp = Path.Combine(root, OverlayManifestFileName);
            var overlayBp = Path.Combine(root, OverlayBinFileName);

            if (!File.Exists(originMp) || !File.Exists(originBp) ||
                !File.Exists(overlayMp) || !File.Exists(overlayBp))
                return null;

            var originMpFull = Path.GetFullPath(originMp);
            var overlayMpFull = Path.GetFullPath(overlayMp);
            var originWriteUtc = File.GetLastWriteTimeUtc(originMpFull);
            var overlayWriteUtc = File.GetLastWriteTimeUtc(overlayMpFull);

            // Fast path: both layer manifests unchanged since the last split load → return the
            // cached merged view; the origin/overlay families are already warm.
            lock (_indexCacheLock)
            {
                if (_splitLoaded && _cachedMergedManifest != null &&
                    string.Equals(_cachedMergedOriginPath, originMpFull, StringComparison.OrdinalIgnoreCase) &&
                    _cachedMergedOriginWriteUtc == originWriteUtc &&
                    string.Equals(_cachedMergedOverlayPath, overlayMpFull, StringComparison.OrdinalIgnoreCase) &&
                    _cachedMergedOverlayWriteUtc == overlayWriteUtc)
                {
                    return _cachedMergedManifest;
                }
            }

            var originMan = await LoadLayerManifestAsync(originMp, originBp, root, OriginBuildGuid, "origin").ConfigureAwait(false);
            if (originMan == null) return null;
            var overlayMan = await LoadLayerManifestAsync(overlayMp, overlayBp, root, OverlayBuildGuid, "overlay").ConfigureAwait(false);
            if (overlayMan == null) return null;

            // FL5 finding A (§1.5 / risk #3): the load-time cross-layer binding guard. An overlay
            // built against a superseded origin stamp excluded a DIFFERENT origin rel-set from its
            // inverted feed (§3.2); serving it against the current origin would double-serve or
            // drop rels that changed layers (the tf-collision the FL4 review found). Refuse the
            // split root until the overlay is rebuilt against the current origin (IsStaleAsync's
            // overlay probe returns stale for exactly this case → overlay-only rebuild, cheap).
            if (!string.Equals(overlayMan.BasedOnOriginStamp, originMan.IndexStamp, StringComparison.Ordinal))
            {
                Dbg($"Split root refused: overlay BasedOnOriginStamp ({overlayMan.BasedOnOriginStamp}) " +
                    $"!= origin IndexStamp ({originMan.IndexStamp}) — overlay rebuild owed.");
                return null;
            }

            // Build the two query families (self-describing file names). Fresh instances each
            // split load — the inverted + corpusfreq handles below populate them.
            var originFam = new SearchFamily(
                binFileName: OriginBinFileName,
                invertedBinFileName: OriginInvertedBinFileName,
                textManifestFileName: OriginTextManifestFileName,
                textBinFileName: OriginTextBinFileName,
                corpusFreqManifestFileName: OriginCorpusFreqManifestFileName,
                corpusFreqBinFileName: OriginCorpusFreqBinFileName)
            {
                CachedManifest = originMan,
                CachedManifestPath = originMpFull,
                CachedManifestWriteUtc = originWriteUtc,
            };
            var overlayFam = new SearchFamily(
                binFileName: OverlayBinFileName,
                invertedBinFileName: OverlayInvertedBinFileName,
                textManifestFileName: OverlayTextManifestFileName,
                textBinFileName: OverlayTextBinFileName,
                corpusFreqManifestFileName: OverlayCorpusFreqManifestFileName,
                corpusFreqBinFileName: OverlayCorpusFreqBinFileName)
            {
                CachedManifest = overlayMan,
                CachedManifestPath = overlayMpFull,
                CachedManifestWriteUtc = overlayWriteUtc,
            };

            // Load each layer's inverted index against ITS OWN manifest stamp (same-build
            // binding; a stale/torn per-layer inverted is refused, leaving that layer to the
            // bloom fallback — §2.2 conservative rule then makes the whole query fall back).
            await TryLoadLayerInvertedAsync(root, originFam, originMan.IndexStamp).ConfigureAwait(false);
            await TryLoadLayerInvertedAsync(root, overlayFam, overlayMan.IndexStamp).ConfigureAwait(false);

            // Load each layer's corpusfreq partial (stamped with that layer's IndexStamp). The
            // FL2 additive fold turns the two partials into the corpus-global view.
            try { await TryLoadCorpusFrequenciesIntoFamilyAsync(root, originMan.IndexStamp, originFam); } catch { }
            try { await TryLoadCorpusFrequenciesIntoFamilyAsync(root, overlayMan.IndexStamp, overlayFam); } catch { }

            // Merged manifest view: entries are the disjoint union of both layers (partition
            // invariant §1.2 → no (rel, side) collision). Origin entries first so any
            // impossible dup resolves origin-wins downstream (entryMap.ToDictionary keep-first).
            var mergedEntries = new List<SearchIndexEntry>(originMan.Entries.Count + overlayMan.Entries.Count);
            mergedEntries.AddRange(originMan.Entries);
            mergedEntries.AddRange(overlayMan.Entries);

            var merged = new SearchIndexManifest
            {
                Version = originMan.Version,
                RootPath = root,
                BuiltUtc = originMan.BuiltUtc,
                BloomBits = BloomBits,
                BloomHashCount = BloomHashCount,
                BuildGuid = originMan.BuildGuid,
                IndexStamp = originMan.IndexStamp,   // diagnostics only
                LayerRole = null,
                Entries = mergedEntries,
            };

            lock (_indexCacheLock)
            {
                _families = new List<SearchFamily> { originFam, overlayFam };
                _splitLoaded = true;
                _cachedMergedManifest = merged;
                _cachedMergedOriginPath = originMpFull;
                _cachedMergedOriginWriteUtc = originWriteUtc;
                _cachedMergedOverlayPath = overlayMpFull;
                _cachedMergedOverlayWriteUtc = overlayWriteUtc;
            }
            // A family partial changed → re-fold the corpus-global corpusfreq on next access.
            InvalidateMergedCorpusFreqs();

            Dbg($"Split root loaded: origin={originMan.Entries.Count} entries, overlay={overlayMan.Entries.Count} entries, " +
                $"originInverted={originFam.Inverted?.IsLoaded == true}, overlayInverted={overlayFam.Inverted?.IsLoaded == true}");

            return merged;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>FL4: read + validate one split-layer bloom manifest (guid/role/root/bloom
    /// geometry + per-entry bloom-offset bounds against its own bin). Null on any mismatch.</summary>
    private async Task<SearchIndexManifest?> LoadLayerManifestAsync(
        string manifestPath, string binPath, string root, string expectedGuid, string expectedRole)
    {
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, Utf8NoBom).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            if (man == null) return null;

            if (!string.Equals(Path.GetFullPath(man.RootPath), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return null;
            if (man.Version != 1) return null;
            if (!string.Equals(man.BuildGuid, expectedGuid, StringComparison.Ordinal)) return null;
            if (!string.Equals(man.LayerRole, expectedRole, StringComparison.Ordinal)) return null;
            if (man.BloomBits != BloomBits || man.BloomHashCount != BloomHashCount) return null;
            if (man.Entries == null) return null;

            var binLen = new FileInfo(binPath).Length;
            foreach (var e in man.Entries)
            {
                if (e.BloomOffset < 0 || e.BloomOffset + BloomBytes > binLen)
                    return null;
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>FL4: load one layer's inverted index onto its family, gated on the layer's
    /// IndexStamp (same-build binding). Silent on absence/refusal — the family simply has no
    /// usable inverted index and the query path's conservative rule (§2.2) handles it.</summary>
    private async Task TryLoadLayerInvertedAsync(string root, SearchFamily fam, string? indexStamp)
    {
        if (string.IsNullOrEmpty(indexStamp)) return;
        try
        {
            var invPath = Path.Combine(root, fam.InvertedBinFileName);
            var inv = new InvertedSearchIndex();
            if (await inv.TryLoadAsync(invPath, indexStamp, CancellationToken.None).ConfigureAwait(false))
                fam.Inverted = inv;
        }
        catch { /* inverted index is optional */ }
    }

    /// <summary>
    /// Loads the corpus frequency index from disk. Returns true on success.
    /// <paramref name="expectedStamp"/> is the loaded main manifest's IndexStamp; the
    /// artifact is refused (props left null) when either stamp is null (legacy file or
    /// legacy main manifest) or the stamps differ (Ordinal) — a crash between the main
    /// manifest commit and the corpusfreq save must not leave a previous build's
    /// frequencies silently trusted for ranking.
    /// </summary>
    public Task<bool> TryLoadCorpusFrequenciesAsync(string root, string? expectedStamp)
        => TryLoadCorpusFrequenciesIntoFamilyAsync(root, expectedStamp, Combined);

    /// <summary>FL4: corpusfreq load core, parametrized by the owning family. The combined
    /// family reads search.corpusfreq.*; the origin/overlay families read their own partials
    /// (search.origin.corpusfreq.* / search.overlay.corpusfreq.*). Sets the family's
    /// CharFreqs/BigramFreqs/TotalChars and invalidates the merged fold. Byte-identical to the
    /// pre-FL4 body when <paramref name="fam"/> == Combined.</summary>
    private async Task<bool> TryLoadCorpusFrequenciesIntoFamilyAsync(string root, string? expectedStamp, SearchFamily fam)
    {
        var manifestPath = Path.Combine(root, fam.CorpusFreqManifestFileName);
        var binPath = Path.Combine(root, fam.CorpusFreqBinFileName);

        if (!File.Exists(manifestPath) || !File.Exists(binPath))
            return false;

        if (string.IsNullOrEmpty(expectedStamp))
            return false;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, Utf8NoBom);
            if (string.IsNullOrWhiteSpace(json)) return false;

            var freqManifest = JsonSerializer.Deserialize<CorpusFreqManifest>(json, JsonOpts);
            if (freqManifest == null || freqManifest.Version != 1) return false;
            if (!string.Equals(freqManifest.BuildGuid, CorpusFreqBuildGuid, StringComparison.Ordinal)) return false;
            if (freqManifest.IndexStamp == null ||
                !string.Equals(freqManifest.IndexStamp, expectedStamp, StringComparison.Ordinal))
            {
                Dbg("Corpus freq index refused: IndexStamp missing or mismatched (stale sibling)");
                return false;
            }

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

            fam.CharFreqs = charFreqs;
            fam.BigramFreqs = bigramFreqs;
            fam.TotalChars = totalChars;
            InvalidateMergedCorpusFreqs(); // FL2: a family partial changed → re-fold on next getter access

            Dbg($"Corpus freq index loaded: {charCount} chars, {bigramCount} bigrams, {totalChars} total");
            return true;
        }
        catch (Exception ex)
        {
            Dbg($"Corpus freq load failed: {ex.Message}");
            return false;
        }
    }

    public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root)
        => TryLoadTextManifestForFamilyAsync(root, Combined);

    /// <summary>FL4 (design §2.5): per-(rel, side) routing target for a verify/snippet read —
    /// the text sidecar entry plus which layer's text.bin + mmap-cache family owns it.</summary>
    private sealed class TextRoute
    {
        public SearchTextEntry? Entry;
        public string TextBinFileName = "";
        public SearchFamily Family = null!;
    }

    /// <summary>
    /// FL4 (design §2.5): union the per-family text sidecars into one (rel, side) → route map.
    /// Combined root → one entry per (rel, side) routed to search.text.bin/Combined (byte-
    /// identical to the pre-FL4 single textEntryMap). Split root → origin sidecar entries route
    /// to search.origin.text.bin/originFam and overlay entries to search.overlay.text.bin/
    /// overlayFam; the partition invariant (§1.2) guarantees no (rel, side) key collision.
    /// </summary>
    private async Task<Dictionary<(string rel, SearchSide side), TextRoute>> BuildTextRouteMapAsync(
        string root, IReadOnlyList<SearchFamily> families)
    {
        var map = new Dictionary<(string rel, SearchSide side), TextRoute>(new RelSideComparer());
        foreach (var fam in families)
        {
            SearchTextManifest? tm;
            try { tm = await TryLoadTextManifestForFamilyAsync(root, fam).ConfigureAwait(false); }
            catch { tm = null; } // Sidecar is optional — verify falls back to XML parse.
            if (tm?.Entries == null) continue;
            // FL7 (§5.3 accelerator binding): a text sidecar that carries an IndexStamp (today
            // only the ORIGIN sidecar, materialized by MaterializeOriginTextAsync) is REFUSED when
            // its stamp no longer matches the owning family's manifest IndexStamp — the origin
            // corpus advanced under a now-stale sidecar. Skipping the whole family routes every one
            // of its (rel, side) reads to _defaultTextRoute (XML parse), so search stays fully
            // correct without it. Combined/overlay sidecars carry no IndexStamp (null) → never refused.
            if (tm.IndexStamp != null &&
                !string.Equals(tm.IndexStamp, fam.CachedManifest?.IndexStamp, StringComparison.Ordinal))
                continue;
            foreach (var e in tm.Entries)
                map[(e.RelPath, e.Side)] = new TextRoute
                {
                    Entry = e,
                    TextBinFileName = fam.TextBinFileName,
                    Family = fam,
                };
        }
        return map;
    }

    /// <summary>FL4 (§2.4): the manifest entries a family owns for the two-bin bloom sweep.
    /// Combined root (one family) → the passed merged entries directly (no cache dependency →
    /// byte-identical to the pre-FL4 single-bin sweep). Split root → the family's own cached
    /// manifest entries, whose bloom offsets are valid only in that family's bin.</summary>
    private static IReadOnlyList<SearchIndexEntry> EntriesForFamily(
        SearchFamily fam, IReadOnlyList<SearchFamily> families, List<SearchIndexEntry> mergedEntries)
    {
        if (families.Count == 1) return mergedEntries;
        return fam.CachedManifest?.Entries ?? (IReadOnlyList<SearchIndexEntry>)Array.Empty<SearchIndexEntry>();
    }

    /// <summary>FL4: the text route for a (rel, side), or the combined fallback (entry == null
    /// → XML-parse path) when no sidecar entry exists for it.</summary>
    private TextRoute RouteFor(Dictionary<(string rel, SearchSide side), TextRoute> map, string rel, SearchSide side)
        => map.TryGetValue((rel, side), out var r) ? r : _defaultTextRoute;

    /// <summary>FL4: text-manifest load core, parametrized by the owning family. The combined
    /// family reads search.text.*; the origin/overlay families read their own text sidecars.
    /// Uses the family's own cache slots. Byte-identical to the pre-FL4 body when
    /// <paramref name="fam"/> == Combined.</summary>
    private async Task<SearchTextManifest?> TryLoadTextManifestForFamilyAsync(string root, SearchFamily fam)
    {
        try
        {
            var mp = Path.Combine(root, fam.TextManifestFileName);
            var bp = Path.Combine(root, fam.TextBinFileName);

            if (!File.Exists(mp) || !File.Exists(bp))
                return null;

            var mpFull = Path.GetFullPath(mp);
            var mpWriteUtc = File.GetLastWriteTimeUtc(mpFull);

            lock (_indexCacheLock)
            {
                if (fam.CachedTextManifest != null &&
                    string.Equals(fam.CachedTextManifestPath, mpFull, StringComparison.OrdinalIgnoreCase) &&
                    fam.CachedTextManifestWriteUtc == mpWriteUtc)
                {
                    return fam.CachedTextManifest;
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
                fam.CachedTextManifest = man;
                fam.CachedTextManifestPath = mpFull;
                fam.CachedTextManifestWriteUtc = mpWriteUtc;
            }

            return man;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveManifestAtomicAsync(
        string root,
        SearchIndexManifest manifest,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        IReadOnlyDictionary<string, SearchIndexEntry>? contentHashCache,
        CancellationToken ct,
        LayerSpec? spec = null)
    {
        // FL3: a null spec (or the combined spec) preserves the pre-FL3 behaviour EXACTLY —
        // BuildGuid, InputHash via the cache-aware compute, GetManifestPath, Combined cache.
        // A layer spec redirects to the layer file name + guid + stamp field, computes the
        // stamp with the plain (cache-less) recipe over the caller-supplied basis dirs, and
        // never mutates the served family's cache.
        bool isLayer = spec != null && spec.Role != LayerRole.Combined;

        manifest.RootPath = root;
        manifest.BuiltUtc = DateTime.UtcNow;
        manifest.Version = 1;
        manifest.BloomBits = BloomBits;
        manifest.BloomHashCount = BloomHashCount;
        manifest.BuildGuid = spec?.BuildGuid ?? BuildGuid;

        if (isLayer)
        {
            manifest.LayerRole = spec!.Role == LayerRole.Origin ? "origin" : "overlay";
            try
            {
                // Content hash (same recipe as InputHash) over the caller-supplied basis dirs.
                // FL6: pass the per-file content-hash cache (this build's fresh Phase-1 hashes,
                // or — on a carve — the hashes carried from the legacy manifest) so the layer
                // stamp is computed by STATS, not a second corpus read. Value-identical to the
                // cache-less recipe (ComputeInputHashAsync invariant); this is what makes a Path-B
                // carve read zero XML for stat-unchanged entries (a fresh layer build also stops
                // paying the previously-redundant second read here).
                var h = await ComputeInputHashAsync(originalDir, translatedDirs,
                    cache: contentHashCache, writeBack: null, ct).ConfigureAwait(false);
                if (spec.StampField == StampField.OriginHash) manifest.OriginHash = h;
                else manifest.OverlayHash = h;
            }
            catch (IOException)
            {
                // Filesystem race — leave the stamp null (FL5 wiring treats absent as stale).
            }

            // §1.5 cross-layer binding: an overlay records the origin manifest's IndexStamp
            // it was built against.
            if (spec.Role == LayerRole.Overlay)
                manifest.BasedOnOriginStamp = await ReadOriginIndexStampAsync(root, ct).ConfigureAwait(false);
        }
        else
        {
            // Snapshot the input-file metadata hash so future IsStaleAsync calls take the
            // fast hash path instead of the legacy mtime check. Also collect every per-file
            // hash via the writeBack channel and propagate onto the corresponding entry's
            // ContentHash field — so the very first IsStaleAsync call after this manifest
            // is written hits the cache-fast path (no re-hashing of unchanged files).
            try
            {
                var freshHashes = new Dictionary<string, string>(StringComparer.Ordinal);
                // INC-2A (D3 item 1): the build path no longer re-reads the whole corpus here.
                // Phase 1 already SHA256'd the raw bytes of every file it read (all files on a
                // full rebuild; only changed/added files on an incremental update, with
                // unchanged files' hashes carried forward from the old manifest), and the
                // caller passes those hashes in as `contentHashCache` keyed by the exact
                // AppendDirRows walk-key scheme. The walk below therefore stat-hits everywhere
                // and the resulting root hash is value-identical to a cache:null computation
                // (per-file digests are the same SHA256 of the same raw bytes). Walk-time
                // cache misses (file changed since the scan, legacy null hash, shadowed
                // multi-dir files) fall back to a fresh read+hash; the writeBack channel plus
                // ApplyContentHashWriteBack remain the safety net that patches those entries.
                manifest.InputHash = await ComputeInputHashAsync(originalDir, translatedDirs, cache: contentHashCache, writeBack: freshHashes, ct).ConfigureAwait(false);

                // Stamp ContentHash onto manifest entries. Uses the same namespacing scheme
                // (orig/, tran0/) as BuildContentHashCache so the next IsStaleAsync call
                // finds the cache via BuildContentHashCache(manifest).
                if (freshHashes.Count > 0)
                {
                    ApplyContentHashWriteBack(manifest, freshHashes);
                }
            }
            catch (IOException)
            {
                // Filesystem race during build — leave InputHash null; legacy mtime path will run.
                manifest.InputHash = null;
            }
        }

        var final = isLayer ? Path.Combine(root, spec!.ManifestFileName) : GetManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct);

        ReplaceFileAtomicWithRetry(tmp, final);

        // Refresh manifest cache immediately so next search avoids JSON reload. FL3: only the
        // served (combined) family caches its manifest — a layer build has no served family.
        if (spec == null || spec.TargetFamily != null)
        {
            try
            {
                var full = Path.GetFullPath(final);
                var writeUtc = File.GetLastWriteTimeUtc(full);
                lock (_indexCacheLock)
                {
                    Combined.CachedManifest = manifest;
                    Combined.CachedManifestPath = full;
                    Combined.CachedManifestWriteUtc = writeUtc;
                }
            }
            catch
            {
                // harmless
            }
        }
    }

    /// <summary>FL3 (§1.5): reads the on-disk origin manifest's IndexStamp for the overlay's
    /// BasedOnOriginStamp binding. Null when the origin manifest is absent/unreadable.</summary>
    private async Task<string?> ReadOriginIndexStampAsync(string root, CancellationToken ct)
    {
        var mp = Path.Combine(root, OriginManifestFileName);
        if (!File.Exists(mp)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(mp, Utf8NoBom, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            return man?.IndexStamp;
        }
        catch { return null; }
    }

    private async Task SaveTextManifestAtomicAsync(string root, SearchTextManifest manifest, CancellationToken ct, LayerSpec? spec = null)
    {
        manifest.RootPath = root;
        manifest.BuiltUtc = DateTime.UtcNow;
        manifest.Version = TextManifestVersion;
        manifest.BuildGuid = TextBuildGuid;

        var final = spec != null ? Path.Combine(root, spec.TextManifestFileName) : GetTextManifestPath(root);
        var tmp = final + ".tmp";

        var json = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, Utf8NoBom, ct);

        ReplaceFileAtomicWithRetry(tmp, final);

        // FL3: only the served (combined) family caches its text manifest.
        if (spec == null || spec.TargetFamily != null)
        {
            try
            {
                var full = Path.GetFullPath(final);
                var writeUtc = File.GetLastWriteTimeUtc(full);
                lock (_indexCacheLock)
                {
                    Combined.CachedTextManifest = manifest;
                    Combined.CachedTextManifestPath = full;
                    Combined.CachedTextManifestWriteUtc = writeUtc;
                }
            }
            catch
            {
                // harmless
            }
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
                Interlocked.Exchange(ref _lastBuildFallbackCount, 0);
                Interlocked.Exchange(ref _lastBuildDeltaGuardTripped, 0);

                // FL5 (design §3.1/§4.3): SPLIT-root orchestration behind the public facade —
                // ensure the frozen origin (adopt/build if stale) then build the live overlay if
                // stale. A combined root (incl. an existing legacy combined family with no split
                // bundle) keeps EXACTLY today's single build path below, so existing users are
                // never mass-rebuilt (#1 constraint).
                // FL6 (§6): a legacy combined root with no split family MIGRATES to split here —
                // Path A (adopt the origin bundle + build the overlay fresh) or Path B (carve from
                // the legacy artifacts, no XML for stat-unchanged entries) — NEVER a mass rebuild.
                // forceRebuild (CI/headless) skips migration and rebuilds directly. Runs before the
                // split orchestration so the legacy family is converted, not served alongside a new
                // split. On a torn carve the migration rolls back to the (still-servable) legacy
                // family, so an existing combined user is never left without search.
                if (!forceRebuild && NeedsMigration(root))
                {
                    await MigrateCombinedToSplitAsync(root, originalDir, translatedDirs,
                        additionalOriginalDirs, additionalTranslatedDirs, progress, ct);
                    // FL7 (§5.3): after a Path-A adopt the origin text sidecar is absent — fill it
                    // at idle. Path-B carve already wrote it, so this is a no-op there.
                    MaybeTriggerOriginTextMaterialization(root, originalDir);
                    return;
                }

                if (UseSplitPath(root, ResolveBundleDir()))
                {
                    await BuildSplitAsync(root, originalDir, translatedDirs,
                        additionalOriginalDirs, additionalTranslatedDirs, forceRebuild, progress, ct);
                    // FL7 (§5.3): after an origin adopt-from-trimmed-bundle the origin text sidecar
                    // is absent — materialize it once at idle (decoupled from the overlay build).
                    // A locally-built/adopted-with-text origin already has it → cheap no-op.
                    MaybeTriggerOriginTextMaterialization(root, originalDir);
                    return;
                }

                // SINGLE FALLBACK (S5): any exception thrown while incremental sourcing
                // was enabled (except cancellation) => delete stray family tmp files and
                // retry ONCE with incremental sourcing disabled (full compute), inside
                // the SAME gate acquisition. Failures on the full path propagate exactly
                // as before.
                if (!forceRebuild)
                {
                    try
                    {
                        await BuildOrUpdateCoreAsync(root, originalDir, translatedDirs,
                            allowIncremental: true, additionalOriginalDirs, additionalTranslatedDirs, progress, ct);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (IncrementalDeltaGuardException gex)
                    {
                        // PERF (E): NOT a failure. The incremental attempt deliberately
                        // bailed because the changed+removed delta exceeded the threshold
                        // (before any artifact was written). Run the clean full rebuild
                        // below WITHOUT counting an S5 fallback — that path stays reserved
                        // for genuine incremental-code faults.
                        Dbg($"Incremental skipped — {gex.Message} Running clean full rebuild.");
                        Interlocked.Exchange(ref _lastBuildDeltaGuardTripped, 1);
                        DeleteStrayFamilyTmpFiles(root);
                    }
                    catch (Exception ex)
                    {
                        Dbg($"Incremental index update FAILED — retrying as full rebuild: {ex.Message}\n{ex.StackTrace}");
                        System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Incremental update failed, retrying as full rebuild: {ex.Message}");
                        Interlocked.Increment(ref _lastBuildFallbackCount);
                        DeleteStrayFamilyTmpFiles(root);
                    }
                }

                await BuildOrUpdateCoreAsync(root, originalDir, translatedDirs,
                    allowIncremental: false, additionalOriginalDirs, additionalTranslatedDirs, progress, ct);
            }
            finally
            {
                _indexIoGate.Release();
            }
        }, ct);
    }

    /// <summary>
    /// FL5 (design §3.1/§4.2): SPLIT-root build orchestration. Re-probes the two layers (the
    /// origin probe adopts/seeds as a side effect), then: builds the frozen origin locally ONLY
    /// when a build is still owed (adoption failed / corpus diverged — the rare last resort);
    /// builds the live overlay when it is stale OR the origin was (re)built (a new origin stamp
    /// invalidates the overlay's <c>BasedOnOriginStamp</c>, §1.5). The overlay build is scoped to
    /// the overlay dirs — a frozen origin is never touched by a translation edit. <paramref
    /// name="forceRebuild"/> (CI/headless) rebuilds both layers unconditionally. Runs inside the
    /// caller's <see cref="_indexIoGate"/> acquisition.
    /// </summary>
    private async Task BuildSplitAsync(
        string root, string originalDir, IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs, IReadOnlyList<string>? additionalTranslatedDirs,
        bool forceRebuild,
        IProgress<(int done, int total, string phase)>? progress, CancellationToken ct)
    {
        Interlocked.Exchange(ref _lastBuildFallbackCount, 0);
        Interlocked.Exchange(ref _lastBuildDeltaGuardTripped, 0);

        bool buildOrigin, buildOverlay;
        if (forceRebuild)
        {
            buildOrigin = true;
            buildOverlay = true;
        }
        else
        {
            var probe = await ProbeLayersAsync(root, originalDir, translatedDirs,
                additionalOriginalDirs, additionalTranslatedDirs, ResolveBundleDir()).ConfigureAwait(false);
            buildOrigin = probe.OriginNeedsBuild;
            // The origin was just (re)built ⇒ its IndexStamp changed ⇒ the overlay's
            // BasedOnOriginStamp no longer matches ⇒ the overlay must be rebuilt too.
            buildOverlay = probe.OverlayNeedsBuild || probe.OriginNeedsBuild;
        }

        if (buildOrigin)
            await BuildOriginLayerAsync(root, originalDir, progress, ct).ConfigureAwait(false);

        if (buildOverlay)
            await BuildOverlayLayerAsync(root, translatedDirs,
                additionalOriginalDirs, additionalTranslatedDirs, progress, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// PERF (E) signal, not an error: the incremental attempt found the changed+removed
    /// delta exceeds <see cref="IncrementalFullRebuildDeltaThreshold"/> of the corpus, so it
    /// bailed BEFORE writing any artifact to let the public wrapper run a clean full rebuild.
    /// Carries no S5 fallback-failure semantics (that path is reserved for genuine faults).
    /// </summary>
    private sealed class IncrementalDeltaGuardException : Exception
    {
        public int DeltaCount { get; }
        public int CorpusSize { get; }
        public IncrementalDeltaGuardException(int deltaCount, int corpusSize)
            : base($"Incremental delta {deltaCount}/{corpusSize} exceeds full-rebuild threshold.")
        {
            DeltaCount = deltaCount;
            CorpusSize = corpusSize;
        }
    }

    /// <summary>Best-effort cleanup of half-written family artifacts before the fallback full rebuild.</summary>
    private static void DeleteStrayFamilyTmpFiles(string root)
    {
        try
        {
            foreach (var tmp in Directory.EnumerateFiles(root, "search.*.tmp", SearchOption.TopDirectoryOnly))
            {
                try { File.Delete(tmp); } catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Decodes raw XML bytes with the same semantics as <c>File.ReadAllText(path, Utf8NoBom)</c>
    /// (StreamReader with BOM detection enabled). INC-2A hashing rule: the raw bytes are read
    /// ONCE, SHA256'd as-is, and only then decoded — never hash a re-encode of a decoded string.
    /// </summary>
    private static string DecodeXmlBytes(byte[] raw)
    {
        using var ms = new MemoryStream(raw, writable: false);
        using var sr = new StreamReader(ms, Utf8NoBom, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Reads one old text.bin block via <see cref="RandomAccess"/> (per-call offsets, safe for
    /// the Phase-1 <c>Parallel.For</c> without a shared-stream lock). Returns null on ANY
    /// anomaly (short read, IO error) so the caller reclassifies the entry as changed and
    /// re-extracts from XML instead of committing a bogus block.
    /// </summary>
    private static byte[]? TryReadOldTextBlock(Microsoft.Win32.SafeHandles.SafeFileHandle handle, long offset, int length)
    {
        if (offset < 0 || length <= 0) return null;
        try
        {
            var buf = new byte[length];
            int read = 0;
            while (read < length)
            {
                int r = RandomAccess.Read(handle, buf.AsSpan(read, length - read), offset + read);
                if (r <= 0) return null;
                read += r;
            }
            return buf;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// INC-3A (D3 item 4): the corpus-frequency counting loop, extracted verbatim from the
    /// full-recount pass so the full path and the algebraic delta share ONE implementation.
    /// Counts every <see cref="IsIndexableCjk"/> code unit of <paramref name="text"/> with
    /// multiplicity into <paramref name="charFreqs"/>, and every adjacent CJK pair into
    /// <paramref name="bigramFreqs"/> (any non-CJK char resets the pair chain, exactly like
    /// the original <c>hasPrev</c> logic), each scaled by <paramref name="sign"/> (+1 add,
    /// -1 subtract); <paramref name="totalChars"/> moves by <paramref name="sign"/> per
    /// counted char. Keys reaching zero are NOT pruned here — the delta merge prunes (or
    /// detects negatives) when combining. Internal for test access (unit-tested against a
    /// literal reimplementation of the original loop).
    /// </summary>
    internal static void CountCorpusFreqs(
        string text,
        Dictionary<string, int> charFreqs,
        Dictionary<string, int> bigramFreqs,
        int sign,
        ref long totalChars)
    {
        char prev = '\0';
        bool hasPrev = false;
        for (int ci = 0; ci < text.Length; ci++)
        {
            char ch = text[ci];
            if (!IsIndexableCjk(ch)) { hasPrev = false; continue; }

            var ck = ch.ToString();
            charFreqs[ck] = charFreqs.TryGetValue(ck, out var cv) ? cv + sign : sign;
            totalChars += sign;

            if (hasPrev)
            {
                var bk = string.Concat(prev, ch);
                bigramFreqs[bk] = bigramFreqs.TryGetValue(bk, out var bv) ? bv + sign : sign;
            }
            prev = ch;
            hasPrev = true;
        }
    }

    /// <summary>
    /// INC-3A: loads the OLD corpusfreq manifest+bin into LOCAL maps for the algebraic
    /// delta (never the instance <see cref="CorpusCharFreqs"/> props — those must not be
    /// mutated mid-build). Returns null unless the artifact loads cleanly AND its
    /// IndexStamp is non-null and Ordinal-equal to <paramref name="expectedOldStamp"/>
    /// (the OLD main manifest's stamp, captured before the new stamp is minted) — a stale
    /// or legacy corpusfreq must never seed the delta.
    /// </summary>
    private static async Task<(Dictionary<string, int> charFreqs, Dictionary<string, int> bigramFreqs, long totalChars)?>
        TryLoadOldCorpusFreqForDeltaAsync(string root, string? expectedOldStamp, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(expectedOldStamp))
            return null;

        var manifestPath = Path.Combine(root, "search.corpusfreq.manifest.json");
        var binPath = Path.Combine(root, "search.corpusfreq.bin");
        if (!File.Exists(manifestPath) || !File.Exists(binPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, Utf8NoBom, ct);
            if (string.IsNullOrWhiteSpace(json)) return null;

            var freqManifest = JsonSerializer.Deserialize<CorpusFreqManifest>(json, JsonOpts);
            if (freqManifest == null || freqManifest.Version != 1) return null;
            if (!string.Equals(freqManifest.BuildGuid, CorpusFreqBuildGuid, StringComparison.Ordinal)) return null;
            if (freqManifest.IndexStamp == null ||
                !string.Equals(freqManifest.IndexStamp, expectedOldStamp, StringComparison.Ordinal))
            {
                Dbg("Corpus freq delta refused: old corpusfreq IndexStamp missing or mismatched with old main manifest");
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(binPath, ct);
            using var ms = new MemoryStream(bytes);
            using var br = new BinaryReader(ms, Utf8NoBom, leaveOpen: false);

            byte m0 = br.ReadByte(), m1 = br.ReadByte(), m2 = br.ReadByte(), m3 = br.ReadByte();
            if (m0 != (byte)'C' || m1 != (byte)'F' || m2 != (byte)'0' || m3 != (byte)'1')
                return null;

            int charCount = br.ReadInt32();
            int bigramCount = br.ReadInt32();
            long totalChars = br.ReadInt64();
            if (charCount < 0 || bigramCount < 0 || totalChars < 0) return null;

            var charFreqs = new Dictionary<string, int>(charCount);
            for (int i = 0; i < charCount; i++)
            {
                char ch = br.ReadChar();
                charFreqs[ch.ToString()] = br.ReadInt32();
            }

            var bigramFreqs = new Dictionary<string, int>(bigramCount);
            for (int i = 0; i < bigramCount; i++)
            {
                char c1 = br.ReadChar();
                char c2 = br.ReadChar();
                bigramFreqs[string.Concat(c1, c2)] = br.ReadInt32();
            }

            return (charFreqs, bigramFreqs, totalChars);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Dbg($"Corpus freq delta: old artifact load failed ({ex.Message}) — full recount");
            return null;
        }
    }

    /// <summary>
    /// INC-3A: reads one OLD text.bin block and accumulates SUBTRACT counts into the delta
    /// maps. A 0-length old block is a legitimately empty extraction (nothing to subtract).
    /// Returns false on any anomaly (bad row bounds, short/failed read) so the caller
    /// discards the whole delta and runs the full recount.
    /// </summary>
    private static bool TrySubtractOldTextCounts(
        SearchTextEntry oldRow,
        Microsoft.Win32.SafeHandles.SafeFileHandle oldTextHandle,
        Dictionary<string, int> deltaChars,
        Dictionary<string, int> deltaBigrams,
        ref long deltaTotal)
    {
        if (oldRow.TextLengthBytes == 0) return true;
        if (oldRow.TextLengthBytes < 0 || oldRow.TextOffset < 0) return false;

        var bytes = TryReadOldTextBlock(oldTextHandle, oldRow.TextOffset, oldRow.TextLengthBytes);
        if (bytes == null) return false;

        // Old blocks were written Utf8NoBom, so plain GetString (no BOM handling).
        CountCorpusFreqs(Utf8NoBom.GetString(bytes), deltaChars, deltaBigrams, -1, ref deltaTotal);
        return true;
    }

    /// <summary>
    /// INC-3A: merges an algebraic count delta into <paramref name="baseMap"/> (the OLD
    /// artifact's counts — a build-local copy, mutated in place). Keys landing exactly at
    /// 0 are PRUNED (a from-scratch rebuild would not contain them); ANY key landing
    /// negative (or overflowing int) means the delta disagrees with the old artifact —
    /// returns false so the caller discards the delta and runs the full recount.
    /// </summary>
    private static bool TryApplyCorpusFreqDelta(Dictionary<string, int> baseMap, Dictionary<string, int> delta)
    {
        foreach (var kv in delta)
        {
            long next = (long)(baseMap.TryGetValue(kv.Key, out var cur) ? cur : 0) + kv.Value;
            if (next < 0 || next > int.MaxValue) return false;
            if (next == 0) baseMap.Remove(kv.Key);
            else baseMap[kv.Key] = (int)next;
        }
        return true;
    }

    // ── FL3 (frozen/live split, design §3.1): layer-scoped build core ──
    // BuildOrUpdateCoreAsync's pipeline (scan → delta guard → Phase-1 parallel compute
    // skip-read → Phase-2 sequential write → commit) is parametrized by a LayerSpec that
    // carries the layer role, artifact file-name set, which dirs to scan, the stamp field,
    // the inverted-exclusion rel-set, and the serving target family. The COMBINED spec
    // routes through exactly today's file names / guid / serving state, so combined output
    // is byte-identical to pre-FL3. ORIGIN/OVERLAY specs write the split families and are
    // directly invokable (tests + `--layer`) but NOT yet loaded by the serving path.

    private enum LayerRole { Combined, Origin, Overlay }

    /// <summary>Which manifest field carries this layer's content hash (design §1.4).</summary>
    private enum StampField { InputHash, OriginHash, OverlayHash }

    private sealed class LayerSpec
    {
        public LayerRole Role;
        // Artifact file-name set (design §1.3). Combined == legacy names.
        public string ManifestFileName = "";
        public string BinFileName = "";
        public string TextManifestFileName = "";
        public string TextBinFileName = "";
        public string CorpusFreqManifestFileName = "";
        public string CorpusFreqBinFileName = "";
        public string InvertedBinFileName = "";
        public string BuildGuid = "";
        public StampField StampField;
        // Dir-scan scope (design §1.2). Combined scans everything.
        public bool ScanPrimaryOriginal;   // originalDir → origFiles (namespaced "orig/")
        public bool ScanTranslated;        // translatedDirs → tranFiles (Translated side)
        public bool ScanAdditional;        // additionalOriginalDirs / additionalTranslatedDirs
        // Overlay only: the origin manifest's rel-set. Used to (a) shadow any additional-orig
        // rel that collides with an active-origin rel out of the overlay entirely (§1.2 keep-
        // first), and (b) omit those rels from the overlay INVERTED feed only (§3.2 — bloom +
        // text + manifest entries are kept). Null for combined/origin (no exclusion).
        public HashSet<string>? OriginRelSet;
        // Gramsets sidecar is combined-only in FL3 (it is a build-time accelerator with a
        // single legacy file name; §5.4 makes it overlay-only in a later PR). Skipping it on
        // layer builds avoids writing a combined-named sidecar during an origin/overlay build.
        public bool SaveGramSets;
        // Serving state target. Combined → the sole family (mmap warm, cached manifest,
        // inverted + corpusfreq handles). Layers → null: a layer build writes on-disk
        // artifacts only and never mutates serving state (not loaded until FL4).
        public SearchFamily? TargetFamily;
    }

    private LayerSpec CombinedSpec() => new()
    {
        Role = LayerRole.Combined,
        ManifestFileName = ManifestFileName,
        BinFileName = BinFileName,
        TextManifestFileName = TextManifestFileName,
        TextBinFileName = TextBinFileName,
        CorpusFreqManifestFileName = CorpusFreqManifestFileName,
        CorpusFreqBinFileName = CorpusFreqBinFileName,
        InvertedBinFileName = InvertedBinFileName,
        BuildGuid = BuildGuid,
        StampField = StampField.InputHash,
        ScanPrimaryOriginal = true,
        ScanTranslated = true,
        ScanAdditional = true,
        OriginRelSet = null,
        SaveGramSets = true,
        TargetFamily = Combined,
    };

    private LayerSpec OriginSpec() => new()
    {
        Role = LayerRole.Origin,
        ManifestFileName = OriginManifestFileName,
        BinFileName = OriginBinFileName,
        TextManifestFileName = OriginTextManifestFileName,
        TextBinFileName = OriginTextBinFileName,
        CorpusFreqManifestFileName = OriginCorpusFreqManifestFileName,
        CorpusFreqBinFileName = OriginCorpusFreqBinFileName,
        InvertedBinFileName = OriginInvertedBinFileName,
        BuildGuid = OriginBuildGuid,
        StampField = StampField.OriginHash,
        ScanPrimaryOriginal = true,
        ScanTranslated = false,
        ScanAdditional = false,
        OriginRelSet = null,
        SaveGramSets = false,
        TargetFamily = null,
    };

    private LayerSpec OverlaySpec(HashSet<string> originRelSet) => new()
    {
        Role = LayerRole.Overlay,
        ManifestFileName = OverlayManifestFileName,
        BinFileName = OverlayBinFileName,
        TextManifestFileName = OverlayTextManifestFileName,
        TextBinFileName = OverlayTextBinFileName,
        CorpusFreqManifestFileName = OverlayCorpusFreqManifestFileName,
        CorpusFreqBinFileName = OverlayCorpusFreqBinFileName,
        InvertedBinFileName = OverlayInvertedBinFileName,
        BuildGuid = OverlayBuildGuid,
        StampField = StampField.OverlayHash,
        ScanPrimaryOriginal = false,
        ScanTranslated = true,
        ScanAdditional = true,
        OriginRelSet = originRelSet,
        SaveGramSets = false,
        TargetFamily = null,
    };

    /// <summary>
    /// FL3: reads the on-disk origin manifest's Original-side rel-set (== the origin corpus
    /// rel-set). The overlay build consults it read-only for dedup shadowing (§1.2) and
    /// inverted exclusion (§3.2). Returns an empty set when the origin manifest is absent.
    /// </summary>
    private async Task<HashSet<string>> LoadOriginRelSetAsync(string root, CancellationToken ct)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mp = Path.Combine(root, OriginManifestFileName);
        if (!File.Exists(mp)) return set;
        try
        {
            var json = await File.ReadAllTextAsync(mp, Utf8NoBom, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return set;
            var man = JsonSerializer.Deserialize<SearchIndexManifest>(json, JsonOpts);
            if (man?.Entries != null)
                foreach (var e in man.Entries)
                    if (e.Side == SearchSide.Original && !string.IsNullOrEmpty(e.RelPath))
                        set.Add(e.RelPath);
        }
        catch { /* absent/unreadable origin manifest → empty set (no shadowing/exclusion) */ }
        return set;
    }

    /// <summary>
    /// FL3 (test/CI-invokable): build the frozen ORIGIN family (search.origin.*) over
    /// <paramref name="originalDir"/> only. Full build (no incremental); does not touch
    /// serving state. NOT yet used by the serving path.
    /// </summary>
    internal Task BuildOriginLayerAsync(
        string root,
        string originalDir,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
        => BuildLayerCoreAsync(OriginSpec(), root, originalDir, Array.Empty<string>(),
            allowIncremental: false, additionalOriginalDirs: null, additionalTranslatedDirs: null,
            progress, ct);

    /// <summary>
    /// FL3 (test/CI-invokable): build the live OVERLAY family (search.overlay.*) over the
    /// translated + additional dirs. Loads the origin manifest's rel-set first for §1.2
    /// shadowing + §3.2 inverted exclusion (so build the origin layer BEFORE this). Full
    /// build; does not touch serving state. NOT yet used by the serving path.
    /// </summary>
    internal async Task BuildOverlayLayerAsync(
        string root,
        IReadOnlyList<string> translatedDirs,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        IProgress<(int done, int total, string phase)>? progress = null,
        CancellationToken ct = default)
    {
        var originRels = await LoadOriginRelSetAsync(root, ct).ConfigureAwait(false);
        await BuildLayerCoreAsync(OverlaySpec(originRels), root, originalDir: "", translatedDirs,
            allowIncremental: false, additionalOriginalDirs, additionalTranslatedDirs,
            progress, ct).ConfigureAwait(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // FL7 (design §5.3): ORIGIN-TEXT MATERIALIZATION JOB.
    //
    // The frozen origin family never rebuilds on a standard install (it is adopt-preferred),
    // and the shipped origin bundle is TRIMMED — it carries no search.origin.text.* (the 779 MB
    // trim constraint). So right after an ADOPT-from-trimmed-bundle the origin text sidecar is
    // ABSENT, and every origin-side verify + snippet parses the corpus XML from disk forever
    // (slow snippets AND slower multi-bigram verify). In the COMBINED world the first incremental
    // catch-up re-materialized text as a side effect; the split world rebuilds only the overlay on
    // an edit, so that side effect is gone. This job replaces it deliberately: it (re)builds
    // search.origin.text.bin + search.origin.text.manifest.json off the adopted origin manifest,
    // once, at idle priority, decoupled from any edit. It is BEST-EFFORT (never throws into a
    // caller) and SKIPPABLE — search is fully correct WITHOUT the sidecar (XML fallback); the job
    // only restores the post-first-edit steady-state snippet/verify speed of the combined world.
    //
    // A locally-built or Path-B-carved origin already has a stamp-current text sidecar (the build
    // core writes it), so the job is a cheap no-op there — it is needed specifically for the
    // adopt-from-trimmed-bundle path.
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>FL7: true iff a stamp-current origin text sidecar (bin + manifest, its
    /// <see cref="SearchTextManifest.IndexStamp"/> == <paramref name="originIndexStamp"/>) already
    /// exists — the idempotency gate for <see cref="MaterializeOriginTextAsync"/>.</summary>
    private bool OriginTextSidecarIsCurrent(string root, string originIndexStamp)
    {
        var bp = Path.Combine(root, OriginTextBinFileName);
        var mp = Path.Combine(root, OriginTextManifestFileName);
        if (!File.Exists(bp) || !File.Exists(mp)) return false;
        try
        {
            var json = File.ReadAllText(mp, Utf8NoBom);
            var man = JsonSerializer.Deserialize<SearchTextManifest>(json, JsonOpts);
            return man != null && string.Equals(man.IndexStamp, originIndexStamp, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    /// <summary>
    /// FL7 (§5.3): (re)materialize the frozen origin text sidecar from the on-disk origin manifest.
    /// Reads each origin XML once (<see cref="MakeSearchableTextFromXml_Fast"/>), writes
    /// search.origin.text.bin + manifest stamped with the CURRENT origin <c>IndexStamp</c> (the
    /// accelerator binding — a stamp-mismatched sidecar is loader-refused in
    /// <see cref="BuildTextRouteMapAsync"/>), atomically (temp + rename). BEST-EFFORT: never throws;
    /// IDEMPOTENT: no-op when a stamp-current sidecar already exists; CANCELLABLE via
    /// <paramref name="ct"/>. Does NOT touch the origin bloom/inverted/corpusfreq family or the
    /// _lastBuildXmlReadCount build counter (this is a decoupled accelerator, not a build).
    /// </summary>
    internal async Task MaterializeOriginTextAsync(string root, string originalDir, CancellationToken ct)
    {
        try
        {
            var originMp = Path.Combine(root, OriginManifestFileName);
            var originBp = Path.Combine(root, OriginBinFileName);
            if (!File.Exists(originMp) || !File.Exists(originBp)) return; // not a split-origin root

            var originMan = await LoadLayerManifestAsync(originMp, originBp, root, OriginBuildGuid, "origin").ConfigureAwait(false);
            if (originMan == null || string.IsNullOrEmpty(originMan.IndexStamp)) return; // nothing to bind the sidecar to
            var stamp = originMan.IndexStamp!;

            // Idempotent: a stamp-current sidecar is already on disk (locally-built/carved origin,
            // or a prior run of this job) → nothing to do.
            if (OriginTextSidecarIsCurrent(root, stamp)) return;

            ct.ThrowIfCancellationRequested();

            var finalBin = Path.Combine(root, OriginTextBinFileName);
            var finalMan = Path.Combine(root, OriginTextManifestFileName);
            var tmpBin = finalBin + ".tmp";
            var tmpMan = finalMan + ".tmp";
            try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
            try { if (File.Exists(tmpMan)) File.Delete(tmpMan); } catch { }

            bool htmlDecode = Options.HtmlDecodeIfAmpersandPresent;
            var textManifest = new SearchTextManifest
            {
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = TextBuildGuid,
                Version = TextManifestVersion,
                IndexStamp = stamp, // accelerator binding to the origin family
            };

            long textOffset = 0;
            using (var outTextFs = new FileStream(tmpBin, FileMode.Create, FileAccess.Write, FileShare.Read, 65536))
            {
                foreach (var e in originMan.Entries)
                {
                    ct.ThrowIfCancellationRequested();

                    // Origin entries are all Original-side; resolve under originalDir (no additional
                    // dirs — the frozen layer is exactly xml-p5). Extract the SAME searchable text
                    // the build core / verify XML-fallback produce (DecodeXmlBytes == ReadAllText
                    // Utf8NoBom semantics), so the sidecar is byte-identical to a locally-built one.
                    string abs = ResolveAbsPath(originalDir, null, e.RelPath);
                    string searchable;
                    try
                    {
                        var raw = File.ReadAllBytes(abs);
                        searchable = MakeSearchableTextFromXml_Fast(DecodeXmlBytes(raw), htmlDecode);
                    }
                    catch
                    {
                        // Best-effort: a missing/unreadable origin file yields a 0-length block; the
                        // verify path XML-falls-back for it (and gracefully handles the missing file).
                        searchable = "";
                    }

                    var bytes = string.IsNullOrEmpty(searchable) ? Array.Empty<byte>() : Utf8NoBom.GetBytes(searchable);
                    if (bytes.Length > 0)
                        await outTextFs.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);

                    textManifest.Entries.Add(new SearchTextEntry
                    {
                        Id = e.Id,
                        RelPath = e.RelPath,
                        Side = e.Side,
                        LastWriteUtcTicks = e.LastWriteUtcTicks,
                        LengthBytes = e.LengthBytes,
                        TextOffset = textOffset,
                        TextLengthBytes = bytes.Length,
                    });
                    textOffset += bytes.Length;
                }
                await outTextFs.FlushAsync(ct).ConfigureAwait(false);
            }

            var manJson = JsonSerializer.Serialize(textManifest, JsonOpts);
            await File.WriteAllTextAsync(tmpMan, manJson, Utf8NoBom, ct).ConfigureAwait(false);

            // Commit the BIN first, then the MANIFEST: a torn commit that leaves only the bin is the
            // clean sidecar-absent state (the loader needs BOTH files), so a reader never sees a
            // manifest whose offsets point past a not-yet-written bin.
            ReplaceFileAtomicWithRetry(tmpBin, finalBin);
            ReplaceFileAtomicWithRetry(tmpMan, finalMan);

            Interlocked.Increment(ref _originTextMaterializeCount);
            Dbg($"Origin text sidecar materialized: {textManifest.Entries.Count} entries, {textOffset} bytes (stamp {stamp}).");
        }
        catch (OperationCanceledException)
        {
            // Cancelled (Dispose / shutdown): leave the sidecar absent → search stays correct.
            CleanupOriginTextTmp(root);
        }
        catch (Exception ex)
        {
            Dbg($"Origin text materialization failed (best-effort; search stays correct via XML fallback): {ex.Message}");
            CleanupOriginTextTmp(root);
        }
    }

    /// <summary>FL7: best-effort removal of any stray origin-text tmp files left by a torn/cancelled
    /// materialization, so a later run starts clean.</summary>
    private static void CleanupOriginTextTmp(string root)
    {
        try { var b = Path.Combine(root, OriginTextBinFileName) + ".tmp"; if (File.Exists(b)) File.Delete(b); } catch { }
        try { var m = Path.Combine(root, OriginTextManifestFileName) + ".tmp"; if (File.Exists(m)) File.Delete(m); } catch { }
    }

    /// <summary>
    /// FL7 (§5.3): fire the origin-text materialization job at IDLE priority when the frozen origin
    /// family is present but its text sidecar is ABSENT — i.e. right after an adopt from a trimmed
    /// bundle. Non-blocking (detached <see cref="Task.Run(Action)"/>), SINGLE-WRITER (a second call
    /// no-ops while one job runs), and self-gating (the job re-checks idempotency). A locally-built
    /// or carved origin already has the sidecar → the cheap File.Exists pre-check skips the spawn,
    /// so the frozen-never-rebuilds edit paths pay nothing. Never blocks the UI or the build.
    /// </summary>
    private void MaybeTriggerOriginTextMaterialization(string root, string originalDir)
    {
        try
        {
            // Cheap synchronous pre-check: a split-origin root whose text sidecar is absent. (A
            // present sidecar — stamp-current or, in the rare stamp-stale case, re-written by the
            // next local origin build — needs no idle job, so skip the spawn.)
            if (!File.Exists(Path.Combine(root, OriginManifestFileName))) return;
            if (File.Exists(Path.Combine(root, OriginTextBinFileName)) &&
                File.Exists(Path.Combine(root, OriginTextManifestFileName)))
                return;

            // Single-writer: only one materialization in flight per service instance.
            if (Interlocked.CompareExchange(ref _originTextMaterializeFlag, 1, 0) != 0) return;

            var ct = _backgroundCts.Token;
            var task = Task.Run(async () =>
            {
                try
                {
                    // Idle/low priority: a best-effort accelerator, never on a hot path.
                    try { Thread.CurrentThread.Priority = ThreadPriority.Lowest; } catch { }
                    await MaterializeOriginTextAsync(root, originalDir, ct).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Exchange(ref _originTextMaterializeFlag, 0);
                }
            }, ct);
            Volatile.Write(ref _originTextMaterializeTask, task);
        }
        catch
        {
            // Trigger is itself best-effort; a failure to spawn just means the sidecar stays absent
            // (search correct via XML fallback) until a later launch retries.
            Interlocked.Exchange(ref _originTextMaterializeFlag, 0);
        }
    }

    /// <summary>FL3: the combined build routes through the shared layer core with the
    /// COMBINED spec — byte-identical to the pre-FL3 pipeline. Kept as its own method so the
    /// public retry wrapper (BuildOrUpdateAsync) is unchanged.</summary>
    private Task BuildOrUpdateCoreAsync(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        bool allowIncremental,
        IReadOnlyList<string>? additionalOriginalDirs,
        IReadOnlyList<string>? additionalTranslatedDirs,
        IProgress<(int done, int total, string phase)>? progress,
        CancellationToken ct)
        => BuildLayerCoreAsync(CombinedSpec(), root, originalDir, translatedDirs,
            allowIncremental, additionalOriginalDirs, additionalTranslatedDirs, progress, ct);

    private async Task BuildLayerCoreAsync(
        LayerSpec spec,
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        bool allowIncremental,
        IReadOnlyList<string>? additionalOriginalDirs,
        IReadOnlyList<string>? additionalTranslatedDirs,
        IProgress<(int done, int total, string phase)>? progress,
        CancellationToken ct,
        LayerCarrySource? carrySource = null)
    {
        // FL3: incremental sourcing (old-artifact skip-read, delta guard, corpusfreq/gramsets
        // reuse) is wired only for the COMBINED layer in FL3. Origin/overlay always run a full
        // build here (their incremental staleness/adoption wiring is FL5). For the combined
        // spec this equals the caller's allowIncremental, so combined behaviour is unchanged.
        //
        // FL6 carve (§6, Path B): a non-null carrySource enables incremental sourcing for an
        // ORIGIN/OVERLAY build too, but points the "old" artifacts at the LEGACY COMBINED family
        // (search.index.* / search.text.*) so every stat-unchanged entry is carried (byte-copy
        // bloom, carried text + ContentHash) with NO XML read. The >20% delta guard, the gramsets
        // sidecar, and the corpusfreq algebraic delta are all bypassed on a carve (grams/tf and
        // the corpusfreq partial are recomputed from the carried text — a one-time O(text) pass).
        bool carve = carrySource != null;
        bool coreIncremental = (allowIncremental && spec.Role == LayerRole.Combined) || carve;
            // NOTE: body indentation is preserved from the pre-INC-2A BuildOrUpdateAsync
            // pipeline (this method is that pipeline, extracted so the public wrapper can
            // retry it once with allowIncremental:false). Keeps the refactor diff reviewable.
            {
                Interlocked.Exchange(ref _lastBuildXmlReadCount, 0);
                Interlocked.Exchange(ref _lastBuildFreqDeltaApplied, 0);
                Interlocked.Exchange(ref _lastBuildGramComputeCount, 0);

                // Make sure stale mmap/manifest caches don't point at files being replaced.
                // FL3: serving-state invalidation is combined-only — a layer build (null
                // TargetFamily) never mutates the served family's caches.
                if (spec.TargetFamily != null)
                    InvalidateIndexCaches();

                SearchIndexManifest? oldMan = null;
                SearchTextManifest? oldTextMan = null;
                // FL6 carve: the "old" artifacts are the LEGACY COMBINED family (passed in), not
                // this layer's own (still-absent) files — the skip-read path carries every
                // stat-unchanged entry from the legacy bin/text.bin.
                string oldBinPath = carve ? carrySource!.BinPath : Path.Combine(root, spec.BinFileName);
                string oldTextBinPath = carve ? carrySource!.TextBinPath : Path.Combine(root, spec.TextBinFileName);

                if (carve)
                {
                    oldMan = carrySource!.Manifest;
                    oldTextMan = carrySource!.TextManifest;

                    // Test instrumentation: a torn-carve injection so the S5-style rollback
                    // (delete partial split families, keep the legacy family) is provable.
                    TestOnlyIncrementalFault?.Invoke();
                }
                else if (coreIncremental)
                {
                    oldMan = await TryLoadAsync(root);
                    oldTextMan = await TryLoadTextManifestAsync(root);

                    // Test instrumentation: simulate a bug in incremental-only code so the
                    // retry-as-full fallback can be proven end-to-end. Never fires in production.
                    TestOnlyIncrementalFault?.Invoke();
                }

                FileStream? oldFs = null;
                if (coreIncremental && oldMan != null && File.Exists(oldBinPath))
                {
                    try { oldFs = new FileStream(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                    catch { oldFs = null; }
                }
                // Old text.bin is opened as a SafeFileHandle (not a FileStream) because
                // Phase 1 reads blocks from it CONCURRENTLY: RandomAccess.Read takes an
                // explicit per-call offset, so no shared seek position exists to race on.
                Microsoft.Win32.SafeHandles.SafeFileHandle? oldTextHandle = null;
                if (coreIncremental && oldTextMan != null && File.Exists(oldTextBinPath))
                {
                    try { oldTextHandle = File.OpenHandle(oldTextBinPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); }
                    catch { oldTextHandle = null; }
                }

                var oldMap = new Dictionary<(string rel, SearchSide side), SearchIndexEntry>(new RelSideComparer());
                if (coreIncremental && oldMan != null)
                {
                    foreach (var e in oldMan.Entries)
                        oldMap[(e.RelPath, e.Side)] = e;
                }
                var oldTextMap = new Dictionary<(string rel, SearchSide side), SearchTextEntry>(new RelSideComparer());
                if (coreIncremental && oldTextMan != null)
                {
                    foreach (var e in oldTextMan.Entries)
                        oldTextMap[(e.RelPath, e.Side)] = e;
                }

                // ── INC-3A (D3 item 4): corpus-frequency algebraic delta ──
                // new = old − counts(changed/removed OLD text blocks) + counts(added/changed
                // NEW searchable texts). Active only when the OLD corpusfreq artifact loads
                // cleanly AND its IndexStamp equals the OLD main manifest's IndexStamp
                // (captured here, before the new stamp is minted below). Any precondition
                // miss or later inconsistency simply falls back to the existing full
                // recount over the new text.bin — never a full index rebuild.
                (Dictionary<string, int> charFreqs, Dictionary<string, int> bigramFreqs, long totalChars)? oldFreq = null;
                Dictionary<string, int>? freqDeltaChars = null;
                Dictionary<string, int>? freqDeltaBigrams = null;
                long freqDeltaTotal = 0;
                bool freqDeltaActive = false;
                if (coreIncremental && !carve && oldMan != null && oldTextMan != null &&
                    oldFs != null && oldTextHandle != null)
                {
                    // FL6 carve: skip the corpusfreq algebraic delta (the legacy combined partial
                    // mixes origin+overlay counts, and the layer partition is not a delta) — the
                    // partial is recounted from this layer's carried text blocks instead.
                    oldFreq = await TryLoadOldCorpusFreqForDeltaAsync(root, oldMan.IndexStamp, ct);
                    if (oldFreq != null)
                    {
                        freqDeltaActive = true;
                        freqDeltaChars = new Dictionary<string, int>(1024);
                        freqDeltaBigrams = new Dictionary<string, int>(4096);
                    }
                }

                progress?.Report((0, 0, "Scanning filesystem..."));

                // Each scan row carries the AppendDirRows walk key ("orig/rel", "tran{i}/rel")
                // when the file is physically under originalDir / translatedDirs[i]; files from
                // additional dirs get null (they are indexed but excluded from InputHash — the
                // walk never visits them, preserving the existing InputHash scope).
                //
                // FL3 (§1.2): the dir set is scoped by the LayerSpec. Combined scans all dirs
                // (byte-identical to before). Origin scans originalDir only. Overlay skips
                // originalDir and consults spec.OriginRelSet to shadow any additional-orig rel
                // that collides with an active-origin rel (reproducing the combined keep-first
                // dedup, since originalDir is not scanned by the overlay).
                var origFiles = new Dictionary<string, (string rel, string abs, FileInfo fi, string? walkKey)>(StringComparer.OrdinalIgnoreCase);
                if (spec.ScanPrimaryOriginal)
                {
                    foreach (var f in Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories))
                    {
                        var rel = NormalizeRelKey(Path.GetRelativePath(originalDir, f));
                        // originalDir is the first source; rels are unique within it, so this
                        // keep-first matches the pre-FL3 ToDictionary (which would have thrown
                        // on a duplicate — none exist here).
                        if (!origFiles.ContainsKey(rel))
                            origFiles[rel] = (rel, f, new FileInfo(f),
                                "orig/" + Path.GetRelativePath(originalDir, f).Replace('\\', '/'));
                    }
                }

                // Scan additional original dirs (e.g., OpenZen alongside CBETA)
                if (spec.ScanAdditional && additionalOriginalDirs != null)
                {
                    foreach (var addDir in additionalOriginalDirs)
                    {
                        if (string.IsNullOrWhiteSpace(addDir) || !Directory.Exists(addDir)) continue;
                        foreach (var f in Directory.EnumerateFiles(addDir, "*.xml", SearchOption.AllDirectories))
                        {
                            var rel = NormalizeRelKey(Path.GetRelativePath(addDir, f));
                            // Combined: shadow by already-scanned origFiles (== originalDir rels).
                            // Overlay: origFiles has no originalDir rels, so also shadow by
                            // spec.OriginRelSet (the origin corpus rel-set) — same net exclusion.
                            if (!origFiles.ContainsKey(rel) &&
                                (spec.OriginRelSet == null || !spec.OriginRelSet.Contains(rel)))
                                origFiles[rel] = (rel, f, new FileInfo(f), null);
                        }
                    }
                }

                var tranFiles = new Dictionary<string, (string rel, string abs, FileInfo fi, string? walkKey)>(StringComparer.OrdinalIgnoreCase);
                if (spec.ScanTranslated)
                {
                    for (int tIdx = 0; tIdx < translatedDirs.Count; tIdx++)
                    {
                        var tDir = translatedDirs[tIdx];
                        if (!Directory.Exists(tDir)) continue;
                        foreach (var f in Directory.EnumerateFiles(tDir, "*.xml", SearchOption.AllDirectories))
                        {
                            var rel = NormalizeRelKey(Path.GetRelativePath(tDir, f));
                            if (!tranFiles.ContainsKey(rel))
                                tranFiles[rel] = (rel, f, new FileInfo(f),
                                    "tran" + tIdx + "/" + Path.GetRelativePath(tDir, f).Replace('\\', '/'));
                        }
                    }
                }
                // Scan additional translated dirs (e.g., OpenZen translations)
                if (spec.ScanAdditional && additionalTranslatedDirs != null)
                {
                    foreach (var tDir in additionalTranslatedDirs)
                    {
                        if (string.IsNullOrWhiteSpace(tDir) || !Directory.Exists(tDir)) continue;
                        foreach (var f in Directory.EnumerateFiles(tDir, "*.xml", SearchOption.AllDirectories))
                        {
                            var rel = NormalizeRelKey(Path.GetRelativePath(tDir, f));
                            if (!tranFiles.ContainsKey(rel))
                                tranFiles[rel] = (rel, f, new FileInfo(f), null);
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

                // ── PERF (E): >20% delta guard ──
                // Only meaningful when there IS a prior index to be incremental against.
                // Classify each present (rel, side) with the SAME (mtime, size) stat test
                // Phase 1 uses (minus the block read-back — the guard is a heuristic, so a
                // rare read-back miss just routes that one entry through the normal per-entry
                // recompute) and count removed old entries. If changed+added+removed exceeds
                // the threshold, bail HERE — before the gramsets sidecar is loaded, before
                // any tmp file is written — and let the public wrapper run a clean full
                // rebuild. Never wrong, only ever a speed choice.
                // FL6 carve: bypass the >20% delta guard entirely — a carve is a one-time full
                // split build (every non-origin entry looks "removed" from the origin layer's
                // perspective, which would spuriously trip the guard and fall back to an XML mass
                // rebuild). The carve is not a delta build.
                if (coreIncremental && !carve && oldMan != null)
                {
                    bool StatUnchanged(string rel, SearchSide side, FileInfo fi)
                    {
                        long ticks = fi.LastWriteTimeUtc.Ticks;
                        long len = fi.Length;
                        return oldMap.TryGetValue((rel, side), out var om)
                            && om.LastWriteUtcTicks == ticks && om.LengthBytes == len && om.BloomOffset >= 0
                            && oldTextMap.TryGetValue((rel, side), out var ot)
                            && ot.LastWriteUtcTicks == ticks && ot.LengthBytes == len
                            && ot.TextOffset >= 0 && ot.TextLengthBytes > 0;
                    }

                    int changedOrAdded = 0;
                    foreach (var rel in allRel)
                    {
                        if (origFiles.TryGetValue(rel, out var o) && !StatUnchanged(rel, SearchSide.Original, o.fi)) changedOrAdded++;
                        if (tranFiles.TryGetValue(rel, out var t) && !StatUnchanged(rel, SearchSide.Translated, t.fi)) changedOrAdded++;
                    }
                    int removed = 0;
                    foreach (var key in oldMap.Keys)
                    {
                        bool present = key.side == SearchSide.Original
                            ? origFiles.ContainsKey(key.rel)
                            : tranFiles.ContainsKey(key.rel);
                        if (!present) removed++;
                    }
                    int denom = Math.Max(1, Math.Max(total, oldMap.Count));
                    if (changedOrAdded + removed > IncrementalFullRebuildDeltaThreshold * denom)
                    {
                        // The tmp-write try/finally that normally disposes these old handles
                        // is never entered on this early-bail path — release them here.
                        try { oldFs?.Dispose(); } catch { }
                        try { oldTextHandle?.Dispose(); } catch { }
                        throw new IncrementalDeltaGuardException(changedOrAdded + removed, denom);
                    }
                }

                var manifest = new SearchIndexManifest
                {
                    RootPath = root,
                    BuiltUtc = DateTime.UtcNow,
                    BuildGuid = spec.BuildGuid,
                    BloomBits = BloomBits,
                    BloomHashCount = BloomHashCount,
                    Version = 1,
                    // Minted per rebuild; the inverted index saved below embeds the same
                    // stamp, and the loader refuses any search.inverted.bin whose stamp
                    // differs from the manifest it is loaded alongside.
                    IndexStamp = Guid.NewGuid().ToString("N"),
                };
                var textManifest = new SearchTextManifest
                {
                    RootPath = root,
                    BuiltUtc = DateTime.UtcNow,
                    BuildGuid = TextBuildGuid,
                    Version = TextManifestVersion,
                };

                var finalBin = Path.Combine(root, spec.BinFileName);
                var tmpBin = finalBin + ".tmp";
                var finalTextBin = Path.Combine(root, spec.TextBinFileName);
                var tmpTextBin = finalTextBin + ".tmp";

                try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
                try { if (File.Exists(tmpTextBin)) File.Delete(tmpTextBin); } catch { }

                // INC-4A: inverted-index documents are (relPath, UNCUT gram set) pairs now —
                // the high-DF cutoff is applied INSIDE InvertedSearchIndex.Build on every
                // save, so terms cut by a previous build resurrect when the corpus shrinks.
                var invertedDocs = new List<(string relPath, uint[] gramSet, int[] gramCounts)>();

                // ── INC-4A: gramsets sidecar (6th artifact, pure accelerator) ──
                // Loaded once per build when incremental sourcing is enabled; a null result
                // (missing/corrupt/mismatched sidecar) just means every entry's gram sets
                // are computed fresh — never a full rebuild, never a failure. On the full
                // path (forceRebuild / fallback) the sidecar is IGNORED entirely.
                // FL6 carve: do NOT consult the (combined-named) gramsets sidecar — recompute gram
                // sets/tf from the carried text, per §6 ("recompute gram sets/tf from the carried
                // text"). A hit would only skip the gram-SET recompute anyway (tf is always
                // re-derived), and the origin/overlay layers write no combined-named sidecar.
                LoadedGramSets? gramSets = (coreIncremental && !carve)
                    ? await GramSetsStore.TryLoadAsync(root, ct)
                    : null;

                // Per-entry inverted-alphabet gram sets, aligned with the final manifest
                // entry order (the Phase-1 work-item order IS the Phase-2 entry order; the
                // work list has exactly `total` items by construction). Kept past Phase 2:
                // the inverted build transposes the inv sets, and the sidecar save persists
                // them after the commit sequence.
                var invGramsByEntry = new uint[total][];
                // v4 tf: per-entry inverted-alphabet gram counts, aligned with
                // invGramsByEntry[i]. Not cached in the (format-frozen) gramsets sidecar,
                // so ALWAYS derived from the entry's materialized searchable text — cheap
                // (the text is already decoded in Phase 1) and deterministic, so the
                // inverted index stays byte-identical between full and incremental builds.
                var invGramCountsByEntry = new int[total][];

                // INC-2A (D3 item 1): per-file content hashes for the InputHash computation
                // in SaveManifestAtomicAsync, keyed by the exact AppendDirRows walk-key
                // scheme. Seeded from the old manifest (existing BuildContentHashCache
                // helper), then overlaid in Phase 2 with this build's per-entry hashes
                // (fresh SHA256 for changed/added, carried-forward for unchanged) at the
                // scan-time (LengthBytes, LastWriteUtcTicks) — so the walk stat-hits
                // everywhere instead of re-reading the whole corpus a second time.
                var inputHashCache = new Dictionary<string, SearchIndexEntry>(StringComparer.Ordinal);
                if (coreIncremental && oldMan != null)
                {
                    foreach (var kv in BuildContentHashCache(oldMan))
                        inputHashCache[kv.Key] = kv.Value;
                }

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

                        // ── Build flat work list (preserves deterministic ordering) ──
                        var workItems = new List<(string relKey, SearchSide side, string absPath, FileInfo fi, string? walkKey)>(total);
                        foreach (var relKey in allRel)
                        {
                            if (origFiles.TryGetValue(relKey, out var o))
                                workItems.Add((relKey, SearchSide.Original, o.abs, o.fi, o.walkKey));
                            if (tranFiles.TryGetValue(relKey, out var t))
                                workItems.Add((relKey, SearchSide.Translated, t.abs, t.fi, t.walkKey));
                        }

                        // ── Phase 1: Parallel compute (CPU+IO bound) ──
                        var buildSw = System.Diagnostics.Stopwatch.StartNew();
                        progress?.Report((0, total, coreIncremental ? "Updating index..." : "Rebuilding index..."));

                        var computed = new ComputedEntry[workItems.Count];
                        bool htmlDecode = Options.HtmlDecodeIfAmpersandPresent;
                        int phase1Done = 0;

                        Parallel.For(0, workItems.Count, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Math.Max(1, Options.MaxBloomDegreeOfParallelism),
                            CancellationToken = ct
                        }, i =>
                        {
                            var (relKey, side, absPath, fi, walkKey) = workItems[i];
                            long ticks = fi.LastWriteTimeUtc.Ticks;
                            long lenBytes = fi.Length;

                            // ── Delta classification (S5) ──
                            // UNCHANGED iff the old MAIN manifest entry stat-matches
                            // (LastWriteUtcTicks, LengthBytes — same criterion as the old bloom
                            // skip) AND the old text-manifest row stat-matches with
                            // TextLengthBytes > 0 (a 0-length old block is a MISS: it may be a
                            // legitimately empty extraction, but re-extracting is cheap and this
                            // kills the sticky-zero bug where a failed text copy became
                            // permanent) AND the old block actually reads back in full.
                            // Everything else — adds, stat mismatch, text row missing/empty,
                            // old artifacts unloadable, block read failure — is CHANGED and
                            // gets a full per-entry recompute from the XML.
                            bool unchanged = false;
                            long oldBloomOffset = -1;
                            byte[]? oldTextBytes = null;
                            string? carriedHash = null;

                            if (coreIncremental && oldFs != null && oldTextHandle != null &&
                                oldMap.TryGetValue((relKey, side), out var old) &&
                                old.LastWriteUtcTicks == ticks &&
                                old.LengthBytes == lenBytes &&
                                old.BloomOffset >= 0 &&
                                oldTextMap.TryGetValue((relKey, side), out var oldText) &&
                                oldText.LastWriteUtcTicks == ticks &&
                                oldText.LengthBytes == lenBytes &&
                                oldText.TextOffset >= 0 &&
                                oldText.TextLengthBytes > 0)
                            {
                                oldTextBytes = TryReadOldTextBlock(oldTextHandle, oldText.TextOffset, oldText.TextLengthBytes);
                                if (oldTextBytes != null)
                                {
                                    unchanged = true;
                                    oldBloomOffset = old.BloomOffset;
                                    carriedHash = old.ContentHash; // may be null (legacy manifest)
                                }
                            }

                            string searchable;
                            ulong[]? bits = null;
                            byte[] textBytes;
                            string? contentHash;

                            if (unchanged)
                            {
                                // Skip-read (D3 item 3): the XML is NOT touched. SearchableText is
                                // sourced from the old text.bin block (blocks were written
                                // Utf8NoBom, so plain GetString — no BOM handling), the bloom
                                // block is byte-copied in Phase 2, and the text block is
                                // rewritten from these validated bytes.
                                searchable = Utf8NoBom.GetString(oldTextBytes!);
                                textBytes = oldTextBytes!;
                                contentHash = carriedHash;
                            }
                            else
                            {
                                // Changed/added: read the raw bytes ONCE, SHA256 them as-is
                                // (feeds InputHash without a second corpus read), then decode
                                // and extract.
                                byte[] raw = File.ReadAllBytes(absPath);
                                Interlocked.Increment(ref _lastBuildXmlReadCount);
                                contentHash = Convert.ToHexString(
                                    System.Security.Cryptography.SHA256.HashData(raw)).ToLowerInvariant();
                                string xml = DecodeXmlBytes(raw);
                                searchable = MakeSearchableTextFromXml_Fast(xml, htmlDecode);

                                bits = new ulong[BloomUlongs];
                                BuildBloomFromText(bits, searchable);

                                textBytes = string.IsNullOrEmpty(searchable)
                                    ? Array.Empty<byte>()
                                    : Utf8NoBom.GetBytes(searchable);
                            }

                            // ── INC-4A: gram sets for both alphabets ──
                            // Changed/added entries ALWAYS compute (the sidecar is never
                            // consulted across a content change). Unchanged entries consult
                            // the sidecar under the S4 per-entry HIT rule: ContentHash
                            // equality (Ordinal) when both hashes are non-null, otherwise
                            // (ticks, len) equality against the current scan. A miss
                            // recomputes from the entry's SearchableText — already
                            // materialized from the old text.bin block, never a re-read
                            // of the XML.
                            uint[]? invGrams = null;
                            int[]? invCounts = null;
                            if (unchanged && gramSets != null && gramSets.TryGet(relKey, side, out var gsRow))
                            {
                                bool gsHit = gsRow.ContentHash != null && carriedHash != null
                                    ? string.Equals(gsRow.ContentHash, carriedHash, StringComparison.Ordinal)
                                    : gsRow.LastWriteUtcTicks == ticks && gsRow.LengthBytes == lenBytes;
                                if (gsHit)
                                {
                                    invGrams = gramSets.ReadInvGrams(gsRow);
                                }
                            }
                            if (invGrams == null)
                            {
                                // Fresh compute: grams + tf counts together (one text pass).
                                // This is the ONLY site that increments the gram-compute
                                // counter (sidecar MISS/changed).
                                (invGrams, invCounts) = InvertedSearchIndex.ComputeGramSetAndCounts(searchable);
                                Interlocked.Increment(ref _lastBuildGramComputeCount);
                            }
                            if (invCounts == null)
                            {
                                // Sidecar HIT: the gram SET is cached but tf is not (the
                                // sidecar format is frozen), so derive counts from the same
                                // searchable text the set was cached for.
                                //
                                // PERF CAVEAT (INC-4A limitation): this is a full O(text) scan
                                // plus a per-doc dictionary — the SAME cost class the sidecar
                                // exists to skip. On the inverted alphabet the accelerator now
                                // only saves the SET derivation, not this tf derivation, so an
                                // incremental rebuild still pays a linear scan per unchanged
                                // entry. Removing this cost requires version-bumping the gramsets
                                // sidecar to persist the aligned counts (its own format guard
                                // makes that a safe, isolated accelerator change).
                                //
                                // The LastBuildGramComputeCount canary deliberately does NOT
                                // count this site — it measures gram-SET recomputes only, so the
                                // sidecar-reuse tests keep asserting set reuse. Be aware they do
                                // NOT prove this tf scan was avoided.
                                invCounts = InvertedSearchIndex.ComputeGramCounts(searchable, invGrams);
                            }
                            invGramsByEntry[i] = invGrams;
                            invGramCountsByEntry[i] = invCounts;

                            computed[i] = new ComputedEntry
                            {
                                RelKey = relKey,
                                Side = side,
                                Ticks = ticks,
                                LenBytes = lenBytes,
                                SearchableText = searchable,
                                Bits = bits,
                                TextBytes = textBytes,
                                CopiedBloom = unchanged,
                                OldBloomOffset = oldBloomOffset,
                                ContentHash = contentHash,
                                WalkKey = walkKey
                            };

                            // Phase 1 progress (thread-safe)
                            int p1 = System.Threading.Interlocked.Increment(ref phase1Done);
                            if (p1 % 200 == 0)
                                progress?.Report((p1 / 2, total, "Reading files..."));
                        });

                        var phase1Ms = buildSw.ElapsedMilliseconds;
                        Dbg($"Index build Phase 1 (parallel compute) done in {phase1Ms} ms for {workItems.Count} items");

                        // ── INC-3A: corpusfreq SUBTRACT pass (changed + removed old texts) ──
                        // Must run HERE: the old text.bin handle is still open (disposed
                        // right after this using block, before the bin swap) and the
                        // Phase-1 classification (computed[i].CopiedBloom) is still intact
                        // (Phase 2 clears the slots). Unchanged entries need no counting
                        // at all — their old text block IS their new text block, net zero.
                        // Note: the delta iterates ENTRIES (rel, side), not rels — both
                        // sides of a rel contribute CJK counts independently.
                        if (freqDeltaActive)
                        {
                            bool ok = true;

                            // Changed entries (recomputed in Phase 1) that existed in the
                            // old corpus: subtract their OLD text counts. Entries the old
                            // text manifest never knew are pure ADDs — nothing to subtract —
                            // unless the old MAIN manifest knew them (inconsistent old
                            // artifact pair: fall back to the full recount).
                            for (int wi = 0; wi < workItems.Count && ok; wi++)
                            {
                                if (computed[wi].CopiedBloom) continue;
                                ct.ThrowIfCancellationRequested();
                                var key = (workItems[wi].relKey, workItems[wi].side);
                                if (!oldTextMap.TryGetValue(key, out var oldTextRow))
                                {
                                    if (oldMap.ContainsKey(key)) ok = false;
                                    continue;
                                }
                                ok = TrySubtractOldTextCounts(oldTextRow, oldTextHandle!,
                                    freqDeltaChars!, freqDeltaBigrams!, ref freqDeltaTotal);
                            }

                            // Removed entries: in the old text manifest, absent from the
                            // new scan.
                            if (ok)
                            {
                                var newKeys = new HashSet<(string rel, SearchSide side)>(new RelSideComparer());
                                foreach (var w in workItems)
                                    newKeys.Add((w.relKey, w.side));

                                foreach (var oldTextRow in oldTextMan!.Entries)
                                {
                                    if (newKeys.Contains((oldTextRow.RelPath, oldTextRow.Side))) continue;
                                    ct.ThrowIfCancellationRequested();
                                    if (!TrySubtractOldTextCounts(oldTextRow, oldTextHandle!,
                                        freqDeltaChars!, freqDeltaBigrams!, ref freqDeltaTotal))
                                    {
                                        ok = false;
                                        break;
                                    }
                                }

                                // A removed entry the old MAIN manifest knew but the old
                                // text manifest did not cannot be subtracted — the old
                                // artifact pair is inconsistent, full recount.
                                if (ok)
                                {
                                    foreach (var e in oldMan!.Entries)
                                    {
                                        if (!newKeys.Contains((e.RelPath, e.Side)) &&
                                            !oldTextMap.ContainsKey((e.RelPath, e.Side)))
                                        {
                                            ok = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!ok)
                            {
                                freqDeltaActive = false;
                                Dbg("Corpus freq delta disabled: old text rows missing/unreadable for changed or removed entries — full recount");
                            }
                        }

                        // ── Phase 2: Sequential write (maintains exact byte ordering) ──
                        for (int i = 0; i < computed.Length; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            var entry = computed[i];

                            long entryBloomOffset = bloomOffset;
                            long entryTextOffset = textOffset;

                            if (entry.CopiedBloom && oldFs != null)
                            {
                                CopyBloomBlock(oldFs, entry.OldBloomOffset, outFs);
                            }
                            else if (entry.Bits != null)
                            {
                                WriteBloom(outFs, entry.Bits);
                            }

                            // Text blocks are always written from the in-memory bytes: fresh
                            // extraction bytes for changed entries, or the old block bytes
                            // already read AND validated in Phase 1 for unchanged entries.
                            // (A Phase-1 read failure reclassified the entry as changed, so
                            // there is no mid-write failure mode that could record a bogus
                            // 0-length block — the sticky-zero bug is gone.)
                            var tb = entry.TextBytes ?? Array.Empty<byte>();
                            if (tb.Length > 0)
                                outTextFs.Write(tb, 0, tb.Length);
                            int textLenBytes = tb.Length;

                            // Entry-order (orig then tran per rel) feed of UNCUT gram sets +
                            // aligned tf counts; keep-first dedup + winner flips are replayed
                            // inside Build (grams and counts are added together, so the kept
                            // entry's set and its tf stay paired).
                            //
                            // FL3 (§3.2): the OVERLAY inverted feed omits any rel present in the
                            // origin rel-set — those docs live in the origin inverted index, so
                            // including them here would duplicate. Bloom + text + manifest entries
                            // for the same rel are STILL written above (needed for the bloom
                            // fallback + verify), exactly as today's keep-first drops the
                            // translated doc from the inverted index but keeps its other artifacts.
                            // Combined: OriginRelSet is null → no exclusion (byte-identical).
                            if (spec.OriginRelSet == null || !spec.OriginRelSet.Contains(entry.RelKey))
                                invertedDocs.Add((entry.RelKey, invGramsByEntry[i], invGramCountsByEntry[i] ?? Array.Empty<int>()));

                            // INC-3A: corpusfreq ADD pass — added/changed entries contribute
                            // their NEW searchable text. Unchanged entries (CopiedBloom)
                            // are net-zero and skipped.
                            if (freqDeltaActive && !entry.CopiedBloom)
                            {
                                CountCorpusFreqs(entry.SearchableText,
                                    freqDeltaChars!, freqDeltaBigrams!, +1, ref freqDeltaTotal);
                            }

                            manifest.Entries.Add(new SearchIndexEntry
                            {
                                Id = id++,
                                RelPath = entry.RelKey,
                                Side = entry.Side,
                                LastWriteUtcTicks = entry.Ticks,
                                LengthBytes = entry.LenBytes,
                                BloomOffset = entryBloomOffset,
                                // Carry-forward for unchanged entries, fresh Phase-1 hash for
                                // changed/added ones. Null (legacy carry) is patched by the
                                // ApplyContentHashWriteBack safety net during the manifest save.
                                ContentHash = entry.ContentHash
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

                            if (entry.WalkKey != null && !string.IsNullOrEmpty(entry.ContentHash))
                            {
                                inputHashCache[entry.WalkKey] = new SearchIndexEntry
                                {
                                    RelPath = entry.RelKey,
                                    Side = entry.Side,
                                    LastWriteUtcTicks = entry.Ticks,
                                    LengthBytes = entry.LenBytes,
                                    ContentHash = entry.ContentHash
                                };
                            }

                            bloomOffset += BloomBytes;
                            textOffset += textLenBytes;
                            done++;

                            if (done % 200 == 0 || done == total)
                                progress?.Report((done, total, coreIncremental ? "Updating index..." : "Rebuilding index..."));

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
                    try { oldTextHandle?.Dispose(); } catch { }
                }

                ReplaceFileAtomicWithRetry(tmpBin, finalBin);
                ReplaceFileAtomicWithRetry(tmpTextBin, finalTextBin);

                // FL3: the manifest stamp basis is the layer's dir set. Combined/origin hash
                // (originalDir, translatedDirs) as-is (combined byte-identical; origin's
                // translatedDirs is empty). Overlay hashes the translated + additional dirs
                // with an empty origin slot (design §1.4 overlay basis).
                string stampOriginalDir = originalDir;
                IReadOnlyList<string> stampTranslatedDirs = translatedDirs;
                if (spec.Role == LayerRole.Overlay)
                {
                    stampOriginalDir = "";
                    var basis = new List<string>(translatedDirs);
                    if (additionalOriginalDirs != null) basis.AddRange(additionalOriginalDirs);
                    if (additionalTranslatedDirs != null) basis.AddRange(additionalTranslatedDirs);
                    stampTranslatedDirs = basis;
                }
                await SaveManifestAtomicAsync(root, manifest, stampOriginalDir, stampTranslatedDirs, inputHashCache, ct, spec);
                await SaveTextManifestAtomicAsync(root, textManifest, ct, spec);

                // Build and save inverted index alongside bloom
                try
                {
                    var invertedIndex = new InvertedSearchIndex();
                    var sortedDocs = invertedDocs.OrderBy(d => d.relPath, StringComparer.OrdinalIgnoreCase).ToList();
                    Dbg($"Inverted index: building from {sortedDocs.Count} docs...");
                    // INC-4A: gram-set Build overload — same stable sort, same keep-FIRST
                    // dedup, same ushort docId cap refusal, DF cutoff applied fresh inside
                    // Build from the uncut sets.
                    invertedIndex.Build(sortedDocs);
                    sortedDocs = null; // free gram-set references immediately
                    invertedDocs.Clear();
                    var invertedPath = Path.Combine(root, spec.InvertedBinFileName);
                    await invertedIndex.SaveAsync(invertedPath, manifest.IndexStamp!, ct);
                    // FL3: only the served (combined) family holds an in-memory inverted handle.
                    if (spec.TargetFamily != null)
                        spec.TargetFamily.Inverted = invertedIndex;
                    // PERF (F): softened from GC.Collect(2, Aggressive, blocking:true, compacting:true).
                    // The aggressive blocking+compacting Gen2/LOH collect fired at builder heap peak
                    // and suspended the UI thread mid-build (D1 perf#3 / D2 jank#1, ranked #1).
                    // A non-blocking, non-compacting Optimized collect lets the runtime reclaim the
                    // now-freed gram-set / doc references without stalling foreground threads.
                    GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: false);
                    Dbg($"Inverted index built: {invertedIndex.TermCount} terms, {invertedIndex.DocCount} docs");
                }
                catch (Exception ex)
                {
                    Dbg($"Inverted index build FAILED: {ex.Message}\n{ex.StackTrace}");
                    System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Inverted index FAILED: {ex.Message}\n{ex.StackTrace}");
                    // The manifest above is already committed with a fresh IndexStamp,
                    // so an inverted file from an earlier build would never load again —
                    // delete it (and any in-memory copy) rather than leave a dead file
                    // around. Search stays correct via bloom + verify.
                    if (spec.TargetFamily != null)
                        spec.TargetFamily.Inverted = null;
                    try
                    {
                        var staleInv = Path.Combine(root, spec.InvertedBinFileName);
                        if (File.Exists(staleInv)) File.Delete(staleInv);
                        if (File.Exists(staleInv + ".paths")) File.Delete(staleInv + ".paths");
                    }
                    catch { }
                }

                // CJK2 postings artifact was retired (superseded by the tf-carrying
                // inverted index; the bloom fallback runs a full sweep without cjk2
                // narrowing). Best-effort delete any stale search.cjk2.* left by an
                // older build so it does not linger next to the live family. FL3: this is a
                // combined-family legacy artifact; skip on layer builds.
                if (spec.Role == LayerRole.Combined)
                {
                    try
                    {
                        var staleCjk2Manifest = Path.Combine(root, "search.cjk2.manifest.json");
                        if (File.Exists(staleCjk2Manifest)) File.Delete(staleCjk2Manifest);
                        var staleCjk2Bin = Path.Combine(root, "search.cjk2.bin");
                        if (File.Exists(staleCjk2Bin)) File.Delete(staleCjk2Bin);
                    }
                    catch { }
                }

                // Build corpus frequency index from text.bin (sequential pass, no parallel alloc)
                try
                {
                    progress?.Report((total, total, "Building frequency index..."));
                    Dictionary<string, int>? corpusCharFreqs = null;
                    Dictionary<string, int>? corpusBigramFreqs = null;
                    long corpusTotalChars = 0;

                    // INC-3A: apply the algebraic delta to the OLD counts (build-local
                    // copies, never the instance props). Keys reaching exactly 0 are
                    // pruned; any key going negative (or a negative total) means the
                    // delta disagrees with the old artifact — discard it and run the
                    // full recount below.
                    if (freqDeltaActive && oldFreq != null)
                    {
                        long deltaTotalChars = oldFreq.Value.totalChars + freqDeltaTotal;
                        if (deltaTotalChars >= 0 &&
                            TryApplyCorpusFreqDelta(oldFreq.Value.charFreqs, freqDeltaChars!) &&
                            TryApplyCorpusFreqDelta(oldFreq.Value.bigramFreqs, freqDeltaBigrams!))
                        {
                            corpusCharFreqs = oldFreq.Value.charFreqs;
                            corpusBigramFreqs = oldFreq.Value.bigramFreqs;
                            corpusTotalChars = deltaTotalChars;
                            Interlocked.Exchange(ref _lastBuildFreqDeltaApplied, 1);
                            Dbg("Corpus freq index: algebraic delta applied (full text.bin recount skipped)");
                        }
                        else
                        {
                            Dbg("Corpus freq delta inconsistent (count went negative) — falling back to full recount");
                        }
                    }

                    if (corpusCharFreqs == null || corpusBigramFreqs == null)
                    {
                        corpusCharFreqs = new Dictionary<string, int>(32768);
                        corpusBigramFreqs = new Dictionary<string, int>(65536);
                        corpusTotalChars = 0;

                        var textBinPath = Path.Combine(root, spec.TextBinFileName);
                        if (File.Exists(textBinPath) && textManifest.Entries.Count > 0)
                        {
                            using var textFs = new FileStream(textBinPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
                            foreach (var te in textManifest.Entries)
                            {
                                if (te.TextLengthBytes <= 0) continue;
                                ct.ThrowIfCancellationRequested();
                                textFs.Seek(te.TextOffset, SeekOrigin.Begin);
                                var buf = new byte[te.TextLengthBytes];
                                int read = 0;
                                while (read < buf.Length)
                                {
                                    int n = textFs.Read(buf, read, buf.Length - read);
                                    if (n == 0) break;
                                    read += n;
                                }
                                var searchable = Utf8NoBom.GetString(buf, 0, read);
                                CountCorpusFreqs(searchable, corpusCharFreqs, corpusBigramFreqs, +1, ref corpusTotalChars);
                            }
                        }
                    }

                    Dbg($"Corpus freq index: {corpusCharFreqs.Count} unique chars, {corpusBigramFreqs.Count} unique bigrams, {corpusTotalChars} total chars");

                    var freqManifest = new CorpusFreqManifest
                    {
                        Version = 1,
                        BuildGuid = CorpusFreqBuildGuid,
                        BuiltUtc = DateTime.UtcNow,
                        TotalCharacters = corpusTotalChars,
                        UniqueCharacters = corpusCharFreqs.Count,
                        UniqueBigrams = corpusBigramFreqs.Count,
                        // Same-build binding: the loader refuses a corpusfreq artifact
                        // whose stamp differs from the main manifest's IndexStamp.
                        IndexStamp = manifest.IndexStamp
                    };

                    var freqManifestFinal = Path.Combine(root, spec.CorpusFreqManifestFileName);
                    var freqManifestTmp = freqManifestFinal + ".tmp";
                    var freqManifestJson = JsonSerializer.Serialize(freqManifest, JsonOpts);
                    await File.WriteAllTextAsync(freqManifestTmp, freqManifestJson, Utf8NoBom, ct);
                    ReplaceFileAtomicWithRetry(freqManifestTmp, freqManifestFinal);

                    var freqBinFinal = Path.Combine(root, spec.CorpusFreqBinFileName);
                    var freqBinTmp = freqBinFinal + ".tmp";
                    using (var fs = new FileStream(freqBinTmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var bw = new BinaryWriter(fs, Utf8NoBom, leaveOpen: false))
                    {
                        bw.Write((byte)'C'); bw.Write((byte)'F'); bw.Write((byte)'0'); bw.Write((byte)'1');
                        bw.Write(corpusCharFreqs.Count);
                        bw.Write(corpusBigramFreqs.Count);
                        bw.Write(corpusTotalChars);

                        foreach (var kv in corpusCharFreqs)
                        {
                            bw.Write(kv.Key[0]);
                            bw.Write(kv.Value);
                        }
                        foreach (var kv in corpusBigramFreqs)
                        {
                            bw.Write(kv.Key[0]);
                            bw.Write(kv.Key[1]);
                            bw.Write(kv.Value);
                        }
                    }
                    ReplaceFileAtomicWithRetry(freqBinTmp, freqBinFinal);

                    // FL3: only the served (combined) family exposes corpusfreq to the query
                    // path. A layer build writes its partial to disk but never mutates the
                    // in-memory merged view (not loaded until FL4).
                    if (spec.TargetFamily != null)
                    {
                        spec.TargetFamily.CharFreqs = corpusCharFreqs;
                        spec.TargetFamily.BigramFreqs = corpusBigramFreqs;
                        spec.TargetFamily.TotalChars = corpusTotalChars;
                        InvalidateMergedCorpusFreqs(); // FL2: a family partial changed → re-fold on next getter access
                    }

                    Dbg($"Corpus freq index saved: {new FileInfo(freqBinFinal).Length} bytes");
                }
                catch (Exception ex)
                {
                    Dbg($"Corpus frequency build FAILED: {ex.Message}");
                }

                // Warm mmap cache after rebuild so next search click is faster (combined only —
                // a layer build has no served family to warm).
                if (spec.TargetFamily != null)
                {
                    try { _ = GetOrCreateMappedAccessor(finalBin, spec.TargetFamily); } catch { }
                }

                // ── INC-4A: persist the gramsets sidecar — strictly AFTER the entire
                // family commit sequence (including the mmap warm). Saved on FULL builds
                // too (that is what warms the cache for the next delta). Best-effort: a
                // failure here is logged and swallowed, because the build already
                // succeeded and losing the sidecar only costs speed, never correctness.
                //
                // FL3: the gramsets sidecar has a single legacy file name (search.gramsets.*)
                // and is an incremental accelerator only. It is written on the COMBINED build
                // (spec.SaveGramSets) — a layer build (which never sources from it in FL3, and
                // whose own file names it does not carry) skips it to avoid writing a
                // combined-named sidecar. §5.4 makes it overlay-only in a later PR.
                if (spec.SaveGramSets)
                {
                    try
                    {
                        var sidecarRows = new List<(GramSetsEntry meta, uint[] invGrams)>(manifest.Entries.Count);
                        for (int i = 0; i < manifest.Entries.Count; i++)
                        {
                            var me = manifest.Entries[i];
                            sidecarRows.Add((new GramSetsEntry
                            {
                                RelPath = me.RelPath,
                                Side = me.Side,
                                // May be null (additional-dir entries or legacy carry-forward);
                                // the per-entry HIT rule then falls back to (ticks, len).
                                ContentHash = me.ContentHash,
                                LastWriteUtcTicks = me.LastWriteUtcTicks,
                                LengthBytes = me.LengthBytes,
                            }, invGramsByEntry[i] ?? Array.Empty<uint>()));
                        }
                        await GramSetsStore.SaveAsync(root, manifest.IndexStamp, sidecarRows, ct);
                        Dbg($"Gramsets sidecar saved: {sidecarRows.Count} entries ({LastBuildGramComputeCount} computed this run)");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Dbg($"Gramsets sidecar save FAILED (accelerator only, build unaffected): {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[SearchIndexService] Gramsets sidecar save failed: {ex.Message}");
                    }
                }

                progress?.Report((total, total, "Done"));
            }
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

        // FL4: capture the query-path family list ONCE (a concurrent reload reassigns the field
        // to a new list; every phase of this query must see the same set). Combined root →
        // [combined]; split root → [origin, overlay].
        var families = _families;

        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        Dbg($"SearchAllAsync START q='{query}' effectiveQ='{effectiveQuery}' len={effectiveQuery.Length} includeO={includeOriginal} includeT={includeTranslated} entries={entries.Count}");

        bool useBloom = effectiveQuery.Length >= 2;
        var grams = MakeQueryGrams(effectiveQuery);

        bool sideAllowed(SearchSide s)
            => (s == SearchSide.Original && includeOriginal) ||
               (s == SearchSide.Translated && includeTranslated);

        progress?.Report(new SearchProgress { Phase = "Building candidates..." });

        var candidates = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var swCandidate = System.Diagnostics.Stopwatch.StartNew();
        bool usedInvertedIndex = false;
        // v4 tf (instant search): per-relPath query term frequency read from the inverted
        // index. Populated only on the inverted fast path; null otherwise (bloom/brute).
        Dictionary<string, long>? tfByRel = null;

        // Fast path: inverted index (0% false positives, sub-millisecond).
        // FL4 disjoint-union merge of the loaded families (design §2.2): per-family doc sets are
        // disjoint (partition invariant §1.2), so candidates + tf UNION with no cross-layer
        // arithmetic, and each layer's ushort docId space stays local to its own GetRelPath.
        //
        // CONSERVATIVE FAST-PATH RULE (§2.2): use the inverted fast path ONLY when EVERY loaded
        // family has a usable (loaded) inverted index. If any family's inverted index failed to
        // load, the whole query falls back to the two-bin bloom sweep — otherwise that family's
        // docs would be silently dropped (inverted covers only the loaded families, and
        // usedInvertedIndex would suppress the bloom fallback). A mixed inverted+bloom mode is
        // explicitly deferred. For a combined root (one family) this collapses to today's rule
        // ("inverted iff the combined index is loaded"), byte-identical.
        bool allInvertedUsable = families.Count > 0 && families.All(f => f.Inverted?.IsLoaded == true);
        if (effectiveQuery.Length >= 2 && allInvertedUsable)
        {
            foreach (var fam in families)
            {
                var inv = fam.Inverted;
                if (inv?.IsLoaded != true) continue;

                var invertedHits = inv.SearchWithTf(effectiveQuery);
                if (invertedHits == null || invertedHits.Length == 0) continue;

                int sideMask = (includeOriginal ? 1 : 0) | (includeTranslated ? 2 : 0);
                tfByRel ??= new Dictionary<string, long>(invertedHits.Length, StringComparer.OrdinalIgnoreCase);
                foreach (var (docId, tf) in invertedHits)
                {
                    var relPath = inv.GetRelPath(docId);
                    if (relPath == null) continue;
                    if (relPathFilter != null && !relPathFilter(relPath)) continue;

                    // The inverted index doesn't track sides per doc — apply the requested side mask.
                    // The verification loop will check each side individually against actual file content.
                    candidates.AddOrUpdate(relPath, _ => sideMask, (_, v) => v | sideMask);
                    // One docId per relPath (Build dedups keep-first), so a plain set is safe;
                    // keep the max defensively if a relPath ever appears twice.
                    if (!tfByRel.TryGetValue(relPath, out var existing) || tf > existing)
                        tfByRel[relPath] = tf;
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

                // FL4 brute 1-char path (§2.4): union of ALL families' manifest entries.
                // `entries` IS that union (the merged view for a split root; the sole family's
                // entries for a combined root), so scan it ONCE — no per-family repeat.
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
                    // FL4 two-bin bloom sweep (§2.4): each family mmaps its OWN bin and scans
                    // ITS OWN manifest entries (bloom offsets are valid only in the owning bin).
                    // Combined root → the sole family owns every entry and its bin is
                    // search.index.bin, so this is byte-identical to the pre-FL4 single-bin sweep.
                    foreach (var fam in families)
                    {
                    var famEntries = EntriesForFamily(fam, families, entries);
                    string binPath = Path.Combine(root, fam.BinFileName);
                    var binFull = Path.GetFullPath(binPath);

                    if (!File.Exists(binFull))
                    {
                        Dbg($"Candidate phase bloom: bin missing '{binFull}'");
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
                            famEntries,
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
                    } // FL4: end foreach family (two-bin bloom sweep)
                }
            }
            finally
            {
                _indexIoGate.Release();
            }
        }
        swCandidate.Stop();

        // Skip-verify hybrid. Emitting a candidate WITHOUT verifying it is only sound when
        // the inverted index has proven the query is contiguous in the document. That holds
        // for a SINGLE-BIGRAM query (a 2-char CJK term): bigram co-occurrence == adjacency.
        // For a MULTI-BIGRAM phrase the index proves only that the constituent bigrams each
        // occur somewhere in the doc, NOT that they form the contiguous phrase — a doc with
        // 無門 and 門關 in unrelated places matches the intersection yet contains zero 無門關.
        // So skip-verify (emit without confirming) must be restricted to single-bigram
        // queries; multi-bigram instant queries still tf-RANK the candidates but verify
        // every one (correctness over the tail-latency win).
        //
        // Two ways to engage skip-verify:
        //  • instant single-bigram (v4 tf, Options.InstantSearch, 2-char CJK) — candidates
        //    rank by index tf (desc); the skipped tail shows the exact tf count.
        //  • legacy 2-char hybrid — exactly-2-char pure-CJK queries, even with instant off.
        //    Candidates rank by a size proxy (unchanged v6 behaviour, byte-identical).
        //
        // Instant tf-RANKING (independent of skip-verify) applies to ANY query resolved
        // through the inverted index, so multi-bigram instant queries still verify the
        // highest-tf docs first.
        bool instantTfRanking = Options.InstantSearch && usedInvertedIndex && tfByRel != null;
        // Adjacency is only proven by the inverted index for a single bigram (2-char CJK).
        bool singleBigramQuery = IsTwoCharCjk(effectiveQuery);
        bool instantSkipVerify = instantTfRanking && singleBigramQuery
                                 && Options.SkipVerifySnippetTopN > 0;
        bool twoCharSkipVerify = singleBigramQuery && Options.SkipVerifySnippetTopN > 0;
        bool hybridSkipVerifyEnabled = instantSkipVerify || twoCharSkipVerify;

        List<string> candidateList;
        if (instantTfRanking)
        {
            // Rank by index tf desc: the highest-frequency docs are the ones a user is
            // most likely to open first, so they get the eager verify + real snippets.
            candidateList = candidates.Keys
                .OrderByDescending(k => tfByRel!.TryGetValue(k, out var t) ? t : 0L)
                .ThenBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else if (hybridSkipVerifyEnabled)
        {
            // Build a per-relPath weight: max LengthBytes across sides. Bigger files plausibly
            // have more hits; this is a fallback for "actual posting-list bigram count" which
            // the inverted index does not expose at this layer (it returns doc-id sets,
            // not per-doc counts).
            var sizeByRel = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (!candidates.ContainsKey(e.RelPath)) continue;
                if (!sizeByRel.TryGetValue(e.RelPath, out var existing) || e.LengthBytes > existing)
                    sizeByRel[e.RelPath] = e.LengthBytes;
            }

            candidateList = candidates.Keys
                .OrderByDescending(k => sizeByRel.TryGetValue(k, out var sz) ? sz : 0L)
                .ThenBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            candidateList = candidates.Keys
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        int totalDocsToVerify = 0;
        foreach (var rel in candidateList)
        {
            int mask = candidates[rel];
            if ((mask & 1) != 0) totalDocsToVerify++;
            if ((mask & 2) != 0) totalDocsToVerify++;
        }

        Dbg($"Verify phase PREP candidateKeys={candidateList.Count} docsToVerify={totalDocsToVerify} hybridSkipVerify={hybridSkipVerifyEnabled} topN={Options.SkipVerifySnippetTopN}");

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
        // PR2 (skip-verify hybrid): the top-N relPaths (already sorted by size desc)
        // are the verify-snippet budget. Pre-compute as a hashset so Parallel.ForEach
        // workers can decide deterministically (work order is non-deterministic).
        HashSet<string>? verifyBudgetRelPaths = null;
        if (hybridSkipVerifyEnabled)
        {
            int take = Math.Min(candidateList.Count, Options.SkipVerifySnippetTopN);
            verifyBudgetRelPaths = new HashSet<string>(
                candidateList.Take(take), StringComparer.OrdinalIgnoreCase);
        }
        int skippedVerifyGroups = 0;
        // FL4 (design §2.5): entryMap is the union of the loaded families' manifests — `entries`
        // already IS that union (the merged view for a split root; the sole family's entries for
        // a combined root), disjoint by §1.2 so no key collision. textRouteMap unions the
        // per-family text sidecars and remembers, per (rel, side), which layer's text.bin +
        // mmap-cache family owns it so each verify reads from the right sidecar.
        var entryMap = entries.ToDictionary(e => (e.RelPath, e.Side), e => e, new RelSideComparer());
        var textRouteMap = await BuildTextRouteMapAsync(root, families);

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

                    // PR2 (skip-verify hybrid): determine whether this row enters the
                    // skip-verify path. The verifyBudgetRelPaths set was pre-computed from
                    // the top-N of the (size-desc-sorted) candidateList. Using a set rather
                    // than an atomic counter guarantees the SAME N relPaths are verified
                    // regardless of Parallel.ForEach work-order non-determinism.
                    bool skipVerify = hybridSkipVerifyEnabled
                                      && verifyBudgetRelPaths != null
                                      && !verifyBudgetRelPaths.Contains(relKey);

                    int hitsO = 0;
                    int hitsT = 0;
                    var originalHits = new List<SearchHit>();
                    var translatedHits = new List<SearchHit>();

                    if (skipVerify)
                    {
                        // Skip-verify path: reached only for single-bigram (2-char CJK) queries,
                        // where the inverted index has proven the query is contiguous in this doc.
                        // We emit a single placeholder child per requested side with
                        // IsSkippedVerify=true so the UI can render a "snippet on demand" affordance.
                        // The hit count is the exact index tf when available (instant mode); otherwise
                        // the sentinel "1" (= at least one, unverified).
                        //
                        // The inverted index stores ONE doc per relPath (Build dedups keep-first,
                        // original-then-translated), so tf measured only ONE side's text. Attribute
                        // that count to a single requested side (original first, matching keep-first)
                        // and keep the honest "at least one" sentinel for the other requested side,
                        // rather than showing a precise-looking tf for a side the index never measured.
                        int skipCount = 1;
                        if (instantSkipVerify && tfByRel != null &&
                            tfByRel.TryGetValue(relKey, out var qtf) && qtf > 0)
                            skipCount = (int)Math.Min(qtf, int.MaxValue);
                        bool tfClaimed = false;
                        if ((mask & 1) != 0)
                        {
                            originalHits.Add(new SearchHit { Index = 0, Left = "", Match = "", Right = "" });
                            hitsO = skipCount;
                            tfClaimed = true;
                            Interlocked.Add(ref totalHits, hitsO);
                        }
                        if ((mask & 2) != 0)
                        {
                            translatedHits.Add(new SearchHit { Index = 0, Left = "", Match = "", Right = "" });
                            // If original already claimed the tf, the translated side is genuinely
                            // unmeasured here — use the sentinel rather than the wrong side's count.
                            hitsT = tfClaimed ? 1 : skipCount;
                            Interlocked.Add(ref totalHits, hitsT);
                        }
                        Interlocked.Increment(ref skippedVerifyGroups);
                    }
                    else
                    {
                        if ((mask & 1) != 0)
                        {
                            string abs = ResolveAbsPath(originalDir, additionalOriginalDirs, relKey);
                            entryMap.TryGetValue((relKey, SearchSide.Original), out var metaOriginal);
                            var routeO = RouteFor(textRouteMap, relKey, SearchSide.Original);
                            originalHits = VerifyFileAllHits(
                                root,
                                relKey,
                                SearchSide.Original,
                                abs,
                                metaOriginal?.LastWriteUtcTicks ?? 0,
                                metaOriginal?.LengthBytes ?? 0,
                                routeO.Entry,
                                effectiveQuery,
                                contextWidth,
                                htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent,
                                textBinFileName: routeO.TextBinFileName,
                                textFamily: routeO.Family);
                            Interlocked.Increment(ref verifiedDocs);
                            hitsO = originalHits.Count;
                            Interlocked.Add(ref totalHits, hitsO);
                        }

                        if ((mask & 2) != 0)
                        {
                            string abs = ResolveAbsPath(translatedDir, additionalTranslatedDirs, relKey);
                            entryMap.TryGetValue((relKey, SearchSide.Translated), out var metaTranslated);
                            var routeT = RouteFor(textRouteMap, relKey, SearchSide.Translated);
                            translatedHits = VerifyFileAllHits(
                                root,
                                relKey,
                                SearchSide.Translated,
                                abs,
                                metaTranslated?.LastWriteUtcTicks ?? 0,
                                metaTranslated?.LengthBytes ?? 0,
                                routeT.Entry,
                                effectiveQuery,
                                contextWidth,
                                htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent,
                                textBinFileName: routeT.TextBinFileName,
                                textFamily: routeT.Family);
                            Interlocked.Increment(ref verifiedDocs);
                            hitsT = translatedHits.Count;
                            Interlocked.Add(ref totalHits, hitsT);
                        }
                    }

                    group.HitsOriginal = hitsO;
                    group.HitsTranslated = hitsT;
                    if (skipVerify)
                    {
                        // Mark each placeholder child with IsSkippedVerify=true so UI templates
                        // can branch on it. The child's Hit is a single-position empty placeholder
                        // (Snippet text is effectively null/empty).
                        var placeholders = BuildResultChildren(relKey, originalHits, translatedHits);
                        foreach (var c in placeholders) c.IsSkippedVerify = true;
                        group.Children.AddRange(placeholders);
                    }
                    else
                    {
                        group.Children.AddRange(BuildResultChildren(relKey, originalHits, translatedHits));
                    }

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
                Dbg($"Verify phase DONE in {swVerify.ElapsedMilliseconds}ms verified={verifiedDocs}/{totalDocsToVerify} groups={emittedGroups} hits={totalHits} skippedVerifyGroups={skippedVerifyGroups}");
                Interlocked.Exchange(ref _lastSearchSkippedVerifyGroups, skippedVerifyGroups);
                Interlocked.Exchange(ref _lastSearchVerifiedGroups, emittedGroups - skippedVerifyGroups);

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

    /// <inheritdoc />
    /// <remarks>
    /// PR A (load-all-snippets): re-verifies skip-verify placeholder rows on demand.
    ///
    /// Implementation notes:
    /// - Walks <paramref name="groups"/> once to collect every (relPath, sideMask) tuple
    ///   whose group currently has at least one <see cref="SearchResultChild.IsSkippedVerify"/>=true
    ///   child. The side mask is derived from the placeholder children themselves so we
    ///   never re-verify sides the caller never requested.
    /// - Runs <c>VerifyFileAllHits</c> for each tuple under <c>Parallel.ForEach</c> with
    ///   <see cref="SearchIndexServiceOptions.MaxVerifyDegreeOfParallelism"/>.
    /// - Returns a per-relPath dictionary of fresh children. The caller (view-model) is
    ///   responsible for: (a) UI-thread marshalling, (b) preserving group identity, and
    ///   (c) re-applying any UI-side children cap. The service deliberately does NOT
    ///   mutate <see cref="SearchResultGroup"/> instances — that decoupling is required
    ///   because <c>SearchResultGroup.Children</c> may be an <c>ObservableCollection</c>
    ///   in the live UI (mutating from a worker thread would throw / corrupt bindings),
    ///   and identity preservation lives one layer up (see PR4 from the prior sprint).
    /// - Cancellation: <c>Parallel.ForEach</c>'s built-in CancellationToken support is used;
    ///   already-completed tuples remain in the returned dictionary so the caller can apply
    ///   partial progress. A canceled call throws <see cref="OperationCanceledException"/>.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(
        string root,
        string originalDir,
        string translatedDir,
        SearchIndexManifest manifest,
        IReadOnlyList<SearchResultGroup> groups,
        string query,
        int contextWidth,
        IProgress<SearchProgress>? progress = null,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        CancellationToken ct = default)
    {
        var emptyResult = new Dictionary<string, IReadOnlyList<SearchResultChild>>(StringComparer.OrdinalIgnoreCase);
        if (groups == null || groups.Count == 0)
            return emptyResult;

        query = (query ?? "").Trim();
        if (query.Length == 0)
            return emptyResult;

        // Mirror SearchAllAsync: CJK queries are normalized so the verify path uses the
        // same effective query string that the original search used to detect candidates.
        string effectiveQuery = CjkMatchNormalizer.ContainsCjk(query)
            ? CjkMatchNormalizer.Normalize(query)
            : query;
        if (effectiveQuery.Length == 0)
            return emptyResult;

        // Collect promotion targets: each group keeps the union of sides we need to verify,
        // derived from the IsSkippedVerify children themselves (so we never re-verify a side
        // the original search didn't request — preserves include-original/include-translated
        // selections without needing to thread them through).
        var promote = new List<(SearchResultGroup group, int sideMask)>(groups.Count);
        foreach (var g in groups)
        {
            if (g.Children == null || g.Children.Count == 0) continue;
            int sideMask = 0;
            foreach (var c in g.Children)
            {
                if (!c.IsSkippedVerify) continue;
                if (c.Side == SearchSide.Original) sideMask |= 1;
                else if (c.Side == SearchSide.Translated) sideMask |= 2;
            }
            if (sideMask != 0)
                promote.Add((g, sideMask));
        }

        if (promote.Count == 0)
            return emptyResult;

        Dbg($"LoadSnippetsForAsync START promoting={promote.Count} q='{query}' effectiveQ='{effectiveQuery}'");

        // Pre-build the entry/text-route maps once for the whole batch (mirrors SearchAllAsync).
        // FL4: entryMap union + per-(rel, side) text sidecar routing across the loaded families.
        var families = _families;
        var entries = manifest?.Entries ?? new List<SearchIndexEntry>();
        var entryMap = entries.ToDictionary(e => (e.RelPath, e.Side), e => e, new RelSideComparer());
        var textRouteMap = await BuildTextRouteMapAsync(root, families);

        var result = new ConcurrentDictionary<string, IReadOnlyList<SearchResultChild>>(StringComparer.OrdinalIgnoreCase);
        int totalToVerify = 0;
        foreach (var (_, mask) in promote)
        {
            if ((mask & 1) != 0) totalToVerify++;
            if ((mask & 2) != 0) totalToVerify++;
        }

        int verifiedDocs = 0;
        int totalHits = 0;

        progress?.Report(new SearchProgress
        {
            Phase = "Loading snippets...",
            Candidates = totalToVerify,
            TotalDocsToVerify = totalToVerify
        });

        var verifyPo = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Math.Max(1, Options.MaxVerifyDegreeOfParallelism)
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await Task.Run(() =>
        {
            Parallel.ForEach(promote, verifyPo, item =>
            {
                ct.ThrowIfCancellationRequested();

                var (group, mask) = item;
                var relKey = group.RelPath;

                List<SearchHit> originalHits = new();
                List<SearchHit> translatedHits = new();

                if ((mask & 1) != 0)
                {
                    string abs = ResolveAbsPath(originalDir, additionalOriginalDirs, relKey);
                    entryMap.TryGetValue((relKey, SearchSide.Original), out var metaOriginal);
                    var routeO = RouteFor(textRouteMap, relKey, SearchSide.Original);
                    originalHits = VerifyFileAllHits(
                        root,
                        relKey,
                        SearchSide.Original,
                        abs,
                        metaOriginal?.LastWriteUtcTicks ?? 0,
                        metaOriginal?.LengthBytes ?? 0,
                        routeO.Entry,
                        effectiveQuery,
                        contextWidth,
                        htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent,
                        textBinFileName: routeO.TextBinFileName,
                        textFamily: routeO.Family);
                    Interlocked.Increment(ref verifiedDocs);
                    Interlocked.Add(ref totalHits, originalHits.Count);
                }

                if ((mask & 2) != 0)
                {
                    string abs = ResolveAbsPath(translatedDir, additionalTranslatedDirs, relKey);
                    entryMap.TryGetValue((relKey, SearchSide.Translated), out var metaTranslated);
                    var routeT = RouteFor(textRouteMap, relKey, SearchSide.Translated);
                    translatedHits = VerifyFileAllHits(
                        root,
                        relKey,
                        SearchSide.Translated,
                        abs,
                        metaTranslated?.LastWriteUtcTicks ?? 0,
                        metaTranslated?.LengthBytes ?? 0,
                        routeT.Entry,
                        effectiveQuery,
                        contextWidth,
                        htmlDecodeIfAmpersandPresent: Options.HtmlDecodeIfAmpersandPresent,
                        textBinFileName: routeT.TextBinFileName,
                        textFamily: routeT.Family);
                    Interlocked.Increment(ref verifiedDocs);
                    Interlocked.Add(ref totalHits, translatedHits.Count);
                }

                var realChildren = BuildResultChildren(relKey, originalHits, translatedHits);
                // Empty hits means the verify produced no real snippets (file gone, etc.).
                // Still report a result so caller can drop the placeholder children — but
                // only if at least one hit was produced. If both sides return zero hits
                // (extremely unlikely since the search index proved adjacency), keep the
                // original placeholders unchanged by omitting from the result map.
                if (realChildren.Count > 0)
                    result[relKey] = realChildren;

                int v = Volatile.Read(ref verifiedDocs);
                if (v <= 10 || v % 10 == 0)
                {
                    int hitsNow = Volatile.Read(ref totalHits);
                    progress?.Report(new SearchProgress
                    {
                        Phase = "Loading snippets...",
                        Candidates = totalToVerify,
                        VerifiedDocs = v,
                        TotalDocsToVerify = totalToVerify,
                        Groups = result.Count,
                        TotalHits = hitsNow,
                        VerifyMs = sw.ElapsedMilliseconds,
                        TotalMs = sw.ElapsedMilliseconds
                    });
                }
            });
        }, ct).ConfigureAwait(false);

        sw.Stop();
        Dbg($"LoadSnippetsForAsync DONE in {sw.ElapsedMilliseconds}ms promoted={result.Count}/{promote.Count} hits={totalHits}");

        progress?.Report(new SearchProgress
        {
            Phase = "Done",
            Candidates = totalToVerify,
            VerifiedDocs = verifiedDocs,
            TotalDocsToVerify = totalToVerify,
            Groups = result.Count,
            TotalHits = totalHits,
            VerifyMs = sw.ElapsedMilliseconds,
            TotalMs = sw.ElapsedMilliseconds
        });

        return new Dictionary<string, IReadOnlyList<SearchResultChild>>(result, StringComparer.OrdinalIgnoreCase);
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
        bool htmlDecodeIfAmpersandPresent,
        string textBinFileName,
        SearchFamily textFamily)
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
            htmlDecodeIfAmpersandPresent,
            textBinFileName,
            textFamily);

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



















