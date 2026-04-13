using System;

namespace ReadZen.App.Models;

/// <summary>
/// Records the resolution of a single disagreement between two coders:
/// which coder's judgment was accepted, by whom, and why.
/// </summary>
public sealed class ConsensusResolution
{
    public string Id { get; set; } = "";
    public string RelPath { get; set; } = "";
    public string FromLb { get; set; } = "";
    public string ToLb { get; set; } = "";
    public string TagId { get; set; } = "";
    public string AcceptedCoder { get; set; } = "";
    public string RejectedCoder { get; set; } = "";
    public string ResolvedBy { get; set; } = "";
    public DateTimeOffset ResolvedUtc { get; set; }
    public string? Reason { get; set; }
}
