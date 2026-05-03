// Views/LinkCreationDialog.axaml.cs
using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class LinkCreationDialog : Window
{
    public LinkNode? Result { get; private set; }

    public LinkCreationDialog()
    {
        InitializeComponent();
        WireButtons();
        KeyDown += OnKeyDown;

        Opened += (_, _) =>
        {
            var txtName = this.FindControl<TextBox>("TxtName");
            txtName?.Focus();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireButtons()
    {
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null)
            btnOk.Click += (_, _) => TryConfirm();

        if (btnCancel != null)
            btnCancel.Click += (_, _) =>
            {
                Result = null;
                Close(null as object);
            };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = null;
            Close(null as object);
            e.Handled = true;
        }
        else if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            TryConfirm();
            e.Handled = true;
        }
    }

    private void TryConfirm()
    {
        var txtName = this.FindControl<TextBox>("TxtName");
        var txtUrl = this.FindControl<TextBox>("TxtUrl");
        var txtDesc = this.FindControl<TextBox>("TxtDescription");

        var name = txtName?.Text?.Trim() ?? "";
        var url = txtUrl?.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            txtName?.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(url))
        {
            txtUrl?.Focus();
            return;
        }

        Result = new LinkNode
        {
            Id = Guid.NewGuid().ToString("N")[..12],
            Name = name,
            Url = url,
            Description = string.IsNullOrWhiteSpace(txtDesc?.Text) ? null : txtDesc.Text.Trim(),
            CreatedUtc = DateTimeOffset.UtcNow
        };

        Close(Result);
    }
}
