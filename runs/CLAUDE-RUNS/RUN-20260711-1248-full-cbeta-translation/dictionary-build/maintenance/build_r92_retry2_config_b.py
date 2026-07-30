#!/usr/bin/env python3
"""Build corrected R92 retry2 config under retry2-only paths."""
from pathlib import Path
base=Path(__file__).with_name("build_r92_retry1_config_b.py")
source=base.read_text(encoding="utf-8").replace("retry1","retry2")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
