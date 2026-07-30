#!/usr/bin/env python3
"""Draft R91 public installer; execution requires a built stage and root authority."""
from pathlib import Path

template_path=Path(__file__).with_name("install_r90_publication_root.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R90","R91").replace("r90","r91")
start=source.index("PRODUCTS = {")
end=source.index("\n}\nEXPECTED_COUNT",start)+2
products='''PRODUCTS = {
    "t_21170b1b9a8d": "4a1f2f2f735fd5b42e8cc81a1ab0f1951cfd761fbb7d153c332d0abe3fe16dcb",
    "t_211c871daa1f": "6db85bc0d81f96a7468c40ffe8272c4bb0e33893e5371772dd2b8e1f06539a45",
    "t_218e4815d84a": "ab3665fec395befb6cfb35e0c605ab5f535e1eac300e9e2756d5094cf048183a",
}'''
source=source[:start]+products+source[end:]
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
