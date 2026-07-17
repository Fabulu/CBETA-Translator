#!/usr/bin/env python3
"""Install only the independently sealed donor-exact worksheet subset."""
import argparse,hashlib,json,os,shutil,sys
from pathlib import Path
from compile_evidence_draft import compile_draft

H=Path(__file__).resolve().parent
M=H/'maintenance'
SOURCE=M/'closure-unresolved-worksheet-roundtrip-sync-by-b-20260717-final.json'
AUDIT=M/'closure-unresolved-worksheet-roundtrip-donor-final-independent-audit-by-c.json'
STAGE=M/'closure-baseline-staging-20260716/entries'
BACK=M/'closure-accepted-189-worksheet-install-backup'
OUT=M/'closure-accepted-189-worksheet-install-ledger.json'
BACK_MANIFEST=M/'closure-accepted-189-worksheet-install-backup-manifest.json'
SEALED='5af6cf0af712b46834bec9bc5e9508ab8f5d83f10e51ad229cad0f03750242a6'
AUDITED='a29270aa5af19066b3f79e4bcdacac5ff27248c39609f809b34aa97b163b4b43'
BACK_MANIFEST_SHA='2ead067ea8797e9715e4438a5e97334085fbd9c019a7dd89441c615a2add400a'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rendered(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def atomic(p,b):
 t=p.with_suffix(p.suffix+'.tmp');t.write_bytes(b);os.replace(t,p)
def rollback(rows,preflight_only=False):
 # Preflight the entire rollback before the first mutation.  Never overwrite
 # state which no longer equals the worksheet installed by this transaction.
 if not BACK_MANIFEST.exists() or sha(BACK_MANIFEST)!=BACK_MANIFEST_SHA:raise SystemExit('sealed backup manifest missing/hash mismatch')
 bm=json.load(open(BACK_MANIFEST))
 if bm.get('installLedgerSha256')!=sha(OUT):raise SystemExit('backup manifest/install ledger binding mismatch')
 backups={r['id']:r for r in bm['rows']}
 for row in rows:
  dest=STAGE/row['id']/'evidence.draft.json'
  if not dest.exists() or sha(dest)!=row['installedWorksheetSha256']:raise SystemExit(f"{row['id']}: rollback current-hash guard failed")
  if row['priorWorksheetExisted']:
   backup=H/row['backup']
   if not backup.exists():raise SystemExit(f"{row['id']}: rollback backup missing")
   if sha(backup)!=backups[row['id']]['backupSha256']:raise SystemExit(f"{row['id']}: rollback backup hash mismatch")
 if preflight_only:
  print(json.dumps({'rollbackPreflight':len(rows),'backupHashesVerified':sum(r['priorWorksheetExisted'] for r in rows),'mutations':0}));return
 for row in reversed(rows):
  dest=STAGE/row['id']/'evidence.draft.json'
  if row['priorWorksheetExisted']:atomic(dest,(H/row['backup']).read_bytes())
  else:dest.unlink()
 print(json.dumps({'rolledBack':len(rows),'entryWrites':0}))
ap=argparse.ArgumentParser();ap.add_argument('--rollback',action='store_true');ap.add_argument('--rollback-preflight-only',action='store_true');args=ap.parse_args()
if args.rollback or args.rollback_preflight_only:
 if not OUT.exists():raise SystemExit('no install ledger to roll back')
 rollback(json.load(open(OUT))['rows'],preflight_only=args.rollback_preflight_only);raise SystemExit(0)
if sha(SOURCE)!=SEALED or sha(AUDIT)!=AUDITED:raise SystemExit('sealed source/audit hash mismatch')
audit=json.load(open(AUDIT));ledger=json.load(open(SOURCE))
if audit.get('verdict')!='ACCEPT_DONOR_ONLY_SUBSET' or audit.get('sealedLedgerSha256')!=SEALED:raise SystemExit('independent acceptance missing')
accepted=[r for r in ledger['rows'] if r['status']=='ROUNDTRIP_BYTE_EXACT']
if len(accepted)!=189:raise SystemExit(f'accepted count {len(accepted)}')
old=json.load(open(OUT)) if OUT.exists() else {'rows':[]};done={r['id']:r for r in old['rows']}
for n,row in enumerate(accepted,1):
 i=row['id'];entry=STAGE/i/'entry.v2.json';dest=entry.with_name('evidence.draft.json');source=Path(row['worksheet'])
 if sha(entry)!=row['entrySha256']:raise SystemExit(f'{i}: current entry hash mismatch')
 if not source.exists() or sha(source)!=row['worksheetSha256']:raise SystemExit(f'{i}: sealed worksheet missing/hash mismatch')
 draft=json.loads(source.read_text(encoding='utf-8-sig'));built,errors=compile_draft(draft)
 if errors or rendered(built)!=entry.read_bytes():raise SystemExit(f'{i}: canonical compile mismatch {errors}')
 if i in done:
  if not dest.exists() or sha(dest)!=done[i]['installedWorksheetSha256']:raise SystemExit(f'{i}: installed hash drift')
  continue
 bd=BACK/i;bd.mkdir(parents=True,exist_ok=True)
 existed=dest.exists()
 if existed:shutil.copy2(dest,bd/'evidence.draft.json')
 atomic(dest,source.read_bytes())
 if sha(dest)!=row['worksheetSha256']:raise SystemExit(f'{i}: post-write hash mismatch')
 done[i]={'id':i,'entrySha256':row['entrySha256'],'sealedWorksheetSha256':row['worksheetSha256'],'installedWorksheetSha256':sha(dest),'priorWorksheetExisted':existed,'backup':str((bd/'evidence.draft.json').relative_to(H)) if existed else None,'canonicalCompileByteIdentical':True}
 if n%50==0 or n==len(accepted):
  out={'schemaVersion':'closure-accepted-donor-worksheet-install.v1','sealedSource':str(SOURCE.relative_to(H)),'sealedSourceSha256':SEALED,'independentAudit':str(AUDIT.relative_to(H)),'independentAuditSha256':AUDITED,'stagingOnly':True,'entryWrites':0,'checkpoint':n,'installed':len(done),'rows':[done[r['id']] for r in accepted if r['id'] in done]}
  atomic(OUT,rendered(out));print(json.dumps({'checkpoint':n,'installed':len(done),'ledgerSha256':sha(OUT)}),flush=True)
print(json.dumps({'installed':len(done),'ledgerSha256':sha(OUT)}))
