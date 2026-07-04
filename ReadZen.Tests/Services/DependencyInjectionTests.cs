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
}
