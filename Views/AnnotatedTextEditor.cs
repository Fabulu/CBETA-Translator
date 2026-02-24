// Views/AnnotatedTextEditor.cs
using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Views;

/// <summary>
/// Wraps AvaloniaEdit TextEditor and supports:
/// - clicking annotation superscript markers to open/select annotations
///
/// Search highlight support was intentionally removed.
/// Public highlight methods are kept as no-ops for compatibility.
/// </summary>
public sealed class AnnotatedTextEditor : UserControl
{
    private readonly TextEditor _editor;

    public event Action<DocAnnotation>? AnnotationClicked;

    public AnnotatedTextEditor()
    {
        _editor = new TextEditor
        {
            IsReadOnly = true
        };

        // Try to enable wrapping across versions (WordWrap / TextWrapping)
        TrySetWrapping(_editor, enable: true);

        Content = _editor;

        // click handling for markers
        _editor.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    public TextEditor Editor => _editor;

    /// <summary>
    /// Set this from outside; used to resolve marker clicks into annotations.
    /// </summary>
    public RenderedDocument? RenderedDocument { get; set; }

    public void SetText(string? text)
    {
        _editor.Document ??= new TextDocument();
        _editor.Document.Text = text ?? "";
    }

    // Compatibility no-ops (old search UI may still call these)
    public void ClearHighlight()
    {
        // intentionally no-op
    }

    public void SetHighlight(int start, int length)
    {
        // intentionally no-op
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var doc = RenderedDocument;
        if (doc == null) return;
        if (_editor.TextArea?.TextView == null) return;

        var tv = _editor.TextArea.TextView;
        var p = e.GetPosition(tv);

        int offset = TryGetDocumentOffsetFromPoint(tv, p);
        if (offset < 0) return;

        if (doc.TryGetAnnotationByMarkerAt(offset, out var ann))
        {
            AnnotationClicked?.Invoke(ann);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Convert a point in TextView coords to document offset.
    /// Uses reflection to support multiple AvaloniaEdit versions.
    /// Returns -1 if not possible.
    /// </summary>
    private int TryGetDocumentOffsetFromPoint(TextView textView, Point p)
    {
        try
        {
            textView.EnsureVisualLines();

            // 1) Try TextView.GetOffsetFromPoint(Point) -> int
            var mOffset = textView.GetType().GetMethod(
                "GetOffsetFromPoint",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Point) },
                modifiers: null);

            if (mOffset != null)
            {
                var r = mOffset.Invoke(textView, new object[] { p });
                if (r is int off1) return ClampOffset(off1, textView.Document);
            }

            // 2) Try TextView.GetPositionFromPoint(Point) -> object with Line/Column
            var mPos = textView.GetType().GetMethod(
                "GetPositionFromPoint",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Point) },
                modifiers: null);

            if (mPos != null)
            {
                var posObj = mPos.Invoke(textView, new object[] { p });
                if (posObj != null && textView.Document != null)
                {
                    int line = TryGetIntProperty(posObj, "Line");
                    int col = TryGetIntProperty(posObj, "Column");

                    if (line > 0 && col >= 0)
                    {
                        var doc = textView.Document;
                        var dl = doc.GetLineByNumber(line);
                        int off2 = dl.Offset + Math.Clamp(col, 0, dl.Length);
                        return ClampOffset(off2, doc);
                    }
                }
            }

            // 3) Fallback: approximate by nearest visual line + relative offset if available
            if (textView.VisualLines != null && textView.VisualLines.Count > 0 && textView.Document != null)
            {
                var vl = textView.VisualLines
                    .OrderBy(v => Math.Abs(v.VisualTop - p.Y))
                    .FirstOrDefault();

                if (vl != null)
                {
                    var mRel = vl.GetType().GetMethod(
                        "GetRelativeOffset",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        types: new[] { typeof(Point) },
                        modifiers: null);

                    if (mRel != null)
                    {
                        var relObj = mRel.Invoke(vl, new object[] { p });
                        if (relObj is int rel)
                        {
                            int lineLen = GetVisualLineLength(vl);
                            int relClamped = Math.Clamp(rel, 0, Math.Max(0, lineLen));

                            var firstLine = vl.FirstDocumentLine;
                            int baseOffset = firstLine?.Offset ?? 0;

                            int off3 = baseOffset + relClamped;
                            return ClampOffset(off3, textView.Document);
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return -1;
    }

    private static int GetVisualLineLength(VisualLine vl)
    {
        try
        {
            var t = vl.GetType();

            var pDocLen = t.GetProperty("DocumentLength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pDocLen?.GetValue(vl) is int dl) return dl;

            var pLen = t.GetProperty("Length", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pLen?.GetValue(vl) is int l) return l;

            var fDocLen = t.GetField("DocumentLength", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fDocLen?.GetValue(vl) is int fdl) return fdl;

            var fLen = t.GetField("Length", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fLen?.GetValue(vl) is int fl) return fl;
        }
        catch { }

        try
        {
            return vl.FirstDocumentLine?.Length ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int TryGetIntProperty(object obj, string name)
    {
        try
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p?.GetValue(obj) is int i) return i;
        }
        catch { }
        return -1;
    }

    private static int ClampOffset(int offset, TextDocument? doc)
    {
        if (doc == null) return offset;
        return Math.Clamp(offset, 0, doc.TextLength);
    }

    private static void TrySetWrapping(TextEditor editor, bool enable)
    {
        try
        {
            var t = editor.GetType();

            var pWordWrap = t.GetProperty("WordWrap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pWordWrap != null && pWordWrap.PropertyType == typeof(bool) && pWordWrap.CanWrite)
            {
                pWordWrap.SetValue(editor, enable);
                return;
            }

            var pTextWrapping = t.GetProperty("TextWrapping", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pTextWrapping != null && pTextWrapping.CanWrite)
            {
                var wrapValue = Enum.Parse(pTextWrapping.PropertyType, "Wrap", ignoreCase: true);
                pTextWrapping.SetValue(editor, wrapValue);
            }
        }
        catch
        {
            // wrapping is optional
        }
    }
}