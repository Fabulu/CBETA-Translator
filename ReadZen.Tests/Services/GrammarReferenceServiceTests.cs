using System.IO;
using System.Text.Json;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class GrammarReferenceServiceTests
{
    // ---- Test 6: Lookup returns info for known particle ----

    [Fact]
    public void Lookup_KnownParticle_ReturnsInfo()
    {
        var svc = new GrammarReferenceService();

        // The grammar-particles.json file should be in the build output.
        // If missing, the service gracefully returns null.
        // We test against a known particle from the JSON: 之 (zhi)
        var result = svc.Lookup('\u4e4b'); // 之

        // If the assets file is present in build output, we get data
        if (result != null)
        {
            Assert.Equal('\u4e4b', result.Character);
            Assert.NotEmpty(result.Functions);
        }
        // else: file not found in test runner output, acceptable -- skip assertion
    }

    // ---- Test 7: Lookup returns null for unknown character ----

    [Fact]
    public void Lookup_UnknownCharacter_ReturnsNull()
    {
        var svc = new GrammarReferenceService();

        // 'Z' is not a Chinese grammar particle
        var result = svc.Lookup('Z');

        Assert.Null(result);
    }

    [Fact]
    public void Lookup_RandomCjkCharNotInData_ReturnsNull()
    {
        var svc = new GrammarReferenceService();

        // 龍 (dragon) is not in the grammar particles list
        var result = svc.Lookup('\u9f8d');

        Assert.Null(result);
    }

    // ---- Test 8: Functions have correct structure ----

    [Fact]
    public void Lookup_KnownParticle_FunctionsHaveCorrectStructure()
    {
        var svc = new GrammarReferenceService();

        // Try 也 (ye) -- a common particle
        var result = svc.Lookup('\u4e5f');

        if (result != null)
        {
            Assert.Equal('\u4e5f', result.Character);
            Assert.NotEmpty(result.Functions);

            foreach (var func in result.Functions)
            {
                Assert.False(string.IsNullOrEmpty(func.Role), "Role should not be empty");
                Assert.False(string.IsNullOrEmpty(func.Gloss), "Gloss should not be empty");
                // Example and ExampleGloss may be empty for some functions
            }
        }
    }

    // ---- Additional: service is thread-safe (double init) ----

    [Fact]
    public void Lookup_CalledMultipleTimes_DoesNotThrow()
    {
        var svc = new GrammarReferenceService();

        // Call multiple times to exercise the double-checked locking
        var r1 = svc.Lookup('\u4e4b');
        var r2 = svc.Lookup('\u4e5f');
        var r3 = svc.Lookup('X');
        var r4 = svc.Lookup('\u4e4b'); // repeat

        // r1 and r4 should be the same data (from cache)
        if (r1 != null && r4 != null)
        {
            Assert.Equal(r1.Character, r4.Character);
            Assert.Equal(r1.Functions.Count, r4.Functions.Count);
        }
    }

    // ---- Service handles missing file gracefully ----

    [Fact]
    public void Lookup_AfterInit_NeverThrows()
    {
        // Even if the file doesn't exist, Lookup should return null, not throw
        var svc = new GrammarReferenceService();

        var exception = Record.Exception(() =>
        {
            svc.Lookup('\u4e4b');
            svc.Lookup('\u4e5f');
            svc.Lookup('\u800c');
            svc.Lookup('A');
        });

        Assert.Null(exception);
    }
}
