// Infrastructure/LineageGraphBuilder.cs
//
// Pure, DOM-free port of the SPA's lineage normalization module
// (ZenLinkPage/lib/lineage-data.js). Turns the rich master roster
// (609 records, IReadOnlyList<LineageMasterRecord>) into a clean
// { nodes, edges, sources, report } graph the forthcoming tidy-forest
// lineage control (plan PR-L4) can draw without ever lying about the evidence.
//
// The governing rule of the whole chart: INK MUST BE EARNED. Missing data must
// render as the weakest thing, never as confident certainty. This module is the
// first line of that guarantee (validation + honest stubs); the renderer's
// attestation fail-safe (ATT_STYLES[att] ?? D) is the second.
//
// Like RowGridBuilder, this class is deliberately PURE — no Avalonia, no I/O, no
// DOM — so it can run off the UI thread and be unit-tested headlessly. It is
// fail-soft: a malformed record never throws; it degrades to a stub / honest
// omission recorded in the report.
//
// Porting notes (JS -> C#), kept faithful on purpose:
//  * JS `x || 0` treats 0 AND null/undefined as "missing"; repYear/formatDates
//    preserve that intent (a death year of 0 is NOT a real year).
//  * JS `Map` uses object (reference) identity for node keys; the ported
//    Dictionary<LineageNode,int> in ComputeSpine relies on reference equality.
//  * `normalizeSchool` lowercases via ToLowerInvariant and matches with
//    CultureInvariant regexes (InvariantGlobalization=true — no locale ops).
//  * Node/edge/source ORDER is list-append deterministic, so two builds of the
//    same input yield identical node/edge sets.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// One graph node — either a real master (<see cref="IsSource"/> == false) or a
/// synthesized book/source pseudo-node (<see cref="IsSource"/> == true). Mutable:
/// the build passes fill <see cref="ParentEdge"/>/<see cref="ChildEdges"/>/
/// <see cref="IsRoot"/>/<see cref="Stub"/>/<see cref="Spine"/>, and the layout
/// port (PR-L3) later fills the layout scaffold fields.
/// </summary>
public sealed class LineageNode
{
    // ---- identity / content ----
    public string Id { get; set; } = "";
    public string Primary { get; set; } = "";
    public IReadOnlyList<string> Names { get; set; } = Array.Empty<string>();
    public string Cjk { get; set; } = "";
    public IReadOnlyList<string> Aliases { get; set; } = Array.Empty<string>();
    public string SchoolRaw { get; set; } = "";
    public string SchoolKey { get; set; } = "";
    public bool Korean { get; set; }
    public bool PreChan { get; set; }

    /// <summary>Validated attestation grade (A/B/C/D) or <c>null</c> when missing
    /// or invalid — the JS <c>undefined</c>. The renderer fail-safes null to D.</summary>
    public string? Attestation { get; set; }

    public string Transmission { get; set; } = "direct";
    public string Teacher { get; set; } = "";
    public string TeacherKey { get; set; } = "";
    public bool TeacherDangling { get; set; }
    public IReadOnlyList<LineageBookTransmission> BookTransmissions { get; set; }
        = Array.Empty<LineageBookTransmission>();
    public bool Contested { get; set; }
    public LineageContestedBy? ContestedBy { get; set; }
    public string EdgeNote { get; set; } = "";
    public string Bio { get; set; } = "";
    public IReadOnlyList<LineageStele> Steles { get; set; } = Array.Empty<LineageStele>();
    public LineageProvenance? Provenance { get; set; }
    public IReadOnlyList<LineageLink> Links { get; set; } = Array.Empty<LineageLink>();
    public int Birth { get; set; }
    public int Death { get; set; }
    public int Floruit { get; set; }
    public string Region { get; set; } = "";
    public bool DatesConjectural { get; set; }
    public bool DatesConflict { get; set; }
    public string DateNote { get; set; } = "";
    public string DatesText { get; set; } = "";

    /// <summary>Representative year for the chronological hint. NEVER invented:
    /// death, else birth+65, else floruit, else <c>null</c>.</summary>
    public int? Year { get; set; }

    public bool IsSource { get; set; }

