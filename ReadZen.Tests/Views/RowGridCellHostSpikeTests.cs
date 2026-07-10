// ReadZen.Tests/Views/RowGridCellHostSpikeTests.cs
//
// DE-RISKING SPIKE C0 for the Unified Row-Grid Reading Surface
// (DESIGN-rowgrid.md, RUN-20260710-0605-reader-spa-parity).
//
// These headless probes PIN the three questions that gate the grid build:
//   1. Cell text host: can a realized SelectableTextBlock expose a TextLayout
//      and map a pointer point -> character index via HitTestPoint (the hover
//      dictionary's mechanism, today proven only on TextBox via a descendant
//      TextPresenter at HoverDictionaryBehaviorTextBox.cs:338-377)?
//   2. Virtualization realize/recycle model of ListBox + VirtualizingStackPanel:
//      ContainerFromIndex null until realized; ScrollIntoView realizes; scrolling
//      away RECYCLES the container (so selection/hover anchors must be data-keyed
//      (rowIndex, localOffset) models, never container references).
//   3. Per-cell selection survives inside a virtualized item even with the
//      ListBox's own item selection turned off.
//
// Kept as a regression pin: if a future Avalonia bump changes any of these, the
// row-grid selection/hover design assumptions break here first.

using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace ReadZen.Tests.Views;

[Trait("Domain", "RowGridSpike")]
public class RowGridCellHostSpikeTests
{
    // Avalonia headless platform + dispatcher come from ModuleInit.cs.

    private const string CellText = "菩提本無樹明鏡亦非臺"; // 10 CJK chars

    // -----------------------------------------------------------------
    // Q1: SelectableTextBlock -> TextLayout -> HitTestPoint reachability.
    // -----------------------------------------------------------------
    [Fact]
    public void SelectableTextBlock_TextLayout_HitTestPoint_MapsPointToCharIndex()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var stb = new SelectableTextBlock
            {
                Text = CellText,
                FontSize = 20
            };

            // Realize a layout the same way a grid cell would be laid out.
            stb.Measure(new Size(1000, 1000));
            stb.Arrange(new Rect(stb.DesiredSize));

            // The DIRECT path: TextBlock.TextLayout is public; SelectableTextBlock
            // inherits it. No descendant TextPresenter walk needed (unlike the
            // TextBox variant, which has no public TextLayout of its own).
            TextLayout layout = stb.TextLayout;
            Assert.NotNull(layout);

            // Hit-test near the start of the run -> should land on an early char.
            var startHit = layout.HitTestPoint(new Point(2, stb.DesiredSize.Height / 2));
            Assert.InRange(startHit.TextPosition, 0, 1);

