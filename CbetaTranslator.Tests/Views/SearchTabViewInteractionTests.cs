using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.App.Views;
using CbetaTranslator.Tests.Stubs;
using Xunit;

namespace CbetaTranslator.Tests.Views;

public class SearchTabViewInteractionTests
{
    private static SearchTabView CreateViewShell(out SearchTabViewModel vm)
    {
        var view = (SearchTabView)RuntimeHelpers.GetUninitializedObject(typeof(SearchTabView));
        vm = new SearchTabViewModel(new StubSearchIndexService());
        SetField(typeof(SearchTabView), view, "_vm", vm);
        return view;
    }

    private static void SetField(Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }

    private static T GetField<T>(Type type, object target, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        return (T)field.GetValue(target)!;
    }

    [Fact]
    public async Task ApplyUiStateAsync_AndExportUiState_RoundTripsCoreViewState()
    {
        var view = CreateViewShell(out _);
        var state = new SearchTabViewModel.SearchUiState
        {
            Query = "wumenguan",
            SearchOriginal = true,
            SearchTranslated = true,
            ZenOnly = true,
            SelectedStatusIndex = 2,
            SelectedContextIndex = 5,
            SelectedTagFilterId = "topic-1"
        };

        await view.ApplyUiStateAsync(state, executeSearch: false);
        var exported = view.ExportUiState();

        Assert.Equal("wumenguan", exported.Query);
        Assert.True(exported.SearchOriginal);
        Assert.True(exported.SearchTranslated);
        Assert.True(exported.ZenOnly);
        Assert.Equal(2, exported.SelectedStatusIndex);
        Assert.Equal(5, exported.SelectedContextIndex);
    }

    [Fact]
    public void SetContext_ForwardsRootAndDirectoriesToViewModel()
    {
        var view = CreateViewShell(out var vm);

        view.SetContext("/root", "/orig", "/tran", rel => (rel, rel, TranslationStatus.Green));

        Assert.Equal("/root", GetField<string?>(typeof(SearchTabViewModel), vm, "_root"));
        Assert.Equal("/orig", GetField<string?>(typeof(SearchTabViewModel), vm, "_originalDir"));
        Assert.Equal("/tran", GetField<string?>(typeof(SearchTabViewModel), vm, "_translatedDir"));
    }

    [Fact]
    public void SetTagFilterData_PopulatesTagFilterItems()
    {
        var view = CreateViewShell(out var vm);
        var tags = new List<DocumentTag>
        {
            new() { RelPath = "T/T48/T48n2005.xml", TagId = "topic-1", CreatedBy = "alice", FromLb = "0292a26", ToLb = "0292a26" }
        };
        var vocab = new TagVocabulary
        {
            Tags = new List<TagDefinition>
            {
                new() { Id = "topic-1", Name = "Topic 1" }
            }
        };

        view.SetTagFilterData(tags, vocab);

        Assert.Contains("Topic 1", vm.TagFilterItems);
        Assert.Equal(0, vm.SelectedTagFilterIndex);
    }
    [Fact]
    public async Task ApplyUiStateAsync_RoundTripsLowestExpandedContextIndex()
    {
        var view = CreateViewShell(out _);
        var state = new SearchTabViewModel.SearchUiState
        {
            Query = "mumonkan",
            SearchOriginal = true,
            SearchTranslated = false,
            ZenOnly = false,
            SelectedStatusIndex = 0,
            SelectedContextIndex = 0
        };

        await view.ApplyUiStateAsync(state, executeSearch: false);
        var exported = view.ExportUiState();

        Assert.Equal("mumonkan", exported.Query);
        Assert.Equal(0, exported.SelectedContextIndex);
        Assert.True(exported.SearchOriginal);
        Assert.False(exported.SearchTranslated);
    }
}

