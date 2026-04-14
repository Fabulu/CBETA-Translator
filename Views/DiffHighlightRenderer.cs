// Views/DiffHighlightRenderer.cs
// AvaloniaEdit background renderer that highlights diff spans (added text in green).
// Used when viewing historical versions in Reader and Compare to show what changed.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Views;

public sealed class DiffHighlightRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private List<DiffSpan> _spans = new();

    private static readonly IBrush AddedFill = new SolidColorBrush(Color.FromArgb(50, 0, 200, 0));
    private static readonly IBrush RemovedFill = new SolidColorBrush(Color.FromArgb(50, 220, 0, 0));

    public DiffHighlightRenderer(TextView textView)
    {
        _textView = textView ?? throw new ArgumentNullException(nameof(textView));
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void SetSpans(List<DiffSpan> spans)
    {
        _spans = spans ?? new();
        _textView.InvalidateVisual();
    }

    public void Clear()
    {
        _spans = new();
        _textView.InvalidateVisual();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_spans.Count == 0) return;
        if (textView.Document == null) return;

        int docLen = textView.Document.TextLength;
        if (docLen <= 0) return;

        foreach (var span in _spans)
        {
            if (span.Start < 0 || span.Start >= docLen) continue;
            var end = Math.Min(span.Start + span.Length, docLen);
            if (end <= span.Start) continue;

            var brush = span.Kind == DiffKind.Added ? AddedFill : RemovedFill;

            var segment = new AvaloniaEdit.Document.TextSegment
            {
                StartOffset = span.Start,
                Length = end - span.Start,
            };

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.FillRectangle(brush, rect);
            }
        }
    }
}
