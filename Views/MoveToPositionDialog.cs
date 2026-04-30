using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace ReadZen.App.Views;

/// <summary>
/// Small inline dialog that lets the user pick a target position (1-based)
/// for reordering a passage within a collection.
/// </summary>
public class MoveToPositionDialog : Window
{
    private readonly NumericUpDown _numPosition;
    private readonly int _currentPosition;

    public MoveToPositionDialog(int currentPosition, int total)
    {
        _currentPosition = currentPosition;

        Title = "Move to Position";
        Width = 300;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        _numPosition = new NumericUpDown
        {
            Minimum = 1,
            Maximum = total,
            Value = currentPosition,
            FormatString = "0",
            Increment = 1
        };

        var label = new TextBlock { Text = $"Current: {currentPosition} of {total}" };

        var btnOk = new Button { Content = "OK", Width = 80 };
        var btnCancel = new Button { Content = "Cancel", Width = 80 };

        btnOk.Click += (_, _) => Close((int?)(int)(_numPosition.Value ?? _currentPosition));
        btnCancel.Click += (_, _) => Close((int?)null);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        buttonPanel.Children.Add(btnOk);
        buttonPanel.Children.Add(btnCancel);

        var root = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 8
        };
        root.Children.Add(label);
        root.Children.Add(_numPosition);
        root.Children.Add(buttonPanel);

        Content = root;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { Close((int?)null); e.Handled = true; }
            if (e.Key == Key.Return) { Close((int?)(int)(_numPosition.Value ?? _currentPosition)); e.Handled = true; }
        };

        Opened += (_, _) => _numPosition.Focus();
    }
}
