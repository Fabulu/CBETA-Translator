import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
z=json.loads((ROOT/'terms/t_3f7a6ab74b68/entry.v2.json').read_text());s=z['Senses'][0]
for o in s['Occurrences']:
 cs=[]
 for c in o.get('ContextMasters') or []:
  if isinstance(c,str):cs.append({'MasterName':c,'Roles':['section-subject']})
  elif isinstance(c,dict) and c.get('MasterName'):cs.append({'MasterName':c['MasterName'],'Roles':c.get('Roles') or ['section-subject']})
 o['ContextMasters']=cs
 if (o.get('ActorAttribution') or {}).get('ActorRole')=='document voice':o['ActorAttribution']['ActorRole']='compiler'
s.update(PreferredTarget="the monks' hall",AlternateTargets=['the communal monks’ hall','the monastic hall'],SearchAliases=['monks hall','communal monastic hall','monastic sleeping hall'],Explanation="The monks' hall is the communal building where resident monks are assigned places, eat or sleep according to the institution's arrangement, and assemble under hall procedures. Records mention its door, bell, sleeping platforms, seating, and movement into or out of it. These physical and procedural uses identify one institutional place; the term does not by itself name an abstract ideal.",Note='The frozen corpus has 1,995 exact hits in 302 files representing 298 works. Seven anchors cover the hall bell, donor seating, sleeping platforms, entry and exit, door closure, explicit room identification, and summer-order assignment across independent works.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_3f7a6ab74b68';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
