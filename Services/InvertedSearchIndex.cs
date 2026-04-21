using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

/// <summary>
/// Bigram inverted index for fast, exact-match candidate filtering.
/// Replaces bloom filters with 0% false positive rate.
///
/// v2 format: varint-delta encoded postings, high-DF cutoff.
/// Typical size: ~10-20MB for 5000 CJK files (down from 173MB in v1).
/// </summary>
public sealed class InvertedSearchIndex
{
    private static readonly byte[] Magic = "IIDX"u8.ToArray();
    private const int Version = 2;
    private const double MaxDocFrequencyRatio = 0.8; // skip only truly ubiquitous bigrams (之所, 如是, etc.)

    private Dictionary<string, ushort[]>? _index;
    private string[]? _docPaths;

    public int TermCount => _index?.Count ?? 0;
    public int DocCount => _docPaths?.Length ?? 0;
    public bool IsLoaded => _index != null;

    /// <summary>Only CJK ideographs are worth indexing.</summary>
    private static bool IsIndexable(char ch)
        => (ch >= '\u4E00' && ch <= '\u9FFF')
        || (ch >= '\u3400' && ch <= '\u4DBF')
        || (ch >= '\uF900' && ch <= '\uFAFF');

    public void Build(IReadOnlyList<(string relPath, string searchableText)> documents)
    {
        // Deduplicate by relPath — keep first occurrence only
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dedupedDocs = new List<(string relPath, string text)>();
        foreach (var (relPath, text) in documents)
        {
            if (seenPaths.Add(relPath))
                dedupedDocs.Add((relPath, text));
        }

        int docCount = dedupedDocs.Count;
        int maxDf = (int)(docCount * MaxDocFrequencyRatio);

        _docPaths = new string[docCount];
        var tempIndex = new Dictionary<long, List<ushort>>(16384);
        var seen = new HashSet<long>(); // reused per doc

        for (int docId = 0; docId < docCount; docId++)
        {
            var (relPath, text) = dedupedDocs[docId];
            _docPaths[docId] = relPath;

            seen.Clear();
            for (int i = 0; i < text.Length - 1; i++)
            {
                char c0 = text[i], c1 = text[i + 1];
                if (!IsIndexable(c0) || !IsIndexable(c1)) continue;

                long key = ((long)c0 << 16) | c1;
                if (!seen.Add(key)) continue;

                if (!tempIndex.TryGetValue(key, out var list))
                {
                    list = new List<ushort>();
                    tempIndex[key] = list;
                }
                list.Add((ushort)docId);
            }
        }

        // Convert to string-keyed dictionary, skipping high-DF terms
        _index = new Dictionary<string, ushort[]>(tempIndex.Count);
        int skipped = 0;
        foreach (var (key, list) in tempIndex)
        {
            if (list.Count > maxDf) { skipped++; continue; } // too common = useless for filtering
            char c0 = (char)(key >> 16), c1 = (char)(key & 0xFFFF);
            _index[string.Concat(c0, c1)] = list.ToArray();
        }
        tempIndex.Clear(); // free memory immediately

        System.Diagnostics.Debug.WriteLine(
            $"[InvertedIndex] Built: {_index.Count} terms ({skipped} skipped >80% DF), {docCount} docs");
    }

    /// <summary>Query: returns sorted doc IDs containing ALL query bigrams.</summary>
    public ushort[]? Search(string query)
    {
        if (_index == null || _docPaths == null || query.Length < 2) return null;

        var bigrams = new List<string>();
        for (int i = 0; i < query.Length - 1; i++)
        {
            char c0 = query[i], c1 = query[i + 1];
            if (char.IsWhiteSpace(c0) || char.IsWhiteSpace(c1)) continue;
            bigrams.Add(string.Concat(c0, c1));
        }
        if (bigrams.Count == 0) return null;

        ushort[]? result = null;
        foreach (var bg in bigrams.OrderBy(b => _index.TryGetValue(b, out var l) ? l.Length : 0))
        {
            if (!_index.TryGetValue(bg, out var postings))
                return Array.Empty<ushort>(); // not found = 0 results

            result = result == null ? postings : Intersect(result, postings);
            if (result.Length == 0) return result;
        }
        return result ?? Array.Empty<ushort>();
    }

    public string? GetRelPath(ushort docId)
        => _docPaths != null && docId < _docPaths.Length ? _docPaths[docId] : null;

    private static ushort[] Intersect(ushort[] a, ushort[] b)
    {
        var result = new List<ushort>(Math.Min(a.Length, b.Length));
        int i = 0, j = 0;
        while (i < a.Length && j < b.Length)
        {
            if (a[i] == b[j]) { result.Add(a[i]); i++; j++; }
            else if (a[i] < b[j]) i++;
            else j++;
        }
        return result.ToArray();
    }

