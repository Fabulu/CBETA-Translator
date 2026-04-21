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
/// Binary format (search.inverted.bin):
///   [4 bytes: magic "IIDX"]
///   [4 bytes: version = 1]
///   [4 bytes: term_count]
///   [4 bytes: file_count]
///   [4 bytes: postings_section_offset]
///   -- Dictionary section (sorted by term) --
///   For each term:
///     [2 bytes: term_byte_length]
///     [N bytes: UTF-8 term]
///     [4 bytes: posting_offset (relative to postings section)]
///     [2 bytes: posting_count]
///   -- Postings section --
///   For each term's postings:
///     [posting_count × 2 bytes: sorted ushort doc IDs]
/// </summary>
public sealed class InvertedSearchIndex
{
    private static readonly byte[] Magic = "IIDX"u8.ToArray();
    private const int Version = 1;

    private Dictionary<string, ushort[]>? _index;
    private string[]? _docPaths; // docId → relPath

    public int TermCount => _index?.Count ?? 0;
    public int DocCount => _docPaths?.Length ?? 0;
    public bool IsLoaded => _index != null;

    /// <summary>
    /// Build the inverted index from extracted text content.
    /// Call once per corpus scan with all (docId, relPath, searchableText) tuples.
    /// </summary>
    /// <summary>Only CJK ideographs are worth indexing. English footnotes/annotations create massive bloat.</summary>
    private static bool IsIndexable(char ch)
        => (ch >= '\u4E00' && ch <= '\u9FFF')   // CJK Unified Ideographs
        || (ch >= '\u3400' && ch <= '\u4DBF')   // CJK Extension A
        || (ch >= '\uF900' && ch <= '\uFAFF');  // CJK Compatibility

