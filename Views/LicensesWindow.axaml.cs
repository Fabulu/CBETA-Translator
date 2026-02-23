using System;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Infrastructure;

namespace CbetaTranslator.App.Views;

public partial class LicensesWindow : Window
{
    private readonly string? _root;

    private TextBox? _txtLicenses;
    private TextBlock? _txtHint;
    private Button? _btnClose;

    public LicensesWindow(string? root)
    {
        _root = root;

        InitializeComponent();
        FindControls();
        WireEvents();

        LoadText();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _txtLicenses = this.FindControl<TextBox>("TxtLicenses");
        _txtHint = this.FindControl<TextBlock>("TxtHint");
        _btnClose = this.FindControl<Button>("BtnClose");
    }

    private void WireEvents()
    {
        if (_btnClose != null)
            _btnClose.Click += (_, _) => Close();
    }

    private void LoadText()
    {
        if (_txtLicenses == null) return;

        string baseDir = AppContext.BaseDirectory;

        // Dictionary attribution (read from shipped dictionary header, same path your runtime uses)
        string dictPath = Path.Combine(baseDir, "assets", "dict", "cedict_ts.u8");
        string dictHeader = SafeReadCedictHeader(dictPath);

        // Top-level notices (copied into publish root by release workflow)
        string thirdPartyNoticesPath = Path.Combine(baseDir, "THIRD_PARTY_NOTICES.txt");
        string projectLicensePath = Path.Combine(baseDir, "LICENSE");

        string thirdPartyNotices = SafeReadTextFile(thirdPartyNoticesPath);
        string projectLicense = SafeReadTextFile(projectLicensePath);

        // Windows-only bundled Git notices copied by release workflow:
        // publish/licenses/git-for-windows/*
        string gitNoticeDir = Path.Combine(baseDir, "licenses", "git-for-windows");
        string gitBundleSummary = BuildGitBundleSummary(gitNoticeDir);

        var sb = new StringBuilder();

        sb.AppendLine("CBETA Translator - Licenses & Attributions");
        sb.AppendLine("===========================================");
        sb.AppendLine();

        sb.AppendLine("=== App License (this project) ===");
        if (!string.IsNullOrWhiteSpace(projectLicense))
        {
            sb.AppendLine(projectLicense.Trim());
        }
        else
        {
            sb.AppendLine("LICENSE file not found next to the app executable.");
        }
        sb.AppendLine();

        sb.AppendLine("=== Third-Party Notices (summary) ===");
        if (!string.IsNullOrWhiteSpace(thirdPartyNotices))
        {
            sb.AppendLine(thirdPartyNotices.Trim());
        }
        else
        {
            sb.AppendLine("THIRD_PARTY_NOTICES.txt not found next to the app executable.");
        }
        sb.AppendLine();

        sb.AppendLine("=== CC-CEDICT Dictionary Attribution (from shipped dictionary header) ===");
        sb.AppendLine(string.IsNullOrWhiteSpace(dictHeader)
            ? "CC-CEDICT dictionary header not available (dictionary file missing or unreadable)."
            : dictHeader.Trim());
        sb.AppendLine();

        sb.AppendLine("=== Bundled Git for Windows (Windows builds only) ===");
        sb.AppendLine(gitBundleSummary);
        sb.AppendLine();

        sb.AppendLine("=== Notes ===");
        sb.AppendLine("- The CC-CEDICT attribution text above is read from the dictionary file header shipped with the app.");
        sb.AppendLine("- If you replace the dictionary file, the displayed dictionary attribution updates automatically.");
        sb.AppendLine("- Full notices are also available in LICENSE and THIRD_PARTY_NOTICES.txt in the app folder.");
        sb.AppendLine("- Windows releases may include additional Git for Windows license files under licenses/git-for-windows/.");

        _txtLicenses.Text = sb.ToString();

        if (_txtHint != null)
        {
            string rootInfo = string.IsNullOrWhiteSpace(_root) ? "(no CBETA root loaded)" : _root!;
            _txtHint.Text = $"CBETA root: {rootInfo}";
        }
    }

    private static string SafeReadCedictHeader(string dictPath)
    {
        try
        {
            if (!File.Exists(dictPath))
                return $"Dictionary file not found: {dictPath}";

            return CedictLicenseService.ReadCedictHeader(dictPath) ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"Failed to read CC-CEDICT header from: {dictPath}{Environment.NewLine}{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static string SafeReadTextFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return string.Empty;

            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            // Fallback in case encoding differs
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                return $"Failed to read file: {path}{Environment.NewLine}{ex.GetType().Name}: {ex.Message}";
            }
        }
    }

    private static string BuildGitBundleSummary(string gitNoticeDir)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return "This is not a Windows build. Git for Windows is not bundled on this platform.";
            }

            if (!Directory.Exists(gitNoticeDir))
            {
                return "No bundled Git for Windows notice folder found. Expected path:\n" + gitNoticeDir;
            }

            var files = Directory.GetFiles(gitNoticeDir, "*", SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            var sb = new StringBuilder();
            sb.AppendLine("This Windows build bundles Git for Windows (PortableGit) to provide an embedded Git executable.");
            sb.AppendLine("Git is licensed under GPLv2. Additional bundled components may have their own licenses.");
            sb.AppendLine("Bundled Git notice files found in:");
            sb.AppendLine(gitNoticeDir);
            sb.AppendLine();

            if (files.Length == 0)
            {
                sb.AppendLine("(No files found in the Git notices folder.)");
            }
            else
            {
                sb.AppendLine("Files:");
                foreach (var f in files)
                    sb.AppendLine(" - " + Path.GetFileName(f));
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Failed to inspect bundled Git notices.{Environment.NewLine}{ex.GetType().Name}: {ex.Message}";
        }
    }
}