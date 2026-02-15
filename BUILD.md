# Build and Bootstrap

## Prerequisites

Required:
- .NET SDK 8
- Git

Optional (PDF export path used by this app):
- `cbeta_gui_dll.dll` available from one of:
  - `CBETA_GUI_DLL_PATH` env var
  - app output folder
  - `D:\Rust-projects\MT15-model\cbeta-gui-dll\target\release\cbeta_gui_dll.dll`

## Windows (PowerShell)

1. Check/install tools:

```powershell
.\eng\bootstrap.ps1
# optional auto install (winget):
.\eng\bootstrap.ps1 -InstallDotnet -InstallGit
```

2. Build:

```powershell
.\eng\build.ps1
# or release
.\eng\build.ps1 -Configuration Release
```

3. Run:

```powershell
dotnet run --project .\CbetaTranslator.App.csproj -c Debug
```

## Linux/WSL

```bash
./eng/build.sh
# or release
./eng/build.sh Release
```

## Markdown Translation Pipeline Notes

This repo now uses markdown as editable translation source:
- Original TEI: `xml-p5/**/*.xml`
- Editable markdown: `md-p5t/**/*.md`
- Materialized translated TEI (generated on demand for PDF/Git): `xml-p5t/**/*.xml`

Generation behavior:
- Markdown file is generated lazily when a file is first opened in the Edit tab and no markdown exists yet.
- Saving in Edit tab saves markdown only.
- PDF export and Git contribution steps materialize translated XML from markdown just-in-time.
