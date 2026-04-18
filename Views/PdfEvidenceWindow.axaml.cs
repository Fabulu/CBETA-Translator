using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Linq;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

// Guard flags to prevent event handler accumulation on repeated calls.

/// <summary>
/// Viewer window for PDF-based witness evidence.
/// Shows a rendered PDF page with an optional highlighted region
/// indicating the specific evidence area.
/// </summary>
public partial class PdfEvidenceWindow : Window
{
    private readonly PdfEvidenceService _pdfService;
    private Bitmap? _currentBitmap;
    private double _zoom = 1.0;
    private bool _downloadWired;
    private bool _witnessSelectorWired;
    private bool _ocrToggleWired;
    private bool _ocrPanelVisible;
    private string? _ocrBaseDir2;
    private string? _ocrSiglum;
    private string? _ocrPageId;

    // Region coordinates (percentages 0.0-1.0), set when evidence has specific coords
    private double _regionX;
    private double _regionY;
    private double _regionWidth = 1.0;
    private double _regionHeight = 1.0;
    private bool _hasRegion;

    // Witness selector state
    private string? _ocrBaseDir;
    private string? _currentLocus;
    private List<string> _availableSigla = new();

    public PdfEvidenceWindow() : this(new PdfEvidenceService()) { }

    public PdfEvidenceWindow(PdfEvidenceService pdfService)
    {
        _pdfService = pdfService;
        InitializeComponent();
        WireEvents();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireEvents()
    {
        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null) btnClose.Click += (_, _) => Close();

        var btnZoomIn = this.FindControl<Button>("BtnZoomIn");
        if (btnZoomIn != null) btnZoomIn.Click += (_, _) => SetZoom(_zoom * 1.25);

        var btnZoomOut = this.FindControl<Button>("BtnZoomOut");
        if (btnZoomOut != null) btnZoomOut.Click += (_, _) => SetZoom(_zoom * 0.8);

        var btnZoomFit = this.FindControl<Button>("BtnZoomFit");
        if (btnZoomFit != null) btnZoomFit.Click += (_, _) => SetZoom(1.0);

        // Scroll wheel zoom
        var scroller = this.FindControl<ScrollViewer>("ImageScroller");
        if (scroller != null)
        {
            scroller.PointerWheelChanged += (_, e) =>
            {
                if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
                {
                    double factor = e.Delta.Y > 0 ? 1.15 : 0.87;
                    SetZoom(_zoom * factor);
                    e.Handled = true;
                }
            };
        }
    }

