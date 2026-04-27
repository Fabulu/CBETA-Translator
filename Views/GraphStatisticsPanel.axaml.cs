using Avalonia.Controls;
using Avalonia.Media;

namespace ReadZen.App.Views;

public partial class GraphStatisticsPanel : UserControl
{
    public GraphStatisticsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the statistics display with the given values.
    /// Call this after any graph change that affects quality metrics.
    /// </summary>
    public void UpdateStats(int orphanPassages, int orphanConcepts, int overloadedConcepts, int weakConcepts, double qualityScore)
    {
        TxtOrphans.Text = $"Orphan passages: {orphanPassages}";
        TxtOrphanConcepts.Text = $"Orphan concepts: {orphanConcepts}";
        TxtOverloaded.Text = $"Overloaded concepts: {overloadedConcepts}";
        TxtWeak.Text = $"Weak concepts: {weakConcepts}";

        QualityBar.Value = qualityScore;
        TxtQuality.Text = $"Quality: {qualityScore:F0}%";

        // Color the bar based on score
        if (qualityScore >= 80)
            QualityBar.Foreground = new SolidColorBrush(Color.Parse("#4CAF50"));
        else if (qualityScore >= 50)
            QualityBar.Foreground = new SolidColorBrush(Color.Parse("#FFC107"));
        else
            QualityBar.Foreground = new SolidColorBrush(Color.Parse("#F44336"));
    }
}
