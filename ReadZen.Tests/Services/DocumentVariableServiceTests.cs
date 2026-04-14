using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for DocumentVariableService: JSONL round-trip, cross-tab counting, empty file.
/// </summary>
public class DocumentVariableServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DocumentVariableService _svc;

    public DocumentVariableServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _svc = new DocumentVariableService();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_ReturnsEmpty()
    {
        var result = await _svc.LoadAsync(_tempDir);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var vars = new List<DocumentVariable>
        {
            new() { RelPath = "file1.xml", VariableName = "genre", VariableValue = "koan" },
            new() { RelPath = "file2.xml", VariableName = "genre", VariableValue = "sutra" },
            new() { RelPath = "file1.xml", VariableName = "period", VariableValue = "Tang" }
        };

        await _svc.SaveAsync(_tempDir, vars);
        var loaded = await _svc.LoadAsync(_tempDir);

        Assert.Equal(3, loaded.Count);
        Assert.Equal("file1.xml", loaded[0].RelPath);
        Assert.Equal("genre", loaded[0].VariableName);
        Assert.Equal("koan", loaded[0].VariableValue);
        Assert.Equal("sutra", loaded[1].VariableValue);
        Assert.Equal("period", loaded[2].VariableName);
    }

    [Fact]
    public async Task SaveAsync_AtomicWrite_FileExists()
    {
        var vars = new List<DocumentVariable>
        {
            new() { RelPath = "test.xml", VariableName = "v1", VariableValue = "val1" }
        };

        await _svc.SaveAsync(_tempDir, vars);

        var path = Path.Combine(_tempDir, "document-variables.jsonl");
        Assert.True(File.Exists(path));

        // No temp file should remain
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void CrossTabulate_GroupsByVariable()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "a", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "2", RelPath = "file1.xml", TagId = "t1", FromLb = "b", ToLb = "b", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "3", RelPath = "file2.xml", TagId = "t1", FromLb = "a", ToLb = "a", CreatedUtc = DateTimeOffset.UtcNow },
            new() { Id = "4", RelPath = "file2.xml", TagId = "t2", FromLb = "a", ToLb = "a", CreatedUtc = DateTimeOffset.UtcNow }
        };

        var vars = new List<DocumentVariable>
        {
            new() { RelPath = "file1.xml", VariableName = "genre", VariableValue = "koan" },
            new() { RelPath = "file2.xml", VariableName = "genre", VariableValue = "sutra" }
        };

        var vocab = new TagVocabulary();
        vocab.Tags.Add(new TagDefinition { Id = "t1", Name = "Theme", CreatedUtc = DateTimeOffset.UtcNow });
        vocab.Tags.Add(new TagDefinition { Id = "t2", Name = "Metaphor", CreatedUtc = DateTimeOffset.UtcNow });

        var result = _svc.CrossTabulate(tags, vars, vocab, "genre");

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("koan"));
        Assert.True(result.ContainsKey("sutra"));

        // koan group: file1 has 2 t1 tags
        Assert.Equal(2, result["koan"]["Theme"]);

        // sutra group: file2 has 1 t1 + 1 t2
        Assert.Equal(1, result["sutra"]["Theme"]);
        Assert.Equal(1, result["sutra"]["Metaphor"]);
    }

    [Fact]
    public void CrossTabulate_MissingVariable_GroupsAsUnset()
    {
        var tags = new List<DocumentTag>
        {
            new() { Id = "1", RelPath = "file1.xml", TagId = "t1", FromLb = "a", ToLb = "a", CreatedUtc = DateTimeOffset.UtcNow }
        };

        // No variables defined for file1
        var vars = new List<DocumentVariable>();
        var vocab = new TagVocabulary();
        vocab.Tags.Add(new TagDefinition { Id = "t1", Name = "Theme", CreatedUtc = DateTimeOffset.UtcNow });

        var result = _svc.CrossTabulate(tags, vars, vocab, "genre");

        Assert.Single(result);
        Assert.True(result.ContainsKey("(unset)"));
        Assert.Equal(1, result["(unset)"]["Theme"]);
    }

    [Fact]
    public void CrossTabulate_EmptyTags_ReturnsEmpty()
    {
        var result = _svc.CrossTabulate(
            new List<DocumentTag>(),
            new List<DocumentVariable>(),
            new TagVocabulary(),
            "genre");

        Assert.Empty(result);
    }
}
