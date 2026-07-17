#!/usr/bin/env python3
"""Fail-closed, rollback-safe installer for the accepted final176 cohort.

Preparation is harmless; execution requires --authorize plus the exact manifest
SHA.  This installer deliberately does not merge dictionaries or mutate lineage.
"""
from __future__ import annotations

import argparse, fcntl, hashlib, json, os, shutil, tempfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[3]
LIVE = ROOT / "terms"
DEFAULT_MANIFEST = ROOT / "maintenance/post-current-investigation720-final176-reconciliation-draft.json"
LINEAGE = REPO / "Assets/Data/lineage-masters.json"
PROTECTED_LINEAGE_SHA256 = "33c008e98468ecab8b89bbb6dcd6008fba631bee01f4909091c7d2887dce1ded"
LOCK = ROOT / "maintenance/.final176-install.lock"

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))

def atomic_json(path: Path, value: dict) -> None:
    data=(json.dumps(value,ensure_ascii=False,indent=2)+"\n").encode()
    tmp=path.with_suffix(path.suffix+".tmp"); tmp.write_bytes(data); os.replace(tmp,path)

def binding_passes(binding: dict) -> bool:
    """Require a SHA-bound independent receipt with an explicit PASS verdict."""
    required={"path","sha256"}
    if not required <= binding.keys(): return False
    path=ROOT/binding["path"]
    if not path.is_file() or sha(path)!=binding["sha256"]: return False
    evidence=load(path)
    verdict=str(binding.get("decision") or binding.get("verdict") or evidence.get("verdict") or evidence.get("disposition") or evidence.get("status") or "").upper()
    hard=evidence.get("hardPass")
    return verdict in {"PASS","ACCEPT","ACCEPTED","KEEP","COMPLETE-INDEPENDENT-REREVIEW-ACCEPTED"} and hard is not False

