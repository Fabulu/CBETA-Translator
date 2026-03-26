using System;
using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

public sealed class ScholarPassage
{
    public string Id { get; set; } = "";
    public string SourceRelPath { get; set; } = "";
    public string ZhText { get; set; } = "";
    public string EnText { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> MasterNames { get; set; } = new();
    public DateTimeOffset AddedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
}

public sealed class ScholarCollection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
    public List<ScholarPassage> Passages { get; set; } = new();
}
