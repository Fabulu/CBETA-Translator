// Infrastructure/MarkdownRenderer.cs
// Shared markdown-to-Avalonia renderer extracted from ProvenancePanel.
// Handles: headings (3 levels), **bold**, `code`, [links](url),
// fenced code blocks, blockquotes, nested bullets, numbered lists,
// | tables |, --- separators, and plain text with wrapping.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace ReadZen.App.Infrastructure;

public static class MarkdownRenderer
{
    /// <summary>
    /// Renders a markdown string into an Avalonia StackPanel with styled elements.
    /// </summary>
    public static StackPanel Render(string markdown)
    {
        var panel = new StackPanel { Spacing = 2 };
        var lines = markdown.Split('\n');
        var tableRows = new List<string>();
        bool inTable = false;
        bool inCodeBlock = false;
        var codeBlockLines = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            // Fenced code block toggle
            if (line.TrimStart().StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    FlushCodeBlock(panel, codeBlockLines);
                    codeBlockLines.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    inCodeBlock = true;
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockLines.Add(line);
                continue;
            }

            // Flush table if we're leaving one
            if (inTable && !line.TrimStart().StartsWith("|"))
            {
                FlushTable(panel, tableRows);
                tableRows.Clear();
                inTable = false;
            }

            // Table row
            if (line.TrimStart().StartsWith("|"))
            {
                inTable = true;
                if (!System.Text.RegularExpressions.Regex.IsMatch(line, @"^\|[\s\-:|]+\|$"))
                    tableRows.Add(line);
                continue;
            }

            // Blank line
            if (string.IsNullOrWhiteSpace(line))
            {
                panel.Children.Add(new Border { Height = 4 });
                continue;
            }

            // Horizontal rule
            if (line.TrimStart().StartsWith("---") && line.Trim().All(c => c == '-' || c == ' '))
            {
                panel.Children.Add(new Border
                {
                    Height = 1,
                    Margin = new Avalonia.Thickness(0, 6),
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255))
                });
                continue;
            }

            // Headings
            if (line.StartsWith("### "))
            {
                panel.Children.Add(MakeHeading(line[4..].Trim(), 11.5, FontWeight.SemiBold,
                    new Avalonia.Thickness(0, 6, 0, 2), 0.8));
                continue;
            }
            if (line.StartsWith("## "))
            {
                panel.Children.Add(MakeHeading(line[3..].Trim(), 12.5, FontWeight.Bold,
                    new Avalonia.Thickness(0, 8, 0, 3), 0.9));
                continue;
            }
            if (line.StartsWith("# "))
            {
                var headingWrap = new StackPanel { Spacing = 0, Margin = new Avalonia.Thickness(0, 12, 0, 2) };
                headingWrap.Children.Add(new TextBlock
                {
                    Text = line[2..].Trim(),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 1.0,
                });
                headingWrap.Children.Add(new Border
                {
                    Height = 1,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0),
                    Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                });
                panel.Children.Add(headingWrap);
                continue;
            }

            // Blockquote
            if (line.TrimStart().StartsWith("> "))
            {
                var quoteText = line.TrimStart()[2..];
                var quoteTb = MakeRichTextBlock(quoteText, 11);
                quoteTb.Opacity = 0.8;
                quoteTb.FontStyle = FontStyle.Italic;
                panel.Children.Add(new Border
                {
                    Child = quoteTb,
                    BorderThickness = new Avalonia.Thickness(3, 0, 0, 0),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 136, 170, 255)),
                    Padding = new Avalonia.Thickness(10, 2, 0, 2),
                    Margin = new Avalonia.Thickness(0, 2),
                });
                continue;
            }

            // Bullet list with nesting
            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
            {
                var indent = line.Length - line.TrimStart().Length;
                var nestLevel = indent / 2;
                var bulletText = line.TrimStart()[2..];
                var tb = MakeRichTextBlock(bulletText, 11);
                var bulletChar = nestLevel == 0 ? "\u2022 " : nestLevel == 1 ? "\u25e6 " : "\u2013 ";
                tb.Margin = new Avalonia.Thickness(8 + nestLevel * 12, 0, 0, 0);
                if (tb.Inlines != null && tb.Inlines.Count > 0)
                    tb.Inlines.Insert(0, new Run(bulletChar)
                    {
                        Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
                    });
                panel.Children.Add(tb);
                continue;
            }

            // Numbered list
            if (line.TrimStart().Length > 2 && char.IsDigit(line.TrimStart()[0]) &&
                line.TrimStart().IndexOf(". ", StringComparison.Ordinal) > 0 &&
                line.TrimStart().IndexOf(". ", StringComparison.Ordinal) < 5)
            {
                var indent = line.Length - line.TrimStart().Length;
                var tb = MakeRichTextBlock(line.TrimStart(), 11);
                tb.Margin = new Avalonia.Thickness(8 + (indent / 2) * 12, 0, 0, 0);
                panel.Children.Add(tb);
                continue;
            }

            // Regular text
            panel.Children.Add(MakeRichTextBlock(line, 11));
        }

        if (inCodeBlock && codeBlockLines.Count > 0)
            FlushCodeBlock(panel, codeBlockLines);

        if (inTable && tableRows.Count > 0)
            FlushTable(panel, tableRows);

        return panel;
    }

    private static TextBlock MakeHeading(string text, double fontSize, FontWeight weight,
        Avalonia.Thickness margin, double opacity)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
            Opacity = opacity,
            Margin = margin,
        };
    }

    /// <summary>Renders inline **bold**, `code`, and [links](url) within a line.</summary>
    public static TextBlock MakeRichTextBlock(string text, double fontSize)
    {
        var tb = new TextBlock
        {
            FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.85,
        };

        int pos = 0;
        while (pos < text.Length)
        {
            // Bold: **...**
            if (pos + 2 < text.Length && text[pos] == '*' && text[pos + 1] == '*')
            {
                var end = text.IndexOf("**", pos + 2, StringComparison.Ordinal);
                if (end > pos + 2)
                {
                    tb.Inlines!.Add(new Run(text[(pos + 2)..end])
                    {
                        FontWeight = FontWeight.Bold
                    });
                    pos = end + 2;
                    continue;
                }
            }

            // Inline code: `...`
            if (text[pos] == '`')
            {
                var end = text.IndexOf('`', pos + 1);
                if (end > pos + 1)
                {
                    tb.Inlines!.Add(new Run("\u2009"));
                    tb.Inlines!.Add(new Run(text[(pos + 1)..end])
                    {
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                        FontSize = fontSize > 1 ? fontSize - 0.5 : fontSize,
                    });
                    tb.Inlines!.Add(new Run("\u2009"));
                    pos = end + 1;
                    continue;
                }
            }

            // Link: [text](url)
            if (text[pos] == '[')
            {
                var closeBracket = text.IndexOf(']', pos + 1);
                if (closeBracket > pos + 1 && closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
                {
                    var closeParen = text.IndexOf(')', closeBracket + 2);
                    if (closeParen > closeBracket + 2)
                    {
                        var linkText = text[(pos + 1)..closeBracket];
                        var linkUrl = text[(closeBracket + 2)..closeParen];
                        AddLinkInline(tb, linkText, linkUrl);
                        pos = closeParen + 1;
                        continue;
                    }
                }
            }

            // Plain text
            var nextSpecial = text.Length;
            var nextBold = text.IndexOf("**", pos, StringComparison.Ordinal);
            var nextCode = text.IndexOf('`', pos);
            var nextLink = text.IndexOf('[', pos);
            if (nextBold >= 0 && nextBold < nextSpecial) nextSpecial = nextBold;
            if (nextCode >= 0 && nextCode < nextSpecial) nextSpecial = nextCode;
            if (nextLink >= 0 && nextLink < nextSpecial) nextSpecial = nextLink;
            if (nextSpecial == pos) nextSpecial = pos + 1;

            tb.Inlines!.Add(new Run(text[pos..nextSpecial]));
            pos = nextSpecial;
        }

        return tb;
    }

    private static void AddLinkInline(TextBlock tb, string displayText, string url)
    {
        var linkBtn = new Button
        {
            Content = displayText,
            FontSize = tb.FontSize,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            Foreground = new SolidColorBrush(Color.FromRgb(136, 170, 255)),
            MinHeight = 0,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(linkBtn, url);
        linkBtn.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* ignore if no browser */ }
        };
        tb.Inlines!.Add(new InlineUIContainer { Child = linkBtn });
    }

    private static void FlushCodeBlock(StackPanel parent, List<string> lines)
    {
        if (lines.Count == 0) return;

        var codeText = string.Join("\n", lines);
        parent.Children.Add(new Border
        {
            Child = new TextBlock
            {
                Text = codeText,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.85,
            },
            Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(10, 6),
            Margin = new Avalonia.Thickness(0, 4),
        });
    }

    private static void FlushTable(StackPanel parent, List<string> rows)
    {
        if (rows.Count == 0) return;

        var tablePanel = new StackPanel { Spacing = 0, Margin = new Avalonia.Thickness(0, 4) };
        bool isHeader = true;
        int dataRowIdx = 0;

        foreach (var row in rows)
        {
            var cells = row.Split('|', StringSplitOptions.None)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c) || row.Contains('|'))
                .ToArray();

            var cleanCells = cells.Where(c => c.Length > 0 || cells.Length <= 2).ToList();
            if (cleanCells.Count == 0) continue;

            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
            };

            foreach (var cell in cleanCells)
            {
                rowPanel.Children.Add(new TextBlock
                {
                    Text = cell,
                    FontSize = 10.5,
                    FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
                    TextWrapping = TextWrapping.NoWrap,
                    MinWidth = 70,
                    Opacity = isHeader ? 0.95 : 0.8,
                });
            }

            if (isHeader)
            {
                var headerBorder = new Border
                {
                    Child = rowPanel,
                    Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    Padding = new Avalonia.Thickness(6, 4),
                    BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                };
                tablePanel.Children.Add(headerBorder);
            }
            else
            {
                var rowBg = dataRowIdx % 2 == 0
                    ? Color.FromArgb(0, 0, 0, 0)
                    : Color.FromArgb(12, 255, 255, 255);
                tablePanel.Children.Add(new Border
                {
                    Child = rowPanel,
                    Background = new SolidColorBrush(rowBg),
                    Padding = new Avalonia.Thickness(6, 3),
                });
                dataRowIdx++;
            }

            isHeader = false;
        }

        var tableScroll = new ScrollViewer
        {
            Content = tablePanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        parent.Children.Add(new Border
        {
            Child = tableScroll,
            Padding = new Avalonia.Thickness(2),
            CornerRadius = new Avalonia.CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(10, 255, 255, 255)),
            Margin = new Avalonia.Thickness(0, 2),
        });
    }
}
