# ReadZen v8.0.0

Read, search, and translate classical Chinese Zen texts — with critical edition support, witness evidence viewing, research graphs, and a built-in dictionary.

## Download

### Windows
| File | Description |
|------|-------------|
| `Fabulu.ReadZen-win-Setup.exe` | **Recommended.** Installer with auto-updates. |
| `Fabulu.ReadZen-win-Portable.zip` | Portable — extract and run, no install needed. |

### Linux
| File | Description |
|------|-------------|
| `Fabulu.ReadZen.AppImage` | **Recommended.** Single file: `chmod +x Fabulu.ReadZen.AppImage && ./Fabulu.ReadZen.AppImage` |
| `ReadZen-linux-x64-v8.0.0.tar.gz` | Extract and run: `tar xzf ReadZen-linux-x64-v8.0.0.tar.gz && ./ReadZen.App` |

**Linux prerequisites:**
```bash
# Required for AppImage
sudo apt install libfuse2t64

# Required X11 libraries (Ubuntu 24.04 minimal may not have these)
sudo apt install libice6 libsm6 libx11-6 libxext6 libxrandr2 libxi6 libxcursor1

# Or install all X11 libs at once:
sudo apt install xorg

# For CJK font support (Chinese characters in charts and PDF export)
sudo apt install fonts-noto-cjk
```

### macOS
| File | Description |
|------|-------------|
| `ReadZen-osx-arm64-v8.0.0.zip` | Apple Silicon (M1–M4) |
| `ReadZen-osx-x64-v8.0.0.zip` | Intel Mac |

Other files in the assets list are auto-update packages used by the app internally.

---

## What's New in v8.0.0

### Critical Edition Support (New)

ReadZen can now display critical editions with full scholarly apparatus, witness comparison, and evidence navigation.

**Faith in Mind (信心銘)** is the first published critical edition — a poem-first edition of the text traditionally attributed to Sengcan, built from 8 witnesses with 4-engine OCR and manual correction.

- **71-line poem** renders with proper verse layout (Chinese left, English right)
- **7 apparatus entries** shown as footnotes below the text with "Apparatus Notes" header
- **Red gutter dots** mark lines with editorial interventions — hover for details
- **Ctrl+click any character** to open a variant flyout showing:
  - Apparatus entry (corrected / supplied / remapped / omitted in base)
  - Accepted and rejected readings with witness attribution
  - Line excerpt for context
  - Evidence level indicator (character / line / page)
  - Witness buttons for all 8 witnesses
- **Click a witness button** to download and view the source manuscript:
  - Wikimedia Commons PDFs downloaded on demand with caching
  - Kyoto University IIIF images fetched at full resolution
  - PdfEvidenceWindow opens zoomed to the locus with yellow highlight overlay
- **Time-travel** through 8 editorial events with synchronized Chinese + English
  - Progress bar shows change type and locus (e.g., "image led opening graph correction @ T1-p031.l01")
  - Character-level `[diff]` brackets highlight what changed
  - Play/pause with configurable speed
- **9-tab Edition Details dialog** with scholarly tabs first:
  - Sources, Apparatus (with Leiden notation toggle), Collation, Corrections, Timeline, Editorial Process, Edition Log, Statistics, Documents
  - Tab tooltips explain each tab's purpose
  - Timeline stage filter uses toggle buttons (no more dropdown)
- **English companion translation** auto-discovered from `-en.xml` alongside the Chinese TEI
- **Provenance sidebar** shows edition metadata, license, witness count, maturity status

### Research Graph & Lineage Web

The Research Graph provides interactive visualization of connections between Zen masters, texts, concepts, and passages.

- **345 built-in edge types** across 7 combination categories (master↔master, master↔text, master↔concept, etc.)
- **Zoom and pan** with scroll wheel + drag
- **Node inspector** shows all connections for a selected entity
- **Lineage web** displays teacher-student transmission lines with temporal positioning
- **Korean masters** positioned near their Chinese contemporaries

### Scholar Collections

- **Auto-summary** for passages using AI assistance
- **Collection picker** shows summaries
- **User-first scholar URL scheme** for shareable links

### Stability & Safety

- **17 critical/high issues fixed** — caches, threads, git safety, memory, UI
- **Comprehensive data loss prevention** — atomic writes, merge safety, save drain on shutdown
- **11 pre-existing test failures fixed**
- **Sync guard deadlock resolved**

### Linux (Ubuntu 24.04)

- **Fixed startup crash** ("Dispatcher shut down") with 5 hardening measures:
  - Removed static constructor I/O that could crash before error handling
  - SkiaSharp font initialization wrapped in try-catch (known Ubuntu 24.04 issue)
  - Startup errors now logged to stderr (no more silent crashes)
  - X11 software rendering fallback for systems without GPU support
  - DBus menu disabled to prevent Wayland dispatcher issues
- **First-run instructions**: `chmod +x` the AppImage and install `libfuse2t64`

### Other Improvements

- **Latin serif fonts** (Georgia, Noto Serif) for English text pane — no more CJK-proportioned Latin glyphs
- **WCAG AA contrast** improvements in flyout text
- **OpenZen red titles fixed** + duplicate translation filter
- **1462 tests** (all passing)

---

**Web preview:** [readzen.pages.dev](https://readzen.pages.dev)  
**Source:** [GitHub](https://github.com/Fabulu/ReadZen)  
**Support:** [Ko-fi](https://ko-fi.com/fabulu)
