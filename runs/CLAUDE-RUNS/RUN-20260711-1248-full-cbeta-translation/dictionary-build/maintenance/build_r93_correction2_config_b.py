#!/usr/bin/env python3
from pathlib import Path

base = Path(__file__).with_name("build_r93_config_b.py")
source = base.read_text(encoding="utf-8")
anchor = 'source=source.replace("R84","R93").replace("r84","r93")'
extra = anchor + '''
for old,new in {
"non-iriya-v7-depth-regeneration-r93-timegate-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-timegate-root.json",
"non-iriya-v7-depth-regeneration-r93-constructor-selection-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-constructor-selection-b.json",
"non-iriya-v7-depth-regeneration-r93-research-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-research-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-config-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-constructor-config-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-command-audit-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-constructor-command-audit-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-checkpoint-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-constructor-checkpoint-b.json",
"non-iriya-v7-depth-regeneration-r93-engine-first-product-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-engine-first-product-b.json",
"non-iriya-v7-depth-regeneration-r93-preclosure-report-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-preclosure-report-b.json",
"non-iriya-v7-depth-regeneration-r93-construction-manifest-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-construction-manifest-b.json",
"non-iriya-v7-depth-regeneration-r93-closure-b.json":"non-iriya-v7-depth-regeneration-r93-correction2-closure-b.json"}.items():
    source=source.replace(old,new)
source=source.replace(
    'str(ROOT/"fresh-build/entries")',
    'str(ROOT/"fresh-build/r93-correction2/entries")')
'''
source = source.replace(anchor, extra)
exec(compile(source, str(base), "exec"), {
    "__name__": "__main__", "__file__": str(base)
})
