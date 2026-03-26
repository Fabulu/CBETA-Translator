using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CbetaTranslator.App.Services;

public sealed class BloomSearchIndexService : IBloomSearchIndexService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public record Entry(string RelPath, long LastWriteUtcTicks, long LengthBytes, int OffsetBytes);

    public record Manifest(int Version, int BloomBytesPerDoc, int HashCount, int NGramSize, List<Entry> Entries);

    private const int Version = 1;

    public string ManifestPath(string root) => Path.Combine(root, "bloom.manifest.json");
    public string IndexPath(string root) => Path.Combine(root, "bloom.index.bin");

    public void Build(string originalDir, string root, IProgress<(int done, int total)>? progress = null)
    {
        Directory.CreateDirectory(root);

        int bloomBytes = Math.Max(256, SearchIndexSettings.BloomBytesPerDocument);
        int hashCount = Math.Clamp(SearchIndexSettings.HashCount, 2, 12);
        int ngram = Math.Clamp(SearchIndexSettings.NGramSize, 1, 6);

        var files = Directory.GetFiles(originalDir, "*.xml", SearchOption.AllDirectories);

        var manifest = new Manifest(
            Version,
            bloomBytes,
            hashCount,
            ngram,
            new List<Entry>(files.Length));

        using var indexStream = File.Create(IndexPath(root));

        int total = files.Length;
        int done = 0;

        foreach (var abs in files)
        {
            var fi = new FileInfo(abs);
            var rel = NormalizeRel(Path.GetRelativePath(originalDir, abs));

            var bloom = new byte[bloomBytes];

            string text = File.ReadAllText(abs);
            foreach (var gram in ExtractNGrams(text, ngram))
                AddGram(bloom, gram, hashCount);

            int offset = checked((int)indexStream.Position);
            indexStream.Write(bloom, 0, bloom.Length);

            manifest.Entries.Add(new Entry(rel, fi.LastWriteTimeUtc.Ticks, fi.Length, offset));

            done++;
            progress?.Report((done, total));
        }

        var json = System.Text.Json.JsonSerializer.Serialize(manifest);
        File.WriteAllText(ManifestPath(root), json, Utf8NoBom);
    }

    public List<string> Search(string root, string query)
    {
        var manifest = LoadManifest(root);

        var grams = ExtractNGrams(query ?? "", manifest.NGramSize).ToList();
        if (grams.Count == 0) return new List<string>();

        using var indexStream = File.OpenRead(IndexPath(root));
        var bloom = new byte[manifest.BloomBytesPerDoc];

        var hits = new List<string>();

        foreach (var e in manifest.Entries)
        {
            indexStream.Position = e.OffsetBytes;
            int read = indexStream.Read(bloom, 0, bloom.Length);
            if (read != bloom.Length) continue;

            bool ok = true;
            foreach (var g in grams)
            {
                if (!ContainsGram(bloom, g, manifest.HashCount))
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
                hits.Add(e.RelPath);
        }

        return hits;
    }

    private Manifest LoadManifest(string root)
    {
        var json = File.ReadAllText(ManifestPath(root));
        var m = System.Text.Json.JsonSerializer.Deserialize<Manifest>(json);
        if (m == null) throw new Exception("Bloom manifest invalid.");
        return m;
    }

    private static IEnumerable<string> ExtractNGrams(string text, int n)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        if (n <= 0) yield break;
        if (text.Length < n) yield break;

        // No case sensitivity: for Latin, you’ll handle query normalization in the caller if needed.
        // For Chinese, casing doesn’t matter anyway.

        for (int i = 0; i <= text.Length - n; i++)
            yield return text.Substring(i, n);
    }

    private static void AddGram(byte[] bloom, string gram, int hashCount)
    {
        int bits = bloom.Length * 8;

        for (int i = 0; i < hashCount; i++)
        {
            int h = Hash(gram, 17 + i * 31);
            int bit = (h & int.MaxValue) % bits;
            bloom[bit / 8] |= (byte)(1 << (bit % 8));
        }
    }

    private static bool ContainsGram(byte[] bloom, string gram, int hashCount)
    {
        int bits = bloom.Length * 8;

        for (int i = 0; i < hashCount; i++)
        {
            int h = Hash(gram, 17 + i * 31);
            int bit = (h & int.MaxValue) % bits;
            if ((bloom[bit / 8] & (1 << (bit % 8))) == 0)
                return false;
        }

        return true;
    }

    private static int Hash(string s, int seed)
    {
        unchecked
        {
            int h = seed;
            for (int i = 0; i < s.Length; i++)
                h = (h * 31) ^ s[i];
            return h;
        }
    }

    private static string NormalizeRel(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');
}
