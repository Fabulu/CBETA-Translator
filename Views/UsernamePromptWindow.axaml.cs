// Views/UsernamePromptWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CbetaTranslator.App.Views;

public partial class UsernamePromptWindow : Window
{
    private TextBox? _txtUsername;
    private TextBlock? _txtError;
    private Button? _btnOk;

    public UsernamePromptWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _txtUsername = this.FindControl<TextBox>("TxtUsername");
        _txtError    = this.FindControl<TextBlock>("TxtError");
        _btnOk       = this.FindControl<Button>("BtnOk");

        if (_btnOk != null)
            _btnOk.Click += OnOkClicked;

        if (_txtUsername != null)
            _txtUsername.KeyDown += OnTextKeyDown;
    }

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryCommit();
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e) => TryCommit();

    private void TryCommit()
    {
        var name = _txtUsername?.Text?.Trim() ?? "";
        if (name.Length == 0)
        {
            if (_txtError != null) _txtError.IsVisible = true;
            return;
        }
        Close(name);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Only allow closing once a valid name has been chosen (Close(name) sets a result).
        // Closing without a result (X button, Alt+F4) is blocked.
        if (!_committed)
            e.Cancel = true;
    }

    // Set to true just before we call Close(name) so the Closing handler lets it through.
    private bool _committed;

    private new void Close(object? result)
    {
        _committed = true;
        base.Close(result);
    }
}
