@echo off
echo Building Read Zen in self-contained mode...
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean ReadZen.App.csproj -c Release

REM Restore packages
echo Restoring packages...
dotnet restore ReadZen.App.csproj

REM Build self-contained single file
echo Building self-contained single file executable...
dotnet publish ReadZen.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o bin\SelfContained

echo.
echo Build complete!
echo Executable location: bin\SelfContained\ReadZen.App.exe
echo.
echo IMPORTANT: You will also need:
echo   1. cbeta-gui-dll.dll (in same directory as exe)
echo   2. ReadZen folder (in same directory as exe)
echo.
echo The ReadZen folder contains the CBETA XML database (~500MB+)
echo and must be copied separately to the application directory.
pause