    /// <summary>
    /// Load and display a PDF page with an optional evidence region highlight.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file (absolute or cache-resolvable).</param>
    /// <param name="pageNumber">Zero-based page number.</param>
    /// <param name="witnessLabel">Display label for the witness (e.g., "T 2005 (Taish\u014d)").</param>
    /// <param name="regionX">Left edge of evidence region (0.0-1.0), or -1 for full page.</param>
    /// <param name="regionY">Top edge of evidence region (0.0-1.0), or -1 for full page.</param>
    /// <param name="regionWidth">Width of evidence region (0.0-1.0).</param>
    /// <param name="regionHeight">Height of evidence region (0.0-1.0).</param>
    public void LoadEvidence(
        string pdfPath,
        int pageNumber,
        string witnessLabel,
        double regionX = -1,
        double regionY = -1,
        double regionWidth = 1.0,
        double regionHeight = 1.0)
    {
        var txtTitle = this.FindControl<TextBlock>("TxtTitle");
        var txtMeta = this.FindControl<TextBlock>("TxtMeta");

        if (txtTitle != null)
            txtTitle.Text = $"Witness Evidence \u2014 {witnessLabel} \u00b7 p.{pageNumber + 1}";
        Title = $"Evidence \u00b7 {witnessLabel} \u00b7 p.{pageNumber + 1}";

        _hasRegion = regionX >= 0 && regionY >= 0;
        _regionX = Math.Max(0, regionX);
        _regionY = Math.Max(0, regionY);
        _regionWidth = Math.Clamp(regionWidth, 0.01, 1.0);
        _regionHeight = Math.Clamp(regionHeight, 0.01, 1.0);

        // Resolve PDF path
        var resolved = _pdfService.ResolvePdfPath(pdfPath);
        if (resolved == null)
        {
            ShowNotCached(pdfPath, witnessLabel, pageNumber);
            return;
        }

        if (txtMeta != null)
            txtMeta.Text = $"Source: {System.IO.Path.GetFileName(resolved)} \u00b7 Page {pageNumber + 1}";

        // Render the page region
        _currentBitmap = _hasRegion
            ? _pdfService.RenderPageRegion(resolved, pageNumber,
                _regionY, _regionHeight, _regionX, _regionWidth)
            : _pdfService.RenderPageRegion(resolved, pageNumber);

        if (_currentBitmap == null)
        {
            ShowStatus("Failed to render PDF page. The file may be corrupt or the page number invalid.", true);
            return;
        }

        var image = this.FindControl<Image>("PageImage");
        if (image != null)
        {
            image.Source = _currentBitmap;
            image.Width = _currentBitmap.PixelSize.Width;
            image.Height = _currentBitmap.PixelSize.Height;
        }

        // Draw highlight overlay if we have a specific region
        if (_hasRegion)
            DrawHighlight();

        SetZoom(1.0);
    }

