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
/// v4 format: per-posting (docId-gap varint, term-frequency varint) pairs, high-DF
/// cutoff, plus an integrity header — a build stamp (must match
/// <c>SearchIndexManifest.IndexStamp</c>) and a SHA-256 checksum of the sibling
/// .paths file. The stamp stops a stale inverted index from being trusted after its
/// rebuild failed; the checksum stops a torn save (new .paths + old .bin, both
/// individually well-formed) from loading with a wrong docId→path mapping. v1/v2/v3
/// files are refused; the BuildGuid bump that shipped with v4 forces a full rebuild,
/// so nothing needs to read them.
///
/// v4 (2026-07): tf. Each posting now carries the number of times the term (a bigram)
/// occurs in that document, so ranking and hit-counts can come from the index without
/// scanning document text. tf is written as a second varint after each docId gap and
/// is always ≥ 1 (a posting exists only when the bigram occurred). The docId ushort
/// cap and high-DF cutoff are unchanged.
/// </summary>
public sealed class InvertedSearchIndex
{
    private static readonly byte[] Magic = "IIDX"u8.ToArray();
    private const int Version = 4;
    private const double MaxDocFrequencyRatio = 0.8; // skip only truly ubiquitous bigrams (之所, 如是, etc.)

    // Per-term postings, split into two parallel, docId-ascending arrays: _index holds
    // the docIds (the read path for the docId-only Search stays byte-for-byte the old
    // logic) and _tf holds the aligned per-document term frequencies (the v4 addition).
    private Dictionary<string, ushort[]>? _index;
    private Dictionary<string, int[]>? _tf;
    private string[]? _docPaths;

    public int TermCount => _index?.Count ?? 0;
    public int DocCount => _docPaths?.Length ?? 0;
    public bool IsLoaded => _index != null;

    /// <summary>Only CJK ideographs are worth indexing.</summary>
    // Canonical 3-range CJK set; routed to CjkText (pinned by CjkTextTests over
    // the full BMP) so the GUID-versioned inverted index cannot silently drift.
    private static bool IsIndexable(char ch) => ReadZen.App.Infrastructure.CjkText.IsIdeograph(ch);

    /// <summary>
    /// Compute a document's indexable bigram set from its raw searchable text: every
    /// adjacent code-unit pair where BOTH chars pass <see cref="IsIndexable"/>, packed
    /// as <c>((uint)c0 &lt;&lt; 16) | c1</c>, unique, sorted ascending. This is the ONLY
    /// producer of inverted-index gram sets — callers (e.g. the gram-sets sidecar) must
    /// use it rather than re-implement the filter, so cached sets can never drift from
    /// what <see cref="Build(IReadOnlyList{ValueTuple{string, string}})"/> would compute.
    /// </summary>
    public static uint[] ComputeGramSet(string searchableText)
    {
        var set = new HashSet<uint>();
        for (int i = 0; i < searchableText.Length - 1; i++)
        {
            char c0 = searchableText[i], c1 = searchableText[i + 1];
            if (!IsIndexable(c0) || !IsIndexable(c1)) continue;
            set.Add(((uint)c0 << 16) | c1);
        }
        var grams = new uint[set.Count];
        set.CopyTo(grams);
        Array.Sort(grams);
        return grams;
    }

    /// <summary>
    /// Like <see cref="ComputeGramSet"/> but also returns, for each gram, how many times
    /// it occurs in <paramref name="searchableText"/> (the per-document term frequency).
    /// <c>grams</c> is byte-for-byte identical to <see cref="ComputeGramSet"/>'s output
    /// (same IsIndexable filter, unique, sorted ascending); <c>counts[i]</c> is the
    /// occurrence count of <c>grams[i]</c> and is always ≥ 1. This is the ONLY producer
    /// of tf so the counts can never drift from the indexed gram set.
    /// </summary>
    public static (uint[] grams, int[] counts) ComputeGramSetAndCounts(string searchableText)
    {
        var dict = new Dictionary<uint, int>();
        for (int i = 0; i < searchableText.Length - 1; i++)
        {
            char c0 = searchableText[i], c1 = searchableText[i + 1];
            if (!IsIndexable(c0) || !IsIndexable(c1)) continue;
            uint key = ((uint)c0 << 16) | c1;
            dict.TryGetValue(key, out var prev);
            dict[key] = prev + 1;
        }

        var grams = new uint[dict.Count];
        dict.Keys.CopyTo(grams, 0);
        Array.Sort(grams);
        var counts = new int[grams.Length];
        for (int i = 0; i < grams.Length; i++)
            counts[i] = dict[grams[i]];
        return (grams, counts);
    }

