#!/usr/bin/env python3
"""Hash-bound authoritative inventory of every staged entry/worksheet pair."""
import hashlib,json,os
from pathlib import Path
from compile_evidence_draft import compile_draft
H=Path(__file__).resolve().parent;STAGE=H/'maintenance/closure-baseline-staging-20260716/entries';OUT=H/'maintenance/closure-staging-worksheet-inventory-final.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rendered(x):return (json.dumps(x,ensure_ascii=False,indent=2)+'\n').encode()
def pollution(x,p='$'):
 out=[]
 if isinstance(x,dict):
  for k,v in x.items():
   q=f'{p}.{k}'
   if k.startswith('Draft') or k=='ExplanationParts':out.append(q)
   out+=pollution(v,q)
 elif isinstance(x,list):
  for i,v in enumerate(x):out+=pollution(v,f'{p}[{i}]')
 return out
rows=[];fail=[]
for d in sorted(x for x in STAGE.iterdir() if x.is_dir()):
 ep=d/'entry.v2.json';wp=d/'evidence.draft.json'
 if not ep.exists() or not wp.exists():fail.append({'id':d.name,'kind':'missing-pair'});continue
 e=json.loads(ep.read_text(encoding='utf-8-sig'));w=json.loads(wp.read_text(encoding='utf-8-sig'));built,errors=compile_draft(w);dirty=pollution(e)
 exact=not errors and rendered(built)==ep.read_bytes();identity=e.get('Id')==d.name and (w.get('Entry') or {}).get('Id')==d.name
 if not exact or dirty or not identity:fail.append({'id':d.name,'kind':'validation','compileErrors':errors,'byteExact':exact,'readerPollution':dirty,'identityExact':identity})
 rows.append({'id':d.name,'entrySha256':sha(ep),'worksheetSha256':sha(wp),'canonicalCompileByteIdentical':exact,'readerResearchFields':dirty,'identityExact':identity})
receipt={'schemaVersion':'closure-staging-worksheet-inventory.v1','stagingRoot':str(STAGE.relative_to(H)),'compiler':'compile_evidence_draft.py','compilerSha256':sha(H/'compile_evidence_draft.py'),'entries':len(rows),'entryWorksheetPairs':sum(1 for r in rows if r['canonicalCompileByteIdentical']),'readerPollutionCount':sum(len(r['readerResearchFields']) for r in rows),'identityFailures':sum(not r['identityExact'] for r in rows),'hardFailures':len(fail),'hardPass':len(rows)==1204 and not fail,'failures':fail,'rows':rows}
t=OUT.with_suffix('.json.tmp');t.write_bytes(rendered(receipt));os.replace(t,OUT);print(json.dumps({k:receipt[k] for k in ('entries','entryWorksheetPairs','readerPollutionCount','identityFailures','hardFailures','hardPass')}));raise SystemExit(0 if receipt['hardPass'] else 1)
