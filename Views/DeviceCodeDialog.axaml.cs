// Views/DeviceCodeDialog.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ReadZen.App.Views;

public partial class DeviceCodeDialog : Window
{
    public DeviceCodeDialog()
    {
        InitializeComponent();
    }

    public DeviceCodeDialog(string userCode, bool copiedToClipboard = true) : this()
    {
        var txtCode = this.FindControl<TextBlock>("TxtCode");
        if (txtCode != null)
            txtCode.Text = userCode;

        if (!copiedToClipboard)
        {
            var txtInstruction = this.FindControl<TextBlock>("TxtInstruction");
            if (txtInstruction != null)
                txtInstruction.Text = "Copy this code and paste it on the GitHub page.";
        }

        var btnOk = this.FindControl<Button>("BtnOk");
        if (btnOk != null)
            btnOk.Click += (_, _) => Close();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void DismissIfOpen()
    {
        try
        {
            if (IsVisible)
                Close();
        }
        catch
        {
            // Window may already be closed
        }
    }
}
