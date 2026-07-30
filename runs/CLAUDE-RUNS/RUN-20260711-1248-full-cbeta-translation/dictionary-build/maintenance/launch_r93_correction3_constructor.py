#!/usr/bin/env python3
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent.parent))
base=Path(__file__).with_name("launch_r93_correction2_constructor.py")
source=base.read_text(encoding="utf-8").replace("correction2","correction3")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
