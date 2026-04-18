namespace ReadZen.App.Models;

public enum TourStepType { Passive, Active, Wait }
public enum TourPlacement { Top, Bottom, Left, Right, Center }

public sealed class TourStep
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? TargetControlName { get; set; }
    public TourStepType Type { get; set; } = TourStepType.Passive;
    public TourPlacement Placement { get; set; } = TourPlacement.Bottom;
    public int? SwitchToTabIndex { get; set; }
    public string? WaitForEvent { get; set; }

    /// <summary>If set, shows an action button in the tour tooltip (e.g., "Sync Now").</summary>
    public string? ActionButtonLabel { get; set; }

    /// <summary>If true, shows a "Skip" link in the tour tooltip for Wait-type steps.</summary>
    public bool CanSkipWait { get; set; }

    /// <summary>If set, auto-opens this file (relative path) when the step activates.</summary>
    public string? AutoOpenRelPath { get; set; }

    /// <summary>If set, jumps the translation editor to this block number (1-based) when the step activates.</summary>
    public int? AutoJumpToBlock { get; set; }

    /// <summary>
    /// Mandatory steps (initial setup: welcome, git check, download, index build) cannot be skipped —
    /// the app needs them to function. Skip button is hidden for these in the tooltip.
    /// </summary>
    public bool IsMandatory { get; set; }
}