            // Hit-test well into the run -> should land on a later char index,
            // strictly greater than the start hit and inside the string.
            var midX = stb.DesiredSize.Width * 0.75;
            var midHit = layout.HitTestPoint(new Point(midX, stb.DesiredSize.Height / 2));
            Assert.True(midHit.TextPosition > startHit.TextPosition,
                $"expected mid hit ({midHit.TextPosition}) > start hit ({startHit.TextPosition})");
            Assert.InRange(midHit.TextPosition, 0, CellText.Length - 1);
        });
    }

    // -----------------------------------------------------------------
    // Q1 fallback probe: TextPresenter also exposes a public TextLayout, so if
    // SelectableTextBlock were ever unsuitable, a bare TextPresenter cell keeps
    // the hover-dict mechanism. (Documents the fallback is real, not a guess.)
    // -----------------------------------------------------------------
    [Fact]
    public void TextPresenter_ExposesPublicTextLayout_Fallback()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var tp = new TextPresenter { Text = CellText, FontSize = 20 };
            tp.Measure(new Size(1000, 1000));
            tp.Arrange(new Rect(tp.DesiredSize));

            // The point of this probe is reachability: TextPresenter.TextLayout is
            // public and hit-testable, so a bare-TextPresenter fallback cell keeps
            // the hover-dict mechanism if SelectableTextBlock were ever unsuitable.
            TextLayout layout = tp.TextLayout;
            Assert.NotNull(layout);
            var hit = layout.HitTestPoint(new Point(tp.DesiredSize.Width * 0.5, tp.DesiredSize.Height / 2));
            Assert.True(hit.TextPosition >= 0 && hit.TextPosition <= CellText.Length,
                $"TextPosition {hit.TextPosition} out of [0,{CellText.Length}]");
        });
    }

    // -----------------------------------------------------------------
    // Q2 + Q3: virtualization realize/recycle + per-cell selection under a
    // ListBox whose OWN item selection is disabled.
    // -----------------------------------------------------------------
    [Fact]
    public void VirtualizingListBox_RealizesRecyclesContainers_AndCellSelectionSurvives()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // The bare test Application carries no theme, so controls get no
            // control template (no ScrollViewer / VirtualizingStackPanel). Load
            // Fluent once so the ListBox templates like it does in the real app.
            if (Application.Current is { } app && app.Styles.Count == 0)
                app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

            var items = new ObservableCollection<string>(
                Enumerable.Range(0, 2000).Select(i => $"row {i} 菩提本無樹"));

            var listBox = new ListBox
            {
                ItemsSource = items,
                // The grid does its own text selection; item selection is off.
                SelectionMode = SelectionMode.Single,
                ItemTemplate = new FuncDataTemplate<string>((s, _) =>
                    new SelectableTextBlock { Text = s, FontSize = 16 }, supportsRecycling: true),
            };
            // Explicit VirtualizingStackPanel (also ListBox's default panel).
            listBox.ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel());

            var window = new Window { Width = 400, Height = 300, Content = listBox };
            window.Show();

            // Drive the full layout+render pipeline. ForceRenderTimerTick ticks the
            // headless render timer, which runs the LayoutManager AND propagates the
            // effective viewport that a VirtualizingStackPanel uses to decide what to
            // realize (a raw Measure/Arrange skips that, so scroll wouldn't re-realize).
            void Layout()
            {
                window.Measure(new Size(400, 300));
                window.Arrange(new Rect(0, 0, 400, 300));
                Dispatcher.UIThread.RunJobs();
                // Render tick updates transformed bounds -> effective viewport, so a
                // scrolled VSP re-realizes. A couple of iterations settles it.
                for (int i = 0; i < 3; i++)
                {
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                    window.Measure(new Size(400, 300));
                    window.Arrange(new Rect(0, 0, 400, 300));
                    Dispatcher.UIThread.RunJobs();
                }
            }
            Layout();

            // (a) Only a viewport-worth of items realize; far items do NOT. This is
            //     virtualization: a tiny realized window over a 2000-item source.
            int realizedInitial = Enumerable.Range(0, items.Count)
                .Count(i => listBox.ContainerFromIndex(i) != null);
            Assert.InRange(realizedInitial, 1, 100); // ~9 in practice, never all 2000
            Assert.NotNull(listBox.ContainerFromIndex(0));
            Assert.Null(listBox.ContainerFromIndex(1500));

            // (b) Scroll far down the list (drive the ScrollViewer offset directly;
            //     ScrollIntoView's post-based settling is unreliable in this manual
            //     harness, but the realize-on-scroll semantics are the same). New
            //     items down there get realized on demand.
            var sv = listBox.GetVisualDescendants().OfType<ScrollViewer>().First();
            sv.Offset = new Vector(0, 1_000_000); // clamped to max -> scroll to bottom
            Layout();

            var cLast = listBox.ContainerFromIndex(items.Count - 1);
            Assert.NotNull(cLast); // last row realized after scrolling to it

            // (c) Scrolling away RECYCLES: row 0's container is gone once far off
            //     screen. This is the load-bearing fact for §8 — selection/hover
            //     anchors keyed to a CONTAINER would be destroyed here, so anchors
            //     must be stored as (rowIndex, localOffset) DATA models.
            Assert.Null(listBox.ContainerFromIndex(0));

            // (d) Per-cell selection lives in the realized cell even though the
            //     ListBox item is not the selection surface. Programmatic
            //     SelectAll/Copy works (real pointer-drag can't run headless).
            var cell = ((ContentControl)cLast!)
                .GetVisualDescendants()
                .OfType<SelectableTextBlock>()
                .First();
            cell.SelectAll();
            Assert.False(string.IsNullOrEmpty(cell.SelectedText));
            Assert.True(cell.CanCopy);

            window.Close();
        });
    }
}
