#!/usr/bin/env python3
"""Draft R91 active-terms installer; execution requires root authority."""
from pathlib import Path

template_path=Path(__file__).with_name("install_r90_terms_atomic_root.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R90","R91").replace("r90","r91")
source=source.replace(
    'BINDINGS_SHA = "0ff9a019466e35c937a57f31ad5ac7c51d1cd85650c636c568e9d100b534a029"',
    'BINDINGS_SHA = "dcd9bb72c8de0ca0ab7d7402e8fea267c209c3b6f8a29596587a6b45d40260ba"')
source=source.replace('"replacementCount": 2,\n    "createdCount": 1,',
                      '"replacementCount": 3,\n    "createdCount": 0,')
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
