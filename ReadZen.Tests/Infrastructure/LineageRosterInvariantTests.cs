// LineageRosterInvariantTests — DATA invariants for Assets/Data/lineage-masters.json
// (943 hand-curated records). Sibling to LineageGraphBuilderTests, which pins the
// BUILDER's port parity; this file pins the ROSTER's integrity instead.
//
// WHY THIS FILE EXISTS
// --------------------
// The roster was corrupted once and shipped. An auto-harvester inflated 609 -> 1012
// records; 354 of the bad ones reached a commit (246b148f, 963 records). Their shape:
// teacher null, teacher_key null, school "", attestation D, no dates, no steles, no
// links, lineage orphans, and 71 of them sharing one byte-identical boilerplate bio.
// Several were not people at all (天童悟徵云 — a mis-segmented verb, "probes, saying";
// a capping-phrase byline; a lay signatory). NOTHING in the test suite caught it, and
// it took ~30 agents to clean up. Every assertion below is one of those audit findings
// encoded so the next corruption fails loudly instead of shipping.
//
// ⚠️ THE TRAP — READ BEFORE ADDING A TEST HERE ⚠️
// `teacher` is a DISPLAY string ("Shishuang Chuyuan 石霜楚圓 (roster idx 421, present").
// `teacher_key` is the canonical parent-NODE name and the ONLY field the graph uses:
// LineageGraphBuilder.cs:344 — "teacher_key is the canonical parent-NODE name — use it."
// Measuring `teacher` reports a ~86% dangling rate on today's roster and is pure noise.
// FOUR separate passes in this project made that exact mistake and reported dangling
// rates of 30.5%, then 6.9%, then 41.6% — all wrong; hours were spent on a false
// premise. Do not be the fifth. NEVER assert on `teacher`. Assert on `teacher_key`.
// TeacherDisplayString_IsDisplayOnly_NeverAnEdgeKey below pins that trap executably.
//
// THRESHOLD PROVENANCE
// --------------------
// Every bound here was MEASURED on both files before being written:
//   * the clean 965 (Assets/Data/lineage-masters.json @ a4d4410d), and
//   * the real corrupted 963 (`git show 246b148f:Assets/Data/lineage-masters.json`).
// Bounds are set to pass the former and REJECT the latter. Where a bound allows known
// violations, they are listed explicitly with the reason, so the list can only shrink.
//
// 2026-07-17 defect-fix (same-day follow-up to the a4d4410d fold): an independent
// review found the fold itself had shipped 21 duplicate-person clusters (22 excess
// records -- the same man merge-added twice, once thrice) and 4 silently-dropped
// teacher edges. Fixing forward (not reverting) collapsed 965 -> 943. Every bound
// below that referenced 965 has been re-measured against the 943-record file and
// updated; where a bound merely allowed a now-fixed defect (the duplicate-person
// allowlists below), the allowlist entry was removed rather than the bound loosened.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Lineage")]
public class LineageRosterInvariantTests
{
    // LineageRosterService caches per-INSTANCE, so a fresh service yields freshly
    // deserialized records — mutating a copy here cannot leak into another test.
    private static IReadOnlyList<LineageMasterRecord> RealRoster()
        => new LineageRosterService().GetAll();

    // ════════════════════════════════════════════════════════════════════════
    // 1. NO HOLLOW RECORDS — the auto-harvester signature
    // ════════════════════════════════════════════════════════════════════════
    //
    // ⚠️ The evidence clause is deliberately scoped to TEACHER evidence, and this
    // is the single most important detail in this file. The 354 committed hollow
    // records DID carry provenance — `provenance.bio` full of dictionary-occurrence
    // hits ("阿難應諾。"). A rule phrased as "no evidence at all" therefore matches
    // ZERO of them (verified: 0/354). What they never had is evidence for the
    // LINEAGE claim: provenance.teacher was empty on all 354. Scoping the clause to
    // teacher-evidence catches 354/354 with 0 false positives on the clean 965.
    //
    // Thin-but-real records exist legitimately (e.g. 'Biefeng', 'Wufeng'), so the
    // FULL conjunction is required — no single clause is a defect on its own.

    private static List<string> HollowRecords(IReadOnlyList<LineageMasterRecord> roster)
    {
        var orphans = LineageOrphanPrimaries(roster);
        return roster.Where(r =>
                string.IsNullOrWhiteSpace(r.Teacher)
                && string.IsNullOrWhiteSpace(r.TeacherKey)
                && string.IsNullOrWhiteSpace(r.School)
                && (r.Provenance?.Teacher is null || r.Provenance.Teacher.Count == 0)
                && (r.Steles is null || r.Steles.Count == 0)
                && string.Equals(r.Attestation, "D", StringComparison.Ordinal)
                && Primary(r) is { } p && orphans.Contains(p))
            .Select(Primary)
            .Where(p => p != null)!
            .ToList()!;
    }

