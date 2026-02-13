# Contributing to CBETA Translator

Thank you for your interest in contributing.

This project focuses on:

- High-performance TEI XML rendering
- Structured translation workflows
- Community note integration
- Git-based translation collaboration

## 🚀 Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch
4. Make your changes
5. Submit a Pull Request

## 📂 Project Structure

CbetaTranslator.App/
- Views/               UI
- Services/            File, Index, Cache services
- Text/                TEI rendering logic
- Models/              Domain models
- Assets/Dict/         Dictionary files

## 🧠 Translation Contributions

Translation changes should:

- Preserve XML structure
- Never break TEI validity
- Avoid reformatting unrelated content
- Be scoped to individual files whenever possible

One file per PR is preferred.

## 🛠 Build Requirements

- .NET 8 SDK
- Windows, Linux, or macOS

Build:

```
dotnet publish -c Release -r win-x64 --self-contained true
```


## 🧪 Code Guidelines

- Keep rendering logic deterministic
- Avoid blocking UI thread
- No unnecessary allocations in hot paths
- Preserve segment mapping correctness

## 📌 Pull Request Rules

- Clear title
- Clear commit message
- Describe what changed and why
- Do not bundle unrelated changes

## 📣 Discussion

If you're unsure about an architectural change, open an Issue first.

We aim to keep this project stable and predictable.


