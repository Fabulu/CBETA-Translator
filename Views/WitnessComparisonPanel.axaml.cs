// Views/WitnessComparisonPanel.axaml.cs
// Witness comparison popup/panel showing readings at a locus.
// Critical reading at top, differing witnesses beneath, copy support.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

public partial class WitnessComparisonPanel : UserControl
{
    private List<WitnessReadingGroup> _allGroups = new();
    private string? _locusId;
    private string? _lemma;

    /// <summary>Fired when the user clicks "Open full text" on a witness siglum.</summary>
    public event EventHandler<WitnessTextEntry>? OpenWitnessFullTextRequested;

    /// <summary>Fired when the user clicks the page-image button on a witness siglum.</summary>
    public event EventHandler<(string WitnessId, string Locus)>? OpenWitnessPageImageRequested;

    public WitnessComparisonPanel()
    {
        InitializeComponent();

        var btnCopy = this.FindControl<Button>("BtnCopyAll");
        if (btnCopy != null) btnCopy.Click += async (_, _) =>
        {
            var text = BuildCopyText(showAll: true);
            var top = TopLevel.GetTopLevel(this);
            if (top?.Clipboard != null && !string.IsNullOrEmpty(text))
                await top.Clipboard.SetTextAsync(text);
        };

        var chkShowAll = this.FindControl<CheckBox>("ChkShowAll");
        if (chkShowAll != null) chkShowAll.IsCheckedChanged += (_, _) => RenderReadings(chkShowAll.IsChecked == true);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Populates the panel with comparison data for a specific locus.
    /// </summary>
    public void SetComparison(string locusId, string? lemma, List<WitnessReadingGroup> groups)
    {
        _locusId = locusId;
        _lemma = lemma;
        _allGroups = groups;

        var txtLocus = this.FindControl<TextBlock>("TxtLocusId");
        if (txtLocus != null) txtLocus.Text = $"Locus: {locusId}";

        var txtLemma = this.FindControl<SelectableTextBlock>("TxtLemma");
        if (txtLemma != null) txtLemma.Text = lemma ?? "(no adopted reading)";

        var txtEmpty = this.FindControl<TextBlock>("TxtEmpty");
        if (txtEmpty != null) txtEmpty.IsVisible = groups.Count == 0;

        RenderReadings(showAll: false);
    }

    private void RenderReadings(bool showAll)
    {
        var host = this.FindControl<StackPanel>("ReadingsHost");
        if (host == null) return;
        host.Children.Clear();

        foreach (var group in _allGroups)
        {
            // Skip lemma-agreeing witnesses unless showAll is checked
            if (group.IsLemma && !showAll) continue;

            var card = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = group.IsLemma
                    ? new SolidColorBrush(Color.FromArgb(40, 0, 180, 0))
                    : new SolidColorBrush(Color.FromArgb(60, 255, 180, 0)),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 2),
            };

            var stack = new StackPanel { Spacing = 3 };

            // Reading text (copyable)
            var readingPanel = new DockPanel();
            readingPanel.Children.Add(new SelectableTextBlock
            {
                Text = group.Reading,
                FontSize = 12,
                FontWeight = group.IsLemma ? Avalonia.Media.FontWeight.Normal : Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });

            // Copy button for this reading
            var copyBtn = new Button
            {
                Content = "Copy",
                FontSize = 9,
                Padding = new Thickness(4, 1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
            };
            DockPanel.SetDock(copyBtn, Dock.Right);
            copyBtn.Click += async (_, _) =>
            {
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null) await top.Clipboard.SetTextAsync(group.Reading);
            };
            readingPanel.Children.Insert(0, copyBtn);

            stack.Children.Add(readingPanel);

            // Witness sigla line — clickable per-siglum buttons that open the full witness text
            var siglaPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            siglaPanel.Children.Add(new TextBlock
            {
                Text = "Witnesses: ",
                FontSize = 10, Opacity = 0.7,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
            });
            foreach (var w in group.Witnesses)
            {
                var siglumLabel = w.Siglum ?? w.WitnessId ?? "?";
                var extras = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(w.Confidence) && w.Confidence != "high")
                    extras.Append($" [{w.Confidence}]");
                if (w.HasOcr && !w.HasHumanCheck)
                    extras.Append(" (OCR)");

                var siglumBtn = new Button
                {
                    Content = siglumLabel + extras.ToString(),
                    FontSize = 10,
                    Padding = new Thickness(5, 1),
                    Margin = new Thickness(0, 0, 4, 2),
                    Background = Avalonia.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Tag = w,
                };
                ToolTip.SetTip(siglumBtn, $"Open full witness text for {w.Label ?? siglumLabel}");
                siglumBtn.Click += (_, _) =>
                {
                    if (siglumBtn.Tag is WitnessTextEntry witness)
                        OpenWitnessFullTextRequested?.Invoke(this, witness);
                };
                siglaPanel.Children.Add(siglumBtn);

                // Page-image button
                var capturedSiglum = siglumLabel;
                var pageBtn = new Button
                {
                    Content = "\U0001F4C4",
                    FontSize = 10,
                    Padding = new Thickness(3, 1),
                    Margin = new Thickness(0, 0, 6, 2),
                    Background = Avalonia.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                };
                ToolTip.SetTip(pageBtn, $"View page image for {capturedSiglum}");
                pageBtn.Click += (_, _) =>
                {
                    if (_locusId != null)
                        OpenWitnessPageImageRequested?.Invoke(this, (capturedSiglum, _locusId));
                };
                siglaPanel.Children.Add(pageBtn);
            }
            stack.Children.Add(siglaPanel);

            // Label if this is the adopted reading
            if (group.IsLemma)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "\u2713 Adopted reading",
                    FontSize = 9,
                    Opacity = 0.5,
                    FontStyle = FontStyle.Italic,
                });
            }

            card.Child = stack;
            host.Children.Add(card);
        }
    }

    private string BuildCopyText(bool showAll)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Locus: {_locusId}");
        sb.AppendLine($"Adopted: {_lemma}");
        sb.AppendLine();

        foreach (var group in _allGroups)
        {
            if (!showAll && group.IsLemma) continue;

            var sigla = string.Join(", ", group.Witnesses.Select(w => w.Siglum ?? w.WitnessId ?? "?"));
            sb.AppendLine($"{sigla}: {group.Reading}");
        }

        return sb.ToString().TrimEnd();
    }
}
