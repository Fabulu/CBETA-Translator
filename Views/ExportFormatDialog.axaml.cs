// Views/ExportFormatDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

public partial class ExportFormatDialog : Window
{
    public ExportFormatDialog()
    {
        InitializeComponent();

        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null)
            btnOk.Click += (_, _) => Close(GetSelectedFormat());

        if (btnCancel != null)
            btnCancel.Click += (_, _) => Close(null);
    }

    private ScholarExportFormat GetSelectedFormat()
    {
        if (IsChecked("RbJson"))
            return ScholarExportFormat.Json;
        if (IsChecked("RbPaperDraft"))
            return ScholarExportFormat.PaperDraft;
        if (IsChecked("RbCslJson"))
            return ScholarExportFormat.CslJson;
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

    private bool IsChecked(string name) => this.FindControl<RadioButton>(name)?.IsChecked == true;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}