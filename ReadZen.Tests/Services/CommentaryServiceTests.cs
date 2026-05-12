// CommentaryServiceTests — exercises the PR 1 commentary plumbing:
// manifest pointer resolution, sibling fallback, mtime caching,
// graceful malformed-JSON handling, and the default-deny language
// filter (BCP-47 prefix match; null/"unknown" excluded by default).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "FiM")]
public class CommentaryServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _xmlPath;

    public CommentaryServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-commentary-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _xmlPath = Path.Combine(_tempDir, "sample.xml");
        File.WriteAllText(_xmlPath, "<TEI/>");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private void WriteCommentary(string filename, IEnumerable<CommentaryEntry> entries)
    {
        var info = new CommentaryInfo { Entries = new List<CommentaryEntry>(entries) };
        var json = JsonSerializer.Serialize(info);
        File.WriteAllText(Path.Combine(_tempDir, filename), json);
    }

    private void WriteManifest(string? commentaryFile)
    {
        var manifest = new ManifestInfo { CommentaryFile = commentaryFile };
        File.WriteAllText(Path.Combine(_tempDir, "manifest.json"), JsonSerializer.Serialize(manifest));
    }

    [Fact]
    public void TryLoad_ReturnsNull_WhenNoCommentaryFile()
    {
        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath);
        Assert.Null(result);
    }

    [Fact]
    public void TryLoad_ResolvesViaManifestPointer_WhenCommentaryFileSet()
    {
        WriteCommentary("custom-commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "via pointer" }
        });
        WriteManifest("custom-commentary.json");

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath);

        Assert.NotNull(result);
        Assert.NotNull(result!.Entries);
        Assert.Single(result.Entries!);
        Assert.Equal("via pointer", result.Entries![0].Title);
    }

    [Fact]
    public void TryLoad_FallsBackToCommentaryJsonSibling()
    {
        // No manifest, no pointer — pure sibling fallback.
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "sibling" }
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath);

        Assert.NotNull(result);
        Assert.Single(result!.Entries!);
        Assert.Equal("sibling", result.Entries![0].Title);
    }

    [Fact]
    public void TryLoad_FiltersByLanguage_WhenAllowedLanguagesProvided()
    {
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "Japanese" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hant", Title = "Trad Chinese" },
            new CommentaryEntry { CommentaryId = "C3", Language = "zh-Hans", Title = "Simp Chinese" },
            new CommentaryEntry { CommentaryId = "C4", Language = "en", Title = "English" },
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "zh-Hans" });

        Assert.NotNull(result);
        Assert.Equal(2, result!.Entries!.Count);
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C2");
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C3");
    }

    [Fact]
    public void TryLoad_PassesThroughAllEntries_WhenAllowedLanguagesNull()
    {
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hant" },
            new CommentaryEntry { CommentaryId = "C3", Language = null },
            new CommentaryEntry { CommentaryId = "C4", Language = "unknown" },
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, allowedLanguages: null);

        Assert.NotNull(result);
        // Provenance/admin path: every entry returned, including null and "unknown".
        Assert.Equal(4, result!.Entries!.Count);
    }

    [Fact]
    public void TryLoad_LanguagePrefixMatch_ZhMatchesZhHantAndZhHans()
    {
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "zh-Hant" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hans" },
            new CommentaryEntry { CommentaryId = "C3", Language = "zh" },
            new CommentaryEntry { CommentaryId = "C4", Language = "zha" },        // strict prefix → must NOT match
            new CommentaryEntry { CommentaryId = "C5", Language = "ja" },
            new CommentaryEntry { CommentaryId = "C6", Language = "ZH-HANT" },    // case-insensitive
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, new[] { "zh" });

        Assert.NotNull(result);
        Assert.Equal(4, result!.Entries!.Count);
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C1");
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C2");
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C3");
        Assert.Contains(result.Entries!, e => e.CommentaryId == "C6");
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C4");
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C5");
    }

    [Fact]
    public void TryLoad_CachesByMtime_DoesNotReparseOnRepeatCall()
    {
        // Single entry; verify the underlying CommentaryInfo instance is
        // re-used between calls (mtime-cache hit). Different language
        // filters on the same call MUST still hit the cache — the cache
        // stores the *unfiltered* parsed result and filters per call.
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "fixture" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hant", Title = "fixture-zh" },
        });

        var svc = new CommentaryService();
        var r1 = svc.TryLoad(_xmlPath);                                  // unfiltered
        var r2 = svc.TryLoad(_xmlPath);                                  // unfiltered again
        var r3 = svc.TryLoad(_xmlPath, new[] { "zh-Hant" });             // filtered

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.NotNull(r3);
        Assert.Same(r1, r2);                                              // same cached instance
        Assert.NotSame(r1, r3);                                           // filtered result is a new wrapper
        // Cached unfiltered entries are still referenced by the filter result.
        Assert.Single(r3!.Entries!);
        Assert.Equal("C2", r3.Entries![0].CommentaryId);
        // The filtered result's entry reference is the SAME instance as in the cache.
        Assert.Same(r1!.Entries![1], r3.Entries![0]);
    }

    [Fact]
    public void TryLoad_GracefulOnMalformedJson_ReturnsNull()
    {
        File.WriteAllText(Path.Combine(_tempDir, "commentary.json"), "{this is not valid json");

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath);

        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────
    // Gap-fill facts (Wave 4 test-writer pass — RUN-20260512-1754).
    // Locks in defensive guards + edge cases none of the first 9
    // facts asserted: null/empty xmlAbsPath, empty entries array,
    // mtime reparse on file change, empty-string language tagging.
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryLoad_ReturnsNull_WhenXmlAbsPathIsNullOrEmpty()
    {
        // Defensive: null / empty / whitespace path must short-circuit
        // (no IO probe, no exception) — the resolver and the service
        // both have this guard, but only the resolver had a fact for it.
        var svc = new CommentaryService();

        Assert.Null(svc.TryLoad(null!));
        Assert.Null(svc.TryLoad(""));
        Assert.Null(svc.TryLoad("   "));
    }

    [Fact]
    public void TryLoad_EmptyEntriesArray_ReturnsInfoWithEmptyEntriesNotNull()
    {
        // commentary.json containing `{"entries": []}` is a valid
        // edition-opted-in state with zero rows. Service must return
        // a non-null CommentaryInfo whose Entries is an empty list
        // (NOT null), so the resolver can distinguish "no file" (null)
        // from "file present but empty" (Entries.Count == 0).
        File.WriteAllText(Path.Combine(_tempDir, "commentary.json"), "{\"entries\":[]}");

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath);

        Assert.NotNull(result);
        Assert.NotNull(result!.Entries);
        Assert.Empty(result.Entries!);
    }

    [Fact]
    public void TryLoad_ReparsesAfterMtimeChange_ReturnsUpdatedEntries()
    {
        // Mtime cache invalidation: when commentary.json changes between
        // calls the service MUST reparse and surface the updated entries.
        // PR 1's existing cache-identity test only covers the "no change"
        // path; this locks in the inverse.
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "zh-Hant", Title = "round1" }
        });

        var svc = new CommentaryService();
        var first = svc.TryLoad(_xmlPath);
        Assert.NotNull(first);
        Assert.Single(first!.Entries!);
        Assert.Equal("round1", first.Entries![0].Title);

        // Bump mtime by re-writing with new content. Sleep briefly so the
        // file's LastWriteTimeUtc.Ticks differ (filesystem timestamp
        // granularity on Windows is ~10 ms but can be coarser on slow IO).
        Thread.Sleep(50);
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "zh-Hant", Title = "round2" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hans", Title = "added" },
        });

        var second = svc.TryLoad(_xmlPath);
        Assert.NotNull(second);
        Assert.NotSame(first, second);                       // cache was invalidated
        Assert.Equal(2, second!.Entries!.Count);
        Assert.Equal("round2", second.Entries![0].Title);
        Assert.Equal("added", second.Entries![1].Title);
    }

    [Fact]
    public void TryLoad_EmptyStringLanguage_ExcludedFromFilteredResult()
    {
        // Language: "" (empty string) — distinct from null on disk but
        // semantically the same "no positive identification". The
        // classifier treats null/whitespace identically (string.IsNullOrWhiteSpace);
        // an empty-string entry should be classified (Tier 2/3/4) before
        // the filter runs. With no body and a pure-CJK title it falls
        // through to Tier 4 → "unknown" → excluded by default-deny.
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "", Title = "信心銘", Body = null },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hant", Title = "explicit Chinese" },
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "zh-Hans" });

        Assert.NotNull(result);
        Assert.Single(result!.Entries!);
        Assert.Equal("C2", result.Entries![0].CommentaryId);

        // Side-map confirms the empty-string entry was reclassified to
        // "unknown" (Tier 4 default-deny — NOT silently dropped without
        // an inference tag).
        var c1Tag = svc.GetInferenceTag("C1");
        Assert.NotNull(c1Tag);
        Assert.Equal("unknown", c1Tag!.Bcp47);
        Assert.Equal(LanguageInferenceSource.Default, c1Tag.Source);
    }

    [Fact]
    public void TryLoad_FiltersOutUnknownLanguage_DefaultDeny()
    {
        // Default-deny posture: an entry tagged "unknown" must never
        // surface to a reader call even though "unknown" is not in any
        // allow list (the failure mode would be a Japanese item that
        // fell through Tier 4 of the classifier being shown anyway).
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "unknown", Title = "default-deny target" },
            new CommentaryEntry { CommentaryId = "C2", Language = "UNKNOWN", Title = "case-insensitive default-deny" },
            new CommentaryEntry { CommentaryId = "C3", Language = null, Title = "missing tag" },
            new CommentaryEntry { CommentaryId = "C4", Language = "zh-Hant", Title = "explicit Chinese" },
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "zh-Hans" });

        Assert.NotNull(result);
        Assert.Single(result!.Entries!);
        Assert.Equal("C4", result.Entries![0].CommentaryId);
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C1");
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C2");
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C3");
    }

    [Fact]
    public void TryLoad_SilentlyDropsJapaneseTagsFromWhitelist_FootGunGuard()
    {
        // Wave 5 foot-gun guard (user directive 2026-05-12): even if a curator
        // explicitly puts "ja" / "JA" / "ja-JP" / "jpn" in commentary_reader_languages,
        // the reader filter must drop them silently. Reader sees only positively-
        // identified Chinese — Japanese commentary cannot leak through a manifest
        // misconfiguration. The Japanese entry below carries an explicit
        // Language = "ja" (would normally pass any "ja"-containing whitelist),
        // but the whitelist is sanitised before use.
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "Japanese commentary" },
            new CommentaryEntry { CommentaryId = "C2", Language = "ja-JP", Title = "Japanese with region" },
            new CommentaryEntry { CommentaryId = "C3", Language = "zh-Hant", Title = "Chinese — should still show" },
        });

        var svc = new CommentaryService();

        // Foot-gun: curator accidentally adds Japanese tags to the whitelist.
        var result = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "ja", "JA", "ja-JP", "jpn" });

        Assert.NotNull(result);
        // Only the Chinese entry survives; "ja" / "ja-JP" / "jpn" were stripped
        // from the whitelist before matching, so the Japanese entries are dropped.
        Assert.Single(result!.Entries!);
        Assert.Equal("C3", result.Entries![0].CommentaryId);
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C1");
        Assert.DoesNotContain(result.Entries!, e => e.CommentaryId == "C2");
    }

    [Fact]
    public void TryLoad_JapaneseOnlyWhitelist_DegeneratesToNoFilter_NotFullDeny()
    {
        // Edge case: curator's whitelist contains ONLY Japanese tags. After the
        // foot-gun guard strips them, whitelist is empty → reader code path is
        // effectively "no opt-in" (passthrough). The intentional behaviour: the
        // manifest declaring ["ja"] is equivalent to declaring no whitelist —
        // because there's nothing the reader is allowed to see. Returning the
        // full list rather than empty matches PR 1's "empty whitelist = no filter"
        // contract; the panel layer (PR 3) still hides because manifest is
        // effectively unconfigured. If this proves surprising in practice, the
        // panel can be taught to treat a stripped-empty whitelist as "do not show".
        WriteCommentary("commentary.json", new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "Japanese" },
            new CommentaryEntry { CommentaryId = "C2", Language = "zh-Hant", Title = "Chinese" },
        });

        var svc = new CommentaryService();
        var result = svc.TryLoad(_xmlPath, new[] { "ja", "ja-JP" });

        // Whitelist becomes empty after guard; passthrough returns both entries.
        Assert.NotNull(result);
        Assert.Equal(2, result!.Entries!.Count);
    }
}
