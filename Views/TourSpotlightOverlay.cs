using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ReadZen.App.Views;

/// <summary>
/// Full-window semi-transparent dark overlay with a rounded-rectangle cutout (spotlight)
/// around a target control. Draws 4 dark rectangles around the cutout rather than using
/// CombinedGeometry for simplicity and reliability.
/// When a cutout is active, the overlay is not hit-test-visible so clicks pass through
/// to the spotlighted control underneath.
/// </summary>
public sealed class TourSpotlightOverlay : Control
{
    private static readonly IBrush BackdropBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
    private const double CutoutPadding = 8;
    private const double CutoutCornerRadius = 8;

    private Rect? _targetBounds;

    /// <summary>
    /// The bounds of the spotlight cutout in overlay-local coordinates.
    /// Set by the orchestrator after locating the target control.
    /// </summary>
    public Rect? TargetBounds
    {
        get => _targetBounds;
        set
        {
            _targetBounds = value;
            // When a cutout is active, let clicks pass through to the control underneath
            IsHitTestVisible = value == null;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var totalWidth = Bounds.Width;
        var totalHeight = Bounds.Height;
        if (totalWidth <= 0 || totalHeight <= 0) return;

        if (_targetBounds == null || _targetBounds.Value.Width <= 0 || _targetBounds.Value.Height <= 0)
        {
            // No cutout — draw full dark overlay
            context.FillRectangle(BackdropBrush, new Rect(0, 0, totalWidth, totalHeight));
            return;
        }

        var cut = _targetBounds.Value;

        // Inflate by padding
        var padded = cut.Inflate(new Thickness(CutoutPadding));

        // Clamp to overlay bounds
        double cx = System.Math.Max(0, padded.X);
        double cy = System.Math.Max(0, padded.Y);
        double cr = System.Math.Min(totalWidth, padded.Right);
        double cb = System.Math.Min(totalHeight, padded.Bottom);

        // Draw 4 rectangles around the cutout
        // Top strip
        if (cy > 0)
            context.FillRectangle(BackdropBrush, new Rect(0, 0, totalWidth, cy));

        // Bottom strip
        if (cb < totalHeight)
            context.FillRectangle(BackdropBrush, new Rect(0, cb, totalWidth, totalHeight - cb));

        // Left strip (between top and bottom)
        if (cx > 0)
            context.FillRectangle(BackdropBrush, new Rect(0, cy, cx, cb - cy));

        // Right strip (between top and bottom)
        if (cr < totalWidth)
            context.FillRectangle(BackdropBrush, new Rect(cr, cy, totalWidth - cr, cb - cy));

        // Draw a rounded-rectangle border around the cutout for emphasis
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 2);
        context.DrawRectangle(null, borderPen,
            new RoundedRect(new Rect(cx, cy, cr - cx, cb - cy), CutoutCornerRadius));
    }
}
