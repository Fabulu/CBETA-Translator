using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ICedictDictionary
{
    bool IsLoaded { get; }
    string DictionaryPath { get; }

    Task EnsureLoadedAsync(CancellationToken ct = default);

    bool TryLookupLongest(string text, int startIndex, out CedictMatch match, int maxLen = 12);
    bool TryLookupChar(char c, out IReadOnlyList<CedictEntry> entries);
}

public sealed class CedictDictionaryService : ICedictDictionary
{
    private readonly object _gate = new();
    private volatile bool _loaded;

    private TrieNode _rootTrad = new();
    private TrieNode _rootSimp = new();
    private int _maxWordLenSeen = 1;

    // Debug-visible state
    private volatile bool _isLoading;
    private volatile string? _lastLoadError;

    // The key fix: all concurrent callers await the same load task instead of "returning early"
    private Task? _loadTask;

    public bool IsLoaded => _loaded;
    public bool IsLoading => _isLoading;
    public string? LastLoadError => _lastLoadError;

    public string DictionaryPath { get; }

    public CedictDictionaryService(string? dictionaryPath = null)
    {
        DictionaryPath = dictionaryPath ?? AppPaths.GetCedictPath();
    }

    public Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded) return Task.CompletedTask;

        Task loadTask;

        lock (_gate)
        {
            if (_loaded) return Task.CompletedTask;

            // Reuse in-flight load task so callers don't "bounce off" _isLoading and continue uninitialized.
            if (_loadTask == null || _loadTask.IsCompleted)
            {
                _isLoading = true;
                _lastLoadError = null;
                _loadTask = LoadCoreAsync();
            }

            loadTask = _loadTask;
        }

        // Each caller can cancel *its own wait* without cancelling the shared load.
        if (!ct.CanBeCanceled)
            return loadTask;

        return WaitWithCancellationAsync(loadTask, ct);
    }

    private async Task LoadCoreAsync()
    {
        try
        {
            Debug.WriteLine("[CEDICT] EnsureLoadedAsync called");
            Debug.WriteLine($"[CEDICT] AppContext.BaseDirectory = {AppContext.BaseDirectory}");
            Debug.WriteLine($"[CEDICT] DictionaryPath = {DictionaryPath}");
            Debug.WriteLine($"[CEDICT] Exists(DictionaryPath) = {File.Exists(DictionaryPath)}");

            AppPaths.EnsureCedictFolderExists();

            if (!File.Exists(DictionaryPath))
                throw new FileNotFoundException(
                    "CC-CEDICT dictionary file not found. Place it at: " + DictionaryPath,
                    DictionaryPath);

            // Build off-thread
            var (trad, simp, maxLen, stats) = await Task.Run(() =>
            {
                var tRoot = new TrieNode();
                var sRoot = new TrieNode();
                int maxWordLen = 1;

                long lines = 0;
                long parsed = 0;
                long skipped = 0;
                long addedTrad = 0;
                long addedSimp = 0;

                var sw = Stopwatch.StartNew();

                using var fs = File.OpenRead(DictionaryPath);
                using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0) { skipped++; continue; }
                    if (line.StartsWith("#")) { skipped++; continue; }

                    lines++;

                    if ((lines % 200000) == 0)
                        Debug.WriteLine($"[CEDICT] reading... lines={lines:n0} parsed={parsed:n0} skipped={skipped:n0} elapsed={sw.Elapsed}");

                    if (!TryParseLine(line, out var entry))
                    {
                        skipped++;
                        continue;
                    }

                    parsed++;

                    if (!string.IsNullOrWhiteSpace(entry.Traditional))
                    {
                        AddToTrie(tRoot, entry.Traditional, entry);
                        addedTrad++;
                        maxWordLen = Math.Max(maxWordLen, entry.Traditional.Length);
                    }

                    if (!string.IsNullOrWhiteSpace(entry.Simplified))
                    {
                        AddToTrie(sRoot, entry.Simplified, entry);
                        addedSimp++;
                        maxWordLen = Math.Max(maxWordLen, entry.Simplified.Length);
                    }
                }

                Debug.WriteLine($"[CEDICT] DONE lines={lines:n0} parsed={parsed:n0} skipped={skipped:n0} trad={addedTrad:n0} simp={addedSimp:n0} maxLen={maxWordLen} elapsed={sw.Elapsed}");

                return (tRoot, sRoot, maxWordLen, (lines, parsed, skipped, addedTrad, addedSimp, sw.Elapsed));
            }).ConfigureAwait(false);

            lock (_gate)
            {
                if (!_loaded)
                {
                    _rootTrad = trad;
                    _rootSimp = simp;
                    _maxWordLenSeen = Math.Max(1, maxLen);
                    _loaded = true;

                    Debug.WriteLine($"[CEDICT] Loaded OK. maxWordLenSeen={_maxWordLenSeen} tradRoots={_rootTrad.Next.Count} simpRoots={_rootSimp.Next.Count}");
                    Debug.WriteLine($"[CEDICT] Stats: lines={stats.lines:n0} parsed={stats.parsed:n0} skipped={stats.skipped:n0} trad={stats.addedTrad:n0} simp={stats.addedSimp:n0} elapsed={stats.Elapsed}");
                }
            }
        }
        catch (Exception ex)
        {
            _lastLoadError = ex.ToString();
            Debug.WriteLine("[CEDICT] LOAD FAILED: " + _lastLoadError);
            throw;
        }
        finally
        {
            lock (_gate)
            {
                _isLoading = false;
            }
        }
    }

    private static async Task WaitWithCancellationAsync(Task task, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs);

        if (task == await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
        {
            await task.ConfigureAwait(false);
            return;
        }

        throw new OperationCanceledException(ct);
    }

    public bool TryLookupLongest(string text, int startIndex, out CedictMatch match, int maxLen = 12)
    {
        match = default!;

        if (!_loaded) return false;
        if (string.IsNullOrEmpty(text)) return false;
        if (startIndex < 0 || startIndex >= text.Length) return false;

        char first = text[startIndex];
        if (!IsCjk(first)) return false;

        int cap = Math.Min(maxLen, _maxWordLenSeen);
        cap = Math.Min(cap, text.Length - startIndex);

        bool found = false;
        CedictMatch best = default!;

        if (TryWalkLongest(_rootTrad, text, startIndex, cap, out var mTrad))
        {
            found = true;
            best = mTrad;
        }

        if (TryWalkLongest(_rootSimp, text, startIndex, cap, out var mSimp))
        {
            if (!found || mSimp.Length > best.Length)
            {
                found = true;
                best = mSimp;
            }
        }

        if (!found) return false;

        match = best;
        return true;
    }

    public bool TryLookupChar(char c, out IReadOnlyList<CedictEntry> entries)
    {
        entries = Array.Empty<CedictEntry>();
        if (!_loaded) return false;
        if (!IsCjk(c)) return false;

        if (TryWalkExact(_rootTrad, c.ToString(), 0, out var t) && t.Count > 0)
        {
            entries = t;
            return true;
        }

        if (TryWalkExact(_rootSimp, c.ToString(), 0, out var s) && s.Count > 0)
        {
            entries = s;
            return true;
        }

        return false;
    }

    private static bool TryWalkLongest(TrieNode root, string text, int start, int cap, out CedictMatch match)
    {
        match = default!;

        TrieNode? node = root;
        int bestLen = 0;
        List<CedictEntry>? bestEntries = null;

        for (int i = 0; i < cap; i++)
        {
            char ch = text[start + i];
            if (node == null || !node.Next.TryGetValue(ch, out node))
                break;

            if (node.Terminal != null && node.Terminal.Count > 0)
            {
                bestLen = i + 1;
                bestEntries = node.Terminal;
            }
        }

        if (bestLen <= 0 || bestEntries == null)
            return false;

        match = new CedictMatch(
            Headword: text.Substring(start, bestLen),
            StartIndex: start,
            Length: bestLen,
            Entries: bestEntries);

        return true;
    }

    private static bool TryWalkExact(TrieNode root, string text, int start, out List<CedictEntry> entries)
    {
        entries = new List<CedictEntry>();
        TrieNode? node = root;

        for (int i = start; i < text.Length; i++)
        {
            char ch = text[i];
            if (node == null || !node.Next.TryGetValue(ch, out node))
                return false;
        }

        if (node?.Terminal == null || node.Terminal.Count == 0)
            return false;

        entries = node.Terminal;
        return true;
    }

    private static void AddToTrie(TrieNode root, string headword, CedictEntry entry)
    {
        var node = root;
        foreach (char ch in headword)
        {
            if (!node.Next.TryGetValue(ch, out var next))
            {
                next = new TrieNode();
                node.Next[ch] = next;
            }
            node = next;
        }

        node.Terminal ??= new List<CedictEntry>();
        node.Terminal.Add(entry);
    }

    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Next { get; } = new();
        public List<CedictEntry>? Terminal { get; set; }
    }

    private static bool TryParseLine(string line, out CedictEntry entry)
    {
        entry = default!;

        int sp1 = line.IndexOf(' ');
        if (sp1 <= 0) return false;

        int sp2 = line.IndexOf(' ', sp1 + 1);
        if (sp2 <= sp1 + 1) return false;

        string trad = line.Substring(0, sp1).Trim();
        string simp = line.Substring(sp1 + 1, sp2 - (sp1 + 1)).Trim();
        string rest = line.Substring(sp2 + 1).Trim();

        if (trad.Length == 0 && simp.Length == 0) return false;

        int b1 = rest.IndexOf('[');
        int b2 = rest.IndexOf(']', b1 + 1);
        if (b1 < 0 || b2 < 0) return false;

        string pinyinRaw = rest.Substring(b1 + 1, b2 - (b1 + 1)).Trim();
        pinyinRaw = pinyinRaw.Trim('[', ']'); // handles [[...]] too

        int s1 = rest.IndexOf('/', b2 + 1);
        int sLast = rest.LastIndexOf('/');
        if (s1 < 0 || sLast <= s1) return false;

        string sensesBlob = rest.Substring(s1 + 1, sLast - (s1 + 1));
        var senses = sensesBlob
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToArray();

        if (senses.Length == 0) return false;

        entry = new CedictEntry(trad, simp, pinyinRaw, senses);
        return true;
    }

    private static bool IsCjk(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)
            || (c >= 0x3400 && c <= 0x4DBF)
            || (c >= 0xF900 && c <= 0xFAFF)
            || (c >= 0x20000 && c <= 0x2A6DF)
            || (c >= 0x2A700 && c <= 0x2B73F)
            || (c >= 0x2B740 && c <= 0x2B81F)
            || (c >= 0x2B820 && c <= 0x2CEAF);
    }
}