    // ---- source-pseudo-node-only fields (unused on real masters) ----
    public string SourceTitle { get; set; } = "";
    public string SourceTitleEn { get; set; } = "";
    public string SourceAuthor { get; set; } = "";
    public string SourceDesc { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public bool SourceInCorpus { get; set; }

    // ---- graph state (filled by the build passes) ----
    public LineageEdge? ParentEdge { get; set; }
    public List<LineageEdge> ChildEdges { get; } = new();
    /// <summary>For a book-with-no-teacher master: the edges to its synthesized book sources.</summary>
    public List<LineageEdge>? BookEdges { get; set; }
    public bool IsRoot { get; set; }
    public bool Stub { get; set; }
    public string? StubLabel { get; set; }
    public bool Spine { get; set; }
    public int Descendants { get; set; }

    // ---- layout scaffold (filled by the layout port, PR-L3) ----
    public int Layer { get; set; } = -1;
    public double X { get; set; }
    public double Y { get; set; }
    public int Order { get; set; }
}

/// <summary>A directed teacher-&gt;student (or book-&gt;master) edge.</summary>
public sealed class LineageEdge
{
    public LineageNode From { get; set; } = null!;
    public LineageNode To { get; set; } = null!;
    public string? Attestation { get; set; }
    public string Transmission { get; set; } = "direct";
    /// <summary>The rival hypothesis when the edge is contested, else null.</summary>
    public LineageContestedBy? Contested { get; set; }
    public string EdgeNote { get; set; } = "";
    /// <summary>"tree" for a teacher edge, "book" for a synthesized source edge.</summary>
    public string Kind { get; set; } = "tree";
}

/// <summary>Honest tallies of what the build accepted, quarantined, or could not resolve.</summary>
public sealed class LineageReport
{
    public int Masters { get; set; }
    public int Edges { get; set; }
    public int Roots { get; set; }
    public int Dangling { get; set; }
    public int BookSources { get; set; }
    public int Contested { get; set; }
    public List<string> BadAttestation { get; } = new();
    public List<string> UnknownSchool { get; } = new();
    public List<string> UnknownTransmission { get; } = new();
    public List<string> UnresolvedTeacherKey { get; } = new();
}

/// <summary>The immutable-ish output of <see cref="LineageGraphBuilder.Build"/>.</summary>
public sealed class LineageGraph
{
    public IReadOnlyList<LineageNode> Nodes { get; init; } = Array.Empty<LineageNode>();
    public IReadOnlyList<LineageEdge> Edges { get; init; } = Array.Empty<LineageEdge>();
    public IReadOnlyList<LineageNode> Sources { get; init; } = Array.Empty<LineageNode>();
    public LineageReport Report { get; init; } = new();
    /// <summary>node.Id -&gt; node (masters plus synthesized sources).</summary>
    public IReadOnlyDictionary<string, LineageNode> ById { get; init; }
        = new Dictionary<string, LineageNode>();
    /// <summary>every name/alias -&gt; node (first wins).</summary>
    public IReadOnlyDictionary<string, LineageNode> ByName { get; init; }
        = new Dictionary<string, LineageNode>();
}

/// <summary>
/// Pure port of <c>buildLineage(masters)</c> and its helpers. Static, allocation-
/// modest, deterministic, headless-testable.
/// </summary>
public static class LineageGraphBuilder
{
    // ── School hues / labels (constants; parity with SPA SCHOOL_HUES/LABELS) ──
    // The 12 canonical school keys, in the SPA's declaration order.
    public static readonly IReadOnlyDictionary<string, int?> SchoolHues = new Dictionary<string, int?>(StringComparer.Ordinal)
    {
        ["linji"] = 8,
        ["caodong"] = 222,
        ["yunmen"] = 275,
        ["fayan"] = 168,
        ["guiyang"] = 45,
        ["hongzhou"] = 28,
        ["shitou"] = 195,
        ["niutou"] = 115,
        ["heze"] = 330,
        ["korean-seon"] = 160,
        ["early-chan"] = 38,
        ["pre-chan"] = null,   // achromatic
        ["other"] = null,
    };

    public static readonly IReadOnlyDictionary<string, string> SchoolLabels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["linji"] = "Linji 臨濟",
        ["caodong"] = "Caodong 曹洞",
        ["yunmen"] = "Yunmen 雲門",
        ["fayan"] = "Fayan 法眼",
        ["guiyang"] = "Guiyang 溈仰",
        ["hongzhou"] = "Hongzhou 洪州",
        ["shitou"] = "Shitou 石頭",
        ["niutou"] = "Niutou 牛頭",
        ["heze"] = "Heze 荷澤",
        ["korean-seon"] = "Korean Seon 禪",
        ["early-chan"] = "Early Chan",
        ["pre-chan"] = "Pre-Chan",
        ["other"] = "Other",
    };

