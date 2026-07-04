using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class TranslationAssistantService : ITranslationAssistantService
{
    // Injected from the container (audit R3-M1). These used to be `new()`d here, so
    // the assistant held PRIVATE TM/termbase/QA instances separate from the ones the
    // rest of the app uses - two cache universes, and SetUsername/cache invalidation
    // through the container never reached the assistant's copies. Sharing the
    // registered singletons fixes that.
    private readonly ITranslationMemoryService _tm;
    private readonly ITermbaseService _terms;
    private readonly ITranslationQaService _qa;

    public TranslationAssistantService(
        ITranslationMemoryService tm,
        ITermbaseService terms,
        ITranslationQaService qa)
    {
        _tm = tm;
        _terms = terms;
        _qa = qa;
    }

    /// <summary>
    /// Sets the current username so the termbase service resolves the per-user file.
    /// </summary>
    public void SetUsername(string? username) => _terms.SetUsername(username);

    /// <inheritdoc />
    public async Task WarmupCacheAsync(string root, CancellationToken ct = default)
    {
        await _tm.WarmupCacheAsync(root, ct).ConfigureAwait(false);
        await _terms.WarmupCacheAsync(root, ct).ConfigureAwait(false);
    }

    public async Task<TranslationAssistantSnapshot> BuildSnapshotAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? originalDir,
        string? translatedDir,
        CancellationToken ct = default,
        int maxResults = 8)
    {
        var approvedTask = _tm.FindApprovedMatchesAsync(ctx, root, translatedDir, ct, maxResults);
        var referenceTask = _tm.FindReferenceMatchesAsync(ctx, root, translatedDir, ct, maxResults);
        var termsTask = _terms.FindTermsAsync(ctx, root, ct);
        var communityTermsTask = _terms.FindCommunityTermsAsync(ctx, root, ct);
        await Task.WhenAll(approvedTask, referenceTask, termsTask, communityTermsTask).ConfigureAwait(false);
        var approved = approvedTask.Result;
        var reference = referenceTask.Result;
        var terms = termsTask.Result;
        var communityTerms = communityTermsTask.Result;

        // Merge: personal terms first, then community (dedup by SourceTerm)
        var personalSourceTerms = new System.Collections.Generic.HashSet<string>(
            terms.Select(t => t.SourceTerm), System.StringComparer.Ordinal);
        terms.AddRange(communityTerms.Where(ct2 => !personalSourceTerms.Contains(ct2.SourceTerm)));

        var qa = _qa.Check(ctx, terms);

        return new TranslationAssistantSnapshot
        {
            Segment = ctx,
            ApprovedMatches = approved,
            ReferenceMatches = reference,
            Terms = terms,
            QaIssues = qa
        };
    }
}