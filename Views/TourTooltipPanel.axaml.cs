using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CbetaTranslator.App.Views;

public partial class TourTooltipPanel : UserControl
{
    private TextBlock? _txtTitle;
    private TextBlock? _txtBody;
    private TextBlock? _txtProgress;
    private Button? _btnBack;
    private Button? _btnNext;
    private Button? _btnSkip;

    public event EventHandler? NextClicked;
    public event EventHandler? BackClicked;
    public event EventHandler? SkipClicked;

    public TourTooltipPanel()
    {
        InitializeComponent();
        FindControls();
        WireEvents();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _txtTitle = this.FindControl<TextBlock>("TxtTitle");
        _txtBody = this.FindControl<TextBlock>("TxtBody");
        _txtProgress = this.FindControl<TextBlock>("TxtProgress");
        _btnBack = this.FindControl<Button>("BtnBack");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnSkip = this.FindControl<Button>("BtnSkip");
    }

    private void WireEvents()
    {
        if (_btnNext != null) _btnNext.Click += (_, _) => NextClicked?.Invoke(this, EventArgs.Empty);
        if (_btnBack != null) _btnBack.Click += (_, _) => BackClicked?.Invoke(this, EventArgs.Empty);
        if (_btnSkip != null) _btnSkip.Click += (_, _) => SkipClicked?.Invoke(this, EventArgs.Empty);
    }

    public void Update(string title, string body, int stepIndex, int totalSteps, bool canGoBack)
    {
        if (_txtTitle != null) _txtTitle.Text = title;
        if (_txtBody != null) _txtBody.Text = body;
        if (_txtProgress != null) _txtProgress.Text = $"Step {stepIndex + 1} of {totalSteps}";
        if (_btnBack != null) _btnBack.IsEnabled = canGoBack;

        // Change button text on last step
        if (_btnNext != null)
            _btnNext.Content = stepIndex >= totalSteps - 1 ? "Finish" : "Next";
    }
}
