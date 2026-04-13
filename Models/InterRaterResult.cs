using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>
/// Aggregate inter-rater reliability metrics for a single document,
/// comparing two coders' tag sets.
/// </summary>
public sealed class InterRaterResult
{
    public required string RelPath { get; init; }
    public required string Coder1 { get; init; }
    public required string Coder2 { get; init; }
    public required int TotalUnits { get; init; }
    public required double OverallPercentAgreement { get; init; }
    public required double OverallCohensKappa { get; init; }
    public required List<PerCodeAgreement> PerCode { get; init; }
}

/// <summary>
/// Per-tag-code agreement between two coders: 2x2 contingency table + derived metrics.
/// </summary>
public sealed class PerCodeAgreement
{
    public required string TagId { get; init; }
    public required string TagName { get; init; }
    public required int BothPresent { get; init; }
    public required int OnlyCoder1 { get; init; }
    public required int OnlyCoder2 { get; init; }
    public required int NeitherPresent { get; init; }
    public required double PercentAgreement { get; init; }
    public required double CohensKappa { get; init; }
}
