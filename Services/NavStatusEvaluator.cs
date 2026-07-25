using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// PR-NV1 (NAV_CACHE_REDESIGN §3.2, §3.3): the single source of truth for a nav
/// entry's translation status. Extracted from the two rival status pipelines in
/// <see cref="ReadZen.App.ViewModels.MainWindowViewModel"/> so that the cache-build
/// path, the launch sweep, and the per-file save path can all agree on ONE verdict.
///
/// In PR-NV1 this service is purely additive: only the meaningfulness core
/// (<see cref="IsMeaningfullyTranslated"/>) is wired into production (the MWVM
/// delegate). <see cref="ComputeCandidateStatus"/> and <see cref="EvaluateEntry"/>
/// exist so the parity matrix can pin their semantics against the still-running
/// <c>EvaluateBestTranslationSource</c> sweep before that sweep is retired in PR-NV4.
///
/// Semantics preserved exactly from MWVM:
///   - per-candidate status = <see cref="ITranslationStatusService.ComputeStatusForPairLive"/>
///     (missing/byte-identical ⇒ Red; else CJK/Latin body analysis ⇒ Green/Yellow/Red),
///     THEN a meaningfulness demotion: a Yellow/Green candidate that XML-DeepEquals its
///     original, or whose indexed document has no non-empty English unit, is demoted to Red.
///   - entry status = max over candidates by the Green &gt; Yellow &gt; Red rank
///     (mirrors <c>GetTranslationStatusRank</c>); Red when there are no candidates.
///
/// Stars, community-vs-personal preference, and mtime are NOT inputs here: in the old
/// pipeline they only tie-break the read-PATH pick among EQUAL-status candidates, never
/// the displayed status. That assumption is pinned by <c>StarsDoNotAffectStatus</c>.
/// </summary>
public interface INavStatusEvaluator
{
    /// <summary>
    /// Status of a single (original, translated) file pair with the meaningfulness
    /// demotion folded in. Missing translated file ⇒ Red.
    /// </summary>
    TranslationStatus ComputeCandidateStatus(string origAbs, string tranAbs);

    /// <summary>
    /// The displayed nav status for one original: the max <see cref="ComputeCandidateStatus"/>
    /// over every candidate translated file, by the Green &gt; Yellow &gt; Red rank.
    /// Red when the candidate list is empty/null.
    /// </summary>
    TranslationStatus EvaluateEntry(string origAbs, IReadOnlyList<string> candidateTranAbsPaths);

    /// <summary>
    /// The meaningfulness core: true iff the translated file is a genuine translation of
    /// the original (not an XML-DeepEqual copy, and its indexed document carries at least
    /// one non-empty English unit). Memoized per translated path by (orig mtime, tran mtime,
    /// tran length) — a given (orig, tran-content) pair is DeepEquals-parsed at most once.
    /// </summary>
    bool IsMeaningfullyTranslated(string origAbs, string tranAbs);

    /// <summary>Drops the meaningfulness memo (called by MWVM on corpus/root change).</summary>
    void ClearCache();
}

/// <inheritdoc cref="INavStatusEvaluator"/>
public sealed class NavStatusEvaluator : INavStatusEvaluator
{
    private readonly ITranslationStatusService _statusService;
    private readonly IIndexedTranslationService _indexedTranslation;

    // Process-lifetime memo, keyed by absolute translated path and validated by the
    // (orig mtime, tran mtime, tran length) triple — same invalidation the MWVM
    // _meaningfulTranslationCache used, now thread-safe (the launch sweep runs it on a
    // background thread while the UI thread calls it via the source dropdown).
    private readonly ConcurrentDictionary<string, MeaningfulCacheEntry> _meaningfulCache =
        new(StringComparer.OrdinalIgnoreCase);

    public NavStatusEvaluator(ITranslationStatusService statusService, IIndexedTranslationService indexedTranslation)
    {
        _statusService = statusService;
        _indexedTranslation = indexedTranslation;
    }

