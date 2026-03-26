using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IRenderedDocumentCacheService
{
    bool TryGet(FileStamp stamp, out RenderedDocument doc);
    void Put(FileStamp stamp, RenderedDocument doc);
    void Invalidate(string absPath);
    void Clear();
}
