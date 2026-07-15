// RowGridBuilderTests — pure builder for the unified row-grid reading surface (Wave C1).
// C1 implements only AlignedLines: one two-column row per <lb/>, ZH from the original and
// EN from the translation's same-key segment (or "" on a graceful miss), keyed lb -> row.

using System;
using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Reader")]
public class RowGridBuilderTests
{
    // Builds a RenderedDocument with the given text and (lb-key, start, endExclusive) segments.
    private static RenderedDocument Doc(string text, params (string key, int start, int end)[] segs)
    {
        var list = new List<RenderSegment>();
        foreach (var (key, start, end) in segs)
            list.Add(new RenderSegment(key, start, end));
        return new RenderedDocument(
            text,
            list,
            new List<DocAnnotation>(),
            new List<AnnotationMarkerInserter.MarkerSpan>());
    }

    // "初句\n二句"  -> 初=0 句=1 \n=2 二=3 句=4  ; "first\nsecond" -> first=0..4, second=6..11
    private static RenderedDocument OrigTwoLines()
        => Doc("初句\n二句", ("lb|0001a01", 0, 2), ("lb|0001a02", 3, 5));

    private static RenderedDocument TranTwoLines()
        => Doc("first\nsecond", ("lb|0001a01", 0, 5), ("lb|0001a02", 6, 12));

