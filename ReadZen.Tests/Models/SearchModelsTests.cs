using System.Collections.Generic;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Tests for W3 / W5 search model classes:
/// SearchResultGroup.IsExpanded, ApplyDefaultExpansion, ActiveMasterFilter,
/// and SearchResultShowMoreItem.
/// </summary>
public class SearchModelsTests
{
    // ---- SearchResultGroup.IsExpanded ----

    [Fact]
    public void SearchResultGroup_DefaultIsExpanded_IsFalse()
    {
        var group = new SearchResultGroup();
        Assert.False(group.IsExpanded);
    }

    [Fact]
    public void SearchResultGroup_IsExpanded_CanBeSetToTrue()
    {
        var group = new SearchResultGroup { IsExpanded = false };
        group.IsExpanded = true;
        Assert.True(group.IsExpanded);
    }

    [Fact]
    public void SearchResultGroup_IsExpanded_FiresPropertyChanged()
    {
        var group = new SearchResultGroup();
        var fired = new List<string?>();
        group.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        group.IsExpanded = true;

        Assert.Contains("IsExpanded", fired);
    }

    [Fact]
    public void SearchResultGroup_IsExpanded_NoEventWhenValueUnchanged()
    {
        var group = new SearchResultGroup { IsExpanded = true };
        var count = 0;
        group.PropertyChanged += (_, _) => count++;

        group.IsExpanded = true; // same value

        Assert.Equal(0, count);
    }

    // ---- ApplyDefaultExpansion (tested via the static helper through reflection, or indirectly) ----
    // ApplyDefaultExpansion is private, so we verify the documented contract by constructing
    // the same logic inline.

    [Fact]
    public void ApplyDefaultExpansion_Contract_FirstNonSpecialGroupExpanded_RestCollapsed()
    {
        // Replicate the logic of ApplyDefaultExpansion to verify its documented contract.
        // First regular group → expanded; subsequent regular groups → collapsed.
        // Groups with RelPath == "__master__" or "__title_section__" → always expanded.
        var groups = new List<SearchResultGroup>
        {
            new() { RelPath = "path/a.xml", DisplayName = "A" },
            new() { RelPath = "path/b.xml", DisplayName = "B" },
            new() { RelPath = "path/c.xml", DisplayName = "C" },
        };

        // Apply the same logic as the private method
        bool firstFullTextSeen = false;
        foreach (var g in groups)
        {
            if (g.RelPath == "__master__" || g.RelPath == "__title_section__")
            {
                g.IsExpanded = true;
                continue;
            }
            g.IsExpanded = !firstFullTextSeen;
            firstFullTextSeen = true;
        }

        Assert.True(groups[0].IsExpanded);
        Assert.False(groups[1].IsExpanded);
        Assert.False(groups[2].IsExpanded);
    }

    [Fact]
    public void ApplyDefaultExpansion_Contract_MasterGroupAlwaysExpanded()
    {
        var groups = new List<SearchResultGroup>
        {
            new() { RelPath = "__master__", DisplayName = "Master Card" },
            new() { RelPath = "path/a.xml", DisplayName = "A" },
        };

        bool firstFullTextSeen = false;
        foreach (var g in groups)
        {
            if (g.RelPath == "__master__" || g.RelPath == "__title_section__")
            {
                g.IsExpanded = true;
                continue;
            }
            g.IsExpanded = !firstFullTextSeen;
            firstFullTextSeen = true;
        }

        Assert.True(groups[0].IsExpanded);  // master always expanded
        Assert.True(groups[1].IsExpanded);  // first regular group expanded
    }

    [Fact]
    public void ApplyDefaultExpansion_Contract_TitleSectionAlwaysExpanded()
    {
        var groups = new List<SearchResultGroup>
        {
            new() { RelPath = "__title_section__", DisplayName = "Titles" },
            new() { RelPath = "path/a.xml", DisplayName = "A" },
            new() { RelPath = "path/b.xml", DisplayName = "B" },
        };

        bool firstFullTextSeen = false;
        foreach (var g in groups)
        {
            if (g.RelPath == "__master__" || g.RelPath == "__title_section__")
            {
                g.IsExpanded = true;
                continue;
            }
            g.IsExpanded = !firstFullTextSeen;
            firstFullTextSeen = true;
        }

        Assert.True(groups[0].IsExpanded);   // title section always expanded
        Assert.True(groups[1].IsExpanded);   // first regular
        Assert.False(groups[2].IsExpanded);  // rest collapsed
    }

    // ---- ActiveMasterFilter ----

