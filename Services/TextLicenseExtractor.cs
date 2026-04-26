// Services/TextLicenseExtractor.cs
// Pure parser: TEI XDocument -> TextLicenseInfo. Walks
// teiHeader/fileDesc/{titleStmt,publicationStmt/availability,sourceDesc}
// and applies keyword-based license classification. Returns null when no
// <teiHeader> exists; returns TextLicenseInfo.UnknownDefault-shaped records
// when a header exists but no availability block is parseable.
//
// MVP keyword rules (in priority order, first match wins):
//   "CC BY-SA"             -> CopyleftAttribution, share-alike, commercial OK
//   "CC BY-NC" | "CBETA non-commercial" | "non-commercial use only"
//                          -> NonCommercial, commercial NOT OK
//   "CC BY"                -> PermissiveAttribution, commercial OK
//   "MIT"                  -> PermissiveAttribution, commercial OK
//   "CC0" | "public domain"| "PD-old" | "publicdomain/mark"
//                          -> PublicDomain, commercial OK
//   "all rights reserved"  -> AllRightsReserved, commercial NOT OK
//   (nothing matches)      -> Unknown
//
// Corpus detection hint:
//   - Header text mentions "OpenZenTexts" or "no CBETA marker"  -> Corpus.Open
//   - Header text mentions "CBETA" strongly                     -> Corpus.Cbeta
//   - else Unknown (the CorpusDetector has the folder-level fallback)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class TextLicenseExtractor
{
    private static readonly XNamespace Tei = "http://www.tei-c.org/ns/1.0";

    /// <summary>Parse an XML string. Returns null if parsing fails or no teiHeader.</summary>
    public static TextLicenseInfo? Extract(string xmlText, string? relPath = null)
    {
        if (string.IsNullOrWhiteSpace(xmlText)) return null;
        try
        {
            var doc = XDocument.Parse(xmlText, LoadOptions.None);
            return Extract(doc, relPath);
        }
        catch (Exception) { return null; }
    }

    // Dynasty prefixes commonly found in CBETA author fields, ordered longest-first
    // to avoid partial matches (e.g. "後秦" before "秦").
    private static readonly string[] DynastyPrefixes =
    {
        "北魏", "東晉", "西晉", "劉宋", "後秦", "後漢",
        "宋", "唐", "梁", "隋", "明", "元", "清", "陳", "齊", "吳", "秦"
    };

    /// <summary>Extract from a pre-parsed XDocument (cheaper when the caller already has one).</summary>
    public static TextLicenseInfo? Extract(XDocument doc, string? relPath = null)
    {
        var root = doc.Root;
        if (root == null) return null;

        var header = root.Element(Tei + "teiHeader") ?? root.Element("teiHeader");
        if (header == null) return null;

        var fileDesc = header.Element(Tei + "fileDesc") ?? header.Element("fileDesc");
        var titleStmt = fileDesc?.Element(Tei + "titleStmt") ?? fileDesc?.Element("titleStmt");
        var pubStmt = fileDesc?.Element(Tei + "publicationStmt") ?? fileDesc?.Element("publicationStmt");
        var availability = pubStmt?.Element(Tei + "availability") ?? pubStmt?.Element("availability");
        var sourceDesc = fileDesc?.Element(Tei + "sourceDesc") ?? fileDesc?.Element("sourceDesc");

        // --- title / author / year ---
        string? title = null;
        string? titleZh = null;
        string? author = null;
        string? year = null;
        string? dynasty = null;
        if (titleStmt != null)
        {
            var titles = titleStmt.Elements(Tei + "title").Concat(titleStmt.Elements("title")).ToList();
            // Prefer xml:lang="en" if present, else first non-alt
            var en = titles.FirstOrDefault(t => (string?)t.Attribute(XNamespace.Xml + "lang") == "en");
            var nonAlt = titles.FirstOrDefault(t => (string?)t.Attribute("type") != "alt");
            title = (en ?? nonAlt ?? titles.FirstOrDefault())?.Value?.Trim();

            // Chinese title: <title xml:lang="zh-Hant" level="m">
            var zhM = titles.FirstOrDefault(t =>
                (string?)t.Attribute(XNamespace.Xml + "lang") == "zh-Hant" &&
                (string?)t.Attribute("level") == "m");
            titleZh = zhM?.Value?.Trim();

            author = (titleStmt.Element(Tei + "author") ?? titleStmt.Element("author"))?.Value?.Trim();

            // Extract dynasty prefix from author (e.g. "唐 慧菀述" -> "唐")
            if (!string.IsNullOrWhiteSpace(author))
            {
                foreach (var dp in DynastyPrefixes)
                {
                    if (author.StartsWith(dp, StringComparison.Ordinal))
                    {
                        dynasty = dp;
                        break;
                    }
                }
            }
        }

        // --- CBETA-specific identifiers from publicationStmt ---
        string? cbetaCanon = null;
        int? cbetaVolume = null;
        string? cbetaNumber = null;
        string? cbetaVersionDate = null;
        if (pubStmt != null)
        {
            var date = pubStmt.Element(Tei + "date") ?? pubStmt.Element("date");
            year = (string?)date?.Attribute("when") ?? date?.Value?.Trim();
            cbetaVersionDate = date?.Value?.Trim();

            // <idno type="CBETA"> contains nested <idno type="canon">, <idno type="vol">, <idno type="no">
            foreach (var idno in pubStmt.Descendants(Tei + "idno").Concat(pubStmt.Descendants("idno")))
            {
                var idnoType = (string?)idno.Attribute("type");
                if (idnoType == null) continue;

                // Direct text content (excluding child elements)
                var directText = string.Concat(idno.Nodes().OfType<XText>().Select(t => t.Value)).Trim();

                switch (idnoType)
                {
                    case "canon":
                        cbetaCanon = directText;
                        break;
                    case "vol":
                        if (int.TryParse(directText, out var vol))
                            cbetaVolume = vol;
                        break;
                    case "no":
                        cbetaNumber = directText;
                        break;
                }
            }
        }

        // --- sourceDesc/bibl -> SourceEdition ---
        string? sourceEdition = null;
        if (sourceDesc != null)
        {
            var bibl = sourceDesc.Element(Tei + "bibl") ?? sourceDesc.Element("bibl");
            var biblText = bibl?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(biblText))
                sourceEdition = biblText;
        }

        // --- extent ---
        string? extent = null;
        var extentEl = fileDesc?.Element(Tei + "extent") ?? fileDesc?.Element("extent");
        if (extentEl != null)
        {
            var extText = extentEl.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(extText))
                extent = extText;
        }

        // --- Derive FileId: Canon + Volume + "n" + Number (e.g. "T48n2005") ---
        string? fileId = null;
        if (!string.IsNullOrWhiteSpace(cbetaCanon) && !string.IsNullOrWhiteSpace(cbetaNumber))
        {
            var volPart = cbetaVolume.HasValue ? cbetaVolume.Value.ToString() : "";
            fileId = $"{cbetaCanon}{volPart}n{cbetaNumber}";
        }

        // --- availability text bucket ---
        var availBuf = new StringBuilder();
        var licenceTargets = new List<string>();
        if (availability != null)
        {
            foreach (var lic in availability.Elements(Tei + "licence").Concat(availability.Elements("licence")))
            {
                var tgt = (string?)lic.Attribute("target");
                if (!string.IsNullOrWhiteSpace(tgt)) licenceTargets.Add(tgt!);
            }
            availBuf.Append(availability.Value);
        }
        string availText = availBuf.ToString();

        // --- keyword classification ---
        var (cls, shortLabel, attrReq, shareAlike, commOk) = Classify(availText, licenceTargets);

        // --- short-label override if <licence target> gives us a canonical URL ---
        if (cls != LicenseClass.Unknown && string.IsNullOrEmpty(shortLabel))
            shortLabel = ShortLabelFromTargets(licenceTargets) ?? shortLabel;

        // --- rights basis + provenance flags ---
        string? rightsBasis = FindLabeledParagraph(availability, "Rights basis");
        string? vetting = ExtractVettingConfidence(availText);
        bool noCbeta = availText.Contains("no CBETA marker", StringComparison.OrdinalIgnoreCase)
                    || availText.Contains("no CBETA material", StringComparison.OrdinalIgnoreCase)
                    || availText.Contains("independent of CBETA", StringComparison.OrdinalIgnoreCase);
        string? requiredAttribution = FindLabeledParagraph(availability, "Required attribution");

        // --- URLs: first URL inside availText; permalink if we can spot one ---
        var urls = ExtractUrls(availText);
        string? sourceUrl = urls.FirstOrDefault(u =>
            u.Contains("wikisource", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("cbeta", StringComparison.OrdinalIgnoreCase)) ?? urls.FirstOrDefault();
        string? stableRevisionUrl = urls.FirstOrDefault(u =>
            u.Contains("oldid=", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("permalink", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("Special:PermanentLink", StringComparison.OrdinalIgnoreCase));

        // sourceDesc may carry an additional digital source link
        if (sourceDesc != null && sourceUrl == null)
        {
            var sdUrls = ExtractUrls(sourceDesc.Value);
            sourceUrl = sdUrls.FirstOrDefault();
        }

        // --- corpus hint ---
        // Detection priority:
        //   1. Explicit OpenZenTexts marker or "no CBETA material" provenance
        //      check → Open. This handles OpenZenTexts files that legitimately
        //      mention "CBETA" in disclaimers without being CBETA-derived.
        //   2. CBETA-specific structural markers (idno type="CBETA",
        //      cbeta.org URL, "CBETA Maintenance Committee", "from CBETA's
        //      edition") → Cbeta.
        //   3. CBETA non-commercial license → Cbeta.
        //   4. Otherwise Unknown — leave it to the folder-name heuristic.
        //
        // The previous version inferred Cbeta from any "CBETA" substring,
        // which false-positives on OpenZenTexts files whose headers mention
        // CBETA in legal disclaimers ("excludes CBETA-derived material",
        // "never to collide with CBETA notation", etc.).
        CorpusKind corpusHint = CorpusKind.Unknown;
        if (header.Value.Contains("OpenZenTexts", StringComparison.OrdinalIgnoreCase) || noCbeta)
        {
            corpusHint = CorpusKind.Open;
        }
        else if (HasCbetaStructuralMarker(header) || cls == LicenseClass.NonCommercial)
        {
            corpusHint = CorpusKind.Cbeta;
        }

        return new TextLicenseInfo
        {
            Corpus = corpusHint,
            LicenseClass = cls,
            ShortLabel = string.IsNullOrEmpty(shortLabel) ? "Unknown" : shortLabel,
            LongText = availText.Trim(),
            Title = title,
            TitleZh = titleZh,
            Author = author,
            YearComposed = year,
            SourceUrl = sourceUrl,
            StableRevisionUrl = stableRevisionUrl,
            RightsBasisText = rightsBasis,
            VettingConfidence = vetting,
            NoCbetaMaterial = noCbeta,
            RequiredAttribution = requiredAttribution,
            AttributionRequired = attrReq,
            ShareAlikeRequired = shareAlike,
            CommercialUseAllowed = commOk,
            CbetaCanon = cbetaCanon,
            CbetaVolume = cbetaVolume,
            CbetaNumber = cbetaNumber,
            SourceEdition = sourceEdition,
            Extent = extent,
            Dynasty = dynasty,
            CbetaVersionDate = cbetaVersionDate,
            FileId = fileId,
            RelPath = relPath
        };
    }

    private static (LicenseClass cls, string shortLabel, bool attrReq, bool shareAlike, bool commOk)
        Classify(string availText, List<string> targets)
    {
        string hay = (availText + " " + string.Join(" ", targets)).ToLowerInvariant();

        if (hay.Contains("cc by-sa") || hay.Contains("by-sa/4"))
            return (LicenseClass.CopyleftAttribution, "CC-BY-SA-4.0", true, true, true);
        // CBETA-specific non-commercial check MUST come before the generic
        // non-commercial check, otherwise CBETA files get the generic
        // "NonCommercial" label instead of the more informative "CBETA-NC".
        // Both branches resolve to LicenseClass.NonCommercial — only the
        // short label differs.
        if (hay.Contains("cbeta") && (hay.Contains("non-commercial") || hay.Contains("academic")))
            return (LicenseClass.NonCommercial, "CBETA-NC", true, false, false);
        if (hay.Contains("cc by-nc"))
            return (LicenseClass.NonCommercial, "CC-BY-NC-4.0", true, false, false);
        if (hay.Contains("non-commercial") || hay.Contains("noncommercial"))
            return (LicenseClass.NonCommercial, "Non-commercial", true, false, false);
        if (hay.Contains("cc by") || hay.Contains("by/4"))
            return (LicenseClass.PermissiveAttribution, "CC-BY-4.0", true, false, true);
        if (hay.Contains("mit license") || hay.Contains("mit/"))
            return (LicenseClass.PermissiveAttribution, "MIT", true, false, true);
        if (hay.Contains("cc0") || hay.Contains("publicdomain/zero"))
            return (LicenseClass.PublicDomain, "CC0-1.0", false, false, true);
        if (hay.Contains("public domain") || hay.Contains("pd-old") || hay.Contains("publicdomain/mark"))
            return (LicenseClass.PublicDomain, "PD-old", false, false, true);
        if (hay.Contains("all rights reserved"))
            return (LicenseClass.AllRightsReserved, "All rights reserved", true, false, false);

        return (LicenseClass.Unknown, "", false, false, false);
    }

    private static string? ShortLabelFromTargets(List<string> targets)
    {
        foreach (var t in targets)
        {
            if (t.Contains("by-sa/4", StringComparison.OrdinalIgnoreCase)) return "CC-BY-SA-4.0";
            if (t.Contains("by/4", StringComparison.OrdinalIgnoreCase)) return "CC-BY-4.0";
            if (t.Contains("publicdomain/mark", StringComparison.OrdinalIgnoreCase)) return "PD-old";
            if (t.Contains("publicdomain/zero", StringComparison.OrdinalIgnoreCase)) return "CC0-1.0";
        }
        return null;
    }

    private static string? FindLabeledParagraph(XElement? availability, string labelPrefix)
    {
        if (availability == null) return null;
        foreach (var p in availability.Descendants(Tei + "p").Concat(availability.Descendants("p")))
        {
            var label = p.Element(Tei + "label") ?? p.Element("label");
            if (label == null) continue;
            var ltext = label.Value?.Trim() ?? "";
            if (ltext.StartsWith(labelPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Return the paragraph text minus the label
                var full = p.Value?.Trim() ?? "";
                if (full.StartsWith(ltext)) full = full.Substring(ltext.Length).Trim();
                return full.TrimStart(':').Trim();
            }
        }
        return null;
    }

    private static string? ExtractVettingConfidence(string text)
    {
        var m = Regex.Match(text, @"[Vv]etting\s+confidence[:\s]+(high|medium|low)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    private static List<string> ExtractUrls(string text)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(text)) return list;
        foreach (Match m in Regex.Matches(text, @"https?://[^\s<>""]+"))
        {
            var url = m.Value.TrimEnd('.', ',', ')', ';', '"');
            list.Add(url);
        }
        return list;
    }

    /// <summary>
    /// True if the teiHeader contains a CBETA-specific structural marker that
    /// proves the file IS CBETA-derived. Generic mentions of the word "CBETA"
    /// in disclaimer text don't count — those false-positive on OpenZenTexts
    /// files that explicitly disclaim CBETA-derived content.
    /// </summary>
    private static bool HasCbetaStructuralMarker(XElement header)
    {
        // 1) <idno type="CBETA"> — the canonical CBETA identifier element
        foreach (var idno in header.Descendants(Tei + "idno").Concat(header.Descendants("idno")))
        {
            var t = (string?)idno.Attribute("type");
            if (string.Equals(t, "CBETA", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 2) cbeta.org URL anywhere in the header
        var headerText = header.Value;
        if (string.IsNullOrEmpty(headerText)) return false;
        if (headerText.Contains("cbeta.org", StringComparison.OrdinalIgnoreCase))
            return true;

        // 3) Boilerplate phrases that appear ONLY in CBETA-derived TEI files
        //    (not in OpenZenTexts disclaimer text)
        if (headerText.Contains("CBETA Maintenance Committee", StringComparison.OrdinalIgnoreCase))
            return true;
        if (headerText.Contains("CBETA Chinese Electronic Tripitaka", StringComparison.OrdinalIgnoreCase))
            return true;
        if (headerText.Contains("Available for non-commercial use when distributed with this header", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
