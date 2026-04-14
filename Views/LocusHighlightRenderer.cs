// Views/LocusHighlightRenderer.cs
// AvaloniaEdit background renderer that highlights a single locus range
// with a pulsing accent color. Used in the Timeline tab text preview
// to show which part of the text was affected by the selected event.

using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace ReadZen.App.Views;

public sealed class LocusHighlightRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private int _start = -1;
    private int _length = 0;

    private static readonly IBrush HighlightFill = new SolidColorBrush(Color.FromArgb(70, 0, 180, 255));
    private static readonly IPen HighlightBorder = new Pen(new SolidColorBrush(Color.FromArgb(150, 0, 140, 255)), 1.5);

    public LocusHighlightRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetHighlight(int start, int length)
    {
        _start = start;
        _length = length;
        _textView.InvalidateVisual();
    }

    public void Clear()
    {
        _start = -1;
        _length = 0;
        _textView.InvalidateVisual();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_start < 0 || _length <= 0) return;
        if (textView.Document == null) return;

        int docLen = textView.Document.TextLength;
        if (docLen <= 0 || _start >= docLen) return;

        var end = Math.Min(_start + _length, docLen);
        if (end <= _start) return;

        var segment = new TextSegment { StartOffset = _start, Length = end - _start };

        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            drawingContext.FillRectangle(HighlightFill, rect);
            drawingContext.DrawRectangle(HighlightBorder, rect);
        }
    }
}
