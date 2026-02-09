namespace CbetaTranslator.App.Models;

public enum NavStatus
{
    Red = 0,
    Yellow = 1,
    Green = 2
}

public sealed class NavItem
{
    public string RelPath { get; init; } = "";
    public string DisplayShort { get; init; } = "";
    public string Tooltip { get; init; } = "";
    public NavStatus Status { get; init; } = NavStatus.Red;

    // Optional: makes debugging nicer if template breaks
    public override string ToString() => DisplayShort.Length > 0 ? DisplayShort : RelPath;
}
