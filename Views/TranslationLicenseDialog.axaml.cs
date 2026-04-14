// Views/TranslationLicenseDialog.axaml.cs
// License picker dialog for translations. Shows human-readable descriptions
// of all compatible licenses and lets the user choose.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class TranslationLicenseDialog : Window
{
    private LicenseOption? _selected;
    private readonly List<Border> _optionCards = new();

    /// <summary>The license the user chose, or null if they cancelled / chose "later".</summary>
    public LicenseOption? ChosenLicense { get; private set; }

    public TranslationLicenseDialog()
    {
        InitializeComponent();

        var btnChoose = this.FindControl<Button>("BtnChoose")!;
        var btnLater = this.FindControl<Button>("BtnDecideLater")!;

        btnChoose.Click += (_, _) =>
        {
            ChosenLicense = _selected;
            Close();
        };

        btnLater.Click += (_, _) =>
        {
            ChosenLicense = null;
            Close();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Populates the dialog with compatible license options.
    /// </summary>
    public void LoadOptions(
        string? sourceLicenseDisplay,
        CorpusKind corpus,
        List<LicenseOption> compatibleLicenses,
        string? currentLicenseId = null)
    {
        var txtSource = this.FindControl<TextBlock>("TxtSourceInfo");
        if (txtSource != null)
        {
            if (corpus == CorpusKind.Cbeta)
                txtSource.Text = "Source: CBETA (non-commercial). Your translation must also be non-commercial.";
            else if (!string.IsNullOrWhiteSpace(sourceLicenseDisplay))
                txtSource.Text = $"Source license: {sourceLicenseDisplay}";
            else
                txtSource.Text = "Source license: Unknown";
        }

        var host = this.FindControl<StackPanel>("OptionsHost");
        if (host == null) return;

        var isLocked = compatibleLicenses.Count == 1 ||
            (compatibleLicenses.Count == 2 && compatibleLicenses.Exists(l => l.Id == "all-rights-reserved"));

        if (isLocked && compatibleLicenses.Count > 0)
        {
            var lockNote = compatibleLicenses[0].Id == "all-rights-reserved"
                ? compatibleLicenses.Count > 1 ? compatibleLicenses[1] : compatibleLicenses[0]
                : compatibleLicenses[0];

            host.Children.Add(new TextBlock
            {
                Text = $"The source uses a sticky license, so your translation must use: {lockNote.DisplayName}",
                FontSize = 11,
                Opacity = 0.7,
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        foreach (var opt in compatibleLicenses)
            host.Children.Add(BuildOptionCard(opt, opt.Id == currentLicenseId));

        // Pre-select current license
        if (currentLicenseId != null)
        {
            var match = compatibleLicenses.FindIndex(o => o.Id == currentLicenseId);
            if (match >= 0) SelectCard(match);
        }
    }

    private Border BuildOptionCard(LicenseOption opt, bool isCurrentChoice)
    {
        var card = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(2),
            BorderBrush = isCurrentChoice
                ? new SolidColorBrush(Color.FromRgb(100, 160, 255))
                : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 2),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
        };

        var content = new StackPanel { Spacing = 3 };

        // Title row with permission icons
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(new TextBlock
        {
            Text = opt.DisplayName,
            FontWeight = FontWeight.Bold,
            FontSize = 13,
        });

        // Permission badges
        var badgeColor = opt.BadgeColor switch
        {
            "green" => Color.FromArgb(60, 0, 180, 0),
            "yellow" => Color.FromArgb(60, 220, 180, 0),
            _ => Color.FromArgb(60, 160, 160, 160),
        };
        titleRow.Children.Add(new Border
        {
            Background = new SolidColorBrush(badgeColor),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = opt.CommercialOk ? "Commercial OK" : "Non-commercial",
                FontSize = 9,
                FontWeight = FontWeight.SemiBold,
            },
        });

        if (opt.AttributionRequired)
            titleRow.Children.Add(MakeSmallBadge("Credit required"));
        if (opt.ShareAlikeRequired)
            titleRow.Children.Add(MakeSmallBadge("Share-alike"));

        content.Children.Add(titleRow);

        // Description (the human-readable tooltip)
        content.Children.Add(new TextBlock
        {
            Text = opt.Tooltip,
            FontSize = 11,
            Opacity = 0.75,
            TextWrapping = TextWrapping.Wrap,
        });

        // "Currently selected" note
        if (isCurrentChoice)
        {
            content.Children.Add(new TextBlock
            {
                Text = "\u2713 Currently selected",
                FontSize = 10,
                Opacity = 0.6,
                FontStyle = FontStyle.Italic,
            });
        }

        card.Child = content;

        var index = _optionCards.Count;
        _optionCards.Add(card);

        card.PointerPressed += (_, _) => SelectCard(index);

        return card;
    }

    private void SelectCard(int index)
    {
        for (int i = 0; i < _optionCards.Count; i++)
        {
            _optionCards[i].BorderBrush = i == index
                ? new SolidColorBrush(Color.FromRgb(100, 160, 255))
                : new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        }

        // Resolve the license from the catalog
        var host = this.FindControl<StackPanel>("OptionsHost");
        if (host == null) return;

        // Count how many actual option cards exist (skip non-Border children like TextBlocks)
        int cardIdx = 0;
        var compatibleOptions = new List<LicenseOption>();
        foreach (var child in host.Children)
        {
            if (child is Border b && _optionCards.Contains(b))
            {
                if (cardIdx == index)
                {
                    // Find the matching license by walking the catalog
                    // The cards are built in the same order as compatibleLicenses
                }
                cardIdx++;
            }
        }

        // Simpler: track options directly
        _selected = index < LicenseCatalog.All.Length ? null : null;

        // We need the actual option list — store it
        if (_compatibleLicenses != null && index < _compatibleLicenses.Count)
            _selected = _compatibleLicenses[index];

        var btnChoose = this.FindControl<Button>("BtnChoose");
        if (btnChoose != null) btnChoose.IsEnabled = _selected != null;
    }

    private List<LicenseOption>? _compatibleLicenses;

    /// <summary>
    /// Overload of LoadOptions that stores the list for selection tracking.
    /// Call this one from the outside.
    /// </summary>
    public void Load(
        string? sourceLicenseDisplay,
        CorpusKind corpus,
        string? sourceLicense,
        string? currentLicenseId = null)
    {
        _compatibleLicenses = LicenseCatalog.GetCompatible(sourceLicense, corpus);
        LoadOptions(sourceLicenseDisplay, corpus, _compatibleLicenses, currentLicenseId);
    }

    private static Border MakeSmallBadge(string text) => new()
    {
        Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
        CornerRadius = new CornerRadius(3),
        Padding = new Thickness(3, 1),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 9, Opacity = 0.7 },
    };
}
