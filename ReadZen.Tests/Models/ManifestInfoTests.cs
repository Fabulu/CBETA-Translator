using System.Text.Json;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Tests for ManifestInfo, particularly the Phase D date-distinction fields
/// (composition_date / manuscript_date / redaction_date / textual_criticism_date)
/// and their backward-compat behavior with manifests that only ship the older
/// year_composed field.
/// </summary>
public class ManifestInfoTests
{
    private static JsonSerializerOptions Opts => new() { PropertyNameCaseInsensitive = true };

    // ---- Phase D: structured date fields ----

    [Fact]
    public void Manifest_ParsesAllFourStructuredDateFields()
    {
        const string json = @"{
            ""text_id"": ""pd.example"",
            ""composition_date"": ""1228 (Shaoding 1)"",
            ""manuscript_date"": ""1632 woodblock"",
            ""redaction_date"": ""case 49 added post-1228"",
            ""textual_criticism_date"": ""2026-04""
        }";

        var m = JsonSerializer.Deserialize<ManifestInfo>(json, Opts);
        Assert.NotNull(m);
        Assert.Equal("1228 (Shaoding 1)", m!.CompositionDate);
        Assert.Equal("1632 woodblock", m.ManuscriptDate);
        Assert.Equal("case 49 added post-1228", m.RedactionDate);
        Assert.Equal("2026-04", m.TextualCriticismDate);
    }

    [Fact]
    public void Manifest_StructuredDateFields_AreNullableAndOmittable()
    {
        // A manifest can ship any subset of the 4 — all must be independently
        // nullable without mutual dependencies.
        const string json = @"{
            ""text_id"": ""pd.example"",
            ""manuscript_date"": ""1632 woodblock""
        }";

        var m = JsonSerializer.Deserialize<ManifestInfo>(json, Opts);
        Assert.NotNull(m);
        Assert.Null(m!.CompositionDate);
        Assert.Equal("1632 woodblock", m.ManuscriptDate);
        Assert.Null(m.RedactionDate);
        Assert.Null(m.TextualCriticismDate);
    }

    [Fact]
    public void Manifest_LegacyYearComposedOnly_StillParses()
    {
        // Backward-compat: existing editions that only have year_composed must
        // still deserialize cleanly with the new structured fields left null.
        const string json = @"{
            ""text_id"": ""pd.example"",
            ""year_composed"": ""1228""
        }";

        var m = JsonSerializer.Deserialize<ManifestInfo>(json, Opts);
        Assert.NotNull(m);
        Assert.Equal("1228", m!.YearComposed);
        Assert.Null(m.CompositionDate);
        Assert.Null(m.ManuscriptDate);
        Assert.Null(m.RedactionDate);
        Assert.Null(m.TextualCriticismDate);
    }

    [Fact]
    public void Manifest_EmptyJson_AllDateFieldsNull()
    {
        // Worst-case manifest with zero fields should not throw or set
        // spurious defaults.
        const string json = "{}";

        var m = JsonSerializer.Deserialize<ManifestInfo>(json, Opts);
        Assert.NotNull(m);
        Assert.Null(m!.YearComposed);
        Assert.Null(m.CompositionDate);
        Assert.Null(m.ManuscriptDate);
        Assert.Null(m.RedactionDate);
        Assert.Null(m.TextualCriticismDate);
    }

    [Fact]
    public void Manifest_CoexistsYearComposedAndStructuredDates()
    {
        // During the migration window we expect editions to temporarily carry
        // both year_composed and composition_date. Parsing must not drop either.
        const string json = @"{
            ""text_id"": ""pd.example"",
            ""year_composed"": ""1228"",
            ""composition_date"": ""1228 (Shaoding 1) — full context""
        }";

        var m = JsonSerializer.Deserialize<ManifestInfo>(json, Opts);
        Assert.NotNull(m);
        Assert.Equal("1228", m!.YearComposed);
        Assert.Equal("1228 (Shaoding 1) — full context", m.CompositionDate);
    }
}
