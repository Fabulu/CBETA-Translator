using ReadZen.App.Infrastructure;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

public class TextDiffTests
{
    [Fact]
    public void ComputeAddedSpans_EmptyInputs_ReturnsEmpty()
    {
        Assert.Empty(TextDiff.ComputeAddedSpans("", "hello"));
        Assert.Empty(TextDiff.ComputeAddedSpans("hello", ""));
        Assert.Empty(TextDiff.ComputeAddedSpans("", ""));
    }

    [Fact]
    public void ComputeAddedSpans_IdenticalTexts_ReturnsEmpty()
    {
        var spans = TextDiff.ComputeAddedSpans("hello world", "hello world");
        Assert.Empty(spans);
    }

    [Fact]
    public void ComputeAddedSpans_AddedWord_ReturnsSpan()
    {
        var spans = TextDiff.ComputeAddedSpans("hello world foo", "hello world");
        Assert.Single(spans);
        Assert.Equal(DiffKind.Added, spans[0].Kind);
        Assert.Equal("hello world ".Length, spans[0].Start);
    }

    [Fact]
    public void ComputeAddedSpans_MultipleAdded_ReturnsMultipleSpans()
    {
        var spans = TextDiff.ComputeAddedSpans("a b c d e", "a c e");
        // "b" and "d" are added (not in historical)
        Assert.Equal(2, spans.Count);
        Assert.All(spans, s => Assert.Equal(DiffKind.Added, s.Kind));
    }

    [Fact]
    public void ComputeAddedSpans_CjkText_Works()
    {
        var current = "趙州從諗禪師語錄";
        var historical = "趙州禪師語錄";
        var spans = TextDiff.ComputeAddedSpans(current, historical);
        // "從諗" is in current but not historical (as a token)
        // Note: CJK without spaces is one token, so the whole string is one token
        // and since they differ, the current is "added"
        Assert.NotEmpty(spans);
    }

    [Fact]
    public void ComputeAddedSpans_NullInputs_ReturnsEmpty()
    {
        Assert.Empty(TextDiff.ComputeAddedSpans(null!, "hello"));
        Assert.Empty(TextDiff.ComputeAddedSpans("hello", null!));
    }
}

public class SyncSummaryTests
{
    [Fact]
    public void Summarize_EmptyInput_ReturnsNoChanges()
    {
        var result = SyncSummary.Summarize("");
        Assert.Single(result);
        Assert.Contains("No changes", result[0]);
    }

    [Fact]
    public void Summarize_TranslationChanges_DetectsCorrectly()
    {
        var input = " xml-p5t/T/T48/T48n2005.xml | 10 ++++---\n 1 file changed";
        var result = SyncSummary.Summarize(input);
        Assert.Contains(result, s => s.Contains("translation"));
    }

    [Fact]
    public void Summarize_TermbaseChanges_DetectsCorrectly()
    {
        var input = " community/termbases/user.jsonl | 5 +++++\n 1 file changed";
        var result = SyncSummary.Summarize(input);
        Assert.Contains(result, s => s.Contains("termbase"));
    }

    [Fact]
    public void Summarize_MixedChanges_DetectsAll()
    {
        var input = " xml-p5t/T/T48/T48n2005.xml | 10 ++++\n community/tags/user.jsonl | 3 +++\n termbase.json | 2 ++\n 3 files changed";
        var result = SyncSummary.Summarize(input);
        Assert.True(result.Count >= 3); // at least translations + tags + termbase
    }

    [Fact]
    public void Summarize_LicenseChanges_DetectsCorrectly()
    {
        var input = " community/translation-licenses/user.jsonl | 1 +\n 1 file changed";
        var result = SyncSummary.Summarize(input);
        Assert.Contains(result, s => s.Contains("license"));
    }

    [Fact]
    public void Summarize_NullInput_ReturnsNoChanges()
    {
        var result = SyncSummary.Summarize(null!);
        Assert.Single(result);
        Assert.Contains("No changes", result[0]);
    }

    [Fact]
    public void Summarize_SummaryLineOnly_ReturnsNoChanges()
    {
        var input = " 3 files changed, 15 insertions(+), 2 deletions(-)";
        var result = SyncSummary.Summarize(input);
        Assert.Single(result);
        Assert.Contains("No changes", result[0]);
    }
}
