using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.Tests.Stubs;

// ---- ITermbaseStorageService ----

public class StubTermbaseStorageService : ITermbaseStorageService
{
    public List<TermbaseEntry> Entries { get; set; } = new();
    public List<TermbaseEntry>? LastSaved { get; private set; }
    public Dictionary<string, List<TermbaseEntry>> CommunityEntriesByUser { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ThrowOnLoad { get; set; }
    public bool ThrowOnSave { get; set; }

    public Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (ThrowOnLoad) throw new InvalidOperationException("Load failed");
        return Task.FromResult(new List<TermbaseEntry>(Entries));
    }

    public Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default)
    {
        if (ThrowOnSave) throw new InvalidOperationException("Save failed");
        LastSaved = new List<TermbaseEntry>(entries);
        return Task.CompletedTask;
    }

    public Task<List<TermbaseEntry>> LoadUserAsync(string root, string username, CancellationToken ct = default)
    {
        if (ThrowOnLoad) throw new InvalidOperationException("Load failed");
        return Task.FromResult(new List<TermbaseEntry>(Entries));
    }

    public Task SaveUserAsync(string root, string username, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default)
    {
        if (ThrowOnSave) throw new InvalidOperationException("Save failed");
        LastSaved = new List<TermbaseEntry>(entries);
        return Task.CompletedTask;
    }

    public Task WriteUserJsonlAsync(string communityDir, string username, List<TermbaseEntry> entries, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<Dictionary<string, List<TermbaseEntry>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default)
        => Task.FromResult(CommunityEntriesByUser.ToDictionary(kv => kv.Key, kv => new List<TermbaseEntry>(kv.Value), StringComparer.OrdinalIgnoreCase));
}

// ---- IGitRepoService ----

