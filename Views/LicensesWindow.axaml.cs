using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Infrastructure;

namespace CbetaTranslator.App.Views;

public partial class LicensesWindow : Window
{
    private readonly string? _root;

    private TextBox? _txtLicenses;
    private TextBlock? _txtHint;
    private Button? _btnClose;

    public LicensesWindow(string? root)
    {
        _root = root;

        InitializeComponent();
        FindControls();
        WireEvents();

        LoadText();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _txtLicenses = this.FindControl<TextBox>("TxtLicenses");
        _txtHint = this.FindControl<TextBlock>("TxtHint");
        _btnClose = this.FindControl<Button>("BtnClose");
    }

    private void WireEvents()
    {
        if (_btnClose != null)
            _btnClose.Click += (_, _) => Close();
    }

    private void LoadText()
    {
        if (_txtLicenses == null) return;

        // Resolve dictionary path the same way your loader does (AppContext.BaseDirectory/assets/...)
        string baseDir = AppContext.BaseDirectory;
        string dictPath = Path.Combine(baseDir, "assets", "dict", "cedict_ts.u8");

        string header = CedictLicenseService.ReadCedictHeader(dictPath);

        _txtLicenses.Text =
            "=== CC-CEDICT (dictionary data) ===\n"
            + header
            + "\n\n"
            + "=== Notes ===\n"
            + "• This attribution text is read from the dictionary file header shipped with the app.\n"
            + "• If you replace the dictionary file, the displayed attribution updates automatically.\n";

        if (_txtHint != null)
        {
            string rootInfo = string.IsNullOrWhiteSpace(_root) ? "(no CBETA root loaded)" : _root!;
            _txtHint.Text = $"CBETA root: {rootInfo}";
        }
    }
}
