using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Tests for W4 TypeaheadModels: new enum values and mutable properties.
/// </summary>
public class TypeaheadModelsTests
{
    // ---- TypeaheadItemKind enum values ----

    [Fact]
    public void TypeaheadItemKind_HasRecentSearchValue()
    {
        // 4C addition
        var kind = TypeaheadItemKind.RecentSearch;
        Assert.Equal(TypeaheadItemKind.RecentSearch, kind);
    }

    [Fact]
    public void TypeaheadItemKind_HasCoocTermValue()
    {
        // 4D addition
        var kind = TypeaheadItemKind.CoocTerm;
        Assert.Equal(TypeaheadItemKind.CoocTerm, kind);
    }

    [Fact]
    public void TypeaheadItemKind_HasCountFooterValue()
    {
        // 4B addition
        var kind = TypeaheadItemKind.CountFooter;
        Assert.Equal(TypeaheadItemKind.CountFooter, kind);
    }

    [Fact]
    public void TypeaheadItemKind_AllOriginalValuesStillExist()
    {
        // Ensure we didn't break existing values
        _ = TypeaheadItemKind.SectionHeader;
        _ = TypeaheadItemKind.Master;
        _ = TypeaheadItemKind.Title;
        _ = TypeaheadItemKind.FullTextAction;
    }

    // ---- TypeaheadDisplayItem.InIndex (mutable, set; not just init) ----

    [Fact]
    public void TypeaheadDisplayItem_InIndex_DefaultIsFalse()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.Title };
        Assert.False(item.InIndex);
    }

    [Fact]
    public void TypeaheadDisplayItem_InIndex_CanBeSetToTrue()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.Title };
        item.InIndex = true;
        Assert.True(item.InIndex);
    }

    [Fact]
    public void TypeaheadDisplayItem_InIndex_CanBeSetBackToFalse()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.Title };
        item.InIndex = true;
        item.InIndex = false;
        Assert.False(item.InIndex);
    }

    // ---- TypeaheadDisplayItem.CountLabel (mutable, set; not just init) ----

    [Fact]
    public void TypeaheadDisplayItem_CountLabel_DefaultIsNull()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.CountFooter };
        Assert.Null(item.CountLabel);
    }

    [Fact]
    public void TypeaheadDisplayItem_CountLabel_CanBeSet()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.CountFooter };
        item.CountLabel = "~42 texts";
        Assert.Equal("~42 texts", item.CountLabel);
    }

    [Fact]
    public void TypeaheadDisplayItem_CountLabel_CanBeSetToNull()
    {
        var item = new TypeaheadDisplayItem { Kind = TypeaheadItemKind.CountFooter };
        item.CountLabel = "some value";
        item.CountLabel = null;
        Assert.Null(item.CountLabel);
    }

    // ---- Typical usage patterns ----

    [Fact]
    public void TypeaheadDisplayItem_RecentSearch_HasQuerySet()
    {
        var item = new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.RecentSearch,
            Query = "wumen"
        };

        Assert.Equal(TypeaheadItemKind.RecentSearch, item.Kind);
        Assert.Equal("wumen", item.Query);
    }

    [Fact]
    public void TypeaheadDisplayItem_CoocTerm_HasQuerySet()
    {
        var item = new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.CoocTerm,
            Query = "慧"
        };

        Assert.Equal(TypeaheadItemKind.CoocTerm, item.Kind);
        Assert.Equal("慧", item.Query);
    }

    [Fact]
    public void TypeaheadDisplayItem_CountFooter_HasCountLabel()
    {
        var item = new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.CountFooter,
        };
        item.CountLabel = "~15 texts";

        Assert.Equal(TypeaheadItemKind.CountFooter, item.Kind);
        Assert.Equal("~15 texts", item.CountLabel);
    }

    [Fact]
    public void TypeaheadDisplayItem_TitleItem_InIndexMutatedAfterCreation()
    {
        // Simulate what SearchTabView does: create item with init-properties,
        // then mutate InIndex after querying the inverted index.
        var item = new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.Title,
            DisplayName = "Gateless Barrier",
        };

        // Initially not in index
        Assert.False(item.InIndex);

        // Later, after index check:
        item.InIndex = true;
        Assert.True(item.InIndex);
    }
}
