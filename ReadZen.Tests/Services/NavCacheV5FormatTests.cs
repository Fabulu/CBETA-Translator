using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-NV2 (NAV_CACHE_REDESIGN §2, §3.1, §3.4, §7 "Format &amp; gate"): the v5 nav cache is
/// machine-independent (source tokens + content sigs, no absolute paths outside the
/// informational RootPath), and its gate reuses everything the source manifest proves
/// unchanged — recomputing ONLY the genuinely-changed candidates.
///
/// Recomputes are counted with a spy <see cref="INavStatusEvaluator"/> wrapping the real
/// evaluator, so <c>CandidateCalls</c> is the exact number of per-candidate status
/// evaluations the refresh performed. The fast path performs ZERO.
/// </summary>
public sealed class NavCacheV5FormatTests : IDisposable
{
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

    private const string OrigXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>禪宗祖師傳法心印無門關</p>\n" +
        "</body></text></TEI>\n";

    private const string GreenXml =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
        "<p>The gateless gate of the ancestors transmitting the mind seal.</p>\n" +
        "</body></text></TEI>\n";

    private static readonly DateTime BaseAnchor = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FutureAnchor = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CloneAnchor = new(2031, 3, 3, 3, 3, 3, DateTimeKind.Utc);

    public NavCacheV5FormatTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-navv5-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    // ----------------------------------------------------------------- helpers

    /// <summary>Counts every per-candidate status evaluation the refresh performs.</summary>
    private sealed class CountingNavEvaluator : INavStatusEvaluator
    {
        private readonly INavStatusEvaluator _inner =
            new NavStatusEvaluator(new TranslationStatusService(), new IndexedTranslationService());
        private int _calls;
        public int CandidateCalls => Volatile.Read(ref _calls);
        public void Reset() => Volatile.Write(ref _calls, 0);

        public TranslationStatus ComputeCandidateStatus(string origAbs, string tranAbs)
        {
            Interlocked.Increment(ref _calls);
            return _inner.ComputeCandidateStatus(origAbs, tranAbs);
        }
        public TranslationStatus EvaluateEntry(string origAbs, IReadOnlyList<string> candidates)
            => _inner.EvaluateEntry(origAbs, candidates);
        public bool IsMeaningfullyTranslated(string origAbs, string tranAbs)
            => _inner.IsMeaningfullyTranslated(origAbs, tranAbs);
        public void ClearCache() => _inner.ClearCache();
    }

