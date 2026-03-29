using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Registers and unregisters the <c>cbeta://</c> protocol handler with the OS.
/// Windows: HKCU\Software\Classes\cbeta (no admin required).
/// Linux: ~/.local/share/applications/ desktop file + xdg-mime.
/// macOS: logs a warning (requires Info.plist in the app bundle).
/// </summary>
public static class ProtocolRegistrationService
{
    private const string ProtocolName = "cbeta";
    private const string Description = "CBETA Translator Deep Link";

    /// <summary>
    /// Returns <c>true</c> if the <c>cbeta://</c> protocol handler appears to be registered.
    /// </summary>
    public static bool IsRegistered()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return IsRegisteredWindows();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return IsRegisteredLinux();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return IsRegisteredMacOS();

        return false;
    }

    /// <summary>
    /// Registers the <c>cbeta://</c> protocol handler for the current user.
    /// </summary>
    public static void Register()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RegisterWindows();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            RegisterLinux();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            RegisterMacOS();
        }
    }

    /// <summary>
    /// Removes the <c>cbeta://</c> protocol handler registration for the current user.
    /// </summary>
    public static void Unregister()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UnregisterWindows();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            UnregisterLinux();
            return;
        }
    }

    // ─── Windows ────────────────────────────────────────────────────────

#pragma warning disable CA1416 // Platform compatibility — guarded by RuntimeInformation checks

    private static string GetExePath()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
            exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        return exePath;
    }

    private static bool IsRegisteredWindows()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Classes\" + ProtocolName);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private static void RegisterWindows()
    {
        var exePath = GetExePath();
        if (string.IsNullOrEmpty(exePath))
            return;

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser
                .CreateSubKey(@"Software\Classes\" + ProtocolName);

            key.SetValue("", "URL:" + Description);
            key.SetValue("URL Protocol", "");

            using var iconKey = key.CreateSubKey("DefaultIcon");
            iconKey.SetValue("", exePath + ",1");

            using var commandKey = key.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", "\"" + exePath + "\" \"%1\"");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to register cbeta:// protocol on Windows: " + ex.Message);
        }
    }

    private static void UnregisterWindows()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser
                .DeleteSubKeyTree(@"Software\Classes\" + ProtocolName, throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to unregister cbeta:// protocol on Windows: " + ex.Message);
        }
    }

#pragma warning restore CA1416

    // ─── Linux ──────────────────────────────────────────────────────────

    private static string GetDesktopFilePath()
    {
        var appsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local", "share", "applications");
        return Path.Combine(appsDir, "cbeta-translator.desktop");
    }

    private static bool IsRegisteredLinux()
    {
        return File.Exists(GetDesktopFilePath());
    }

    private static void RegisterLinux()
    {
        var exePath = GetExePath();
        if (string.IsNullOrEmpty(exePath))
            return;

        try
        {
            var desktopPath = GetDesktopFilePath();
            var appsDir = Path.GetDirectoryName(desktopPath)!;
            Directory.CreateDirectory(appsDir);

            var content =
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=CBETA Translator\n" +
                "Exec=" + exePath + " %u\n" +
                "StartupNotify=false\n" +
                "Terminal=false\n" +
                "MimeType=x-scheme-handler/cbeta;\n";

            File.WriteAllText(desktopPath, content);

            // Register with xdg-mime
            var psi = new ProcessStartInfo("xdg-mime", "default cbeta-translator.desktop x-scheme-handler/cbeta")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to register cbeta:// protocol on Linux: " + ex.Message);
        }
    }

    private static void UnregisterLinux()
    {
        try
        {
            var desktopPath = GetDesktopFilePath();
            if (File.Exists(desktopPath))
                File.Delete(desktopPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to unregister cbeta:// protocol on Linux: " + ex.Message);
        }
    }

    // ===== macOS =====

    private static bool IsRegisteredMacOS()
    {
        try
        {
            // Check if a handler script exists
            var handlerPath = GetMacHandlerScriptPath();
            return File.Exists(handlerPath);
        }
        catch
        {
            return false;
        }
    }

    private static void RegisterMacOS()
    {
        try
        {
            var exePath = GetExePath();
            if (string.IsNullOrEmpty(exePath)) return;

            // Create a minimal .app bundle wrapper that handles cbeta:// URLs
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Applications", "CbetaTranslatorLink.app");
            var contentsDir = Path.Combine(appDir, "Contents");
            var macosDir = Path.Combine(contentsDir, "MacOS");

            Directory.CreateDirectory(macosDir);

            // Write Info.plist with URL handler
            var plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n<dict>\n" +
                "    <key>CFBundleName</key><string>CBETA Translator Link</string>\n" +
                "    <key>CFBundleIdentifier</key><string>com.cbeta.translator.link</string>\n" +
                "    <key>CFBundleVersion</key><string>1.0</string>\n" +
                "    <key>CFBundlePackageType</key><string>APPL</string>\n" +
                "    <key>CFBundleExecutable</key><string>open-cbeta</string>\n" +
                "    <key>CFBundleURLTypes</key>\n" +
                "    <array><dict>\n" +
                "        <key>CFBundleURLName</key><string>CBETA Deep Link</string>\n" +
                "        <key>CFBundleURLSchemes</key><array><string>cbeta</string></array>\n" +
                "    </dict></array>\n" +
                "</dict>\n</plist>\n";
            File.WriteAllText(Path.Combine(contentsDir, "Info.plist"), plist);

            // Write launcher script that forwards the URL to the real app
            var script = "#!/bin/bash\n" +
                "exec \"" + exePath + "\" \"$@\"\n";
            var scriptPath = Path.Combine(macosDir, "open-cbeta");
            File.WriteAllText(scriptPath, script);

            // Make executable
            var chmod = new ProcessStartInfo("chmod", "+x \"" + scriptPath + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var chmodProc = Process.Start(chmod);
            chmodProc?.WaitForExit(5000);

            // Register with LaunchServices
            var lsregister = new ProcessStartInfo(
                "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister",
                "-R \"" + appDir + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var lsProc = Process.Start(lsregister);
            lsProc?.WaitForExit(5000);

            Debug.WriteLine("Registered cbeta:// protocol handler via " + appDir);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to register cbeta:// protocol on macOS: " + ex.Message);
        }
    }

    private static string GetMacHandlerScriptPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Applications", "CbetaTranslatorLink.app", "Contents", "MacOS", "open-cbeta");
    }
}
