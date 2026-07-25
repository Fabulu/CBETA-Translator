// Views/RowGridSurface.cs
//
// The unified row-grid reading surface (DESIGN-rowgrid.md, Wave C). A virtualized
// ListBox over RowVm rows: each row is a Border (block cues) wrapping a Grid
// [bar][id][ZH][EN] whose text cells are SelectableTextBlocks (per-cell selection +
// Ctrl-C, and a public TextLayout for the hover-dictionary in a later wave). Item
// selection is not the selection surface — the per-cell SelectableTextBlocks are — so
// container selection is neutralized visually.
//
// C5 (this file) makes the surface CONSUME the RowVm block-styling fields the builder
// stamps for the segment-map modes (AlignedBlocks / MergedStacked): SegType/IsHeader
// drive foreground tint, italic, a faint commentary wash and heading weight; Align
// centers verse (ZH side only); IndentEm indents dialogue; LeftBar paints a left accent
// bar; IsUnitStart paints a subtle top separator between AlignedBlocks units so grouped
// blocks read distinctly from the ungrouped AlignedLines grid. This ports the SPA
// .line-row--verse/--dialogue/--commentary/--heading/--dharani and .merged-seg--* looks
// (ZenLinkPage-spafix/style.css) and mirrors the desktop's own SegmentTypeTransformer
// palette so the grid and two-editor surfaces stay visually consistent. A row that
// carries no segment type (e.g. AlignedLines/Interleaved, or any text with no segment
// map) renders exactly as C1 did — every cue collapses to transparent/plain.
//
// Font zoom is honored via the surface-level ReaderFontSize bound into every cell.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Views;

/// <summary>
/// Virtualized bilingual row-grid reading surface. Bind <see cref="ReaderFontSize"/> to the
/// reader's font-zoom and set <see cref="ListBox.ItemsSource"/> to a RowGridModel's rows.
/// </summary>
public sealed class RowGridSurface : ListBox
{
    /// <summary>Surface-level font size fanned out to every cell (drives Ctrl+/- zoom).</summary>
    public static readonly StyledProperty<double> ReaderFontSizeProperty =
        AvaloniaProperty.Register<RowGridSurface, double>(nameof(ReaderFontSize), 14.0);

    public double ReaderFontSize
    {
        get => GetValue(ReaderFontSizeProperty);
        set => SetValue(ReaderFontSizeProperty, value);
    }

    // Matches the reader's serif/CJK stack (ReadableTabView.axaml line 23).
    private static readonly FontFamily ReaderFont = new(
        "Noto Serif CJK SC, Source Han Serif SC, SimSun, Songti SC, serif");

    /// <summary>
    /// Style this control as a <see cref="ListBox"/>. This is LOAD-BEARING: a ListBox subclass
    /// defaults its style key to its own type (<c>RowGridSurface</c>), so the Fluent
    /// <see cref="ListBox"/> ControlTheme — keyed to <c>typeof(ListBox)</c> — never matches and the
    /// control receives NO control template. With no template there is no ScrollViewer /
    /// VirtualizingStackPanel, no item container is ever realized, and every row renders blank in
    /// all four grid modes (the cell-render wiring below is correct but never runs because no cell
    /// is ever instantiated). Redirecting the style key to <see cref="ListBox"/> makes the theme
    /// apply and the surface virtualize + render like the app's other ListBoxes. Regression pinned
    /// by RowGridSurfaceRenderSmokeTests.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(ListBox);

