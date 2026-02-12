using System;
using System.Text;

namespace CbetaTranslator.App.Infrastructure;

public static class CommunityNoteXmlEditor
{
    public static string InsertCommunityNote(string xml, int xmlIndex, string noteText, string? resp)
    {
        xml ??= "";
        if (xml.Length == 0) return xml;

        xmlIndex = Math.Clamp(xmlIndex, 0, xml.Length);

        // Move insertion point to a safe location (not inside a tag).
        int safe = FindSafeInsertionIndex(xml, xmlIndex);

        string note = BuildCommunityNote(noteText, resp);

        var sb = new StringBuilder(xml.Length + note.Length + 8);
        sb.Append(xml, 0, safe);
        sb.Append(note);
        sb.Append(xml, safe, xml.Length - safe);
        return sb.ToString();
    }

    public static string DeleteSpan(string xml, int start, int endExclusive)
    {
        xml ??= "";
        if (xml.Length == 0) return xml;

        start = Math.Clamp(start, 0, xml.Length);
        endExclusive = Math.Clamp(endExclusive, 0, xml.Length);
        if (endExclusive <= start) return xml;

        return xml.Remove(start, endExclusive - start);
    }

    private static int FindSafeInsertionIndex(string xml, int idx)
    {
        idx = Math.Clamp(idx, 0, xml.Length);

        if (idx >= xml.Length) return xml.Length;
        if (idx <= 0) return 0;

        // If we are inside a tag, jump to after the closing '>' of that tag.
        if (IsInsideTag(xml, idx))
        {
            int gt = xml.IndexOf('>', idx);
            if (gt >= 0) return gt + 1;
            return xml.Length;
        }

        // If we landed exactly on '<', we’re at a tag boundary — insert after the tag.
        if (xml[idx] == '<')
        {
            int gt = xml.IndexOf('>', idx);
            if (gt >= 0) return gt + 1;
            return xml.Length;
        }

        // If we landed in the middle of an entity, try to avoid splitting it.
        // Mapping sometimes points to the '&' start; that’s fine. If it points after '&' but before ';', move back to '&'.
        int amp = xml.LastIndexOf('&', idx);
        if (amp >= 0)
        {
            int semi = xml.IndexOf(';', amp);
            if (semi >= 0 && amp < idx && idx < semi)
                return amp; // insert before '&...;'
        }

        return idx;
    }

    private static bool IsInsideTag(string xml, int idx)
    {
        // Determine if at idx we are between a '<' and a following '>' with no intervening '>'.
        int lastLt = xml.LastIndexOf('<', idx);
        int lastGt = xml.LastIndexOf('>', idx);

        // lastLt after lastGt => we’re currently inside a tag
        return lastLt >= 0 && lastLt > lastGt;
    }

    private static string BuildCommunityNote(string noteText, string? resp)
    {
        string t = EscapeXmlText(noteText ?? "");
        string r = string.IsNullOrWhiteSpace(resp) ? "" : $" resp=\"{EscapeXmlAttr(resp.Trim())}\"";
        return $"<note type=\"community\"{r}>{t}</note>";
    }

    private static string EscapeXmlText(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string EscapeXmlAttr(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
