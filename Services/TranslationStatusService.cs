using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Computes per-file translation status (Red/Yellow/Green) by comparing
/// original and translated XML files. Extracted from IndexCacheService
/// during Wave 7 service split.
/// </summary>
public sealed class TranslationStatusService : ITranslationStatusService
{
    private static readonly Regex CjkRegex = new Regex(
        @"[\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches any Latin/ASCII letter — evidence that English translation
    /// content exists in a text node (as opposed to pure CJK or whitespace/
    /// punctuation). Used to distinguish "partially translated" (Yellow) from
    /// "Chinese-only stub" (Red).
    /// </summary>
    private static readonly Regex NonCjkLetterRegex = new Regex(
        @"[A-Za-z]",
        RegexOptions.Compiled);

    public TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true)
    {
        return ComputeStatus(origAbs, tranAbs, rootForLogs, relKeyForLogs, verboseLog);
    }

    internal static TranslationStatus ComputeStatus(
        string origPath, string tranPath, string rootForLogs, string relKeyForLogs, bool verboseLog)
    {
        // missing translated => red
        if (!File.Exists(tranPath))
            return TranslationStatus.Red;

        // identical bytes => red
        bool same;
        try
        {
            same = FilesEqualFast(origPath, tranPath);
        }
        catch
        {
            same = false;
        }

        if (same)
            return TranslationStatus.Red;

        // File differs from original — determine whether it's been translated.
        // Check the body for CJK (remaining Chinese) and non-CJK (English).
        // Red = no English at all (untranslated stub that diverged from
        //       original, e.g. via auto-generation with different header).
        // Yellow = has both CJK and non-CJK text (partially translated).
        // Green = no CJK remaining (fully translated).
        try
        {
            var (hasCjk, hasNonCjk) = BodyTextAnalysis(tranPath);
            if (!hasCjk)
                return TranslationStatus.Green;  // fully translated
            if (!hasNonCjk)
                return TranslationStatus.Red;    // Chinese-only stub, no EN at all
        }
        catch
        {
            // fall through to yellow
        }

        return TranslationStatus.Yellow;
    }

    private static bool FilesEqualFast(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;

        const int Buf = 1024 * 64;
        byte[] ba = new byte[Buf];
        byte[] bb = new byte[Buf];

        using var sa = File.OpenRead(a);
        using var sb = File.OpenRead(b);

        while (true)
        {
            int ra = sa.Read(ba, 0, Buf);
            int rb = sb.Read(bb, 0, Buf);
            if (ra != rb) return false;
            if (ra == 0) return true;

            for (int i = 0; i < ra; i++)
                if (ba[i] != bb[i]) return false;
        }
    }

    /// <summary>
    /// Scans the &lt;body&gt; of a TEI XML file and reports whether it contains
    /// CJK text (remaining Chinese) and/or non-CJK text (English translation).
    /// Used to distinguish three states:
    ///   - (hasCjk=false) → fully translated (Green)
    ///   - (hasCjk=true, hasNonCjk=false) → Chinese-only stub, no EN (Red)
    ///   - (hasCjk=true, hasNonCjk=true) → partially translated (Yellow)
    /// </summary>
    internal static (bool HasCjk, bool HasNonCjk) BodyTextAnalysis(string xmlPath)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = false,
            CloseInput = true,
        };

        using var fs = File.OpenRead(xmlPath);
        using var reader = XmlReader.Create(fs, settings);

        bool inBody = false;
        bool foundCjk = false;
        bool foundNonCjk = false;
        var ignoreCjkStack = new System.Collections.Generic.Stack<bool>();
        var elementStack = new System.Collections.Generic.Stack<string>();

        while (reader.Read())
        {
            // Early exit: both flags set, no need to keep scanning.
            if (foundCjk && foundNonCjk)
                return (true, true);

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        var local = reader.LocalName;

                        if (local.Equals("body", StringComparison.OrdinalIgnoreCase))
                            inBody = true;

                        elementStack.Push(local);

                        bool parentIgnore = ignoreCjkStack.Count > 0 && ignoreCjkStack.Peek();
                        bool ignoreHere = parentIgnore;

                        if (!ignoreHere && local.Equals("mulu", StringComparison.OrdinalIgnoreCase))
                            ignoreHere = true;
                        if (!ignoreHere && local.Equals("note", StringComparison.OrdinalIgnoreCase))
                        {
                            var typeAttr = reader.GetAttribute("type");
                            if (!string.IsNullOrWhiteSpace(typeAttr) &&
                                typeAttr.Equals("community", StringComparison.OrdinalIgnoreCase))
                                ignoreHere = true;
                        }

                        ignoreCjkStack.Push(ignoreHere);

                        if (reader.IsEmptyElement)
                        {
                            elementStack.Pop();
                            ignoreCjkStack.Pop();

                            if (inBody && local.Equals("body", StringComparison.OrdinalIgnoreCase))
                                return (foundCjk, foundNonCjk);
                        }

                        break;
                    }

                case XmlNodeType.EndElement:
                    {
                        var local = reader.LocalName;

                        if (elementStack.Count > 0) elementStack.Pop();
                        if (ignoreCjkStack.Count > 0) ignoreCjkStack.Pop();

                        if (local.Equals("body", StringComparison.OrdinalIgnoreCase))
                            return (foundCjk, foundNonCjk);

                        break;
                    }

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                    {
                        if (!inBody) break;

                        if (ignoreCjkStack.Count > 0 && ignoreCjkStack.Peek())
                            break;

                        if (elementStack.Count > 0 &&
                            elementStack.Peek().Equals("g", StringComparison.OrdinalIgnoreCase))
                            break;

                        var text = reader.Value;
                        if (string.IsNullOrEmpty(text)) break;

                        if (CjkRegex.IsMatch(text))
                            foundCjk = true;

                        // Non-CJK = Latin/ASCII letters (English translation content).
                        // Whitespace/punctuation alone doesn't count.
                        if (NonCjkLetterRegex.IsMatch(text))
                            foundNonCjk = true;

                        break;
                    }
            }
        }

        return (foundCjk, foundNonCjk);
    }

    // Legacy wrapper for callers that only need the CJK check.
    internal static bool BodyHasCjkTextNodesOnly(string xmlPath) => BodyTextAnalysis(xmlPath).HasCjk;
}
