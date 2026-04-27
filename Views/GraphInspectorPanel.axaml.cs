using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Views;

/// <summary>
/// Inspector panel for the Research Graph that shows details about the selected node
/// (passage, concept, master, term, or collection) with contextual action buttons.
/// </summary>
public partial class GraphInspectorPanel : UserControl
{
    public event EventHandler<string>? NavigateRequested;
    public event EventHandler<string>? RemoveRequested;

    public GraphInspectorPanel()
    {
        InitializeComponent();
    }

    public void ShowNode(ResearchGraphNode? node, ScholarCollection? collection)
    {
        var content = this.FindControl<StackPanel>("ContentPanel")!;
        var actions = this.FindControl<StackPanel>("ActionPanel")!;
        var title = this.FindControl<TextBlock>("TitleText")!;
        var indicator = this.FindControl<Ellipse>("TypeIndicator")!;
        var empty = this.FindControl<TextBlock>("EmptyText")!;

        content.Children.Clear();
        actions.Children.Clear();

        if (node == null || collection == null)
        {
            empty.IsVisible = true;
            title.Text = "Inspector";
            return;
        }

        empty.IsVisible = false;
        title.Text = node.Label;

        // Set type indicator color
        indicator.Fill = node.NodeType switch
        {
            ScholarNodeType.Passage => new SolidColorBrush(Color.Parse("#6EAFF8")),
            ScholarNodeType.Concept => new SolidColorBrush(Color.Parse("#FF8A65")),
            ScholarNodeType.ZenMaster => new SolidColorBrush(Color.Parse("#64B5F6")),
            ScholarNodeType.TermbaseEntry => new SolidColorBrush(Color.Parse("#81C784")),
            ScholarNodeType.Collection => new SolidColorBrush(Color.Parse("#AB47BC")),
            _ => new SolidColorBrush(Colors.Gray)
        };

        switch (node.NodeType)
        {
            case ScholarNodeType.Passage:
                ShowPassage(node, collection, content, actions);
                break;
            case ScholarNodeType.Concept:
                ShowConcept(node, collection, content, actions);
                break;
            case ScholarNodeType.ZenMaster:
                ShowMaster(node, content, actions);
                break;
            case ScholarNodeType.TermbaseEntry:
                ShowTerm(node, content, actions);
                break;
            case ScholarNodeType.Collection:
                ShowCollection(node, content, actions);
                break;
        }
    }

    private void ShowPassage(ResearchGraphNode node, ScholarCollection collection, StackPanel content, StackPanel actions)
    {
        var passage = collection.Passages.FirstOrDefault(p => p.Id == node.NodeId);
        if (passage == null) return;

        // Source
        AddSection(content, "Source", passage.SourceRelPath ?? "");

        // Summary
        if (!string.IsNullOrWhiteSpace(passage.Summary))
            AddHighlightBox(content, passage.Summary);

        // Chinese text
        if (!string.IsNullOrWhiteSpace(passage.ZhText))
            AddSection(content, "Chinese", passage.ZhText, maxLines: 8);

        // English text
        if (!string.IsNullOrWhiteSpace(passage.EnText))
            AddSection(content, "English", passage.EnText, maxLines: 8);

        // Tags
        if (passage.Tags.Count > 0)
            AddChips(content, "Tags", passage.Tags);

        // Masters
        if (passage.MasterNames.Count > 0)
            AddChips(content, "Masters", passage.MasterNames);

        // Notes
        if (!string.IsNullOrWhiteSpace(passage.Notes))
            AddSection(content, "Notes", passage.Notes, maxLines: 4);

        // Stats
        AddSection(content, "Connections", $"{node.Degree} edges");

        // Actions
        AddButton(actions, "Open in Reader", () => NavigateRequested?.Invoke(this, node.NodeId));
        AddButton(actions, "Remove", () => RemoveRequested?.Invoke(this, node.NodeId));
    }

