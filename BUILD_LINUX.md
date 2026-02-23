# Linux Build Instructions for CBETA Translator

## Prerequisites

- .NET SDK 8.0 or later
- Git

### Install .NET SDK 8 on Ubuntu/Debian:
```bash
# Preferred for self-contained Linux publish (non-snap)
sudo apt update
sudo apt install dotnet-sdk-8.0

# If dotnet-sdk-8.0 is unavailable in your apt sources,
# install from Microsoft packages:
# https://learn.microsoft.com/dotnet/core/install/linux-ubuntu
```

## Build Options

### 1. Development Build (requires .NET runtime to run)
```bash
./eng/build-linux.sh
# or for release configuration
./eng/build-linux.sh Release
```

Run the application:
```bash
./run-cbeta.sh
```

### 2. Self-Contained Build (standalone executable)
```bash
# Debug build
./eng/build-linux.sh Debug true linux-x64

# Release build (recommended for distribution)
./eng/build-linux.sh Release true linux-x64
```

Run the standalone executable:
```bash
./run-cbeta-selfcontained.sh linux-x64
```

## Build Script Parameters

The `build-linux.sh` script accepts four parameters:

1. **Configuration**: `Debug` (default) or `Release`
2. **Publish**: `false` (default, development build) or `true` (self-contained)
3. **Runtime Identifier**: `linux-x64` (default), `linux-arm64`, etc.
4. **Single File**: `false` (default, recommended) or `true` (advanced)

### Examples:
```bash
# Development build (default)
./eng/build-linux.sh

# Release development build
./eng/build-linux.sh Release

# Self-contained debug build
./eng/build-linux.sh Debug true

# Self-contained release build for distribution
./eng/build-linux.sh Release true linux-x64

# Build for ARM64
./eng/build-linux.sh Release true linux-arm64

# Advanced: force single-file self-contained publish
./eng/build-linux.sh Release true linux-x64 true
```

## Optional Dependencies

For PDF export functionality, you may need:
- `cbeta_gui_dll.dll` (place in output folder or set `CBETA_GUI_DLL_PATH` environment variable)

## Troubleshooting

### Permission denied
```bash
chmod +x ./eng/build-linux.sh ./run-cbeta.sh ./run-cbeta-selfcontained.sh
```

### dotnet command not found
Install .NET SDK 8.0 as shown in prerequisites, then restart your terminal.

### Self-contained app fails with GLIBC or SkiaSharp errors
- Rebuild using non-single-file mode (default):
  `./eng/build-linux.sh Release true linux-x64`
- Launch via:
  `./run-cbeta-selfcontained.sh linux-x64`
- If your `dotnet` comes from Snap, use a non-snap SDK for Linux self-contained publish.