    private static void WriteFile(string path, string content, DateTime mtimeUtc)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, mtimeUtc);
    }

    private static string Rel(int i) => $"t{i:D4}.xml";
    private string OrigPath(string rel) => Path.Combine(_origDir, rel);
    private string TranPath(string rel) => Path.Combine(_tranDir, rel);
    private string CommunityPath(string user, string rel)
        => Path.Combine(_root, "community", "translations", user, rel);

    private void MakeGreenCorpus(int n)
    {
        for (int i = 0; i < n; i++)
        {
            WriteFile(OrigPath(Rel(i)), OrigXml, BaseAnchor);
            WriteFile(TranPath(Rel(i)), GreenXml, BaseAnchor);
        }
    }

    private async Task<(IndexCacheService svc, CountingNavEvaluator eval, IndexCache loaded)> BuildAndLoadAsync()
    {
        var eval = new CountingNavEvaluator();
        var svc = new IndexCacheService(eval, new TranslationStatusService());
        var built = await svc.BuildAsync(_origDir, _tranDir, _root);
        await svc.SaveAsync(_root, built);
        var loaded = await svc.TryLoadAsync(_root);
        Assert.NotNull(loaded);
        eval.Reset(); // count only the refresh under test
        return (svc, eval, loaded!);
    }

    private static FileNavItem Entry(IndexCache cache, string rel)
        => cache.Entries.Single(e => string.Equals(
            e.RelPath.Replace('\\', '/'), rel.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    // ================================================================ machine independence

    /// <summary>The serialized cache embeds NO absolute path anywhere except the
    /// informational RootPath — the property that lets a CI-baked bundle load on any machine.</summary>
    [Fact]
    public async Task Cache_HasNoAbsolutePaths()
    {
        MakeGreenCorpus(3);
        WriteFile(CommunityPath("alice", Rel(0)), GreenXml, BaseAnchor);
        var (svc, _, _) = await BuildAndLoadAsync();

        var json = await File.ReadAllTextAsync(svc.GetCachePath(_root));

        // Blank the one sanctioned absolute field, then assert nothing machine-bound remains.
        var node = JsonNode.Parse(json)!;
        node["RootPath"] = "";
        var stripped = node.ToJsonString();

        Assert.DoesNotContain(_root, stripped, StringComparison.OrdinalIgnoreCase);
        Assert.False(Regex.IsMatch(stripped, "[A-Za-z]:\\\\"), "no Windows drive path");
        Assert.DoesNotContain("/home/", stripped, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Users/", stripped, StringComparison.OrdinalIgnoreCase);

        // Sanity: candidate tokens are RELATIVE ids, not paths.
        var reloaded = JsonSerializer.Deserialize<IndexCache>(json)!;
        var withSrc = reloaded.Entries.First(e => e.Sources.Count > 0);
        Assert.Contains(withSrc.Sources, s => s.Token == "canonical" || s.Token == "user:alice");
        Assert.All(reloaded.Entries, e => Assert.Null(e.TranResolvedPath));
    }

    /// <summary>A cache whose baked RootPath points at a foreign machine LOADS (root is no
    /// longer compared) and is RE-HOMED to the local root on the next save.</summary>
    [Fact]
    public async Task ForeignRootPath_LoadsAndAdopts()
    {
        MakeGreenCorpus(2);
        var (svc, _, _) = await BuildAndLoadAsync();

        var cachePath = svc.GetCachePath(_root);
        var node = JsonNode.Parse(await File.ReadAllTextAsync(cachePath))!;
        node["RootPath"] = "Z:\\ci\\bake\\CbetaZenTranslations";
        await File.WriteAllTextAsync(cachePath, node.ToJsonString());

        var loaded = await svc.TryLoadAsync(_root);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Entries.Count);

        // Adoption writes the local root back (re-home).
        await svc.SaveAsync(_root, loaded);
        var reRead = JsonNode.Parse(await File.ReadAllTextAsync(cachePath))!;
        Assert.Equal(_root, (string?)reRead["RootPath"]);
    }

    // ================================================================ the re-clone cert

    /// <summary>
    /// Re-clone cert (NAV_CACHE_REDESIGN §8 row 2): rewriting EVERY working-tree mtime
    /// (content unchanged) hits the fast path — ZERO per-candidate evaluations, statuses
    /// preserved. (The healed mtime hints cost exactly one write-back save; the SECOND
    /// refresh is then fully steady — zero recomputes AND no save.)
    /// </summary>
    [Fact]
    public async Task MtimeReset_AllFiles_ZeroRecomputes()
    {
        MakeGreenCorpus(4);
        var (svc, eval, loaded) = await BuildAndLoadAsync();

        var before = loaded.Entries.ToDictionary(
            e => e.RelPath.Replace('\\', '/'), e => e.Status, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < 4; i++)
        {
            File.SetLastWriteTimeUtc(OrigPath(Rel(i)), CloneAnchor);
            File.SetLastWriteTimeUtc(TranPath(Rel(i)), CloneAnchor);
        }

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, eval.CandidateCalls);                       // the cert: zero recomputes
        foreach (var e in refreshed.Entries)
            Assert.Equal(before[e.RelPath.Replace('\\', '/')], e.Status);

        // Steady state: second refresh does no work and no save.
        eval.Reset();
        var cachePath = svc.GetCachePath(_root);
        var bytes = File.ReadAllBytes(cachePath);
        var reloaded = await svc.TryLoadAsync(_root);
        var again = await svc.RefreshAsync(reloaded!, _origDir, _tranDir, _root);
        Assert.Equal(0, eval.CandidateCalls);
        Assert.Equal(4, again.Entries.Count);
        Assert.Equal(bytes, File.ReadAllBytes(cachePath));          // no resave
    }

    // ================================================================ per-candidate gate (K = 1)

    /// <summary>An original edit (size change) recomputes exactly that one entry — and,
    /// because status is a function of (orig, tran), re-evaluates its candidate.</summary>
    [Fact]
    public async Task OrigSizeChange_RecomputesOne()
    {
        MakeGreenCorpus(3);
        var (svc, eval, loaded) = await BuildAndLoadAsync();

        WriteFile(OrigPath(Rel(0)), OrigXml.Replace("</body>", "<p>增訂內容</p></body>"), FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);
        Assert.Equal(3, refreshed.Entries.Count);
    }

    /// <summary>A translation content edit (bytes + mtime move ⇒ hint miss ⇒ re-hash ⇒ new
    /// ContentSig) recomputes exactly that one candidate.</summary>
    [Fact]
    public async Task TranslationContentChange_RecomputesOne()
    {
        MakeGreenCorpus(3);
        var (svc, eval, loaded) = await BuildAndLoadAsync();

        WriteFile(TranPath(Rel(1)), GreenXml + "<!-- refined -->\n", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);
        Assert.Equal(TranslationStatus.Green, Entry(refreshed, Rel(1)).Status);
    }

    /// <summary>A new community translation for a rel that already has a canonical one adds
    /// exactly one candidate — only THAT candidate is evaluated (canonical's verdict reused).</summary>
    [Fact]
    public async Task NewCommunityFile_RecomputesOne()
    {
        MakeGreenCorpus(3);
        var (svc, eval, loaded) = await BuildAndLoadAsync();
        Assert.Equal(new[] { "canonical" }, Entry(loaded, Rel(0)).Sources.Select(s => s.Token).ToArray());

        WriteFile(CommunityPath("alice", Rel(0)), GreenXml, FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(1, eval.CandidateCalls);                       // only the new user:alice candidate
        var e = Entry(refreshed, Rel(0));
        Assert.Equal(new[] { "canonical", "user:alice" }, e.Sources.Select(s => s.Token).ToArray());
    }

    /// <summary>A removed translation drops its candidate and recomputes the entry to Red —
    /// a trivial no-candidate verdict, so ZERO evaluator calls.</summary>
    [Fact]
    public async Task RemovedTranslation_RecomputesToRed()
    {
        MakeGreenCorpus(2);
        var (svc, eval, loaded) = await BuildAndLoadAsync();
        Assert.Equal(TranslationStatus.Green, Entry(loaded, Rel(0)).Status);

        File.Delete(TranPath(Rel(0)));

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, eval.CandidateCalls);                       // Red by absence
        var e = Entry(refreshed, Rel(0));
        Assert.Equal(TranslationStatus.Red, e.Status);
        Assert.Empty(e.Sources);
    }

    // ================================================================ titles = display-only

    /// <summary>A titles.jsonl edit re-derives display fields ONLY, keeps statuses, and calls
    /// the evaluator ZERO times (NAV_CACHE_REDESIGN §3.4).</summary>
    [Fact]
    public async Task TitlesEdit_RefreshesDisplayFields_KeepsStatuses_NoEvaluatorCalls()
    {
        WriteFile(Path.Combine(_root, "titles.jsonl"),
            "{\"path\":\"t0000.xml\",\"en\":\"Original title\",\"enShort\":\"Orig\"}\n", BaseAnchor);
        MakeGreenCorpus(3);
        var (svc, eval, loaded) = await BuildAndLoadAsync();
        Assert.Equal("Orig", Entry(loaded, Rel(0)).DisplayShort);

        WriteFile(Path.Combine(_root, "titles.jsonl"),
            "{\"path\":\"t0000.xml\",\"en\":\"Edited title\",\"enShort\":\"Edited\"}\n", FutureAnchor);

        var refreshed = await svc.RefreshAsync(loaded, _origDir, _tranDir, _root);

        Assert.Equal(0, eval.CandidateCalls);
        Assert.Equal("Edited", Entry(refreshed, Rel(0)).DisplayShort);
        Assert.All(refreshed.Entries, e => Assert.Equal(TranslationStatus.Green, e.Status));
    }

    // ================================================================ hint heal (stampWriteBack)

    /// <summary>
    /// Hint-heal (NAV_CACHE_REDESIGN §2.2): a cache carrying CI-baked hints whose (size,mtime)
    /// do not match the local files re-hashes those K translations on the FIRST launch and
    /// heals the hints (one write-back save); the SECOND launch matches every hint and does
    /// no work and no save. Recomputes stay 0 throughout (content, hence SourceSig, unchanged).
    /// </summary>
    [Fact]
    public async Task HintHeal_FirstLaunchRehashes_SecondLaunchSteady()
    {
        MakeGreenCorpus(3); // K = 3 translated files
        var (svc, eval, _) = await BuildAndLoadAsync();
        var cachePath = svc.GetCachePath(_root);

        // Simulate CI-baked hints: rewrite every stored hint's mtime to a bogus value so the
        // first local scan misses (content sig stays correct, so the fast path still holds).
        var node = JsonNode.Parse(await File.ReadAllTextAsync(cachePath))!;
        foreach (var entry in node["Entries"]!.AsArray())
        {
            var hints = entry!["TranLocalHints"]?.AsArray();
            if (hints == null) continue;
            foreach (var h in hints)
                h!["MtimeTicks"] = 123456789L;
        }
        await File.WriteAllTextAsync(cachePath, node.ToJsonString());

        // First launch: hint miss ⇒ re-hash K, heal, save. Zero recomputes.
        eval.Reset();
        var loaded1 = await svc.TryLoadAsync(_root);
        var r1 = await svc.RefreshAsync(loaded1!, _origDir, _tranDir, _root);
        Assert.Equal(0, eval.CandidateCalls);

        // Healed on disk: hint mtime now equals the local file mtime.
        var healed = await svc.TryLoadAsync(_root);
        var hint0 = Entry(healed!, Rel(0)).TranLocalHints!.Single(h => h.Token == "canonical");
        Assert.Equal(BaseAnchor.Ticks, hint0.MtimeTicks);

        // Second launch: every hint matches ⇒ no re-hash, no save.
        eval.Reset();
        var bytes = File.ReadAllBytes(cachePath);
        var r2 = await svc.RefreshAsync(healed!, _origDir, _tranDir, _root);
        Assert.Equal(0, eval.CandidateCalls);
        Assert.Equal(bytes, File.ReadAllBytes(cachePath)); // steady state ⇒ no resave
        Assert.Equal(3, r2.Entries.Count);
    }
}