    private void ShowConcept(ResearchGraphNode node, ScholarCollection collection, StackPanel content, StackPanel actions)
    {
        var concept = collection.Concepts.FirstOrDefault(c => c.Id == node.NodeId);
        if (concept == null) return;

        // Status badge
        if (concept.Status != ConceptStatus.Active)
            AddBadge(content, concept.Status.ToString(), "#FF6B6B");

        // Description
        if (!string.IsNullOrWhiteSpace(concept.Description))
            AddSection(content, "Description", concept.Description);

        // Tags
        if (concept.Tags.Count > 0)
            AddChips(content, "Tags", concept.Tags);

        // Linked passages count
        var linkedPassages = collection.Edges
            .Count(e => e.ToNodeId == concept.Id && e.ToNodeType == ScholarNodeType.Concept);
        AddSection(content, "Evidence", $"{linkedPassages} linked passages");

        // Connected concepts
        var connectedConcepts = collection.Edges
            .Count(e => (e.FromNodeId == concept.Id || e.ToNodeId == concept.Id)
                     && e.FromNodeType == ScholarNodeType.Concept && e.ToNodeType == ScholarNodeType.Concept);
        if (connectedConcepts > 0)
            AddSection(content, "Related Concepts", $"{connectedConcepts} connections");

        // Actions
        AddButton(actions, "Rename", () => { /* TODO */ });
        AddButton(actions, "Remove", () => RemoveRequested?.Invoke(this, node.NodeId));
    }

    private void ShowMaster(ResearchGraphNode node, StackPanel content, StackPanel actions)
    {
        var name = node.NodeId.StartsWith("master:") ? node.NodeId[7..] : node.Label;
        AddSection(content, "Zen Master", name);
        AddSection(content, "Connections", $"{node.Degree} edges");
        AddButton(actions, "View Profile", () => NavigateRequested?.Invoke(this, node.NodeId));
        AddButton(actions, "Remove", () => RemoveRequested?.Invoke(this, node.NodeId));
    }

    private void ShowTerm(ResearchGraphNode node, StackPanel content, StackPanel actions)
    {
        var term = node.NodeId.StartsWith("term:") ? node.NodeId[5..] : node.Label;
        AddSection(content, "Term", term);
        AddSection(content, "Connections", $"{node.Degree} edges");
        AddButton(actions, "Open Termbase", () => NavigateRequested?.Invoke(this, node.NodeId));
        AddButton(actions, "Remove", () => RemoveRequested?.Invoke(this, node.NodeId));
    }

    private void ShowCollection(ResearchGraphNode node, StackPanel content, StackPanel actions)
    {
        AddSection(content, "Collection Reference", node.Label);
        AddSection(content, "Connections", $"{node.Degree} edges");
        AddButton(actions, "Open", () => NavigateRequested?.Invoke(this, node.NodeId));
        AddButton(actions, "Remove", () => RemoveRequested?.Invoke(this, node.NodeId));
    }

    // --- Helper methods ---

    private void AddSection(StackPanel parent, string label, string value, int maxLines = 0)
    {
        var section = new StackPanel { Spacing = 2 };
        section.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeight.SemiBold, Opacity = 0.7 });
        var tb = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        if (maxLines > 0) tb.MaxLines = maxLines;
        section.Children.Add(tb);
        parent.Children.Add(section);
    }

    private void AddHighlightBox(StackPanel parent, string text)
    {
        var border = new Border
        {
            BorderThickness = new Avalonia.Thickness(2, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.Parse("#d4ab58")),
            Padding = new Avalonia.Thickness(8, 6),
            Margin = new Avalonia.Thickness(0, 4)
        };
        border.Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 };
        parent.Children.Add(border);
    }

    private void AddChips(StackPanel parent, string label, List<string> items)
    {
        var section = new StackPanel { Spacing = 4 };
        section.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeight.SemiBold, Opacity = 0.7 });
        var wrap = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        foreach (var item in items.Take(8))
        {
            var chip = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new Avalonia.CornerRadius(3),
                Padding = new Avalonia.Thickness(6, 2),
                Margin = new Avalonia.Thickness(0, 0, 4, 4)
            };
            chip.Child = new TextBlock { Text = item, FontSize = 10 };
            wrap.Children.Add(chip);
        }
        section.Children.Add(wrap);
        parent.Children.Add(section);
    }

    private void AddBadge(StackPanel parent, string text, string colorHex)
    {
        var badge = new Border
        {
            Background = new SolidColorBrush(Color.Parse(colorHex)),
            CornerRadius = new Avalonia.CornerRadius(3),
            Padding = new Avalonia.Thickness(8, 3),
            Margin = new Avalonia.Thickness(0, 4),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };
        badge.Child = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeight.SemiBold };
        parent.Children.Add(badge);
    }

    private void AddButton(StackPanel parent, string label, Action onClick)
    {
        var btn = new Button { Content = label, Padding = new Avalonia.Thickness(8, 4), FontSize = 11 };
        btn.Click += (_, _) => { try { onClick(); } catch { /* safe */ } };
        parent.Children.Add(btn);
    }
}
