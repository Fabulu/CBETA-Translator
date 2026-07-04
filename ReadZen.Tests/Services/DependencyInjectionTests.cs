using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Guards the DI-hygiene work (audit P3.2 / R3-M1): services that were previously
/// constructed ad hoc with `new` in views are registered in the container so a single
/// shared instance is used. This test grows as more services are migrated.
/// </summary>
public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider()
        => new ServiceCollection().AddAppServices().BuildServiceProvider();

    [Fact]
    public void CitationService_IsRegistered_AsSingleton()
    {
        using var provider = BuildProvider();

        var a = provider.GetService<ICitationService>();
        var b = provider.GetService<ICitationService>();

        Assert.NotNull(a);
        Assert.IsType<CitationService>(a);
        Assert.Same(a, b); // singleton: one shared instance, not per-resolution
    }

    [Fact]
    public void ZenMasterManagerService_IsRegistered_AsSingleton()
    {
        using var provider = BuildProvider();

        var a = provider.GetService<ZenMasterManagerService>();
        var b = provider.GetService<ZenMasterManagerService>();

        Assert.NotNull(a);
        // One shared instance across all callers (previously each view did
        // `new ZenMasterManagerService(...)`, and ResearchGraphWindow even built its
        // own MasterDatesService — a second cache universe; audit R3-M1).
        Assert.Same(a, b);
    }

    [Fact]
    public void TranslationAssistantService_UsesContainerTmTermbaseQa_NotPrivateCopies()
    {
        using var provider = BuildProvider();

        var assistant = provider.GetRequiredService<ITranslationAssistantService>();
        var tm = provider.GetRequiredService<ITranslationMemoryService>();
        var terms = provider.GetRequiredService<ITermbaseService>();
        var qa = provider.GetRequiredService<ITranslationQaService>();

        // White-box: the assistant must hold the CONTAINER's singletons, not private
        // `new()` copies (audit R3-M1) — otherwise SetUsername / cache invalidation
        // routed through the container never reaches the assistant's copies.
        var type = assistant.GetType();
        object? Field(string name) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(assistant);

        Assert.Same(tm, Field("_tm"));
        Assert.Same(terms, Field("_terms"));
        Assert.Same(qa, Field("_qa"));
    }
}
