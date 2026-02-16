# Building CBETA Translator as Self-Contained Executable

This guide explains how to build CBETA Translator as a single self-contained executable that only requires the Rust DLL to run.

## 🎯 What Self-Contained Means

- **Single executable**: All .NET dependencies are bundled into one `.exe` file
- **No .NET runtime required**: Users don't need to install .NET 8.0
- **Only external dependency**: Just the Rust DLL (`cbeta-gui-dll.dll`) needs to be shipped separately
- **Portable**: Can be copied to any Windows machine and run

## 📋 Prerequisites

- .NET 8.0 SDK (for building only)
- Visual Studio 2022 or VS Code (optional)
- Rust DLL (`cbeta-gui-dll.dll`) in the output directory

## 🚀 Quick Build

### Method 1: Using the Build Script (Recommended)

```bash
# Run the build script
.\build-selfcontained.bat
```

This will create the executable in `bin\SelfContained\`

### Method 2: Manual Command

```bash
# Clean previous builds
dotnet clean -c Release

# Build self-contained single file
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o bin\SelfContained
```

### Method 3: Using Visual Studio

1. Right-click the project → **Publish**
2. Select **Folder** target
3. Choose **win-x64** runtime
4. Set **Deployment mode** to **Self-contained**
5. Enable **Produce single file**
6. Click **Publish**

## 📁 Output Structure

After building, you'll have:

```
bin\SelfContained\
├── CbetaTranslator.App.exe    # ~46MB single executable
├── Assets\
│   └── Dict\
│       └── cedict_ts.u8      # Dictionary file (embedded)
└── cbeta-gui-dll.dll         # Rust DLL (you must add this)
```

## ⚙️ Configuration Details

The project file (`CbetaTranslator.App.csproj`) includes these key settings:

```xml
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
<PublishTrimmed>false</PublishTrimmed>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

### Why `PublishTrimmed=false`?

- **Avalonia compatibility**: Trimming can break Avalonia UI frameworks
- **Reflection-heavy code**: Buddhist metadata service uses reflection
- **Stability**: Full framework inclusion prevents runtime issues

## 🔧 Deployment

### For Distribution

1. Copy the entire `bin\SelfContained\` folder
2. Ensure `cbeta-gui-dll.dll` is in the same directory as the exe
3. The application will run without any .NET runtime installation

### File Size

- **Single exe**: ~46MB (compressed with all dependencies)
- **Dictionary**: ~9.3MB (embedded)
- **Rust DLL**: ~5MB (separate)
- **Total**: ~60MB

## 🐛 Troubleshooting

### Common Issues

1. **"DLL not found" error**
   - Ensure `cbeta-gui-dll.dll` is in the same directory as the exe
   - Check that the Rust DLL matches the target architecture (x64)

2. **Build fails with warnings**
   - Warnings are expected (nullable reference warnings)
   - Build should still succeed despite warnings

3. **Application won't start**
   - Verify Windows version compatibility (Windows 10+)
   - Check antivirus isn't blocking the executable

### Verification

To verify the build worked:

```bash
# Check file size (should be ~46MB)
dir bin\SelfContained\CbetaTranslator.App.exe

# Check embedded assets
dir bin\SelfContained\Assets\Dict\cedict_ts.u8
```

## 🔄 Development vs Production

- **Development**: Use regular Debug/Release builds for faster iteration
- **Production**: Use self-contained builds for distribution
- **Testing**: Test self-contained builds before distribution

## 📝 Notes

- The self-contained build includes the Chinese dictionary (`cedict_ts.u8`) automatically
- Build time is longer due to framework bundling (~20-30 seconds)
- Startup time may be slightly slower due to decompression
- Memory usage is comparable to regular builds
