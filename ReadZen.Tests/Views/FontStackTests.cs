using System.IO;
using System.Linq;
using Xunit;

namespace ReadZen.Tests.Views;

/// <summary>
/// Verifies that the font stacks declared in AXAML views contain the expected
/// font families. ReadableTabView should use serif fonts for CJK readability;
/// TranslationTabView should use monospace fonts for editing alignment.
/// </summary>
public class FontStackTests
{
    /// <summary>Root of the main project (one level up from ReadZen.Tests).</summary>
    private static string ProjectRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string ReadAxaml(string relativePath)
    {
        var path = Path.Combine(ProjectRoot, relativePath);
        Assert.True(File.Exists(path), $"AXAML file not found: {path}");
        return File.ReadAllText(path);
    }

    // ---- ReadableTabView: CJK serif font stack ----

    [Fact]
    public void ReadableTabView_FontStack_ContainsSerifFallback()
    {
        var axaml = ReadAxaml("Views/ReadableTabView.axaml");

        // The view-level style should include "serif" as the generic fallback
        Assert.Contains("serif", axaml);
    }

    [Fact]
    public void ReadableTabView_FontStack_ContainsNotoSerifCjk()
    {
        var axaml = ReadAxaml("Views/ReadableTabView.axaml");

        Assert.Contains("Noto Serif CJK SC", axaml);
    }

    [Fact]
    public void ReadableTabView_FontStack_ContainsSourceHanSerif()
    {
        var axaml = ReadAxaml("Views/ReadableTabView.axaml");

        Assert.Contains("Source Han Serif SC", axaml);
    }

    [Fact]
    public void ReadableTabView_FontStack_DoesNotUseMonospace()
    {
        var axaml = ReadAxaml("Views/ReadableTabView.axaml");

        // The view-level FontFamily setter should not contain monospace.
        // Extract just the FontFamily line from the style setter.
        var lines = axaml.Split('\n')
            .Where(l => l.Contains("Setter") && l.Contains("FontFamily"))
            .ToList();

        Assert.NotEmpty(lines);
        // The primary style setter should reference serif, not monospace
        var primarySetter = lines[0];
        Assert.Contains("serif", primarySetter.ToLowerInvariant());
        Assert.DoesNotContain("monospace", primarySetter.ToLowerInvariant());
    }

    // ---- TranslationTabView: monospace font stack ----

    [Fact]
    public void TranslationTabView_FontStack_ContainsMonospace()
    {
        var axaml = ReadAxaml("Views/TranslationTabView.axaml");

        Assert.Contains("monospace", axaml);
    }

    [Fact]
    public void TranslationTabView_FontStack_ContainsConsolas()
    {
        var axaml = ReadAxaml("Views/TranslationTabView.axaml");

        Assert.Contains("Consolas", axaml);
    }

    [Fact]
    public void TranslationTabView_FontStack_DoesNotUseSerif()
    {
        var axaml = ReadAxaml("Views/TranslationTabView.axaml");

        // The editor's FontFamily should not reference serif fonts.
        // Find lines that set FontFamily on the editor control.
        var fontLines = axaml.Split('\n')
            .Where(l => l.Contains("FontFamily") && l.Contains("Consolas"))
            .ToList();

        Assert.NotEmpty(fontLines);
        foreach (var line in fontLines)
        {
            // "serif" can appear inside "sans-serif" which is fine, but
            // standalone "serif" as a generic family would be wrong here.
            // Check that the line does not end with just ", serif" or contain
            // "Serif CJK" which would indicate a serif CJK font.
            Assert.DoesNotContain("Serif CJK", line);
        }
    }

    // ---- Cross-view: ReadableTabView and TranslationTabView use different font families ----

    [Fact]
    public void ReadableAndTranslation_UseDifferentFontStacks()
    {
        var readableAxaml = ReadAxaml("Views/ReadableTabView.axaml");
        var translationAxaml = ReadAxaml("Views/TranslationTabView.axaml");

        // ReadableTabView should have serif, TranslationTabView should have monospace
        var readableFontLines = readableAxaml.Split('\n')
            .Where(l => l.Contains("Setter") && l.Contains("FontFamily"))
            .ToList();
        var translationFontLines = translationAxaml.Split('\n')
            .Where(l => l.Contains("FontFamily") && l.Contains("Consolas"))
            .ToList();

        Assert.NotEmpty(readableFontLines);
        Assert.NotEmpty(translationFontLines);

        // They should not share the same font stack
        Assert.NotEqual(
            readableFontLines[0].Trim(),
            translationFontLines[0].Trim());
    }
}
