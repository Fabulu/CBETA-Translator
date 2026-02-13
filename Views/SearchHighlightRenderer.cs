using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CbetaTranslator.App.Views;

/// <summary>
/// Highlight a single text range in AvaloniaEdit without touching selection.
/// Draws behind text using TextView background rendering.
/// </summary>
public sealed class SearchHighlightRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private int _start = -1;
    private int _length = 0;

    private static readonly IBrush Fill = new SolidColorBrush(Color.Parse("#66FFD54A"));
    private static readonly IPen Outline = new Pen(new SolidColorBrush(Color.Parse("#CCFFD54A")), 1);

    public SearchHighlightRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public KnownLayer Layer => KnownLayer.Selection; // behind caret, above background

    public void Clear()
    {
        _start = -1;
        _length = 0;
        _textView.InvalidateVisual();
    }

    public void SetRange(int start, int length)
    {
        _start = start;
        _length = length;
        _textView.InvalidateVisual();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_start < 0 || _length <= 0) return;
        if (textView.Document == null) return;

        int docLen = textView.Document.TextLength;
        if (docLen <= 0) return;

        int start = Math.Clamp(_start, 0, docLen);
        int end = Math.Clamp(_start + _length, 0, docLen);
        if (end <= start) return;

        textView.EnsureVisualLines();

        var seg = new SimpleSegment(start, end - start);

        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, seg))
        {
            drawingContext.FillRectangle(Fill, rect);
            drawingContext.DrawRectangle(Outline, rect);
        }
    }
}