    public void Build(IReadOnlyList<(string relPath, string searchableText)> documents)
    {
        // Deduplicate by relPath — merge text from both sides (Original + Translated)
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relPath, text) in documents)
        {
            if (merged.TryGetValue(relPath, out var existing))
                merged[relPath] = existing + " " + text;
            else
                merged[relPath] = text;
        }

        var dedupedDocs = merged.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).ToList();
        _docPaths = new string[dedupedDocs.Count];
        var tempIndex = new Dictionary<string, List<ushort>>();

        for (int docId = 0; docId < dedupedDocs.Count; docId++)
        {
            var (relPath, text) = (dedupedDocs[docId].Key, dedupedDocs[docId].Value);
            _docPaths[docId] = relPath;

            // Extract unique bigrams from this document
            var seen = new HashSet<string>();
            for (int i = 0; i < text.Length - 1; i++)
            {
                char c0 = text[i], c1 = text[i + 1];
                // Skip non-meaningful characters
                if (!IsIndexable(c0) || !IsIndexable(c1)) continue;

                string bigram = string.Concat(c0, c1);
                if (!seen.Add(bigram)) continue; // dedupe within doc

                if (!tempIndex.TryGetValue(bigram, out var list))
                {
                    list = new List<ushort>();
                    tempIndex[bigram] = list;
                }
                list.Add((ushort)docId);
            }
        }

        // Convert to sorted arrays
        _index = new Dictionary<string, ushort[]>(tempIndex.Count);
        foreach (var (term, list) in tempIndex)
        {
            var arr = list.ToArray();
            Array.Sort(arr);
            _index[term] = arr;
        }
    }

    /// <summary>
    /// Query the index: returns sorted doc IDs that contain ALL bigrams of the query.
    /// Returns null if index not loaded.
    /// </summary>
    public ushort[]? Search(string query)
    {
        if (_index == null || _docPaths == null || query.Length < 2) return null;

        // Extract bigrams from query
        var bigrams = new List<string>();
        for (int i = 0; i < query.Length - 1; i++)
        {
            char c0 = query[i], c1 = query[i + 1];
            if (char.IsWhiteSpace(c0) || char.IsWhiteSpace(c1)) continue;
            string bg = string.Concat(c0, c1);
            bigrams.Add(bg);
        }

        if (bigrams.Count == 0) return null;

        // Start with shortest posting list for efficiency
        ushort[]? result = null;
        foreach (var bg in bigrams.OrderBy(b => _index.TryGetValue(b, out var l) ? l.Length : 0))
        {
            if (!_index.TryGetValue(bg, out var postings))
                return Array.Empty<ushort>(); // term not in corpus = 0 results

            result = result == null ? postings : Intersect(result, postings);
            if (result.Length == 0) return result;
        }

        return result ?? Array.Empty<ushort>();
    }

    /// <summary>Map doc IDs back to relative paths.</summary>
    public string? GetRelPath(ushort docId)
        => _docPaths != null && docId < _docPaths.Length ? _docPaths[docId] : null;

    /// <summary>Intersect two sorted ushort arrays.</summary>
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

    /// <summary>Save the index to a binary file.</summary>
    public async Task SaveAsync(string path, CancellationToken ct = default)
    {
        if (_index == null || _docPaths == null) return;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        // Header
        bw.Write(Magic);
        bw.Write(Version);
        bw.Write(_index.Count);
        bw.Write(_docPaths.Length);
        bw.Write(0); // placeholder for postings offset

        // Sort terms for deterministic output
        var sorted = _index.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();

        // Dictionary section — compute posting offsets
        int postingOffset = 0;
        var dictEntries = new List<(byte[] termBytes, int offset, ushort count)>();
        foreach (var (term, postings) in sorted)
        {
            var termBytes = Encoding.UTF8.GetBytes(term);
            dictEntries.Add((termBytes, postingOffset, (ushort)postings.Length));
            postingOffset += postings.Length * 2; // ushort = 2 bytes
        }

        foreach (var (termBytes, offset, count) in dictEntries)
        {
            bw.Write((ushort)termBytes.Length);
            bw.Write(termBytes);
            bw.Write(offset);
            bw.Write(count);
        }

        // Record postings section start
        long postingsSectionOffset = ms.Position;
        ms.Seek(16, SeekOrigin.Begin); // back to placeholder
        bw.Write((int)postingsSectionOffset);
        ms.Seek(postingsSectionOffset, SeekOrigin.Begin);

        // Postings section
        foreach (var (_, postings) in sorted)
        {
            foreach (var docId in postings)
                bw.Write(docId);
        }

        bw.Flush();

        // Doc paths section (appended as simple newline-delimited UTF8)
        // Store separately for easy loading
        var docPathsPath = path + ".paths";
        await File.WriteAllLinesAsync(docPathsPath, _docPaths, Encoding.UTF8, ct);

        await File.WriteAllBytesAsync(path, ms.ToArray(), ct);
    }

    /// <summary>Load the index from a binary file.</summary>
    public async Task<bool> TryLoadAsync(string path, CancellationToken ct = default)
    {
        var docPathsPath = path + ".paths";
        if (!File.Exists(path) || !File.Exists(docPathsPath)) return false;

        try
        {
            _docPaths = await File.ReadAllLinesAsync(docPathsPath, Encoding.UTF8, ct);

            var data = await File.ReadAllBytesAsync(path, ct);
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.UTF8);

            // Verify magic
            var magic = br.ReadBytes(4);
            if (!magic.SequenceEqual(Magic)) return false;

            var version = br.ReadInt32();
            if (version != Version) return false;

            int termCount = br.ReadInt32();
            int fileCount = br.ReadInt32();
            int postingsOffset = br.ReadInt32();

            // Read dictionary
            var dict = new Dictionary<string, (int offset, ushort count)>(termCount);
            for (int i = 0; i < termCount; i++)
            {
                int termLen = br.ReadUInt16();
                var termBytes = br.ReadBytes(termLen);
                string term = Encoding.UTF8.GetString(termBytes);
                int offset = br.ReadInt32();
                ushort count = br.ReadUInt16();
                dict[term] = (offset, count);
            }

            // Read postings
            _index = new Dictionary<string, ushort[]>(termCount);
            foreach (var (term, (offset, count)) in dict)
            {
                ms.Seek(postingsOffset + offset, SeekOrigin.Begin);
                var postings = new ushort[count];
                for (int i = 0; i < count; i++)
                    postings[i] = br.ReadUInt16();
                _index[term] = postings;
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
}
