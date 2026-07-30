#!/usr/bin/env python3
from pathlib import Path
base=Path(__file__).with_name("start_r93_correction2.py")
source=base.read_text(encoding="utf-8").replace("correction2","correction3")
source=source.replace(
    "started = time.time()",
    "started = 1785424146.688171")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
