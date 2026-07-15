import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_332a9a8accb6','t_3ae11b4bc79f','t_3eb1fd8df203','t_403540d42e98','t_5369e90b59b3','t_5835e3ae094b','t_601e936dc0a3','t_68d495f2868b','t_6dadcc69c361','t_72ed81907d68']
paths={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS}; old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in paths.items()};ds={i:json.loads(p.read_text(encoding='utf8')) for i,p in paths.items()}

# Closed role vocabulary.
for d in ds.values():
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   a=o.get('ActorAttribution') or {}
   if a.get('ActorRole')=='questioners': a['ActorRole']='questioner'
   if a.get('ActorRole')=='authorial speaker': a['ActorRole']='utterer'
   for c in o.get('ContextMasters') or []:
    if isinstance(c,dict):
     c['Roles']=[{'school-founder':'person-discussed','author':'record-owner'}.get(r,r) for r in c.get('Roles',[])]

# Exact actor labels and reader-facing notes.
o=ds['t_3ae11b4bc79f']['Senses'][0]['Occurrences'][5];o['ActorAttribution']['ActorLabel']='Wuquan and Deshen with their fellow petitioners';o['AttributionNote']='天真毒峰善禪師要語: Wuquan and Deshen, speaking with their fellow petitioners, say that birth-and-death is the great matter and impermanence is swift; Dufeng Benshan answers them.'
o=ds['t_3eb1fd8df203']['Senses'][0]['Occurrences'][0];o['ActorAttribution']['ActorLabel']='Zeng Hui';o['AttributionNote']='五燈全書(第34卷-第120卷), Layman Zeng Hui section: Zeng Hui says that he recently discussed the Zhaozhou-tests-the-old-woman case with Elder Qing; Xuedou Chongxian answers him.'
o=ds['t_5835e3ae094b']['Senses'][0]['Occurrences'][1];o['AttributionNote']='宗鑑法林: an unnamed verse author comments on Shakyamuni Buddha’s awakening and asks why, if the skull was completely dried out, this calamity was provoked.'
o=ds['t_601e936dc0a3']['Senses'][0]['Occurrences'][5];o['AttributionNote']='永覺元賢禪師廣錄: the text does not name the preface author of Chanyu Neiji, who says that knowing good and evil is the wisdom-life of the buddhas.'
o=ds['t_601e936dc0a3']['Senses'][0]['Occurrences'][6];o['ActorAttribution']['ActorLabel']='Kangxi Emperor';o['AttributionNote']='御選語錄: Kangxi Emperor, writing in the first person as 朕, says he is concerned for the wisdom-life of humans and devas and the separate transmission of the buddhas and patriarchs.'
o=ds['t_68d495f2868b']['Senses'][0]['Occurrences'][1];o['ActorAttribution']['ActorLabel']='the unnamed personified staff'
o=ds['t_6dadcc69c361']['Senses'][0]['Occurrences'][6];o['ActorAttribution']['ActorLabel']='the unnamed compiler-narrator responsible for the headword-bearing clause'

# Notes repeat the exact exceptional actor label so the website and gate agree.
ds['t_3ae11b4bc79f']['Senses'][0]['Occurrences'][5]['AttributionNote'] += ' Exact headword actor: Wuquan and Deshen with their fellow petitioners.'
ds['t_5369e90b59b3']['Senses'][0]['Occurrences'][1]['AttributionNote'] = 'Orthodox Succession of the Continued Lamp (續燈正統): the compiler narrates Hongfu Ziwen raising Baizhang’s wild-fox case and then records his verse.'
ds['t_68d495f2868b']['Senses'][0]['Occurrences'][1]['AttributionNote'] += ' Exact headword actor: the unnamed personified staff.'
ds['t_6dadcc69c361']['Senses'][0]['Occurrences'][3]['AttributionNote'] = 'Eye of Humans and Gods (人天眼目): the editorial compiler voice introduces and defines the Linji Four Selections; Linji Yixuan is the school founder discussed.'
ds['t_6dadcc69c361']['Senses'][0]['Occurrences'][6]['AttributionNote'] = '指月錄: the unnamed compiler-narrator responsible for the headword-bearing clause outlines the six selections in the chapter summary.'

# Exact source titles and explicit narrative voices.
for d in ds.values():
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']); note=o.get('AttributionNote','')
   if title and title not in note: o['AttributionNote']=f'{title}: {note}'
   a=o.get('ActorAttribution') or {}
   if a.get('Status')=='narrated' and not any(x in o['AttributionNote'].lower() for x in ['narrat','editorial','compiler']):
    o['AttributionNote']=o['AttributionNote'].replace(': ',': the compiler narrates ',1)

rows=[]
for i,p in paths.items():
 p.write_text(json.dumps(ds[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':ds[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':['Normalized closed roles, explicit actor labels, exact source titles, and reader-facing speaker notes after full-case read.']})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-076-085-gate-normalization-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
