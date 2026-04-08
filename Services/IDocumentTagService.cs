using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IDocumentTagService
{
    // Vocabulary (single JSON file per user)
    Task<TagVocabulary> LoadVocabularyAsync(string root, string username, CancellationToken ct = default);
    Task SaveVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default);

    // Applied tags (JSONL per user)
    Task<List<DocumentTag>> LoadUserTagsAsync(string root, string username, CancellationToken ct = default);
    Task SaveUserTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default);

    // Community tags (read other users' tags)
    Task<Dictionary<string, List<DocumentTag>>> LoadAllCommunityTagsAsync(string root, CancellationToken ct = default);
    Task<Dictionary<string, TagVocabulary>> LoadAllCommunityVocabulariesAsync(string root, CancellationToken ct = default);

    // Share own tags to community
    Task WriteUserCommunityTagsAsync(string root, string username, List<DocumentTag> tags, CancellationToken ct = default);
    Task WriteUserCommunityVocabularyAsync(string root, string username, TagVocabulary vocab, CancellationToken ct = default);
}
