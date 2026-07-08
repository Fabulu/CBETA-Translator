// Infrastructure/FileNameSanitizer.cs
using System;
using System.IO;
using System.Text;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Filename sanitization, consolidated from nine private copies (audit P3.6).
/// Two semantic families existed and BOTH are preserved verbatim — they are not
/// interchangeable:
/// <list type="bullet">
/// <item><see cref="Strict"/> — identity-derived filenames (community usernames →
/// per-user jsonl files). Existing on-disk community files were named by this
/// exact rule; changing it would orphan them.</item>
/// <item><see cref="Lenient"/> — display-text-derived export filenames (keeps dots
/// and spaces, substitutes invalid chars).</item>
/// </list>
/// Behavior is pinned by FileNameSanitizerTests.
/// </summary>
public static class FileNameSanitizer
{
    /// <summary>
    /// Strips invalid filename chars plus '.' and ' '; returns "unknown" when
    /// nothing survives. (The seven service copies' shared body.)
    /// </summary>
    public static string Strict(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (Array.IndexOf(invalid, ch) < 0 && ch != '.' && ch != ' ')
                sb.Append(ch);
        }
        return sb.Length > 0 ? sb.ToString() : "unknown";
    }

    /// <summary>
    /// Replaces invalid filename chars with '_'; keeps everything else including
    /// dots and spaces. (The license-service / edition-dialog copies' body.)
    /// </summary>
    public static string Lenient(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        return sb.ToString();
    }
}