    [Fact]
    public void NoHollowRecords_TheAutoHarvesterSignature()
    {
        var hollow = HollowRecords(RealRoster());

        // Measured 0 on the clean 965; measured 354 on the corrupt 246b148f.
        Assert.Empty(hollow);
    }

    [Fact]
    public void HollowDetector_CatchesTheHarvesterSignature_NegativeControl()
    {
        // A faithful replica of a real 246b148f record — INCLUDING the bio provenance
        // that made the naive "no evidence" phrasing miss all 354 of them.
        var mutant = RealRoster().ToList();
        mutant.Add(new LineageMasterRecord
        {
            Names = new List<string> { "Ananda" },
            School = "",
            Teacher = null,
            TeacherKey = null,
            Bio = "Corpus-attested Zen roster identity added by strict dictionary adjudication.",
            Attestation = "D",
            Transmission = "direct",
            AddedNode = true,
            Provenance = new LineageProvenance
            {
                Teacher = new List<LineageProvenanceItem>(), // the tell: no lineage evidence
                Bio = new List<LineageProvenanceItem>
                {
                    new() { Source = "X/X79/X79n1557.xml", Lb = "0014a22", Rung = "corpus", Quote = "阿難應諾。" },
                },
            },
        });

        var hollow = HollowRecords(mutant);
        Assert.Contains("Ananda", hollow);
    }

