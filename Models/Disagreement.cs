namespace ReadZen.App.Models;

/// <summary>
/// A single point of disagreement between two coders: one tagged a unit, the other did not.
/// </summary>
public sealed class Disagreement
{
    public required string FromLb { get; init; }
    public required string ToLb { get; init; }
    public required string TagId { get; init; }
    public required string TagName { get; init; }
    public required bool Coder1HasIt { get; init; }
    public required bool Coder2HasIt { get; init; }
}
