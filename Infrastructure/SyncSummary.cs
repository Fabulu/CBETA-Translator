// Infrastructure/SyncSummary.cs
// Parses git diff --stat output into a human-friendly "What's New" summary.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Infrastructure;

public static class SyncSummary
{
    /// <summary>
    /// Parses git diff --stat output and returns a human-friendly summary.
    /// Groups changes by artifact type (translations, TM, termbase, tags, etc.)
    /// </summary>
    public static List<string> Summarize(string diffStatOutput)
    {
        var lines = (diffStatOutput ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var summary = new List<string>();

        int translations = 0;
        int tmEntries = 0;
        int termbases = 0;
        int tags = 0;
        int collections = 0;
        int reviews = 0;
        int masterDates = 0;
        int licenses = 0;
        int other = 0;

        foreach (var line in lines)
        {
            // Skip the summary line ("X files changed, Y insertions(+), Z deletions(-)")
            if (line.Contains("files changed") || line.Contains("file changed"))
                continue;

            // Extract path from "path | +N -M" format
            var pipeIdx = line.IndexOf('|');
            if (pipeIdx < 0) continue;
            var path = line[..pipeIdx].Trim();

            if (path.Contains("xml-p5t/") || path.Contains("xml-open-t/") || path.Contains("community/translations/"))
                translations++;
            else if (path.Contains("translation-memory"))
                tmEntries++;
            else if (path.Contains("termbase"))
                termbases++;
            else if (path.Contains("community/tags/") || path.Contains("tag-vocabularies"))
                tags++;
            else if (path.Contains("scholar-collections") || path.Contains("community/collections/"))
                collections++;
            else if (path.Contains("community/reviews/"))
                reviews++;
            else if (path.Contains("community/master-dates/"))
                masterDates++;
            else if (path.Contains("community/translation-licenses/"))
                licenses++;
            else
                other++;
        }

        if (translations > 0) summary.Add($"{translations} translation{Plural(translations)} updated");
        if (tmEntries > 0) summary.Add($"{tmEntries} translation memory file{Plural(tmEntries)} changed");
        if (termbases > 0) summary.Add($"{termbases} termbase file{Plural(termbases)} changed");
        if (tags > 0) summary.Add($"{tags} tag file{Plural(tags)} changed");
        if (collections > 0) summary.Add($"{collections} scholar collection{Plural(collections)} changed");
        if (reviews > 0) summary.Add($"{reviews} review file{Plural(reviews)} changed");
        if (masterDates > 0) summary.Add($"{masterDates} master date file{Plural(masterDates)} changed");
        if (licenses > 0) summary.Add($"{licenses} license file{Plural(licenses)} changed");
        if (other > 0) summary.Add($"{other} other file{Plural(other)} changed");

        if (summary.Count == 0)
            summary.Add("No changes from remote");

        return summary;
    }

    private static string Plural(int n) => n == 1 ? "" : "s";
}
