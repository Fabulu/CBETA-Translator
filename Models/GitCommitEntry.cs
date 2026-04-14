// Models/GitCommitEntry.cs
// Represents a single commit in a file's git history.

using System;

namespace ReadZen.App.Models;

public sealed record GitCommitEntry(
    string Hash,
    DateTimeOffset Date,
    string Author,
    string Subject)
{
    /// <summary>
    /// Display-friendly date string (relative or absolute depending on age).
    /// </summary>
    public string DateDisplay
    {
        get
        {
            var age = DateTimeOffset.Now - Date;
            if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
            if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
            if (age.TotalDays < 7) return $"{(int)age.TotalDays}d ago";
            return Date.ToString("yyyy-MM-dd");
        }
    }

    /// <summary>Short hash for display (first 7 chars).</summary>
    public string ShortHash => Hash.Length > 7 ? Hash[..7] : Hash;
}
