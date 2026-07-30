#!/usr/bin/env python3
"""R91 two-note worksheet correction compile."""
from pathlib import Path

template_path=Path(__file__).with_name("apply_r91_review_corrections_b.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace(
    'non-iriya-v7-depth-regeneration-r91-review-correction-b.json',
    'non-iriya-v7-depth-regeneration-r91-note-correction-b.json')
source=source.replace(
    'IDS=["t_21170b1b9a8d","t_218e4815d84a"]',
    'IDS=["t_218e4815d84a"]')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
