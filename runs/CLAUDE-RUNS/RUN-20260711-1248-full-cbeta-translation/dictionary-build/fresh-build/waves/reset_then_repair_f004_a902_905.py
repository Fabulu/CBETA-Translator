#!/usr/bin/env python3
import subprocess,sys
from pathlib import Path
H=Path(__file__).resolve().parent;R=H.parent.parent
keep=R/'fresh-build/entries/t_cc68e32cf1b4'; names=['evidence.draft.json','entry.v2.json','compile-report.json','WORK.md','STATUS']; saved={n:(keep/n).read_bytes() for n in names}
subprocess.run([sys.executable,str(H/'author_f004_a901_905.py')],check=True)
for n,b in saved.items():(keep/n).write_bytes(b)
subprocess.run([sys.executable,str(H/'repair_f004_a902_905_review.py')],check=True)
