#!/usr/bin/env python3
"""Execute the reviewed R92 builder with retry-scoped mutable artifact paths."""
from pathlib import Path

base=Path(__file__).with_name("build_r92_config_b.py")
source=base.read_text(encoding="utf-8")
needle='source=source.replace("R84","R92").replace("r84","r92")'
replacement=needle+'''
for old,new in {
 "non-iriya-v7-depth-regeneration-r92-timegate-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-timegate-root.json",
 "non-iriya-v7-depth-regeneration-r92-constructor-selection-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-constructor-selection-b.json",
 "non-iriya-v7-depth-regeneration-r92-research-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-research-b.json",
 "non-iriya-v7-depth-regeneration-r92-constructor-config-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-constructor-config-b.json",
 "non-iriya-v7-depth-regeneration-r92-constructor-command-audit-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-constructor-command-audit-b.json",
 "non-iriya-v7-depth-regeneration-r92-constructor-checkpoint-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-constructor-checkpoint-b.json",
 "non-iriya-v7-depth-regeneration-r92-engine-first-product-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-engine-first-product-b.json",
 "non-iriya-v7-depth-regeneration-r92-preclosure-report-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-preclosure-report-b.json",
 "non-iriya-v7-depth-regeneration-r92-construction-manifest-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-construction-manifest-b.json",
 "non-iriya-v7-depth-regeneration-r92-closure-b.json":"non-iriya-v7-depth-regeneration-r92-retry1-closure-b.json",
}.items(): source=source.replace(old,new)
'''
if needle not in source:
    raise RuntimeError("reviewed R92 builder substitution anchor missing")
source=source.replace(needle,replacement)
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