    /// <summary>
    /// Load evidence from a Commons URL, downloading if needed.
    /// </summary>
    public void LoadEvidenceFromUrl(
        string url,
        string localFileName,
        int pageNumber,
        string witnessLabel,
        string? expectedSha256 = null,
        double regionX = -1,
        double regionY = -1,
        double regionWidth = 1.0,
        double regionHeight = 1.0)
    {
        var txtTitle = this.FindControl<TextBlock>("TxtTitle");
        if (txtTitle != null)
            txtTitle.Text = $"Witness Evidence \u2014 {witnessLabel} p.{pageNumber + 1}";

        // Check if already cached
        var cached = _pdfService.ResolvePdfPath(localFileName);
        if (cached != null)
        {
            LoadEvidence(cached, pageNumber, witnessLabel,
                regionX, regionY, regionWidth, regionHeight);
            return;
        }

        // Show download prompt (guard against handler accumulation)
        var btnDownload = this.FindControl<Button>("BtnDownload");
        if (btnDownload != null && !_downloadWired)
        {
            _downloadWired = true;
            btnDownload.IsVisible = true;
            btnDownload.Click += async (_, _) =>
            {
                btnDownload.IsEnabled = false;
                btnDownload.Content = "Downloading...";

                var progressBar = this.FindControl<ProgressBar>("DownloadProgress");
                if (progressBar != null) progressBar.IsVisible = true;

                ShowStatus("Downloading PDF from Wikimedia Commons...", false);

                string? result;
                try
                {
                    result = await _pdfService.DownloadPdfAsync(
                        url, localFileName, expectedSha256,
                        progress: p =>
                        {
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (progressBar != null) progressBar.Value = p * 100;
                                btnDownload.Content = $"Downloading... {p:P0}";
                            });
                        },
                        ct: CancellationToken.None);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    ShowStatus("Download failed \u2014 check your internet connection.", true);
                    btnDownload.IsEnabled = true;
                    btnDownload.Content = "Retry Download";
                    if (progressBar != null) progressBar.IsVisible = false;
                    return;
                }

                if (result != null)
                {
                    if (progressBar != null) progressBar.IsVisible = false;
                    btnDownload.IsVisible = false;
                    HideStatus();
                    LoadEvidence(result, pageNumber, witnessLabel,
                        regionX, regionY, regionWidth, regionHeight);
                }
                else
                {
                    ShowStatus("Downloaded file does not match the expected SHA-256 hash. The source may have been modified since the edition was produced.", true);
                    btnDownload.IsEnabled = true;
                    btnDownload.Content = "Retry Download";
                    if (progressBar != null) progressBar.IsVisible = false;
                }
            };
        }

        ShowStatus($"Source PDF not cached locally. Click 'Download PDF' to fetch from Commons.\n{url}", false);
    }

    /// <summary>
    /// Load and display a PNG page image directly (no Pdfium).
    /// </summary>
    /// <param name="pngPath">Absolute path to the PNG file.</param>
    /// <param name="witnessLabel">Display label for the witness.</param>
    public void LoadPageImageEvidence(string pngPath, string witnessLabel)
    {
        _hasRegion = false;
        var txtTitle = this.FindControl<TextBlock>("TxtTitle");
        var txtMeta = this.FindControl<TextBlock>("TxtMeta");

        var fileName = System.IO.Path.GetFileName(pngPath);
        // Extract locus from filename if available (e.g. "T1-p008.l01.png" -> "T1-p008.l01")
        var locusFromFile = System.IO.Path.GetFileNameWithoutExtension(pngPath);
        var locusDisplay = !string.IsNullOrEmpty(_currentLocus) ? _currentLocus : locusFromFile;
        if (txtTitle != null)
            txtTitle.Text = $"Witness Evidence \u2014 {witnessLabel} \u2014 {locusDisplay}";
        if (txtMeta != null)
            txtMeta.Text = $"Page image: {fileName}";
        Title = $"Evidence \u00b7 {witnessLabel} \u00b7 {locusDisplay}";

        _currentBitmap = _pdfService.LoadPageImage(pngPath);
        if (_currentBitmap == null)
        {
            ShowStatus($"Failed to load page image: {fileName}", true);
            return;
        }

        var image = this.FindControl<Image>("PageImage");
        if (image != null)
        {
            image.Source = _currentBitmap;
            image.Width = _currentBitmap.PixelSize.Width;
            image.Height = _currentBitmap.PixelSize.Height;
        }

        // Clear any highlight overlay
        var canvas = this.FindControl<Canvas>("HighlightCanvas");
        if (canvas != null) canvas.Children.Clear();

        SetZoom(1.0);
    }

    /// <summary>
    /// Configures the witness selector ComboBox with available sigla.
    /// When the user switches witness, the page image for the same locus
    /// is loaded from the new witness's page-images directory.
    /// </summary>
    /// <param name="sigla">Available witness sigla.</param>
    /// <param name="currentSiglum">Currently displayed siglum.</param>
    /// <param name="locus">Current locus ID (for resolving page paths on switch).</param>
    /// <param name="ocrBaseDir">Base directory for OCR page images.</param>
    public void SetWitnessSelector(List<string> sigla, string currentSiglum, string locus, string ocrBaseDir)
    {
        _availableSigla = sigla;
        _currentLocus = locus;
        _ocrBaseDir = ocrBaseDir;

        var cb = this.FindControl<ComboBox>("CbWitness");
        if (cb == null || sigla.Count <= 1) return;

        cb.ItemsSource = sigla;
        cb.SelectedItem = currentSiglum;
        cb.IsVisible = true;

        if (_witnessSelectorWired) return;
        _witnessSelectorWired = true;
        cb.SelectionChanged += (_, _) =>
        {
            if (cb.SelectedItem is not string newSiglum) return;
            if (string.IsNullOrEmpty(_currentLocus) || string.IsNullOrEmpty(_ocrBaseDir)) return;

            var newPath = PdfEvidenceService.ResolvePageImagePath(_ocrBaseDir, newSiglum, _currentLocus);
            if (newPath != null)
            {
                LoadPageImageEvidence(newPath, newSiglum);
                // Reload OCR readings for the new witness
                if (_ocrBaseDir2 != null && _ocrPageId != null)
                    LoadOcrReadings(_ocrBaseDir2, newSiglum, _ocrPageId);
            }
            else
            {
                ShowStatus($"No page image found for witness {newSiglum} at locus {_currentLocus}", true);
            }
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void SetZoom(double newZoom)
    {
        _zoom = Math.Clamp(newZoom, 0.1, 5.0);
        var image = this.FindControl<Image>("PageImage");
        if (image != null && _currentBitmap != null)
        {
            image.Width = _currentBitmap.PixelSize.Width * _zoom;
            image.Height = _currentBitmap.PixelSize.Height * _zoom;
        }

        var panel = this.FindControl<Panel>("ImageHost");
        if (panel != null)
        {
            panel.Width = (_currentBitmap?.PixelSize.Width ?? 800) * _zoom;
            panel.Height = (_currentBitmap?.PixelSize.Height ?? 600) * _zoom;
        }

        // Redraw highlight at new zoom
        if (_hasRegion) DrawHighlight();

        var txtZoom = this.FindControl<TextBlock>("TxtZoom");
        if (txtZoom != null)
            txtZoom.Text = $"{_zoom:P0}";
    }

    private void DrawHighlight()
    {
        var canvas = this.FindControl<Canvas>("HighlightCanvas");
        if (canvas == null || _currentBitmap == null) return;

        canvas.Children.Clear();

        int rw = (int)(_currentBitmap.PixelSize.Width * _zoom);
        int rh = (int)(_currentBitmap.PixelSize.Height * _zoom);
        canvas.Width = rw;
        canvas.Height = rh;

        var rect = _pdfService.GetHighlightRect(
            rw, rh, _regionX, _regionY, _regionWidth, _regionHeight);

        if (rect == null) return;
        var (hx, hy, hw, hh) = rect.Value;

        var highlight = new Rectangle
        {
            Width = hw,
            Height = hh,
            Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 0)),
            Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 200, 0)),
            StrokeThickness = 2,
        };

        Canvas.SetLeft(highlight, hx);
        Canvas.SetTop(highlight, hy);
        canvas.Children.Add(highlight);
    }

    private void ShowNotCached(string originalPath, string witnessLabel, int pageNumber)
    {
        ShowStatus(
            $"Source PDF not found: {System.IO.Path.GetFileName(originalPath)}\n" +
            "Use the 'Download PDF' button to fetch from Wikimedia Commons.",
            true);

        var btnDownload = this.FindControl<Button>("BtnDownload");
        if (btnDownload != null && !_downloadWired)
        {
            _downloadWired = true;
            btnDownload.IsVisible = true;
            btnDownload.Content = "Open Commons page";
            btnDownload.Click += (_, _) =>
            {
                // Best-effort: open the expected Commons URL pattern
                try
                {
                    Process.Start(new ProcessStartInfo(
                        $"https://commons.wikimedia.org/wiki/File:{System.IO.Path.GetFileName(originalPath)}")
                    { UseShellExecute = true });
                }
                catch { /* best-effort */ }
            };
        }
    }

    private void ShowStatus(string message, bool isWarning)
    {
        var banner = this.FindControl<Border>("StatusBanner");
        var txt = this.FindControl<TextBlock>("TxtStatus");
        if (banner == null || txt == null) return;

        txt.Text = message;
        banner.IsVisible = true;
        banner.BorderBrush = isWarning ? Brushes.OrangeRed : Brushes.Gray;
        banner.Opacity = isWarning ? 0.9 : 0.6;
    }

    private void HideStatus()
    {
        var banner = this.FindControl<Border>("StatusBanner");
        if (banner != null) banner.IsVisible = false;
    }

    /// <summary>
    /// Loads OCR engine readings for a page and populates the side panel.
    /// </summary>
    /// <param name="ocrBaseDir">Base directory containing witness OCR data.</param>
    /// <param name="siglum">Witness siglum (e.g. "T1").</param>
    /// <param name="pageId">Page identifier (e.g. "p008").</param>
    public void LoadOcrReadings(string ocrBaseDir, string siglum, string pageId)
    {
        _ocrBaseDir2 = ocrBaseDir;
        _ocrSiglum = siglum;
        _ocrPageId = pageId;

        var engineTexts = WitnessOcrLoader.LoadAllEngineTexts(ocrBaseDir, siglum, pageId);

        var panel = this.FindControl<Border>("OcrPanel");
        var content = this.FindControl<StackPanel>("OcrContent");
        var toggleBtn = this.FindControl<Button>("BtnToggleOcr");
        if (content == null || toggleBtn == null) return;

        content.Children.Clear();

        // Engine display names
        var engineLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["rapidocr"] = "Rapid",
            ["tesseract-full-pass"] = "Tess",
            ["paddleocr-ppocrv4"] = "Paddle",
            ["easyocr-full-pass"] = "Easy",
        };

        // Canonical order
        var orderedEngines = new[] { "rapidocr", "tesseract-full-pass", "paddleocr-ppocrv4", "easyocr-full-pass" };

        // Also include any engines not in canonical list
        var allEngines = new List<string>(orderedEngines);
        foreach (var key in engineTexts.Keys)
        {
            if (!allEngines.Contains(key, StringComparer.OrdinalIgnoreCase))
                allEngines.Add(key);
        }

        bool anyEngine = false;
        foreach (var engine in allEngines)
        {
            string label = engineLabels.TryGetValue(engine, out var l) ? l : engine;
            string text;
            bool hasText = engineTexts.TryGetValue(engine, out var t) && !string.IsNullOrWhiteSpace(t);
            text = hasText ? t! : "(not available)";
            if (hasText) anyEngine = true;

            var header = new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Opacity = hasText ? 1.0 : 0.5,
                Margin = new Thickness(0, 4, 0, 2),
            };

            var body = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontFamily = new FontFamily("Consolas, monospace"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = hasText ? 1.0 : 0.4,
                Margin = new Thickness(0, 0, 0, 2),
            };

            var separator = new Border
            {
                Height = 1,
                Opacity = 0.3,
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0),
            };

            content.Children.Add(header);
            content.Children.Add(body);
            content.Children.Add(separator);
        }

        // Always show the toggle button; disable if no OCR data exists
        toggleBtn.IsVisible = true;
        if (!anyEngine)
        {
            toggleBtn.IsEnabled = false;
            toggleBtn.Content = "No OCR data";
        }
        else
        {
            toggleBtn.IsEnabled = true;
            WireOcrToggle();
        }
    }

    /// <summary>
    /// Extracts a page ID from a locus string (e.g. "T1-p008.l01" becomes "p008").
    /// </summary>
    internal static string? ExtractPageIdFromLocus(string locus)
    {
        if (string.IsNullOrEmpty(locus)) return null;

        // Strip .lNN suffix
        var page = locus;
        var dotIdx = locus.LastIndexOf('.');
        if (dotIdx > 0 && dotIdx < locus.Length - 1 && locus[dotIdx + 1] == 'l')
            page = locus[..dotIdx];

        // Extract page portion after the last '-p'
        var pIdx = page.LastIndexOf("-p", StringComparison.Ordinal);
        if (pIdx < 0) return null;

        return page[(pIdx + 1)..]; // e.g. "p008"
    }

    private void WireOcrToggle()
    {
        if (_ocrToggleWired) return;
        _ocrToggleWired = true;

        var toggleBtn = this.FindControl<Button>("BtnToggleOcr");
        if (toggleBtn == null) return;

        toggleBtn.Click += (_, _) =>
        {
            _ocrPanelVisible = !_ocrPanelVisible;
            var panel = this.FindControl<Border>("OcrPanel");
            if (panel != null) panel.IsVisible = _ocrPanelVisible;
            toggleBtn.Content = _ocrPanelVisible ? "Hide OCR \u25C2" : "Show OCR \u25B8";
        };
    }
}
