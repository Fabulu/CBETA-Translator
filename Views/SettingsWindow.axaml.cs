using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Models;
using System.Globalization;

namespace CbetaTranslator.App.Views;

public partial class SettingsWindow : Window
{
    private RadioButton? _radioLightTheme;
    private RadioButton? _radioDarkTheme;

    private RadioButton? _radioPdfAlternating;
    private RadioButton? _radioPdfSideBySide;
    private CheckBox? _chkPdfIncludeEnglish;
    private CheckBox? _chkPdfForceSideBySideWhenEnglish;
    private CheckBox? _chkPdfAutoScaleFonts;
    private CheckBox? _chkPdfLockBilingualFontSize;
    private TextBox? _txtPdfLineSpacing;
    private TextBox? _txtPdfTrackingChinese;
    private TextBox? _txtPdfTrackingEnglish;
    private TextBox? _txtPdfParagraphSpacing;
    private TextBox? _txtPdfTargetFillRatio;
    private TextBox? _txtPdfMinFontSize;
    private TextBox? _txtPdfMaxFontSize;

    private Button? _btnApply;
    private Button? _btnCancel;

    private readonly AppConfig _working;

    public SettingsWindow() : this(new AppConfig())
    {
    }

    public SettingsWindow(AppConfig config)
    {
        _working = CloneConfig(config);
        InitializeComponent();
        BindFromConfig();
    }

    private static AppConfig CloneConfig(AppConfig cfg) => new()
    {
        TextRootPath = cfg.TextRootPath,
        LastSelectedRelPath = cfg.LastSelectedRelPath,
        IsDarkTheme = cfg.IsDarkTheme,
        PdfLayoutMode = cfg.PdfLayoutMode,
        PdfIncludeEnglish = cfg.PdfIncludeEnglish,
        PdfForceSideBySideWhenEnglish = cfg.PdfForceSideBySideWhenEnglish,
        PdfLineSpacing = cfg.PdfLineSpacing,
        PdfTrackingChinese = cfg.PdfTrackingChinese,
        PdfTrackingEnglish = cfg.PdfTrackingEnglish,
        PdfParagraphSpacing = cfg.PdfParagraphSpacing,
        PdfAutoScaleFonts = cfg.PdfAutoScaleFonts,
        PdfTargetFillRatio = cfg.PdfTargetFillRatio,
        PdfMinFontSize = cfg.PdfMinFontSize,
        PdfMaxFontSize = cfg.PdfMaxFontSize,
        PdfLockBilingualFontSize = cfg.PdfLockBilingualFontSize,
        Version = cfg.Version
    };

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _radioLightTheme = this.FindControl<RadioButton>("RadioLightTheme");
        _radioDarkTheme = this.FindControl<RadioButton>("RadioDarkTheme");

        _radioPdfAlternating = this.FindControl<RadioButton>("RadioPdfAlternating");
        _radioPdfSideBySide = this.FindControl<RadioButton>("RadioPdfSideBySide");
        _chkPdfIncludeEnglish = this.FindControl<CheckBox>("ChkPdfIncludeEnglish");
        _chkPdfForceSideBySideWhenEnglish = this.FindControl<CheckBox>("ChkPdfForceSideBySideWhenEnglish");
        _chkPdfAutoScaleFonts = this.FindControl<CheckBox>("ChkPdfAutoScaleFonts");
        _chkPdfLockBilingualFontSize = this.FindControl<CheckBox>("ChkPdfLockBilingualFontSize");
        _txtPdfLineSpacing = this.FindControl<TextBox>("TxtPdfLineSpacing");
        _txtPdfTrackingChinese = this.FindControl<TextBox>("TxtPdfTrackingChinese");
        _txtPdfTrackingEnglish = this.FindControl<TextBox>("TxtPdfTrackingEnglish");
        _txtPdfParagraphSpacing = this.FindControl<TextBox>("TxtPdfParagraphSpacing");
        _txtPdfTargetFillRatio = this.FindControl<TextBox>("TxtPdfTargetFillRatio");
        _txtPdfMinFontSize = this.FindControl<TextBox>("TxtPdfMinFontSize");
        _txtPdfMaxFontSize = this.FindControl<TextBox>("TxtPdfMaxFontSize");

        _btnApply = this.FindControl<Button>("BtnApply");
        _btnCancel = this.FindControl<Button>("BtnCancel");

