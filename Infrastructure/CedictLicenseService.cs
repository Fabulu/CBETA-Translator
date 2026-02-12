using System;
using System.IO;
using System.Text;

namespace CbetaTranslator.App.Infrastructure;

public static class CedictLicenseService
{
    public static string ReadCedictHeader(string cedictPath, int maxLines = 250)
    {
        if (string.IsNullOrWhiteSpace(cedictPath))
            return "CC-CEDICT header not found (invalid path).";

        if (!File.Exists(cedictPath))
            return $"CC-CEDICT header not found (missing file): {cedictPath}";

        var sb = new StringBuilder();

        using var fs = File.OpenRead(cedictPath);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        int lines = 0;
        string? line;
        while ((line = sr.ReadLine()) != null && lines < maxLines)
        {
            lines++;

            // CEDICT header lines are comment lines starting with '#'
            if (line.StartsWith("#"))
            {
                sb.AppendLine(line.TrimEnd());
                continue;
            }

            // first non-header line ends the header
            break;
        }

        var text = sb.ToString().Trim();
        if (text.Length == 0)
            return "No CC-CEDICT header comments were found at the top of the dictionary file.";

        return text;
    }
}
