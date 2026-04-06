// Views/SearchExportFormatDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Views;

public partial class SearchExportFormatDialog : Window
{
    public SearchExportFormatDialog()
    {
        InitializeComponent();

        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null)
            btnOk.Click += (_, _) => Close(GetSelectedFormat());

        if (btnCancel != null)
            btnCancel.Click += (_, _) => Close(null);
    }

    private SearchExportFormat GetSelectedFormat()
    {
        if (IsChecked("RbJson"))
            return SearchExportFormat.Json;
        if (IsChecked("RbTsv"))
            return SearchExportFormat.Tsv;
        if (IsChecked("RbCsv"))
            return SearchExportFormat.Csv;
        if (IsChecked("RbPlainText"))
            return SearchExportFormat.PlainText;
        if (IsChecked("RbMarkdown"))
            return SearchExportFormat.Markdown;
        return SearchExportFormat.Html;
    }

    private bool IsChecked(string name) => this.FindControl<RadioButton>(name)?.IsChecked == true;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}