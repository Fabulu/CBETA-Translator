// Views/EditionExportFormatDialog.axaml.cs
// Simple format-picker dialog for critical edition export.

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ReadZen.App.Views;

/// <summary>
/// A modal dialog that lets the user choose an export format for a critical edition.
/// Returns the selected <see cref="EditionExportFormat"/> via <see cref="SelectedFormat"/>,
/// or null if the user cancelled.
/// </summary>
public partial class EditionExportFormatDialog : Window
{
    private RadioButton? _rbTei, _rbPdf, _rbHtml, _rbLatex, _rbLeiden, _rbCsv;

    /// <summary>The format chosen by the user, or null if cancelled.</summary>
    public EditionExportFormat? SelectedFormat { get; private set; }

    public EditionExportFormatDialog()
    {
        InitializeComponent();

        _rbTei = this.FindControl<RadioButton>("RbTei");
        _rbPdf = this.FindControl<RadioButton>("RbPdf");
        _rbHtml = this.FindControl<RadioButton>("RbHtml");
        _rbLatex = this.FindControl<RadioButton>("RbLatex");
        _rbLeiden = this.FindControl<RadioButton>("RbLeiden");
        _rbCsv = this.FindControl<RadioButton>("RbCsv");

        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null) btnOk.Click += (_, _) =>
        {
            SelectedFormat = ResolveSelection();
            Close();
        };

        if (btnCancel != null) btnCancel.Click += (_, _) =>
        {
            SelectedFormat = null;
            Close();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private EditionExportFormat ResolveSelection()
    {
        if (_rbPdf?.IsChecked == true) return EditionExportFormat.Pdf;
        if (_rbHtml?.IsChecked == true) return EditionExportFormat.Html;
        if (_rbLatex?.IsChecked == true) return EditionExportFormat.Latex;
        if (_rbLeiden?.IsChecked == true) return EditionExportFormat.Leiden;
        if (_rbCsv?.IsChecked == true) return EditionExportFormat.Csv;
        return EditionExportFormat.TeiXml; // default
    }
}

/// <summary>Export format options for critical edition export.</summary>
public enum EditionExportFormat
{
    TeiXml,
    Pdf,
    Html,
    Latex,
    Leiden,
    Csv
}
