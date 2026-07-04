using System.Xml.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P2.4 (RUN-20260702-2259 R2-M7): the per-element
/// node-path walk was replaced by a single-pass map. Path strings are persisted unit
/// identities (ElementNodePath / ByNodePath lookups), so the new builder must produce
/// byte-identical paths — including sibling indices per (parent, name) pair and the
/// tei/cb/ns namespace prefixes.
/// </summary>
public class NodePathMapTests
{
    [Fact]
    public void Map_ProducesTheHistoricalPathFormat()
    {
        var doc = XDocument.Parse(
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\" xmlns:cb=\"http://www.cbeta.org/ns/1.0\">" +
            "<text><body>" +
            "<p>one</p>" +
            "<p>two<note>n</note></p>" +
            "<cb:div><p>three</p></cb:div>" +
            "<cb:div><p>four</p><p>five</p></cb:div>" +
            "</body></text></TEI>",
            LoadOptions.PreserveWhitespace);

        var map = IndexedTranslationService.BuildNodePathMap(doc);

        var body = doc.Root!.Element(XName.Get("text", "http://www.tei-c.org/ns/1.0"))!
                            .Element(XName.Get("body", "http://www.tei-c.org/ns/1.0"))!;

        Assert.Equal("tei:TEI[1]", map[doc.Root!]);
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]", map[body]);

        var ps = body.Elements(XName.Get("p", "http://www.tei-c.org/ns/1.0")).ToArray();
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[1]", map[ps[0]]);
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/tei:p[2]", map[ps[1]]);

        var divs = body.Elements(XName.Get("div", "http://www.cbeta.org/ns/1.0")).ToArray();
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/cb:div[1]", map[divs[0]]);
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/cb:div[2]", map[divs[1]]);

        // Sibling index counts per (parent, name): the second div's ps restart at [1].
        var div2ps = divs[1].Elements(XName.Get("p", "http://www.tei-c.org/ns/1.0")).ToArray();
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/cb:div[2]/tei:p[1]", map[div2ps[0]]);
        Assert.Equal("tei:TEI[1]/tei:text[1]/tei:body[1]/cb:div[2]/tei:p[2]", map[div2ps[1]]);

        // Unknown namespaces get the "ns" prefix (matches the old PrefixFor behavior).
        var doc2 = XDocument.Parse("<x xmlns=\"urn:other\"><y/></x>");
        var map2 = IndexedTranslationService.BuildNodePathMap(doc2);
        Assert.Equal("ns:x[1]", map2[doc2.Root!]);
        Assert.Equal("ns:x[1]/ns:y[1]", map2[doc2.Root!.Elements().First()]);
    }

    [Fact]
    public void Map_CoversEveryElement()
    {
        var doc = XDocument.Parse(
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><teiHeader><fileDesc/></teiHeader>" +
            "<text><body><p>a<note>b</note></p></body></text></TEI>");

        var map = IndexedTranslationService.BuildNodePathMap(doc);

        foreach (var el in doc.Root!.DescendantsAndSelf())
            Assert.True(map.ContainsKey(el), $"missing path for <{el.Name.LocalName}>");
    }
}
