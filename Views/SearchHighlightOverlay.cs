using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace CbetaTranslator.App.Views;

/// <summary>
/// Draws a highlight rectangle for a text range inside a TextBox WITHOUT touching SelectionStart/End.
/// Uses reflection to access TextPresenter -> TextLayout -> HitTestTextRange.
/// IMPORTANT: must re-render when TextBox scrolls (ScrollViewer.Offset changes).
/// </summary>
public sealed class SearchHighlightOverlay : Control
{
    private TextBox? _target;
    private ScrollViewer? _sv;

    public TextBox? Target
    {
        get => _target;
        set
        {
            if (ReferenceEquals(_target, value)) return;

            Detach();
            _target = value;
            Attach();

            InvalidateVisual();
        }
    }

    public int Start { get; private set; } = -1;
    public int Length { get; private set; } = 0;

    private static readonly IBrush FillBrush = new SolidColorBrush(Color.Parse("#66FFD54A")); // semi
    private static readonly IPen OutlinePen = new Pen(new SolidColorBrush(Color.Parse("#CCFFD54A")), 1);

    public SearchHighlightOverlay()
    {
        // If the overlay is detached/re-attached, keep handlers sane.
        AttachedToVisualTree += (_, _) => Attach();
        DetachedFromVisualTree += (_, _) => Detach();
    }

    public void Clear()
    {
        Start = -1;
        Length = 0;
        InvalidateVisual();
    }

    public void SetRange(int start, int length)
    {
        Start = start;
        Length = length;
        InvalidateVisual();
    }

    private void Attach()
    {
        if (_target == null) return;

        // Track template changes too (TextPresenter/ScrollViewer can change).
        _target.TemplateApplied += Target_TemplateApplied;
        _target.LayoutUpdated += Target_LayoutUpdated;

        _sv = FindScrollViewer(_target);
        if (_sv != null)
        {
            _sv.PropertyChanged += Sv_PropertyChanged;
        }
    }

    private void Detach()
    {
        if (_target != null)
        {
            _target.TemplateApplied -= Target_TemplateApplied;
            _target.LayoutUpdated -= Target_LayoutUpdated;
        }

        if (_sv != null)
        {
            _sv.PropertyChanged -= Sv_PropertyChanged;
            _sv = null;
        }
    }

    private void Target_TemplateApplied(object? sender, Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        // ScrollViewer/TextPresenter may be recreated -> re-hook.
        if (_sv != null)
            _sv.PropertyChanged -= Sv_PropertyChanged;

        _sv = _target != null ? FindScrollViewer(_target) : null;
        if (_sv != null)
            _sv.PropertyChanged += Sv_PropertyChanged;

        InvalidateVisual();
    }

    private void Target_LayoutUpdated(object? sender, EventArgs e)
    {
        // Cheap + robust: if layout changes (wrapping/size), redraw.
        if (Start >= 0 && Length > 0)
            InvalidateVisual();
    }

    private void Sv_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // THIS is the real fix: scrolling changes Offset but doesn't automatically repaint overlay.
        if (e.Property == ScrollViewer.OffsetProperty ||
            e.Property == ScrollViewer.ViewportProperty ||
            e.Property == ScrollViewer.ExtentProperty)
        {
            if (Start >= 0 && Length > 0)
                InvalidateVisual();
        }
    }

    private static ScrollViewer? FindScrollViewer(Control c)
        => c.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var tb = _target;
        if (tb == null) return;
        if (Start < 0 || Length <= 0) return;

        var text = tb.Text ?? "";
        if (text.Length == 0) return;

        int start = Math.Clamp(Start, 0, text.Length);
        int len = Math.Clamp(Length, 0, text.Length - start);
        if (len <= 0) return;

        var presenter = tb.GetVisualDescendants().FirstOrDefault(v => v.GetType().Name == "TextPresenter");
        if (presenter == null) return;

        // --- CLIP TO TEXTBOX BOUNDS (prevents bleeding into other UI) ---
        var tbTopLeft = tb.TranslatePoint(new Point(0, 0), this);
        if (tbTopLeft == null) return;

        var clipRect = new Rect(tbTopLeft.Value, tb.Bounds.Size);

        using (context.PushClip(clipRect))
        {
            // (your existing reflection + rect drawing code stays the same)

            object? textLayoutObj = null;
            try
            {
                var t = presenter.GetType();
                var pTextLayout = t.GetProperty("TextLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pTextLayout != null)
                    textLayoutObj = pTextLayout.GetValue(presenter);

                if (textLayoutObj == null)
                {
                    var f = t.GetField("_textLayout", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (f != null)
                        textLayoutObj = f.GetValue(presenter);
                }
            }
            catch { return; }

            if (textLayoutObj == null) return;

            var mi = textLayoutObj.GetType().GetMethod("HitTestTextRange", new[] { typeof(int), typeof(int) });
            if (mi == null) return;

            object? rectsObj;
            try { rectsObj = mi.Invoke(textLayoutObj, new object[] { start, len }); }
            catch { return; }

            if (rectsObj is not System.Collections.IEnumerable rectsEnum)
                return;

            var origin = presenter.TranslatePoint(new Point(0, 0), this) ?? new Point(0, 0);

            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            {
                foreach (var rObj in rectsEnum)
                {
                    if (rObj is Rect r)
                    {
                        var rr = new Rect(r.X, r.Y, r.Width, r.Height);
                        context.FillRectangle(FillBrush, rr);
                        context.DrawRectangle(OutlinePen, rr);
                    }
                }
            }
        }
    }
}
