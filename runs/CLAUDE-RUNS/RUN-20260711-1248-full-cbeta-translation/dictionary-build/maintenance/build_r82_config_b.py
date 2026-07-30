#!/usr/bin/env python3
"""Regenerate a rescheduled cohort config from the sealed R81 authoring program.

The cohort is explicit so no expired R81 time-bound path can survive silently.
The source program contains the adjudicated entry payload; only cohort/path
bindings are parameterized here.
"""
import argparse
from pathlib import Path

HERE = Path(__file__).resolve().parent


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--cohort", required=True)
    args = parser.parse_args()
    cohort = args.cohort.upper()
    if cohort != "R82":
        raise SystemExit("this sealed reschedule accepts --cohort R82 only")
    source = (HERE / "build_r81_config_b.py").read_text(encoding="utf-8")
    if "R81" not in source or "r81" not in source:
        raise SystemExit("R81 authoring source no longer has cohort bindings")
    regenerated = source.replace("R81", cohort).replace("r81", cohort.lower())
    namespace = {"__name__": "__main__", "__file__": str(HERE / f"build_{cohort.lower()}_generated.py")}
    exec(compile(regenerated, namespace["__file__"], "exec"), namespace)


if __name__ == "__main__":
    main()