    public RowGridSurface()
    {
        // Virtualize: texts can run to ~10k lines.
        ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

        // Item selection is NOT the selection surface (the per-cell SelectableTextBlocks
        // are). Avalonia's SelectionMode has no "None"; we keep Single and neutralize the
        // container's selected visual + padding below.
        SelectionMode = SelectionMode.Single;
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        FontFamily = ReaderFont;

        Styles.Add(NeutralizedItemStyle());
        // Fluent paints selection/hover on the item's inner ContentPresenter, which beats a
        // plain ListBoxItem Background setter — so kill the accent on the presenter itself. The
        // per-cell SelectableTextBlocks are the real selection surface (review m-1).
        Styles.Add(PresenterBrushStyle(":selected", Brushes.Transparent));
        Styles.Add(PresenterBrushStyle(":pointerover", Brushes.Transparent));
        // C4 copy-link button: subtle by default, brightened while the pointer is over its row.
        // Both are style-priority so the hover setter wins over the base one on match (opacity
        // is NOT set locally on the button, which would out-prioritize these).
        Styles.Add(CopyLinkOpacityStyle(hover: false, 0.4));
        Styles.Add(CopyLinkOpacityStyle(hover: true, 0.95));

        ItemTemplate = new FuncDataTemplate<RowVm>((_, _) => BuildRow(), supportsRecycling: true);
    }

