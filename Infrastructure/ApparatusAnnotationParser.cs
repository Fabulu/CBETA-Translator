// Infrastructure/ApparatusAnnotationParser.cs
// Parses apparatus annotation text (format "Lem: X\nRdg: Y [wit]\nRdg: Z [wit2]")
// into an ApparatusEntry. Extracted from ReadableTabView/ScholarTabViewModel so it
// can be unit-tested independently.

using System;
using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

public static class ApparatusAnnotationParser
{
    /// <summary>
    /// Parses apparatus annotation text (format "Lem: X\nRdg: Y [wit]\nRdg: Z [wit2]")
    /// into an ApparatusEntry with Lemma and Readings populated.
    /// </summary>
    public static ApparatusEntry? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string? lemma = null;
        var readings = new List<ApparatusReading>();

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("Lem:", StringComparison.Ordinal))
            {
                lemma = line.Substring(4).Trim();
            }
            else if (line.StartsWith("Rdg:", StringComparison.Ordinal))
            {
                var rdgText = line.Substring(4).Trim();
                string? witnessId = null;
                // Extract witness from trailing "[wit]"
                int bracketStart = rdgText.LastIndexOf('[');
                int bracketEnd = rdgText.LastIndexOf(']');
                if (bracketStart >= 0 && bracketEnd > bracketStart)
                {
                    witnessId = rdgText.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();
                    rdgText = rdgText.Substring(0, bracketStart).Trim();
                }
                readings.Add(new ApparatusReading
                {
                    WitnessId = witnessId,
                    Reading = rdgText
                });
            }
        }

        if (lemma == null && readings.Count == 0)
            return null;

        return new ApparatusEntry
        {
            Lemma = lemma,
            Readings = readings.Count > 0 ? readings : null
        };
    }
}
