using System.IO;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

// The WriteUserJsonlAsync / LoadAllCommunityJsonlAsync tests were retired: personal
// termbases are now local-only, so those methods (community publish + community read)
// were removed from TermbaseStorageService and its interface. Only the surviving
// GetCommunityTermbasesDir path helper is still exercised here.
public class TermbaseStorageServiceJsonlTests
{
    // ---- GetCommunityTermbasesDir — correct path ----

    [Fact]
    public void GetCommunityTermbasesDir_ReturnsCorrectPath()
    {
        var dir = TermbaseStorageService.GetCommunityTermbasesDir("/repo/root");

        Assert.Equal(Path.Combine("/repo/root", "community", "termbases"), dir);
    }

    [Fact]
    public void GetCommunityTermbasesDir_Interface_ReturnsCorrectPath()
    {
        // Also test the interface static method
        var dir = ITermbaseStorageService.GetCommunityTermbasesDir("/repo/root");

        Assert.Equal(Path.Combine("/repo/root", "community", "termbases"), dir);
    }
}
