// Models/LicenseCatalog.cs
// Hardcoded catalog of all supported translation licenses with human-readable descriptions.
// The compatibility matrix gates which licenses a translator can choose based on the source license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadZen.App.Models;

public sealed record LicenseOption(
    string Id,
    string DisplayName,
    string Tooltip,
    string Url,
    bool CommercialOk,
    bool AttributionRequired,
    bool ShareAlikeRequired)
{
    /// <summary>Badge color category for UI.</summary>
    public string BadgeColor => CommercialOk ? "green" : Id == "all-rights-reserved" ? "gray" : "yellow";
}

public static class LicenseCatalog
{
    // ── Full catalog, most restrictive → most permissive ─────────────

    public static readonly LicenseOption AllRightsReserved = new(
        "all-rights-reserved",
        "All Rights Reserved",
        "You keep full copyright. Others can read your translation in ReadZen but cannot copy or redistribute it without your permission.",
        "",
        CommercialOk: false, AttributionRequired: false, ShareAlikeRequired: false);

    public static readonly LicenseOption CbetaNc = new(
        "cbeta-nc",
        "CBETA Non-Commercial",
        "Inherited from CBETA. Anyone can read and share for non-commercial purposes. Required for all CBETA-based translations.",
        "https://www.cbeta.org/copyright.php",
        CommercialOk: false, AttributionRequired: true, ShareAlikeRequired: false);

    public static readonly LicenseOption CcByNcNd = new(
        "CC-BY-NC-ND-4.0",
        "CC BY-NC-ND 4.0",
        "Others can share your translation for non-commercial purposes, but they can't change it or build on it. They must credit you.",
        "https://creativecommons.org/licenses/by-nc-nd/4.0/",
        CommercialOk: false, AttributionRequired: true, ShareAlikeRequired: false);

    public static readonly LicenseOption CcByNcSa = new(
        "CC-BY-NC-SA-4.0",
        "CC BY-NC-SA 4.0",
        "Others can remix and share for non-commercial purposes, but their versions must use this same license. They must credit you.",
        "https://creativecommons.org/licenses/by-nc-sa/4.0/",
        CommercialOk: false, AttributionRequired: true, ShareAlikeRequired: true);

    public static readonly LicenseOption CcByNc = new(
        "CC-BY-NC-4.0",
        "CC BY-NC 4.0",
        "Others can remix and share for non-commercial purposes. They must credit you, but can choose their own license for their changes.",
        "https://creativecommons.org/licenses/by-nc/4.0/",
        CommercialOk: false, AttributionRequired: true, ShareAlikeRequired: false);

    public static readonly LicenseOption CcBySa = new(
        "CC-BY-SA-4.0",
        "CC BY-SA 4.0",
        "Anyone can use, remix, and share \u2014 even commercially \u2014 but their versions must use this same license. They must credit you.",
        "https://creativecommons.org/licenses/by-sa/4.0/",
        CommercialOk: true, AttributionRequired: true, ShareAlikeRequired: true);

    public static readonly LicenseOption CcBy = new(
        "CC-BY-4.0",
        "CC BY 4.0",
        "Anyone can use, remix, and share \u2014 even commercially. They just have to credit you.",
        "https://creativecommons.org/licenses/by/4.0/",
        CommercialOk: true, AttributionRequired: true, ShareAlikeRequired: false);

    public static readonly LicenseOption Mit = new(
        "MIT",
        "MIT License",
        "Basically do whatever you want with it, just keep the copyright notice. Common in software, works for text too.",
        "https://opensource.org/licenses/MIT",
        CommercialOk: true, AttributionRequired: true, ShareAlikeRequired: false);

    public static readonly LicenseOption Cc0 = new(
        "CC0-1.0",
        "CC0 (Public Domain)",
        "You give up all rights. Anyone can do anything with your translation. No credit needed. Maximum freedom for everyone.",
        "https://creativecommons.org/publicdomain/zero/1.0/",
        CommercialOk: true, AttributionRequired: false, ShareAlikeRequired: false);

    public static readonly LicenseOption Unlicense = new(
        "Unlicense",
        "Unlicense (Public Domain)",
        "Equivalent to CC0 but with software-style legal text. Anyone can do anything with your translation.",
        "https://unlicense.org/",
        CommercialOk: true, AttributionRequired: false, ShareAlikeRequired: false);

    /// <summary>All licenses in display order (most restrictive → most permissive).</summary>
    public static readonly LicenseOption[] All =
    {
        AllRightsReserved,
        CbetaNc,
        CcByNcNd,
        CcByNcSa,
        CcByNc,
        CcBySa,
        CcBy,
        Mit,
        Cc0,
        Unlicense,
    };

    private static readonly Dictionary<string, LicenseOption> ById =
        All.ToDictionary(l => l.Id, StringComparer.OrdinalIgnoreCase);

    public static LicenseOption? Find(string id) =>
        ById.TryGetValue(id, out var opt) ? opt : null;

    // ── Compatibility matrix ─────────────────────────────────────────

