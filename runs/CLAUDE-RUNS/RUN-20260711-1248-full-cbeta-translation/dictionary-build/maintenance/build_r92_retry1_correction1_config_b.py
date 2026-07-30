#!/usr/bin/env python3
"""Build corrected R92 retry artifacts under distinct immutable paths."""
from pathlib import Path
base=Path(__file__).with_name("build_r92_retry1_config_b.py")
source=base.read_text(encoding="utf-8")
source=source.replace("retry1-constructor-selection","retry1-correction1-constructor-selection")
source=source.replace("retry1-research","retry1-correction1-research")
source=source.replace("retry1-constructor-config","retry1-correction1-constructor-config")
source=source.replace("retry1-constructor-command-audit","retry1-correction1-constructor-command-audit")
source=source.replace("retry1-constructor-checkpoint","retry1-correction1-constructor-checkpoint")
source=source.replace("retry1-engine-first-product","retry1-correction1-engine-first-product")
source=source.replace("retry1-preclosure-report","retry1-correction1-preclosure-report")
source=source.replace("retry1-construction-manifest","retry1-correction1-construction-manifest")
source=source.replace("retry1-closure","retry1-correction1-closure")
exec(compile(source,str(base),"exec"),{"__name__":"__main__","__file__":str(base)})
