// Views/ExportFormatDialog.axaml.cs
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

public partial class ExportFormatDialog : Window
{
    /// <summary>
    /// Names of radio buttons whose formats do not use inline citations.
    /// The citation style panel is hidden when one of these is selected.
    /// </summary>
    private static readonly HashSet<string> _noCitationStyleFormats = new()
    {
        "RbBibTex", "RbCslJson", "RbRis", "RbJson", "RbReaderTagBundle", "RbReaderTagTsv"
    };

    /// <summary>
    /// Names of every radio button in the dialog, in declaration order.
    /// Used to wire up the visibility toggle for the citation style panel.
    /// </summary>
    private static readonly string[] _allRadioButtons =
    {
        "RbHtml", "RbMarkdown", "RbPlainText", "RbCsv", "RbTsv",
        "RbReaderTagBundle", "RbReaderTagTsv", "RbPaperDraft",
        "RbBibTex", "RbRis", "RbCslJson", "RbJson"
    };

    public ExportFormatDialog()
    {
        InitializeComponent();

        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null)
            btnOk.Click += (_, _) => Close(new ExportDialogResult(GetSelectedFormat(), GetSelectedCitationStyle()));

        if (btnCancel != null)
            btnCancel.Click += (_, _) => Close(null);

        // Wire each radio button to toggle citation style panel visibility.
        foreach (var name in _allRadioButtons)
        {
            var rb = this.FindControl<RadioButton>(name);
            if (rb == null) continue;
            var rbName = name; // capture for closure
            rb.IsCheckedChanged += (_, _) =>
            {
                if (rb.IsChecked == true)
                    UpdateCitationStylePanelVisibility(rbName);
            };
        }

        // Set initial visibility (HTML is checked by default, so panel is visible).
        UpdateCitationStylePanelVisibility("RbHtml");
    }

    private void UpdateCitationStylePanelVisibility(string checkedRadioName)
    {
        var panel = this.FindControl<StackPanel>("CitationStylePanel");
        if (panel != null)
            panel.IsVisible = !_noCitationStyleFormats.Contains(checkedRadioName);
    }

    private ScholarExportFormat GetSelectedFormat()
    {
        if (IsChecked("RbJson"))
            return ScholarExportFormat.Json;
        if (IsChecked("RbPaperDraft"))
            return ScholarExportFormat.PaperDraft;
        if (IsChecked("RbCslJson"))
            return ScholarExportFormat.CslJson;
        if (IsChecked("RbRis"))
            return ScholarExportFormat.Ris;
        if (IsChecked("RbBibTex"))
            return ScholarExportFormat.BibTex;
        if (IsChecked("RbReaderTagBundle"))
            return ScholarExportFormat.ReaderTagBundle;
        if (IsChecked("RbReaderTagTsv"))
            return ScholarExportFormat.ReaderTagTsv;
        if (IsChecked("RbCsv"))
            return ScholarExportFormat.Csv;
        if (IsChecked("RbTsv"))
            return ScholarExportFormat.Tsv;
        if (IsChecked("RbMarkdown"))
            return ScholarExportFormat.Markdown;
        if (IsChecked("RbPlainText"))
            return ScholarExportFormat.PlainText;

        return ScholarExportFormat.Html;
    }

    private CitationStyle GetSelectedCitationStyle()
    {
        var cb = this.FindControl<ComboBox>("CbCitationStyle");
        return (cb?.SelectedIndex ?? 1) switch
        {
            0 => CitationStyle.Plain,
            1 => CitationStyle.Chicago,
            2 => CitationStyle.Apa,
            3 => CitationStyle.Mla,
            4 => CitationStyle.Sbl,
            5 => CitationStyle.CbetaReference,
            _ => CitationStyle.Chicago,
        };
    }

    private bool IsChecked(string name) => this.FindControl<RadioButton>(name)?.IsChecked == true;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
