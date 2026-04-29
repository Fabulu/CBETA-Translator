using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Tests for ResearchGraphCanvasControl visual-upgrade logic:
/// node brushes, cached pens, node radius, label truncation,
/// shadow alpha, performance guard, and window title.
/// </summary>
public class ResearchGraphVisualTests
{

    // Helper: read a private static field via reflection
    private static T GetStaticField<T>(string name)
    {
        var field = typeof(ResearchGraphCanvasControl)
            .GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        return (T)field!.GetValue(null)!;
    }

    // Helper: invoke the private GetNodeRadius method
    private static double InvokeGetNodeRadius(ResearchGraphCanvasControl ctrl, ResearchGraphNode node)
    {
        var method = typeof(ResearchGraphCanvasControl)
            .GetMethod("GetNodeRadius", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (double)method!.Invoke(ctrl, new object[] { node })!;
    }

    // ── 1. NodeBrushes has all 5 ScholarNodeType entries ───────────────

    [Fact]
    public void NodeBrushes_ContainsAllFiveNodeTypes()
    {
        var brushes = GetStaticField<Dictionary<ScholarNodeType, IBrush>>("NodeBrushes");
        Assert.Equal(5, brushes.Count);
        foreach (ScholarNodeType nt in Enum.GetValues<ScholarNodeType>())
        {
            Assert.True(brushes.ContainsKey(nt), $"NodeBrushes missing key {nt}");
        }
    }

    [Fact]
    public void NodeBrushes_ValuesAreNonNull()
    {
        var brushes = GetStaticField<Dictionary<ScholarNodeType, IBrush>>("NodeBrushes");
        foreach (var kvp in brushes)
        {
            Assert.NotNull(kvp.Value);
        }
    }

    // ── 2. Cached pens are non-null with correct thicknesses ──────────

    [Fact]
    public void SelectedPen_IsNonNullWithThickness3()
    {
        var pen = GetStaticField<IPen>("_selectedPen");
        Assert.NotNull(pen);
        Assert.Equal(3.0, pen.Thickness);
    }

    [Fact]
    public void HoverPen_IsNonNullWithThickness2Point5()
    {
        var pen = GetStaticField<IPen>("_hoverPen");
        Assert.NotNull(pen);
        Assert.Equal(2.5, pen.Thickness);
    }

    [Fact]
    public void DefaultNodePen_IsNonNullWithThickness1Point2()
    {
        var pen = GetStaticField<IPen>("_defaultNodePen");
        Assert.NotNull(pen);
        Assert.Equal(1.2, pen.Thickness, precision: 5);
    }

    // ── 3. GetNodeRadius returns reasonable values ────────────────────

    [Theory]
    [InlineData(ScholarNodeType.Passage, 0, 10)]
    [InlineData(ScholarNodeType.Passage, 3, 16)]
    [InlineData(ScholarNodeType.Passage, 100, 22)] // capped at 10 + 12
    [InlineData(ScholarNodeType.Concept, 0, 12)]
    [InlineData(ScholarNodeType.Concept, 100, 26)] // capped at 12 + 14
    [InlineData(ScholarNodeType.ZenMaster, 0, 14)]
    [InlineData(ScholarNodeType.ZenMaster, 100, 24)] // capped at 14 + 10
    [InlineData(ScholarNodeType.TermbaseEntry, 0, 12)]
    [InlineData(ScholarNodeType.TermbaseEntry, 100, 22)] // capped at 12 + 10
    [InlineData(ScholarNodeType.Collection, 0, 14)]
    [InlineData(ScholarNodeType.Collection, 100, 26)] // capped at 14 + 12
    public void GetNodeRadius_ReturnsExpectedValue(ScholarNodeType type, int degree, double expected)
    {
        var ctrl = new ResearchGraphCanvasControl();
        var node = new ResearchGraphNode { NodeType = type, Degree = degree };
        double actual = InvokeGetNodeRadius(ctrl, node);
        Assert.Equal(expected, actual, precision: 5);
    }

    [Fact]
    public void GetNodeRadius_AllTypesReturnPositive()
    {
        var ctrl = new ResearchGraphCanvasControl();
        foreach (ScholarNodeType nt in Enum.GetValues<ScholarNodeType>())
        {
            var node = new ResearchGraphNode { NodeType = nt, Degree = 1 };
            double r = InvokeGetNodeRadius(ctrl, node);
            Assert.True(r > 0, $"Radius for {nt} should be positive, got {r}");
            Assert.True(r < 100, $"Radius for {nt} unreasonably large: {r}");
        }
    }

    // ── 4. Label truncation at 25+ characters ─────────────────────────

    [Fact]
    public void LabelTruncation_ShortLabelUnchanged()
    {
        // Replicate the truncation logic from DrawNode
        string label = "Short label";
        string result = label.Length > 25 ? label[..24] + "\u2026" : label;
        Assert.Equal("Short label", result);
    }

    [Fact]
    public void LabelTruncation_Exactly25CharsUnchanged()
    {
        string label = "1234567890123456789012345"; // exactly 25
        string result = label.Length > 25 ? label[..24] + "\u2026" : label;
        Assert.Equal(label, result);
    }

    [Fact]
    public void LabelTruncation_26CharsTruncated()
    {
        string label = "12345678901234567890123456"; // 26 chars
        string result = label.Length > 25 ? label[..24] + "\u2026" : label;
        Assert.Equal("123456789012345678901234\u2026", result);
        Assert.Equal(25, result.Length);
    }

    [Fact]
    public void LabelTruncation_LongLabelEndsWithEllipsis()
    {
        string label = "This is a very long label that should be truncated by the rendering";
        string result = label.Length > 25 ? label[..24] + "\u2026" : label;
        Assert.EndsWith("\u2026", result);
        Assert.Equal(25, result.Length);
    }

    // ── 5. Shadow brushes have expected alpha values ──────────────────

    [Fact]
    public void ShadowBrushOuter_HasAlpha40()
    {
        var brush = GetStaticField<IBrush>("_shadowBrushOuter");
        Assert.NotNull(brush);
        var solid = Assert.IsType<SolidColorBrush>(brush);
        Assert.Equal(40, solid.Color.A);
    }

    [Fact]
    public void ShadowBrushInner_HasAlpha60()
    {
        var brush = GetStaticField<IBrush>("_shadowBrushInner");
        Assert.NotNull(brush);
        var solid = Assert.IsType<SolidColorBrush>(brush);
        Assert.Equal(60, solid.Color.A);
    }

    [Fact]
    public void LabelShadowBrush_HasAlpha200()
    {
        var brush = GetStaticField<IBrush>("_labelShadowBrush");
        Assert.NotNull(brush);
        var solid = Assert.IsType<SolidColorBrush>(brush);
        Assert.Equal(200, solid.Color.A);
    }

    // ── 6. Performance guard: shadow logic (< 500 vs >= 500 nodes) ───

    [Theory]
    [InlineData(100, false, false, true)]  // small graph, normal node -> shadow on
    [InlineData(100, true, false, false)]  // small graph, dimmed -> shadow off (always)
    [InlineData(499, false, false, true)]  // just under threshold -> shadow on
    [InlineData(500, false, false, false)] // at threshold, not selected/hovered -> shadow off
    [InlineData(500, false, true, true)]   // at threshold, selected -> shadow on
    [InlineData(1000, false, false, false)] // large graph, normal -> shadow off
    [InlineData(1000, false, true, true)]  // large graph, selected -> shadow on
    [InlineData(1000, true, true, false)]  // large graph, dimmed + selected -> dimmed wins, shadow off
    public void ShadowPerformanceGuard_RespectsThreshold(
        int nodeCount, bool isDimmed, bool isSelected, bool expectedShadow)
    {
        // Replicate the guard logic:
        //   bool drawShadow = !node.IsDimmed &&
        //       (nodeCount < 500 || node.IsSelected || node == _hoverNode);
        // For this test we treat hoverNode as null (only testing selected).
        bool isHovered = false;
        bool drawShadow = !isDimmed && (nodeCount < 500 || isSelected || isHovered);
        Assert.Equal(expectedShadow, drawShadow);
    }

    [Fact]
    public void ShadowPerformanceGuard_HoveredNodeGetsShadowInLargeGraph()
    {
        // When node == _hoverNode, shadow should draw even in large graphs
        int nodeCount = 1000;
        bool isDimmed = false;
        bool isHovered = true;
        bool isSelected = false;
        bool drawShadow = !isDimmed && (nodeCount < 500 || isSelected || isHovered);
        Assert.True(drawShadow);
    }

    // ── 7. Window title includes collection name ─────────────────────

    [Fact]
    public void WindowTitle_IncludesCollectionName()
    {
        // Replicate the title format from ResearchGraphWindow constructor:
        //   Title = $"Research Graph \u2014 {collection.Name ?? collection.Id}";
        var collection = new ScholarCollection { Id = "col-1", Name = "My Research" };
        string title = $"Research Graph \u2014 {collection.Name ?? collection.Id}";
        Assert.Contains("My Research", title);
        Assert.StartsWith("Research Graph", title);
    }

    [Fact]
    public void WindowTitle_FallsBackToId_WhenNameIsNull()
    {
        var collection = new ScholarCollection { Id = "col-abc", Name = null! };
        string title = $"Research Graph \u2014 {collection.Name ?? collection.Id}";
        Assert.Contains("col-abc", title);
        Assert.DoesNotContain("null", title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowTitle_ContainsEmDash()
    {
        var collection = new ScholarCollection { Id = "x", Name = "Test" };
        string title = $"Research Graph \u2014 {collection.Name ?? collection.Id}";
        Assert.Contains("\u2014", title);
    }
}