    [Fact]
    public void AlignedLines_EmitsOneTwoColumnRowPerLb_WithPairedText()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), segMap: null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, showLineIds: false);

        Assert.Equal(2, model.Rows.Count);

        Assert.Equal(0, model.Rows[0].Index);
        Assert.Equal("0001a01", model.Rows[0].Lb);
        Assert.Equal(RowShape.TwoColumn, model.Rows[0].Shape);
        Assert.Equal("初句", model.Rows[0].ZhText);
        Assert.Equal("first", model.Rows[0].EnText);

        Assert.Equal(1, model.Rows[1].Index);
        Assert.Equal("0001a02", model.Rows[1].Lb);
        Assert.Equal("二句", model.Rows[1].ZhText);
        Assert.Equal("second", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_LbToRow_MapsEachLbToItsRowIndex()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(0, model.LbToRow["0001a01"]);
        Assert.Equal(1, model.LbToRow["0001a02"]);
    }

    [Fact]
    public void AlignedLines_RecoversText_FromZeroLengthLbAnchors()
    {
        // The canonical CBETA "lb-before-p" shape: <lb/> is a ZERO-LENGTH anchor and the
        // line's text lives in the following p| segment. The builder must span lb-start →
        // next-lb-start (NOT the lb segment's own [Start,EndExclusive), which is empty) or it
        // drops the first line of every paragraph. Text "趙州狗子\n無佛性": 趙0 州1 狗2 子3 \n4 無5 佛6 性7.
        var orig = Doc("趙州狗子\n無佛性",
            ("lb|0001a01", 0, 0),   // zero-length anchor
            ("p|p1", 0, 8),          // the paragraph carries the text
            ("lb|0001a02", 5, 5));   // zero-length anchor before 無
        var tran = Doc("Zhaozhou\ndog-nature",
            ("lb|0001a01", 0, 0), ("p|q1", 0, 19), ("lb|0001a02", 9, 9));

        var model = RowGridBuilder.Build(
            orig, tran, null, ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.Equal("趙州狗子", model.Rows[0].ZhText);   // would be "" under the naive lb-slice bug
        Assert.Equal("無佛性", model.Rows[1].ZhText);
        Assert.Equal("Zhaozhou", model.Rows[0].EnText);
        Assert.Equal("dog-nature", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_PairsEn_ToleratingEditionSuffixInLbKeys()
    {
        // Translation lb keys carry an edition suffix ("lb|…|T") that the original's do not.
        // Pairing keys on the bare n-value must still match, or the whole EN column blanks.
        var orig = Doc("初\n二", ("lb|0001a01", 0, 0), ("p|p", 0, 3), ("lb|0001a02", 2, 2));
        var tran = Doc("one\ntwo", ("lb|0001a01|T", 0, 0), ("p|q", 0, 7), ("lb|0001a02|T", 4, 4));

        var model = RowGridBuilder.Build(
            orig, tran, null, ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal("one", model.Rows[0].EnText);
        Assert.Equal("two", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_MissingTranslationLine_YieldsEmptyEn_Gracefully()
    {
        // Translation carries only the first lb; the second row must still render with EN "".
        var tran = Doc("first", ("lb|0001a01", 0, 5));
        var model = RowGridBuilder.Build(
            OrigTwoLines(), tran, null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.Equal("first", model.Rows[0].EnText);
        Assert.Equal("", model.Rows[1].EnText);   // graceful miss, not a crash
        Assert.Equal("二句", model.Rows[1].ZhText); // ZH still present
    }

    [Theory]
    [InlineData(true, "0001a01")]
    [InlineData(false, "")]
    public void AlignedLines_ShowLineIds_TogglesIdLabel(bool show, string expectedFirstLabel)
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, showLineIds: show);

        Assert.Equal(expectedFirstLabel, model.Rows[0].IdLabel);
    }

    [Fact]
    public void AlignedLines_SkipsNonLbSegments()
    {
        // A stray non-"lb|" segment must not become a row.
        var orig = Doc("初句\n二句",
            ("head|x", 0, 0), ("lb|0001a01", 0, 2), ("lb|0001a02", 3, 5));
        var model = RowGridBuilder.Build(
            orig, TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.All(model.Rows, r => Assert.StartsWith("0001a0", r.Lb));
    }

    [Fact]
    public void AlignedLines_NeedsNoSegmentMap()
    {
        // Passing a null segMap must not throw — AlignedLines pairs purely by lb key.
        var ex = Record.Exception(() => RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false));
        Assert.Null(ex);
    }

    [Fact]
    public void EmptyOriginal_YieldsEmptyModel()
    {
        var model = RowGridBuilder.Build(
            RenderedDocument.Empty, RenderedDocument.Empty, null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Empty(model.Rows);
        Assert.Empty(model.LbToRow);
    }

    [Theory]
    [InlineData(ReadingLayoutMode.Page)]
    [InlineData(ReadingLayoutMode.MergedFlow)]
    public void NotYetImplementedModes_Throw(ReadingLayoutMode mode)
    {
        // The grid implements AlignedLines, AlignedBlocks, Interleaved and MergedStacked; every
        // other mode must fail loudly rather than silently render blank (the router never sends
        // them here yet).
        Assert.Throws<NotSupportedException>(() => RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null, mode, ReaderViewMode.Both, false));
    }

    // ---- Interleaved (single-column: ZH row then optional EN row per source line) ----

    [Fact]
    public void Interleaved_EmitsZhThenEn_SkipsEnWhenTranslationMissing()
    {
        // Line A (0001a01) HAS a translation; line B (0001a02) does NOT. Expect rows:
        //   [A-zh, A-en, B-zh]  — B's EN row is skipped, B's ZH row still renders.
        var tran = Doc("first", ("lb|0001a01", 0, 5));   // only line A translated
        var model = RowGridBuilder.Build(
            OrigTwoLines(), tran, segMap: null,
            ReadingLayoutMode.Interleaved, ReaderViewMode.Both, showLineIds: false);

        Assert.Equal(3, model.Rows.Count);

        // A-zh
        Assert.Equal(RowShape.SingleColumn, model.Rows[0].Shape);
        Assert.Equal(RowSide.Zh, model.Rows[0].Side);
        Assert.Equal("0001a01", model.Rows[0].Lb);
        Assert.Equal("初句", model.Rows[0].ZhText);

        // A-en (immediately after A-zh, single column, carries text in EnText)
        Assert.Equal(RowShape.SingleColumn, model.Rows[1].Shape);
        Assert.Equal(RowSide.En, model.Rows[1].Side);
        Assert.Equal("0001a01", model.Rows[1].Lb);
        Assert.Equal("first", model.Rows[1].EnText);
        Assert.Equal("first", model.Rows[1].PrimaryText); // surface renders EnText for an En row

        // B-zh only — no B-en
        Assert.Equal(RowShape.SingleColumn, model.Rows[2].Shape);
        Assert.Equal(RowSide.Zh, model.Rows[2].Side);
        Assert.Equal("0001a02", model.Rows[2].Lb);
        Assert.Equal("二句", model.Rows[2].ZhText);
    }

    [Fact]
    public void Interleaved_AllRowsAreSingleColumn_WithNoEnColumn()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.Interleaved, ReaderViewMode.Both, false);

        // Both lines translated → [A-zh, A-en, B-zh, B-en].
        Assert.Equal(4, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.Equal(RowShape.SingleColumn, r.Shape);
            Assert.False(r.ShowEnColumn);        // single-column: EN column hidden
            Assert.Equal(2, r.PrimaryColumnSpan); // primary cell fills the EN slot (no gutter)
        });
    }

    [Fact]
    public void Interleaved_SpacerId_EmitsZhRowButNoEnRow()
    {
        // A source line whose lb n-value is a "__" spacer must not get an EN row even when a
        // translation with the same key exists (SPA: `not __ spacer`). ZH row still renders.
        var orig = Doc("初\n二",
            ("lb|__spacer", 0, 0), ("p|p", 0, 3), ("lb|0001a02", 2, 2));
        var tran = Doc("skip\ntwo",
            ("lb|__spacer", 0, 0), ("p|q", 0, 8), ("lb|0001a02", 5, 5));

        var model = RowGridBuilder.Build(
            orig, tran, null, ReadingLayoutMode.Interleaved, ReaderViewMode.Both, false);

        // Expect [spacer-zh, B-zh, B-en] — spacer's EN row skipped.
        Assert.Equal(3, model.Rows.Count);
        Assert.Equal("__spacer", model.Rows[0].Lb);
        Assert.Equal(RowSide.Zh, model.Rows[0].Side);
        Assert.Equal("0001a02", model.Rows[1].Lb);
        Assert.Equal(RowSide.Zh, model.Rows[1].Side);
        Assert.Equal("0001a02", model.Rows[2].Lb);
        Assert.Equal(RowSide.En, model.Rows[2].Side);
        Assert.Equal("two", model.Rows[2].EnText);
    }

    [Fact]
    public void Interleaved_LbToRow_TargetsTheZhRow()
    {
        // The lb → row lookup must point at each line's ZH row (the durable scroll target),
        // not its EN row.
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.Interleaved, ReaderViewMode.Both, false);

        Assert.Equal(0, model.LbToRow["0001a01"]);            // A-zh
        Assert.Equal(RowSide.Zh, model.Rows[model.LbToRow["0001a01"]].Side);
        Assert.Equal(2, model.LbToRow["0001a02"]);            // B-zh (A-zh, A-en, B-zh)
        Assert.Equal(RowSide.Zh, model.Rows[model.LbToRow["0001a02"]].Side);
    }

    [Fact]
    public void Interleaved_ShowLineIds_LabelsZhRowOnly()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.Interleaved, ReaderViewMode.Both, showLineIds: true);

        Assert.Equal("0001a01", model.Rows[0].IdLabel); // ZH row echoes the lb
        Assert.Equal("", model.Rows[1].IdLabel);        // EN continuation row: no id echo
    }

    // ---- MergedStacked (single-column, segment-map-driven: per-unit healed ZH then EN paras) ----

    // Builds a SegmentMap from (unitId, type, lb-ids) tuples. Each unit's lb-ids map to one
    // shared SegmentInfo (mirrors the .segments.jsonl → SegmentMap.ByLbId contract).
    private static SegmentMap Map(params (string unitId, string type, string[] lbs)[] units)
    {
        var segs = new List<SegmentInfo>();
        var byLb = new Dictionary<string, SegmentInfo>(StringComparer.Ordinal);
        foreach (var (unitId, type, lbs) in units)
        {
            var si = new SegmentInfo { UnitId = unitId, Type = type, LbRange = new List<string>(lbs) };
            segs.Add(si);
            foreach (var lb in lbs) byLb[lb] = si;
        }
        return new SegmentMap(segs, byLb);
    }

    // Four source lines, two per unit. "甲一\n甲二\n乙一\n乙二": lb anchors at 0/3/6/9 → each
    // line's text spans anchor→next-anchor (ExtractLbSpans), trimmed.
    private static RenderedDocument OrigTwoUnits()
        => Doc("甲一\n甲二\n乙一\n乙二",
            ("lb|0001a01", 0, 0), ("lb|0001a02", 3, 3),
            ("lb|0001a03", 6, 6), ("lb|0001a04", 9, 9));

    // Full translation for all four lines. "One-A\nOne-B\nTwo-A\nTwo-B": anchors at 0/6/12/18.
    private static RenderedDocument TranTwoUnits()
        => Doc("One-A\nOne-B\nTwo-A\nTwo-B",
            ("lb|0001a01", 0, 0), ("lb|0001a02", 6, 6),
            ("lb|0001a03", 12, 12), ("lb|0001a04", 18, 18));

    private static SegmentMap TwoUnitMap()
        => Map(("u1", "", new[] { "0001a01", "0001a02" }),
               ("u2", "", new[] { "0001a03", "0001a04" }));

    [Fact]
    public void MergedStacked_TwoUnits_EmitsHealedZhThenEnParagraphPerUnit()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, showLineIds: false);

        // [u1-zh, u1-en, u2-zh, u2-en] — all single-column.
        Assert.Equal(4, model.Rows.Count);
        Assert.All(model.Rows, r => Assert.Equal(RowShape.SingleColumn, r.Shape));

        // u1 ZH paragraph healed (no separator — woodblock cut closed).
        Assert.Equal(RowSide.Zh, model.Rows[0].Side);
        Assert.Equal("0001a01", model.Rows[0].Lb);
        Assert.Equal("甲一甲二", model.Rows[0].ZhText);

        // u1 EN paragraph healed (space-joined) — carried in EnText and surfaced via PrimaryText.
        Assert.Equal(RowSide.En, model.Rows[1].Side);
        Assert.Equal("0001a01", model.Rows[1].Lb);
        Assert.Equal("One-A One-B", model.Rows[1].EnText);
        Assert.Equal("One-A One-B", model.Rows[1].PrimaryText);
        Assert.Equal("", model.Rows[1].IdLabel);   // EN continuation: no id echo

        // u2 ZH then EN.
        Assert.Equal(RowSide.Zh, model.Rows[2].Side);
        Assert.Equal("乙一乙二", model.Rows[2].ZhText);
        Assert.Equal("0001a03", model.Rows[2].Lb);
        Assert.Equal(RowSide.En, model.Rows[3].Side);
        Assert.Equal("Two-A Two-B", model.Rows[3].EnText);
    }

    [Fact]
    public void MergedStacked_LbToRow_TargetsUnitZhRow_ForEveryLbInTheUnit()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false);

        // Every lb resolves to its unit's ZH paragraph row (the durable scroll target).
        Assert.Equal(0, model.LbToRow["0001a01"]);
        Assert.Equal(0, model.LbToRow["0001a02"]); // merged into u1's row, still scrollable
        Assert.Equal(2, model.LbToRow["0001a03"]);
        Assert.Equal(2, model.LbToRow["0001a04"]);
        Assert.Equal(RowSide.Zh, model.Rows[model.LbToRow["0001a02"]].Side);
    }

    [Fact]
    public void MergedStacked_UnitWithNoTranslation_EmitsUntranslatedPlaceholder_NotBlank()
    {
        // Translation covers only u1; u2 has no paired translation for either line.
        var tran = Doc("One-A\nOne-B",
            ("lb|0001a01", 0, 0), ("lb|0001a02", 6, 6));

        var model = RowGridBuilder.Build(
            OrigTwoUnits(), tran, TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false);

        Assert.Equal(4, model.Rows.Count);
        Assert.Equal("One-A One-B", model.Rows[1].EnText);          // u1 translated
        Assert.Equal(RowSide.En, model.Rows[3].Side);
        Assert.Equal("(untranslated)", model.Rows[3].EnText);       // u2 placeholder, not blank
    }

    [Fact]
    public void MergedStacked_HeadingUnit_BreaksOutAsItsOwnBlock_NoEnCompanion()
    {
        // u1 is a heading → one standalone header row, no EN companion. u2 is normal prose.
        var map = Map(("u1", "heading", new[] { "0001a01", "0001a02" }),
                      ("u2", "", new[] { "0001a03", "0001a04" }));

        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), map,
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false);

        // [u1-heading (no EN), u2-zh, u2-en]
        Assert.Equal(3, model.Rows.Count);

        Assert.True(model.Rows[0].IsHeader);
        Assert.Equal(RowSide.Zh, model.Rows[0].Side);
        Assert.Equal("甲一甲二", model.Rows[0].ZhText);
        Assert.Equal("heading", model.Rows[0].SegType);

        // The row right after the heading is u2's ZH — the heading has no EN companion row.
        Assert.False(model.Rows[1].IsHeader);
        Assert.Equal(RowSide.Zh, model.Rows[1].Side);
        Assert.Equal("乙一乙二", model.Rows[1].ZhText);
        Assert.Equal(RowSide.En, model.Rows[2].Side);
        Assert.Equal("Two-A Two-B", model.Rows[2].EnText);
    }

    [Fact]
    public void MergedStacked_ShowLineIds_LabelsUnitZhRowOnly()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, showLineIds: true);

        Assert.Equal("0001a01", model.Rows[0].IdLabel); // u1 ZH row echoes its first lb
        Assert.Equal("", model.Rows[1].IdLabel);        // EN continuation row: no id echo
        Assert.Equal("0001a03", model.Rows[2].IdLabel); // u2 ZH row
    }

    [Fact]
    public void MergedStacked_NullSegmentMap_YieldsEmptyModel_NotThrows()
    {
        // The view downgrades to Interleaved BEFORE calling the builder when no map exists; a
        // null map reaching the builder must degrade to an empty model (the view's zero-row
        // guard then falls back), never throw.
        var ex = Record.Exception(() => RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), segMap: null,
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false));
        Assert.Null(ex);

        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), null,
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false);
        Assert.Empty(model.Rows);
        Assert.Empty(model.LbToRow);
    }

    // ---- AlignedBlocks (two-column ZH|EN, grouped + re-aligned at segment-unit boundaries) ----

    [Fact]
    public void AlignedBlocks_FullyTranslated_EmitsTwoColumnRow_PerSourceLine()
    {
        // Every line translated → each unit's EN count == its ZH count, so the zip produces one
        // two-column row per source lb (visually the same as AlignedLines when nothing is missing).
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, showLineIds: false);

        Assert.Equal(4, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.Equal(RowShape.TwoColumn, r.Shape);
            Assert.True(r.ShowEnColumn);           // two-column: EN column shown
            Assert.Equal(RowSide.Zh, r.Side);      // two-column rows carry ZH on the primary side
        });

        Assert.Equal("0001a01", model.Rows[0].Lb);
        Assert.Equal("甲一", model.Rows[0].ZhText);
        Assert.Equal("One-A", model.Rows[0].EnText);
        Assert.Equal("甲二", model.Rows[1].ZhText);
        Assert.Equal("One-B", model.Rows[1].EnText);
        Assert.Equal("乙一", model.Rows[2].ZhText);
        Assert.Equal("Two-A", model.Rows[2].EnText);
        Assert.Equal("乙二", model.Rows[3].ZhText);
        Assert.Equal("Two-B", model.Rows[3].EnText);
    }

    [Fact]
    public void AlignedBlocks_MidUnitUntranslatedLine_ReflowsEnUpAndPadsTrailingCell()
    {
        // ONE unit of three lines; only lines 1 and 3 are translated (line 2 has no EN). Because
        // the EN column is the unit's non-empty translations (empties dropped), the third line's
        // EN reflows UP onto the second row, and the unit's trailing EN cell pads blank — the
        // block-fill behavior that distinguishes AlignedBlocks from AlignedLines. "甲一\n甲二\n甲三":
        // anchors at 0/3/6, text length 8.
        var orig = Doc("甲一\n甲二\n甲三",
            ("lb|0001a01", 0, 0), ("lb|0001a02", 3, 3), ("lb|0001a03", 6, 6));
        // Translation only for lines 1 and 3. "T1\nT3": anchors at 0/3.
        var tran = Doc("T1\nT3",
            ("lb|0001a01", 0, 0), ("lb|0001a03", 3, 3));
        var map = Map(("u1", "", new[] { "0001a01", "0001a02", "0001a03" }));

        var model = RowGridBuilder.Build(
            orig, tran, map, ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, false);

        Assert.Equal(3, model.Rows.Count);

        Assert.Equal("甲一", model.Rows[0].ZhText);
        Assert.Equal("T1", model.Rows[0].EnText);   // first EN pairs with first ZH

        Assert.Equal("甲二", model.Rows[1].ZhText);
        Assert.Equal("T3", model.Rows[1].EnText);   // T3 reflows UP (line 2 had no EN)

        Assert.Equal("甲三", model.Rows[2].ZhText);
        Assert.Equal("", model.Rows[2].EnText);     // shorter EN than ZH → trailing cell padded blank

        // Every source lb still resolves to its own ZH row (the durable scroll target).
        Assert.Equal(0, model.LbToRow["0001a01"]);
        Assert.Equal(1, model.LbToRow["0001a02"]);
        Assert.Equal(2, model.LbToRow["0001a03"]);
    }

    [Fact]
    public void AlignedBlocks_ResetsColumnsAtUnitBoundary_NextUnitEnDoesNotBleedUp()
    {
        // Unit u1 (a01,a02): only a01 translated. Unit u2 (a03,a04): both translated. The unit
        // reset is the whole point of "blocks": u2's first EN (Two-A) must land on u2's FIRST row
        // (乙一), NOT reflow up into u1's blank second EN cell. A naive doc-wide compaction would
        // put Two-A next to 甲二 — the per-unit reset prevents that.
        var tran = Doc("One-A\nTwo-A\nTwo-B",
            ("lb|0001a01", 0, 0), ("lb|0001a03", 6, 6), ("lb|0001a04", 12, 12)); // a02 missing

        var model = RowGridBuilder.Build(
            OrigTwoUnits(), tran, TwoUnitMap(),
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, false);

        Assert.Equal(4, model.Rows.Count);

        // u1: a01 translated, a02 padded blank (EN does NOT pull u2's Two-A up).
        Assert.Equal("甲一", model.Rows[0].ZhText);
        Assert.Equal("One-A", model.Rows[0].EnText);
        Assert.Equal("甲二", model.Rows[1].ZhText);
        Assert.Equal("", model.Rows[1].EnText);      // unit boundary reset — Two-A stays in u2

        // u2: columns realign at the unit start.
        Assert.Equal("乙一", model.Rows[2].ZhText);
        Assert.Equal("Two-A", model.Rows[2].EnText); // u2's first EN on u2's first row
        Assert.Equal("乙二", model.Rows[3].ZhText);
        Assert.Equal("Two-B", model.Rows[3].EnText);
    }

    [Fact]
    public void AlignedBlocks_ShowLineIds_LabelsEveryZhRow()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, showLineIds: true);

        Assert.Equal("0001a01", model.Rows[0].IdLabel);
        Assert.Equal("0001a02", model.Rows[1].IdLabel);
        Assert.Equal("0001a03", model.Rows[2].IdLabel);
        Assert.Equal("0001a04", model.Rows[3].IdLabel);
    }

    [Fact]
    public void AlignedBlocks_NullSegmentMap_YieldsEmptyModel_NotThrows()
    {
        // The view downgrades to AlignedLines BEFORE calling the builder when no map exists; a
        // null map reaching the builder must degrade to an empty model (the view's zero-row guard
        // then falls back), never throw.
        var ex = Record.Exception(() => RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), segMap: null,
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, false));
        Assert.Null(ex);

        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), null,
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Both, false);
        Assert.Empty(model.Rows);
        Assert.Empty(model.LbToRow);
    }

    // ---- PR-5d: ZH/Both/EN view filtering on the TWO-COLUMN grid modes ----
    // The filter is expressed through the RowVm getters (ShowEnColumn / PrimaryText /
    // PrimaryColumnSpan). Both = both columns (byte-identical to pre-filter). Zh = ZH-only (EN
    // column collapsed). En = EN-only (EN text moves into the primary cell; ZH never left up).

    [Fact]
    public void AlignedLines_ViewZh_CollapsesEnColumn_PrimaryStaysZh()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), segMap: null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Zh, showLineIds: false);

        Assert.Equal(2, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.Equal(RowShape.TwoColumn, r.Shape);
            Assert.False(r.ShowEnColumn);          // EN column hidden under ZH-only
            Assert.Equal(2, r.PrimaryColumnSpan);  // primary cell fills the EN slot (no gutter)
        });
        // Content still carries both strings; only the VISIBLE (primary) cell is ZH.
        Assert.Equal("初句", model.Rows[0].PrimaryText);
        Assert.Equal("first", model.Rows[0].EnText);  // still populated, just not shown
    }

    [Fact]
    public void AlignedLines_ViewEn_CollapsesEnColumn_PrimaryCarriesEnNotZh()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.En, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.Equal(RowShape.TwoColumn, r.Shape);
            Assert.False(r.ShowEnColumn);          // EN folded into the primary cell → column hidden
            Assert.Equal(2, r.PrimaryColumnSpan);
        });
        // The visible cell must show EN, never ZH (task: "never leave it showing ZH").
        Assert.Equal("first", model.Rows[0].PrimaryText);
        Assert.Equal("second", model.Rows[1].PrimaryText);
    }

    [Fact]
    public void AlignedLines_ViewBoth_IsByteIdenticalToPreFilterRendering()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.All(model.Rows, r =>
        {
            Assert.True(r.ShowEnColumn);           // both columns shown
            Assert.Equal(1, r.PrimaryColumnSpan);  // primary keeps its single column
        });
        Assert.Equal("初句", model.Rows[0].PrimaryText); // primary is ZH under Both
        Assert.Equal("first", model.Rows[0].EnText);
        Assert.Equal("二句", model.Rows[1].PrimaryText);
        Assert.Equal("second", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedBlocks_ViewZh_CollapsesEnColumn_PrimaryStaysZh()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.Zh, false);

        Assert.Equal(4, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.Equal(RowShape.TwoColumn, r.Shape);
            Assert.False(r.ShowEnColumn);
            Assert.Equal(2, r.PrimaryColumnSpan);
        });
        Assert.Equal("甲一", model.Rows[0].PrimaryText);
        Assert.Equal("乙二", model.Rows[3].PrimaryText);
    }

    [Fact]
    public void AlignedBlocks_ViewEn_PrimaryCarriesEnNotZh()
    {
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.AlignedBlocks, ReaderViewMode.En, false);

        Assert.Equal(4, model.Rows.Count);
        Assert.All(model.Rows, r =>
        {
            Assert.False(r.ShowEnColumn);
            Assert.Equal(2, r.PrimaryColumnSpan);
        });
        Assert.Equal("One-A", model.Rows[0].PrimaryText);
        Assert.Equal("One-B", model.Rows[1].PrimaryText);
        Assert.Equal("Two-A", model.Rows[2].PrimaryText);
        Assert.Equal("Two-B", model.Rows[3].PrimaryText);
    }

    [Fact]
    public void AlignedBlocks_ViewEn_MissingTranslation_PrimaryCellIsBlank_NotZh()
    {
        // Line 2 of the (3-line) unit has no EN; under EN-only its padded row must show "" in the
        // primary cell — never fall back to the ZH text.
        var orig = Doc("甲一\n甲二\n甲三",
            ("lb|0001a01", 0, 0), ("lb|0001a02", 3, 3), ("lb|0001a03", 6, 6));
        var tran = Doc("T1\nT3", ("lb|0001a01", 0, 0), ("lb|0001a03", 3, 3));
        var map = Map(("u1", "", new[] { "0001a01", "0001a02", "0001a03" }));

        var model = RowGridBuilder.Build(
            orig, tran, map, ReadingLayoutMode.AlignedBlocks, ReaderViewMode.En, false);

        Assert.Equal("T1", model.Rows[0].PrimaryText);
        Assert.Equal("T3", model.Rows[1].PrimaryText); // T3 reflowed up (blocks behavior)
        Assert.Equal("", model.Rows[2].PrimaryText);   // trailing pad blank, NOT "甲三"
    }

    [Theory]
    [InlineData(ReaderViewMode.Zh)]
    [InlineData(ReaderViewMode.En)]
    [InlineData(ReaderViewMode.Both)]
    public void Interleaved_IgnoresView_OutputIdenticalRegardlessOfViewMode(ReaderViewMode view)
    {
        // Single-column modes suppress the ZH/Both/EN toggle (SPA passage.js:499): the built rows
        // must be identical to the Both baseline for ANY view passed.
        var baseline = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.Interleaved, ReaderViewMode.Both, false);
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.Interleaved, view, false);

        Assert.Equal(baseline.Rows.Count, model.Rows.Count);
        for (int i = 0; i < model.Rows.Count; i++)
        {
            Assert.Equal(baseline.Rows[i].Shape, model.Rows[i].Shape);
            Assert.Equal(baseline.Rows[i].Side, model.Rows[i].Side);
            Assert.Equal(baseline.Rows[i].PrimaryText, model.Rows[i].PrimaryText);
            Assert.Equal(baseline.Rows[i].ShowEnColumn, model.Rows[i].ShowEnColumn);
            Assert.Equal(baseline.Rows[i].PrimaryColumnSpan, model.Rows[i].PrimaryColumnSpan);
        }
        // Every single-column row folds to one column no matter the view.
        Assert.All(model.Rows, r =>
        {
            Assert.False(r.ShowEnColumn);
            Assert.Equal(2, r.PrimaryColumnSpan);
        });
    }

    [Theory]
    [InlineData(ReaderViewMode.Zh)]
    [InlineData(ReaderViewMode.En)]
    [InlineData(ReaderViewMode.Both)]
    public void MergedStacked_IgnoresView_OutputIdenticalRegardlessOfViewMode(ReaderViewMode view)
    {
        var baseline = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, ReaderViewMode.Both, false);
        var model = RowGridBuilder.Build(
            OrigTwoUnits(), TranTwoUnits(), TwoUnitMap(),
            ReadingLayoutMode.MergedStacked, view, false);

        Assert.Equal(baseline.Rows.Count, model.Rows.Count);
        for (int i = 0; i < model.Rows.Count; i++)
        {
            Assert.Equal(baseline.Rows[i].Side, model.Rows[i].Side);
            Assert.Equal(baseline.Rows[i].PrimaryText, model.Rows[i].PrimaryText);
            Assert.Equal(baseline.Rows[i].ShowEnColumn, model.Rows[i].ShowEnColumn);
            Assert.Equal(baseline.Rows[i].PrimaryColumnSpan, model.Rows[i].PrimaryColumnSpan);
        }
    }
}
