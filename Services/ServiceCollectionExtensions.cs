using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // Register all services as singletons
        services.AddSingleton<IAppConfigService, AppConfigService>();
        services.AddSingleton<ICedictDictionary, CedictDictionaryService>();
        services.AddSingleton<IFileService, FileService>();
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
        services.AddSingleton<EditionStatsService>();
        services.AddSingleton<DocumentsService>();
        services.AddSingleton<TimelineService>();
        services.AddSingleton<HumanLogService>();
        services.AddSingleton<TranslationLicenseService>();
        services.AddSingleton<WitnessTextService>();
        services.AddSingleton<IIndexedTranslationService, IndexedTranslationService>();
        services.AddSingleton<IRenderedDocumentCacheService>(_ => new RenderedDocumentCacheService(48));
                services.AddSingleton<ISearchIndexService, SearchIndexService>();
        services.AddSingleton<ISearchExportService, SearchExportService>();
        services.AddSingleton<ISearchIndexBuilder>(sp => new SearchIndexBuilder(sp.GetRequiredService<ISearchIndexService>()));
        services.AddSingleton<ISearchEngine>(sp => new SearchEngine(sp.GetRequiredService<ISearchIndexService>()));
        services.AddSingleton<ICooccurrenceService, CooccurrenceService>();
        services.AddSingleton<IZenTextsService, ZenTextsService>();
        services.AddSingleton<ITranslationAssistantService, TranslationAssistantService>();
        services.AddSingleton<ITranslationAssistantBuildService, TranslationAssistantBuildService>();
        services.AddSingleton<ITranslationReviewService, TranslationReviewService>();
        services.AddSingleton<ITranslationMemoryService, TranslationMemoryService>();
        services.AddSingleton<ITranslationQaService, TranslationQaService>();
        services.AddSingleton<ITermbaseService, TermbaseService>();
        services.AddSingleton<ITermbaseStorageService, TermbaseStorageService>();
        services.AddSingleton<ICommunityDataService, CommunityDataService>();

        services.AddSingleton<IScholarCollectionsService, ScholarCollectionsService>();
        services.AddSingleton<IDocumentTagService, DocumentTagService>();
        services.AddSingleton<IScholarExportService, ScholarExportService>();
        services.AddSingleton<IParallelPassageFinderService, ParallelPassageFinderService>();
        services.AddSingleton<IGrammarReferenceService, GrammarReferenceService>();
        services.AddSingleton<IMasterDatesService, MasterDatesService>();
        services.AddSingleton<OnboardingTourService>();
        services.AddSingleton<IDocumentVariableService, DocumentVariableService>();
        return services;
    }
}
