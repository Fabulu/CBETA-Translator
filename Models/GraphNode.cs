namespace ReadZen.App.Models;

public class GraphNode
{
    public string PassageId { get; set; } = "";
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }
    public bool IsSelected { get; set; }
}
