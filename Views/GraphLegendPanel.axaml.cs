using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace ReadZen.App.Views;

/// <summary>
/// Represents a single edge type entry for the legend display.
/// </summary>
public record EdgeLegendEntry(string RelationType, string ColorHex, string Label);

public partial class GraphLegendPanel : UserControl
{
    public GraphLegendPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the edge legend section with the given edge types.
    /// Pass distinct edge types currently present in the graph.
    /// </summary>
    public void UpdateLegend(IEnumerable<EdgeLegendEntry> edgeTypes)
    {
        EdgeLegendPanel.Children.Clear();

        var sorted = edgeTypes.OrderBy(e => e.Label).ToList();

        foreach (var et in sorted)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            var swatch = new Border
            {
                Width = 20,
                Height = 4,
                Background = new SolidColorBrush(Color.Parse(et.ColorHex)),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new Avalonia.CornerRadius(2)
            };

            var label = new TextBlock
            {
                Text = et.Label,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };

            row.Children.Add(swatch);
            row.Children.Add(label);
            EdgeLegendPanel.Children.Add(row);
        }

        if (sorted.Count == 0)
        {
            EdgeLegendPanel.Children.Add(new TextBlock
            {
                Text = "No edges in graph",
                FontSize = 10,
                Opacity = 0.5
            });
        }
    }
}
