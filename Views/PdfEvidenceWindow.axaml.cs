using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReadZen.App.Services;

namespace ReadZen.App.Views;

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

    // Region coordinates (percentages 0.0-1.0), set when evidence has specific coords
    private double _regionX;
    private double _regionY;
    private double _regionWidth = 1.0;
    private double _regionHeight = 1.0;
    private bool _hasRegion;

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
            txtTitle.Text = $"Witness Evidence \u2014 {witnessLabel} p.{pageNumber + 1}";
        Title = $"Evidence \u00b7 {witnessLabel} p.{pageNumber + 1}";

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

        // Show download prompt
        var btnDownload = this.FindControl<Button>("BtnDownload");
        if (btnDownload != null)
        {
            btnDownload.IsVisible = true;
            btnDownload.Click += async (_, _) =>
            {
                btnDownload.IsEnabled = false;
                btnDownload.Content = "Downloading...";

                var progressBar = this.FindControl<ProgressBar>("DownloadProgress");
                if (progressBar != null) progressBar.IsVisible = true;

                ShowStatus("Downloading PDF from Wikimedia Commons...", false);

                var result = await _pdfService.DownloadPdfAsync(
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
                    ShowStatus("Download failed or hash verification failed. Please try again.", true);
                    btnDownload.IsEnabled = true;
                    btnDownload.Content = "Retry Download";
                    if (progressBar != null) progressBar.IsVisible = false;
                }
            };
        }

        ShowStatus($"Source PDF not cached locally. Click 'Download PDF' to fetch from Commons.\n{url}", false);
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
        if (btnDownload != null)
        {
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
}
