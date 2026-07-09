using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Reader/writer for the gramsets sidecar (search.gramsets.manifest.json +
/// search.gramsets.bin), the 6th index artifact. The sidecar caches, per
/// (RelPath, Side) entry, two sorted unique packed-uint bigram arrays:
/// the inverted-index alphabet (produced by <c>InvertedSearchIndex.ComputeGramSet</c> —
/// NOT reimplemented here) and the cjk2 compact alphabet
/// (<see cref="GramSetCodec.ComputeCjk2GramSet"/>).
///
/// Bin format: 4-byte magic "GSB1", then a 16-byte pairing token (fresh per save,
/// mirrored as <c>GramSetsManifest.BinPairingToken</c>), then back-to-back
/// little-endian uint32 arrays; each entry's manifest row records absolute byte
/// offsets + element counts. The token binds the pair: a crash between the bin move
/// and the manifest move leaves old-manifest + new-bin whose tokens differ, so the
/// torn pair is refused instead of serving other entries' shifted gram data.
///
/// Contract: <see cref="TryLoadAsync"/> returns null on ANY anomaly (never throws to
/// its caller); <see cref="SaveAsync"/> writes bin first then manifest, each via
/// tmp + move, deletes its tmps and rethrows on failure — the CALLER decides to
/// swallow (losing the sidecar only costs speed, never correctness).
/// </summary>
public static class GramSetsStore
{
    public const string ManifestFileName = "search.gramsets.manifest.json";
    public const string BinFileName = "search.gramsets.bin";
    public const string BuildGuid = "search-v1-gramsets";

    // Bin magic "GSB1".
    private static readonly byte[] Magic = { (byte)'G', (byte)'S', (byte)'B', (byte)'1' };

    // Byte length of the per-save pairing token written right after the magic.
    private const int PairingTokenLength = 16;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public static string GetManifestPath(string root) => Path.Combine(root, ManifestFileName);
    public static string GetBinPath(string root) => Path.Combine(root, BinFileName);

    /// <summary>
    /// Writes the sidecar family: bin first, then manifest, each via tmp +
    /// File.Move(overwrite: true). Fills each meta's InvOffset/InvCount/
    /// Cjk2Offset/Cjk2Count in place as the bin is written. Gram arrays are
    /// expected sorted ascending and unique (the codecs guarantee this).
    /// On any exception both tmps are deleted and the exception rethrown.
    /// </summary>
    public static async Task SaveAsync(
        string root,
        string? indexStamp,
        IReadOnlyList<(GramSetsEntry meta, uint[] invGrams, uint[] cjk2Grams)> entries,
        CancellationToken ct)
    {
        string binFinal = GetBinPath(root);
        string manifestFinal = GetManifestPath(root);
        string binTmp = binFinal + ".tmp";
        string manifestTmp = manifestFinal + ".tmp";

        // Fresh per save; written into BOTH files so a torn pair (old manifest + new
        // bin, or vice versa) is detectable at load time regardless of bounds luck.
        byte[] pairingToken = Guid.NewGuid().ToByteArray();

        try
        {
            // 1) Bin: magic, pairing token, then back-to-back LE uint32 arrays;
            //    record offsets as we go.
            using (var fs = new FileStream(binTmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true))
            {
                await fs.WriteAsync(Magic, ct).ConfigureAwait(false);
                await fs.WriteAsync(pairingToken, ct).ConfigureAwait(false);
                long pos = Magic.Length + PairingTokenLength;

                foreach (var (meta, invGrams, cjk2Grams) in entries)
                {
                    ct.ThrowIfCancellationRequested();

                    meta.InvOffset = pos;
                    meta.InvCount = invGrams.Length;
                    await WriteUIntArrayLeAsync(fs, invGrams, ct).ConfigureAwait(false);
                    pos += 4L * invGrams.Length;

                    meta.Cjk2Offset = pos;
                    meta.Cjk2Count = cjk2Grams.Length;
                    await WriteUIntArrayLeAsync(fs, cjk2Grams, ct).ConfigureAwait(false);
                    pos += 4L * cjk2Grams.Length;
                }

                await fs.FlushAsync(ct).ConfigureAwait(false);
            }

            File.Move(binTmp, binFinal, overwrite: true);

            // 2) Manifest (after the bin; a crash between the two moves leaves the
            //    OLD manifest next to the NEW bin — their pairing tokens differ, so
            //    TryLoadAsync refuses the torn pair => sidecar absent).
            var manifest = new GramSetsManifest
            {
                Version = 1,
                BuildGuid = BuildGuid,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                IndexStamp = indexStamp,
                BinPairingToken = Convert.ToHexString(pairingToken),
            };
            foreach (var (meta, _, _) in entries)
                manifest.Entries.Add(meta);

            string json = JsonSerializer.Serialize(manifest, JsonOpts);
            await File.WriteAllTextAsync(manifestTmp, json, Utf8NoBom, ct).ConfigureAwait(false);
            File.Move(manifestTmp, manifestFinal, overwrite: true);
        }
        catch
        {
            TryDelete(binTmp);
            TryDelete(manifestTmp);
            throw;
        }
    }

    /// <summary>
    /// Loads and validates the sidecar. Returns null on ANY anomaly: missing files,
    /// malformed JSON, Version/BuildGuid mismatch, RootPath not full-path-equal to
    /// <paramref name="root"/> (OrdinalIgnoreCase), bad bin magic, bin/manifest
    /// pairing-token mismatch (torn pair from a crash between the two moves),
    /// per-entry bounds violations (negative offset/count, offset + 4*count past end,
    /// overflow), or duplicate (RelPath, Side) keys. Never throws to the caller
    /// (OperationCanceledException excepted).
    /// </summary>
    public static async Task<LoadedGramSets?> TryLoadAsync(string root, CancellationToken ct)
    {
        try
        {
            string manifestPath = GetManifestPath(root);
            string binPath = GetBinPath(root);
            if (!File.Exists(manifestPath) || !File.Exists(binPath))
                return null;

            string json = await File.ReadAllTextAsync(manifestPath, Utf8NoBom, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var manifest = JsonSerializer.Deserialize<GramSetsManifest>(json, JsonOpts);
            if (manifest == null || manifest.Entries == null)
                return null;

            if (manifest.Version != 1)
                return null;
            if (!string.Equals(manifest.BuildGuid, BuildGuid, StringComparison.Ordinal))
                return null;
            if (string.IsNullOrEmpty(manifest.RootPath))
                return null;
            if (!string.Equals(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(manifest.RootPath)),
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)),
                    StringComparison.OrdinalIgnoreCase))
                return null;

            byte[] bin = await File.ReadAllBytesAsync(binPath, ct).ConfigureAwait(false);
            if (bin.Length < Magic.Length + PairingTokenLength)
                return null;
            for (int i = 0; i < Magic.Length; i++)
                if (bin[i] != Magic[i])
                    return null;

            // Pairing token: the manifest must name exactly the bin it was saved with.
            // This is the ONLY check that catches a torn pair whose bounds happen to
            // pass (e.g. old manifest + larger new bin) — bounds and per-entry content
            // identity validate the XML files, not the bin bytes.
            if (string.IsNullOrEmpty(manifest.BinPairingToken))
                return null;
            string binToken = Convert.ToHexString(bin.AsSpan(Magic.Length, PairingTokenLength));
            if (!string.Equals(manifest.BinPairingToken, binToken, StringComparison.OrdinalIgnoreCase))
                return null;

            long binLen = bin.LongLength;
            var byKey = new Dictionary<(string RelPath, SearchSide Side), GramSetsEntry>(
                manifest.Entries.Count, EntryKeyComparer.Instance);

            foreach (var e in manifest.Entries)
            {
                if (e == null || e.RelPath == null)
                    return null;
                if (!AreBoundsValid(e.InvOffset, e.InvCount, binLen))
                    return null;
                if (!AreBoundsValid(e.Cjk2Offset, e.Cjk2Count, binLen))
                    return null;
                // Duplicate (RelPath, Side) => anomaly => whole sidecar absent.
                if (!byKey.TryAdd((e.RelPath, e.Side), e))
                    return null;
            }

            return new LoadedGramSets(manifest, bin, byKey);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    // offset >= 0, count >= 0, offset + 4*count <= binLen — written to be overflow-proof
    // (4L * count cannot overflow a long; the subtraction form avoids offset + size overflow).
    private static bool AreBoundsValid(long offset, int count, long binLen)
        => offset >= 0 && count >= 0 && offset <= binLen && 4L * count <= binLen - offset;

    private static async Task WriteUIntArrayLeAsync(FileStream fs, uint[] grams, CancellationToken ct)
    {
        if (grams.Length == 0)
            return;

        var buf = new byte[4 * grams.Length];
        for (int i = 0; i < grams.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(i * 4, 4), grams[i]);
        await fs.WriteAsync(buf, ct).ConfigureAwait(false);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }

    internal sealed class EntryKeyComparer : IEqualityComparer<(string RelPath, SearchSide Side)>
    {
        public static readonly EntryKeyComparer Instance = new();

        public bool Equals((string RelPath, SearchSide Side) x, (string RelPath, SearchSide Side) y)
            => x.Side == y.Side && string.Equals(x.RelPath, y.RelPath, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string RelPath, SearchSide Side) obj)
            => HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.RelPath ?? ""),
                obj.Side);
    }
}

