using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IScholarCollectionsService
{
    Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default);
    Task ExportAsync(string filePath, List<ScholarCollection> collections, CancellationToken ct = default);
    Task<List<ScholarCollection>> ImportAsync(string filePath, CancellationToken ct = default);

    Task WriteUserJsonlAsync(string communityDir, string username, List<ScholarCollection> collections, CancellationToken ct = default);
    Task<Dictionary<string, List<ScholarCollection>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default);

    Task<List<ScholarCollection>> LoadUserAsync(string root, string username, CancellationToken ct = default);
    Task SaveUserAsync(string root, string username, List<ScholarCollection> collections, CancellationToken ct = default);

    static string GetCommunityCollectionsDir(string repoRoot) => System.IO.Path.Combine(repoRoot, "community", "collections");
}
