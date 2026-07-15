import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
z=json.loads((ROOT/'terms/t_22885135d39e/entry.v2.json').read_text()); old=z['Senses'][0]; thought=[old['Occurrences'][0]]; dust=old['Occurrences'][1:]
for o in thought+dust:
 if o.get('MasterName'):o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':o['MasterName'],'Roles':['utterer']}]
 else:
  cs=[]
  for c in o.get('ContextMasters') or []:
   if isinstance(c,str):cs.append({'MasterName':c,'Roles':['respondent']})
   elif isinstance(c,dict) and c.get('MasterName'):cs.append({'MasterName':c['MasterName'],'Roles':c.get('Roles') or ['respondent']})
  o['ContextMasters']=cs
z.update(CreatedBy='Codex fresh-build lane C',WrittenUtc=None,CorpusBaselineSha256='42d32a5294365a4aa4d5aa2b5f11729e147cb44324f4535065047e946e7b3a2a')
z['Senses']=[{'SenseKey':None,'PreferredTarget':'one speck of dust','AlternateTargets':['a single dust mote','one particle of dust'],'SearchAliases':['one speck dust','single dust mote','one particle'], 'Status':'preferred','Explanation':'One speck of dust is the smallest visible particle used as a unit or limiting image. Records ask what it is, say that none is established, or place its arising and containment beside the whole earth or a field of worlds. The surrounding claim supplies the relation; the bare phrase itself names the minute particle.','Validation':'multi-source','Note':'The frozen corpus has 1,995 exact hits in 318 files representing 314 works. Six anchors cover a direct definition question, non-establishment, arising, containment, and paired world-scale formulas across independent works.','Occurrences':dust,'SourceTexts':sorted({o['RelPath'] for o in dust}),'RelatedMasters':sorted({o['MasterName'] for o in dust if o.get('MasterName')}),'RelatedTerms':['微塵','塵','法界']},{'SenseKey':'thought-dust','PreferredTarget':'one thought-speck','AlternateTargets':['one speck of mental dust'],'SearchAliases':['thought speck','mental dust speck'],'Status':'preferred','Explanation':'One thought-speck is an explicitly defined figurative referent in Dazhu Huihai’s explanation: he identifies the single dust particle with a momentary dust of thought. This is retained separately because the passage itself changes the referent from a physical particle to a mental event.','Validation':'provisional','Note':'One exact source explicitly supplies this self-gloss. It is not projected onto the corpus-wide physical-particle uses.','Occurrences':thought,'SourceTexts':[thought[0]['RelPath']],'RelatedMasters':['Dazhu Huihai'],'RelatedTerms':['心塵','一念']}]
out=ROOT/'fresh-build/entries/t_22885135d39e';out.mkdir(parents=True,exist_ok=True);(out/'entry.v2.json').write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');(out/'STATUS').write_text('drafted\n')
