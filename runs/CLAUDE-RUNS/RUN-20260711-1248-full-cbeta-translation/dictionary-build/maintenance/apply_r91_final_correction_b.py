#!/usr/bin/env python3
"""Final R91 changed-coordinate worksheet compile."""
from pathlib import Path

template_path=Path(__file__).with_name("apply_r91_review_corrections_b.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace(
    'non-iriya-v7-depth-regeneration-r91-review-correction-b.json',
    'non-iriya-v7-depth-regeneration-r91-final-correction-b.json')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
