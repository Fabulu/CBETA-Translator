using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

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

        // different => yellow unless body has zero CJK => green
        try
        {
            bool hasCjkText = BodyHasCjkTextNodesOnly(tranPath);
            if (!hasCjkText)
                return TranslationStatus.Green;
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

    internal static bool BodyHasCjkTextNodesOnly(string xmlPath)
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
        var ignoreCjkStack = new System.Collections.Generic.Stack<bool>();
        var elementStack = new System.Collections.Generic.Stack<string>();

        while (reader.Read())
        {
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
                        {
                            ignoreHere = true;
                        }
                        if (!ignoreHere && local.Equals("note", StringComparison.OrdinalIgnoreCase))
                        {
                            var typeAttr = reader.GetAttribute("type");
                            if (!string.IsNullOrWhiteSpace(typeAttr) &&
                                typeAttr.Equals("community", StringComparison.OrdinalIgnoreCase))
                            {
                                ignoreHere = true;
                            }
                        }

                        ignoreCjkStack.Push(ignoreHere);

                        if (reader.IsEmptyElement)
                        {
                            elementStack.Pop();
                            ignoreCjkStack.Pop();

                            if (inBody && local.Equals("body", StringComparison.OrdinalIgnoreCase))
                                return false;
                        }

                        break;
                    }

                case XmlNodeType.EndElement:
                    {
                        var local = reader.LocalName;

                        if (elementStack.Count > 0) elementStack.Pop();
                        if (ignoreCjkStack.Count > 0) ignoreCjkStack.Pop();

                        if (local.Equals("body", StringComparison.OrdinalIgnoreCase))
                            return false;

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
                        if (!string.IsNullOrEmpty(text) && CjkRegex.IsMatch(text))
                            return true;

                        break;
                    }
            }
        }

        return false;
    }
}
