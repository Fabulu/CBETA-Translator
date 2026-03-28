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
        var rbMarkdown = this.FindControl<RadioButton>("RbMarkdown");
        var rbPlainText = this.FindControl<RadioButton>("RbPlainText");

        if (rbMarkdown?.IsChecked == true)
            return ScholarExportFormat.Markdown;
        if (rbPlainText?.IsChecked == true)
            return ScholarExportFormat.PlainText;

        return ScholarExportFormat.Html;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
