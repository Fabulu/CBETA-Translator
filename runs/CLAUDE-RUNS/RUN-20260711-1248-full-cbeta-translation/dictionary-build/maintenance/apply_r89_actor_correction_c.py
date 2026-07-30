#!/usr/bin/env python3
"""Authorized R89 actor-coordinate correction."""
from pathlib import Path
template_path=Path(__file__).with_name("apply_r84_late_construction_b.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R84","R89").replace("r84","r89")
source=source.replace(
 'non-iriya-v7-depth-regeneration-r89-late-construction-correction-b.json',
 'non-iriya-v7-depth-regeneration-r89-actor-correction-c.json')
source=source.replace(
 'IDS=["t_1cec9c4c3c40","t_1cfa8b8aa2a3","t_1d0056511f4d"]',
 'IDS=["t_1e41b014d80e","t_1f3653f30389","t_1fe4eac13d6e"]')
source=source.replace(
 '"watchdogFailure":"adjudicated config late: 545.858s"',
 '"watchdogFailure":"authorized exact-coordinate repair: Lin Xuan remains MasterName null and is structured as the named non-master questioner; Tianyin remains respondent/record owner only; original clock preserved"')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
