import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def load(id):p=ROOT/'fresh-build/entries'/id/'entry.v2.json';return p,json.loads(p.read_text())
def save(id,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==id);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['state']=(p.parent/'STATUS').read_text().strip();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def occ(rel,kwic,note,master=None,actor=None,contexts=None):
 v=zc.verify(rel,kwic);assert v['ok'],(rel,kwic,v);o={'RelPath':rel,'FromLb':v['fromLb'],'ToLb':v['toLb'],'Kwic':kwic,'ContextMasters':contexts or [],'Curated':True,'AttributionNote':f'Source text ({zc.title(rel)}). {note}'}
 if master:o['MasterName']=master;o['ContextMasters']=[{'MasterName':master,'Roles':['utterer']}]
 else:o['ActorAttribution']=actor
 return o
def unnamed(label,role='questioner'):
 return {'Status':'reviewed-unnamed','Kind':'monk' if 'monk' in label else 'non-monastic interlocutor','ActorLabel':label,'ActorRole':role,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T00:10:00Z'}
def narrated(label='the record compiler'):
 return {'Status':'narrated','Kind':'case or biographical narration','ActorLabel':label,'ActorRole':'compiler','GrammarEvidence':'The headword occurs in the recorder’s third-person narrative rather than in speech by the person described.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T00:10:00Z'}
def identified(label,kind='named public author',role='compiler'):
 return {'Status':'identified-non-master','Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':'Expanded context explicitly identifies this public author or interlocutor as the source of the headword-bearing clause.','ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T00:10:00Z'}

# 木佛: split narration from the superintendent's direct objection; correct official question.
p,z=load('t_338c380e905a');s=z['Senses'][0];old=s['Occurrences'].pop(0);rel=old['RelPath'];s['Occurrences'].insert(0,occ(rel,'乃取木佛燒火','Compiler narration reports Danxia taking a wooden buddha and burning it.',actor=narrated(),contexts=[{'MasterName':'Danxia Tianran','Roles':['person-described']}]))
s['Occurrences'].insert(1,occ(rel,'何得燒我木佛？','The unnamed monastery superintendent directly objects, asking why his wooden buddha was burned.',actor=unnamed('the unnamed monastery superintendent','questioner'),contexts=[{'MasterName':'Danxia Tianran','Roles':['addressee']}]))
o=s['Occurrences'][-1];rel=o['RelPath'];s['Occurrences'][-1]=occ(rel,'有官人問丹霞燒木佛院主為什麼眉鬚墮落','An unnamed official asks the headword-bearing question about Danxia; Zhaozhou answers.',actor=unnamed('the unnamed official','questioner'),contexts=[{'MasterName':'Zhaozhou Congshen','Roles':['respondent','record-owner']}]);save('t_338c380e905a',p,z)

p,z=load('t_462d9613abe9');o=z['Senses'][1]['Occurrences'][0];o.pop('MasterName',None);o['ContextMasters']=[];o['ActorAttribution']=identified('the memorial author Shen Xun','named memorial author','compiler');o['AttributionNote']='Selected Official Records of the Chan School (禪關策進): the memorial author Shen Xun is the identified non-master author describing dangerous bird-path travel.';save('t_462d9613abe9',p,z)

p,z=load('t_77774b8724f1');o=z['Senses'][0]['Occurrences'][3];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Parshva','Roles':['person-described']}];o['ActorAttribution']=narrated();o['AttributionNote']='Compendium of the Five Lamps (五燈會元): compiler narration says that the patriarch Parshva entered transformation after transmitting the teaching.';save('t_77774b8724f1',p,z)

p,z=load('t_94be914de45d');o=z['Senses'][0]['Occurrences'][4];o.pop('MasterName',None);o['ContextMasters']=[{'MasterName':'Guangxiao Dezhou','Roles':['later-raiser','record-owner']}];o['ActorAttribution']=unnamed('the unnamed earlier verse author','verse-author');o['AttributionNote']='Complete Collection of the Five Lamps (五燈全書(第34卷-第120卷)): Guangxiao Dezhou raises an earlier verse; the full attribution ladder does not identify its headword-bearing verse author, so Dezhou is retained only as later raiser and record owner.';save('t_94be914de45d',p,z)

# 出身處: four questions belong to unnamed monks; named masters respond.
p,z=load('t_447ad9648add');s=z['Senses'][0]
for idx,kw,name in [(0,'如何是出身處。','Huaguang Fan'),(1,'問如何是諸佛出身處','Yunmen Wenyan'),(4,'問：如何是學人出身處？','Yungai Zhiben'),(6,'問。如何是學人出身處。','Baoen Huaiyue')]:
 rel=s['Occurrences'][idx]['RelPath'];s['Occurrences'][idx]=occ(rel,kw,f'An unnamed monk asks the exact headword-bearing question; {name} supplies the following response.',actor=unnamed('the unnamed questioning monk'),contexts=[{'MasterName':name,'Roles':['respondent','record-owner']}])
save('t_447ad9648add',p,z)

# 敗闕: split mixed Linji/Tangtou turns; correct unnamed questioners.
p,z=load('t_b8d2633b12ef');s=z['Senses'][0];old=s['Occurrences'].pop(1);rel=old['RelPath'];s['Occurrences'].insert(1,occ(rel,'臨濟云。這老漢今日敗闕。','Linji Yixuan states that the old fellow has shown a defect today.',master='Linji Yixuan'));s['Occurrences'].insert(2,occ(rel,'堂頭法叔禪師道。那裏是他敗闕處。','Tangtou Fashu asks where that defect lies.',master='Tangtou Fashu'))
# Indexes shifted: original o3 is now index 3; original o7 is final.
o=s['Occurrences'][3];s['Occurrences'][3]=occ(o['RelPath'],'如何得無敗闕？','An unnamed monk asks how to be without defect; Zhanran Yuancheng responds.',actor=unnamed('the unnamed questioning monk'),contexts=[{'MasterName':'Zhanran Yuancheng','Roles':['respondent','record-owner']}]);o=s['Occurrences'][-1];s['Occurrences'][-1]=occ(o['RelPath'],'如何自納敗闕？','An unnamed questioner asks how one incurs a defect oneself; Baohua Xian responds after a pause.',actor=unnamed('the unnamed questioning monk'),contexts=[{'MasterName':'Baohua Xian','Roles':['respondent','record-owner']}]);save('t_b8d2633b12ef',p,z)