def main() -> int:
    ap=argparse.ArgumentParser()
    ap.add_argument("--manifest",type=Path,default=DEFAULT_MANIFEST)
    ap.add_argument("--manifest-sha256",required=True)
    ap.add_argument("--authorize",action="store_true")
    args=ap.parse_args()
    if not args.authorize: raise SystemExit("install requires explicit --authorize")
    manifest=args.manifest.resolve()
    if sha(manifest)!=args.manifest_sha256: raise SystemExit("manifest SHA mismatch")
    plan=load(manifest); failures=[]
    rows=plan.get("candidates") or []
    accepted=[r for r in rows if r.get("disposition")=="ACCEPTED"]
    rejected=[r for r in rows if r.get("disposition")=="REJECTED"]
    if plan.get("publicationReady") is not True: failures.append("manifest is not publicationReady=true")
    if plan.get("mode") not in {"INSTALL-AUTHORIZED","PUBLICATION-READY"}: failures.append("manifest mode does not authorize install")
    if len(rows)!=176 or len(accepted)!=152 or len(rejected)!=24: failures.append("expected final176 totals 176/152/24")
    ids=[r.get("id") for r in rows]; terms=[r.get("term") for r in rows]
    if len(set(ids))!=176 or len(set(terms))!=176: failures.append("candidate IDs/headwords are not unique")
    if sha(LINEAGE)!=PROTECTED_LINEAGE_SHA256: failures.append("protected lineage hash drift")

    live_ids={}; live_terms={}
    for status in LIVE.glob("*/STATUS"):
        if status.read_text(encoding="utf-8").strip()!="done": continue
        entry=status.parent/"entry.v2.json"
        if not entry.is_file(): failures.append(f"installed STATUS=done directory lacks entry: {status.parent.name}"); continue
        data=load(entry); live_ids[data["Id"]]=status.parent; live_terms[data["SourceTerm"]]=data["Id"]
    for row in accepted:
        src=ROOT/row.get("entryPath","")
        if not src.is_file(): failures.append(f"missing accepted source {row.get('id')}"); continue
        if sha(src)!=row.get("currentEntrySha256"): failures.append(f"current SHA drift {row['id']}")
        data=load(src)
        if data.get("Id")!=row["id"] or data.get("SourceTerm")!=row["term"]: failures.append(f"source identity mismatch {row['id']}")
        bindings=row.get("independentPassBindings") or []
        if not bindings and row.get("independentPassReceipt"):
            bindings=[row["independentPassReceipt"]]
        if (not bindings or not all(binding_passes(b) for b in bindings)
                or not all(b.get("reviewedEntrySha256") == row.get("currentEntrySha256") for b in bindings)):
            failures.append(f"missing/drifted independent PASS binding {row['id']}")
        if row["id"] in live_ids: failures.append(f"target ID collision {row['id']}")
        if row["term"] in live_terms: failures.append(f"target headword collision {row['term']} -> {live_terms[row['term']]}")
    # Rejected rows are never staged, even if stale fresh-build files exist.
    if any(r.get("id") in {x["id"] for x in accepted} for r in rejected): failures.append("accepted/rejected overlap")
    if failures: raise SystemExit("preflight failed:\n"+"\n".join(failures))

    LOCK.parent.mkdir(parents=True,exist_ok=True)
    fd=os.open(LOCK,os.O_RDWR|os.O_CREAT,0o600); fcntl.flock(fd,fcntl.LOCK_EX)
    tag=args.manifest_sha256[:16]
    stage=Path(tempfile.mkdtemp(prefix=f".final176-stage-{tag}-",dir=LIVE.parent))
    backup=ROOT/f"maintenance/final176-install-backup-{tag}"
    receipt=ROOT/f"maintenance/final176-install-receipt-{tag}.json"
    if backup.exists() or receipt.exists(): raise SystemExit("transaction path already exists; inspect prior run")
    backup.mkdir(); installed=[]; replaced=[]
    try:
        for row in accepted:
            src=(ROOT/row["entryPath"]).parent; dst=stage/row["id"]
            shutil.copytree(src,dst); (dst/"STATUS").write_text("done\n",encoding="utf-8")
            if sha(dst/"entry.v2.json")!=row["currentEntrySha256"]: raise RuntimeError(f"staging SHA mismatch {row['id']}")
        if sha(LINEAGE)!=PROTECTED_LINEAGE_SHA256: raise RuntimeError("lineage drift immediately before promotion")
        for row in accepted:
            ident=row["id"]; target=LIVE/ident
            # Preflight forbids this, but preserve rollback safety against a race.
            if target.exists(): os.replace(target,backup/ident); replaced.append(ident)
            os.replace(stage/ident,target); installed.append(ident)
            if sha(target/"entry.v2.json")!=row["currentEntrySha256"] or (target/"STATUS").read_text().strip()!="done": raise RuntimeError(f"post-install verification failed {ident}")
        if sha(LINEAGE)!=PROTECTED_LINEAGE_SHA256: raise RuntimeError("lineage changed during install")
        payload={"schemaVersion":"final176-atomic-install-receipt.v1","status":"INSTALLED_NOT_MERGED","generatedUtc":datetime.now(timezone.utc).isoformat(),"manifestPath":str(manifest.relative_to(ROOT)),"manifestSha256":args.manifest_sha256,"acceptedInstalled":len(installed),"rejectedInstalled":0,"installedIds":installed,"replacedExisting":replaced,"backupPath":str(backup.relative_to(ROOT)),"protectedLineagePath":str(LINEAGE.relative_to(REPO)),"protectedLineageSha256":PROTECTED_LINEAGE_SHA256,"lineageWrites":0,"mergeExecuted":False,"hardPass":True}
        atomic_json(receipt,payload); print(receipt)
    except BaseException:
        for ident in reversed(installed):
            target=LIVE/ident
            if target.exists(): shutil.rmtree(target)
            if (backup/ident).exists(): os.replace(backup/ident,target)
        raise
    finally:
        shutil.rmtree(stage,ignore_errors=True); os.close(fd)
    return 0

if __name__=="__main__": raise SystemExit(main())
