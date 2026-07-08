using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for the read-only merged reading preview (audit P4.3b; design in
/// RUN-20260513-2238/ARCHITECT_SYNTHESIS_v3 "Translation tab read-only merged view").
/// Consecutive translation units are grouped by the segment their trailing lb maps
/// to; ZH is concatenated (no separator — CJK), EN joined with spaces skipping
/// blanks; unmapped/lb-less units continue the running group.
/// </summary>
[Trait("Domain", "Segmentation")]
public class MergedPreviewTests
{
    private const string TeiOpen =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>T</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body>";

    private const string TeiClose = "</body></text></TEI>";

    private static SegmentMap MakeMap(params (string type, string[] lbs)[] segs)
    {
        var list = new List<SegmentInfo>();
        var byLb = new Dictionary<string, SegmentInfo>(System.StringComparer.Ordinal);
        foreach (var (type, lbs) in segs)
        {
            var info = new SegmentInfo { Type = type, LbRange = new List<string>(lbs) };
            list.Add(info);
            foreach (var lb in lbs) byLb[lb] = info;
        }
        return new SegmentMap(list, byLb);
    }

    [Fact]
    public void GroupsConsecutiveUnitsBySegment_AndConcatenatesZhAndEn()
    {
        var svc = new IndexedTranslationService();
        // Two segments: lbs a01+a02 form one dialogue segment, a03 its own verse segment.
        var orig = TeiOpen +
            "<p>甲甲甲<lb n=\"0001a01\" ed=\"T\"/>乙乙乙<lb n=\"0001a02\" ed=\"T\"/>丙丙丙<lb n=\"0001a03\" ed=\"T\"/></p>" +
            TeiClose;

        var doc = svc.BuildIndex(orig, orig);
        // Give the lines EN so concatenation is observable.
        int i = 0;
        foreach (var u in doc.Units)
            if (u.Kind == TranslationUnitKind.Body) u.En = "EN" + (++i);

        var map = MakeMap(("dialogue", new[] { "0001a01", "0001a02" }), ("verse", new[] { "0001a03" }));

        var preview = svc.RenderMergedPreview(doc, TranslationEditMode.Body, map);

        Assert.Contains("<seg 1 | dialogue>", preview);
        Assert.Contains("ZH: 甲甲甲乙乙乙", preview);       // two lines merged, no separator
        Assert.Contains("EN: EN1 EN2", preview);            // EN joined with spaces
        Assert.Contains("<seg 2 | verse>", preview);
        Assert.Contains("ZH: 丙丙丙", preview);
        Assert.Contains("read-only", preview);              // header marks it non-editable
    }

    [Fact]
    public void UnitsWithoutMappedLb_ContinueTheRunningGroup()
    {
        var svc = new IndexedTranslationService();
        var orig = TeiOpen +
            "<p>甲甲<lb n=\"0001a01\" ed=\"T\"/>乙乙<lb n=\"9999x99\" ed=\"T\"/></p>" +
            TeiClose;

        var doc = svc.BuildIndex(orig, orig);
        // 9999x99 is NOT in the map: its line must stay in segment 1, not start one.
        var map = MakeMap(("prose", new[] { "0001a01" }));

        var preview = svc.RenderMergedPreview(doc, TranslationEditMode.Body, map);

        Assert.Contains("<seg 1 | prose>", preview);
        Assert.Contains("甲甲乙乙", preview);
        Assert.DoesNotContain("<seg 2", preview);
    }

    [Fact]
    public void LeadingUnmappedUnits_AreLabeledUnsegmented()
    {
        var svc = new IndexedTranslationService();
        var orig = TeiOpen +
            "<p>前置<lb n=\"zzz\" ed=\"T\"/>正文<lb n=\"0001a01\" ed=\"T\"/></p>" +
            TeiClose;

        var doc = svc.BuildIndex(orig, orig);
        var map = MakeMap(("prose", new[] { "0001a01" }));

        var preview = svc.RenderMergedPreview(doc, TranslationEditMode.Body, map);

        Assert.Contains("<seg 1 | unsegmented>", preview);
        Assert.Contains("<seg 2 | prose>", preview);
    }

    [Fact]
    public void BlankEnLines_AreSkippedInTheJoin()
    {
        var svc = new IndexedTranslationService();
        var orig = TeiOpen +
            "<p>甲<lb n=\"0001a01\" ed=\"T\"/>乙<lb n=\"0001a02\" ed=\"T\"/></p>" +
            TeiClose;

        var doc = svc.BuildIndex(orig, orig);
        var units = doc.Units.FindAll(u => u.Kind == TranslationUnitKind.Body);
        units[0].En = "only this";
        // units[1].En stays "" — must not produce a stray double space

        var map = MakeMap(("prose", new[] { "0001a01", "0001a02" }));
        var preview = svc.RenderMergedPreview(doc, TranslationEditMode.Body, map);

        Assert.Contains("EN: only this\n", preview.Replace("\r\n", "\n"));
    }
}
