using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Coverage for <see cref="TranslationMemoryService"/>, which had NO direct tests despite
/// the v8.0.0..HEAD churn that (a) made <see cref="TmRow.BlockNumber"/> nullable — so a
/// JSONL row that omits the key must surface as <c>0</c> in the match output
/// (<c>x.Row.BlockNumber ?? 0</c>) — and (b) replaced the six per-slot cache fields with a
/// single immutable <c>TmCacheSlot(path, writeTimeUtc, rows)</c> reference-swapped per trust
/// level, invalidated by the file's last-write time or an explicit
/// <see cref="TranslationMemoryService.InvalidateCache"/> (audit P2.6 / R3-M10).
///
/// The scoring internals are exercised only enough to force a deterministic match: every
/// fixture row's SourceText is IDENTICAL to the query ZhText, which the service scores 100
/// (well above the approved cutoff 18 / reference cutoff 30) with a guaranteed shared-phrase
/// overlap. Rows are given a RelPath that differs from the query so the self-exclusion in
/// IsExactCurrentSegment never drops them.
/// </summary>
public sealed class TranslationMemoryServiceTests : IDisposable
{
    private readonly string _root;

    // A 5-ideograph phrase: normalized length >= 2 and, being identical to the query,
    // scores 100 with an explainable >=2-char overlap.
    private const string Zh = "菩提本無樹"; // 菩提本無樹

    public TranslationMemoryServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-tm-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private const string ApprovedFile = "translation-memory.approved.jsonl";
    private const string ReferenceFile = "translation-memory.reference.jsonl";

    private string Path_(string file) => Path.Combine(_root, file);

    /// <summary>Serialize rows the same way the community writers do (missing BlockNumber key omitted).</summary>
    private void WriteJsonl(string file, params TmRow[] rows)
    {
        var sb = new StringBuilder();
        foreach (var r in rows)
            sb.Append(JsonSerializer.Serialize(r)).Append('\n');
        File.WriteAllText(Path_(file), sb.ToString(), new UTF8Encoding(false));
    }

    private static TmRow Row(string target, string rel, int? block) => new()
    {
        SourceText = Zh,
        TargetText = target,
        RelPath = rel,
        BlockNumber = block,
    };

    private static CurrentSegmentContext Ctx() => new()
    {
        RelPath = "T/T01/current.xml",
        BlockNumber = 5,
        ZhText = Zh,
    };

    // ---------------------------------------------------------- nullable BlockNumber

    [Fact]
    public async Task FindApprovedMatches_RowWithoutBlockNumberKey_SurfacesBlockZero()
    {
        // The line-131 change: a legacy/approved row whose JSONL omits BlockNumber
        // deserializes to null and must map to 0 in the emitted match (never null, never crash).
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("Bodhi has no tree", "T/T01/other.xml", block: null));

