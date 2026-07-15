import json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
z=json.loads((ROOT/'terms/t_2f533e7ff5f8/entry.v2.json').read_text());s=z['Senses'][0]
for o in s['Occurrences']:
 if o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
 else:
  cs=[]
  for c in o.get('ContextMasters') or []:
   if isinstance(c,str):cs.append({'MasterName':c,'Roles':['respondent']})
   elif isinstance(c,dict) and c.get('MasterName'):cs.append({'MasterName':c['MasterName'],'Roles':c.get('Roles') or ['respondent']})
  o['ContextMasters']=cs
rel='J/J36/J36nB369.xml';k='二時豐約總隨緣，大事剋期要了辦';v=zc.verify(rel,k);assert v['ok'];s['Occurrences'].append({'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':k,'MasterName':'Zhean Fan','ContextMasters':[{'MasterName':'Zhean Fan','Roles':['utterer']}],'Curated':True,'AttributionNote':'蔗菴範禪師語錄: Zhean Fan says in his restriction-period instruction that whether the two daily meals are ample or spare, all follows conditions.'})
s.update(PreferredTarget='follow conditions',AlternateTargets=['accord with conditions','go along with circumstances'],SearchAliases=['follow conditions','accord circumstances','go with conditions','respond to circumstances'],Explanation='To follow conditions is to proceed in accordance with the circumstances that arise rather than insist on a fixed arrangement. Records apply it to eating, travelling, daily conduct, responding to people and things, and accepting ample or spare provisions. Some passages pair it with unconfined activity or say that responsive change does not lose the thing’s character. These are deployments of one manner of proceeding, not separate senses.',Note='The frozen corpus has 1,982 exact hits in 332 files representing 328 works. Seven anchors cover daily conduct, travel, provisions, responsive functioning, a biographical maxim, and a direct encounter question across independent works.')
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
out=ROOT/'fresh-build/entries/t_2f533e7ff5f8';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
