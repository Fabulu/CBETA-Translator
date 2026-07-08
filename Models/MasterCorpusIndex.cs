// Models/MasterCorpusIndex.cs
// Index of zen master appearances across the CBETA and OpenZen corpora.
// Built by scanning all XML files for master Chinese name mentions.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>Top-level corpus index for all master appearances.</summary>
public sealed class MasterCorpusIndex
{
    [JsonPropertyName("built_utc")]
    public string? BuiltUtc { get; set; }

    [JsonPropertyName("corpus")]
    public string? Corpus { get; set; }

    /// <summary>
    /// Stat-stamp of the corpus at build time ("files=N;maxTicks=T" across all
    /// discovered corpus dirs). TryLoadAsync compares it against the live corpus to
    /// refuse stale caches (audit P4.6). Null in caches from older builds — treated
    /// as stale when a freshness check is requested.
    /// </summary>
    [JsonPropertyName("corpus_stamp")]
    public string? CorpusStamp { get; set; }

    [JsonPropertyName("file_count")]
    public int FileCount { get; set; }

    [JsonPropertyName("master_count")]
    public int MasterCount { get; set; }

    [JsonPropertyName("appearances")]
    public List<MasterTextAppearance> Appearances { get; set; } = new();
}

/// <summary>A single master's appearance in a text.</summary>
public sealed class MasterTextAppearance
{
    /// <summary>The master's canonical name (English).</summary>
    [JsonPropertyName("master_name")]
    public string MasterName { get; set; } = "";

    /// <summary>The Chinese name that matched.</summary>
    [JsonPropertyName("matched_name")]
    public string MatchedName { get; set; } = "";

    /// <summary>Relative path to the XML file.</summary>
    [JsonPropertyName("rel_path")]
    public string RelPath { get; set; } = "";

    /// <summary>Text title (from titles.jsonl or TEI header).</summary>
    [JsonPropertyName("text_title")]
    public string? TextTitle { get; set; }

    /// <summary>Chinese title.</summary>
    [JsonPropertyName("text_title_zh")]
    public string? TextTitleZh { get; set; }

    /// <summary>"primary" (author/subject) or "secondary" (quoted/mentioned).</summary>
    [JsonPropertyName("appearance_type")]
    public string AppearanceType { get; set; } = "secondary";

    /// <summary>Number of times the name appears in this text.</summary>
    [JsonPropertyName("mention_count")]
    public int MentionCount { get; set; }

    /// <summary>Short context snippet around the first mention.</summary>
    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
}
