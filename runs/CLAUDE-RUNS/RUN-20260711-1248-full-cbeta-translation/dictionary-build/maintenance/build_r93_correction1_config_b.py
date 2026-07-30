#!/usr/bin/env python3
from pathlib import Path
base=Path(__file__).with_name("build_r93_config_b.py")
source=base.read_text()
anchor='source=source.replace("R84","R93").replace("r84","r93")'
extra=anchor+'''
for old,new in {
"non-iriya-v7-depth-regeneration-r93-timegate-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-timegate-root.json",
"non-iriya-v7-depth-regeneration-r93-constructor-selection-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-constructor-selection-b.json",
"non-iriya-v7-depth-regeneration-r93-research-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-research-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-config-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-constructor-config-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-command-audit-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-constructor-command-audit-b.json",
"non-iriya-v7-depth-regeneration-r93-constructor-checkpoint-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-constructor-checkpoint-b.json",
"non-iriya-v7-depth-regeneration-r93-engine-first-product-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-engine-first-product-b.json",
"non-iriya-v7-depth-regeneration-r93-preclosure-report-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-preclosure-report-b.json",
"non-iriya-v7-depth-regeneration-r93-construction-manifest-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-construction-manifest-b.json",
"non-iriya-v7-depth-regeneration-r93-closure-b.json":"non-iriya-v7-depth-regeneration-r93-correction1-closure-b.json"}.items(): source=source.replace(old,new)
'''
source=source.replace(anchor,extra)
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
