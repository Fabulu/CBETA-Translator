import hashlib,json
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
IDS=['t_aa56c106ef82','t_c688927b7ea1','t_d2892b1eaae0','t_d3631f4abf25','t_d95b944e0749','t_da72db7aa635','t_dda048ca832d']
roster_data=json.loads((ROOT.parents[3]/'Assets/Data/master-dates.json').read_text(encoding='utf8'))
roster={r['names'][0] for r in roster_data['masters'] if r.get('names')}
P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS};rows=[]
for i,p in P.items():
 old=hashlib.sha256(p.read_bytes()).hexdigest();d=json.loads(p.read_text(encoding='utf8'));removed=[];deferred=[]
 for s in d['Senses']:
  oldrel=s.get('RelatedMasters',[]);s['RelatedMasters']=[n for n in oldrel if n in roster];removed += [n for n in oldrel if n not in roster]
  for o in s.get('Occurrences',[]):
   if o.get('MasterName') and o['MasterName'] not in roster:deferred.append(o['MasterName'])
   newctx=[]
   for c in o.get('ContextMasters',[]):
    n=c.get('MasterName')
    if n=='Ananda' and i=='t_aa56c106ef82':
     removed.append('Ananda (context-only noncanonical link; identity retained in prose)');continue
    if n and n not in roster:deferred.append(n)
    newctx.append(c)
   o['ContextMasters']=newctx
 # Reviewer specifically required explicit unnamed wording in 立雪; preserve it mechanically.
 if i=='t_d2892b1eaae0':
  for s in d['Senses']:
   for o in s.get('Occurrences',[]):
    a=o.get('ActorAttribution')
    if a and a.get('Status')=='reviewed-unnamed' and 'unnamed' not in a.get('ActorLabel','').lower():a['ActorLabel']='the unnamed '+a.get('ActorLabel','actor')
 d.setdefault('DraftEvidence',{})['IndependentRosterRereview096105']={'ReviewedUtc':datetime.now(timezone.utc).isoformat(),'Decision':'semantic attribution accepted; roster-only identities deferred','DeferredNames':sorted(set(deferred)),'RemovedInvalidMasterLinks':sorted(set(removed)),'Rule':'Do not guess or demote a real master because roster integration is incomplete. Noncanonical RelatedMasters links are removed; exact occurrence identities remain explicit pending roster integration.'}
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':d['SourceTerm'],'oldSha256':old,'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'deferredNames':sorted(set(deferred)),'removedInvalidLinks':sorted(set(removed))})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-096-105-independent-revise7-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