    // Book-master-with-no-teacher fallback: a generic bilingual sutra node,
    // never hanja-only. (Parity: BOOK_FALLBACK.)
    private static readonly LineageBookTransmission BookFallback = new()
    {
        Id = "book:unknown",
        TitleEn = "Sutra",
        TitleHanja = "經",
    };

    // ~15 curated school-founders / pivots that anchor the cold "spine" view.
    private static readonly HashSet<string> SpineFounders = new(StringComparer.Ordinal)
    {
        "Bodhidharma", "Huike", "Sengcan", "Daoxin", "Hongren", "Huineng",
        "Shenxiu", "Qingyuan Xingsi", "Nanyue Huairang", "Mazu Daoyi",
        "Baizhang Huaihai", "Shitou Xiqian", "Linji Yixuan", "Dongshan Liangjie",
        "Caoshan Benji", "Yunmen Wenyan", "Fayan Wenyi", "Guishan Lingyou",
        "Deshan Xuanjian", "Xuefeng Yicun", "Huangbo Xiyun", "Nanquan Puyuan",
        "Zhaozhou Congshen", "Dahui Zonggao", "Yangqi Fanghui", "Huanglong Huinan",
        "Jinul", "Taego Bou", "Doui", "Beomil",
    };

    // ── Regexes (CultureInvariant; input is already ToLowerInvariant, so no
    //    IgnoreCase — the JS patterns are lowercase and rely on the lowered string). ──
    private const RegexOptions Opt = RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly Regex ResPreChan = new(@"not chan|scholar-monk|pre-chan|kum[aā]raj[iī]va|4th-century|4th century", Opt);
    private static readonly Regex ReYunmen = new(@"yunmen|雲門|云门", Opt);
    private static readonly Regex ReFayan = new(@"fayan|法眼", Opt);
    private static readonly Regex ReGuiyang = new(@"guiyang|gui-?yang|溈仰|潙仰|沩仰|仰宗", Opt);
    private static readonly Regex ReCaodong = new(@"caodong|曹洞", Opt);
    private static readonly Regex ReLinji = new(@"linji|臨濟|临济|yangqi|楊岐|huanglong|黃龍|sanfeng|三峰", Opt);
    private static readonly Regex ReKorean = new(@"korean|seon|jogye|조계|海東|goryeo|joseon|silla", Opt);
    private static readonly Regex ReHongzhou = new(@"hongzhou|洪州", Opt);
    private static readonly Regex ReShitou = new(@"shitou|石頭|石头|qingyuan|青原", Opt);
    private static readonly Regex ReNiutou = new(@"niutou|oxhead|牛頭|牛头", Opt);
    private static readonly Regex ReHeze = new(@"heze|荷澤|荷泽", Opt);
    private static readonly Regex ReEarlyChan = new(@"early chan|楞伽|東山|东山|lank[aā]vat[aā]ra|^chan$|禪宗|禅宗", Opt);

    private static readonly Regex ReKoreanRegion = new(@"korea|goryeo|koryo|joseon|choson|silla|고려|조선|신라|海東|해동", Opt);

    // CJK Ext-A (U+3400) through CJK Unified Ideographs (U+9FFF): the JS /[㐀-鿿]/.
    private static readonly Regex ReCjk = new(@"[㐀-鿿]", Opt);

    // Valid transmission markers (parity with the JS anchored alternation).
    private static readonly Regex ReValidTransmission = new(@"^(direct|book|disputed|none|遙嗣|代囑)$", Opt);

    // Attestation must be exactly one of A/B/C/D.
    private static readonly Regex ReValidAtt = new(@"^[ABCD]$", Opt);