/// <summary>
/// A validated, fully-loaded gramsets sidecar: the manifest plus the whole bin in
/// memory. Lookup is keyed (RelPath OrdinalIgnoreCase, Side); gram arrays are
/// materialized on demand from the bin bytes.
/// </summary>
public sealed class LoadedGramSets
{
    private readonly byte[] _bin;
    private readonly Dictionary<(string RelPath, SearchSide Side), GramSetsEntry> _byKey;

    internal LoadedGramSets(
        GramSetsManifest manifest,
        byte[] bin,
        Dictionary<(string RelPath, SearchSide Side), GramSetsEntry> byKey)
    {
        Manifest = manifest;
        _bin = bin;
        _byKey = byKey;
    }

    public GramSetsManifest Manifest { get; }

    public bool TryGet(string relPath, SearchSide side, out GramSetsEntry entry)
    {
        if (_byKey.TryGetValue((relPath, side), out var found))
        {
            entry = found;
            return true;
        }
        entry = null!;
        return false;
    }

    public uint[] ReadInvGrams(GramSetsEntry e) => ReadArray(e.InvOffset, e.InvCount);

    public uint[] ReadCjk2Grams(GramSetsEntry e) => ReadArray(e.Cjk2Offset, e.Cjk2Count);

    private uint[] ReadArray(long offset, int count)
    {
        if (count == 0)
            return Array.Empty<uint>();

        // Bounds were validated at load; the checked cast is a belt-and-braces guard
        // (the bin fits in a byte[], so any valid offset fits in an int).
        int start = checked((int)offset);
        var result = new uint[count];
        for (int i = 0; i < count; i++)
            result[i] = BinaryPrimitives.ReadUInt32LittleEndian(_bin.AsSpan(start + i * 4, 4));
        return result;
    }
}

