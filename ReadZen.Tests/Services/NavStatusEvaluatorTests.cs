using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// PR-NV1 (NAV_CACHE_REDESIGN §7 "Evaluator parity"): pins <see cref="NavStatusEvaluator"/>
/// against the behavior of the still-running MainWindowViewModel sweep
/// (<c>EvaluateBestTranslationSource</c>) BEFORE that sweep is retired (§8, PR-NV4 lands
/// two PRs later precisely to give this evaluator soak time).
///
/// The backbone assertion is <c>evaluator.EvaluateEntry(...) == OldEntryStatus(...)</c>,
/// where <see cref="OldEntryStatus"/> is a faithful transcription of the OLD MWVM inner
/// loop: per candidate, <c>File.Exists &amp;&amp; meaningful ? ComputeStatus : Red</c>, then
/// the max over candidates by the Green &gt; Yellow &gt; Red rank (stars / community /
/// mtime only tie-break the read-PATH pick among EQUAL-status candidates, so they never
/// change the displayed status). The critical demotion cells (identical-bytes,
/// DeepEqual-not-byte-equal, Chinese-only stub, and the Latin-romanization false-Yellow)
/// also carry explicit literal pins.
/// </summary>
public sealed class NavStatusEvaluatorTests : IDisposable
{
    private readonly string _root;

    public NavStatusEvaluatorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-navstatus-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    // ------------------------------------------------------------------ fixtures

    // Chinese-only original (無門關 opening).
    private const string BodyOrigCjk = "<p>禪宗祖師傳法心印無門關</p>";
    // Fully translated: English only, no CJK ⇒ Green.
    private const string BodyFullEn = "<p>The gateless gate of the ancestors transmitting the mind-seal.</p>";
    // Partially translated: CJK + English ⇒ Yellow (and meaningful).
    private const string BodyPartial = "<p>禪宗祖師 the gateless gate transmitting the mind-seal.</p>";
    // Chinese-only stub that DIFFERS from the original ⇒ not byte-identical, but no EN ⇒ Red.
    private const string BodyStubCjk = "<p>完全沒有翻譯只有不同的漢字內容在這裡</p>";
    // Original whose body already carries Latin romanization: step-1 body analysis sees
    // both CJK and Latin ⇒ false-Yellow. Only the meaningfulness demotion catches it.
    private const string BodyOrigRomanized = "<p>禪宗祖師 chan zong zu shi chuan fa xin yin</p>";

    private static string Tei(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>" + body + "</body></text></TEI>\n";

    // Parses to the SAME element tree as Tei(body) (an extra space inside the <body>
    // start tag is normalized away by the parser) but with different bytes ⇒ a
    // deterministic "DeepEqual-but-not-byte-equal" pair with no dependency on how the
    // XML declaration or trailing whitespace is retained.
    private static string TeiTagSpaced(string body) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body >" + body + "</body></text></TEI>\n";

    private string Write(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static INavStatusEvaluator NewEvaluator() =>
        new NavStatusEvaluator(new TranslationStatusService(), new IndexedTranslationService());

    // ------------------------------------------------------------- old-pipeline oracle

    // Faithful transcription of the OLD MainWindowViewModel.EvaluateBestTranslationSource
    // inner loop's STATUS math (memo omitted — it never changes a verdict).
    private static TranslationStatus OldEntryStatus(string origAbs, IEnumerable<string> candidates)
    {
        var best = TranslationStatus.Red;
        foreach (var tran in candidates)
        {
            var status = TranslationStatus.Red;
            if (File.Exists(tran) && OldIsMeaningful(origAbs, tran))
                status = new TranslationStatusService().ComputeStatusForPairLive(origAbs, tran, string.Empty, string.Empty, false);
            if (Rank(status) > Rank(best))
                best = status;
        }
        return best;
    }

    private static bool OldIsMeaningful(string origAbs, string tranAbs)
    {
        try
        {
            if (!File.Exists(origAbs) || !File.Exists(tranAbs))
                return false;
            var originalXml = File.ReadAllText(origAbs, Encoding.UTF8);
            var candidateXml = File.ReadAllText(tranAbs, Encoding.UTF8);
            if (Parses(originalXml) && Parses(candidateXml)
                && XNode.DeepEquals(
                    XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace),
                    XDocument.Parse(candidateXml, LoadOptions.PreserveWhitespace)))
                return false;
            try
            {
                var doc = new IndexedTranslationService().BuildIndex(originalXml, candidateXml);
                return doc.Units.Any(u => !string.IsNullOrWhiteSpace(u.En));
            }
            catch { return true; }
        }
        catch { return false; }
    }

    private static bool Parses(string xml)
    {
        try { _ = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo); return true; }
        catch { return false; }
    }