    /// <summary>Normalize the free-text <c>school</c> string to one of 12 canonical keys.</summary>
    public static string NormalizeSchool(string? raw)
    {
        var s = (raw ?? "").ToLowerInvariant();
        if (s.Trim().Length == 0) return "other";
        if (ResPreChan.IsMatch(s)) return "pre-chan";
        if (ReYunmen.IsMatch(s)) return "yunmen";
        if (ReFayan.IsMatch(s)) return "fayan";
        if (ReGuiyang.IsMatch(s)) return "guiyang";
        if (ReCaodong.IsMatch(s)) return "caodong";
        if (ReLinji.IsMatch(s)) return "linji";
        if (ReKorean.IsMatch(s)) return "korean-seon";
        if (ReHongzhou.IsMatch(s)) return "hongzhou";
        if (ReShitou.IsMatch(s)) return "shitou";
        if (ReNiutou.IsMatch(s)) return "niutou";
        if (ReHeze.IsMatch(s)) return "heze";
        if (ReEarlyChan.IsMatch(s)) return "early-chan";
        return "other";
    }

    /// <summary>Build the lineage graph from the raw roster. Fail-soft; never throws on bad records.</summary>
    public static LineageGraph Build(IReadOnlyList<LineageMasterRecord> masters)
    {
        masters ??= Array.Empty<LineageMasterRecord>();

        var nodes = new List<LineageNode>();
        var byName = new Dictionary<string, LineageNode>(StringComparer.Ordinal); // every name/alias -> node (first wins)
        var report = new LineageReport();

        // ── Pass 1: nodes + name index ──
        foreach (var m in masters)
        {
            var names = (m.Names ?? new List<string>()).Where(n => !string.IsNullOrEmpty(n)).ToList();
            if (names.Count == 0) continue;
            var primary = names[0];

            var schoolKey = NormalizeSchool(m.School);
            if (schoolKey == "other") report.UnknownSchool.Add(primary);

            var att = !string.IsNullOrEmpty(m.Attestation) && ReValidAtt.IsMatch(m.Attestation!)
                ? m.Attestation
                : null;
            if (!string.IsNullOrEmpty(m.Attestation) && att == null)
                report.BadAttestation.Add(primary + ":" + m.Attestation);

            var transmission = string.IsNullOrEmpty(m.Transmission) ? "direct" : m.Transmission!;
            if (!ReValidTransmission.IsMatch(transmission))
                report.UnknownTransmission.Add(primary + ":" + transmission);

            var node = new LineageNode
            {
                Id = primary,
                Primary = primary,
                Names = names,
                Cjk = FirstCjk(names),
                Aliases = names.Skip(1).ToList(),
                SchoolRaw = m.School ?? "",
                SchoolKey = schoolKey,
                Korean = IsKoreanNode(m, schoolKey),
                PreChan = schoolKey == "pre-chan",
                Attestation = att,
                Transmission = transmission,
                Teacher = m.Teacher ?? "",
                TeacherKey = m.TeacherKey ?? "",
                TeacherDangling = m.TeacherDangling,
                BookTransmissions = m.BookTransmissions ?? new List<LineageBookTransmission>(),
                Contested = m.Contested,
                ContestedBy = m.ContestedBy,
                EdgeNote = m.EdgeNote ?? "",
                Bio = !string.IsNullOrEmpty(m.Bio) ? m.Bio! : (m.Notes ?? ""),
                Steles = m.Steles ?? new List<LineageStele>(),
                Provenance = m.Provenance,
                Links = m.Links ?? new List<LineageLink>(),
                Birth = m.Birth ?? 0,
                Death = m.Death ?? 0,
                Floruit = m.Floruit ?? 0,
                Region = m.Region ?? "",
                DatesConjectural = m.DatesConjectural,
                DatesConflict = m.DatesConflict,
                DateNote = m.DateNote ?? "",
                DatesText = FormatDates(m),
                Year = RepYear(m),
                IsSource = false,
            };
            nodes.Add(node);
            report.Masters++;
            foreach (var nm in names)
                if (!byName.ContainsKey(nm)) byName[nm] = node;
        }

        var byId = new Dictionary<string, LineageNode>(StringComparer.Ordinal);
        foreach (var n in nodes)
            byId[n.Id] = n; // parity with `new Map(nodes.map(n => [n.id, n]))` — last wins on a duplicate id

        // ── Pass 2: edges. teacher_key is the canonical parent-NODE name — use it. ──
        var edges = new List<LineageEdge>();
        var sources = new List<LineageNode>();

        LineageEdge AddEdge(LineageNode from, LineageNode to, string kind, string? attOverride, string? transOverride)
        {
            var e = new LineageEdge
            {
                From = from,
                To = to,
                Attestation = attOverride ?? to.Attestation,
                Transmission = transOverride ?? to.Transmission,
                Contested = to.Contested ? to.ContestedBy : null,
                EdgeNote = to.EdgeNote,
                Kind = kind,
            };
            edges.Add(e);
            to.ParentEdge = e;
            from.ChildEdges.Add(e);
            return e;
        }

        // Snapshot the master nodes: the JS for-of walks a growing array but every
        // appended source is immediately skipped via `if (node.isSource) continue`,
        // so iterating the pre-loop master set is behaviorally identical (and avoids
        // mutating the collection we're iterating).
        var masterNodes = nodes.ToList();
        foreach (var node in masterNodes)
        {
            if (node.IsSource) continue; // (defensive; masterNodes has no sources)

            // Book-with-no-teacher: synthesize first-class source pseudo-nodes,
            // one per book (the Jinul case: three books, three nodes).
            if (node.Transmission == "book" && string.IsNullOrEmpty(node.TeacherKey)
                && string.IsNullOrEmpty(node.Teacher) && !node.IsSource)
            {
                var books = node.BookTransmissions.Count > 0
                    ? node.BookTransmissions
                    : new List<LineageBookTransmission> { BookFallback };
                node.BookEdges = new List<LineageEdge>();
                foreach (var b in books)
                {
                    var titleEn = b.TitleEn ?? "";
                    var titleCjk = b.TitleHanja ?? "";
                    var src = new LineageNode
                    {
                        Id = "__src__" + node.Id + "__" + Or(b.Id, Or(titleCjk, titleEn)),
                        Primary = Or(titleEn, titleCjk),
                        Names = new[] { titleEn, titleCjk }.Where(x => !string.IsNullOrEmpty(x)).ToList(),
                        Cjk = titleCjk,
                        Aliases = Array.Empty<string>(),
                        SchoolKey = "source",
                        Korean = false,
                        PreChan = false,
                        Attestation = null,
                        Transmission = "source",
                        IsSource = true,
                        SourceTitle = titleCjk,
                        SourceTitleEn = titleEn,
                        SourceAuthor = b.Author ?? "",
                        SourceDesc = b.Description ?? "",
                        SourcePath = b.Path ?? "",
                        SourceInCorpus = b.InCorpus,
                        DatesText = "",
                        Year = node.Year,
                    };
                    nodes.Add(src);
                    sources.Add(src);
                    byId[src.Id] = src;
                    foreach (var nm in src.Names)
                        if (!byName.ContainsKey(nm)) byName[nm] = src;
                    report.BookSources++;
                    node.BookEdges.Add(AddEdge(src, node, "book", node.Attestation, "book"));
                }
                node.ParentEdge = node.BookEdges[0]; // narrative parent: the first book
                continue;
            }

            if (!string.IsNullOrEmpty(node.TeacherKey))
            {
                if (byName.TryGetValue(node.TeacherKey, out var parent) && !ReferenceEquals(parent, node))
                {
                    AddEdge(parent, node, "tree", null, null);
                    if (node.Contested && node.ContestedBy != null) report.Contested++;
                    continue;
                }
                // Named a parent-key but it isn't in the corpus -> honest stub.
                report.UnresolvedTeacherKey.Add(node.Primary + " -> " + node.TeacherKey);
                node.Stub = true;
                node.StubLabel = !string.IsNullOrEmpty(node.Teacher) ? node.Teacher : node.TeacherKey;
                report.Dangling++;
                continue;
            }

            if (node.TeacherDangling && !string.IsNullOrEmpty(node.Teacher))
            {
                // Teacher named in the record, not (yet) in this corpus.
                node.Stub = true;
                node.StubLabel = node.Teacher;
                report.Dangling++;
                continue;
            }

            // Genuine root: nothing above it. (Bodhidharma simply begins.)
            node.IsRoot = true;
            report.Roots++;
        }

        // ── Pass 3: student back-edges — recover a few parents, never overriding one. ──
        foreach (var m in masters)
        {
            // JS reads m.names[0] directly (may be a falsy "" — byName never holds an
            // empty key, so an empty/absent first name resolves to null/no-parent).
            var key = (m.Names != null && m.Names.Count > 0) ? m.Names[0] : null;
            if (string.IsNullOrEmpty(key) || !byName.TryGetValue(key, out var parent)) continue;
            if (m.Students == null) continue;
            foreach (var sName in m.Students)
            {
                if (string.IsNullOrEmpty(sName) || !byName.TryGetValue(sName, out var child)) continue;
                if (ReferenceEquals(child, parent)) continue;
                if (child.ParentEdge != null || child.Stub) continue; // never override
                // guard against a trivial cycle (parent already a descendant path)
                if (child.ChildEdges.Any(e => ReferenceEquals(e.To, parent))) continue;
                child.IsRoot = false;
                report.Roots--;
                AddEdge(parent, child, "tree", null, null);
            }
        }

        report.Edges = edges.Count;

        // Spine set: descendants>=8 OR contested OR founder OR korean; + ancestor closure.
        ComputeSpine(nodes, edges);

        return new LineageGraph
        {
            Nodes = nodes,
            Edges = edges,
            Sources = sources,
            Report = report,
            ById = byId,
            ByName = byName,
        };
    }

