# Contributing to Read Zen

Thank you for your interest in contributing.

Read Zen is a desktop app for reading, translating, and studying Chinese Zen Buddhist texts across the CBETA and OpenZenTexts corpora. It also includes the web preview at readzen.pages.dev and the OpenZenTexts curation pipeline.

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch
4. Make your changes
5. Run `dotnet test` (888 automated tests)
6. Submit a Pull Request

## Project Structure

```
ReadZen/
  Views/                 Avalonia XAML + code-behind
  ViewModels/            MVVM view models
  Services/              Business logic (translation, search, sync, provenance)
  Models/                Domain types
  Infrastructure/        Avalonia behaviors, path helpers
  ReadZen.Tests/         xUnit test project (888 tests)
  Assets/Dict/           CC-CEDICT dictionary
```

## What We Value

- Corpus integrity: never silently change XML structure
- Translation safety: the projection editor rejects unsafe states rather than mangling them
- Performance: rendering is called frequently; avoid unnecessary allocations in hot paths
- Determinism: rendering logic should produce the same output for the same input
- UI responsiveness: never block the UI thread (use async/await)
- Test coverage: if it affects translation structure, search, or sync, add tests

## Translation Contributions

Translation changes should:

- Preserve XML structure and TEI validity
- Avoid reformatting unrelated content
- Be scoped to individual files when possible
- One file per PR is preferred

## OpenZenTexts Contributions

New texts for the OpenZenTexts corpus should follow the curation pipeline documented in `docs/curation/` of the OpenZenTexts repository:

- Source witnesses must be non-CBETA and freely licensed
- Each text needs a `manifest.json` with witness metadata and SHA-256 hashes
- Editorial reading editions should have provenance documentation (see the wumenguan-1632 exemplar)
- Use the `PROCESS_LOG_TEMPLATE.md` to document the transcription process as you work

## Build Requirements

- .NET 8 SDK
- Windows, Linux, or macOS

```bash
dotnet build                    # debug build
dotnet test                     # run all tests
dotnet publish -c Release -r win-x64 --self-contained true  # release build
```

## Pull Request Guidelines

- Clear title and commit message
- Describe what changed and why
- Do not bundle unrelated changes
- Run `dotnet test` before submitting
- If adding features that affect translation, search, or sync, add corresponding tests

## Discussion

If you're unsure about an architectural change, open an Issue first. We aim to keep this project stable and predictable.
