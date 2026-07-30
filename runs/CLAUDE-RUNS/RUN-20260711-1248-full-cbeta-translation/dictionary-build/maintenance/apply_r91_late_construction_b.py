#!/usr/bin/env python3
"""Authorized R91 same-scope bounded late construction."""
from pathlib import Path

template_path=Path(__file__).with_name("apply_r84_late_construction_b.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R84","R91").replace("r84","r91")
source=source.replace(
 'IDS=["t_1cec9c4c3c40","t_1cfa8b8aa2a3","t_1d0056511f4d"]',
 'IDS=["t_21170b1b9a8d","t_211c871daa1f","t_218e4815d84a"]')
source=source.replace(
 '"watchdogFailure":"adjudicated config late: 545.858s"',
 '"watchdogFailure":"research audit failed closed on a canonical argv mismatch; the root-authorized late-research artifact preserved scope but was not constructor-consumable because it retained hardPass:false and omitted the normal schedule fields"')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
