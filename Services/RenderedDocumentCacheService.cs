using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class RenderedDocumentCacheService
{
    private sealed class Entry
    {
        public FileStamp Stamp;
        public RenderedDocument Doc;
        public long LastAccessTicks;
        public Entry(FileStamp stamp, RenderedDocument doc)
        {
            Stamp = stamp;
            Doc = doc;
            LastAccessTicks = DateTime.UtcNow.Ticks;
        }
    }

    private readonly ConcurrentDictionary<string, Entry> _cache = new(StringComparer.OrdinalIgnoreCase);

    // Keep this small; rendered text can be large.
    private readonly int _maxEntries;

    public RenderedDocumentCacheService(int maxEntries = 32)
    {
        _maxEntries = Math.Max(4, maxEntries);
    }

    private static string MakeKey(FileStamp stamp) => stamp.AbsPath;

    public bool TryGet(FileStamp stamp, out RenderedDocument doc)
    {
        doc = RenderedDocument.Empty;

        var key = MakeKey(stamp);
        if (_cache.TryGetValue(key, out var e))
        {
            if (e.Stamp.IsSameContentAs(stamp))
            {
                e.LastAccessTicks = DateTime.UtcNow.Ticks;
                doc = e.Doc;
                return true;
            }

            // stale
            _cache.TryRemove(key, out _);
        }

        return false;
    }

    public void Put(FileStamp stamp, RenderedDocument doc)
    {
        var key = MakeKey(stamp);
        _cache[key] = new Entry(stamp, doc);

        TrimIfNeeded();
    }

    public void Invalidate(string absPath)
    {
        if (string.IsNullOrWhiteSpace(absPath)) return;
        _cache.TryRemove(absPath, out _);
    }

    public void Clear() => _cache.Clear();

    private void TrimIfNeeded()
    {
        // cheap trim: if over cap, remove oldest ~25%
        int count = _cache.Count;
        if (count <= _maxEntries) return;

        var list = new List<(string key, long lastAccess)>(count);
        foreach (var kv in _cache)
            list.Add((kv.Key, kv.Value.LastAccessTicks));

        list.Sort((a, b) => a.lastAccess.CompareTo(b.lastAccess));

        int removeCount = Math.Max(1, (count - _maxEntries) + (_maxEntries / 4));
        for (int i = 0; i < removeCount && i < list.Count; i++)
            _cache.TryRemove(list[i].key, out _);
    }
}
