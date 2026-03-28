namespace CbetaTranslator.App.Models;

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
}
