// Views/ExportFormatDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

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
        var rbJson = this.FindControl<RadioButton>("RbJson");
        var rbBibTex = this.FindControl<RadioButton>("RbBibTex");
        var rbCsv = this.FindControl<RadioButton>("RbCsv");
        var rbTsv = this.FindControl<RadioButton>("RbTsv");
        var rbMarkdown = this.FindControl<RadioButton>("RbMarkdown");
        var rbPlainText = this.FindControl<RadioButton>("RbPlainText");

        if (rbJson?.IsChecked == true)
            return ScholarExportFormat.Json;
        if (rbBibTex?.IsChecked == true)
            return ScholarExportFormat.BibTex;
        if (rbCsv?.IsChecked == true)
            return ScholarExportFormat.Csv;
        if (rbTsv?.IsChecked == true)
            return ScholarExportFormat.Tsv;
        if (rbMarkdown?.IsChecked == true)
            return ScholarExportFormat.Markdown;
        if (rbPlainText?.IsChecked == true)
            return ScholarExportFormat.PlainText;

        return ScholarExportFormat.Html;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
