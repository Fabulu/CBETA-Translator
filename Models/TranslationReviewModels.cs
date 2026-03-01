using System;

namespace CbetaTranslator.App.Models;

public static class TranslationReviewStatuses
{
    public const string Approved = "approved";
    public const string NeedsWork = "needs-work";
    public const string Rejected = "rejected";

    public static string Normalize(string? status)
    {
        var s = (status ?? "").Trim().ToLowerInvariant();

        return s switch
        {
            Approved => Approved,
            NeedsWork => NeedsWork,
            Rejected => Rejected,
            _ => NeedsWork
        };
    }
}

public sealed class TranslationReviewEntry
{
    public string SegmentKey { get; set; } = "";
    public string RelPath { get; set; } = "";
    public string TextId { get; set; } = "";
    public string Mode { get; set; } = "";
    public int BlockNumber { get; set; }

    public string ZhText { get; set; } = "";
    public string EnText { get; set; } = "";

    public string Status { get; set; } = TranslationReviewStatuses.NeedsWork;
    public string Reviewer { get; set; } = "User";
    public string Comment { get; set; } = "";
    public DateTime ReviewedUtc { get; set; } = DateTime.UtcNow;

    public string ZhHash { get; set; } = "";
    public string EnHash { get; set; } = "";
}