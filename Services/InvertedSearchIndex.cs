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
/// v3 format: varint-delta encoded postings, high-DF cutoff, plus an integrity
/// header — a build stamp (must match <c>SearchIndexManifest.IndexStamp</c>) and a
/// SHA-256 checksum of the sibling .paths file. The stamp stops a stale inverted
/// index from being trusted after its rebuild failed; the checksum stops a torn
/// save (new .paths + old .bin, both individually well-formed) from loading with a
/// wrong docId→path mapping. v1/v2 files are refused; the BuildGuid bump that
/// shipped with v3 forces a full rebuild, so nothing needs to read them.
/// </summary>
public sealed class InvertedSearchIndex
{
    private static readonly byte[] Magic = "IIDX"u8.ToArray();
    private const int Version = 3;
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

        var dedupedDocs = new List<(string relPath, uint[] gramSet)>(keptIndices.Count);
        foreach (int i in keptIndices)
        {
            var (relPath, text) = documents[i];
            dedupedDocs.Add((relPath, ComputeGramSet(text)));
        }
        Build(dedupedDocs);
    }

    /// <summary>
    /// Build from precomputed per-document gram sets (as produced by
    /// <see cref="ComputeGramSet"/>: packed bigrams, unique, sorted ascending —
    /// callers must honor that precondition). Semantics are identical to the text
    /// overload: keep-FIRST dedup by relPath (OrdinalIgnoreCase), loud refusal past
    /// the ushort docId cap, and the high-DF cutoff applied here at build time from
    /// the UNCUT input sets — so cached sets must never be pre-cut, and terms cut by
    /// a previous build resurrect when the corpus shrinks below the threshold.
    /// </summary>
    public void Build(IReadOnlyList<(string relPath, uint[] gramSet)> documents)
    {
        // Deduplicate by relPath — keep first occurrence only
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dedupedDocs = new List<(string relPath, uint[] gramSet)>();
        foreach (var (relPath, gramSet) in documents)
        {
            if (seenPaths.Add(relPath))
                dedupedDocs.Add((relPath, gramSet));
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

        for (int docId = 0; docId < docCount; docId++)
        {
            var (relPath, gramSet) = dedupedDocs[docId];
            _docPaths[docId] = relPath;

            foreach (var key in gramSet)
            {
                if (!tempIndex.TryGetValue(key, out var list))
                {
                    list = new List<ushort>();
                    tempIndex[key] = list;
                }
                list.Add((ushort)docId); // docIds appended in doc order → ascending
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
    }

    /// <summary>
    /// Load a v3 index. Refuses (returns false, stays unloaded) when the file's
    /// embedded build stamp differs from <paramref name="expectedBuildStamp"/> (stale
    /// index from a failed rebuild) or when the .paths file does not match the
    /// checksum recorded in the .bin header (torn save). Older v1/v2 files are
    /// refused too — the BuildGuid bump that introduced v3 forces a full rebuild.
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
