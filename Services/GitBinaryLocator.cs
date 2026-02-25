using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CbetaTranslator.App.Services;

public static class GitBinaryLocator
{
    // Default behavior:
    // - Prefer SYSTEM Git (full install) if found
    // - Otherwise use bundled Portable Git
    // - Final fallback: "git" (PATH)
    //
    // Optional override (env var):
    //   CBETA_GIT_PREFERENCE=system   -> prefer system git
    //   CBETA_GIT_PREFERENCE=bundled  -> prefer bundled git
    //
    // This gives you "real git first" while still supporting your bundled copy.

    public static string ResolveGitExecutablePath()
    {
        try
        {
            var pref = GetPreference();

            if (pref == GitPreference.SystemFirst)
            {
                var sys = TryResolveSystemGitAbsolutePath();
                if (!string.IsNullOrWhiteSpace(sys))
                    return sys!;

                var bundled = TryResolveBundledGitExecutablePath();
                if (!string.IsNullOrWhiteSpace(bundled))
                    return bundled!;
            }
            else // BundledFirst
            {
                var bundled = TryResolveBundledGitExecutablePath();
                if (!string.IsNullOrWhiteSpace(bundled))
                    return bundled!;

                var sys = TryResolveSystemGitAbsolutePath();
                if (!string.IsNullOrWhiteSpace(sys))
                    return sys!;
            }
        }
        catch
        {
            // ignore and fall back
        }

        return "git"; // PATH fallback
    }

    public static bool IsUsingBundledGit()
    {
        var p = ResolveGitExecutablePath();
        return IsBundledGitPath(p);
    }

    public static string? GetBundledGitRoot()
    {
        try
        {
            // Return a root only if the currently resolved git is the bundled one.
            var exe = ResolveGitExecutablePath();
            if (!IsBundledGitPath(exe))
                return null;

            return GetBundledRootFromExePath(exe);
        }
        catch
        {
            return null;
        }
    }

    public static void EnrichProcessStartInfoForBundledGit(ProcessStartInfo psi)
    {
        try
        {
            // Only enrich if the chosen executable is bundled.
            var exe = ResolveGitExecutablePath();
            if (!IsBundledGitPath(exe))
                return;

            var root = GetBundledRootFromExePath(exe);
            if (string.IsNullOrWhiteSpace(root))
                return;

            var cmd = Path.Combine(root!, "cmd");
            var bin = Path.Combine(root!, "bin");
            var mingw64bin = Path.Combine(root!, "mingw64", "bin");
            var usrBin = Path.Combine(root!, "usr", "bin");

            var oldPath = psi.Environment.TryGetValue("PATH", out var cur) ? (cur ?? "") : "";

            var parts = new[]
            {
                Directory.Exists(cmd) ? cmd : null,
                Directory.Exists(bin) ? bin : null,
                Directory.Exists(mingw64bin) ? mingw64bin : null,
                Directory.Exists(usrBin) ? usrBin : null,
                oldPath
            }
            .Where(s => !string.IsNullOrWhiteSpace(s));

            psi.Environment["PATH"] = string.Join(Path.PathSeparator, parts);

            // Helps Git locate internal commands (Portable Git on Windows)
            var gitExecPath = Path.Combine(root!, "mingw64", "libexec", "git-core");
            if (Directory.Exists(gitExecPath))
                psi.Environment["GIT_EXEC_PATH"] = gitExecPath;
        }
        catch
        {
            // ignore
        }
    }

    // -------------------------
    // Internal helpers
    // -------------------------

    private enum GitPreference
    {
        SystemFirst,
        BundledFirst
    }

    private static GitPreference GetPreference()
    {
        try
        {
            var env = Environment.GetEnvironmentVariable("CBETA_GIT_PREFERENCE")?.Trim();
            if (!string.IsNullOrWhiteSpace(env))
            {
                if (env.Equals("bundled", StringComparison.OrdinalIgnoreCase))
                    return GitPreference.BundledFirst;

                if (env.Equals("system", StringComparison.OrdinalIgnoreCase))
                    return GitPreference.SystemFirst;
            }
        }
        catch
        {
            // ignore
        }

        // Your requested default: prefer real/system Git over bundled.
        return GitPreference.SystemFirst;
    }

    private static string? TryResolveBundledGitExecutablePath()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;

            if (OperatingSystem.IsWindows())
            {
                // Bundled Portable Git layout (release YAML should create this)
                var p1 = Path.Combine(baseDir, "tools", "git", "win-x64", "cmd", "git.exe");
                if (File.Exists(p1)) return p1;

                var p2 = Path.Combine(baseDir, "tools", "git", "win-x64", "bin", "git.exe");
                if (File.Exists(p2)) return p2;
            }

            if (OperatingSystem.IsLinux())
            {
                var p = Path.Combine(baseDir, "tools", "git", "linux-x64", "bin", "git");
                if (File.Exists(p)) return p;
            }

            if (OperatingSystem.IsMacOS())
            {
                var arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                var p = Path.Combine(baseDir, "tools", "git", arch, "bin", "git");
                if (File.Exists(p)) return p;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? TryResolveSystemGitAbsolutePath()
    {
        try
        {
            // 1) PATH scan (absolute lookup) - works cross-platform
            var pathValue = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var part in pathValue.Split(Path.PathSeparator))
            {
                var dir = (part ?? "").Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(dir)) continue;

                try
                {
                    if (!Directory.Exists(dir)) continue;

                    var candidate = OperatingSystem.IsWindows()
                        ? Path.Combine(dir, "git.exe")
                        : Path.Combine(dir, "git");

                    if (File.Exists(candidate))
                        return candidate;
                }
                catch
                {
                    // ignore bad PATH entry
                }
            }

            // 2) Common Windows install locations (Git for Windows)
            if (OperatingSystem.IsWindows())
            {
                var candidates = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "cmd", "git.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Git", "bin", "git.exe"),
                };

                foreach (var c in candidates.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    if (File.Exists(c))
                        return c;
                }
            }

            // 3) As a final "system" probe, allow PATH command name if no absolute path found.
            //    We don't verify here (process launch will verify), but this preserves old behavior.
            return "null";
        }
        catch
        {
            return null;
        }
    }

    private static bool IsBundledGitPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (string.Equals(path, "git", StringComparison.OrdinalIgnoreCase)) return false;

        try
        {
            var full = Path.GetFullPath(path);
            var baseDir = Path.GetFullPath(AppContext.BaseDirectory);

            var bundledRoot = Path.Combine(baseDir, "tools", "git") + Path.DirectorySeparatorChar;
            return full.StartsWith(bundledRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string? GetBundledRootFromExePath(string exe)
    {
        try
        {
            // ...\tools\git\win-x64\cmd\git.exe -> ...\tools\git\win-x64
            var cmdOrBin = Path.GetDirectoryName(exe);
            if (cmdOrBin == null) return null;

            return Directory.GetParent(cmdOrBin)?.FullName;
        }
        catch
        {
            return null;
        }
    }
}