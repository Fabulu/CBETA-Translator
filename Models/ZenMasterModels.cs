using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Models;

public sealed class ZenMasterVariant
{
    public List<string> Names { get; init; } = new();
    public int Floruit { get; init; }
    public int Death { get; init; }
    public bool IsBase { get; init; }
    public string? Username { get; init; }

    // Biographical expansion
    public string? Notes { get; init; }
    public string? School { get; init; }
    public string? Teacher { get; init; }
    public List<string>? Students { get; init; }
    public string? Region { get; init; }
    public string? ReferenceUrl { get; init; }

    public string PrimaryName => Names.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "(unnamed)";

    public string DatesSummary => Floruit > 0 && Death > 0
        ? $"{Floruit}-{Death}"
        : Floruit > 0
            ? $"fl. {Floruit}"
            : Death > 0
                ? $"d. {Death}"
                : "date unknown";

    public string SourceSummary => IsBase
        ? "Bundled"
        : string.IsNullOrWhiteSpace(Username)
            ? "Community"
            : $"Community: {Username}";
}

public sealed class ZenMasterRecord
{
    public string CanonicalName { get; set; } = "";
    public List<string> Aliases { get; set; } = new();
    public List<ZenMasterVariant> Variants { get; set; } = new();

    public bool HasBase => Variants.Any(v => v.IsBase);
    public int CommunityVariantCount => Variants.Count(v => !v.IsBase);

    public ZenMasterVariant? PrimaryVariant => GetPreferredVariant(null);

    public IReadOnlyList<string> CommunityUsers => Variants
        .Where(v => !v.IsBase && !string.IsNullOrWhiteSpace(v.Username))
        .Select(v => v.Username!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public string DatesSummary => PrimaryVariant?.DatesSummary ?? "date unknown";

    /// <summary>Best available notes (prefers base, falls back to community).</summary>
    public string? Notes => Variants.Select(v => v.Notes).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

    /// <summary>Best available school.</summary>
    public string? School => Variants.Select(v => v.School).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

    /// <summary>Best available teacher name/ID.</summary>
    public string? Teacher => Variants.Select(v => v.Teacher).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

    /// <summary>Merged students list from all variants.</summary>
    public List<string> Students => Variants
        .Where(v => v.Students != null)
        .SelectMany(v => v.Students!)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>Best available region.</summary>
    public string? Region => Variants.Select(v => v.Region).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));

    /// <summary>Best available reference URL.</summary>
    public string? ReferenceUrl => Variants.Select(v => v.ReferenceUrl).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u));

    public string SourceSummary
    {
        get
        {
            if (HasBase && CommunityVariantCount > 0)
                return $"Bundled + {CommunityVariantCount} community";
            if (HasBase)
                return "Bundled";
            return CommunityVariantCount > 0 ? $"{CommunityVariantCount} community" : "Unknown";
        }
    }

    public ZenMasterVariant? GetPreferredVariant(string? preferredUser)
    {
        if (!string.IsNullOrWhiteSpace(preferredUser))
        {
            var userVariant = Variants.FirstOrDefault(v =>
                !v.IsBase &&
                !string.IsNullOrWhiteSpace(v.Username) &&
                string.Equals(v.Username, preferredUser.Trim(), StringComparison.OrdinalIgnoreCase));
            if (userVariant != null)
                return userVariant;
        }

        return Variants.FirstOrDefault(v => v.IsBase)
            ?? Variants.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Username))
            ?? Variants.FirstOrDefault();
    }

    public bool MatchesFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        var terms = filter.Trim()
            .Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (terms.Length == 0)
            return true;

        var haystacks = Aliases
            .Concat(Variants.SelectMany(v => v.Names))
            .Concat(CommunityUsers)
            .Append(CanonicalName)
            .Append(DatesSummary)
            .Append(SourceSummary)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return terms.All(term => haystacks.Any(h => h.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed class ZenMasterCatalog
{
    public List<ZenMasterRecord> Records { get; init; } = new();

    public int BundledRecordCount => Records.Count(r => r.HasBase);

    public int CommunityOnlyRecordCount => Records.Count(r => !r.HasBase);

    public int CommunityVariantCount => Records.Sum(r => r.CommunityVariantCount);

    public string SummaryText
    {
        get
        {
            if (Records.Count == 0)
                return "No Zen masters found.";

            var parts = new List<string>
            {
                $"Loaded {Records.Count:n0} Zen master(s)"
            };

            if (BundledRecordCount > 0)
                parts.Add($"{BundledRecordCount:n0} bundled");
            if (CommunityOnlyRecordCount > 0)
                parts.Add($"{CommunityOnlyRecordCount:n0} community-only");
            if (CommunityVariantCount > 0)
                parts.Add($"{CommunityVariantCount:n0} community variant(s)");

            return string.Join("; ", parts) + ".";
        }
    }
}

public sealed class ZenMasterLandingMatch
{
    public required ZenMasterRecord Record { get; init; }
    public required ZenMasterVariant Variant { get; init; }
    public bool UsedPreferredUser { get; init; }
}
