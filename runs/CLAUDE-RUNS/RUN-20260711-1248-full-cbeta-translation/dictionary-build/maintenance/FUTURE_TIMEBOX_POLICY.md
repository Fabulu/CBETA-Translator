# Future global timebox policy

New dictionary artifact-zero receipts use
`bounded-dictionary-timegate.v4` and declare
`timeboxMultiplier: 2.0`.

The multiplier is applied exactly once to the base evidence schedule's absolute
deadlines. It covers selection/viability, extraction, adjudicated config,
constructor/first product/construction, independent review, correction, and
publication. It does not reset the immutable artifact-zero epoch.

Historical v1-v3 receipts retain their original byte-bound schedules and remain
valid. The multiplier must not be inferred, retrofitted, compounded, or applied
again by a watchdog, retry, correction, publication run, or ad hoc process
review. Ad hoc reviews should quote the v4 receipt's already-derived deadline;
they must not multiply that deadline themselves.