        var matches = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);

        var m = Assert.Single(matches);
        Assert.Equal(0, m.BlockNumber);
        Assert.Equal("Bodhi has no tree", m.TargetText);
        Assert.Equal(TranslationResourceTrust.Approved, m.Trust);
    }

    [Fact]
    public async Task FindApprovedMatches_RowWithBlockNumber_PreservesIt()
    {
        // The ?? 0 must NOT clobber a present value — pins both sides of the coalesce.
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("with block", "T/T01/other.xml", block: 7));

        var matches = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);

        var m = Assert.Single(matches);
        Assert.Equal(7, m.BlockNumber);
    }

    [Fact]
    public async Task FindReferenceMatches_ReadsReferenceFile_AndTagsTrust()
    {
        // Reference matches come from the .reference.jsonl file and carry AiReference trust.
        var svc = new TranslationMemoryService();
        WriteJsonl(ReferenceFile, Row("ai draft", "T/T01/other.xml", block: null));

        var matches = await svc.FindReferenceMatchesAsync(Ctx(), _root, null);

        var m = Assert.Single(matches);
        Assert.Equal(TranslationResourceTrust.AiReference, m.Trust);
        Assert.Equal(0, m.BlockNumber);
        Assert.Equal("ai draft", m.TargetText);
    }

    // ---------------------------------------------------------- empty / missing inputs

    [Fact]
    public async Task FindApprovedMatches_NullOrMissingRoot_ReturnsEmpty()
    {
        var svc = new TranslationMemoryService();
        Assert.Empty(await svc.FindApprovedMatchesAsync(Ctx(), null, null));
        Assert.Empty(await svc.FindApprovedMatchesAsync(Ctx(), "   ", null));
    }

    [Fact]
    public async Task FindApprovedMatches_FileAbsent_ReturnsEmpty()
    {
        var svc = new TranslationMemoryService();
        // Root exists but the approved file was never written.
        Assert.Empty(await svc.FindApprovedMatchesAsync(Ctx(), _root, null));
    }

    // ---------------------------------------------------------- cache: mtime + invalidate

    [Fact]
    public async Task Cache_ServesByMtime_ThenReloadsWhenWriteTimeAdvances()
    {
        // First query caches the single row; rewriting the file with a SECOND matching row
        // and advancing the last-write time must invalidate the (path, time, rows) slot so
        // the new row appears without an explicit InvalidateCache().
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("first", "T/T01/a.xml", block: null));

        var first = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Single(first);

        WriteJsonl(ApprovedFile,
            Row("first", "T/T01/a.xml", block: null),
            Row("second", "T/T01/b.xml", block: null));
        File.SetLastWriteTimeUtc(Path_(ApprovedFile), DateTime.UtcNow.AddSeconds(10));

        var second = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Equal(2, second.Count);
    }

    [Fact]
    public async Task Cache_UnchangedMtime_ServesStale_UntilInvalidateCache()
    {
        // Prime the cache, then rewrite the file with an added row but PIN the last-write time
        // to its previous value so the mtime check would keep serving the cached rows. This
        // isolates two behaviors at once: (1) the cache genuinely serves by (path, mtime) —
        // the added row is NOT seen while mtime is unchanged; (2) InvalidateCache() drops the
        // slot so the very next query re-reads from disk and sees the new row.
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("first", "T/T01/a.xml", block: null));

        var primed = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Single(primed);
        var pinnedTime = File.GetLastWriteTimeUtc(Path_(ApprovedFile));

        WriteJsonl(ApprovedFile,
            Row("first", "T/T01/a.xml", block: null),
            Row("second", "T/T01/b.xml", block: null));
        File.SetLastWriteTimeUtc(Path_(ApprovedFile), pinnedTime); // mtime unchanged → cache stays warm

        var stale = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Single(stale); // served from cache; the second row is not visible yet

        svc.InvalidateCache();

        var fresh = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Equal(2, fresh.Count); // slot dropped → disk re-read picks up the added row
    }

    [Fact]
    public async Task Cache_ApprovedAndReferenceSlots_AreIndependent()
    {
        // The two trust levels have separate slots — InvalidateCache clears both, but a warm
        // approved query must not populate/serve the reference slot or vice versa.
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("appr", "T/T01/a.xml", block: 1));
        WriteJsonl(ReferenceFile, Row("ref", "T/T01/b.xml", block: 2));

        var appr = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        var refr = await svc.FindReferenceMatchesAsync(Ctx(), _root, null);

        Assert.Equal("appr", Assert.Single(appr).TargetText);
        Assert.Equal("ref", Assert.Single(refr).TargetText);
        Assert.Equal(TranslationResourceTrust.Approved, appr[0].Trust);
        Assert.Equal(TranslationResourceTrust.AiReference, refr[0].Trust);
    }

    [Fact]
    public async Task WarmupCache_ThenQuery_ServesFromCacheWithoutRereading()
    {
        // Warmup populates both slots; a subsequent same-mtime query must return the warmed
        // rows (proves warmup and query share the same slot keyed by path+mtime).
        var svc = new TranslationMemoryService();
        WriteJsonl(ApprovedFile, Row("warm", "T/T01/a.xml", block: null));

        await svc.WarmupCacheAsync(_root, CancellationToken.None);

        // Corrupt the on-disk file WITHOUT touching mtime — if warmup cached correctly, the
        // query is served from the warm slot and still finds the row.
        var pinnedTime = File.GetLastWriteTimeUtc(Path_(ApprovedFile));
        File.WriteAllText(Path_(ApprovedFile), "{ not json at all", new UTF8Encoding(false));
        File.SetLastWriteTimeUtc(Path_(ApprovedFile), pinnedTime);

        var matches = await svc.FindApprovedMatchesAsync(Ctx(), _root, null);
        Assert.Single(matches);
        Assert.Equal("warm", matches[0].TargetText);
    }
}