    /// <summary>
    /// Computes the tf counts aligned to an ALREADY-KNOWN gram set (e.g. one read back
    /// from the gramsets sidecar for an unchanged entry, where the set is cached but the
    /// counts are not). <paramref name="grams"/> must be the <see cref="ComputeGramSet"/>
    /// output for <paramref name="searchableText"/>; each returned count is ≥ 1. Used so
    /// tf stays available on the incremental warm-sidecar path without recomputing (or
    /// re-caching) the gram set itself.
    /// </summary>
    public static int[] ComputeGramCounts(string searchableText, uint[] grams)
    {
        if (grams.Length == 0) return Array.Empty<int>();

        var dict = new Dictionary<uint, int>(grams.Length);
        for (int i = 0; i < searchableText.Length - 1; i++)
        {
            char c0 = searchableText[i], c1 = searchableText[i + 1];
            if (!IsIndexable(c0) || !IsIndexable(c1)) continue;
            uint key = ((uint)c0 << 16) | c1;
            dict.TryGetValue(key, out var prev);
            dict[key] = prev + 1;
        }

        var counts = new int[grams.Length];
        for (int i = 0; i < grams.Length; i++)
            counts[i] = dict.TryGetValue(grams[i], out var c) && c > 0 ? c : 1;
        return counts;
    }

    public void Build(IReadOnlyList<(string relPath, string searchableText)> documents)
    {
        // Deduplicate by relPath BEFORE computing gram sets — keep first occurrence
        // only, and never spend work on dropped duplicates. The gram-set overload
        // below re-checks dedup, which is a no-op on this already-unique list.
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keptIndices = new List<int>(documents.Count);
        for (int i = 0; i < documents.Count; i++)
        {
            if (seenPaths.Add(documents[i].relPath))
                keptIndices.Add(i);
        }

        // Refuse an oversized corpus BEFORE the full-corpus bigram scan: the loud
        // ushort-cap refusal must stay instant (the gram-set overload re-checks it,
        // but by then every gram set would already have been computed).
        if (keptIndices.Count > ushort.MaxValue)
            throw new InvalidOperationException(
                $"InvertedSearchIndex supports at most {ushort.MaxValue} documents; got {keptIndices.Count}.");

        var dedupedDocs = new List<(string relPath, uint[] gramSet, int[] gramCounts)>(keptIndices.Count);
        foreach (int i in keptIndices)
        {
            var (relPath, text) = documents[i];
            var (grams, counts) = ComputeGramSetAndCounts(text);
            dedupedDocs.Add((relPath, grams, counts));
        }
        Build(dedupedDocs);
    }

    /// <summary>
    /// Convenience overload for callers that have only the gram SETS and no tf counts
    /// (e.g. some tests). Each posting is stored with tf = 1 (unknown → "at least one").
    /// Production must use the tf-carrying <see cref="Build(IReadOnlyList{ValueTuple{string, uint[], int[]}})"/>
    /// overload so ranking/counts reflect real per-document frequencies.
    /// </summary>
    public void Build(IReadOnlyList<(string relPath, uint[] gramSet)> documents)
    {
        var withCounts = new List<(string relPath, uint[] gramSet, int[] gramCounts)>(documents.Count);
        foreach (var (relPath, gramSet) in documents)
        {
            var counts = new int[gramSet.Length];
            Array.Fill(counts, 1);
            withCounts.Add((relPath, gramSet, counts));
        }
        Build(withCounts);
    }

