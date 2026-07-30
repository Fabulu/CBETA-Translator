#!/usr/bin/env python3
"""Draft R92 retry2 terms installer; do not execute without final root authority."""
from pathlib import Path
template_path=Path(__file__).with_name("install_r90_terms_atomic_root.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R90","R92").replace("r90","r92-retry2")
source=source.replace(
 "non-iriya-v7-depth-regeneration-r92-retry2-release-authority-bindings-b.json",
 "non-iriya-v7-depth-regeneration-r92-retry2-release-authority-bindings-root.json")
source=source.replace(
 'BINDINGS_SHA = "0ff9a019466e35c937a57f31ad5ac7c51d1cd85650c636c568e9d100b534a029"',
 'BINDINGS_SHA = "45592db47df1db8f370c3cb0e0e4cebaaf22aa6260f5a8a54d62271e2a78e330"')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
