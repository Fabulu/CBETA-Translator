using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IAppConfigService
{
    string ConfigPath { get; }
    int NavStatusFilterIndex { get; set; }

    /// <summary>
    /// Applied value of <see cref="Models.AppConfig.ShowApparatusNotes"/>, mirrored
    /// into the singleton service so views (e.g. the reader's apparatus-notes gate)
    /// can read the current setting without holding the whole config. Synced from
    /// the loaded config in <see cref="TryLoadAsync"/> and refreshed when settings
    /// are applied at runtime. Default false (panel OFF).
    /// </summary>
    bool ShowApparatusNotes { get; set; }

    /// <summary>Path of the backup written when a corrupt config.json is detected.</summary>
    string CorruptBackupPath { get; }

    /// <summary>
    /// One-time notice set by <see cref="TryLoadAsync"/> when config.json fails to
    /// parse: the bad file is preserved at <see cref="CorruptBackupPath"/> and
    /// defaults are used for the session. Non-null after a corrupt load so the UI
    /// can inform the user instead of silently resetting every setting. Null when
    /// the last load was clean.
    /// </summary>
    string? LoadWarning { get; }

    Task<AppConfig?> TryLoadAsync();
    Task SaveAsync(AppConfig cfg);
}
