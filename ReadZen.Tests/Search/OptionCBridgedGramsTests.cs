using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Option C end-to-end coverage: the desktop inverted index is now gapless over CJK —
/// it strips editorial punctuation and bridges every tag boundary (incl a self-closing
/// &lt;lb/&gt;), so a CJK phrase split across an &lt;lb/&gt; resolves on the instant
/// inverted path (previously silently missed when another doc had a contiguous hit). It
/// is also treated as AUTHORITATIVE for all-CJK queries (empty result = genuine zero →
/// bloom sweep skipped), while mixed/non-CJK queries still fall to the bloom path.
///
/// The candidate-phase progress report (Phase = "... (inverted index)" vs
/// "Candidate filtering done" vs "Brute candidates (1-char search)") is the observable
/// used to assert WHICH path ran.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class OptionCBridgedGramsTests : IDisposable
{
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

    public OptionCBridgedGramsTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-optionc-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    /// <summary>Synchronous IProgress capture (Progress&lt;T&gt; posts async and would race the asserts).</summary>
    private sealed class PhaseCapture : IProgress<SearchIndexService.SearchProgress>
    {
        public readonly List<string> Phases = new();
        public void Report(SearchIndexService.SearchProgress value) => Phases.Add(value.Phase);
    }

    private void WriteOrig(string name, string bodyInner)
        => File.WriteAllText(Path.Combine(_origDir, name),
            $"<TEI><text><body>{bodyInner}</body></text></TEI>");

    private void WriteTran(string name, string bodyInner)
        => File.WriteAllText(Path.Combine(_tranDir, name),
            $"<TEI><text><body>{bodyInner}</body></text></TEI>");

    private async Task<(List<SearchResultGroup> groups, PhaseCapture prog)> RunAsync(
        SearchIndexService svc, string query, bool includeTranslated = false)
    {
        var manifest = await svc.TryLoadAsync(_root);
        Assert.NotNull(manifest);
        var prog = new PhaseCapture();
        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            _root, _origDir, _tranDir, manifest!, query,
            includeOriginal: true, includeTranslated: includeTranslated,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null), contextWidth: 30,
            progress: prog))
        {
            groups.Add(g);
        }
        return (groups, prog);
    }

    private async Task<SearchIndexService> BuildAsync()
    {
        var svc = new SearchIndexService();
        await svc.BuildAsync(_root, _origDir, new[] { _tranDir });
        return svc;
    }

    [Fact(Skip = "Option C StripAndBridge pending user approval (PUNCT_DECISION_v1.md, RUN-20260715-1458): production gram producers intentionally do not bridge across a <lb/> boundary. Un-skip when Option C is approved, wired into the 3 producers, and the search BuildGuid is bumped.")]
    public async Task CrossLbPhrase_Found_ViaInvertedPath()
    {
        // Doc A: the phrase 峨而 contiguous. Doc B: the SAME phrase split by a real
        // self-closing <lb/> (峨<lb/>而) — R4's dominant silent-miss shape. Both must be
        // returned, and the candidate phase must report the inverted path (no bloom sweep).
        WriteOrig("a-contiguous.xml", "上堂峨而不群");
        WriteOrig("b-crosslb.xml", "上堂峨<lb n=\"0384a05\"/>而不群");

        var svc = await BuildAsync();
        var (groups, prog) = await RunAsync(svc, "峨而");

        var rels = groups.Select(g => Path.GetFileName(g.RelPath)).ToHashSet();
        Assert.Contains("a-contiguous.xml", rels);
        Assert.Contains("b-crosslb.xml", rels); // pre-fix: silently missed
        Assert.Equal(2, groups.Count);

        Assert.Contains(prog.Phases, p => p.Contains("(inverted index)"));
        Assert.DoesNotContain(prog.Phases, p => p == "Candidate filtering done"); // bloom phrase absent
    }

    [Fact(Skip = "Option C gapless-authoritative index pending user approval (PUNCT_DECISION_v1.md, RUN-20260715-1458): without bridging the inverted index is not gapless, so production does not treat an empty all-CJK result as authoritative and still runs the bloom sweep. Un-skip when Option C lands.")]
    public async Task AbsentAllCjkQuery_YieldsZero_WithoutBloomSweep()
    {
        // An all-CJK query absent from every doc: the gapless inverted index is
        // authoritative, so an empty result is a genuine zero and the bloom sweep is
        // skipped (usedInvertedIndex = true even at zero hits).
        WriteOrig("a.xml", "上堂峨而不群");
        WriteOrig("b.xml", "山河大地");

        var svc = await BuildAsync();
        var (groups, prog) = await RunAsync(svc, "麒麟"); // present nowhere

        Assert.Empty(groups);
        Assert.Contains(prog.Phases, p => p.Contains("(inverted index)"));
        Assert.DoesNotContain(prog.Phases, p => p == "Candidate filtering done");
    }

    [Fact]
    public async Task MixedScriptQuery_StillUsesBloom()
    {
        // A query with a non-CJK code unit is NOT fully covered by the CJK-only inverted
        // index → usedInvertedIndex stays false → the bloom candidate path runs.
        WriteOrig("a.xml", "上堂峨而不群");

        var svc = await BuildAsync();
        var (_, prog) = await RunAsync(svc, "峨A");

        Assert.Contains(prog.Phases, p => p == "Candidate filtering done"); // bloom path
        Assert.DoesNotContain(prog.Phases, p => p.Contains("(inverted index)"));
    }

    [Fact]
    public async Task NoteText_RemainsSearchable()
    {
        // D1/SPA parity: <note> text is included in the searchable text (only <app> is
        // skipped), so a bigram that appears ONLY inside a note is found.
        WriteOrig("noted.xml", "上堂說法<note>祖庭事苑</note>而退");

        var svc = await BuildAsync();
        var (groups, prog) = await RunAsync(svc, "事苑"); // occurs only in the note

        Assert.Single(groups);
        Assert.Equal("noted.xml", Path.GetFileName(groups[0].RelPath));
        Assert.Contains(prog.Phases, p => p.Contains("(inverted index)"));
    }

    // ---- Documentation test: which SIDE the "authoritative" index actually covers ----

    /// <summary>
    /// PINS CURRENT REALITY (documentation, not an endorsement). The inverted index holds
    /// ONE gram set per relPath: entries are fed orig-then-tran and Build dedups keep-FIRST
    /// (InvertedSearchIndex.cs:233-239), so for a rel that has BOTH sides the ORIGINAL
    /// side's grams win and the translated side's grams are computed and then dropped.
    /// A translated-ONLY rel has no original entry, so its translated grams are what the
    /// index stores for it.
    ///
    /// CONSEQUENCE (the accepted edge): CJK that exists ONLY in a both-sides rel's
    /// translation is not indexed, and because an all-CJK query is treated as
    /// authoritative the bloom sweep no longer catches it — such a query returns zero.
    /// This is near-nil in practice: translations are English projections, so any CJK they
    /// carry mirrors the indexed original. If it ever becomes real, the fix is to UNION
    /// both sides' grams per rel at build time (NOT to gate the authoritative branch on
    /// !includeTranslated, which would push every translated-inclusive CJK search onto the
    /// slow bloom sweep). Any future change here should be deliberate — and should start
    /// by making this test fail on purpose.
    /// </summary>
    [Fact(Skip = "Option C gapless-authoritative index pending user approval (PUNCT_DECISION_v1.md, RUN-20260715-1458): asserts the authoritative all-CJK path skips the bloom rescue (part 2), which is not production behavior until the gapless-bridging index lands. Un-skip when Option C is approved.")]
    public async Task InvertedIndex_StoresOriginalSideGrams_TranslatedSideIsProjection()
    {
        // both.xml: CJK 峨而 on the original side; the translation carries 麒麟, which
        // appears NOWHERE in the original — the shape the index cannot see.
        WriteOrig("both.xml", "上堂峨而不群");
        WriteTran("both.xml", "He entered the hall 麒麟 alone.");
        // tranonly.xml: no original side at all → keep-first keeps the TRANSLATED grams.
        WriteTran("tranonly.xml", "The gateless barrier 山河大地 case.");

        var svc = await BuildAsync();

        // (1) Original-side CJK is indexed → found.
        var (origHits, _) = await RunAsync(svc, "峨而", includeTranslated: true);
        Assert.Single(origHits);

        // (2) Translated-only CJK on a BOTH-sides rel: dropped by keep-first, and the
        //     authoritative all-CJK path skips the bloom sweep → zero. Current behavior.
        var (tranHits, prog) = await RunAsync(svc, "麒麟", includeTranslated: true);
        Assert.Empty(tranHits);
        Assert.Contains(prog.Phases, p => p.Contains("(inverted index)")); // no bloom rescue

        // (3) A translated-ONLY rel wins keep-first, so ITS translated CJK is indexed.
        var (tranOnlyHits, _) = await RunAsync(svc, "山河", includeTranslated: true);
        Assert.Single(tranOnlyHits);
        Assert.Equal("tranonly.xml", Path.GetFileName(tranOnlyHits[0].RelPath));
    }

    // ---- `invertedAuthoritative` condition boundaries ----

    /// <summary>
    /// Length boundary: len == 1 is BELOW the inverted path's 2-char floor (a bigram index
    /// cannot answer a unigram), so neither the inverted nor the bloom path runs — the
    /// brute 1-char sweep does, and still finds the hit. Partner of the len >= 2 cases
    /// above. (Desktop has no unigram shards; SPA parity there is deferred, plan D5.)
    /// </summary>
    [Fact]
    public async Task SingleCharCjkQuery_UsesBruteSweep()
    {
        WriteOrig("a.xml", "上堂峨而不群");

        var svc = await BuildAsync();
        var (groups, prog) = await RunAsync(svc, "峨");

        Assert.Single(groups); // brute sweep is still correct
        Assert.Contains(prog.Phases, p => p == "Brute candidates (1-char search)");
        Assert.DoesNotContain(prog.Phases, p => p.Contains("(inverted index)"));
    }

    /// <summary>
    /// Range boundary: Yijing hexagram symbols (U+4DC0-U+4DFF) sit in the DELIBERATE gap
    /// between Ext-A and CJK Unified in CjkText.IsIdeograph — they are not ideographs, are
    /// never indexed, and must therefore never make the index authoritative (that would be
    /// a guaranteed false zero). Must reach the bloom path.
    /// </summary>
    [Fact]
    public async Task YijingHexagramQuery_FallsBackToBloom()
    {
        WriteOrig("a.xml", "上堂峨而不群");

        var svc = await BuildAsync();
        var (_, prog) = await RunAsync(svc, "䷀䷁");

        Assert.Contains(prog.Phases, p => p == "Candidate filtering done"); // bloom
        Assert.DoesNotContain(prog.Phases, p => p.Contains("(inverted index)"));
    }

    /// <summary>
    /// Supplementary-plane boundary: a CJK Ext-B ideograph is a SURROGATE PAIR, so it is
    /// two chars long (passing the len >= 2 gate) but neither half satisfies IsIndexableCjk
    /// — it is never indexed, so the index must not claim authority and the query must
    /// still reach bloom rather than report a false zero.
    /// </summary>
    [Fact]
    public async Task ExtBSurrogateQuery_FallsBackToBloom()
    {
        WriteOrig("a.xml", "上堂峨而不群");

        var svc = await BuildAsync();
        var (_, prog) = await RunAsync(svc, "\U00020000");

        Assert.Contains(prog.Phases, p => p == "Candidate filtering done"); // bloom
        Assert.DoesNotContain(prog.Phases, p => p.Contains("(inverted index)"));
    }

    /// <summary>
    /// Predicate-mismatch guard. The query-normalization trigger
    /// (<c>CjkMatchNormalizer.ContainsCjk</c>) covers U+4E00-U+9FFF ONLY, while the
    /// authority gate (<c>IsIndexableCjk</c>) covers the wider three-range ideograph set.
    /// So an all-Ext-A query skips query normalization yet is still treated as
    /// authoritative — which READS like a bug and is not: normalization only strips
    /// whitespace and editorial punctuation, and an all-ideograph query has neither, so
    /// Normalize() would be a no-op. Ext-A IS indexed, so the index genuinely is
    /// authoritative here. (An Ext-A query that DID contain punctuation would keep it,
    /// fail the all-indexable check, and fall to bloom — also safe.)
    /// </summary>
    [Fact]
    public async Task AllExtACjkQuery_IsAuthoritative_AndFound()
    {
        WriteOrig("exta.xml", "㐀㐁㐂");

        var svc = await BuildAsync();
        var (groups, prog) = await RunAsync(svc, "㐀㐁");

        Assert.Single(groups);
        Assert.Contains(prog.Phases, p => p.Contains("(inverted index)"));
    }
}