        if (_btnApply != null)
            _btnApply.Click += OnApplyClicked;
        if (_btnCancel != null)
            _btnCancel.Click += OnCancelClicked;
    }

    private void BindFromConfig()
    {
        if (_radioLightTheme != null)
            _radioLightTheme.IsChecked = !_working.IsDarkTheme;
        if (_radioDarkTheme != null)
            _radioDarkTheme.IsChecked = _working.IsDarkTheme;

        if (_radioPdfAlternating != null)
            _radioPdfAlternating.IsChecked = _working.PdfLayoutMode == PdfLayoutMode.Alternating;
        if (_radioPdfSideBySide != null)
            _radioPdfSideBySide.IsChecked = _working.PdfLayoutMode == PdfLayoutMode.SideBySide;

        if (_chkPdfIncludeEnglish != null)
            _chkPdfIncludeEnglish.IsChecked = _working.PdfIncludeEnglish;
        if (_chkPdfForceSideBySideWhenEnglish != null)
            _chkPdfForceSideBySideWhenEnglish.IsChecked = _working.PdfForceSideBySideWhenEnglish;
        if (_chkPdfAutoScaleFonts != null)
            _chkPdfAutoScaleFonts.IsChecked = _working.PdfAutoScaleFonts;
        if (_chkPdfLockBilingualFontSize != null)
            _chkPdfLockBilingualFontSize.IsChecked = _working.PdfLockBilingualFontSize;

        if (_txtPdfLineSpacing != null)
            _txtPdfLineSpacing.Text = _working.PdfLineSpacing.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfTrackingChinese != null)
            _txtPdfTrackingChinese.Text = _working.PdfTrackingChinese.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfTrackingEnglish != null)
            _txtPdfTrackingEnglish.Text = _working.PdfTrackingEnglish.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfParagraphSpacing != null)
            _txtPdfParagraphSpacing.Text = _working.PdfParagraphSpacing.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfTargetFillRatio != null)
            _txtPdfTargetFillRatio.Text = _working.PdfTargetFillRatio.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfMinFontSize != null)
            _txtPdfMinFontSize.Text = _working.PdfMinFontSize.ToString("0.###", CultureInfo.InvariantCulture);
        if (_txtPdfMaxFontSize != null)
            _txtPdfMaxFontSize.Text = _working.PdfMaxFontSize.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        _working.IsDarkTheme = _radioDarkTheme?.IsChecked == true;
        _working.PdfLayoutMode = _radioPdfSideBySide?.IsChecked == true
            ? PdfLayoutMode.SideBySide
            : PdfLayoutMode.Alternating;
        _working.PdfIncludeEnglish = _chkPdfIncludeEnglish?.IsChecked == true;
        _working.PdfForceSideBySideWhenEnglish = _chkPdfForceSideBySideWhenEnglish?.IsChecked == true;
        _working.PdfAutoScaleFonts = _chkPdfAutoScaleFonts?.IsChecked == true;
        _working.PdfLockBilingualFontSize = _chkPdfLockBilingualFontSize?.IsChecked == true;

        _working.PdfLineSpacing = ParseFloat(_txtPdfLineSpacing?.Text, 1.4f, min: 1.0f, max: 2.2f);
        _working.PdfTrackingChinese = ParseFloat(_txtPdfTrackingChinese?.Text, 12.0f, min: 0.0f, max: 50.0f);
        _working.PdfTrackingEnglish = ParseFloat(_txtPdfTrackingEnglish?.Text, 8.0f, min: 0.0f, max: 40.0f);
        _working.PdfParagraphSpacing = ParseFloat(_txtPdfParagraphSpacing?.Text, 0.6f, min: 0.0f, max: 2.0f);
        _working.PdfTargetFillRatio = ParseFloat(_txtPdfTargetFillRatio?.Text, 0.88f, min: 0.60f, max: 0.98f);
        _working.PdfMinFontSize = ParseFloat(_txtPdfMinFontSize?.Text, 10.0f, min: 7.0f, max: 24.0f);
        _working.PdfMaxFontSize = ParseFloat(_txtPdfMaxFontSize?.Text, 18.0f, min: 8.0f, max: 30.0f);
        if (_working.PdfMaxFontSize < _working.PdfMinFontSize)
        {
            (_working.PdfMinFontSize, _working.PdfMaxFontSize) = (_working.PdfMaxFontSize, _working.PdfMinFontSize);
        }

        Close(_working);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private static float ParseFloat(string? s, float fallback, float min, float max)
    {
        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return fallback;
        if (v < min) return min;
        if (v > max) return max;
        return v;
    }
}
