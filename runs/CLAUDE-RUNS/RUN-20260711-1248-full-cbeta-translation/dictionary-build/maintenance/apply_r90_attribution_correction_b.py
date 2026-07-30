#!/usr/bin/env python3
"""Authorized exact-coordinate R90 attribution-note correction."""
from pathlib import Path

template_path=Path(__file__).with_name("apply_r84_late_construction_b.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R84","R90").replace("r84","r90")
source=source.replace(
 'IDS=["t_1cec9c4c3c40","t_1cfa8b8aa2a3","t_1d0056511f4d"]',
 'IDS=["t_207efae5f6bd","t_20d13943f1a6","t_20ff8118754b"]')
source=source.replace(
 'RECEIPT=M/"non-iriya-v7-depth-regeneration-r90-late-construction-correction-b.json"',
 'RECEIPT=M/"non-iriya-v7-depth-regeneration-r90-attribution-correction-b.json"')
source=source.replace(
 '"watchdogFailure":"adjudicated config late: 545.858s"',
 '"watchdogFailure":"normal attribution gate required Huihong, the already-structured later quoter, to be named in the public AttributionNote; exact note-only config correction on the original clock"')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
