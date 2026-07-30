#!/usr/bin/env python3
"""Launch corrected R92 retry2 through the governed constructor."""
from pathlib import Path
base=Path(__file__).with_name("launch_r92_retry1_constructor.py")
source=base.read_text(encoding="utf-8").replace("retry1","retry2")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
