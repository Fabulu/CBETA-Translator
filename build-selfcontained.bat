@echo off
echo Building CBETA Translator in self-contained mode...
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean CbetaTranslator.App.csproj -c Release

REM Restore packages
echo Restoring packages...
dotnet restore CbetaTranslator.App.csproj

REM Build self-contained single file
echo Building self-contained single file executable...
dotnet publish CbetaTranslator.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o bin\SelfContained

echo.
echo Build complete!
echo Executable location: bin\SelfContained\CbetaTranslator.App.exe
echo.
echo Note: You will still need the Rust DLL in the same directory as the exe.
pause
