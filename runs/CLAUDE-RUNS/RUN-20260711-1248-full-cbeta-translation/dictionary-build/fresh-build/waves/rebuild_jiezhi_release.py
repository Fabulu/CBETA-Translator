import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];z=json.loads((ROOT/'terms/t_04ec52b69afa/entry.v2.json').read_text())
closed={'utterer','respondent','questioner','interlocutor','addressee','section-subject','record-owner','person-described','person-discussed','commentator','later-raiser','later-quoter','teacher','student','compiler','verse-author','case-figure'}
for s in z['Senses']:
 for o in s['Occurrences']:
  if o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
  else:
   cs=[]
   for c in o.get('ContextMasters') or []:
    if isinstance(c,str):cs.append({'MasterName':c,'Roles':['section-subject']})
    elif isinstance(c,dict) and c.get('MasterName'):cs.append({'MasterName':c['MasterName'],'Roles':[r for r in c.get('Roles',[]) if r in closed] or ['section-subject']})
   o['ContextMasters']=cs
   a=o.get('ActorAttribution') or {}
   if a.get('ActorRole') not in closed:a['ActorRole']='compiler' if a.get('Status') in {'narrated','impersonal'} else 'questioner'
s0,s1=z['Senses']
s0.update(PreferredTarget='end the monastic restriction period',AlternateTargets=['release the seasonal restriction','close the restriction period'],SearchAliases=['end restriction period','release seasonal retreat','close restriction period','release-day address'],Explanation='To end the monastic restriction period is to formally release the resident community at the opposite calendar boundary from opening the restriction. Records date the event, label hall addresses and small gatherings held on it, describe monks dispersing, and ask for an appropriate release-period saying. Occasion headings belong to the recorder; questions and statements inside the address retain their own exact speakers.',Note='The frozen corpus has 1,814 exact hits in 256 files representing 254 works. Seven anchors cover dated release, occasion headings, dispersal, and direct public questions across independent works.')
s1.update(PreferredTarget='release an inward restriction',AlternateTargets=['break an inward constraint','bring an inner restriction to an end'],SearchAliases=['release inward restriction','break inner constraint','inner release'],Explanation='To release an inward restriction is a deliberate reuse of the institutional term for a different event in the person. These passages explicitly say that the restriction is formed and released in one’s own mind, or define release as an obstructing thing, net, or accumulated impediment breaking apart. A negative witness rejects merely imitative gestures as deserving this name. Because the passages change the referent from a calendar institution to a personal constraint, this is a separate sense.',Note='Four independent works explicitly define or police this personal referent. The sense is multi-source and is not inferred from the ordinary ceremony alone.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_04ec52b69afa';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