public class StubGitRepoService : IGitRepoService
{
    public void TryCancelRunningProcess() { }
    public Task<bool> CheckGitAvailableAsync(CancellationToken ct) => Task.FromResult(true);
    public Task<GitOpResult> CloneAsync(string repoUrl, string targetDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> FetchAsync(string repoDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public virtual Task<string[]?> GetStatusPorcelainAsync(string repoDir, CancellationToken ct) => Task.FromResult<string[]?>(Array.Empty<string>());
    public Task<string> GetCurrentBranchAsync(string repoDir, CancellationToken ct) => Task.FromResult("main");
    public Task EnsureUserIdentityAsync(string repoDir, string? username, IProgress<string> progress, CancellationToken ct) => Task.CompletedTask;
    public virtual Task<GitOpResult> StagePathAsync(string repoDir, string relPath, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> StashKeepIndexAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> StashAllAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> SwitchCreateBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> CommitAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> SwitchBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> StashPopAsync(string repoDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> HardResetToRemoteMainAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> CleanUntrackedAsync(string repoDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<string?> GetRemoteUrlAsync(string repoDir, string remoteName, CancellationToken ct) => Task.FromResult<string?>(null);
    public Task<GitOpResult> RemoveRemoteAsync(string repoDir, string remoteName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> EnsureRemoteUrlAsync(string repoDir, string remoteName, string cleanRemoteUrl, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> PushSetUpstreamAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> PushSetUpstreamWithTokenAsync(string repoDir, string remoteName, string branchName, string accessToken, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> EnsureLocalExcludeAsync(string repoDir, string[] patterns, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> EnsureCredentialHelperAsync(string repoDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<GitOpResult> EnsureLineEndingConfigAsync(string repoDir, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public virtual Task<string[]?> GetChangedPathsForBackupAsync(string repoDir, string[]? includePrefixes, CancellationToken ct) => Task.FromResult<string[]?>(Array.Empty<string>());
    public virtual Task<(int behind, int ahead)?> GetAheadBehindAsync(string repoDir, string upstreamRef, CancellationToken ct) => Task.FromResult<(int behind, int ahead)?>((0, 0));
    public Task<GitOpResult> CreateBranchAtHeadAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct) => Task.FromResult(new GitOpResult(true));
    public Task<List<GitCommitEntry>> GetFileLogAsync(string repoDir, string relPath, int maxCount = 50, CancellationToken ct = default) => Task.FromResult(new List<GitCommitEntry>());
    public Task<string?> GetFileAtCommitAsync(string repoDir, string commitHash, string relPath, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string> GetFileDiffAsync(string repoDir, string commitHashA, string commitHashB, string relPath, CancellationToken ct = default) => Task.FromResult("");
    public Task<string?> GetHeadShaAsync(string repoDir, CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task<string> GetDiffStatAsync(string repoDir, string commitA, string commitB, CancellationToken ct = default) => Task.FromResult("");
}

// ---- IGitHubAuthService ----

public class StubGitHubAuthService : IGitHubAuthService
{
    public Task<GitHubToken?> AuthorizeDeviceFlowAsync(IProgress<string> log, CancellationToken ct, Action<DeviceCodeReady>? onDeviceCodeReady = null)
        => Task.FromResult<GitHubToken?>(null);
    public void Dispose() { }
}

// ---- IGitHubApiService ----

public class StubGitHubApiService : IGitHubApiService
{
    public Task<GitHubUser?> GetMeAsync(string accessToken, CancellationToken ct) => Task.FromResult<GitHubUser?>(null);
    public Task<bool> ForkExistsAsync(string accessToken, string owner, string repo, CancellationToken ct) => Task.FromResult(false);
    public Task<bool> CreateForkAsync(string accessToken, string upstreamOwner, string upstreamRepo, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> WaitForForkAsync(string accessToken, string owner, string repo, TimeSpan timeout, IProgress<string> log, CancellationToken ct) => Task.FromResult(true);
    public virtual Task<string?> CreatePullRequestAsync(string accessToken, string upstreamOwner, string upstreamRepo, string head, string baseBranch, string title, string body, CancellationToken ct) => Task.FromResult<string?>(null);
    public void Dispose() { }
}

// ---- ICommunityDataService ----

public class StubCommunityDataService : ICommunityDataService
{
    public Task<int> SortAndDedupApprovedTmAsync(string root, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> MergeApprovedTmFromAsync(string localRoot, string upstreamTmPath, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> SortAndDedupTermbaseAsync(string root, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> MergeTermbaseFromAsync(string localRoot, string upstreamTermbasePath, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> SortAndDedupScholarCollectionsAsync(string root, CancellationToken ct = default) => Task.FromResult(0);
    public Task<int> MergeScholarCollectionsFromAsync(string localRoot, string upstreamPath, CancellationToken ct = default) => Task.FromResult(0);
}

// ---- ISearchIndexService ----

public class StubSearchIndexService : ISearchIndexService
{
    public SearchIndexService.SearchIndexServiceOptions Options => new();
    public IReadOnlyDictionary<string, int>? CorpusCharFreqs => null;
    public IReadOnlyDictionary<string, int>? CorpusBigramFreqs => null;
    public long CorpusTotalChars => 0;
    public bool HasCorpusFrequencies => false;

    public string GetManifestPath(string root) => "";
    public string GetBinPath(string root) => "";
    public string GetTextManifestPath(string root) => "";
    public string GetTextBinPath(string root) => "";
    public string GetCjk2ManifestPath(string root) => "";

    public void ClearBloomCache() { }
    public void ClearVerifyTextCache() { }
    public void InvalidateIndexCaches() { }

    public Task<SearchIndexManifest?> TryLoadAsync(string root) => Task.FromResult<SearchIndexManifest?>(null);
    public Task<SearchTextManifest?> TryLoadTextManifestAsync(string root) => Task.FromResult<SearchTextManifest?>(null);
    public Task<SearchCjkBigramManifest?> TryLoadCjk2ManifestAsync(string root) => Task.FromResult<SearchCjkBigramManifest?>(null);

    public Task<bool> IsStaleAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs) => Task.FromResult(false);

    public Task BuildAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task BuildOrUpdateAsync(string root, string originalDir, IReadOnlyList<string> translatedDirs, bool forceRebuild, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, IProgress<(int done, int total, string phase)>? progress = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public async IAsyncEnumerable<SearchResultGroup> SearchAllAsync(string root, string originalDir, string translatedDir, SearchIndexManifest manifest, string query, bool includeOriginal, bool includeTranslated, Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta, int contextWidth, IProgress<SearchIndexService.SearchProgress>? progress = null, Func<string, bool>? relPathFilter = null, IReadOnlyList<string>? additionalOriginalDirs = null, IReadOnlyList<string>? additionalTranslatedDirs = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>> LoadSnippetsForAsync(
        string root,
        string originalDir,
        string translatedDir,
        SearchIndexManifest manifest,
        IReadOnlyList<SearchResultGroup> groups,
        string query,
        int contextWidth,
        IProgress<SearchIndexService.SearchProgress>? progress = null,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<SearchResultChild>>>(
            new Dictionary<string, IReadOnlyList<SearchResultChild>>(StringComparer.OrdinalIgnoreCase));

    public void Dispose() { }
}

// ---- IFileService ----

public class StubFileService : IFileService
{
    public Task<List<string>> EnumerateXmlRelativePathsAsync(string originalDir) => Task.FromResult(new List<string>());
    public Task<(string OriginalXml, string TranslatedXml)> ReadPairAsync(string originalDir, string translatedDir, string relativePath) => Task.FromResult(("", ""));
    public Task<(string OriginalXml, string MarkdownText)> ReadOriginalAndMarkdownAsync(string originalDir, string markdownDir, string relativePath) => Task.FromResult(("", ""));
    public Task WriteTranslatedAsync(string translatedDir, string relativePath, string translatedXml) => Task.CompletedTask;
    public Task WriteMarkdownAsync(string markdownDir, string relativePath, string markdownText) => Task.CompletedTask;
    public Task<string?> ReadOriginalAsync(string originalDir, string relPath) => Task.FromResult<string?>(null);
    public Task<string?> ReadTranslatedAsync(string translatedDir, string relPath) => Task.FromResult<string?>(null);
}

// ---- IAppConfigService ----

public class StubAppConfigService : IAppConfigService
{
    public string ConfigPath => "test-config.json";
    public int NavStatusFilterIndex { get; set; }
    public AppConfig? ConfigToReturn { get; set; }

    public Task<AppConfig?> TryLoadAsync() => Task.FromResult(ConfigToReturn);
    public Task SaveAsync(AppConfig cfg) => Task.CompletedTask;
}

// ---- IIndexCacheService ----

public class StubIndexCacheService : IIndexCacheService
{
    public string GetCachePath(string root) => "cache.json";
    public Task<IndexCache?> TryLoadAsync(string root, string? originalsRepoRoot = null) => Task.FromResult<IndexCache?>(null);
    public Task SaveAsync(string root, IndexCache cache, string? originalsRepoRoot = null) => Task.CompletedTask;
    public TranslationStatus ComputeStatusForPairLive(string origAbs, string tranAbs, string rootForLogs, string relKeyForLogs, bool verboseLog = true) => TranslationStatus.Red;
    public Task<IndexCache> BuildAsync(string originalDir, string translatedDir, string root, IProgress<(int done, int total)>? progress = null, CancellationToken ct = default) => Task.FromResult(new IndexCache());
}

// ---- IRenderedDocumentCacheService ----

public class StubRenderedDocumentCacheService : IRenderedDocumentCacheService
{
    public bool TryGet(FileStamp stamp, out RenderedDocument doc) { doc = RenderedDocument.Empty; return false; }
    public void Put(FileStamp stamp, RenderedDocument doc) { }
    public void Invalidate(string absPath) { }
    public void Clear() { }
}

// ---- IZenTextsService ----

public class StubZenTextsService : IZenTextsService
{
    public Task LoadAsync(string root) => Task.CompletedTask;
    public bool IsZen(string relPath) => false;
    public Task SetZenAsync(string root, string relPath, bool isZen) => Task.CompletedTask;
}

// ---- IIndexedTranslationService ----

public class StubIndexedTranslationService : IIndexedTranslationService
{
    public string LastBuildTranslatedXmlDebugDump => "";
    public string LastBuildTranslatedXmlDebugDumpPath => "";
    public IndexedTranslationDocument BuildIndex(string originalXml, string? translatedXml, string? originalAbsPath = null) => new();
    public string RenderProjection(IndexedTranslationDocument doc, TranslationEditMode mode) => "";
    public void ApplyProjectionEdits(IndexedTranslationDocument doc, TranslationEditMode mode, string editedText) { }
    public string BuildTranslatedXml(IndexedTranslationDocument doc, out int updatedCount) { updatedCount = 0; return ""; }
}

// ---- ILicenseMetadataService ----

public class StubLicenseMetadataService : ILicenseMetadataService
{
    public bool TryGet(string absPath, out TextLicenseInfo? info) { info = null; return false; }
    public void Set(string absPath, TextLicenseInfo info) { }
    public void Clear() { }
}

// ---- IManifestService ----

public class StubManifestService : IManifestService
{
    public ManifestInfo? TryLoad(string xmlAbsPath) => null;
}

// ---- ITranslationAssistantService ----

public class StubTranslationAssistantService : ITranslationAssistantService
{
    public string? LastUsername { get; private set; }
    public void SetUsername(string? username) { LastUsername = username; }
    public Task<TranslationAssistantSnapshot> BuildSnapshotAsync(CurrentSegmentContext ctx, string? root, string? originalDir, string? translatedDir, CancellationToken ct = default, int maxResults = 8)
        => Task.FromResult(new TranslationAssistantSnapshot());
    public Task WarmupCacheAsync(string root, CancellationToken ct = default) => Task.CompletedTask;
}

// ---- ITranslationAssistantBuildService ----

public class StubTranslationAssistantBuildService : ITranslationAssistantBuildService
{
    public Task<bool> IsReferenceStaleAsync(string root, string translatedDir) => Task.FromResult(false);
    public Task<int> BuildReferenceTranslationMemoryAsync(string root, string originalDir, string translatedDir, Func<string, bool> isZen, IProgress<(int done, int total, string status)>? progress = null, CancellationToken ct = default) => Task.FromResult(0);
    public Task AppendApprovedEntryAsync(string root, CurrentSegmentContext ctx, string reviewStatus = "Approved", string translator = "User", CancellationToken ct = default) => Task.CompletedTask;
}

// ---- ITranslationReviewService ----

public class StubTranslationReviewService : ITranslationReviewService
{
    public Task AppendReviewAsync(string root, CurrentSegmentContext ctx, string status, string reviewer = "User", string? comment = null, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Dictionary<string, TranslationReviewEntry>> LoadLatestEntriesAsync(string root, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, TranslationReviewEntry>());
    public Task<TranslationReviewEntry?> GetLatestEntryAsync(string root, CurrentSegmentContext ctx, CancellationToken ct = default) => Task.FromResult<TranslationReviewEntry?>(null);
    public Task<int> RebuildApprovedTranslationMemoryAsync(string root, CancellationToken ct = default) => Task.FromResult(0);
    public Task WriteUserReviewJsonlAsync(string communityReviewsDir, string username, CancellationToken ct = default) => Task.CompletedTask;
    public Task RefreshAggregationCacheAsync(string root, string? communityReviewsDir, CancellationToken ct = default) => Task.CompletedTask;
    public SegmentReviewAggregation? GetAggregatedReview(string segmentKey) => null;
}

// ---- IScholarCollectionsService ----

public class StubScholarCollectionsService : IScholarCollectionsService
{
    public List<ScholarCollection> Collections { get; set; } = new();
    public List<ScholarCollection>? LastSaved { get; private set; }
    public bool ThrowOnLoad { get; set; }
    public bool ThrowOnSave { get; set; }

    /// <summary>Data returned by LoadAllCommunityJsonlAsync.</summary>
    public Dictionary<string, List<ScholarCollection>> CommunityData { get; set; } = new();

    public Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (ThrowOnLoad) throw new InvalidOperationException("Load failed");
        return Task.FromResult(new List<ScholarCollection>(Collections));
    }

    public Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (ThrowOnSave) throw new InvalidOperationException("Save failed");
        LastSaved = new List<ScholarCollection>(collections);
        return Task.CompletedTask;
    }

    public Task ExportAsync(string filePath, List<ScholarCollection> collections, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<ScholarCollection>> ImportAsync(string filePath, CancellationToken ct = default) => Task.FromResult(new List<ScholarCollection>());
    public Task WriteUserJsonlAsync(string communityDir, string username, List<ScholarCollection> collections, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Dictionary<string, List<ScholarCollection>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, List<ScholarCollection>>(CommunityData));
    public Task WriteIndexJsonAsync(string communityCollectionsDir, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<ScholarCollection>> LoadUserAsync(string root, string username, CancellationToken ct = default)
    {
        if (ThrowOnLoad) throw new InvalidOperationException("Load failed");
        return Task.FromResult(new List<ScholarCollection>(Collections));
    }
    public Task SaveUserAsync(string root, string username, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (ThrowOnSave) throw new InvalidOperationException("Save failed");
        LastSaved = new List<ScholarCollection>(collections);
        return Task.CompletedTask;
    }
}

// ---- IMasterDatesService ----

public class StubMasterDatesService : IMasterDatesService
{
    public Task WriteMasterDatesJsonlAsync(string communityDir, string username, List<MasterDateEntry> entries, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Dictionary<string, List<MasterDateEntry>>> LoadAllCommunityMasterDatesAsync(string communityDir, CancellationToken ct = default) => Task.FromResult(new Dictionary<string, List<MasterDateEntry>>());
}

// ---- ITranslationStarService ----

public class StubTranslationStarService : ITranslationStarService
{
    public Task LoadAllStarsAsync(string communityStarsDir, CancellationToken ct) => Task.CompletedTask;
    public int GetStarCount(string fileId, string translator) => 0;
    public string? GetMostStarredTranslator(string fileId) => null;
    public bool IsStarredByUser(string fileId, string translator, string username) => false;
    public Task SetStarAsync(string communityStarsDir, string username, string fileId, string translator, bool starred, CancellationToken ct) => Task.CompletedTask;
    public Task WriteUserStarsJsonlAsync(string communityStarsDir, string username, CancellationToken ct) => Task.CompletedTask;
    public Task ExportAggregatedCountsAsync(string repoDir, CancellationToken ct) => Task.CompletedTask;
}

// ---- IDocumentTagService ----

public class StubDocumentTagService : IDocumentTagService
{
    public TagVocabulary Vocabulary { get; set; } = new();
    public List<DocumentTag> UserTags { get; set; } = new();
    public Dictionary<string, List<DocumentTag>> CommunityTags { get; set; } = new();
    public Dictionary<string, TagVocabulary> CommunityVocabularies { get; set; } = new();

    public Task<TagVocabulary> LoadVocabularyAsync(string root, string username, CancellationToken ct = default) => Task.FromResult(Vocabulary);
    public Task SaveVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default)
    {
        Vocabulary = vocab;
        return Task.CompletedTask;
    }

    public Task<List<DocumentTag>> LoadUserTagsAsync(string root, string username, CancellationToken ct = default)
        => Task.FromResult(new List<DocumentTag>(UserTags));

    public Task SaveUserTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default)
    {
        UserTags = new List<DocumentTag>(tags);
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, List<DocumentTag>>> LoadAllCommunityTagsAsync(string root, CancellationToken ct = default)
        => Task.FromResult(CommunityTags.ToDictionary(kv => kv.Key, kv => new List<DocumentTag>(kv.Value), StringComparer.OrdinalIgnoreCase));

    public Task<Dictionary<string, TagVocabulary>> LoadAllCommunityVocabulariesAsync(string root, CancellationToken ct = default)
        => Task.FromResult(new Dictionary<string, TagVocabulary>(CommunityVocabularies, StringComparer.OrdinalIgnoreCase));

    public Task WriteUserCommunityTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default) => Task.CompletedTask;
    public Task WriteUserCommunityVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default) => Task.CompletedTask;
}

// ---- ICommentaryService ----

/// <summary>
/// Stub for <see cref="ICommentaryService"/>. Returns null by default
/// (matches "edition has no commentary surface" semantics). Tests that
/// want canned entries set <see cref="PreloadedEntries"/>; the stub then
/// mimics CommentaryService's allowedLanguages filter logic so view-level
/// tests see realistic filtered output without touching disk.
/// </summary>
public class StubCommentaryService : ICommentaryService
{
    /// <summary>
    /// Canned commentary entries returned (after filtering) whenever
    /// <see cref="TryLoad"/> is called. Leave null to simulate "no
    /// commentary.json present" — TryLoad will return null.
    /// </summary>
    public List<CommentaryEntry>? PreloadedEntries { get; set; }

    /// <summary>
    /// Canned inference tags exposed via <see cref="GetInferenceTag"/>.
    /// Tests that exercise admin-provenance paths populate this directly;
    /// the stub does not run the real classifier.
    /// </summary>
    public Dictionary<string, LanguageTag> PreloadedInferenceTags { get; } = new(StringComparer.Ordinal);

    public int CallCount { get; private set; }
    public string? LastXmlAbsPath { get; private set; }
    public List<string>? LastAllowedLanguages { get; private set; }

    public LanguageTag? GetInferenceTag(string commentaryId)
    {
        if (string.IsNullOrEmpty(commentaryId))
            return null;
        return PreloadedInferenceTags.TryGetValue(commentaryId, out var tag) ? tag : null;
    }

    public CommentaryInfo? TryLoad(string xmlAbsPath, IEnumerable<string>? allowedLanguages = null)
    {
        CallCount++;
        LastXmlAbsPath = xmlAbsPath;
        LastAllowedLanguages = allowedLanguages?.ToList();

        if (PreloadedEntries == null)
            return null;

        // Mirror CommentaryService's allowlist semantics so this stub
        // behaves consistently with the real service for view tests.
        List<string>? whitelist = null;
        if (allowedLanguages != null)
        {
            whitelist = new List<string>();
            foreach (var tag in allowedLanguages)
                if (!string.IsNullOrWhiteSpace(tag)) whitelist.Add(tag);
            if (whitelist.Count == 0) whitelist = null;
        }

        if (whitelist == null)
            return new CommentaryInfo { Entries = new List<CommentaryEntry>(PreloadedEntries) };

        var filtered = new List<CommentaryEntry>();
        foreach (var e in PreloadedEntries)
        {
            if (string.IsNullOrWhiteSpace(e.Language)) continue;
            if (string.Equals(e.Language, "unknown", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var f in whitelist)
            {
                if (e.Language!.Equals(f, StringComparison.OrdinalIgnoreCase) ||
                    e.Language.StartsWith(f + "-", StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(e);
                    break;
                }
            }
        }
        return new CommentaryInfo { Entries = filtered };
    }
}
