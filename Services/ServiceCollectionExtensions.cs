using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Register all services as singletons
        services.AddSingleton<IAppConfigService, AppConfigService>();
        services.AddSingleton<ICedictDictionary, CedictDictionaryService>();
        services.AddSingleton<IGitRepoService, GitRepoService>();
        services.AddSingleton<IGitHubApiService, GitHubApiService>();
        services.AddSingleton<IGitHubAuthService, GitHubAuthService>();
        services.AddSingleton<ISelectionSyncService, SelectionSyncService>();
        services.AddSingleton<ITranslationStatusService, TranslationStatusService>();
        services.AddSingleton<IIndexCacheService, IndexCacheService>();
        services.AddSingleton<ILicenseMetadataService, LicenseMetadataService>();
        services.AddSingleton<IManifestService, ManifestService>();
        services.AddSingleton<ProcessService>();
        services.AddSingleton<ApparatusService>();
        services.AddSingleton<ICommentaryService, CommentaryService>();
        services.AddSingleton<ISegmentMapService, SegmentMapService>();
        services.AddSingleton<EditionStatsService>();
        services.AddSingleton<DocumentsService>();
        services.AddSingleton<TimelineService>();
        services.AddSingleton<HumanLogService>();
        services.AddSingleton<TranslationLicenseService>();
        services.AddSingleton<WitnessTextService>();
        services.AddSingleton<MasterCorpusSearchService>();
        services.AddSingleton<IIndexedTranslationService, IndexedTranslationService>();
        services.AddSingleton<IRenderedDocumentCacheService>(_ => new RenderedDocumentCacheService(48));
                services.AddSingleton<ISearchIndexService, SearchIndexService>();
        services.AddSingleton<ISearchExportService, SearchExportService>();
        services.AddSingleton<IZenTextsService, ZenTextsService>();
        services.AddSingleton<ITranslationAssistantService, TranslationAssistantService>();
        services.AddSingleton<ITranslationAssistantBuildService, TranslationAssistantBuildService>();
        services.AddSingleton<ITranslationReviewService, TranslationReviewService>();
        services.AddSingleton<ITranslationMemoryService, TranslationMemoryService>();
        services.AddSingleton<ITranslationQaService, TranslationQaService>();
        services.AddSingleton<ITermbaseService, TermbaseService>();
        services.AddSingleton<ITermbaseStorageService, TermbaseStorageService>();
        services.AddSingleton<IDictionaryStore, DictionaryStore>();
        services.AddSingleton<IZenDictionaryLookup, ZenDictionaryLookupService>();
        services.AddSingleton<IDictionaryEvidenceService, DictionaryEvidenceService>();
        services.AddSingleton<ICommunityDataService, CommunityDataService>();
        services.AddSingleton<ITranslationStarService, TranslationStarService>();
        services.AddSingleton<ICitationService, CitationService>();

        services.AddSingleton<IScholarCollectionsService, ScholarCollectionsService>();
        services.AddSingleton<IDocumentTagService, DocumentTagService>();
        services.AddSingleton<IScholarExportService, ScholarExportService>();
        services.AddSingleton<IParallelPassageFinderService, ParallelPassageFinderService>();
        services.AddSingleton<IGrammarReferenceService, GrammarReferenceService>();
        services.AddSingleton<IMasterDatesService, MasterDatesService>();
        services.AddSingleton<ILineageRosterService, LineageRosterService>();
        services.AddSingleton<ZenMasterManagerService>();
        services.AddSingleton<OnboardingTourService>();
        services.AddSingleton<IDocumentVariableService, DocumentVariableService>();
        services.AddSingleton<PdfEvidenceService>();
        services.AddSingleton<AppUpdateService>();
        services.AddSingleton<BookmarkService>();
        services.AddSingleton<ReaderStateService>();
        return services;
    }
}
