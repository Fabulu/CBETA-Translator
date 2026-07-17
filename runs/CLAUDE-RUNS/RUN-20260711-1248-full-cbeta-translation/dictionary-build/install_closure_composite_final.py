#!/usr/bin/env python3
import hashlib,json,os,shutil
from pathlib import Path
from compile_evidence_draft import compile_draft
H=Path(__file__).resolve().parent;M=H/'maintenance';S=M/'closure-baseline-staging-20260716/entries';F=H/'fresh-build/entries';T=H/'terms';R=M/'closure-composite-final-install-receipt.json';OUT=M/'closure-composite-final-install-ledger.json';BACK=M/'closure-composite-final-install-backup';EXPECTED='615aef6f5b8096640d368c5597828faa645415ca75b469af0f1e479638828f48'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rb(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def atomic(p,b):p.parent.mkdir(parents=True,exist_ok=True);t=p.with_suffix(p.suffix+'.tmp');t.write_bytes(b);os.replace(t,p)
if sha(R)!=EXPECTED:raise SystemExit('authorization receipt hash mismatch')
receipt=json.load(open(R));
if not receipt.get('installAuthorized') or len(receipt['entryHashes'])!=1204:raise SystemExit('install not authorized')
rows=receipt['entryHashes'];pre=[]
# Whole-scope preflight before first mutation.
for r in rows:
 i=r['id'];ep=S/i/'entry.v2.json';wp=ep.with_name('evidence.draft.json')
 if sha(ep)!=r['entrySha256'] or sha(wp)!=r['worksheetSha256']:raise SystemExit(f'{i}: staged hash drift')
 built,errors=compile_draft(json.load(open(wp)))
 if errors or rb(built)!=ep.read_bytes():raise SystemExit(f'{i}: compiler parity drift')
 pre.append((r,ep,wp))
done=[]
try:
 for n,(r,ep,wp) in enumerate(pre,1):
  i=r['id'];fd=F/i;td=T/i;bd=BACK/i;bd.mkdir(parents=True,exist_ok=True)
  targets={'freshEntry':fd/'entry.v2.json','freshWorksheet':fd/'evidence.draft.json','termsEntry':td/'entry.v2.json','termsStatus':td/'STATUS'}
  existed={}
  for k,p in targets.items():
   existed[k]=p.exists()
   if p.exists():shutil.copy2(p,bd/k)
  atomic(targets['freshEntry'],ep.read_bytes());atomic(targets['freshWorksheet'],wp.read_bytes());atomic(targets['termsEntry'],ep.read_bytes());atomic(targets['termsStatus'],b'done\n')
  if sha(targets['freshEntry'])!=r['entrySha256'] or sha(targets['freshWorksheet'])!=r['worksheetSha256'] or sha(targets['termsEntry'])!=r['entrySha256']:raise RuntimeError(f'{i}: post-write hash mismatch')
  done.append({'id':i,'entrySha256':r['entrySha256'],'worksheetSha256':r['worksheetSha256'],'preexisting':existed})
  if n%100==0 or n==1204:
   ledger={'schemaVersion':'closure-composite-install.v1','authorizationReceiptSha256':EXPECTED,'checkpoint':n,'installed':len(done),'rows':done};atomic(OUT,rb(ledger));print(json.dumps({'checkpoint':n,'installed':len(done)}),flush=True)
except Exception:
 # Roll back only rows written by this invocation, in reverse order.
 for row in reversed(done):
  i=row['id'];fd=F/i;td=T/i;bd=BACK/i
  for k,p in {'freshEntry':fd/'entry.v2.json','freshWorksheet':fd/'evidence.draft.json','termsEntry':td/'entry.v2.json','termsStatus':td/'STATUS'}.items():
   if row['preexisting'][k]:atomic(p,(bd/k).read_bytes())
   elif p.exists():p.unlink()
 raise
print(json.dumps({'installed':len(done),'ledgerSha256':sha(OUT)}))