    [Fact]
    public void HollowDetector_IgnoresLegitimateThinRecords_PrecisionControl()
    {
        // Precision guard: a thin-but-real record (a school, an orphan, grade D, no
        // teacher) must NOT be flagged. Only the full conjunction is the corruption.
        var mutant = RealRoster().ToList();
        mutant.Add(new LineageMasterRecord
        {
            Names = new List<string> { "Thin But Real", "薄實" },
            School = "Linji",       // <- one honest field is enough to clear the signature
            Teacher = null,
            TeacherKey = null,
            Attestation = "D",
        });

        Assert.DoesNotContain("Thin But Real", HollowRecords(mutant));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. NO BOILERPLATE BIOS
    // ════════════════════════════════════════════════════════════════════════
    //
    // The corruption's loudest signal: 227 of 403 harvested records shared ONE bio
    // (71 byte-identical copies survived into the 246b148f commit).
    //
    // N = 2, justified by measurement: on the clean roster (943 records, post
    // 2026-07-17 defect-fix) every non-empty bio is UNIQUE — the real max repeat is
    // 1. N=2 tolerates a single legitimate
    // accidental collision (two records for closely-related figures) while still
    // catching the real boilerplate by a factor of ~35x. Corrupt file measures 71.

    private static (int Count, string Bio) MaxBioRepeat(IReadOnlyList<LineageMasterRecord> roster)
    {
        var top = roster
            .Select(r => (r.Bio ?? "").Trim())
            .Where(b => b.Length > 0)
            .GroupBy(b => b, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        return top is null ? (0, "") : (top.Count(), top.Key);
    }

    [Fact]
    public void NoBoilerplateBios_NoBioRepeatsMoreThanTwice()
    {
        var (count, bio) = MaxBioRepeat(RealRoster());

        Assert.True(count <= 2,
            $"A bio is repeated {count}x — boilerplate is the auto-harvester's signature. Bio: {Excerpt(bio)}");
    }

    [Fact]
    public void BoilerplateBioDetector_Catches300xDuplicate_NegativeControl()
    {
        var mutant = RealRoster().ToList();
        const string boiler = "Corpus-attested Zen roster identity added after provenance harvest.";
        for (var i = 0; i < 300; i++)
            mutant.Add(new LineageMasterRecord { Names = new List<string> { $"Fake {i}" }, Bio = boiler });

        var (count, _) = MaxBioRepeat(mutant);
        Assert.True(count >= 300);          // detector sees it
        Assert.False(count <= 2);           // and the real assertion above would go red
    }

    [Fact]
    public void NoNearBoilerplateBios_PrefixRepeatStaysLow()
    {
        // Catches boilerplate that varies only in its tail (the real corruption had
        // 227 bios sharing a 50-char prefix while only 71 were byte-identical).
        // Measured: clean roster (943 records, post 2026-07-17 defect-fix) max = 3
        // ("Northern Song Linji master of the Huanglong branch"), which is
        // legitimate house style. Corrupt 963 max = 227.
        const int prefixLen = 50;
        var top = RealRoster()
            .Select(r => (r.Bio ?? "").Trim())
            .Where(b => b.Length >= prefixLen)
            .Select(b => b[..prefixLen])
            .GroupBy(p => p, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var count = top?.Count() ?? 0;
        Assert.True(count <= 8,
            $"{count} bios share a {prefixLen}-char prefix — near-boilerplate. Prefix: {Excerpt(top?.Key ?? "")}");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. EVERY RECORD CARRIES EVIDENCE — the anti-fabrication ratchet
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EveryRecord_HasAValidAttestationGrade()
    {
        // Measured: 943/943 carry A/B/C/D. Pin at zero.
        // A missing grade is not harmless — the chart fail-safes it to D (faint dots),
        // silently demoting a real master's edge to "barely attested".
        var bad = RealRoster()
            .Where(r => r.Attestation is not ("A" or "B" or "C" or "D"))
            .Select(r => $"{Primary(r)}:{r.Attestation ?? "<null>"}")
            .ToList();

        Assert.Empty(bad);
    }

    [Fact]
    public void AttestationGradeDetector_CatchesAMissingGrade_NegativeControl()
    {
        var mutant = RealRoster().ToList();
        mutant.Add(new LineageMasterRecord { Names = new List<string> { "Ungraded" } });

        Assert.Contains(mutant, r => r.Attestation is not ("A" or "B" or "C" or "D"));
    }

    [Fact]
    public void EveryRecord_HasProvenanceOrSteles_ExceptFourKnownGaps()
    {
        // ⚠️ The fold's write-up claims "0 records lack provenance/attestation".
        // Attestation is indeed 100%. PROVENANCE IS NOT: four records carry an empty
        // provenance block AND no steles. Three of them at least carry a `links` entry;
        // 'Biefeng' carries no evidence of any kind (no provenance, no steles, no links,
        // no bio, no teacher, grade D) — it is the closest thing to a hollow record left
        // in the roster, and escapes the §1 signature only because school="Linji".
        // This allowlist is a RATCHET: it may shrink, never grow.
        var known = new[] { "Geumheo Beopcheom", "Biefeng", "Wufeng", "Baoshou Yanzhao" };

        var missing = RealRoster()
            .Where(r => !HasProvenance(r) && (r.Steles is null || r.Steles.Count == 0))
            .Select(Primary)
            .ToList();

        Assert.All(missing, p => Assert.Contains(p, known));
        Assert.True(missing.Count <= known.Length,
            $"{missing.Count} records lack provenance AND steles: {string.Join(", ", missing)}");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. teacher_key RESOLVES — measured on teacher_key, NEVER on teacher
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TeacherKeyResolution_StaysAtOrBelowOnePercent()
    {
        var roster = RealRoster();
        var g = LineageGraphBuilder.Build(roster);

        var withKey = roster.Count(r => !string.IsNullOrWhiteSpace(r.TeacherKey));
        var unresolved = g.Report.UnresolvedTeacherKey.Count;
        var rate = (double)unresolved / withKey;

        // Measured on the 943-record roster (post fold + 2026-07-17 defect-fix):
        // 876 teacher_key edges, 874 resolved, 2 unresolved = 0.23%. The 2 allowed are
        // BOTH the 龍巖慧彦 pair (below) — a teacher of two Korean masters who is
        // himself not on the roster. They are the same 2 that the clean 609 baseline
        // had (0.37% of 542), and the same 2 the fold originally reported at 894/892 —
        // collapsing 21 duplicate-person clusters removed 18 net teacher_key edges
        // (22 dropped records minus 4 newly-resolved noteacher edges) without
        // resolving or breaking either of the 2 pre-existing unresolved keys.
        Assert.True(withKey >= 876, $"teacher_key coverage regressed: {withKey} edges");
        Assert.True(rate <= 0.01, $"{unresolved}/{withKey} teacher_key edges unresolved ({rate:P2})");
    }

    [Fact]
    public void TeacherDisplayString_IsDisplayOnly_NeverAnEdgeKey()
    {
        // This test exists to STOP a fifth agent from measuring the wrong field.
        // It asserts the trap rather than the data: `teacher` does NOT resolve as a
        // node key and never did. If you are about to write a test that reports a
        // "30-40% dangling rate", you are reading this field. Read teacher_key.
        var roster = RealRoster();
        var byName = new HashSet<string>(
            roster.SelectMany(r => r.Names ?? new List<string>()).Where(n => !string.IsNullOrEmpty(n)),
            StringComparer.Ordinal);

        var withTeacher = roster.Where(r => !string.IsNullOrWhiteSpace(r.Teacher)).ToList();
        var danglingByDisplay = withTeacher.Count(r => !byName.Contains(r.Teacher!));
        var displayRate = (double)danglingByDisplay / withTeacher.Count;

        // ~86% of `teacher` strings are not node names — they are prose
        // ("Shishuang Chuyuan 石霜楚圓 (roster idx 421, present"). That number is NOISE,
        // not a graph defect: measured on teacher_key the same roster is 99.78% resolved.
        Assert.True(displayRate > 0.5,
            "`teacher` unexpectedly resolves as a node key. If the roster changed such that it " +
            "does, DELETE this test — but do NOT start asserting on `teacher`: the graph reads " +
            "teacher_key (LineageGraphBuilder.cs:344) and only teacher_key is canonical.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. DANGLING IS A FEATURE — but never a SILENT one
    // ════════════════════════════════════════════════════════════════════════
    //
    // Bodhidharma -> Prajnatara dangles BY DESIGN: the Indian patriarchs are
    // deliberately excluded ("anything before Bodhidharma is myth"). 49 records carry
    // teacher_dangling: true. The invariant is not "nothing dangles" — it is that
    // nothing dangles SILENTLY.

    /// <summary>The only teacher_key allowed to not resolve, with the reason.</summary>
    private static readonly string[] KnownAllowedUnresolvedTeacherKeys =
    {
        // 龍巖慧彦 — teacher of the Korean masters Yeongwol Bongyul and Gyeongheo
        // Seongu. He is not himself on the roster; the same 2 edges dangled in the
        // clean 609 baseline. Deliberate, documented, and the ONLY allowed pair.
        "龍巖慧彦",
    };

    [Fact]
    public void NoSilentDangles_EveryUnresolvedTeacherIsFlaggedOrKnown()
    {
        var roster = RealRoster();
        var byName = new HashSet<string>(
            roster.SelectMany(r => r.Names ?? new List<string>()).Where(n => !string.IsNullOrEmpty(n)),
            StringComparer.Ordinal);

        var silent = roster
            .Where(r => !string.IsNullOrWhiteSpace(r.TeacherKey))
            .Where(r => !byName.Contains(r.TeacherKey!))
            .Where(r => !r.TeacherDangling)
            .Where(r => !KnownAllowedUnresolvedTeacherKeys.Contains(r.TeacherKey))
            .Select(r => $"{Primary(r)} -> {r.TeacherKey}")
            .ToList();

        Assert.Empty(silent);
    }

    [Fact]
    public void SilentDangleDetector_CatchesAnUnflaggedBrokenKey_NegativeControl()
    {
        var mutant = RealRoster().ToList();
        mutant.Add(new LineageMasterRecord
        {
            Names = new List<string> { "Silently Broken" },
            TeacherKey = "NoSuchMasterAnywhere",   // points at nothing...
            TeacherDangling = false,               // ...and does not admit it
            Attestation = "B",
        });

        var byName = new HashSet<string>(
            mutant.SelectMany(r => r.Names ?? new List<string>()), StringComparer.Ordinal);
        var silent = mutant
            .Where(r => !string.IsNullOrWhiteSpace(r.TeacherKey))
            .Where(r => !byName.Contains(r.TeacherKey!))
            .Where(r => !r.TeacherDangling)
            .Where(r => !KnownAllowedUnresolvedTeacherKeys.Contains(r.TeacherKey))
            .ToList();

        Assert.Single(silent);
        Assert.Equal("Silently Broken", Primary(silent[0]));
    }

    [Fact]
    public void FlaggedDangles_AreDeliberate_TeacherNamedButNoKey()
    {
        // The roster's own convention, measured and pinned: a record that names a
        // teacher but supplies no teacher_key is ALWAYS flagged teacher_dangling.
        // Measured on the 943-record roster: 925 records name a teacher, 876 have a
        // key, and the 49-record difference is EXACTLY the 49 flagged records (the
        // same 49 as the fold's original 965 -- the 2026-07-17 defect-fix pass only
        // touched records that already had both a teacher and a teacher_key). Zero
        // exceptions.
        var roster = RealRoster();

        var unflagged = roster
            .Where(r => !string.IsNullOrWhiteSpace(r.Teacher))
            .Where(r => string.IsNullOrWhiteSpace(r.TeacherKey))
            .Where(r => !r.TeacherDangling)
            .Select(Primary)
            .ToList();

        Assert.Empty(unflagged);
        Assert.Equal(49, roster.Count(r => r.TeacherDangling));
    }

    [Fact]
    public void Bodhidharma_DanglesDeliberately_AndRendersAsAnHonestStub()
    {
        // The rule working, not damage: Prajnatara is off-chart by design.
        var g = LineageGraphBuilder.Build(RealRoster());

        Assert.True(g.ByName.TryGetValue("Bodhidharma", out var bodhi));
        Assert.True(bodhi!.TeacherDangling, "Bodhidharma's off-chart teacher must be FLAGGED, not silent.");
        Assert.True(bodhi.Stub, "A flagged dangle must render as an honest stub, never as a root.");
        Assert.False(bodhi.IsRoot);
        Assert.Contains("Prajnatara", bodhi.StubLabel ?? "");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6. THE EXCLUDED STAY EXCLUDED
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ExcludedNonChanMasters_StayExcluded()
    {
        // A deliberate user ruling. Do NOT re-add these "helpfully":
        //
        //  三昧寂光 / 古心如馨 — a real, documented transmission — but a VINAYA one
        //    (律宗千華派), not Chan. No lineage edge is lost by excluding them:
        //    三昧寂光 was only a TONSURE master to a Chan heir, and tonsure is not
        //    dharma transmission (a distinction DILA itself routinely conflates).
        //  天童悟徵云 — NOT A PERSON. It is 天童悟 (=密雲圓悟) followed by 徵云,
        //    "probes, saying" — a verb the harvester mis-segmented into a monk.
        //    Proved by counting: 天童悟云 51x vs 天童悟徵云 3x, and the same verb
        //    attaches to 報慈遂徵云 (23x), 雲居錫徵云, 法眼益徵云. Merged into 密雲圓悟.
        //  鄭溥元 — the LAY SIGNATORY of a portrait encomium (the encomium's actual
        //    subject, 即非如一, is on the roster).
        //  雲門萬壽 — the identity does not exist: zero hits in 4,990 files, no DILA
        //    record. Its one citation belongs to 福州大中立誌 (A014496).
        //
        // NOT listed here on purpose: 普會 and 鳳山. Both were harvester junk, but both
        // are ALSO legitimate aliases of real masters now on the roster (普會 is a
        // posthumous title of 石霜慶諸; 鳳山 the anthology glosses as 元叟端). Asserting
        // their absence by name would be a false positive.
        string[] excluded = { "三昧寂光", "古心如馨", "天童悟徵云", "鄭溥元", "雲門萬壽" };

        var present = RealRoster()
            .Where(r => (r.Names ?? new List<string>()).Any(n => excluded.Contains(n)))
            .Select(Primary)
            .ToList();

        Assert.Empty(present);
    }

    [Fact]
    public void ExclusionDetector_CatchesAReAddedVinayaMaster_NegativeControl()
    {
        var mutant = RealRoster().ToList();
        mutant.Add(new LineageMasterRecord
        {
            Names = new List<string> { "Sanmei Jiguang", "三昧寂光" },
            School = "律宗千華派",
            Attestation = "A",
        });

        string[] excluded = { "三昧寂光", "古心如馨", "天童悟徵云", "鄭溥元", "雲門萬壽" };
        Assert.Contains(mutant, r => (r.Names ?? new List<string>()).Any(n => excluded.Contains(n)));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7. CONTESTED EDGES KEEP THEIR GRADES — "keep the man, dash the edge"
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    // 茶陵郁山主 — attestation D (faint dots). The early lamps file him 「未詳法嗣」, and
    // the early account's 「因茲更不遊方」 (he never travelled again) positively EXCLUDES
    // the journey the later 楊岐方會 attribution needs. Both readings are recorded;
    // neither is silently resolved. Keep the man on the chart, dash his edge.
    [InlineData("茶陵郁山主", "D")]
    // 興教明 — attestation C (dashed). Deshao taught him and gave him a verse, but
    // 景德傳燈錄's roster of Deshao's 49 heirs OMITS him. QUALIFIED, not certain.
    [InlineData("興教明", "C")]
    public void ContestedEdges_KeepTheirDeliberatelyWeakGrades(string cjkName, string expectedGrade)
    {
        // A grade "upgrade" here is not a cleanup — it is the chart telling a lie in ink.
        // The corrupt 246b148f had 興教明 at D, so this pins the value from BOTH sides.
        var rec = RealRoster().SingleOrDefault(r => (r.Names ?? new List<string>()).Contains(cjkName));

        Assert.True(rec != null, $"{cjkName} was removed from the roster — the ruling was 'keep the man'.");
        Assert.Equal(expectedGrade, rec!.Attestation);
    }

    [Theory]
    [InlineData("茶陵郁山主")]
    [InlineData("興教明")]
    public void ContestedEdges_ActuallyRenderDashed_TheWholePointOfTheGrade(string cjkName)
    {
        // Ties the data assertion to its rendering consequence: the chart picks edge ink
        // ONLY from `attestation` (LineageChartControl.StyleFor), and C/D are the dashed
        // grades. If this ever goes solid, the roster has silently upgraded the claim.
        var rec = RealRoster().Single(r => (r.Names ?? new List<string>()).Contains(cjkName));

        var style = LineageChartControl.StyleFor(rec.Attestation);
        Assert.NotEmpty(style.Dash);
    }

    [Fact]
    public void ContestedGradeDetector_CatchesAnUpgrade_NegativeControl()
    {
        var mutant = RealRoster().Select(Clone).ToList();
        var target = mutant.Single(r => (r.Names ?? new List<string>()).Contains("茶陵郁山主"));
        target.Attestation = "A";   // "helpfully" resolving a deliberately-contested edge

        Assert.NotEqual("D", target.Attestation);
        Assert.Empty(LineageChartControl.StyleFor(target.Attestation).Dash); // now renders SOLID = a lie
    }

    // ════════════════════════════════════════════════════════════════════════
    // 8. NO DUPLICATE PEOPLE
    // ════════════════════════════════════════════════════════════════════════
    //
    // ⚠️ A naive "two records share a name => duplicate" test FALSE-POSITIVES badly:
    // 27 record pairs share a name legitimately. Generic posthumous titles collide by
    // design (大覺禪師 belongs to THREE unrelated monks: 西堂智藏, 育王懷璉, 徑山道欽;
    // 弘覺禪師 to three; 佛鑑禪師 to two), and 海舟普慈 / 海舟永慈 genuinely share the
    // alias 海舟慈 — that near-collision IS the forgery this roster documents.
    //
    // So the rule below is narrow and high-precision rather than broad and noisy:
    //   shared name  AND  identical teacher_key  AND  identical, non-null dates.
    // Two different men may share an epithet, or a teacher, or unknown dates — but
    // sharing a name AND a teacher AND exact lifespan means one man, twice.
    // Verified: this rule does NOT fire on any of the legitimate pairs above.

    private static List<string> DuplicatePersonPairs(IReadOnlyList<LineageMasterRecord> roster)
    {
        var pairs = new SortedSet<string>(StringComparer.Ordinal);
        var byName = new Dictionary<string, List<LineageMasterRecord>>(StringComparer.Ordinal);
        foreach (var r in roster)
            foreach (var n in (r.Names ?? new List<string>()).Where(n => !string.IsNullOrEmpty(n)).Distinct())
                (byName.TryGetValue(n, out var l) ? l : byName[n] = new List<LineageMasterRecord>()).Add(r);

        foreach (var (_, group) in byName.Where(kv => kv.Value.Count > 1))
            for (var i = 0; i < group.Count; i++)
                for (var j = i + 1; j < group.Count; j++)
                {
                    var (a, b) = (group[i], group[j]);
                    if (string.IsNullOrWhiteSpace(a.TeacherKey) || a.TeacherKey != b.TeacherKey) continue;
                    if (a.Birth != b.Birth || a.Death != b.Death) continue;
                    if (a.Birth is null && a.Death is null) continue; // unknown dates prove nothing
                    pairs.Add(PairKey(Primary(a), Primary(b)));
                }
        return pairs.ToList();
    }

    /// <summary>
    /// Duplicate-person pairs present in the roster. The list is a RATCHET: it may
    /// shrink as they are merged, but a new duplicate fails the test.
    /// 2026-07-17 defect-fix: all 5 pairs previously listed here (plus 16 more this
    /// narrow shared-name+teacher_key+dates rule didn't happen to catch -- 21
    /// duplicate-person clusters in total, found by a Fable review and an
    /// independent Opus test-writer, both converging on the same defect) were
    /// collapsed by merging the duplicate into the richer/earlier record (union of
    /// names/students/links/provenance) and deleting the redundant one. Empty now;
    /// stays empty unless a new duplicate ships.
    /// </summary>
    private static readonly string[] KnownDuplicatePersonPairs = Array.Empty<string>();

    [Fact]
    public void NoNewDuplicatePeople()
    {
        var found = DuplicatePersonPairs(RealRoster());

        // Subset check, not equality: merging a known duplicate must KEEP this green.
        var unexpected = found.Except(KnownDuplicatePersonPairs, StringComparer.Ordinal).ToList();
        Assert.Empty(unexpected);
    }

    [Fact]
    public void DuplicatePersonDetector_CatchesAClonedRecord_NegativeControl()
    {
        var roster = RealRoster();
        var original = roster.First(r =>
            !string.IsNullOrWhiteSpace(r.TeacherKey) && (r.Birth != null || r.Death != null));

        var mutant = roster.ToList();
        var clone = Clone(original);
        clone.Names = new List<string>(original.Names!);   // same names, same teacher, same dates
        mutant.Add(clone);

        var found = DuplicatePersonPairs(mutant);
        var unexpected = found.Except(KnownDuplicatePersonPairs, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(unexpected);
    }

    [Fact]
    public void DuplicateDetector_DoesNotFlagUnrelatedMonksSharingAPosthumousTitle_PrecisionControl()
    {
        // 大覺禪師 is held by three unrelated monks; 海舟慈 by two. Neither may be flagged.
        var found = DuplicatePersonPairs(RealRoster());

        Assert.DoesNotContain(found, p => p.Contains("Xitang Zhizang", StringComparison.Ordinal));
        Assert.DoesNotContain(found, p => p.Contains("Haizhou Puci", StringComparison.Ordinal));
        Assert.DoesNotContain(found, p => p.Contains("Haizhou Yongci", StringComparison.Ordinal));
    }

    /// <summary>
    /// A record's primary name (names[0]) is its graph NODE ID:
    /// LineageGraphBuilder does <c>byId[n.Id] = node</c> — "last wins on a duplicate id".
    /// Two records with the same primary name therefore make one of them unreachable by
    /// id. Ratchet: may shrink, not grow.
    /// 2026-07-17 defect-fix: both entries previously listed here (Cuiyan Kezhen,
    /// idx 647+940; Dayu Shouzhi, idx 658+929, part of a THREE-way duplicate with
    /// 648 'Cuiyan Shouzhi') were collapsed into single records. Empty now.
    /// </summary>
    private static readonly string[] KnownDuplicatePrimaryNames = Array.Empty<string>();

    [Fact]
    public void NoNewDuplicatePrimaryNames_NodeIdsMustBeUnique()
    {
        var dupes = RealRoster()
            .Select(Primary)
            .Where(p => p != null)
            .GroupBy(p => p!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        var unexpected = dupes.Except(KnownDuplicatePrimaryNames, StringComparer.Ordinal).ToList();
        Assert.Empty(unexpected);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 9. SCHEMA — deserializes with no silent data loss
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Schema_EveryRecordDeserializesWithIdentityAndGrade()
    {
        var roster = RealRoster();

        Assert.True(roster.Count >= 943, $"roster shrank to {roster.Count}");
        Assert.All(roster, r =>
        {
            Assert.NotNull(r.Names);
            Assert.NotEmpty(r.Names!);                                  // no name = no node (builder skips it)
            Assert.All(r.Names!, n => Assert.False(string.IsNullOrWhiteSpace(n)));
            Assert.NotNull(r.Attestation);
        });
    }

    [Fact]
    public void Schema_NoJsonFieldIsSilentlyDropped()
    {
        // System.Text.Json ignores unmapped properties — so a field added to the roster
        // and NOT to LineageMasterRecord vanishes without a word. This catches that.
        var modelled = new HashSet<string>(
            typeof(LineageMasterRecord)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name),
            StringComparer.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(AppPaths.LineageMastersPath));
        var unmapped = doc.RootElement.EnumerateArray()
            .SelectMany(rec => rec.EnumerateObject().Select(p => p.Name))
            .Distinct(StringComparer.Ordinal)
            .Where(name => !modelled.Contains(name))
            .ToList();

        Assert.Empty(unmapped);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 10. AGGREGATE SHAPE — a bulk-injection ratchet
    // ════════════════════════════════════════════════════════════════════════
    //
    // The §1 conjunction is precise but brittle: a harvester that writes school="" only
    // 99% of the time would slip past it. These four coverage bounds are coarse and
    // robust — they describe the shape of a HAND-CURATED roster, and a bulk injection
    // of hollow records moves them all at once. Bounds are set well clear of today's
    // values but well inside the corrupt file's. Measured (clean 943, post 2026-07-17
    // defect-fix / corrupt 963):
    //   school coverage      97.2% / 61.8%
    //   teacher_key coverage 92.9% / 56.3%
    //   grade-D share         4.9% / 41.4%
    //   lineage-orphan share  0.3% / 40.3%
    // (The "lineage-orphan share" figure previously written here as 3.1% for the
    // 965-record roster was independently re-measured while updating this comment
    // and found to already be wrong -- the real figure on the unmodified 965 file
    // is 0.4% (4 orphans: Shanhui Dashi, Jianfu Chenggu, Sin'gwang Chonghwi, Dufeng
    // Shan -- the last is one of the 4 dropped-teacher-edge fixes below, an orphan
    // no longer once its edge is written). The 0.10 ceiling below was never at risk
    // either way; only the illustrative comment was stale.)

    [Fact]
    public void AggregateShape_LooksLikeAHandCuratedRoster()
    {
        var roster = RealRoster();
        var n = (double)roster.Count;

        var school = roster.Count(r => !string.IsNullOrWhiteSpace(r.School)) / n;
        var key = roster.Count(r => !string.IsNullOrWhiteSpace(r.TeacherKey)) / n;
        var gradeD = roster.Count(r => r.Attestation == "D") / n;
        var orphan = LineageOrphanPrimaries(roster).Count / n;

        Assert.True(school >= 0.90, $"school coverage fell to {school:P1} (hand-curated ~97%)");
        Assert.True(key >= 0.85, $"teacher_key coverage fell to {key:P1} (hand-curated ~93%)");
        Assert.True(gradeD <= 0.15, $"grade-D share rose to {gradeD:P1} (hand-curated ~5%; the harvest was 41%)");
        Assert.True(orphan <= 0.10, $"lineage-orphan share rose to {orphan:P1} (hand-curated ~3%; the harvest was 40%)");
    }

    [Fact]
    public void AggregateShape_CatchesABulkHollowInjection_NegativeControl()
    {
        // 400 hollow records — the corruption at its real scale.
        var mutant = RealRoster().ToList();
        for (var i = 0; i < 400; i++)
            mutant.Add(new LineageMasterRecord
            {
                Names = new List<string> { $"Harvested {i}" },
                School = "",
                Teacher = null,
                TeacherKey = null,
                Attestation = "D",
                Bio = "Corpus-attested Zen roster identity added after provenance harvest.",
            });

        var n = (double)mutant.Count;
        Assert.False(mutant.Count(r => !string.IsNullOrWhiteSpace(r.School)) / n >= 0.90);
        Assert.False(mutant.Count(r => !string.IsNullOrWhiteSpace(r.TeacherKey)) / n >= 0.85);
        Assert.False(mutant.Count(r => r.Attestation == "D") / n <= 0.15);
        Assert.False(LineageOrphanPrimaries(mutant).Count / n <= 0.10);
        Assert.NotEmpty(HollowRecords(mutant));   // and the precise detector fires too
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string? Primary(LineageMasterRecord r)
        => (r.Names ?? new List<string>()).FirstOrDefault(n => !string.IsNullOrEmpty(n));

    private static bool HasProvenance(LineageMasterRecord r)
        => r.Provenance is { } p
           && ((p.Teacher?.Count ?? 0) + (p.Dates?.Count ?? 0)
               + (p.School?.Count ?? 0) + (p.Bio?.Count ?? 0)) > 0;

    private static string PairKey(string? a, string? b)
    {
        var (x, y) = (a ?? "", b ?? "");
        return string.CompareOrdinal(x, y) <= 0 ? $"{x} | {y}" : $"{y} | {x}";
    }

    private static string Excerpt(string s)
        => s.Length <= 90 ? s : s[..90] + "…";

    /// <summary>A lineage orphan AS THE CHART SEES ONE: no parent edge, no children,
    /// and not an honest off-chart stub. Built through the real consumer so the
    /// definition can never drift from the graph.</summary>
    private static HashSet<string> LineageOrphanPrimaries(IReadOnlyList<LineageMasterRecord> roster)
    {
        var g = LineageGraphBuilder.Build(roster);
        return new HashSet<string>(
            g.Nodes.Where(n => !n.IsSource && n.ParentEdge is null && n.ChildEdges.Count == 0 && !n.Stub)
                   .Select(n => n.Primary),
            StringComparer.Ordinal);
    }

    /// <summary>Property-wise shallow clone, so a mutant can never write through to
    /// the shared roster instance.</summary>
    private static LineageMasterRecord Clone(LineageMasterRecord r)
    {
        var c = new LineageMasterRecord();
        foreach (var p in typeof(LineageMasterRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.CanWrite) p.SetValue(c, p.GetValue(r));
        return c;
    }
}