    /// <summary>Mark <c>node.Spine=true</c> for the cold default view.</summary>
    private static void ComputeSpine(List<LineageNode> nodes, List<LineageEdge> edges)
    {
        // descendant counts (memoized; the pre-seed 0 also guards against cycles)
        var desc = new Dictionary<LineageNode, int>(); // reference-equality keys (parity with JS Map)
        int CountDesc(LineageNode n)
        {
            if (desc.TryGetValue(n, out var cached)) return cached;
            desc[n] = 0; // guard
            var c = 0;
            foreach (var e in n.ChildEdges) c += 1 + CountDesc(e.To);
            desc[n] = c;
            return c;
        }
        foreach (var n in nodes) if (!n.IsSource) n.Descendants = CountDesc(n);

        bool Qualifies(LineageNode n) =>
            !n.IsSource && (
                n.Descendants >= 8 ||
                n.Contested ||
                SpineFounders.Contains(n.Primary) ||
                (n.Korean && n.Descendants >= 2));

        // ancestor closure so the spine is always connected
        var spine = new HashSet<LineageNode>();
        foreach (var n in nodes)
        {
            if (!Qualifies(n)) continue;
            LineageNode? cur = n;
            var guard = 0;
            while (cur != null && !spine.Contains(cur) && guard++ < 200)
            {
                spine.Add(cur);
                cur = cur.ParentEdge != null ? cur.ParentEdge.From : null;
            }
        }
        // book sources ride with their (spine) child
        foreach (var e in edges)
            if (e.Kind == "book" && spine.Contains(e.To)) spine.Add(e.From);

        foreach (var n in nodes) n.Spine = spine.Contains(n);
    }