    // ── Runtime instrumentation (best-effort safety net) ─────────────────────────────────────
    // Writes to <BaseDirectory>/rowgrid-render.log so a real launch pinpoints where a blank comes
    // from: the ItemsSource count on every (re)assignment (one line per mode switch), and a
    // one-shot "first row realized" line per ItemsSource carrying the rendered text length and the
    // render path taken (plain | find | merged). Entirely wrapped in try/catch — logging must never
    // affect rendering. Reset per ItemsSource set so each mode logs its own first-realized row.
    private static readonly object LogGate = new();
    private static volatile bool _loggedFirstRow;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            _loggedFirstRow = false; // new mode/model → allow one fresh "first row realized" line
            int count = -1;
            try
            {
                if (change.GetNewValue<System.Collections.IEnumerable?>() is { } items)
                {
                    count = 0;
                    foreach (var _ in items) count++;
                }
            }
            catch { /* counting is best-effort */ }
            TryLog($"ItemsSource set: count={count}");
        }
    }

    private static void TryLog(string line)
    {
        try
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "rowgrid-render.log");
            lock (LogGate)
                System.IO.File.AppendAllText(path,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}{Environment.NewLine}");
        }
        catch { /* instrumentation must never throw into the render/layout path */ }
    }

    /// <summary>Removes the ListBoxItem padding so cells sit flush.</summary>
    private static Style NeutralizedItemStyle()
    {
        var style = new Style(x => x.OfType<ListBoxItem>());
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(MinHeightProperty, 0.0));
        style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
        return style;
    }

    /// <summary>Transparent-background style for the item's templated ContentPresenter in a
    /// given pseudo-class state (:selected / :pointerover) — where Fluent draws the accent.</summary>
    private static Style PresenterBrushStyle(string pseudoClass, IBrush brush)
    {
        var style = new Style(x => x.OfType<ListBoxItem>().Class(pseudoClass)
            .Template().OfType<ContentPresenter>());
        style.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, brush));
        return style;
    }

    /// <summary>Opacity for the id-column copy-link button, either at rest (<paramref name="hover"/>
    /// false) or while the pointer is over the button's row (hover true, targeting the button as a
    /// visual descendant of the hovered ListBoxItem).</summary>
    private static Style CopyLinkOpacityStyle(bool hover, double opacity)
    {
        var selector = hover
            ? new Style(x => x.OfType<ListBoxItem>().Class(":pointerover")
                .Descendant().OfType<Button>().Class(CopyLinkClass))
            : new Style(x => x.OfType<Button>().Class(CopyLinkClass));
        selector.Setters.Add(new Setter(Visual.OpacityProperty, opacity));
        return selector;
    }

    /// <summary>Builds one [bar][id][ZH][EN] row template, wrapped in a Border that carries the
    /// block-level C5 cues (background tint, unit-boundary separator). Cells top-align, wrap, and
    /// take their font size from the surface so alignment (grid row max height) and zoom both
    /// work. The 3px bar column is present on EVERY row (transparent unless the row asks for a
    /// bar), so per-row cues never shift text horizontally out of column.</summary>
    private Control BuildRow()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3)));                     // left accent bar
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));                       // id
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));  // ZH / primary
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));  // EN

        // Left accent bar (verse/dialogue/commentary). Reserved on every row; painted only when
        // the row carries LeftBar, so text stays column-aligned regardless of segment type.
        var bar = new Border { VerticalAlignment = VerticalAlignment.Stretch };
        bar.Bind(Border.BackgroundProperty, new MultiBinding
        {
            Converter = BarBrushConverter.Instance,
            Bindings = { new Binding(nameof(RowVm.LeftBar)), new Binding(nameof(RowVm.SegType)) },
        });
        Grid.SetColumn(bar, 0);

        // Id column: an apparatus dot and a per-line copy-link button on the left, with the lb
        // label RIGHT-aligned in the trailing star column. Because the label is right-aligned and
        // the host width is fixed, the left-side dot/button never shift the label horizontally —
        // both slots are Auto and collapse when absent. Width widened from the C1 56px to fit the
        // extra glyph across all four grid modes (same in single-column Interleaved/MergedStacked).
        var idHost = new Grid { MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        idHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // apparatus dot
        idHost.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // copy-link button
        idHost.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star))); // lb label

        var apparatusDot = new Ellipse
        {
            Width = 5,
            Height = 5,
            Fill = ApparatusDotBrush,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 6, 3, 0),
        };
        apparatusDot.Bind(Visual.IsVisibleProperty, new Binding(nameof(RowVm.HasApparatus)));
        Grid.SetColumn(apparatusDot, 0);

        // C4: per-line copy-link (permalink "#"). The surface carries no app services, so it does
        // NOT copy anything itself — it raises the standard bubbling Button.Click, and the host
        // (ReadableTabView) resolves the row's lb → deep link → clipboard. Hidden for spacer/empty
        // rows via RowVm.CanCopyLink so no dead link is ever offered. Opacity is driven by the
        // row-hover styles above (not set locally), and it is not focusable/tab-stop.
        var copyLink = new Button
        {
            Classes = { CopyLinkClass },
            Tag = CopyLinkButtonTag,
            Content = "#",
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(0, 3, 2, 0),
            MinWidth = 0,
            MinHeight = 0,
            VerticalAlignment = VerticalAlignment.Top,
            Focusable = false,
            Cursor = new Cursor(StandardCursorType.Hand),
            FontFamily = ReaderFont,
        };
        ToolTip.SetTip(copyLink, "Copy link to this line");
        copyLink.Bind(Visual.IsVisibleProperty, new Binding(nameof(RowVm.CanCopyLink)));
        copyLink.Bind(TextElement.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));
        Grid.SetColumn(copyLink, 1);

        var id = new SelectableTextBlock
        {
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0.5,
            FontFamily = ReaderFont,
        };
        id.Bind(SelectableTextBlock.TextProperty, new Binding(nameof(RowVm.IdLabel)));
        id.Bind(SelectableTextBlock.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));
        Grid.SetColumn(id, 2);

        idHost.Children.Add(apparatusDot);
        idHost.Children.Add(copyLink);
        idHost.Children.Add(id);
        Grid.SetColumn(idHost, 1);

        // Primary content cell (column 2). For two-column rows this is the ZH text; for
        // single-column rows (Interleaved) it carries the row's own text and spans column 3 so
        // there is no empty right-hand gutter (PrimaryText/PrimaryColumnSpan on RowVm).
        var zh = MakePrimaryCell();
        Grid.SetColumn(zh, 2);
        zh.Bind(Grid.ColumnSpanProperty, new Binding(nameof(RowVm.PrimaryColumnSpan)));
        // C2 find highlights: the primary cell shows ZH normally, or EN when collapsed to an
        // EN-only view / a single-column EN row — so it picks the matching highlight list.
        // I3 Zen-dictionary underlines apply only while the primary cell shows ZH (the spans
        // index into ZhText, so they must never decorate a collapsed-EN cell).
        WireHighlightableText(zh, r => r.PrimaryText,
            r => r.PrimaryIsZh ? r.ZhHighlights : r.EnHighlights,
            r => r.PrimaryIsZh ? r.ZenHighlights : Array.Empty<Hspan>());

        // EN column (column 3) — shown only for two-column rows; hidden for single-column so
        // the primary cell's span fills its slot.
        var en = MakeCell(new Thickness(0, 0, 0, 4));
        Grid.SetColumn(en, 3);
        WireHighlightableText(en, r => r.EnText, r => r.EnHighlights);
        en.Bind(Visual.IsVisibleProperty, new Binding(nameof(RowVm.ShowEnColumn)));
        // Headings carry weight on both columns; other cues are source-side (see MakePrimaryCell).
        en.Bind(SelectableTextBlock.FontWeightProperty,
            new Binding(nameof(RowVm.IsHeader)) { Converter = HeaderWeightConverter.Instance });

        grid.Children.Add(bar);
        grid.Children.Add(idHost);
        grid.Children.Add(zh);
        grid.Children.Add(en);

        // Row root: block-level tint + a subtle top separator at each AlignedBlocks unit start.
        var root = new Border
        {
            BorderBrush = UnitSeparatorBrush,
            Child = grid,
        };
        // Background is the segment-type tint normally, but a nav/deep-link/bookmark pulse
        // (IsNavHighlighted) overrides it with the highlight wash for its lifetime (C3).
        root.Bind(Border.BackgroundProperty, new MultiBinding
        {
            Converter = RowBackgroundConverter.Instance,
            Bindings = { new Binding(nameof(RowVm.SegType)), new Binding(nameof(RowVm.IsNavHighlighted)) },
        });
        root.Bind(Border.BorderThicknessProperty,
            new Binding(nameof(RowVm.IsUnitStart)) { Converter = UnitSeparatorConverter.Instance });
        return root;
    }

    /// <summary>The primary (ZH/left) cell, carrying the source-side C5 cues: foreground tint and
    /// italic (verse/dharani/byline), verse centering, heading weight and dialogue indent — each
    /// gated so it never re-styles a translation shown in the primary slot (RowVm.PrimaryIsZh).</summary>
    private SelectableTextBlock MakePrimaryCell()
    {
        var cell = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            FontFamily = ReaderFont,
            // Tag identifies the ZH/primary content cell for the surface-level on-click dictionary
            // (only ZH primary cells resolve a term); the text itself is set by WireHighlightableText.
            Tag = PrimaryCellTag,
        };
        cell.Bind(SelectableTextBlock.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));

        // Dialogue indent: left margin = IndentEm * fontSize (0 for every other type). Keeps the
        // base 12px right / 4px bottom gutter of the C1 primary cell.
        cell.Bind(Layoutable.MarginProperty, new MultiBinding
        {
            Converter = IndentMarginConverter.Instance,
            Bindings =
            {
                new Binding(nameof(RowVm.IndentEm)),
                new Binding(nameof(ReaderFontSize)) { Source = this },
            },
        });

        // Verse centering — ZH side only (PrimaryIsZh gates it so a collapsed EN-only view or a
        // single-column EN row is never centered).
        cell.Bind(SelectableTextBlock.TextAlignmentProperty, new MultiBinding
        {
            Converter = PrimaryAlignConverter.Instance,
            Bindings = { new Binding(nameof(RowVm.Align)), new Binding(nameof(RowVm.PrimaryIsZh)) },
        });

        // Foreground tint (verse gold, dialogue blue, commentary gray, dharani orchid,
        // preface/colophon/byline muted) — ZH side only; default (UnsetValue) keeps the theme color.
        cell.Bind(SelectableTextBlock.ForegroundProperty, new MultiBinding
        {
            Converter = PrimaryForegroundConverter.Instance,
            Bindings = { new Binding(nameof(RowVm.SegType)), new Binding(nameof(RowVm.PrimaryIsZh)) },
        });

        // Italic (verse / dharani / byline) — ZH side only.
        cell.Bind(SelectableTextBlock.FontStyleProperty, new MultiBinding
        {
            Converter = PrimaryFontStyleConverter.Instance,
            Bindings = { new Binding(nameof(RowVm.SegType)), new Binding(nameof(RowVm.PrimaryIsZh)) },
        });

        // Heading weight — side-agnostic (a heading row is bold whichever text it shows).
        cell.Bind(SelectableTextBlock.FontWeightProperty,
            new Binding(nameof(RowVm.IsHeader)) { Converter = HeaderWeightConverter.Instance });

        return cell;
    }

    private SelectableTextBlock MakeCell(Thickness margin)
    {
        var cell = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = margin,
            FontFamily = ReaderFont,
        };
        // Text is set by WireHighlightableText (below in BuildRow) so find highlights render.
        cell.Bind(SelectableTextBlock.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));
        return cell;
    }

    /// <summary>Tag marking the ZH/primary content cell (target of the on-click grid dictionary).</summary>
    internal const string PrimaryCellTag = "rowgrid-primary";

    /// <summary>Tag marking the id-column copy-link button, so the host can identify a bubbling
    /// Button.Click (C4 per-line copy link) and skip the surface-level press handling for it.</summary>
    internal const string CopyLinkButtonTag = "rowgrid-copylink";

    /// <summary>Style class on the copy-link button (drives the base/row-hover opacity styles).</summary>
    private const string CopyLinkClass = "row-copylink";

    // ── C2: find-in-page highlight rendering ─────────────────────────────────────────────────
    // Match colors mirror the two-editor FindHighlightTransformer so both surfaces read the same.
    private static readonly IBrush FindMatchBg = new SolidColorBrush(Color.FromArgb(100, 255, 215, 0));
    private static readonly IBrush FindCurrentBg = new SolidColorBrush(Color.FromArgb(180, 255, 165, 0));

    // ── I3: Zen-dictionary term underline ───────────────────────────────────────────────────
    // Mirrors the SPA reader's .zen-term look (a quiet dashed underline meaning exactly
    // "this span has a Zen dictionary entry — click it"). Muted gold, dashed, kept subtle so
    // running text stays calm; find highlights (background) compose with it independently.
    private static readonly TextDecorationCollection ZenUnderline = new()
    {
        new TextDecoration
        {
            Location = TextDecorationLocation.Underline,
            Stroke = new SolidColorBrush(Color.FromArgb(0xA0, 212, 171, 88)),
            StrokeThickness = 1.2,
            StrokeThicknessUnit = TextDecorationUnit.Pixel,
            StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 2, 2 },
        },
    };

    /// <summary>
    /// Wires a cell so its text is rebuilt as highlighted inlines whenever the row's find or
    /// Zen-term spans change. Rows are virtualized/recycled, so this subscribes to the CURRENT
    /// DataContext's PropertyChanged and always detaches the previous one first — at most one
    /// live subscription per reused cell, so recycling never leaks or mis-targets. A row with
    /// no spans renders a single plain run (byte-identical to the old Text binding).
    /// </summary>
    private static void WireHighlightableText(SelectableTextBlock cell,
        Func<RowVm, string> textSel, Func<RowVm, IReadOnlyList<Hspan>> spanSel,
        Func<RowVm, IReadOnlyList<Hspan>>? zenSel = null)
    {
        RowVm? bound = null;
        PropertyChangedEventHandler onProp = (_, e) =>
        {
            if (e.PropertyName == nameof(RowVm.ZhHighlights) || e.PropertyName == nameof(RowVm.EnHighlights)
                || e.PropertyName == nameof(RowVm.ZenHighlights))
                RenderHighlightedCell(cell, bound, textSel, spanSel, zenSel);
        };
        cell.DataContextChanged += (_, _) =>
        {
            if (bound != null) bound.PropertyChanged -= onProp;
            bound = cell.DataContext as RowVm;
            if (bound != null) bound.PropertyChanged += onProp;
            RenderHighlightedCell(cell, bound, textSel, spanSel, zenSel);
        };
    }

    private static void RenderHighlightedCell(SelectableTextBlock cell, RowVm? row,
        Func<RowVm, string> textSel, Func<RowVm, IReadOnlyList<Hspan>> spanSel,
        Func<RowVm, IReadOnlyList<Hspan>>? zenSel)
    {
        if (row == null) { SetPlainText(cell, ""); return; }
        var text = textSel(row) ?? "";
        var spans = spanSel(row);
        var zen = zenSel?.Invoke(row);
        bool hasFind = spans is { Count: > 0 };
        bool hasZen = zen is { Count: > 0 };

        // One-shot per-mode instrumentation: pin that the first row actually got realized and which
        // render path it took, so a blank surface in the wild is diagnosable from the log alone.
        if (!_loggedFirstRow && text.Length > 0)
        {
            _loggedFirstRow = true;
            string path = hasZen ? "merged" : hasFind ? "find" : "plain";
            TryLog($"first row realized: text len = {text.Length}, path = {path}");
        }

        if (!hasFind && !hasZen) { SetPlainText(cell, text); return; }

        int len = text.Length;

        // Fast path: no Zen underlines — keep the original find-only rendering verbatim.
        if (!hasZen)
        {
            var inlines = ResetCellInlines(cell);
            int pos = 0;
            foreach (var s in spans!)
            {
                int start = s.Start;
                int end = s.Start + s.Length;
                if (start < 0) start = 0;
                if (end > len) end = len;
                if (end <= start || start < pos) continue; // clamp / skip out-of-order overlaps
                if (start > pos) inlines.Add(new Run(text.Substring(pos, start - pos)));
                inlines.Add(new Run(text.Substring(start, end - start))
                {
                    Background = s.IsCurrent ? FindCurrentBg : FindMatchBg,
                });
                pos = end;
            }
            if (pos < len) inlines.Add(new Run(text.Substring(pos)));
            return;
        }

        // Merged path: cut the text at every find/zen span boundary, then emit one run per
        // segment carrying the union of its states (find background, zen underline). The cut
        // set keeps runs minimal; spans are clamped so a stale index can never throw.
        var cuts = new SortedSet<int> { 0, len };
        void AddSpanCuts(IReadOnlyList<Hspan>? list)
        {
            if (list == null) return;
            foreach (var s in list)
            {
                int a = Math.Clamp(s.Start, 0, len);
                int b = Math.Clamp(s.Start + s.Length, 0, len);
                if (b > a) { cuts.Add(a); cuts.Add(b); }
            }
        }
        AddSpanCuts(spans);
        AddSpanCuts(zen);

        var merged = ResetCellInlines(cell);
        int prev = -1;
        foreach (var cut in cuts)
        {
            if (prev >= 0 && cut > prev)
            {
                var run = new Run(text.Substring(prev, cut - prev));

                if (hasFind)
                {
                    foreach (var s in spans!)
                    {
                        if (s.Start <= prev && s.Start + s.Length >= cut && s.Length > 0)
                        {
                            run.Background = s.IsCurrent ? FindCurrentBg : FindMatchBg;
                            if (s.IsCurrent) break;
                        }
                    }
                }

                foreach (var z in zen!)
                {
                    if (z.Start <= prev && z.Start + z.Length >= cut && z.Length > 0)
                    {
                        run.TextDecorations = ZenUnderline;
                        break;
                    }
                }

                merged.Add(run);
            }
            prev = cut;
        }
    }

    /// <summary>
    /// Returns the cell's OWN inline collection, cleared and ready to receive runs, with Text
    /// cleared so it can never shadow the inlines. Runs MUST be added to this returned collection
    /// (not to a detached one that is then assigned) — that is what makes the text render: in
    /// Avalonia 11.3 assigning an already-populated <see cref="InlineCollection"/> to
    /// <see cref="TextBlock.Inlines"/> does not raise the per-item Invalidated event that triggers
    /// a text-layout rebuild, so the cell measures empty and renders blank. Adding to the cell's
    /// own (host-wired) collection fires Invalidated → re-measure. This mirrors the working
    /// WitnessComparisonPanel / EditionProcessDialog idiom and was the fix for every RowGrid row
    /// rendering blank once Zen-term underlines routed normal reading through the inline path.
    /// </summary>
    private static InlineCollection ResetCellInlines(SelectableTextBlock cell)
    {
        cell.Text = null; // Text would shadow an empty run set; keep inlines authoritative
        var inlines = cell.Inlines;
        if (inlines is null)
        {
            inlines = new InlineCollection();
            cell.Inlines = inlines;
        }
        else
        {
            inlines.Clear();
        }
        return inlines;
    }

    private static void SetPlainText(SelectableTextBlock cell, string text)
    {
        var inlines = cell.Inlines;
        if (inlines is { Count: > 0 }) inlines.Clear();
        cell.Text = text;
    }

    // ── C5 palette ─────────────────────────────────────────────────────────────────────────
    // Foreground tints mirror Infrastructure/SegmentTypeTransformer (the two-editor surface's
    // segment styling) so both surfaces read the same; the bar/background alphas mirror the SPA
    // style.css §"Semantic segment type styling". Kept subtle by design.
    private static readonly IBrush VerseFg = new SolidColorBrush(Color.FromRgb(218, 165, 32));   // goldenrod
    private static readonly IBrush DialogueFg = new SolidColorBrush(Color.FromRgb(100, 149, 237)); // cornflower blue
    private static readonly IBrush CommentaryFg = new SolidColorBrush(Color.FromRgb(160, 160, 160)); // gray
    private static readonly IBrush DharaniFg = new SolidColorBrush(Color.FromRgb(186, 85, 211));  // medium orchid
    private static readonly IBrush MutedFg = new SolidColorBrush(Color.FromRgb(140, 140, 140));   // dim gray
    private static readonly IBrush BylineFg = new SolidColorBrush(Color.FromRgb(112, 128, 144));  // slate gray

    private static readonly IBrush VerseBar = new SolidColorBrush(Color.FromArgb(0x73, 212, 171, 88));
    private static readonly IBrush DialogueBar = new SolidColorBrush(Color.FromArgb(0x4D, 100, 149, 237));
    private static readonly IBrush CommentaryBar = new SolidColorBrush(Color.FromArgb(0x66, 139, 123, 105));

    private static readonly IBrush VerseBg = new SolidColorBrush(Color.FromArgb(0x0D, 212, 171, 88));
    private static readonly IBrush DialogueBg = new SolidColorBrush(Color.FromArgb(0x0D, 212, 171, 88));
    private static readonly IBrush CommentaryBg = new SolidColorBrush(Color.FromArgb(0x14, 160, 160, 160)); // == transformer wash
    private static readonly IBrush UnitSeparatorBrush = new SolidColorBrush(Color.FromArgb(0x33, 128, 128, 128));

    // Apparatus dot (id column): neutral mid-grey, matching the two-editor apparatus
    // marker scheme (grey = CBETA apparatus; yellow = masters, blue = our comments).
    private static readonly IBrush ApparatusDotBrush = new SolidColorBrush(Color.FromRgb(154, 154, 154));
    // Nav / deep-link / bookmark pulse wash — a soft accent that reads over any segment tint.
    private static readonly IBrush NavHighlightBg = new SolidColorBrush(Color.FromArgb(0x40, 100, 149, 237));

    private static bool Is(object? v, string name)
        => v is string s && string.Equals(s, name, StringComparison.OrdinalIgnoreCase);

    // ── Converters ─────────────────────────────────────────────────────────────────────────

    /// <summary>[LeftBar(bool), SegType(string)] → left accent-bar brush (Transparent when off).</summary>
    private sealed class BarBrushConverter : IMultiValueConverter
    {
        public static readonly BarBrushConverter Instance = new();
        public object Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Segment-type accent bars removed — reading text carries no structural
            // color; only annotation markers (masters/comments/CBETA) + highlights tint.
            return Brushes.Transparent;
        }
    }

    /// <summary>[SegType(string), IsNavHighlighted(bool)] → row background: the nav pulse wash when
    /// highlighted, else the segment-type tint (Transparent for plain rows).</summary>
    private sealed class RowBackgroundConverter : IMultiValueConverter
    {
        public static readonly RowBackgroundConverter Instance = new();
        public object Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Nav pulse highlight kept (functional); segment-type background tints removed.
            if (values.Count > 1 && values[1] is true) return NavHighlightBg;
            return Brushes.Transparent;
        }
    }

    /// <summary>IsUnitStart(bool) → top-only BorderThickness for the unit separator.</summary>
    private sealed class UnitSeparatorConverter : IValueConverter
    {
        public static readonly UnitSeparatorConverter Instance = new();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? new Thickness(0, 1, 0, 0) : new Thickness(0);
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>IsHeader(bool) → FontWeight (Bold / Normal).</summary>
    private sealed class HeaderWeightConverter : IValueConverter
    {
        public static readonly HeaderWeightConverter Instance = new();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? FontWeight.Bold : FontWeight.Normal;
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BindingOperations.DoNothing;
    }

    /// <summary>[IndentEm(double), fontSize(double)] → left-inset Thickness (dialogue indent),
    /// keeping the primary cell's base 12px-right / 4px-bottom gutter.</summary>
    private sealed class IndentMarginConverter : IMultiValueConverter
    {
        public static readonly IndentMarginConverter Instance = new();
        public object Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            double em = values.Count > 0 && values[0] is double d ? d : 0.0;
            double fs = values.Count > 1 && values[1] is double f ? f : 14.0;
            double left = em > 0 ? em * fs : 0.0;
            return new Thickness(left, 0, 12, 4);
        }
    }

    /// <summary>[Align(RowAlign), PrimaryIsZh(bool)] → TextAlignment. Center only for a verse row
    /// whose primary cell shows ZH; Left otherwise (never centers a translation).</summary>
    private sealed class PrimaryAlignConverter : IMultiValueConverter
    {
        public static readonly PrimaryAlignConverter Instance = new();
        public object Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool primaryZh = values.Count > 1 && values[1] is true;
            bool center = values.Count > 0 && values[0] is RowAlign.Center;
            return primaryZh && center ? TextAlignment.Center : TextAlignment.Left;
        }
    }

    /// <summary>[SegType(string), PrimaryIsZh(bool)] → source-side foreground tint, or UnsetValue
    /// (keep the theme foreground) for plain rows or when the primary cell shows a translation.</summary>
    private sealed class PrimaryForegroundConverter : IMultiValueConverter
    {
        public static readonly PrimaryForegroundConverter Instance = new();
        public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Segment-type foreground tints removed — reading text uses the default
            // color; only annotation markers (masters/comments/CBETA) + highlights tint it.
            return AvaloniaProperty.UnsetValue;
        }
    }

    /// <summary>[SegType(string), PrimaryIsZh(bool)] → FontStyle. Italic for verse / dharani /
    /// byline on the ZH side; Normal otherwise.</summary>
    private sealed class PrimaryFontStyleConverter : IMultiValueConverter
    {
        public static readonly PrimaryFontStyleConverter Instance = new();
        public object Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            // Segment-type italics removed — reading text keeps a uniform upright face.
            return FontStyle.Normal;
        }
    }
}