    /// <summary>
    /// Build from precomputed per-document gram sets + aligned tf counts (as produced by
    /// <see cref="ComputeGramSetAndCounts"/>: packed bigrams, unique, sorted ascending,
    /// with <c>gramCounts[i]</c> the occurrence count of <c>gramSet[i]</c> — callers must
    /// honor that precondition). Semantics are identical to the text overload: keep-FIRST
    /// dedup by relPath (OrdinalIgnoreCase), loud refusal past the ushort docId cap, and
    /// the high-DF cutoff applied here at build time from the UNCUT input sets — so cached
    /// sets must never be pre-cut, and terms cut by a previous build resurrect when the
    /// corpus shrinks below the threshold.
    /// </summary>
    public void Build(IReadOnlyList<(string relPath, uint[] gramSet, int[] gramCounts)> documents)
    {
        // Deduplicate by relPath — keep first occurrence only
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dedupedDocs = new List<(string relPath, uint[] gramSet, int[] gramCounts)>();
        foreach (var (relPath, gramSet, gramCounts) in documents)
        {
            if (seenPaths.Add(relPath))
                dedupedDocs.Add((relPath, gramSet, gramCounts));
        }

        int docCount = dedupedDocs.Count;
        // Doc IDs are stored as ushort (postings + per-term counts). Past 65,535 the
        // cast below would silently wrap and attribute hits to the WRONG files, so
        // refuse loudly instead — the caller treats a build failure as "no inverted
        // index" and search stays correct via the bloom + verify path.
        if (docCount > ushort.MaxValue)
            throw new InvalidOperationException(
                $"InvertedSearchIndex supports at most {ushort.MaxValue} documents; got {docCount}.");
        int maxDf = (int)(docCount * MaxDocFrequencyRatio);

        _docPaths = new string[docCount];
        var tempIndex = new Dictionary<uint, List<ushort>>(16384);
        var tempTf = new Dictionary<uint, List<int>>(16384);

        for (int docId = 0; docId < docCount; docId++)
        {
            var (relPath, gramSet, gramCounts) = dedupedDocs[docId];
            _docPaths[docId] = relPath;

            for (int k = 0; k < gramSet.Length; k++)
            {
                uint key = gramSet[k];
                // tf ≥ 1: a gram present in the set occurred at least once. A missing/
                // short counts array (defensive) falls back to 1 so a posting is never
                // recorded with tf 0.
                int tf = gramCounts != null && k < gramCounts.Length && gramCounts[k] > 0
                    ? gramCounts[k]
                    : 1;
                if (!tempIndex.TryGetValue(key, out var list))
                {
                    list = new List<ushort>();
                    tempIndex[key] = list;
                    tempTf[key] = new List<int>();
                }
                list.Add((ushort)docId); // docIds appended in doc order → ascending
                tempTf[key].Add(tf);      // aligned with list
            }
        }

        // Convert to string-keyed dictionaries, skipping high-DF terms
        _index = new Dictionary<string, ushort[]>(tempIndex.Count);
        _tf = new Dictionary<string, int[]>(tempIndex.Count);
        int skipped = 0;
        foreach (var (key, list) in tempIndex)
        {
            if (list.Count > maxDf) { skipped++; continue; } // too common = useless for filtering
            char c0 = (char)(key >> 16), c1 = (char)(key & 0xFFFF);
            string term = string.Concat(c0, c1);
            _index[term] = list.ToArray();
            _tf[term] = tempTf[key].ToArray();
        }
        tempIndex.Clear(); // free memory immediately
        tempTf.Clear();

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

    /// <summary>
    /// Like <see cref="Search"/>, but also returns a per-document term-frequency estimate
    /// for the query. For a single-bigram query (a 2-char CJK term) the tf is EXACT — the
    /// number of times the bigram occurs in the document, which equals the match count.
    /// For a multi-bigram phrase the tf is the MIN over the query's constituent bigrams'
    /// per-document tf: an UPPER-bound estimate of the phrase count (the phrase cannot
    /// occur more often than its rarest bigram, but it may occur far fewer times — even
    /// zero, when the bigrams co-occur without ever forming the contiguous phrase).
    /// Return semantics mirror
    /// <see cref="Search"/>: null when the index is unloaded / query too short / no
    /// bigrams; an empty array when a query bigram is absent (or high-DF-cut) or the
    /// intersection is empty. Result docIds are ascending.
    /// </summary>
    public (ushort docId, long tf)[]? SearchWithTf(string query)
    {
        if (_index == null || _tf == null || _docPaths == null || query.Length < 2) return null;

        var bigrams = new List<string>();
        for (int i = 0; i < query.Length - 1; i++)
        {
            char c0 = query[i], c1 = query[i + 1];
            if (char.IsWhiteSpace(c0) || char.IsWhiteSpace(c1)) continue;
            bigrams.Add(string.Concat(c0, c1));
        }
        if (bigrams.Count == 0) return null;

        ushort[]? docs = null;
        long[]? tfs = null;
        foreach (var bg in bigrams.OrderBy(b => _index.TryGetValue(b, out var l) ? l.Length : 0))
        {
            if (!_index.TryGetValue(bg, out var postings))
                return Array.Empty<(ushort, long)>(); // not found (absent or DF-cut) = 0 results
            var bgTfs = _tf[bg];

            if (docs == null)
            {
                docs = postings;
                tfs = new long[bgTfs.Length];
                for (int i = 0; i < bgTfs.Length; i++) tfs[i] = bgTfs[i];
            }
            else
            {
                (docs, tfs) = IntersectTf(docs, tfs!, postings, bgTfs);
            }
            if (docs.Length == 0) return Array.Empty<(ushort, long)>();
        }

        if (docs == null || tfs == null) return Array.Empty<(ushort, long)>();
        var result = new (ushort docId, long tf)[docs.Length];
        for (int i = 0; i < docs.Length; i++) result[i] = (docs[i], tfs[i]);
        return result;
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

    /// <summary>
    /// docId-ascending intersection that carries tf as the running MIN across bigrams.
    /// <paramref name="aTfs"/> is aligned with <paramref name="aDocs"/> (already a running
    /// min from earlier bigrams); <paramref name="bTfs"/> is aligned with
    /// <paramref name="bDocs"/> (a single bigram's per-doc counts).
    /// </summary>
    private static (ushort[] docs, long[] tfs) IntersectTf(ushort[] aDocs, long[] aTfs, ushort[] bDocs, int[] bTfs)
    {
        int cap = Math.Min(aDocs.Length, bDocs.Length);
        var docs = new List<ushort>(cap);
        var tfs = new List<long>(cap);
        int i = 0, j = 0;
        while (i < aDocs.Length && j < bDocs.Length)
        {
            if (aDocs[i] == bDocs[j])
            {
                docs.Add(aDocs[i]);
                tfs.Add(Math.Min(aTfs[i], bTfs[j]));
                i++; j++;
            }
            else if (aDocs[i] < bDocs[j]) i++;
            else j++;
        }
        return (docs.ToArray(), tfs.ToArray());
    }

    /// <summary>
    /// Save with varint-delta encoding. Both files are written to temp paths and moved
    /// into place atomically (.paths first, then .bin); the .bin header embeds
    /// <paramref name="buildStamp"/> and a SHA-256 of the .paths bytes, so any torn
    /// combination of old/new files is rejected by <see cref="TryLoadAsync"/> instead
    /// of loading with a wrong docId→path mapping.
    /// </summary>
    public async Task SaveAsync(string path, string buildStamp, CancellationToken ct = default)
    {
        if (_index == null || _docPaths == null) return;
        if (string.IsNullOrEmpty(buildStamp))
            throw new ArgumentException("A non-empty build stamp is required.", nameof(buildStamp));

        var pathsFile = path + ".paths";
        var tmpPaths = pathsFile + ".tmp";
        var tmpBin = path + ".tmp";
        try
        {
            await File.WriteAllLinesAsync(tmpPaths, _docPaths, Encoding.UTF8, ct);
            var pathsChecksum = ComputeSha256(await File.ReadAllBytesAsync(tmpPaths, ct));

            using (var fs = new FileStream(tmpBin, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true))
            {
                WriteBody(bw, buildStamp, pathsChecksum);
                bw.Flush();
            }

            File.Move(tmpPaths, pathsFile, overwrite: true);
            File.Move(tmpBin, path, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(tmpPaths)) File.Delete(tmpPaths); } catch { }
            try { if (File.Exists(tmpBin)) File.Delete(tmpBin); } catch { }
            throw;
        }
    }

    private void WriteBody(BinaryWriter bw, string buildStamp, byte[] pathsChecksum)
    {
        var sorted = _index!.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();

        // Header
        bw.Write(Magic);
        bw.Write(Version);
        var stampBytes = Encoding.UTF8.GetBytes(buildStamp);
        bw.Write((ushort)stampBytes.Length);
        bw.Write(stampBytes);
        bw.Write(pathsChecksum); // 32 bytes
        bw.Write(sorted.Count);
        bw.Write(_docPaths!.Length);

        // Pre-compute encoded postings to know offsets. v4: each posting is a docId-gap
        // varint immediately followed by a tf varint (both for the same doc).
        var encodedPostings = new List<byte[]>(sorted.Count);
        foreach (var (term, postings) in sorted)
            encodedPostings.Add(EncodePostings(postings, _tf![term]));

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

        // Postings section (varint-delta docIds interleaved with tf varints)
        foreach (var encoded in encodedPostings)
            bw.Write(encoded);
    }

    /// <summary>
    /// Load a v4 index. Refuses (returns false, stays unloaded) when the file's
    /// embedded build stamp differs from <paramref name="expectedBuildStamp"/> (stale
    /// index from a failed rebuild) or when the .paths file does not match the
    /// checksum recorded in the .bin header (torn save). Older v1/v2/v3 files are
    /// refused too — the BuildGuid bump that introduced v4 forces a full rebuild.
    /// </summary>
    public async Task<bool> TryLoadAsync(string path, string expectedBuildStamp, CancellationToken ct = default)
    {
        var pathsFile = path + ".paths";
        if (!File.Exists(path) || !File.Exists(pathsFile)) return false;
        if (string.IsNullOrEmpty(expectedBuildStamp)) return false;

        try
        {
            var pathsBytes = await File.ReadAllBytesAsync(pathsFile, ct);
            var data = await File.ReadAllBytesAsync(path, ct);
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.UTF8);

            var magic = br.ReadBytes(4);
            if (!magic.SequenceEqual(Magic)) return false;

            int version = br.ReadInt32();
            if (version != Version) return false;

            int stampLen = br.ReadUInt16();
            string stamp = Encoding.UTF8.GetString(br.ReadBytes(stampLen));
            if (!string.Equals(stamp, expectedBuildStamp, StringComparison.Ordinal)) return false;

            var storedChecksum = br.ReadBytes(32);
            if (!storedChecksum.SequenceEqual(ComputeSha256(pathsBytes))) return false;

            _docPaths = ReadPathLines(pathsBytes);

            int termCount = br.ReadInt32();
            int fileCount = br.ReadInt32();
            if (fileCount != _docPaths.Length) { _docPaths = null; return false; }

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
            _tf = new Dictionary<string, int[]>(termCount);
            foreach (var (term, offset, count) in entries)
            {
                ms.Seek(postingsStart + offset, SeekOrigin.Begin);
                var (docs, tfs) = DecodePostings(br, count);
                _index[term] = docs;
                _tf[term] = tfs;
            }

            return true;
        }
        catch
        {
            _index = null;
            _tf = null;
            _docPaths = null;
            return false;
        }
    }

