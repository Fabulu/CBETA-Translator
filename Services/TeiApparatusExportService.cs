// Services/TeiApparatusExportService.cs
// Exports critical apparatus data as TEI XML with <listWit> and <listApp> sections.

using System.Linq;
using System.Text;
using System.Xml.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Generates TEI XML enriched with critical apparatus entries and witness declarations.
/// </summary>
public static class TeiApparatusExportService
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";
    private static readonly XNamespace Xml = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// Merges apparatus data and witness metadata into a base TEI XML document.
    /// Returns the complete XML string with &lt;listWit&gt; and &lt;listApp&gt; sections.
    /// </summary>
    public static string ExportTeiWithApparatus(
        string baseTeiXml,
        ApparatusInfo apparatus,
        WitnessTextRegistry? witnesses)
    {
        var doc = XDocument.Parse(baseTeiXml);
        var ns = doc.Root?.Name.Namespace ?? Tei;

        AddWitnessList(doc, ns, witnesses);
        AddApparatusSection(doc, ns, apparatus);

        return doc.Declaration != null
            ? doc.Declaration + "\n" + doc
            : doc.ToString();
    }

    private static void AddWitnessList(XDocument doc, XNamespace ns, WitnessTextRegistry? witnesses)
    {
        if (witnesses?.Witnesses is not { Count: > 0 } witList) return;

        var sourceDesc = doc.Descendants(ns + "sourceDesc").FirstOrDefault();
        if (sourceDesc == null)
        {
            var fileDesc = doc.Descendants(ns + "fileDesc").FirstOrDefault();
            if (fileDesc == null) return;
            sourceDesc = new XElement(ns + "sourceDesc");
            fileDesc.Add(sourceDesc);
        }

        var listWit = new XElement(ns + "listWit");
        foreach (var w in witList)
        {
            var witnessEl = new XElement(ns + "witness",
                new XAttribute(Xml + "id", w.WitnessId ?? w.Siglum ?? "unknown"),
                w.Label ?? w.Siglum ?? w.WitnessId ?? "");
            listWit.Add(witnessEl);
        }

        sourceDesc.Add(listWit);
    }

    private static void AddApparatusSection(XDocument doc, XNamespace ns, ApparatusInfo apparatus)
    {
        if (apparatus.Entries is not { Count: > 0 } entries) return;

        var body = doc.Descendants(ns + "body").FirstOrDefault();
        var back = doc.Descendants(ns + "back").FirstOrDefault();
        if (back == null)
        {
            back = new XElement(ns + "back");
            body?.Parent?.Add(back);
            if (back.Parent == null)
                doc.Root?.Add(back);
        }

        var div = new XElement(ns + "div", new XAttribute("type", "apparatus"));
        var listApp = new XElement(ns + "listApp");

        foreach (var entry in entries)
        {
            var app = new XElement(ns + "app");
            if (!string.IsNullOrEmpty(entry.LocusId))
            {
                app.SetAttributeValue("from", "#" + entry.LocusId);
                app.SetAttributeValue("to", "#" + entry.LocusId);
            }

            // Lemma element
            var lem = new XElement(ns + "lem", entry.Lemma ?? "");
            if (entry.WitnessesSupporting is { Count: > 0 })
                lem.SetAttributeValue("wit", string.Join(" ", entry.WitnessesSupporting.Select(w => "#" + w)));
            app.Add(lem);

            // Reading elements
            if (entry.Readings != null)
            {
                foreach (var r in entry.Readings)
                {
                    var rdg = new XElement(ns + "rdg", r.Reading ?? "");
                    if (!string.IsNullOrEmpty(r.WitnessId))
                        rdg.SetAttributeValue("wit", "#" + r.WitnessId);
                    if (!string.IsNullOrEmpty(r.Certainty))
                        rdg.SetAttributeValue("cert", MapCertainty(r.Certainty));
                    if (!string.IsNullOrEmpty(r.Type))
                        rdg.SetAttributeValue("type", r.Type);
                    if (!string.IsNullOrEmpty(r.Editor))
                        rdg.SetAttributeValue("resp", "#" + r.Editor);
                    app.Add(rdg);
                }
            }

            listApp.Add(app);
        }

        div.Add(listApp);
        back.Add(div);
    }

    private static string MapCertainty(string certainty) => certainty?.ToLowerInvariant() switch
    {
        "high" => "high",
        "medium" => "medium",
        "low" => "low",
        _ => "unknown",
    };
}