    private static int Rank(TranslationStatus s) => s switch
    {
        TranslationStatus.Green => 2,
        TranslationStatus.Yellow => 1,
        _ => 0,
    };

    // =========================================================== single-source matrix

    public enum Kind { IdenticalBytes, DeepEqualCopy, StubNoEn, Partial, Full, Romanized }

    [Theory]
    // {canonical} × each kind, and {community-user} × each kind (same code path — the
    // evaluator is source-agnostic; the candidate is just a path).
    [InlineData(Kind.IdenticalBytes, TranslationStatus.Red)]
    [InlineData(Kind.DeepEqualCopy, TranslationStatus.Red)]
    [InlineData(Kind.StubNoEn, TranslationStatus.Red)]
    [InlineData(Kind.Partial, TranslationStatus.Yellow)]
    [InlineData(Kind.Full, TranslationStatus.Green)]
    [InlineData(Kind.Romanized, TranslationStatus.Red)]
    public void SingleSource_MatchesOldPipeline_AndExpectedStatus(Kind kind, TranslationStatus expected)
    {
        var (orig, tran) = MakePair(kind);

        var evaluator = NewEvaluator();

        // Backbone: identical to the old sweep for one candidate.
        Assert.Equal(OldEntryStatus(orig, new[] { tran }), evaluator.EvaluateEntry(orig, new[] { tran }));
        // Explicit semantic pin.
        Assert.Equal(expected, evaluator.EvaluateEntry(orig, new[] { tran }));
        // Per-candidate helper agrees with the single-candidate entry result.
        Assert.Equal(expected, evaluator.ComputeCandidateStatus(orig, tran));
    }

