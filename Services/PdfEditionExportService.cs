// Services/PdfEditionExportService.cs — critical-edition PDF export via QuestPDF.
using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class PdfEditionExportService
{
    private const float BaseFontSize = 12f, ApparatusFontSize = 9f, HeaderFontSize = 9f;
    private const float TitleFontSize = 24f, SubtitleFontSize = 14f, LineNumberFontSize = 8f;

    /// <summary>Exports a critical edition PDF and returns the raw bytes.</summary>
    public static byte[] ExportPdf(
        string title, string author, string baseText,
        ApparatusInfo? apparatus, WitnessTextRegistry? witnesses,
        string? fontPath = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var fontFamily = "Noto Sans CJK SC";
        if (!string.IsNullOrEmpty(fontPath))
        {
            using var stream = System.IO.File.OpenRead(fontPath);
            QuestPDF.Drawing.FontManager.RegisterFont(stream);
        }

        var lines = SplitLines(baseText);
        var entries = apparatus?.Entries ?? new List<ApparatusEntry>();
        var witnessList = witnesses?.Witnesses ?? new List<WitnessTextEntry>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Title page
                page.Size(PageSizes.A4);
                page.Margin(60);
                page.Content().AlignCenter().AlignMiddle().Column(col =>
                {
                    col.Item().Text(title).FontSize(TitleFontSize).Bold().FontFamily(fontFamily);
                    col.Item().PaddingTop(12).Text(author).FontSize(SubtitleFontSize).FontFamily(fontFamily);
                    col.Item().PaddingTop(24).Text("Critical Edition").FontSize(SubtitleFontSize).Italic();
                    col.Item().PaddingTop(12).Text(DateTime.Now.ToString("yyyy-MM-dd")).FontSize(BaseFontSize);
                    if (witnessList.Count > 0)
                        col.Item().PaddingTop(8).Text($"{witnessList.Count} witness(es) collated").FontSize(BaseFontSize);
                });
            });

            // Witness legend page (only if witnesses exist)
            if (witnessList.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(60);
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(12).Text("Witness Legend").FontSize(SubtitleFontSize).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.ConstantColumn(60);  // Siglum
                                cd.RelativeColumn(3);   // Label
                                cd.RelativeColumn(1);   // Role
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(4).Text("Siglum").Bold().FontSize(BaseFontSize);
                                header.Cell().BorderBottom(1).Padding(4).Text("Description").Bold().FontSize(BaseFontSize);
                                header.Cell().BorderBottom(1).Padding(4).Text("Role").Bold().FontSize(BaseFontSize);
                            });

                            foreach (var w in witnessList)
                            {
                                table.Cell().Padding(4).Text(w.Siglum ?? w.WitnessId ?? "?").FontSize(BaseFontSize).FontFamily(fontFamily);
                                table.Cell().Padding(4).Text(w.Label ?? "").FontSize(BaseFontSize).FontFamily(fontFamily);
                                table.Cell().Padding(4).Text(w.Role ?? "").FontSize(BaseFontSize);
                            }
                        });
                    });
                });
            }

            // Text pages with apparatus
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.MarginLeft(70); // extra room for line numbers

                page.Header().BorderBottom(0.5f).PaddingBottom(4).Row(row =>
                {
                    row.RelativeItem().Text(title).FontSize(HeaderFontSize).FontFamily(fontFamily);
                    row.ConstantItem(60).AlignRight().Text(t =>
                        t.CurrentPageNumber().FontSize(HeaderFontSize));
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    // Build a lookup: line index → list of apparatus entries
                    var lineApparatus = BuildLineApparatusMap(lines, entries);

                    // Render body text (~70%) and apparatus (~30%) together.
                    // QuestPDF handles page breaks; we render all lines and
                    // collect apparatus per logical page via inline placement.
                    int footnoteCounter = 0;

                    for (int i = 0; i < lines.Count; i++)
                    {
                        int lineNum = i + 1;
                        bool hasApparatus = lineApparatus.ContainsKey(i);
                        if (hasApparatus) footnoteCounter++;
                        int marker = hasApparatus ? footnoteCounter : 0;

                        var lineIndex = i; // capture for closure
                        col.Item().Row(row =>
                        {
                            // Line number in left margin
                            row.ConstantItem(30).AlignRight().PaddingRight(8)
                               .Text($"{lineNum}").FontSize(LineNumberFontSize).FontColor(Colors.Grey.Medium);

                            // Text content
                            row.RelativeItem().Text(text =>
                            {
                                text.Span(lines[lineIndex]).FontSize(BaseFontSize).FontFamily(fontFamily);
                                if (marker > 0)
                                    text.Span($" ({marker})").FontSize(LineNumberFontSize).Superscript()
                                        .FontColor(Colors.Red.Medium);
                            });
                        });
                    }

                    // Apparatus block at the end
                    if (footnoteCounter > 0)
                    {
                        col.Item().PaddingTop(16).BorderTop(0.5f).PaddingTop(8)
                           .Text("Apparatus").FontSize(ApparatusFontSize).Bold();

                        int appNum = 0;
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (!lineApparatus.ContainsKey(i)) continue;
                            appNum++;
                            foreach (var entry in lineApparatus[i])
                            {
                                var note = FormatApparatusEntry(appNum, entry, witnessList);
                                col.Item().Text(note).FontSize(ApparatusFontSize).FontFamily(fontFamily);
                            }
                        }
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>Splits base text into lines, preserving empty lines.</summary>
    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string> { "" };
        return text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
    }

    /// <summary>Maps line indices to apparatus entries by matching locus IDs to line content.</summary>
    private static Dictionary<int, List<ApparatusEntry>> BuildLineApparatusMap(
        List<string> lines, List<ApparatusEntry> entries)
    {
        var map = new Dictionary<int, List<ApparatusEntry>>();
        foreach (var entry in entries)
        {
            // Try to find the line containing the lemma
            var lemma = entry.Lemma;
            if (string.IsNullOrEmpty(lemma)) continue;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(lemma, StringComparison.Ordinal))
                {
                    if (!map.ContainsKey(i)) map[i] = new List<ApparatusEntry>();
                    map[i].Add(entry);
                    break; // first match only
                }
            }
        }
        return map;
    }

    /// <summary>Formats an apparatus entry in Leiden-style notation.</summary>
    private static string FormatApparatusEntry(int num, ApparatusEntry entry,
        List<WitnessTextEntry> witnesses)
    {
        var readings = entry.Readings ?? new List<ApparatusReading>();
        if (readings.Count == 0)
            return $"{num}) {entry.Lemma ?? "—"}";

        var parts = new List<string>();
        foreach (var r in readings)
        {
            var siglum = ResolveSiglum(r.WitnessId, witnesses);
            var readingText = r.Reading ?? "om.";
            parts.Add($"{readingText} {siglum}");
        }

        return $"{num}) {entry.Lemma ?? "—"} ] {string.Join(" | ", parts)}";
    }

    /// <summary>Resolves a witness ID to its siglum for display.</summary>
    private static string ResolveSiglum(string? witnessId, List<WitnessTextEntry> witnesses)
    {
        if (string.IsNullOrEmpty(witnessId)) return "?";
        var w = witnesses.FirstOrDefault(x =>
            string.Equals(x.WitnessId, witnessId, StringComparison.OrdinalIgnoreCase));
        return w?.Siglum ?? witnessId;
    }
}