    /// <summary>Save with varint-delta encoding. Writes directly to file.</summary>
    public async Task SaveAsync(string path, CancellationToken ct = default)
    {
        if (_index == null || _docPaths == null) return;

        await File.WriteAllLinesAsync(path + ".paths", _docPaths, Encoding.UTF8, ct);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536);
        using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true);

        var sorted = _index.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();

        // Header
        bw.Write(Magic);
        bw.Write(Version);
        bw.Write(sorted.Count);
        bw.Write(_docPaths.Length);

        // Pre-compute varint-delta encoded postings to know offsets
        var encodedPostings = new List<byte[]>(sorted.Count);
        foreach (var (_, postings) in sorted)
            encodedPostings.Add(EncodeVarintDelta(postings));

        // Dictionary section
        int postingOffset = 0;
        for (int i = 0; i < sorted.Count; i++)
        {
            var (term, postings) = sorted[i];
            var encoded = encodedPostings[i];
            var termBytes = Encoding.UTF8.GetBytes(term);
            bw.Write((ushort)termBytes.Length);
            bw.Write(termBytes);
            bw.Write(postingOffset);
            bw.Write((ushort)postings.Length);
            postingOffset += encoded.Length;
        }

        // Postings section (varint-delta encoded)
        foreach (var encoded in encodedPostings)
            bw.Write(encoded);

        bw.Flush();
    }

    /// <summary>Load varint-delta encoded index.</summary>
    public async Task<bool> TryLoadAsync(string path, CancellationToken ct = default)
    {
        var pathsFile = path + ".paths";
        if (!File.Exists(path) || !File.Exists(pathsFile)) return false;

        try
        {
            _docPaths = await File.ReadAllLinesAsync(pathsFile, Encoding.UTF8, ct);
            var data = await File.ReadAllBytesAsync(path, ct);
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.UTF8);

            var magic = br.ReadBytes(4);
            if (!magic.SequenceEqual(Magic)) return false;

            int version = br.ReadInt32();
            if (version != Version)
            {
                // Also accept v1 for backward compat — but v1 postings are raw ushort
                if (version == 1) return TryLoadV1(br, ms);
                return false;
            }

            int termCount = br.ReadInt32();
            int fileCount = br.ReadInt32();

            // Read dictionary
            var entries = new List<(string term, int offset, ushort count)>(termCount);
            for (int i = 0; i < termCount; i++)
            {
                int termLen = br.ReadUInt16();
                string term = Encoding.UTF8.GetString(br.ReadBytes(termLen));
                int offset = br.ReadInt32();
                ushort count = br.ReadUInt16();
                entries.Add((term, offset, count));
            }

            long postingsStart = ms.Position;

            _index = new Dictionary<string, ushort[]>(termCount);
            foreach (var (term, offset, count) in entries)
            {
                ms.Seek(postingsStart + offset, SeekOrigin.Begin);
                _index[term] = DecodeVarintDelta(br, count);
            }

            return true;
        }
        catch
        {
            _index = null;
            _docPaths = null;
            return false;
        }
    }

    /// <summary>Backward compat: load v1 format (raw ushort postings, no DF cutoff).</summary>
    private bool TryLoadV1(BinaryReader br, MemoryStream ms)
    {
        try
        {
            int termCount = br.ReadInt32();
            int fileCount = br.ReadInt32();
            int postingsOffset = br.ReadInt32();

            var dict = new List<(string term, int offset, ushort count)>(termCount);
            for (int i = 0; i < termCount; i++)
            {
                int termLen = br.ReadUInt16();
                string term = Encoding.UTF8.GetString(br.ReadBytes(termLen));
                int offset = br.ReadInt32();
                ushort count = br.ReadUInt16();
                dict.Add((term, offset, count));
            }

            _index = new Dictionary<string, ushort[]>(termCount);
            foreach (var (term, offset, count) in dict)
            {
                ms.Seek(postingsOffset + offset, SeekOrigin.Begin);
                var postings = new ushort[count];
                for (int i = 0; i < count; i++)
                    postings[i] = br.ReadUInt16();
                _index[term] = postings;
            }
            return true;
        }
        catch { return false; }
    }

    // --- Varint delta encoding ---

    private static byte[] EncodeVarintDelta(ushort[] sorted)
    {
        using var ms = new MemoryStream(sorted.Length); // estimate ~1 byte per entry
        ushort prev = 0;
        foreach (var val in sorted)
        {
            uint delta = (uint)(val - prev);
            prev = val;
            // Write varint
            while (delta >= 0x80)
            {
                ms.WriteByte((byte)(delta | 0x80));
                delta >>= 7;
            }
            ms.WriteByte((byte)delta);
        }
        return ms.ToArray();
    }

    private static ushort[] DecodeVarintDelta(BinaryReader br, int count)
    {
        var result = new ushort[count];
        ushort prev = 0;
        for (int i = 0; i < count; i++)
        {
            uint delta = 0;
            int shift = 0;
            byte b;
            do
            {
                b = br.ReadByte();
                delta |= (uint)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            prev = (ushort)(prev + delta);
            result[i] = prev;
        }
        return result;
    }
}
