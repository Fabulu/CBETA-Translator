#!/usr/bin/env python3
"""Draft R92 retry2 public installer; do not execute without final root authority."""
from pathlib import Path
template_path=Path(__file__).with_name("install_r90_publication_root.py")
source=template_path.read_text(encoding="utf-8")
source=source.replace("R90","R92 retry2").replace("r90","r92-retry2")
start=source.index("PRODUCTS = {")
end=source.index("\n}\nEXPECTED_COUNT",start)+2
products='''PRODUCTS = {
    "t_219099a33daa": "1851fa3d1be027706c496dfe2c5ba1d30449f5998fc96ba75cf4622d8b14931b",
    "t_21a3463bc0db": "7bc95a32e9d7dd9a99aa10dc083c0486ab9f1e973665b26f062501b9e71768fa",
    "t_21b44f051c7a": "2304e8a688fded4103efb27daa4de6dcb3b9bc348dbfe0e3c0663242ffebe5d3",
}'''
source=source[:start]+products+source[end:]
exec(compile(source,str(template_path),"exec"),{"__name__":"__main__","__file__":str(template_path)})
