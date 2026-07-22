using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// One longest-match hit against the Zen dictionary at a position in a Chinese string.
/// Mirrors <see cref="CedictMatch"/> but carries the owning rich <see cref="DictionaryEntry"/>
/// (the whole article, all senses) so a card renderer has everything it needs.
/// </summary>
public sealed record ZenDictionaryMatch(
    string Headword,
    int StartIndex,
    int Length,
    DictionaryEntry Entry);

/// <summary>
/// Reader-facing lookup over the rich Zen dictionary. <see cref="IDictionaryStore"/> is
/// load/save only; this is the missing query layer. It builds an in-memory longest-match
/// index (a char trie, mirroring <see cref="CedictDictionaryService"/>) keyed on every
/// entry's <see cref="DictionaryEntry.SourceTerm"/> plus every sense's
/// <see cref="DictionarySense.SearchAliases"/> value, each pointing at the owning entry.
/// Longest match wins, exactly like the CC-CEDICT trie, so a UI can switch between the two
/// behind a common notion.
/// </summary>
public interface IZenDictionaryLookup
{
    /// <summary>True once the index has been built for some root.</summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Build (or rebuild) the lookup index from the dictionary at <paramref name="root"/>.
    /// Thread-safe and idempotent: concurrent callers await the same in-flight build, and a
    /// call for an already-loaded root is a no-op. Passing a different root rebuilds.
    /// </summary>
    Task EnsureLoadedAsync(string root, CancellationToken ct = default);

    /// <summary>
    /// Longest dictionary term (or alias) that starts at <paramref name="startIndex"/> in
    /// <paramref name="text"/>. Returns false if the index is unloaded or nothing matches.
    /// </summary>
    bool TryLookupLongest(string text, int startIndex, out ZenDictionaryMatch match, int maxLen = 16);

    /// <summary>Exact lookup of a whole term (head term or alias) to its owning entry.</summary>
    bool TryLookupExact(string term, out DictionaryEntry entry);
}

public sealed class ZenDictionaryLookupService : IZenDictionaryLookup
{
    private readonly IDictionaryStore _store;

    private readonly object _gate = new();
    private volatile bool _loaded;
    private string? _loadedRoot;
    private Task? _loadTask;
    private string? _pendingRoot;

    private TrieNode _root = new();
    private int _maxTermLenSeen = 1;

    public ZenDictionaryLookupService(IDictionaryStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public bool IsLoaded => _loaded;

    public Task EnsureLoadedAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        Task loadTask;

        lock (_gate)
        {
            // Already built for this exact root — nothing to do.
            if (_loaded && string.Equals(_loadedRoot, root, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            // Reuse an in-flight build for the same root so callers don't race a rebuild.
            if (_loadTask is { IsCompleted: false } &&
                string.Equals(_pendingRoot, root, StringComparison.OrdinalIgnoreCase))
            {
                loadTask = _loadTask;
            }
            else
            {
                _pendingRoot = root;
                _loadTask = LoadCoreAsync(root);
                loadTask = _loadTask;
            }
        }

        if (!ct.CanBeCanceled)
            return loadTask;

        return WaitWithCancellationAsync(loadTask, ct);
    }

    private async Task LoadCoreAsync(string root)
    {
        try
        {
            var file = await _store.LoadAsync(root).ConfigureAwait(false);

            var (trie, maxLen) = await Task.Run(() => BuildIndex(file)).ConfigureAwait(false);

            lock (_gate)
            {
                _root = trie;
                _maxTermLenSeen = Math.Max(1, maxLen);
                _loadedRoot = root;
                _loaded = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[ZENDICT] LOAD FAILED: " + ex);
            throw;
        }
    }

    /// <summary>
    /// Build the trie. Head terms are inserted first so a real head term always wins a key
    /// collision with an alias; aliases only fill keys no head term already owns.
    /// </summary>
    private static (TrieNode Root, int MaxLen) BuildIndex(DictionaryFile? file)
    {
        var root = new TrieNode();
        int maxLen = 1;

        if (file?.Entries == null)
            return (root, maxLen);

        // Pass 1: head terms (authoritative keys).
        foreach (var entry in file.Entries)
        {
            var term = entry?.SourceTerm;
            if (string.IsNullOrWhiteSpace(term)) continue;

            if (AddToTrie(root, term!, entry!, overwrite: true))
                maxLen = Math.Max(maxLen, term!.Length);
        }

        // Pass 2: search aliases (only where no head term already claims the key).
        foreach (var entry in file.Entries)
        {
            if (entry?.Senses == null) continue;
            foreach (var sense in entry.Senses)
            {
                if (sense?.SearchAliases == null) continue;
                foreach (var alias in sense.SearchAliases)
                {
                    if (string.IsNullOrWhiteSpace(alias)) continue;
                    if (AddToTrie(root, alias, entry, overwrite: false))
                        maxLen = Math.Max(maxLen, alias.Length);
                }
            }
        }

        return (root, maxLen);
    }

    public bool TryLookupLongest(string text, int startIndex, out ZenDictionaryMatch match, int maxLen = 16)
    {
        match = default!;

        if (!_loaded) return false;
        if (string.IsNullOrEmpty(text)) return false;
        if (startIndex < 0 || startIndex >= text.Length) return false;

        int cap = Math.Min(maxLen, _maxTermLenSeen);
        cap = Math.Min(cap, text.Length - startIndex);
        if (cap <= 0) return false;

        TrieNode? node = _root;
        int bestLen = 0;
        DictionaryEntry? bestEntry = null;

        for (int i = 0; i < cap; i++)
        {
            char ch = text[startIndex + i];
            if (node == null || !node.Next.TryGetValue(ch, out node))
                break;

            if (node.Entry != null)
            {
                bestLen = i + 1;
                bestEntry = node.Entry;
            }
        }

        if (bestLen <= 0 || bestEntry == null)
            return false;

        match = new ZenDictionaryMatch(
            Headword: text.Substring(startIndex, bestLen),
            StartIndex: startIndex,
            Length: bestLen,
            Entry: bestEntry);
        return true;
    }

    public bool TryLookupExact(string term, out DictionaryEntry entry)
    {
        entry = default!;

        if (!_loaded) return false;
        if (string.IsNullOrEmpty(term)) return false;

        TrieNode? node = _root;
        foreach (char ch in term)
        {
            if (node == null || !node.Next.TryGetValue(ch, out node))
                return false;
        }

        if (node?.Entry == null)
            return false;

        entry = node.Entry;
        return true;
    }

    /// <summary>Insert a key. Returns true if the key now points at <paramref name="entry"/>.</summary>
    private static bool AddToTrie(TrieNode root, string key, DictionaryEntry entry, bool overwrite)
    {
        var node = root;
        foreach (char ch in key)
        {
            if (!node.Next.TryGetValue(ch, out var next))
            {
                next = new TrieNode();
                node.Next[ch] = next;
            }
            node = next;
        }

        if (node.Entry != null && !overwrite)
            return false;

        node.Entry = entry;
        return true;
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

    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Next { get; } = new();
        public DictionaryEntry? Entry { get; set; }
    }
}