    // ── Helpers ──

    /// <summary>First name containing a CJK ideograph, else "".</summary>
    private static string FirstCjk(IReadOnlyList<string> names)
    {
        foreach (var n in names) if (ReCjk.IsMatch(n)) return n;
        return "";
    }

    private static bool IsKoreanNode(LineageMasterRecord m, string schoolKey)
    {
        if (schoolKey == "korean-seon") return true;
        var region = (m.Region ?? "").ToLowerInvariant();
        return ReKoreanRegion.IsMatch(region);
    }

    private static string FormatDates(LineageMasterRecord m)
    {
        var b = m.Birth ?? 0;
        var d = m.Death ?? 0;
        var f = m.Floruit ?? 0;
        var c = m.DatesConjectural ? "c. " : "";
        if (b != 0 && d != 0) return $"{c}{b}–{d}"; // en-dash
        if (d != 0) return $"{c}d. {d}";
        if (b != 0) return $"{c}b. {b}";
        if (f != 0) return $"{c}fl. {f}";
        return "";
    }

    /// <summary>Representative year. NEVER invented: death, else birth+65, else floruit, else null.
    /// A value of 0 is treated as missing (JS truthiness).</summary>
    private static int? RepYear(LineageMasterRecord m)
    {
        if ((m.Death ?? 0) != 0) return m.Death;
        if ((m.Birth ?? 0) != 0) return m.Birth + 65;
        if ((m.Floruit ?? 0) != 0) return m.Floruit;
        return null;
    }

    /// <summary>JS `a || b` for strings: <paramref name="a"/> if non-empty, else <paramref name="b"/> (or "").</summary>
    private static string Or(string? a, string? b) => !string.IsNullOrEmpty(a) ? a! : (b ?? "");
}