    public TranslationStatus ComputeCandidateStatus(string origAbs, string tranAbs)
    {
        // Step 1 — ComputeStatus (root/relKey args are log-only; verboseLog:false).
        var status = _statusService.ComputeStatusForPairLive(
            origAbs, tranAbs, string.Empty, string.Empty, verboseLog: false);

        // Step 2 — meaningfulness demotion applies only to a Yellow/Green verdict.
        // (Red is already terminal, so skipping the expensive check when Red is both
        // faster and identical to the old "meaningful ? ComputeStatus : Red" ordering.)
        if (status is TranslationStatus.Yellow or TranslationStatus.Green
            && !IsMeaningfullyTranslated(origAbs, tranAbs))
        {
            return TranslationStatus.Red;
        }

        return status;
    }

    public TranslationStatus EvaluateEntry(string origAbs, IReadOnlyList<string> candidateTranAbsPaths)
    {
        var best = TranslationStatus.Red;
        if (candidateTranAbsPaths == null)
            return best;

        foreach (var tranAbs in candidateTranAbsPaths)
        {
            if (string.IsNullOrWhiteSpace(tranAbs))
                continue;

            var status = ComputeCandidateStatus(origAbs, tranAbs);
            if (Rank(status) > Rank(best))
                best = status;
        }

        return best;
    }

    public bool IsMeaningfullyTranslated(string origAbs, string tranAbs)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(origAbs) || string.IsNullOrWhiteSpace(tranAbs))
                return false;

            if (!File.Exists(origAbs) || !File.Exists(tranAbs))
                return false;

            var originalWriteUtc = File.GetLastWriteTimeUtc(origAbs);
            var candidateInfo = new FileInfo(tranAbs);
            var candidateWriteUtc = candidateInfo.LastWriteTimeUtc;
            var candidateLength = candidateInfo.Length;

            if (_meaningfulCache.TryGetValue(tranAbs, out var cached)
                && cached.OriginalWriteUtc == originalWriteUtc
                && cached.CandidateWriteUtc == candidateWriteUtc
                && cached.CandidateLength == candidateLength)
            {
                return cached.IsMeaningful;
            }

            var originalXml = File.ReadAllText(origAbs, Encoding.UTF8);
            var candidateXml = File.ReadAllText(tranAbs, Encoding.UTF8);

            bool isMeaningful;
            if (TryParseXml(originalXml)
                && TryParseXml(candidateXml)
                && XNode.DeepEquals(
                    XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace),
                    XDocument.Parse(candidateXml, LoadOptions.PreserveWhitespace)))
            {
                // Translated file is structurally identical to the original ⇒ not translated.
                isMeaningful = false;
            }
            else
            {
                try
                {
                    var doc = _indexedTranslation.BuildIndex(originalXml, candidateXml);
                    isMeaningful = doc.Units.Any(u => !string.IsNullOrWhiteSpace(u.En));
                }
                catch
                {
                    // A parse/index failure is treated as "meaningful" (fail open) — the
                    // same conservative choice the MWVM original made.
                    isMeaningful = true;
                }
            }

            if (_meaningfulCache.Count > 5000)
                _meaningfulCache.Clear();
            _meaningfulCache[tranAbs] = new MeaningfulCacheEntry(
                originalWriteUtc,
                candidateWriteUtc,
                candidateLength,
                isMeaningful);

            return isMeaningful;
        }
        catch
        {
            return false;
        }
    }

    public void ClearCache() => _meaningfulCache.Clear();

    // Green > Yellow > Red — mirrors MainWindowViewModel.GetTranslationStatusRank.
    private static int Rank(TranslationStatus status) => status switch
    {
        TranslationStatus.Green => 2,
        TranslationStatus.Yellow => 1,
        _ => 0,
    };

    // Mirrors MainWindowViewModel.TryParseXml (same LoadOptions), boolean-only here.
    private static bool TryParseXml(string xml)
    {
        try
        {
            _ = XDocument.Parse(xml ?? string.Empty, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record MeaningfulCacheEntry(
        DateTime OriginalWriteUtc,
        DateTime CandidateWriteUtc,
        long CandidateLength,
        bool IsMeaningful);
}
