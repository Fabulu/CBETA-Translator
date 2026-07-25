// Views/PoppedOutPlaceholder.cs
//
// The stand-in shown in a tab's Carousel slot while its real content is popped out
// into a FloatingTabWindow (POPOUT_TABS_DESIGN §4.3). Selecting the tab does NOT
// auto-restore — using another tab while the float stays open is the whole point — so
// this offers two explicit actions: bring the float to front, or dock it back here.
//
// Code-only (no XAML) and theme-aware: it lives inside MainWindow's visual tree, so
// DynamicResource bindings restyle it on light/dark switches like the rest of the shell.

using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace ReadZen.App.Views;

public sealed class PoppedOutPlaceholder : UserControl
{
    public PoppedOutPlaceholder(string title, Action onBringToFront, Action onDockBack)
    {
        var heading = new TextBlock
        {
            Text = $"{title} is open in a separate window",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        heading[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("TextFg");

        var subtitle = new TextBlock
        {
            Text = "Keep working in other tabs — this pop-out stays live and independent.",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 360,
        };
        subtitle.Classes.Add("muted");

        var bringToFront = new Button
        {
            Content = "Bring to front",
            Padding = new Avalonia.Thickness(14, 6),
        };
        bringToFront.Click += (_, _) => onBringToFront();

        var dockBack = new Button
        {
            Content = "Dock back here",
            Padding = new Avalonia.Thickness(14, 6),
        };
        dockBack.Click += (_, _) => onDockBack();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        buttons.Children.Add(bringToFront);
        buttons.Children.Add(dockBack);

        var stack = new StackPanel
        {
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        stack.Children.Add(heading);
        stack.Children.Add(subtitle);
        stack.Children.Add(buttons);

        var root = new Border
        {
            Padding = new Avalonia.Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = stack,
        };
        root[!Border.BackgroundProperty] = new DynamicResourceExtension("AppBg");

        Content = root;
    }
}