    private static byte[] ComputeSha256(byte[] bytes)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return sha.ComputeHash(bytes);
    }

    /// <summary>
    /// Decode the .paths bytes with the same semantics File.ReadAllLinesAsync had
    /// (UTF-8, optional BOM stripped, \r\n or \n separators, no trailing empty line).
    /// Decoding from the already-read bytes keeps the checksum and the parsed content
    /// guaranteed to come from the same file snapshot.
    /// </summary>
    private static string[] ReadPathLines(byte[] pathsBytes)
    {
        var text = Encoding.UTF8.GetString(pathsBytes);
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];
        var lines = text.Split('\n');
        int count = lines.Length;
        if (count > 0 && lines[count - 1].Length == 0) count--; // trailing newline
        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = lines[i].TrimEnd('\r');
        return result;
    }

    // --- Varint delta encoding (v4: docId-gap varint + tf varint per posting) ---

    private static byte[] EncodePostings(ushort[] docs, int[] tfs)
    {
        // Estimate ~2 bytes per entry (docId gap + small tf).
        using var ms = new MemoryStream(docs.Length * 2);
        ushort prev = 0;
        for (int i = 0; i < docs.Length; i++)
        {
            uint delta = (uint)(docs[i] - prev);
            prev = docs[i];
            WriteVarint(ms, delta);
            // tf is stored raw (not delta): a posting always exists with tf ≥ 1, so the
            // fallback keeps the format self-consistent even for a defensive 0.
            uint tf = i < tfs.Length && tfs[i] > 0 ? (uint)tfs[i] : 1u;
            WriteVarint(ms, tf);
        }
        return ms.ToArray();
    }

    private static void WriteVarint(MemoryStream ms, uint value)
    {
        while (value >= 0x80)
        {
            ms.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        ms.WriteByte((byte)value);
    }

    private static (ushort[] docs, int[] tfs) DecodePostings(BinaryReader br, int count)
    {
        var docs = new ushort[count];
        var tfs = new int[count];
        ushort prev = 0;
        for (int i = 0; i < count; i++)
        {
            uint delta = ReadVarint(br);
            prev = (ushort)(prev + delta);
            docs[i] = prev;
            tfs[i] = (int)ReadVarint(br);
        }
        return (docs, tfs);
    }

    private static uint ReadVarint(BinaryReader br)
    {
        uint value = 0;
        int shift = 0;
        byte b;
        do
        {
            b = br.ReadByte();
            value |= (uint)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return value;
    }
}
