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

        // macOS — cannot check easily
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
            Debug.WriteLine(
                "macOS protocol registration requires Info.plist configuration in the app bundle. "
                + "Automatic registration is not supported.");
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
}