    /// <summary>
    /// Returns the licenses a translator can choose for their translation,
    /// given the source text's license. Source license is matched by known
    /// keywords — unknown sources default to All Rights Reserved only.
    /// </summary>
    public static List<LicenseOption> GetCompatible(string? sourceLicense, CorpusKind corpus)
    {
        // CBETA hard lock: non-commercial only
        if (corpus == CorpusKind.Cbeta)
            return new List<LicenseOption> { CbetaNc, CcByNc, CcByNcSa, CcByNcNd, AllRightsReserved };

        if (string.IsNullOrWhiteSpace(sourceLicense))
            return new List<LicenseOption> { AllRightsReserved };

        var src = sourceLicense.Trim();

        // CC0 / PD: full freedom
        if (IsMatch(src, "CC0", "PD-old", "public domain", "Public Domain", "Unlicense"))
            return new List<LicenseOption>(All);

        // Order matters: check most specific (most restrictive) first to avoid false matches.
        // CC-BY-NC-SA must be checked before CC-BY-NC, CC-BY-SA, and CC-BY.

        // CC BY-NC-SA: fully sticky (most restrictive CC with derivatives)
        if (IsMatch(src, "CC-BY-NC-SA", "CC BY-NC-SA"))
            return new List<LicenseOption> { CcByNcSa, AllRightsReserved };

        // CC BY-NC-ND: no derivatives allowed
        if (IsMatch(src, "CC-BY-NC-ND", "CC BY-NC-ND"))
            return new List<LicenseOption> { CcByNcNd, AllRightsReserved };

        // CC BY-NC: can add restrictions but must stay NC
        if (IsMatch(src, "CC-BY-NC", "CC BY-NC"))
            return new List<LicenseOption> { CcByNc, CcByNcSa, CcByNcNd, AllRightsReserved };

        // CC BY-SA: sticky copyleft (must check before plain CC-BY)
        if (IsMatch(src, "CC-BY-SA", "CC BY-SA"))
            return new List<LicenseOption> { CcBySa, AllRightsReserved };

        // CC BY: full freedom with attribution
        if (IsMatch(src, "CC-BY-4.0", "CC BY 4.0", "CC-BY-") && !ContainsAny(src, "SA", "NC", "ND"))
            return new List<LicenseOption>(All);

        // Unknown: safe default
        return new List<LicenseOption> { AllRightsReserved };
    }

    /// <summary>
    /// Returns the default license for a given source context.
    /// Default is always "inherit from source" — the same license as the source text.
    /// This is the safest default and requires no active choice.
    /// </summary>
    public static LicenseOption? GetDefault(string? sourceLicense, CorpusKind corpus)
    {
        if (corpus == CorpusKind.Cbeta) return CbetaNc;
        if (string.IsNullOrWhiteSpace(sourceLicense)) return null;

        var src = sourceLicense.Trim();

        // Map source license to the matching catalog entry (most specific first)
        if (IsMatch(src, "CC0", "PD-old", "public domain", "Unlicense")) return Cc0;
        if (IsMatch(src, "CC-BY-NC-SA", "CC BY-NC-SA")) return CcByNcSa;
        if (IsMatch(src, "CC-BY-NC-ND", "CC BY-NC-ND")) return CcByNcNd;
        if (IsMatch(src, "CC-BY-NC", "CC BY-NC")) return CcByNc;
        if (IsMatch(src, "CC-BY-SA", "CC BY-SA")) return CcBySa;
        if (IsMatch(src, "CC-BY-4.0", "CC BY 4.0", "CC-BY-")) return CcBy;

        return null;
    }

    /// <summary>
    /// Returns a human-readable explanation of what the inherited default means for the translator.
    /// Shown as a sync warning when the user hasn't explicitly chosen a license.
    /// </summary>
    public static string GetDefaultExplanation(LicenseOption defaultLicense) => defaultLicense.Id switch
    {
        "cbeta-nc" =>
            "Your translation inherits CBETA's non-commercial license. Anyone can read and share it for non-commercial purposes, but not sell it. You can change this to a more specific NC license if you want.",
        "CC0-1.0" =>
            "The source is public domain (CC0), so your translation defaults to CC0 too \u2014 anyone can do anything with it. If you want to keep some rights, choose a different license before syncing.",
        "CC-BY-SA-4.0" =>
            "The source uses CC BY-SA (share-alike), so your translation must use the same license. Others can use it commercially but must share their changes under the same terms and credit you.",
        "CC-BY-NC-SA-4.0" =>
            "The source uses CC BY-NC-SA, so your translation must use the same license. Others can share non-commercially under the same terms. They must credit you.",
        "CC-BY-NC-ND-4.0" =>
            "The source uses CC BY-NC-ND. Others can share your translation non-commercially but cannot modify it. They must credit you.",
        "CC-BY-NC-4.0" =>
            "The source uses CC BY-NC. Others can remix and share non-commercially. They must credit you.",
        "CC-BY-4.0" =>
            "The source uses CC BY, so your translation defaults to CC BY too \u2014 anyone can use it, even commercially, as long as they credit you. You can choose a different license if you want.",
        _ =>
            $"Your translation inherits the source license ({defaultLicense.DisplayName}). You can change this before syncing.",
    };

    private static bool IsMatch(string source, params string[] patterns) =>
        patterns.Any(p => source.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsAny(string source, params string[] terms) =>
        terms.Any(t => source.Contains(t, StringComparison.OrdinalIgnoreCase));
}
