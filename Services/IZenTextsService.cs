using System.Threading.Tasks;

namespace ReadZen.App.Services;

public interface IZenTextsService
{
    Task LoadAsync(string root);
    bool IsZen(string relPath);
    Task SetZenAsync(string root, string relPath, bool isZen);
}
