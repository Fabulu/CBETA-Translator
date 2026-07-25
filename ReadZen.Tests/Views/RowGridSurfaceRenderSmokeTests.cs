// ReadZen.Tests/Views/RowGridSurfaceRenderSmokeTests.cs
//
// RENDER SMOKE / REGRESSION test for the "reader renders completely blank" bug
// (RUN-20260710-0605 reader-spa-parity, I3 Zen-dictionary underline regression).
//
// The four RowGrid reader modes went blank once the Zen-dictionary underline feature
// (ReadableTabView.StampZenTermHighlights) started stamping RowVm.ZenHighlights on
// essentially every CJK row: those rows stopped taking RowGridSurface's plain-text
// path (SetPlainText → SelectableTextBlock.Text) and started taking the highlight
// INLINE path (RenderHighlightedCell → cell.Inlines). This test realizes the real
// virtualized ListBox, lets DataContextChanged fire, and asserts the primary/EN cells
// actually carry their text — via Inlines runs OR Text — so a blank cell fails here.
//
// Rows WITHOUT ZenHighlights exercise the plain path; rows WITH ZenHighlights exercise
// the merged inline path (the suspected blank). Both must render.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

[Trait("Domain", "Reader")]
public class RowGridSurfaceRenderSmokeTests
{
    // Avalonia initialization handled by ModuleInit.cs [ModuleInitializer].

    private const string Zh1 = "菩提本無樹";
    private const string Zh2 = "明鏡亦非臺";
    private const string En1 = "Bodhi is fundamentally without any tree";
    private const string En2 = "The bright mirror is also not a stand";

    /// <summary>The effective text a realized cell will actually paint: the concatenation of its
    /// inline Runs when it uses the inline path, otherwise its plain Text.</summary>
    private static string EffectiveText(SelectableTextBlock cell)
    {
        var inlines = cell.Inlines;
        if (inlines is { Count: > 0 })
        {
            var sb = new StringBuilder();
            foreach (var inline in inlines)
                if (inline is Run r) sb.Append(r.Text);
            return sb.ToString();
        }
        return cell.Text ?? "";
    }

    private static RowVm TwoCol(int index, string lb, string zh, string en) => new()
    {
        Index = index,
        Lb = lb,
        IdLabel = lb,
        Shape = RowShape.TwoColumn,
        Side = RowSide.Zh,
        View = ReaderViewMode.Both,
        ZhText = zh,
        EnText = en,
    };

    [Fact]
    public void AllRows_RenderTheirText_WithAndWithoutZenHighlights()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            // The bare test Application carries no theme, so a ListBox gets no control template
            // (no ScrollViewer / VirtualizingStackPanel) and never realizes items. Load Fluent so
            // the surface templates + virtualizes exactly like the running app.
            if (Application.Current is { } app && app.Styles.Count == 0)
                app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

            var rows = new ObservableCollection<RowVm>
            {
                TwoCol(0, "0001a01", Zh1, En1), // no zen highlight → plain path
                TwoCol(1, "0001a02", Zh2, En2), // no zen highlight → plain path
            };
            // Rows 2..5 carry a Zen underline span (as StampZenTermHighlights does for CJK rows) →
            // route through the merged inline path (the suspected blank).
            for (int i = 2; i < 6; i++)
            {
                var r = TwoCol(i, $"0001b{i:00}", Zh1, En1);
                r.ZenHighlights = new[] { new Hspan(0, 2, false) }; // underline "菩提"
                rows.Add(r);
            }

            var surface = new RowGridSurface { ReaderFontSize = 16 };
            surface.ItemsSource = rows;

            var window = new Window { Width = 700, Height = 500, Content = surface };
            window.Show();

            void Layout()
            {
                window.Measure(new Size(700, 500));
                window.Arrange(new Rect(0, 0, 700, 500));
                Dispatcher.UIThread.RunJobs();
                for (int i = 0; i < 4; i++)
                {
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                    window.Measure(new Size(700, 500));
                    window.Arrange(new Rect(0, 0, 700, 500));
                    Dispatcher.UIThread.RunJobs();
                }
            }
            Layout();

            // Sanity: containers were actually realized (else the test proves nothing).
            var realized = Enumerable.Range(0, rows.Count)
                .Select(i => surface.ContainerFromIndex(i))
                .Where(c => c != null)
                .ToList();
            Assert.NotEmpty(realized);

            // Walk each realized row and pull its ZH (primary) + EN cell.
            int checkedZh = 0, checkedEn = 0;
            foreach (var container in realized)
            {
                var row = (RowVm)((ContentControl)container!).DataContext!;
                var cells = container!.GetVisualDescendants().OfType<SelectableTextBlock>().ToList();

                var primary = cells.FirstOrDefault(c => (c.Tag as string) == RowGridSurface.PrimaryCellTag);
                Assert.NotNull(primary);
                Assert.Equal(row.ZhText, EffectiveText(primary!));
                checkedZh++;

                // EN cell: the non-primary, non-id (left-aligned) wrapped cell that is visible.
                var en = cells.FirstOrDefault(c =>
                    (c.Tag as string) != RowGridSurface.PrimaryCellTag
                    && c.TextAlignment != Avalonia.Media.TextAlignment.Right
                    && c.IsVisible);
                Assert.NotNull(en);
                Assert.Equal(row.EnText, EffectiveText(en!));
                checkedEn++;
            }

            Assert.True(checkedZh >= rows.Count, $"only {checkedZh}/{rows.Count} ZH cells realized+checked");
            Assert.True(checkedEn >= rows.Count, $"only {checkedEn}/{rows.Count} EN cells realized+checked");

            window.Close();
        });
    }

    [Fact]
    public void ZenHighlightRow_StillCarriesUnderlineDecoration()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (Application.Current is { } app && app.Styles.Count == 0)
                app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

            var row = TwoCol(0, "0001a01", Zh1, En1);
            row.ZenHighlights = new[] { new Hspan(0, 2, false) };
            var rows = new ObservableCollection<RowVm> { row };

            var surface = new RowGridSurface { ReaderFontSize = 16 };
            surface.ItemsSource = rows;
            var window = new Window { Width = 700, Height = 300, Content = surface };
            window.Show();
            window.Measure(new Size(700, 300));
            window.Arrange(new Rect(0, 0, 700, 300));
            Dispatcher.UIThread.RunJobs();
            for (int i = 0; i < 4; i++)
            {
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                window.Measure(new Size(700, 300));
                window.Arrange(new Rect(0, 0, 700, 300));
                Dispatcher.UIThread.RunJobs();
            }

            var container = surface.ContainerFromIndex(0);
            Assert.NotNull(container);
            var primary = container!.GetVisualDescendants().OfType<SelectableTextBlock>()
                .First(c => (c.Tag as string) == RowGridSurface.PrimaryCellTag);

            // Text still renders in full...
            Assert.Equal(Zh1, EffectiveText(primary));
            // ...AND the first run (the underlined term span) carries a decoration.
            var firstRun = primary.Inlines?.OfType<Run>().FirstOrDefault();
            Assert.NotNull(firstRun);
            Assert.NotNull(firstRun!.TextDecorations);

            window.Close();
        });
    }
}
