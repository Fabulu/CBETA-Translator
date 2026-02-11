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
/// </summary>
public sealed class SearchHighlightOverlay : Control
{
    public TextBox? Target { get; set; }

    public int Start { get; private set; } = -1;
    public int Length { get; private set; } = 0;

    private static readonly IBrush FillBrush = new SolidColorBrush(Color.Parse("#66FFD54A")); // semi
    private static readonly IPen OutlinePen = new Pen(new SolidColorBrush(Color.Parse("#CCFFD54A")), 1);

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

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var tb = Target;
        if (tb == null) return;
        if (Start < 0 || Length <= 0) return;

        var text = tb.Text ?? "";
        if (text.Length == 0) return;

        int start = Math.Clamp(Start, 0, text.Length);
        int len = Math.Clamp(Length, 0, text.Length - start);
        if (len <= 0) return;

        var presenter = tb.GetVisualDescendants().FirstOrDefault(v => v.GetType().Name == "TextPresenter");
        if (presenter == null) return;

        // Try to get a TextLayout instance from the presenter (internal API; reflection).
        object? textLayoutObj = null;

        try
        {
            var t = presenter.GetType();

            // Common patterns in Avalonia:
            // - property "TextLayout" (public/internal)
            // - private field "_textLayout"
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
        catch
        {
            return;
        }

        if (textLayoutObj == null) return;

        // HitTestTextRange exists on Avalonia.Media.TextFormatting.TextLayout.
        var mi = textLayoutObj.GetType().GetMethod("HitTestTextRange", new[] { typeof(int), typeof(int) });
        if (mi == null) return;

        object? rectsObj;
        try
        {
            rectsObj = mi.Invoke(textLayoutObj, new object[] { start, len });
        }
        catch
        {
            return;
        }

        if (rectsObj is not System.Collections.IEnumerable rectsEnum)
            return;

        // Translate presenter origin into overlay coordinates
        var origin = presenter.TranslatePoint(new Point(0, 0), this) ?? new Point(0, 0);

        using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
        {
            foreach (var rObj in rectsEnum)
            {
                if (rObj is Rect r)
                {
                    // Expand slightly for visibility
                    var rr = new Rect(r.X, r.Y, r.Width, r.Height);
                    context.FillRectangle(FillBrush, rr);
                    context.DrawRectangle(OutlinePen, rr);
                }
            }
        }
    }
}
