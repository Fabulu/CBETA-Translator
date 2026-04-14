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

        // CC BY (not SA, not NC): full freedom with attribution note
        if (IsMatch(src, "CC-BY-4.0", "CC BY 4.0", "CC-BY-") && !ContainsAny(src, "SA", "NC", "ND"))
            return new List<LicenseOption>(All);

        // CC BY-SA: sticky — only CC BY-SA
        if (IsMatch(src, "CC-BY-SA", "CC BY-SA"))
            return new List<LicenseOption> { CcBySa, AllRightsReserved };

        // CC BY-NC-SA: fully sticky
        if (IsMatch(src, "CC-BY-NC-SA", "CC BY-NC-SA"))
            return new List<LicenseOption> { CcByNcSa, AllRightsReserved };

        // CC BY-NC: can add restrictions
        if (IsMatch(src, "CC-BY-NC", "CC BY-NC"))
            return new List<LicenseOption> { CcByNc, CcByNcSa, CcByNcNd, AllRightsReserved };

        // Unknown: safe default
        return new List<LicenseOption> { AllRightsReserved };
    }

    /// <summary>
    /// Returns the default license for a given source context.
    /// CBETA → cbeta-nc, OpenZen CC0/PD → null (prompt user), everything else → null.
    /// </summary>
    public static LicenseOption? GetDefault(string? sourceLicense, CorpusKind corpus)
    {
        if (corpus == CorpusKind.Cbeta) return CbetaNc;
        return null; // prompt user to choose
    }

    private static bool IsMatch(string source, params string[] patterns) =>
        patterns.Any(p => source.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static bool ContainsAny(string source, params string[] terms) =>
        terms.Any(t => source.Contains(t, StringComparison.OrdinalIgnoreCase));
}
