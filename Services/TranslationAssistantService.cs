using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationAssistantService : ITranslationAssistantService
{
    private readonly TranslationMemoryService _tm = new();
    private readonly TermbaseService _terms = new();
    private readonly TranslationQaService _qa = new();

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
        await Task.WhenAll(approvedTask, referenceTask, termsTask).ConfigureAwait(false);
        var approved = approvedTask.Result;
        var reference = referenceTask.Result;
        var terms = termsTask.Result;
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