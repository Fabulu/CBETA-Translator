using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Text;

/// <summary>
/// Render-layer dedupe of outline/heading entries. CBETA TEI legitimately repeats a
/// section heading (e.g. T48n2005 (無門關) has two consecutive &lt;cb:div type="xu"&gt;
/// prefaces both headed 禪宗無門關); the outline must list it once. The corpus XML is
/// canonical and never edited, so the collapse happens at the render layer.
/// </summary>
public class HeadingDeduplicatorTests
{
    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>無門關</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body>";

    private const string Footer = "</body></text></TEI>";

    // ---- helper unit tests ----

    [Fact]
    public void Dedupe_DropsAdjacentIdentical_KeepsFirst()
    {
        var input = new List<HeadingInfo>
        {
            new("禪宗無門關", RenderedOffset: 10, Level: 1),
            new("禪宗無門關", RenderedOffset: 80, Level: 1), // adjacent identical -> dropped
        };

        var outp = HeadingDeduplicator.Dedupe(input);

        Assert.Single(outp);
        Assert.Equal(10, outp[0].RenderedOffset); // first occurrence's nav target survives
    }

    [Fact]
    public void Dedupe_KeepsDistinctAndNonAdjacentRepeats()
    {
        var input = new List<HeadingInfo>
        {
            new("序", 0, 1),
            new("序", 5, 1),    // adjacent identical -> dropped
            new("正宗", 10, 1), // distinct -> kept
            new("序", 20, 1),   // same text as first but NOT adjacent -> kept
            new("序", 30, 2),   // same text, different level -> kept
        };

        var outp = HeadingDeduplicator.Dedupe(input);

        Assert.Equal(new[] { 0, 10, 20, 30 }, outp.Select(h => h.RenderedOffset).ToArray());
    }

    [Fact]
    public void Dedupe_IsWhitespaceInsensitive()
    {
        var input = new List<HeadingInfo>
        {
            new("禪宗 無門關", 0, 1),
            new("禪宗　無門關", 9, 1),
        };

        Assert.Single(HeadingDeduplicator.Dedupe(input));
    }

    [Fact]
    public void Dedupe_NullOrEmpty_YieldsEmpty()
    {
        Assert.Empty(HeadingDeduplicator.Dedupe(null));
        Assert.Empty(HeadingDeduplicator.Dedupe(new List<HeadingInfo>()));
    }

    // ---- TeiRenderer integration: the outline the app actually shows ----

    [Fact]
    public void TeiRenderer_CollapsesTwoConsecutiveIdenticalHeadings()
    {
        // Two consecutive preface divisions, both headed 禪宗無門關 (the T48n2005 shape).
        var xml = Header +
            "<cb:div xmlns:cb=\"http://www.cbeta.org/ns/1.0\" type=\"xu\">" +
            "<lb n=\"0292a25\" ed=\"T\"/><head>禪宗無門關</head><p>first preface</p></cb:div>" +
            "<cb:div xmlns:cb=\"http://www.cbeta.org/ns/1.0\" type=\"xu\">" +
            "<lb n=\"0292b11\" ed=\"T\"/><head>禪宗無門關</head><p>second preface</p></cb:div>" +
            Footer;

        var doc = TeiRenderer.Render(xml);

        Assert.Single(doc.Headings);
        Assert.Equal("禪宗無門關", doc.Headings[0].Text);
    }

    [Fact]
    public void TeiRenderer_KeepsDistinctHeadings()
    {
        var xml = Header +
            "<lb n=\"0001a01\" ed=\"T\"/><head>序</head><p>a</p>" +
            "<lb n=\"0001a02\" ed=\"T\"/><head>正宗分</head><p>b</p>" +
            Footer;

        var doc = TeiRenderer.Render(xml);

        Assert.Equal(2, doc.Headings.Count);
    }
}
