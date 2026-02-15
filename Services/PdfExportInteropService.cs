using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class PdfExportInteropService
{
    private static bool _loadAttempted;

    [DllImport("cbeta_gui_dll.dll", EntryPoint = "generate_pdf_output_ffi", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int GeneratePdfOutputFfi(
        [In] string[] chineseSections,
        [In] string[] englishSections,
        UIntPtr sectionCount,
        string outputPath,
        int layoutMode,
        float lineSpacing,
        float trackingChinese,
        float trackingEnglish,
        float paragraphSpacing);

    public bool TryGeneratePdf(
        IReadOnlyList<string> chineseSections,
        IReadOnlyList<string> englishSections,
        string outputPath,
        AppConfig config,
        out string error)
    {
        error = string.Empty;

        if (chineseSections.Count != englishSections.Count)
        {
            error = "Chinese and English section counts must match.";
            return false;
        }

        if (chineseSections.Count == 0)
        {
            error = "No content available to export.";
            return false;
        }

        EnsureNativeDllLoaded();

        try
        {
            var result = GeneratePdfOutputFfi(
                chineseSections is string[] ca ? ca : new List<string>(chineseSections).ToArray(),
                englishSections is string[] ea ? ea : new List<string>(englishSections).ToArray(),
                (UIntPtr)chineseSections.Count,
                outputPath,
                (int)config.PdfLayoutMode,
                config.PdfLineSpacing,
                config.PdfTrackingChinese,
                config.PdfTrackingEnglish,
                config.PdfParagraphSpacing);

            if (result == 0)
                return true;

            error = "Native PDF generator returned an error.";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Native PDF call failed: {ex.Message}";
            return false;
        }
    }

    private static void EnsureNativeDllLoaded()
    {
        if (_loadAttempted)
            return;

        _loadAttempted = true;

        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CBETA_GUI_DLL_PATH"),
            Path.Combine(AppContext.BaseDirectory, "cbeta_gui_dll.dll"),
            @"D:\Rust-projects\MT15-model\cbeta-gui-dll\target\release\cbeta_gui_dll.dll",
            "/mnt/d/Rust-projects/MT15-model/cbeta-gui-dll/target/release/cbeta_gui_dll.dll"
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
                continue;

            if (NativeLibrary.TryLoad(candidate, out _))
                return;
        }
    }
}
