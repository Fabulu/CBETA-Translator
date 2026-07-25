namespace ReadZen.App.Messages;

// Status updates
public sealed record StatusMessage(string Text);

// Settings: broadcast after the config changes (startup load + settings dialog apply).
// First live messenger usage per the CLAUDE.md UI-architecture ratchet - views
// register for this instead of adding MainWindowViewModel bridge delegates.
public sealed record SettingsAppliedMessage(Models.AppConfig Config);

// File navigation
public sealed record FileSelectedMessage(string RelPath);
public sealed record RootLoadedMessage(string Root, string OriginalDir, string TranslatedDir);
public sealed record RootClonedMessage(string RepoRoot);

// Translation operations
public sealed record SaveRequestedMessage;
public sealed record RevertRequestedMessage;
public sealed record SegmentChangedMessage(int BlockNumber, string ZhText, string EnText, string ZhContextText);
public sealed record ReviewActionMessage(string Status);
public sealed record NextUnapprovedMessage;

// Navigation
public sealed record NavigateToFileMessage(Models.NavigationRequest Request);

// Community notes
public sealed record CommunityNoteInsertMessage(int XmlIndex, string NoteText, string? Resp);
public sealed record CommunityNoteDeleteMessage(int XmlStart, int XmlEndExclusive);

// Zen
public sealed record ZenFlagChangedMessage(string RelPath, bool IsZen);

// Lineage chart fullscreen (SPA parity: the chart fills the whole screen). Sent by the
// embedded ZenMasterManagerView so the host MainWindow hides its app chrome (top bar +
// nested TabStrip + status bar) around the FullScreen window state, leaving only the
// lineage tab content visible; On=false restores the chrome. The floating
// ZenMasterManagerWindow is chrome-less and already fills, so the view never sends this
// when hosted there.
public sealed record LineageFullscreenRequestedMessage(bool On);

// Open a Zen master in the Zen Master Manager window (SPA "#/master/{name}" parity).
// Sent by dictionary surfaces (dict tab + pop-out, reader study panel, entry cards);
// MainWindow registers the single handler, so every host window gets the behavior
// without per-host event wiring.
public sealed record OpenMasterRequestedMessage(string MasterName);

// Termbase
public sealed record TermsSavedMessage;
public sealed record BuildReferenceTmMessage;
public sealed record ManageTermsMessage;

// Git / corpus
// Broadcast after a git sync/clone/update/panic-reset finishes successfully;
// MainWindowViewModel queues the IsStaleAsync-gated, debounced auto index build -
// no MWVM bridge delegate per the ratchet.
public sealed record CorpusFilesChangedMessage(string RepoRoot);
