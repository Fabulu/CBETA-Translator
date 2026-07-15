# Iriya semantic adjudication log

Corpus manifest: `42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a`

Status: **whole queue quarantined; no Iriya construction is eligible**.

The authoritative running decisions live in `fresh-build/iriya-admission/packet-001.json` through
`packet-021.json`; this log records durable 50-row checkpoints and adjudication findings. Every row receives
exactly one disposition from `IRIYA_ADJUDICATION_GUIDE.md`: `KEEP (couplet)`, `KEEP (component)`,
`PROVISIONAL`, or `REJECT`. Mechanical flags never decide the result. Exact-absent rows require explicit
clause and segmentation searches; anchor-inflation rows use Pair rather than Anchor counts; one-work
Chan deployments are provisional rather than rejected.

No checkpoints completed yet: **0 / 2,008 adjudicated**.
