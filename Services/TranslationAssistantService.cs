using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationAssistantService : ITranslationAssistantService
{
    private readonly TranslationMemoryService _tm = new();
    private readonly TermbaseService _terms = new();
    private readonly TranslationQaService _qa = new();

    /// <summary>
    /// Sets the current username so the termbase service resolves the per-user file.
    /// </summary>
    public void SetUsername(string? username) => _terms.SetUsername(username);

    public async Task<TranslationAssistantSnapshot> BuildSnapshotAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? originalDir,
        string? translatedDir,
        CancellationToken ct = default)
    {
        var approvedTask = _tm.FindApprovedMatchesAsync(ctx, root, translatedDir, ct);
        var referenceTask = _tm.FindReferenceMatchesAsync(ctx, root, translatedDir, ct);
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