/// <summary>
/// Packing/unpacking for cached bigram sets, plus the cjk2-alphabet gram-set
/// computation. The inverted-index alphabet is intentionally NOT implemented here —
/// it is owned by <c>InvertedSearchIndex.ComputeGramSet</c> (the IsIndexable filter
/// must never be duplicated).
/// </summary>
public static class GramSetCodec
{
    /// <summary>Packs two UTF-16 code units into one uint: high 16 bits = first char.</summary>
    public static uint PackGram(char c0, char c1) => ((uint)c0 << 16) | c1;

    /// <summary>Unpacks a packed gram back to its 2-char string form.</summary>
    public static string UnpackGram(uint g)
        => string.Concat((char)(g >> 16), (char)(g & 0xFFFF));

    /// <summary>
    /// Computes the cjk2-alphabet gram set of <paramref name="searchableText"/>:
    /// compact = <see cref="CjkMatchNormalizer.Normalize"/>(text), then EVERY adjacent
    /// code-unit pair of compact — NO indexability filter (Latin bigrams and
    /// surrogate-half pairs included) — unique, packed, sorted ascending.
    /// </summary>
    public static uint[] ComputeCjk2GramSet(string searchableText)
    {
        string compact = CjkMatchNormalizer.Normalize(searchableText);
        if (compact.Length < 2)
            return Array.Empty<uint>();

        var set = new HashSet<uint>();
        for (int i = 0; i + 1 < compact.Length; i++)
            set.Add(PackGram(compact[i], compact[i + 1]));

        var result = new uint[set.Count];
        set.CopyTo(result);
        Array.Sort(result);
        return result;
    }
}
