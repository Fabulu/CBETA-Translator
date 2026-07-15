// Views/RowGridSurface.cs
//
// The unified row-grid reading surface (DESIGN-rowgrid.md, Wave C). A virtualized
// ListBox over RowVm rows: each row is a Grid [id][ZH][EN] whose cells are
// SelectableTextBlocks (per-cell selection + Ctrl-C, and a public TextLayout for the
// hover-dictionary in a later wave). Item selection is not the selection surface — the
// per-cell SelectableTextBlocks are — so container selection is neutralized visually.
//
// C1 renders ZH + EN text and an optional id label only. No per-line copy-link (C4),
// no block styling (C5), no apparatus dot (C3). Font zoom is honored via the
// surface-level ReaderFontSize bound into every cell.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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
        "Georgia, Noto Serif, Noto Serif CJK SC, Source Han Serif SC, SimSun, Songti SC, serif");

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

        ItemTemplate = new FuncDataTemplate<RowVm>((_, _) => BuildRow(), supportsRecycling: true);
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

    /// <summary>Builds one [id][ZH][EN] row template. Cells top-align, wrap, and take their
    /// font size from the surface so alignment (grid row max height) and zoom both work.</summary>
    private Control BuildRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,*"),
        };

        var id = new SelectableTextBlock
        {
            MinWidth = 56,                       // lock the id column so rows don't jitter
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0.5,
            Margin = new Thickness(0, 0, 8, 0),
            FontFamily = ReaderFont,
        };
        id.Bind(SelectableTextBlock.TextProperty, new Binding(nameof(RowVm.IdLabel)));
        id.Bind(SelectableTextBlock.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));
        Grid.SetColumn(id, 0);

        // Primary content cell (column 1). For two-column rows this is the ZH text; for
        // single-column rows (Interleaved) it carries the row's own text and spans column 2 so
        // there is no empty right-hand gutter (PrimaryText/PrimaryColumnSpan on RowVm).
        var zh = MakeCell(nameof(RowVm.PrimaryText), new Thickness(0, 0, 12, 4));
        Grid.SetColumn(zh, 1);
        zh.Bind(Grid.ColumnSpanProperty, new Binding(nameof(RowVm.PrimaryColumnSpan)));

        // EN column (column 2) — shown only for two-column rows; hidden for single-column so
        // the primary cell's span fills its slot.
        var en = MakeCell(nameof(RowVm.EnText), new Thickness(0, 0, 0, 4));
        Grid.SetColumn(en, 2);
        en.Bind(Visual.IsVisibleProperty, new Binding(nameof(RowVm.ShowEnColumn)));

        grid.Children.Add(id);
        grid.Children.Add(zh);
        grid.Children.Add(en);
        return grid;
    }

    private SelectableTextBlock MakeCell(string textPath, Thickness margin)
    {
        var cell = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = margin,
            FontFamily = ReaderFont,
        };
        cell.Bind(SelectableTextBlock.TextProperty, new Binding(textPath));
        cell.Bind(SelectableTextBlock.FontSizeProperty, this.GetObservable(ReaderFontSizeProperty));
        return cell;
    }
}
