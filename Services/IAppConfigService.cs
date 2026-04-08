using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IAppConfigService
{
    string ConfigPath { get; }
    int NavStatusFilterIndex { get; set; }

    Task<AppConfig?> TryLoadAsync();
    Task SaveAsync(AppConfig cfg);
}
