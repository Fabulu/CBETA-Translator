// Services/StemmaParserService.cs
// Parses witness stemma data from adjacency-list markdown or witness registry.
// Used by the Collation tab's Stemma sub-tab.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Parses stemma (witness transmission tree) data from markdown files
/// or generates a basic stemma from witness registry family groupings.
/// </summary>
public static class StemmaParserService
{
    /// <summary>Result of stemma parsing: directed edges and unique node names.</summary>
    public sealed class StemmaData
    {
        public List<(string From, string To)> Edges { get; } = new();
        public List<string> NodeNames { get; } = new();
    }

    /// <summary>
    /// Tries to parse a family-stemma.md file. Supports two formats:
    /// 1. Adjacency list: "parent -> child" lines
    /// 2. ASCII tree: indented tree with | and + connectors
    /// Returns null if the file doesn't exist or has no parseable data.
    /// </summary>
    public static StemmaData? TryParseFile(string path)
    {
        if (!File.Exists(path)) return null;

        string[] lines;
        try { lines = File.ReadAllLines(path); }
        catch { return null; }

        // First try adjacency-list format (arrow notation)
        var arrowData = ParseArrowFormat(lines);
        if (arrowData != null && arrowData.Edges.Count > 0) return arrowData;

        // Then try to extract relationships from ASCII tree
        var asciiData = ParseAsciiTree(lines);
        if (asciiData != null && asciiData.Edges.Count > 0) return asciiData;

        return null;
    }

    /// <summary>
    /// Parses lines like "archetype -> T1" or "parent -> child".
    /// Lines starting with # are comments/headers and are skipped.
    /// </summary>
    private static StemmaData? ParseArrowFormat(string[] lines)
    {
        var data = new StemmaData();
        var nodeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            // Match "from -> to" pattern
            var arrowIdx = trimmed.IndexOf("->", StringComparison.Ordinal);
            if (arrowIdx < 0) continue;

            var from = trimmed[..arrowIdx].Trim();
            var to = trimmed[(arrowIdx + 2)..].Trim();

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) continue;

            data.Edges.Add((from, to));
            nodeSet.Add(from);
            nodeSet.Add(to);
        }

        if (nodeSet.Count == 0) return null;
        data.NodeNames.AddRange(nodeSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        return data;
    }

    /// <summary>
    /// Extracts edges from ASCII-art tree diagrams by detecting bracketed node
    /// names [NodeName] and inferring parent-child from the visual hierarchy.
    /// Falls back to extracting witness IDs from indented lines.
    /// </summary>
    private static StemmaData? ParseAsciiTree(string[] lines)
    {
        // Look for lines inside ``` code blocks that have bracketed names
        var data = new StemmaData();
        var nodeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool inCodeBlock = false;

        // Collect bracketed entries with their indentation level
        var entries = new List<(int Indent, string Name)>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (!inCodeBlock) continue;

            // Find bracketed names like [Original 信心銘] or plain witness IDs
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Extract all [Name] tokens from the line
            int searchStart = 0;
            while (searchStart < trimmed.Length)
            {
                int open = trimmed.IndexOf('[', searchStart);
                if (open < 0) break;
                int close = trimmed.IndexOf(']', open + 1);
                if (close < 0) break;

                var name = trimmed[(open + 1)..close].Trim();
                if (!string.IsNullOrEmpty(name))
                    entries.Add((open, name));

                searchStart = close + 1;
            }

            // Also look for plain witness IDs (alphanumeric-dash tokens not part of connectors)
            // These appear as leaf nodes like "korea-commons", "ndl-2537640"
            if (entries.Count == 0 || !trimmed.Contains('['))
            {
                // Check for witness-like tokens (contain letters and possibly digits/hyphens)
                var tokens = trimmed.Split(new[] { ' ', '|', '+', '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tok in tokens)
                {
                    // Skip pure punctuation, numbers, and connector chars
                    if (tok.All(c => c == '(' || c == ')' || char.IsDigit(c) || c == ',' || c == '.'))
                        continue;
                    // A witness ID pattern: contains at least one letter and no spaces
                    if (tok.Any(char.IsLetter) && tok.Length >= 3 &&
                        !tok.StartsWith('(') && !tok.EndsWith(')') &&
                        tok != "derivative")
                    {
                        int indent = trimmed.IndexOf(tok, StringComparison.Ordinal);
                        entries.Add((indent, tok));
                    }
                }
            }
        }

        // Build parent-child edges from indent levels using a stack
        if (entries.Count < 2) return null;

        // Simple heuristic: nodes with smaller indent are parents of nodes with larger indent
        // We use a stack to track the current parent chain
        var parentStack = new Stack<(int Indent, string Name)>();

        foreach (var entry in entries)
        {
            // Pop nodes from stack that are at same or deeper indent
            while (parentStack.Count > 0 && parentStack.Peek().Indent >= entry.Indent)
                parentStack.Pop();

            if (parentStack.Count > 0)
            {
                var parent = parentStack.Peek();
                data.Edges.Add((parent.Name, entry.Name));
            }

            nodeSet.Add(entry.Name);
            parentStack.Push(entry);
        }

        if (nodeSet.Count == 0) return null;
        data.NodeNames.AddRange(nodeSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        return data;
    }

    /// <summary>
    /// Generates a basic stemma from a witness registry by grouping witnesses
    /// by family_id. Each family becomes a parent node, witnesses are children.
    /// Used as fallback when no family-stemma.md exists.
    /// </summary>
    public static StemmaData? GenerateFromRegistry(WitnessTextRegistry? registry)
    {
        if (registry?.Witnesses is not { Count: >= 2 }) return null;

        var data = new StemmaData();
        var nodeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Group by family
        var families = registry.Witnesses
            .Where(w => !string.IsNullOrWhiteSpace(w.FamilyId))
            .GroupBy(w => w.FamilyId!, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (families.Count == 0)
        {
            // No family info — create a flat star from "archetype"
            nodeSet.Add("archetype");
            foreach (var w in registry.Witnesses)
            {
                var name = w.Siglum ?? w.WitnessId ?? "?";
                nodeSet.Add(name);
                data.Edges.Add(("archetype", name));
            }
        }
        else
        {
            // Root node
            nodeSet.Add("archetype");

            foreach (var family in families)
            {
                var familyName = family.Key;
                nodeSet.Add(familyName);
                data.Edges.Add(("archetype", familyName));

                foreach (var w in family)
                {
                    var name = w.Siglum ?? w.WitnessId ?? "?";
                    nodeSet.Add(name);
                    data.Edges.Add((familyName, name));
                }
            }

            // Witnesses without a family — attach directly to archetype
            foreach (var w in registry.Witnesses.Where(w => string.IsNullOrWhiteSpace(w.FamilyId)))
            {
                var name = w.Siglum ?? w.WitnessId ?? "?";
                nodeSet.Add(name);
                data.Edges.Add(("archetype", name));
            }
        }

        if (data.Edges.Count == 0) return null;
        data.NodeNames.AddRange(nodeSet.OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        return data;
    }
}
