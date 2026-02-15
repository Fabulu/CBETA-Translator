using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class PdfExportInteropService
{
    private static bool _loadAttempted;
    private static IntPtr _dllHandle;
    private static GeneratePdfOutputFfiDelegate? _ffi;
    private static string _loadedDllPath = "(not loaded)";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GeneratePdfOutputFfiDelegate(
        IntPtr chineseSections,
        IntPtr englishSections,
        UIntPtr sectionCount,
        IntPtr outputPath,
        int layoutMode,
        float lineSpacing,
        float trackingChinese,
        float trackingEnglish,
        float paragraphSpacing,
        int autoScaleFonts,
        float targetFillRatio,
        float minFontSize,
        float maxFontSize,
        int lockBilingualFontSize);

    public static string NativeDllDiagnostics => _loadedDllPath;

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

        if (!EnsureNativeDllLoaded(out var loadErr))
        {
            error = "Native PDF DLL load failed: " + loadErr;
            return false;
        }

        IntPtr[]? zhPtrs = null;
        IntPtr[]? enPtrs = null;
        IntPtr zhArray = IntPtr.Zero;
        IntPtr enArray = IntPtr.Zero;
        IntPtr outputPathPtr = IntPtr.Zero;

        try
        {
            int count = chineseSections.Count;
            zhPtrs = new IntPtr[count];
            enPtrs = new IntPtr[count];

            for (int i = 0; i < count; i++)
            {
                zhPtrs[i] = Marshal.StringToCoTaskMemUTF8(chineseSections[i] ?? string.Empty);
                enPtrs[i] = Marshal.StringToCoTaskMemUTF8(englishSections[i] ?? string.Empty);
            }

            zhArray = Marshal.AllocHGlobal(IntPtr.Size * count);
            enArray = Marshal.AllocHGlobal(IntPtr.Size * count);
            for (int i = 0; i < count; i++)
            {
                Marshal.WriteIntPtr(zhArray, i * IntPtr.Size, zhPtrs[i]);
                Marshal.WriteIntPtr(enArray, i * IntPtr.Size, enPtrs[i]);
            }

            outputPathPtr = Marshal.StringToCoTaskMemUTF8(outputPath ?? string.Empty);

            if (string.Equals(Environment.GetEnvironmentVariable("CBETA_PDF_DEBUG_DUMP"), "1", StringComparison.Ordinal))
            {
                try
                {
                    var dumpPath = outputPath + ".input.txt";
                    using var sw = new StreamWriter(dumpPath, false);
                    sw.WriteLine("DLL=" + _loadedDllPath);
                    sw.WriteLine("COUNT=" + count);
                    for (int i = 0; i < count; i++)
                    {
                        sw.WriteLine($"--- SECTION {i + 1} ---");
                        sw.WriteLine("ZH: " + (chineseSections[i] ?? ""));
                        sw.WriteLine("EN: " + (englishSections[i] ?? ""));
                    }
                }
                catch
                {
                    // ignore debug dump errors
                }
            }

            var result = _ffi!(
                zhArray,
                enArray,
                (UIntPtr)count,
                outputPathPtr,
                (int)config.PdfLayoutMode,
                config.PdfLineSpacing,
                config.PdfTrackingChinese,
                config.PdfTrackingEnglish,
                config.PdfParagraphSpacing,
                config.PdfAutoScaleFonts ? 1 : 0,
                config.PdfTargetFillRatio,
                config.PdfMinFontSize,
                config.PdfMaxFontSize,
                config.PdfLockBilingualFontSize ? 1 : 0);

            if (result == 0)
                return true;

            error = $"Native PDF generator returned an error. DLL={_loadedDllPath}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Native PDF call failed: {ex.Message}. DLL={_loadedDllPath}";
            return false;
        }
        finally
        {
            if (zhPtrs != null)
            {
                for (int i = 0; i < zhPtrs.Length; i++)
                {
                    if (zhPtrs[i] != IntPtr.Zero) Marshal.FreeCoTaskMem(zhPtrs[i]);
                }
            }

            if (enPtrs != null)
            {
                for (int i = 0; i < enPtrs.Length; i++)
                {
                    if (enPtrs[i] != IntPtr.Zero) Marshal.FreeCoTaskMem(enPtrs[i]);
                }
            }

            if (zhArray != IntPtr.Zero) Marshal.FreeHGlobal(zhArray);
            if (enArray != IntPtr.Zero) Marshal.FreeHGlobal(enArray);
            if (outputPathPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(outputPathPtr);
        }
    }

    private static bool EnsureNativeDllLoaded(out string error)
    {
        error = string.Empty;
        if (_loadAttempted)
            return _ffi != null;

        _loadAttempted = true;

        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CBETA_GUI_DLL_PATH"),
            @"D:\Rust-projects\MT15-model\cbeta-gui-dll\target\release\cbeta_gui_dll.dll",
            "/mnt/d/Rust-projects/MT15-model/cbeta-gui-dll/target/release/cbeta_gui_dll.dll",
            Path.Combine(AppContext.BaseDirectory, "cbeta_gui_dll.dll"),
        };

        var loadErrors = new List<string>();
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
                continue;

            try
            {
                _dllHandle = NativeLibrary.Load(candidate);
                if (_dllHandle == IntPtr.Zero)
                {
                    loadErrors.Add($"{candidate}: load returned zero handle");
                    continue;
                }

                var symbol = NativeLibrary.GetExport(_dllHandle, "generate_pdf_output_ffi");
                _ffi = Marshal.GetDelegateForFunctionPointer<GeneratePdfOutputFfiDelegate>(symbol);
                _loadedDllPath = candidate;
                return true;
            }
            catch (Exception ex)
            {
                loadErrors.Add($"{candidate}: {ex.Message}");
            }
        }

        _loadedDllPath = "(load failed)";
        error = loadErrors.Count == 0
            ? "No candidate DLL found."
            : string.Join(" | ", loadErrors);
        return false;
    }
}
