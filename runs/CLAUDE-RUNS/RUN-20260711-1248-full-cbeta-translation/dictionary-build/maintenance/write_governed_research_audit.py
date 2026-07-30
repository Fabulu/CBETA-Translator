#!/usr/bin/env python3
"""Write an exact governed research-command audit from any working directory."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import sys
import time


def governed_command(
    wrapper: Path, extractor: Path, extraction_output: Path, research_skeleton: Path
) -> list[str]:
    return [
        str(Path(sys.executable).resolve()),
        str(wrapper.resolve()),
        "--script",
        str(extractor.resolve()),
        "--",
        "--extraction-output",
        str(extraction_output.resolve()),
        "--research-skeleton",
        str(research_skeleton.resolve()),
    ]


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--wrapper", required=True, type=Path)
    parser.add_argument("--extractor", required=True, type=Path)
    parser.add_argument("--extraction-output", required=True, type=Path)
    parser.add_argument("--research-skeleton", required=True, type=Path)
    parser.add_argument("--description", default="governed bounded research extraction")
    args = parser.parse_args()
    if not args.wrapper.resolve().is_file():
        raise SystemExit("wrapper is not a file")
    if not args.extractor.resolve().is_file():
        raise SystemExit("extractor is not a file")
    payload = {
        "complete": True,
        "commands": [{
            "epoch": time.time(),
            "argv": governed_command(
                args.wrapper, args.extractor,
                args.extraction_output, args.research_skeleton),
            "command": args.description,
        }],
    }
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name(f".{output.name}.{os.getpid()}.tmp")
    temporary.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
