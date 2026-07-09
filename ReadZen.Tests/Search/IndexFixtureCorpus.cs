using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReadZen.Tests.Search;

/// <summary>
/// Synthetic TEI corpus in a temp directory for index-family equivalence testing
/// (INC-1C harness; see runs/CLAUDE-RUNS/RUN-20260708-2206-desktop-incremental-reindex).
///
/// Shape (locked in by <c>FullRebuildDeterminismTests.FixtureShape_SelfTest</c>):
///   - <c>xml-p5</c> (originals) + <c>xml-p5t</c> (translations) subtrees with
///     <c>T/T01</c> and <c>T/T48</c> subdirectories;
///   - 20 rels / 36 files total: 16 both-sides rels, 2 orig-only rels, 2 tran-only rels;
///   - file names leave deliberate sort gaps (b0010, d0020, f0030, h0040, ...) so
///     <see cref="AddFileMidCorpus"/> can insert names (c0015, g0035, ...) that sort
///     strictly mid-corpus and shift every later positional Id (manifest Id,
///     cjk2 EntryId, inverted docId);
///   - the common gram 無門 (<see cref="CommonGram"/>) appears in EVERY winner doc
///     (the side the inverted index keeps after keep-first dedup), i.e. &gt;80% doc
///     frequency, which arms the 0.8 MaxDocFrequencyRatio cutoff;
///   - every file additionally carries a unique rare bigram
///     (<see cref="UniqueOrigGram"/> / <see cref="UniqueTranGram"/>) for semantic
///     backstop assertions;
///   - translated sides carry CJK text too (so tran entries contribute cjk2 grams).
///
/// Never touches the real corpora — everything lives under a fresh
/// <c>%TEMP%/readzen-eqharness-XXXXXXXX</c> directory removed on Dispose.
/// </summary>
public sealed class IndexFixtureCorpus : IDisposable
{
    /// <summary>無門 — present in every winner doc, so it exceeds the 0.8 DF cutoff.</summary>
    public const string CommonGram = "無門";

    public string Root { get; }
    /// <summary>Originals subtree (xml-p5).</summary>
    public string OrigDir { get; }
    /// <summary>Translations subtree (xml-p5t).</summary>
    public string TranDir { get; }

    private readonly List<string> _bothSidesRels = new();
    private readonly List<string> _origOnlyRels = new();
    private readonly List<string> _tranOnlyRels = new();

    // Unique-gram bookkeeping, keyed by rel (forward-slash form, case-insensitive).
    private readonly Dictionary<string, string> _uniqueOrigGrams = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _uniqueTranGrams = new(StringComparer.OrdinalIgnoreCase);
    private int _nextUniqueIndex;
    private int _changeCount;

    // Gap rels available for mid-corpus adds. Each sorts strictly between two
    // pre-existing names (c0015 between b0010 and d0020, g0035 between f0030 and
    // h0040, ...) so an add always shifts later positional Ids.
    private readonly Queue<string> _gapRelQueue = new(new[]
    {
        "T/T01/c0015.xml",
        "T/T48/c0015.xml",
        "T/T01/g0035.xml",
        "T/T48/g0035.xml",
    });

    // Base names with deliberate sort gaps between them.
    private static readonly string[] GapNames =
    {
        "b0010", "d0020", "f0030", "h0040", "j0050",
        "l0060", "n0070", "p0080", "r0090", "t0100",
    };

    public IndexFixtureCorpus()
    {
        Root = Path.Combine(Path.GetTempPath(), "readzen-eqharness-" + Guid.NewGuid().ToString("N")[..8]);
        OrigDir = Path.Combine(Root, "xml-p5");
        TranDir = Path.Combine(Root, "xml-p5t");
        Directory.CreateDirectory(OrigDir);
        Directory.CreateDirectory(TranDir);

        // T/T01: 10 both-sides rels.
        foreach (var name in GapNames)
            CreateRel($"T/T01/{name}.xml", orig: true, tran: true);

        // T/T48: 6 both-sides, 2 orig-only, 2 tran-only.
        for (int i = 0; i < 6; i++)
            CreateRel($"T/T48/{GapNames[i]}.xml", orig: true, tran: true);
        CreateRel($"T/T48/{GapNames[6]}.xml", orig: true, tran: false);  // n0070 orig-only
        CreateRel($"T/T48/{GapNames[7]}.xml", orig: true, tran: false);  // p0080 orig-only
        CreateRel($"T/T48/{GapNames[8]}.xml", orig: false, tran: true);  // r0090 tran-only
        CreateRel($"T/T48/{GapNames[9]}.xml", orig: false, tran: true);  // t0100 tran-only
    }