    private (string orig, string tran) MakePair(Kind kind)
    {
        var tag = kind.ToString();
        return kind switch
        {
            Kind.IdenticalBytes => (
                Write($"{tag}.orig.xml", Tei(BodyOrigCjk)),
                Write($"{tag}.tran.xml", Tei(BodyOrigCjk))),                 // byte-identical ⇒ Red
            Kind.DeepEqualCopy => (
                Write($"{tag}.orig.xml", Tei(BodyFullEn)),
                Write($"{tag}.tran.xml", TeiTagSpaced(BodyFullEn))),         // step-1 Green, DeepEqual ⇒ Red
            Kind.StubNoEn => (
                Write($"{tag}.orig.xml", Tei(BodyOrigCjk)),
                Write($"{tag}.tran.xml", Tei(BodyStubCjk))),                 // differs, Chinese-only ⇒ Red
            Kind.Partial => (
                Write($"{tag}.orig.xml", Tei(BodyOrigCjk)),
                Write($"{tag}.tran.xml", Tei(BodyPartial))),                 // CJK + EN ⇒ Yellow
            Kind.Full => (
                Write($"{tag}.orig.xml", Tei(BodyOrigCjk)),
                Write($"{tag}.tran.xml", Tei(BodyFullEn))),                  // EN only ⇒ Green
            Kind.Romanized => (
                Write($"{tag}.orig.xml", Tei(BodyOrigRomanized)),
                Write($"{tag}.tran.xml", TeiTagSpaced(BodyOrigRomanized))),  // false-Yellow, DeepEqual ⇒ Red
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    // =========================================================== multi-source (max) matrix

    [Fact]
    public void NoCandidates_IsRed()
    {
        var orig = Write("empty.orig.xml", Tei(BodyOrigCjk));
        var evaluator = NewEvaluator();
        Assert.Equal(TranslationStatus.Red, evaluator.EvaluateEntry(orig, Array.Empty<string>()));
        Assert.Equal(OldEntryStatus(orig, Array.Empty<string>()), evaluator.EvaluateEntry(orig, Array.Empty<string>()));
    }

    [Theory]
    // {canonical kind, community kind} ⇒ expected max — pins §3.2 "max status over candidates".
    [InlineData(Kind.StubNoEn, Kind.Full, TranslationStatus.Green)]     // Red + Green ⇒ Green
    [InlineData(Kind.Partial, Kind.Full, TranslationStatus.Green)]      // Yellow + Green ⇒ Green
    [InlineData(Kind.Full, Kind.StubNoEn, TranslationStatus.Green)]     // Green + Red ⇒ Green (order-independent)
    [InlineData(Kind.StubNoEn, Kind.Partial, TranslationStatus.Yellow)] // Red + Yellow ⇒ Yellow
    [InlineData(Kind.IdenticalBytes, Kind.StubNoEn, TranslationStatus.Red)] // Red + Red ⇒ Red
    public void BothSources_TakesMaxStatus_MatchesOldPipeline(Kind canonicalKind, Kind communityKind, TranslationStatus expected)
    {
        // Each candidate needs its OWN original (kinds define their orig too); the entry's
        // "original" is shared in production, but status is a pure (orig,tran) function, so
        // to isolate the max-semantics we evaluate each candidate against its own orig via
        // ComputeCandidateStatus and confirm EvaluateEntry over a shared orig matches the max.
        var origCjk = Write("multi.orig.xml", Tei(BodyOrigCjk));

        // Build candidate translated files whose status against origCjk is the intended kind.
        var canonicalTran = MakeCandidateAgainstCjkOrig(canonicalKind, "canonical");
        var communityTran = MakeCandidateAgainstCjkOrig(communityKind, "community");

        var evaluator = NewEvaluator();
        var candidates = new[] { canonicalTran, communityTran };

        Assert.Equal(expected, evaluator.EvaluateEntry(origCjk, candidates));
        Assert.Equal(OldEntryStatus(origCjk, candidates), evaluator.EvaluateEntry(origCjk, candidates));
    }

    // Produces a translated file whose ComputeCandidateStatus against Tei(BodyOrigCjk) is the
    // intended kind (Red/Yellow/Green). Uses only kinds whose verdict is independent of a
    // matching-original (no DeepEqual/romanization here — those need a bespoke orig).
    private string MakeCandidateAgainstCjkOrig(Kind kind, string label) => kind switch
    {
        Kind.IdenticalBytes => Write($"{label}.tran.xml", Tei(BodyOrigCjk)),  // == orig ⇒ Red
        Kind.StubNoEn => Write($"{label}.tran.xml", Tei(BodyStubCjk)),        // Chinese-only ⇒ Red
        Kind.Partial => Write($"{label}.tran.xml", Tei(BodyPartial)),         // Yellow
        Kind.Full => Write($"{label}.tran.xml", Tei(BodyFullEn)),             // Green
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "kind not valid against a CJK original"),
    };

    // =========================================================== demotion pins (§3.3)

    [Fact]
    public void IdenticalBytes_IsRed()
    {
        var orig = Write("id.orig.xml", Tei(BodyOrigCjk));
        var tran = Write("id.tran.xml", Tei(BodyOrigCjk));
        Assert.Equal(TranslationStatus.Red, NewEvaluator().ComputeCandidateStatus(orig, tran));
    }

    [Fact]
    public void DeepEqualNotByteEqual_DemotedToRed()
    {
        var orig = Write("deq.orig.xml", Tei(BodyFullEn));
        var tran = Write("deq.tran.xml", TeiTagSpaced(BodyFullEn));

        // Fixture sanity: genuinely different bytes, genuinely DeepEqual trees.
        Assert.NotEqual(File.ReadAllText(orig), File.ReadAllText(tran));
        Assert.True(XNode.DeepEquals(
            XDocument.Parse(File.ReadAllText(orig), LoadOptions.PreserveWhitespace),
            XDocument.Parse(File.ReadAllText(tran), LoadOptions.PreserveWhitespace)));

        Assert.Equal(TranslationStatus.Red, NewEvaluator().ComputeCandidateStatus(orig, tran));
    }

    [Fact]
    public void RomanizedOriginal_FalseYellow_DemotedToRed()
    {
        // The row that would false-Yellow if meaningfulness were skipped: step-1 body
        // analysis sees CJK + Latin romanization ⇒ Yellow; the DeepEqual demotion ⇒ Red.
        var orig = Write("rom.orig.xml", Tei(BodyOrigRomanized));
        var tranDeepEqual = Write("rom.tran.xml", TeiTagSpaced(BodyOrigRomanized));

        // Prove step-1 alone WOULD have said Yellow (both CJK and Latin present).
        var step1 = new TranslationStatusService()
            .ComputeStatusForPairLive(orig, tranDeepEqual, string.Empty, string.Empty, false);
        Assert.Equal(TranslationStatus.Yellow, step1);

        // The evaluator applies the demotion ⇒ Red.
        Assert.Equal(TranslationStatus.Red, NewEvaluator().ComputeCandidateStatus(orig, tranDeepEqual));
    }

    // =========================================================== stars assumption (§3.2)

    [Fact]
    public void StarsDoNotAffectStatus()
    {
        // Structural pin: stars are not even a constructor dependency, so they cannot be
        // a gate input. If a future change tries to feed a star service in, this trips.
        var ctorParams = typeof(NavStatusEvaluator).GetConstructors().Single().GetParameters();
        Assert.DoesNotContain(ctorParams, p =>
            p.ParameterType.Name.IndexOf("Star", StringComparison.OrdinalIgnoreCase) >= 0);

        // Behavioral pin: two equal-status (Green) candidates ⇒ Green, regardless of any
        // star weighting the old read-path pick would have applied among equal candidates.
        var orig = Write("stars.orig.xml", Tei(BodyOrigCjk));
        var greenA = Write("stars.canonical.xml", Tei(BodyFullEn));
        var greenB = Write("stars.community.xml", Tei(BodyFullEn));
        Assert.Equal(TranslationStatus.Green, NewEvaluator().EvaluateEntry(orig, new[] { greenA, greenB }));
    }

    // =========================================================== memo persistence (§3.3)

    /// <summary>
    /// The same (orig, tran-content) pair is DeepEquals/BuildIndex-analyzed at most once —
    /// the second evaluation is served from the memo. Pinned by spying on BuildIndex, which
    /// only the (non-DeepEqual) meaningfulness branch invokes.
    /// </summary>
    [Fact]
    public void SameOrigTranPair_NeverAnalyzesTwice()
    {
        var orig = Write("memo.orig.xml", Tei(BodyOrigCjk));
        var tran = Write("memo.tran.xml", Tei(BodyFullEn)); // Green ⇒ meaningfulness runs ⇒ BuildIndex called

        var spy = new CountingIndexedTranslationService();
        var evaluator = new NavStatusEvaluator(new TranslationStatusService(), spy);

        var first = evaluator.ComputeCandidateStatus(orig, tran);
        var second = evaluator.ComputeCandidateStatus(orig, tran);

        Assert.Equal(TranslationStatus.Green, first);
        Assert.Equal(TranslationStatus.Green, second);
        Assert.Equal(1, spy.BuildIndexCount); // second call hit the memo, no re-analysis
    }

    /// <summary>Wraps the real service so meaningfulness verdicts are genuine while every
    /// <c>BuildIndex</c> call is tallied.</summary>
    private sealed class CountingIndexedTranslationService : IIndexedTranslationService
    {
        private readonly IIndexedTranslationService _inner = new IndexedTranslationService();
        private int _buildIndexCount;
        public int BuildIndexCount => Volatile.Read(ref _buildIndexCount);

        public string LastBuildTranslatedXmlDebugDump => _inner.LastBuildTranslatedXmlDebugDump;
        public string LastBuildTranslatedXmlDebugDumpPath => _inner.LastBuildTranslatedXmlDebugDumpPath;
        public int LastBuildSkippedUnsafeGroupCount => _inner.LastBuildSkippedUnsafeGroupCount;
        public int LastBuildSkippedDirtyGroupCount => _inner.LastBuildSkippedDirtyGroupCount;

        public IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml, string? originalAbsPath = null)
        {
            Interlocked.Increment(ref _buildIndexCount);
            return _inner.BuildIndex(originalXml, translatedXml, originalAbsPath);
        }

        public string RenderProjection(IndexedTranslationDocument doc, TranslationEditMode mode)
            => _inner.RenderProjection(doc, mode);

        public string RenderMergedPreview(IndexedTranslationDocument doc, TranslationEditMode mode, SegmentMap segmentMap)
            => _inner.RenderMergedPreview(doc, mode, segmentMap);

        public void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText)
            => _inner.ApplyProjectionEdits(doc, mode, editedText);

        public string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount)
            => _inner.BuildTranslatedXml(doc, out updatedCount);
    }
}
