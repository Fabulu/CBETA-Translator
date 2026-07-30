#!/usr/bin/env python3
"""Launch correction1 using the reviewed retry launcher with distinct paths."""
from pathlib import Path
base=Path(__file__).with_name("launch_r92_retry1_constructor.py")
source=base.read_text(encoding="utf-8")
for stem in ("constructor-config","constructor-command-audit","constructor-checkpoint",
             "replacement-staging-authority"):
    source=source.replace(f"retry1-{stem}",f"retry1-correction1-{stem}")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
