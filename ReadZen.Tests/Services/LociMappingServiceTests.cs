using System.Collections.Generic;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class LociMappingServiceTests
{
    private const string MiniTei = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TEI xmlns="http://www.tei-c.org/ns/1.0"><teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader><text><body><lg><l n="1" corresp="urn:locus:T1-p007.l01">first</l><l n="22" corresp="urn:locus:T1-p031.l01">second</l><l n="66" corresp="urn:locus:T1-p075" type="omission_judgment">third</l></lg></body></text></TEI>
        """;

    [Fact]
    public void BuildFromXml_ExtractsLElements()
    {
        var map = LociMappingService.BuildFromXml(MiniTei);

        Assert.Equal(3, map.Count);
        Assert.True(map.ContainsKey("l|1"));
        Assert.True(map.ContainsKey("l|22"));
        Assert.True(map.ContainsKey("l|66"));
        Assert.Equal("urn:locus:T1-p007.l01", map["l|1"].Corresp);
        Assert.Equal("urn:locus:T1-p031.l01", map["l|22"].Corresp);
        Assert.Equal("urn:locus:T1-p075", map["l|66"].Corresp);
    }

    [Fact]
    public void BuildFromXml_HandlesOmissionJudgmentType()
    {
        var map = LociMappingService.BuildFromXml(MiniTei);

        var entry = map["l|66"];
        Assert.Equal("omission_judgment", entry.Type);
    }

    [Fact]
    public void BuildFromXml_SkipsLWithoutN()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <TEI xmlns="http://www.tei-c.org/ns/1.0"><teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p/></publicationStmt><sourceDesc><p/></sourceDesc></fileDesc></teiHeader><text><body><lg><l corresp="foo">text</l></lg></body></text></TEI>
            """;

        var map = LociMappingService.BuildFromXml(xml);

        Assert.Empty(map);
    }

    [Fact]
    public void BuildFromXml_EmptyXml_ReturnsEmptyMap()
    {
        var map = LociMappingService.BuildFromXml(string.Empty);

        Assert.Empty(map);
    }

    [Fact]
    public void TryGetLocus_ReturnsCorresp()
    {
        var map = LociMappingService.BuildFromXml(MiniTei);

        var result = LociMappingService.TryGetLocus(map, "l|22");

        Assert.Equal("urn:locus:T1-p031.l01", result);
    }

    [Fact]
    public void TryGetLocus_ReturnsNull_WhenNotFound()
    {
        var map = LociMappingService.BuildFromXml(MiniTei);

        var result = LociMappingService.TryGetLocus(map, "l|999");

        Assert.Null(result);
    }

    [Fact]
    public void StripLocusUrn_RemovesPrefix()
    {
        var result = LociMappingService.StripLocusUrn("urn:locus:T1-p031.l01");

        Assert.Equal("T1-p031.l01", result);
    }

    [Fact]
    public void StripLocusUrn_PassesThroughNonUrn()
    {
        var result = LociMappingService.StripLocusUrn("T1-p031.l01");

        Assert.Equal("T1-p031.l01", result);
    }
}
