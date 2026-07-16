# Quick owner combined-gate repair R1

Status: complete.

Target: `t_2d4525b4b123` (`教外別傳`) S1/O1, `X/X80/X80n1565.xml:0031a08`.

## Exact-case decision

The complete case sits under the `釋迦牟尼佛` heading. Shakyamuni Buddha raises the flower and, after Mahakasyapa alone smiles, speaks the complete formula containing `教外別傳` and entrusts it to Mahakasyapa.

- Exact speaker: `Shakyamuni Buddha`.
- Context figure: `Mahakasyapa`, sole smiling responder and recipient.
- The old note's claim that Shakyamuni was unavailable to the roster no longer suppresses the source-attested speaker.

## Gates

- Strict decision dry-run: 1/1 prepared, zero failures.
- Apply: 1/1, zero failures.
- Focused decision comparison: zero mismatches.
- Whole-entry `zc.verify`: 5/5 exact KWICs pass at their stored `FromLb` values.
- Entry JSON parse and `git diff --check`: passed; only unrelated existing CRLF warnings were emitted.

The focused audit changes unresolved actors from 2 to 1 and named occurrences from 3 to 4. The remaining unresolved row is the pre-existing raised case at S1/O5 (`C/C077/C077n1710.xml`), outside R1's assigned occurrence; its corpus anchor nevertheless verifies.

No merge, commit, or push was performed.
