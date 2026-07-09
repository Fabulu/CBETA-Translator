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

// Termbase
public sealed record TermsSavedMessage;
public sealed record BuildReferenceTmMessage;
public sealed record ManageTermsMessage;

// Git / corpus
// Broadcast after a git sync/clone/update/panic-reset finishes successfully;
// MainWindowViewModel queues the IsStaleAsync-gated, debounced auto index build -
// no MWVM bridge delegate per the ratchet.
public sealed record CorpusFilesChangedMessage(string RepoRoot);