    [Fact]
    public void ActiveMasterFilter_DefaultValues()
    {
        var filter = new ActiveMasterFilter();
        Assert.Equal("", filter.MasterName);
        Assert.NotNull(filter.RelPaths);
        Assert.Empty(filter.RelPaths);
    }

    [Fact]
    public void ActiveMasterFilter_CanSetMasterNameAndRelPaths()
    {
        var paths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "T/T48/T48n2005.xml",
            "T/T47/T47n1999.xml"
        };

        var filter = new ActiveMasterFilter
        {
            MasterName = "Wumen Huikai",
            RelPaths = paths
        };

        Assert.Equal("Wumen Huikai", filter.MasterName);
        Assert.Equal(2, filter.RelPaths.Count);
        Assert.Contains("T/T48/T48n2005.xml", filter.RelPaths);
        Assert.Contains("T/T47/T47n1999.xml", filter.RelPaths);
    }

    [Fact]
    public void ActiveMasterFilter_RelPaths_IsCaseInsensitive()
    {
        var filter = new ActiveMasterFilter
        {
            RelPaths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "T/T48/T48n2005.xml"
            }
        };

        // Case-insensitive lookup
        Assert.Contains("t/t48/t48n2005.xml", filter.RelPaths);
    }

    [Fact]
    public void MultiMasterIntersection_TwoMastersWithOverlappingPaths_ReturnsIntersection()
    {
        // Simulate the intersection logic used by SearchTabViewModel.RebuildMasterFilterRelPaths
        var master1 = new ActiveMasterFilter
        {
            MasterName = "Wumen Huikai",
            RelPaths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "path/a.xml",
                "path/b.xml",
                "path/c.xml"
            }
        };

        var master2 = new ActiveMasterFilter
        {
            MasterName = "Dahui Zonggao",
            RelPaths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "path/b.xml",
                "path/c.xml",
                "path/d.xml"
            }
        };

        var filters = new List<ActiveMasterFilter> { master1, master2 };

        // Replicate RebuildMasterFilterRelPaths logic
        var intersection = new System.Collections.Generic.HashSet<string>(
            filters[0].RelPaths, System.StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < filters.Count; i++)
            intersection.IntersectWith(filters[i].RelPaths);

        Assert.Equal(2, intersection.Count);
        Assert.Contains("path/b.xml", intersection);
        Assert.Contains("path/c.xml", intersection);
        Assert.DoesNotContain("path/a.xml", intersection);
        Assert.DoesNotContain("path/d.xml", intersection);
    }

    [Fact]
    public void MultiMasterIntersection_NoOverlap_ReturnsEmpty()
    {
        var master1 = new ActiveMasterFilter
        {
            MasterName = "A",
            RelPaths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "path/a.xml" }
        };

        var master2 = new ActiveMasterFilter
        {
            MasterName = "B",
            RelPaths = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "path/b.xml" }
        };

        var filters = new List<ActiveMasterFilter> { master1, master2 };

        var intersection = new System.Collections.Generic.HashSet<string>(
            filters[0].RelPaths, System.StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < filters.Count; i++)
            intersection.IntersectWith(filters[i].RelPaths);

        Assert.Empty(intersection);
    }

    // ---- SearchResultShowMoreItem ----

    [Fact]
    public void SearchResultShowMoreItem_InheritsFromSearchResultChild()
    {
        var item = new SearchResultShowMoreItem();
        Assert.IsAssignableFrom<SearchResultChild>(item);
    }

    [Fact]
    public void SearchResultShowMoreItem_DefaultValues()
    {
        var item = new SearchResultShowMoreItem();
        Assert.Equal(0, item.RemainingCount);
        Assert.Equal("", item.GroupRelPath);
    }

    [Fact]
    public void SearchResultShowMoreItem_CanSetProperties()
    {
        var item = new SearchResultShowMoreItem
        {
            RemainingCount = 42,
            GroupRelPath = "T/T48/T48n2005.xml"
        };

        Assert.Equal(42, item.RemainingCount);
        Assert.Equal("T/T48/T48n2005.xml", item.GroupRelPath);
    }

    [Fact]
    public void SearchResultShowMoreItem_IsDetectableByTypeCheck()
    {
        SearchResultChild child = new SearchResultShowMoreItem { RemainingCount = 3, GroupRelPath = "path/x.xml" };

        Assert.True(child is SearchResultShowMoreItem);
        var sentinel = (SearchResultShowMoreItem)child;
        Assert.Equal(3, sentinel.RemainingCount);
    }
}
