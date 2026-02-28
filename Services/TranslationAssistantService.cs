using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationAssistantService
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
        var approved = await _tm.FindApprovedMatchesAsync(ctx, root, translatedDir, ct);
        var reference = await _tm.FindReferenceMatchesAsync(ctx, root, translatedDir, ct);
        var terms = await _terms.FindTermsAsync(ctx, root, ct);
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