using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Tests for the BookPickerDialog.BookEntry model class.
/// The dialog itself requires Avalonia UI, but the model is testable in isolation.
/// </summary>
public class BookPickerDialogBookEntryTests
{
    [Fact]
    public void BookEntry_HasRelPathProperty()
    {
        var entry = new BookPickerDialog.BookEntry { RelPath = "xml-p5/T/T0001.xml" };
        Assert.Equal("xml-p5/T/T0001.xml", entry.RelPath);
    }

    [Fact]
    public void BookEntry_HasDisplayShortProperty()
    {
        var entry = new BookPickerDialog.BookEntry { DisplayShort = "Platform Sutra" };
        Assert.Equal("Platform Sutra", entry.DisplayShort);
    }

    [Fact]
    public void BookEntry_HasSubtitleProperty()
    {
        var entry = new BookPickerDialog.BookEntry { Subtitle = "By Huineng" };
        Assert.Equal("By Huineng", entry.Subtitle);
    }

    [Fact]
    public void BookEntry_HasTooltipProperty()
    {
        var entry = new BookPickerDialog.BookEntry { Tooltip = "Full tooltip text" };
        Assert.Equal("Full tooltip text", entry.Tooltip);
    }

    [Fact]
    public void BookEntry_DefaultsToEmptyStrings()
    {
        var entry = new BookPickerDialog.BookEntry();
        Assert.Equal("", entry.RelPath);
        Assert.Equal("", entry.DisplayShort);
        Assert.Equal("", entry.Subtitle);
        Assert.Equal("", entry.Tooltip);
    }

    [Fact]
    public void BookEntry_AllPropertiesSetViaInitSyntax()
    {
        var entry = new BookPickerDialog.BookEntry
        {
            RelPath = "path/to/book.xml",
            DisplayShort = "Blue Cliff Record",
            Subtitle = "100 Cases",
            Tooltip = "Blue Cliff Record\n100 Cases of Zen"
        };

        Assert.Equal("path/to/book.xml", entry.RelPath);
        Assert.Equal("Blue Cliff Record", entry.DisplayShort);
        Assert.Equal("100 Cases", entry.Subtitle);
        Assert.Equal("Blue Cliff Record\n100 Cases of Zen", entry.Tooltip);
    }
}
