#!/usr/bin/env python3
"""Apply the root-authorized R91 worksheet corrections and compile once."""
import hashlib, json, subprocess, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT))
from atomic_write import atomic_write_json, atomic_write_text
from maintenance.generic_bounded_constructor import (
    verify_actor_closure, verify_whole_config_preclosure, canonical_compile_prewrite
)

M=ROOT/"maintenance"
CFG=M/"non-iriya-v7-depth-regeneration-r91-constructor-config-b.json"
OUT=M/"non-iriya-v7-depth-regeneration-r91-review-correction-b.json"
IDS=["t_21170b1b9a8d","t_218e4815d84a"]

def read(path): return json.loads(path.read_text(encoding="utf-8"))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

if OUT.exists():
    raise SystemExit("immutable correction receipt already exists")
config=read(CFG)
verify_actor_closure(config)
verify_whole_config_preclosure(config)
projected=canonical_compile_prewrite(config)
rows=[]
for row in config["entries"]:
    if row["id"] not in IDS:
        continue
    entry_dir=ROOT/"fresh-build/entries"/row["id"]
    dossier=entry_dir/"source-dossier.json"
    draft=entry_dir/"evidence.draft.json"
    product=entry_dir/"entry.v2.json"
    report=entry_dir/"evidence-compile-report.json"
    atomic_write_json(dossier,row["sourceDossier"])
    worksheet=row["evidenceDraft"]
    worksheet["EvidenceTransport"]["DossierSha256"]=sha(dossier)
    atomic_write_json(draft,worksheet)
    subprocess.run([
      sys.executable,str(ROOT/"compile_evidence_draft.py"),str(draft),
      "--output",str(product),"--report",str(report),"--new-entry"
    ],cwd=ROOT,check=True)
    if read(product) != projected[row["id"]]:
        raise RuntimeError(f"{row['id']}: compiler projection mismatch")
    work=entry_dir/"WORK.md"
    if row["id"]=="t_218e4815d84a":
        atomic_write_text(work,work.read_text(encoding="utf-8")+
          "\nreview-correction: Dropped misattributed redundant J25 witness; restored frozen context roles.\n"
          "depth-exception: Root authorized six independent families after frozen-candidate exhaustion; "
          "X78 duplicates Zhaozhou, B25 duplicates Huanglong, and D51 is excluded Japanese material.\n")
    rows.append({"id":row["id"],"entrySha256":sha(product),
      "worksheetSha256":sha(draft),"dossierSha256":sha(dossier),
      "compileReportSha256":sha(report)})
atomic_write_json(OUT,{"schemaVersion":"r91-review-correction.v1","cohort":"R91",
  "authorization":"Root-authorized worksheet correction only; no new extraction or product hand edit.",
  "configSha256":sha(CFG),"rows":rows,"hardPass":True,"publicMutation":False})
print(json.dumps(read(OUT),ensure_ascii=False))
