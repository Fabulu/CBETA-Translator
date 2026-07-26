// FL8 RETIRED: this suite tested the COMBINED incremental build path — a legacy combined root
// updated in place via BuildOrUpdateAsync(forceRebuild:false): skip-read of stat-unchanged
// entries, the >20% delta guard, the algebraic corpusfreq delta, incremental-inverted gram-set
// recomputation, and winner-flip/transpose bookkeeping.
//
// That path is RETIRED by EAGER migration: a legacy combined root now MIGRATES to the split on the
// next launch instead of doing a combined incremental update, and the split builds each layer FULL
// (origin adopt/full, overlay full). The carry-forward machinery these tests exercised survives only
// on the one-shot Path B CARVE, covered by SplitMigrationTests (PathB: zero XML reads for
// stat-unchanged entries, byte-identical results). Build==full-recount equivalence is covered by
// SplitParityTests; corpusfreq additivity by CorpusFreqDeltaTests' CountCorpusFreqs unit tests +
// SplitParityTests' exact merged==combined corpusfreq assertion.
//
// These tests had been RED since FL6 introduced migration-on-incremental (a combined root's
// forceRebuild:false build migrates, so the incremental counters/assertions no longer hold), so
// retiring them removes already-broken tests, not active coverage. (Was: IncrementalEquivalenceTests — 11 tests.)
namespace ReadZen.Tests.Search;
