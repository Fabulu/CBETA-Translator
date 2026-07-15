using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ReadZen.App.Views;

/// <summary>
/// Thin code-behind for the lineage detail side-panel (plan PR-L6). All content
/// is driven by compiled bindings against a <see cref="ViewModels.LineageChartViewModel"/>
/// (set as this control's DataContext by the host window); there is no logic here.
/// </summary>
public partial class LineageDetailPanel : UserControl
{
    public LineageDetailPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
