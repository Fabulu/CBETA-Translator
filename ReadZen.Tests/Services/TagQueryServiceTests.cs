using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for TagQueryService: AND/OR/NOT operators, empty tags, query serialization.
/// </summary>
public class TagQueryServiceTests
{
    private static DocumentTag MakeTag(string tagId, string relPath = "T48/T48n2005.xml")
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = relPath,
            FromLb = "p0001a01",
            ToLb = "p0001a01",
            TagId = tagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

    [Fact]
    public void And_BothPresent_ReturnsFile()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t2", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.And };
        var results = TagQueryService.Execute(query, tags);

        Assert.Single(results);
        Assert.Equal("file1.xml", results[0].RelPath);
        Assert.Contains("t1", results[0].MatchedTagIds);
        Assert.Contains("t2", results[0].MatchedTagIds);
    }

    [Fact]
    public void And_OnlyOnePresent_ReturnsEmpty()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.And };
        var results = TagQueryService.Execute(query, tags);

        Assert.Empty(results);
    }

    [Fact]
    public void Or_EitherPresent_ReturnsBoth()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t2", "file2.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Or };
        var results = TagQueryService.Execute(query, tags);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.RelPath == "file1.xml");
        Assert.Contains(results, r => r.RelPath == "file2.xml");
    }

    [Fact]
    public void Or_BothPresent_ReturnsFile()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t2", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Or };
        var results = TagQueryService.Execute(query, tags);

        Assert.Single(results);
    }

    [Fact]
    public void Not_APresentBAbsent_ReturnsFile()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Not };
        var results = TagQueryService.Execute(query, tags);

        Assert.Single(results);
        Assert.Equal("file1.xml", results[0].RelPath);
    }

    [Fact]
    public void Not_BothPresent_ReturnsEmpty()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t2", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Not };
        var results = TagQueryService.Execute(query, tags);

        Assert.Empty(results);
    }

    [Fact]
    public void Not_AAbsent_ReturnsEmpty()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t2", "file1.xml")
        };

        var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Not };
        var results = TagQueryService.Execute(query, tags);

        Assert.Empty(results);
    }

    [Fact]
    public void EmptyTags_AllOperators_ReturnEmpty()
    {
        var empty = new List<DocumentTag>();

        foreach (var op in new[] { TagQueryOperator.And, TagQueryOperator.Or, TagQueryOperator.Not })
        {
            var query = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = op };
            Assert.Empty(TagQueryService.Execute(query, empty));
        }
    }

    [Fact]
    public void MultipleFiles_FilteredCorrectly()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t2", "file1.xml"),
            MakeTag("t1", "file2.xml"),
            // file2 has t1 but not t2
            MakeTag("t2", "file3.xml")
            // file3 has t2 but not t1
        };

        var andQuery = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.And };
        var andResults = TagQueryService.Execute(andQuery, tags);
        Assert.Single(andResults);
        Assert.Equal("file1.xml", andResults[0].RelPath);

        var notQuery = new TagQuery { CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.Not };
        var notResults = TagQueryService.Execute(notQuery, tags);
        Assert.Single(notResults);
        Assert.Equal("file2.xml", notResults[0].RelPath);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrips()
    {
        var queries = new List<TagQuery>
        {
            new() { Name = "Test Query 1", CodeA = "t1", CodeB = "t2", Operator = TagQueryOperator.And },
            new() { Name = "Test Query 2", CodeA = "t3", CodeB = "t4", Operator = TagQueryOperator.Not }
        };

        var json = TagQueryService.SerializeQueries(queries);
        var deserialized = TagQueryService.DeserializeQueries(json);

        Assert.Equal(2, deserialized.Count);
        Assert.Equal("Test Query 1", deserialized[0].Name);
        Assert.Equal("t1", deserialized[0].CodeA);
        Assert.Equal("t2", deserialized[0].CodeB);
        Assert.Equal(TagQueryOperator.And, deserialized[0].Operator);
        Assert.Equal("Test Query 2", deserialized[1].Name);
        Assert.Equal(TagQueryOperator.Not, deserialized[1].Operator);
    }

    [Fact]
    public void DeserializeQueries_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(TagQueryService.DeserializeQueries(""));
        Assert.Empty(TagQueryService.DeserializeQueries("   "));
    }

    [Fact]
    public void DeserializeQueries_InvalidJson_ReturnsEmpty()
    {
        Assert.Empty(TagQueryService.DeserializeQueries("not json at all"));
    }
}