    public IReadOnlyList<string> BothSidesRels => _bothSidesRels;
    public IReadOnlyList<string> OrigOnlyRels => _origOnlyRels;
    public IReadOnlyList<string> TranOnlyRels => _tranOnlyRels;

    /// <summary>All rels currently in the fixture, sorted OrdinalIgnoreCase (index entry order).</summary>
    public IReadOnlyList<string> AllRels
        => _bothSidesRels.Concat(_origOnlyRels).Concat(_tranOnlyRels)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Next rel that <see cref="AddFileMidCorpus"/> would create (peek, not consumed).</summary>
    public string NextGapRel => _gapRelQueue.Peek();

    /// <summary>Total number of xml files currently on disk (both subtrees).</summary>
    public int TotalFileCount
        => Directory.EnumerateFiles(OrigDir, "*.xml", SearchOption.AllDirectories).Count()
         + Directory.EnumerateFiles(TranDir, "*.xml", SearchOption.AllDirectories).Count();

    public string OrigPath(string rel) => Path.Combine(OrigDir, rel.Replace('/', Path.DirectorySeparatorChar));
    public string TranPath(string rel) => Path.Combine(TranDir, rel.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The rare bigram unique to this rel's ORIGINAL side text.</summary>
    public string UniqueOrigGram(string rel) => _uniqueOrigGrams[rel];

    /// <summary>The rare bigram unique to this rel's TRANSLATED side text.</summary>
    public string UniqueTranGram(string rel) => _uniqueTranGrams[rel];

    /// <summary>
    /// Fraction of winner docs (orig side when present, tran side otherwise — the
    /// inverted index keep-first dedup semantics) whose text contains
    /// <see cref="CommonGram"/>. Must exceed 0.8 to arm the DF cutoff.
    /// </summary>
    public double CommonGramWinnerDocFraction()
    {
        var rels = AllRels;
        if (rels.Count == 0) return 0;
        int with = 0;
        foreach (var rel in rels)
        {
            var path = File.Exists(OrigPath(rel)) ? OrigPath(rel) : TranPath(rel);
            if (File.ReadAllText(path).Contains(CommonGram, StringComparison.Ordinal))
                with++;
        }
        return (double)with / rels.Count;
    }

    // ===== Mutation helpers (each returns the rel that changed) =====

    /// <summary>
    /// Adds a new both-sides rel whose name sorts strictly MID-corpus (in a deliberate
    /// gap between existing names), so every later entry's positional Id shifts.
    /// Returns the new rel.
    /// </summary>
    public string AddFileMidCorpus()
    {
        if (_gapRelQueue.Count == 0)
            throw new InvalidOperationException("IndexFixtureCorpus: no gap rels left for AddFileMidCorpus.");
        var rel = _gapRelQueue.Dequeue();
        CreateRel(rel, orig: true, tran: true);
        return rel;
    }

    /// <summary>Deletes every side of <paramref name="rel"/> that exists. Returns the rel.</summary>
    public string RemoveFile(string rel)
    {
        bool removed = false;
        var op = OrigPath(rel);
        if (File.Exists(op)) { File.Delete(op); removed = true; }
        var tp = TranPath(rel);
        if (File.Exists(tp)) { File.Delete(tp); removed = true; }
        if (!removed)
            throw new InvalidOperationException($"IndexFixtureCorpus.RemoveFile: '{rel}' is not present.");
        _bothSidesRels.RemoveAll(r => string.Equals(r, rel, StringComparison.OrdinalIgnoreCase));
        _origOnlyRels.RemoveAll(r => string.Equals(r, rel, StringComparison.OrdinalIgnoreCase));
        _tranOnlyRels.RemoveAll(r => string.Equals(r, rel, StringComparison.OrdinalIgnoreCase));
        return rel;
    }

    /// <summary>
    /// Rewrites one side of <paramref name="rel"/> (orig when present, tran otherwise)
    /// with extra CJK content inserted before the closing &lt;/p&gt;. The byte LENGTH is
    /// guaranteed to change, so both the (mtime,size) stat check and a content hash miss.
    /// Returns the rel.
    /// </summary>
    public string ChangeFile(string rel)
    {
        var path = File.Exists(OrigPath(rel)) ? OrigPath(rel)
                 : File.Exists(TranPath(rel)) ? TranPath(rel)
                 : throw new InvalidOperationException($"IndexFixtureCorpus.ChangeFile: '{rel}' is not present.");

        var text = File.ReadAllText(path);
        if (!text.Contains("</p>", StringComparison.Ordinal))
            throw new InvalidOperationException($"IndexFixtureCorpus.ChangeFile: '{rel}' has no </p> to change.");

        // Marker chars come from a dedicated U+6100 range so they never collide with
        // the per-file unique gram ranges (U+5100 orig / U+5800 tran).
        int c = _changeCount++;
        var marker = string.Concat((char)(0x6100 + 2 * c), (char)(0x6101 + 2 * c), '改', '動', '之', '文');
        long oldLen = new FileInfo(path).Length;
        File.WriteAllText(path, text.Replace("</p>", marker + "</p>", StringComparison.Ordinal));
        if (new FileInfo(path).Length == oldLen)
            throw new InvalidOperationException($"IndexFixtureCorpus.ChangeFile: '{rel}' byte length did not change.");
        return rel;
    }

    // ===== File creation =====

    private void CreateRel(string rel, bool orig, bool tran)
    {
        int i = _nextUniqueIndex++;
        // Unique rare grams: two adjacent code points from dedicated CJK Unified ranges
        // (U+5100+ for orig, U+5800+ for tran) never reused by filler text or markers,
        // so each pair occurs in exactly one file corpus-wide.
        var uniqueOrig = string.Concat((char)(0x5100 + 2 * i), (char)(0x5101 + 2 * i));
        var uniqueTran = string.Concat((char)(0x5800 + 2 * i), (char)(0x5801 + 2 * i));

        if (orig)
        {
            WriteXml(OrigPath(rel), BuildOrigBody(uniqueOrig, i));
            _uniqueOrigGrams[rel] = uniqueOrig;
        }
        if (tran)
        {
            WriteXml(TranPath(rel), BuildTranBody(uniqueTran, rel, i));
            _uniqueTranGrams[rel] = uniqueTran;
        }

        if (orig && tran) _bothSidesRels.Add(rel);
        else if (orig) _origOnlyRels.Add(rel);
        else if (tran) _tranOnlyRels.Add(rel);
    }

    private static string BuildOrigBody(string uniqueGram, int i)
    {
        var sb = new StringBuilder();
        sb.Append("<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>");
        sb.Append(CommonGram);   // 無門 — the >80%-DF common gram
        sb.Append(uniqueGram);   // per-file rare gram
        sb.Append('云');
        // Filler scaled by index so file sizes diverge (shared grams here get cut too).
        for (int k = 0; k <= i % 5; k++)
            sb.Append("如是我聞一時佛在");
        sb.Append("</p></body></text></TEI>");
        return sb.ToString();
    }

    private static string BuildTranBody(string uniqueGram, string rel, int i)
    {
        // Translated side carries CJK too: the common gram (so tran-only winner docs
        // still arm the DF cutoff) plus its own unique rare gram.
        return "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>" +
               $"Case {rel} of the gateless barrier {CommonGram}{uniqueGram}中. " +
               new string('x', 10 + i) +
               "</p></body></text></TEI>";
    }

    private static void WriteXml(string absPath, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(absPath)!);
        File.WriteAllText(absPath, content);
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, true); } catch { }
    }
}
