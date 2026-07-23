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
    /// Composite v2 freshness stamp at build time:
    /// <c>v2;corpus=files={N};bytes={SUM};pathsig={P16};titles={T16};roster=count={M};hash={R16}</c>.
    /// Derived entirely from content/structure (no mtime), so it is identical across
    /// machines/clones for identical content. TryLoadAsync recomputes it live and refuses
    /// the cache when it differs — catching corpus, titles, AND roster changes. Null in
    /// caches from older builds and legacy v1 stamps ("files=N;maxTicks=T") never equal a
    /// live v2 stamp, so both are treated as stale. See SPEC §1.2.
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
