import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[2];W=R/'fresh-build'/'waves';REPO=R.parents[3]
masters=json.loads((REPO/'Assets/Data/master-dates.json').read_text())['masters'];A=[]
for m in masters:
 for a in m.get('names',[])[1:]:
  if len(a)>=2:A.append((a,m['names'][0]))
A.sort(key=lambda z:len(z[0]),reverse=True)
P=json.loads((W/'f004-author-repair-cohort1-attribution-packet.json').read_text());out=[]
for p in P['packets']:
 t=p['sourceTerm'];case=p['caseText'];proof=p.get('turnProofCandidates') or [];hp=proof[0]['headwordStart'] if proof else case.find(t);before=case[:hp];after=case[hp+len(t):];hits=[]
 for a,n in A:
  j=before.rfind(a)
  if j>=0:hits.append((j,a,n))
 hits.sort(reverse=True);owner=hits[0][2] if hits else None;cue=(proof[0].get('nearestPrecedingCue') or {}).get('text','') if proof else ''
 local=before[max(0,len(before)-160):]
 if re.search(r'(?:僧問|問曰|僧曰|僧云|進云)[：：「“]?[^。；]{0,120}$',local):kind='unnamed-questioner';actor=None
 elif owner and (cue in {'師曰','師云','良久曰'} or re.search(r'(?:上堂|小參|示眾|乃曰|乃云|師曰|師云|頌曰)[：：「“]?[^。]{0,160}$',local)):kind='named-utterer';actor=owner
 elif re.search(r'(?:曰|云|道)[：：「“]?[^。]{0,120}$',local):kind='reviewed-utterer';actor=None
 else:kind='narrated';actor=None
 out.append({'entryId':p['entryId'],'term':t,'sense':p['sense'],'occurrence':p['occurrence'],'rel':p['RelPath'],'lb':p['FromLb'],'title':p['title'],'headwordClause':proof[0]['headwordClause'] if proof else t,'nearestCue':cue,'nearestOwnerAlias':hits[0][1] if hits else None,'actor':actor,'decision':kind,'riskFlags':p['riskFlags']})
(W/'f004-author-repair-cohort1-proposed-decisions.json').write_text(json.dumps({'schemaVersion':1,'decisions':out},ensure_ascii=False,indent=2)+'\n');print(json.dumps(out[:10],ensure_ascii=False,indent=2))
