import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(id):p=ROOT/'fresh-build/entries'/id/'entry.v2.json';return p,json.loads(p.read_text())
def save(id,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==id);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def actor(status,label,role,kind='public figure'):
 a={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T00:25:00Z'}
 if status=='reviewed-unnamed':a['RungsChecked']=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
 else:a['GrammarEvidence']='Expanded context establishes this documentary or explicitly named non-roster actor as the source of the headword-bearing clause.'
 return a
def mk(rel,kw,note,master=None,a=None,ctx=None):
 v=zc.verify(rel,kw);assert v['ok'],(rel,kw,v);o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kw,'ContextMasters':ctx or [],'Curated':True,'AttributionNote':f'Source text ({zc.title(rel)}). {note}'}
 if master:o['MasterName']=master;o['ContextMasters']=[{'MasterName':master,'Roles':['utterer']}]
 else:o['ActorAttribution']=a
 return o

p,z=load('t_ad2c9d24126f');s=z['Senses'][0];old=s['Occurrences'].pop(0);rel=old['RelPath'];s['Occurrences'].insert(0,mk(rel,'入定者為有心入定耶，為無心入定耶？','Zhirong asks whether entering concentration is entered with mind or without mind.',master='Zhirong'));s['Occurrences'].insert(1,mk(rel,'智皇曰：「吾正入定之時，不見有無之心。」','Zhihuang replies that while entering concentration he sees no mind of being or nonbeing.',master='Zhihuang'));o=s['Occurrences'][-1];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=actor('narrated','the case compiler','compiler','case narration');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Compiler narration describes the unnamed woman entering and remaining in concentration.';save('t_ad2c9d24126f',p,z)

p,z=load('t_6bc71cc88c2f')
for idx,desc,ctx in [(0,'reports Huineng throwing the robe and bowl onto a rock.',[{'MasterName':'Huineng','Roles':['person-described']}]),(1,'states the disciplinary procedure for burning robes, bowls, and implements.',[]),(2,'states the procedural handing of the robe and bowl to the attendant.',[])]:
 o=z['Senses'][0]['Occurrences'][idx];o.pop('MasterName',None);o['ContextMasters']=ctx;o['ActorAttribution']=actor('narrated','the record or rule compiler','compiler','procedural narration');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Compiler narration {desc}'
o=z['Senses'][1]['Occurrences'][3];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Feiyin Tongrong','Roles':['person-discussed']}];o['ActorAttribution']=actor('narrated','the prose narrator','compiler','prose narration');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Prose narration reports the possession and transmission of Feiyin Tongrong’s robe and bowl.';save('t_6bc71cc88c2f',p,z)

p,z=load('t_85eef19d3d3a');s=z['Senses'][0];o=s['Occurrences'][4];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Baozhou Juean','Roles':['person-described']}];o['ActorAttribution']=actor('narrated','the biographical compiler','compiler','biographical narration');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Compiler narration reports water surging from the water bottle onto Baozhou Juean.';o=s['Occurrences'][6];s['Occurrences'][6]=mk(o['RelPath'],'師曰。與老僧過淨瓶來。','Nanyang Huizhong tells the emperor to pass him the water bottle.',master='Nanyang Huizhong');save('t_85eef19d3d3a',p,z)

p,z=load('t_37261001c332');s=z['Senses'][0]
for idx,label in [(4,'the Yongzheng Emperor'),(5,'Layman Xuri'),(6,'Layman Xuri'),(8,'Layman Xuri')]:
 o=s['Occurrences'][idx];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=actor('identified-non-master',label,'utterer');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {label} is the exact identified non-master author of this headword-bearing line.'
for idx in (1,7):
 o=s['Occurrences'][idx];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=actor('reviewed-unnamed','the anonymous treatise answerer','respondent','treatise answer voice');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). The anonymous treatise answerer utters the headword-bearing answer; traditional title attribution alone does not establish Bodhidharma as the speaker.'
save('t_37261001c332',p,z)

p,z=load('t_df9aad1ce22d');s=z['Senses'][0];o=s['Occurrences'][0];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=actor('identified-non-master','the official Li Bo','questioner');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). The official Li Bo is the exact identified non-master questioner discussing a mustard seed containing Mount Sumeru.';o=s['Occurrences'][4];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Yongzheng Emperor','Roles':['respondent']}];o['ActorAttribution']=actor('reviewed-unnamed','the unnamed questioner addressing the emperor','questioner','court interlocutor');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). An unnamed questioner utters both mustard-seed questions; the Yongzheng Emperor supplies the paired answers.';save('t_df9aad1ce22d',p,z)

p,z=load('t_c657778889b0');o=z['Senses'][0]['Occurrences'][2];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=actor('identified-non-master','the Yongzheng Emperor','utterer');o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). The Yongzheng Emperor is the identified non-master author asking what word was ever spoken in the private robe transmission.';save('t_c657778889b0',p,z)

p,z=load('t_08ffd55c812e');s=z['Senses'][0];old=s['Occurrences'].pop(1);rel=old['RelPath'];s['Occurrences'].insert(1,mk(rel,'你如何喚作杜撰？','The elder asks why the personified figure is called Du-zuan.',master='Tian\'an Sheng'));s['Occurrences'].insert(2,mk(rel,'長老若不杜撰，如何終日與人說張說李？','The self-named personified Du-zuan answers that the elder fabricates stories all day.',a=actor('reviewed-unnamed','the personified speaker named Du-zuan','respondent','personified dialogue figure'),ctx=[{'MasterName':'Tian\'an Sheng','Roles':['interlocutor']}]))
save('t_08ffd55c812e',p,z)

p,z=load('t_e3226b1e195a');o=z['Senses'][0]['Occurrences'][6];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). Signed donor prose describes the named patron seeing through the higher matter; the document voice is the exact grammatical source, not the patron as utterer.';o['ActorAttribution']['Kind']='signed donor document prose';o['ActorAttribution']['ActorLabel']='the signed document voice';o['ActorAttribution']['ActorRole']='compiler';save('t_e3226b1e195a',p,z)
