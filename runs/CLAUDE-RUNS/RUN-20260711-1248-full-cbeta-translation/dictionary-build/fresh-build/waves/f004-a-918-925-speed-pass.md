# Speed pass after f004 A918–925

Measured on the final eight-entry pre-review gate:

- total gate time: 19.927 s
- attribution-packet generation: 13.623 s (68.4%)
- all other gate phases together: 6.304 s
- exact verification: 0.512 s for 53 occurrences
- compilation itself was sub-second per entry; it was not the material bottleneck

The expensive human step was repeatedly reconstructing the same source container while
moving entry-by-entry. The next author packet should therefore be source-first while
retaining an entry index:

1. Group all selected occurrences by `RelPath` and physical proximity.
2. Emit the complete case once, followed by every headword occurrence inside it.
3. Emit the nearest section owner and the exact manifest title automatically.
4. Resolve roster aliases to `names[0]` in the packet, but never infer that the owner
   uttered the headword. The author still reads the complete case and chooses the actor.
5. Provide closed-role choices and the exact `DifferentThingTest.Decision` vocabulary in
   the packet so schema typos are impossible.
6. Compile and exact-verify at each checkpoint, but generate expensive attribution packets
   once per completed cohort unless an actor field changed after that run.

This wave also shows why actor automation cannot replace reading: the fast defaults missed
an invalid lexical segmentation (`祖印可之`) and a person-versus-book split (`孟子`). The
safe speedup is deduplicated context, canonical metadata injection, and delayed cohort-wide
packet generation—not heuristic speaker assignment.

Recommended next measurement: compare 50 occurrences authored from an entry-first packet
against 50 from a source-grouped packet, recording reading minutes, context bytes rendered,
compile retries, and attribution-packet cache hits.
