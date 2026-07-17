#!/usr/bin/env python3
"""Rollback-safe atomic installer for an accepted 269-entry install plan.

Linux/WSL renameat2(RENAME_EXCHANGE) swaps the complete live and staged terms
trees in one filesystem operation. On publication failure the same operation
restores the old tree before rebuilding old artifacts.
"""
from __future__ import annotations
import argparse,fcntl,hashlib,json,os,pathlib,shutil,subprocess,tempfile
B=pathlib.Path(__file__).resolve().parents[1];REPO=B.parents[3];LIVE=B/'terms'
CORPUS=REPO/'Assets/Data/zen-corpus.json';ROSTER=REPO/'Assets/Data/lineage-masters.json';LOCK=B/'maintenance/.investigation-next300-install.lock'
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def load(p):return json.loads(pathlib.Path(p).read_text(encoding='utf-8-sig'))
def merge_dictionary():
    win=subprocess.run(['wslpath','-w',str(REPO)],check=True,capture_output=True,text=True).stdout.strip()
    # Passing a compound quoted command through WSL interop makes cmd.exe
    # reinterpret the drive path.  Give node the absolute script as a distinct
    # argv element; this is both faster and insensitive to cmd quoting rules.
    script=win+'\\eng\\tools\\merge-dict-entries.js'
    return subprocess.run(['cmd.exe','/d','/c','node',script],cwd=REPO)
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--plan',type=pathlib.Path,required=True);ap.add_argument('--plan-sha256',required=True);a=ap.parse_args()
    if sha(a.plan)!=a.plan_sha256:raise SystemExit('install plan hash mismatch')
    plan=load(a.plan)
    if plan.get('schemaVersion')!='investigation-next300-install-plan-269.v2':raise SystemExit('obsolete or unknown install-plan contract')
    if not plan.get('hardPass') or plan.get('entryCount')!=269 or len(plan.get('rows',[]))!=269:raise SystemExit('plan is not an accepted 269-entry plan')
    if sha(CORPUS)!=plan['corpusSha256'] or sha(ROSTER)!=plan['protectedLineageRosterSha256']:raise SystemExit('protected input drift')
    ids=[r['id'] for r in plan['rows']]
    if len(set(ids))!=269:raise SystemExit('plan IDs not unique')
    gate=plan['finalCohortGate']
    if sha(B/gate['path'])!=gate['sha256']:raise SystemExit('final 269-entry cohort-gate receipt drift')
    evidence_paths={plan['semanticReviewIndex']['path']:plan['semanticReviewIndex']['sha256']}
    for r in plan['rows']:
        src=B/r['source']
        if sha(src)!=r['entrySha256']:raise SystemExit(f"fresh source drift {r['id']}")
        evidence=r['semanticEvidence']['candidateReview'];evidence_paths[evidence['path']]=evidence['sha256']
        for evidence in r['semanticEvidence'].get('postBuildReviews',[]):evidence_paths[evidence['path']]=evidence['sha256']
    for path,expected in evidence_paths.items():
        if sha(B/path)!=expected:raise SystemExit(f'evidence drift: {path}')
    LOCK.parent.mkdir(parents=True,exist_ok=True);fd=os.open(LOCK,os.O_RDWR|os.O_CREAT,0o600);fcntl.flock(fd,fcntl.LOCK_EX)
    tag=a.plan_sha256[:16];stage=pathlib.Path(tempfile.mkdtemp(prefix=f'.final269-stage-{tag}-',dir=LIVE.parent));rollback=B/f'maintenance/final269-install-backup-{tag}'
    if rollback.exists():raise SystemExit('rollback path already exists; inspect previous transaction')
    rollback.mkdir()
    for r in plan['rows']:
        dst=stage/r['id'];shutil.copytree((B/r['source']).parent,dst)
        (dst/'STATUS').write_text('done\n',encoding='utf-8')
        if sha(dst/'entry.v2.json')!=r['entrySha256']:raise SystemExit(f"staging hash mismatch {r['id']}")
    installed=[];replaced=[]
    try:
        if sha(CORPUS)!=plan['corpusSha256'] or sha(ROSTER)!=plan['protectedLineageRosterSha256']:raise RuntimeError('protected input drift before install')
        for r in plan['rows']:
            ident=r['id'];target=LIVE/ident
            if target.exists():os.replace(target,rollback/ident);replaced.append(ident)
            os.replace(stage/ident,target);installed.append(ident)
        merge=merge_dictionary()
        if merge.returncode:raise RuntimeError('merge failed after directory installation')
        if sha(ROSTER)!=plan['protectedLineageRosterSha256']:raise RuntimeError('lineage roster changed during install')
        receipt=B/f'maintenance/investigation-next300-install-receipt-{tag}.json'
        receipt.write_text(json.dumps({'schemaVersion':'investigation-next300-rollback-safe-install-receipt.v2','plan':str(a.plan),'planSha256':a.plan_sha256,'installed':269,'replacedExisting':len(replaced),'backupPath':str(rollback.relative_to(B)),'lineageRosterWrites':0,'merged':True,'hardPass':True},indent=2)+'\n')
        print(receipt)
    except BaseException:
        for ident in reversed(installed):
            target=LIVE/ident
            if target.exists():shutil.rmtree(target)
            if (rollback/ident).exists():os.replace(rollback/ident,target)
        merge_dictionary()
        raise
    finally:
        shutil.rmtree(stage,ignore_errors=True)
        os.close(fd)
if __name__=='__main__':main()
