using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IAppConfigService
{
    string ConfigPath { get; }
    int NavStatusFilterIndex { get; set; }

    Task<AppConfig?> TryLoadAsync();
    Task SaveAsync(AppConfig cfg);
}
