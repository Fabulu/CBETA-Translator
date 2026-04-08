# Building Read Zen as Self-Contained Executable

This guide explains how to build Read Zen as a single self-contained executable.

## ðŸŽ¯ What Self-Contained Means

- **Single executable**: All .NET dependencies are bundled into one `.exe` file
- **No .NET runtime required**: Users don't need to install .NET 8.0

- **Portable**: Can be copied to any Windows machine and run

## ðŸ“‹ Prerequisites

- .NET 8.0 SDK (for building only)
- Visual Studio 2022 or VS Code (optional)

## ðŸš€ Quick Build

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

1. Right-click the project â†’ **Publish**
2. Select **Folder** target
3. Choose **win-x64** runtime
4. Set **Deployment mode** to **Self-contained**
5. Enable **Produce single file**
6. Click **Publish**

## ðŸ“ Output Structure

After building, you'll have:

```
bin\SelfContained\
â”œâ”€â”€ ReadZen.App.exe    # ~46MB single executable
â”œâ”€â”€ Assets\
â”‚   â””â”€â”€ Dict\
â”‚       â””â”€â”€ cedict_ts.u8      # Dictionary file (embedded)
```

## âš ï¸ **IMPORTANT: Additional Data Required**

The self-contained executable **does NOT include the CBETA XML database**. You must also provide:

### Required External Data:
1. **ReadZen folder** (~500MB+)
   - Contains 4,990 XML files in `xml-p5/` subfolder
   - Translation files in `xml-p5t/` and `md-p5t/`
   - Index files and metadata
   - **Must be placed in same directory as the exe**

### Complete Deployment Structure:
```
[Application Directory]\
â”œâ”€â”€ ReadZen.App.exe          # ~46MB self-contained exe
â””â”€â”€ ReadZen\                   # ~500MB+ CBETA database
    â”œâ”€â”€ xml-p5\                      # Original XML files
    â”œâ”€â”€ xml-p5t\                     # Translated XML files  
    â”œâ”€â”€ md-p5t\                      # Markdown translations
    â”œâ”€â”€ canons.json                  # Canon metadata
    â””â”€â”€ index.cache.json             # Search index
```

### Why Not Include ReadZen in Self-Contained Build?
- **Size**: The XML database is ~500MB+ vs 46MB exe
- **Updates**: Database can be updated independently of app
- **Flexibility**: Users can choose which canons to include
- **Build time**: Including 5,000+ files would make builds extremely slow

## âš™ï¸ Configuration Details

The project file (`ReadZen.App.csproj`) includes these key settings:

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

## ðŸ”§ Deployment

### For Distribution

1. Copy the entire `bin\SelfContained\` folder
2. The application will run without any .NET runtime installation

### File Size

- **Single exe**: ~46MB (compressed with all dependencies)
- **Dictionary**: ~9.3MB (embedded)
- **Total**: ~55MB

## ðŸ› Troubleshooting

### Common Issues

1. **Build fails with warnings**
   - Warnings are expected (nullable reference warnings)
   - Build should still succeed despite warnings

3. **Application won't start**
   - Verify Windows version compatibility (Windows 10+)
   - Check antivirus isn't blocking the executable

### Verification

To verify the build worked:

```bash
# Check file size (should be ~46MB)
dir bin\SelfContained\ReadZen.App.exe

# Check embedded assets
dir bin\SelfContained\Assets\Dict\cedict_ts.u8
```

## ðŸ”„ Development vs Production

- **Development**: Use regular Debug/Release builds for faster iteration
- **Production**: Use self-contained builds for distribution
- **Testing**: Test self-contained builds before distribution

## ðŸ“ Notes

- The self-contained build includes the Chinese dictionary (`cedict_ts.u8`) automatically
- Build time is longer due to framework bundling (~20-30 seconds)
- Startup time may be slightly slower due to decompression
- Memory usage is comparable to regular builds

