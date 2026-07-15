import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
lp=ROOT/'fresh-build/waves/f001-laneC.json';led=json.loads(lp.read_text())
def L(i):p=ROOT/'fresh-build/entries'/i/'entry.v2.json';return p,json.loads(p.read_text())
def S(i,p,z):
 p.write_text(json.dumps(z,ensure_ascii=False,indent=2)+'\n');e=next(x for x in led['entries'] if x['id']==i);e['entrySha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['gateReport']={'rootRevision':'applied-pending-review'};led['updatedUtc']=datetime.now(timezone.utc).isoformat();lp.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n')
def A(status,label,role,kind='public figure'):
 a={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'ReviewedBy':'Codex root-revision repair','ReviewedUtc':'2026-07-15T00:45:00Z'}
 if status=='reviewed-unnamed':a['RungsChecked']=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']
 else:a['GrammarEvidence']='The full section and grammatical turn establish this documentary, narrated, quoted, or named public actor classification.'
 return a
def setA(o,a,note,ctx=[]):o.pop('MasterName',None);o['ActorAttribution']=a;o['ContextMasters']=ctx;o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). {note}'

# C62-71
p,z=L('t_408abe2e38ca');o=z['Senses'][0]['Occurrences'][3];o['Kwic']='不見睦州凡遇僧來，便云：『現成公案。』';v=zc.verify(o['RelPath'],o['Kwic']);o.update(FromLb=v['fromLb'],ToLb=v['toLb'],MasterName='Muzhou Daoming',ContextMasters=[{'MasterName':'Muzhou Daoming','Roles':['utterer']}],AttributionNote=f'Source text ({zc.title(o["RelPath"])}). An explicit quotation assigns the headword-bearing phrase to Muzhou Daoming.');o.pop('ActorAttribution',None);S('t_408abe2e38ca',p,z)
p,z=L('t_04994645e07a');o=z['Senses'][0]['Occurrences'][4];setA(o,A('identified-non-master','the official Zhang Jun','compiler'),'The official Zhang Jun authors the later commentary about repeatedly raising the case.');S('t_04994645e07a',p,z)
p,z=L('t_9dfa307c0458');o=z['Senses'][0]['Occurrences'][6];o['MasterName']='Chuanzi Decheng';o.pop('ActorAttribution',None);o['ContextMasters']=[{'MasterName':'Chuanzi Decheng','Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). The formula explicitly introduces Chuanzi Decheng as the quoted speaker.';S('t_9dfa307c0458',p,z)
p,z=L('t_acbbe22bdc76');s=z['Senses'][0];setA(s['Occurrences'][0],A('narrated','the case compiler','compiler','case narration'),'Compiler narration reports that an unnamed monk sat daily.');setA(s['Occurrences'][2],A('narrated','the current record speaker','compiler','raising and commentary'),'The record raises Yuantong Xiu’s quoted line and then repeats it in current commentary.',[{'MasterName':'Yuantong Xiu','Roles':['later-quoter']}]);S('t_acbbe22bdc76',p,z)
p,z=L('t_f7aa7ea86229');s=z['Senses'][0];setA(s['Occurrences'][2],A('narrated','the dialogue compiler','compiler','mixed dialogue narration'),'The compiler preserves the exchange between Vinaya Master Faming and the responding Chan master without falsely assigning the whole mixed KWIC to one actor.');s['Occurrences'][5]['MasterName']='Dahui Zonggao';s['Occurrences'][5].pop('ActorAttribution',None);s['Occurrences'][5]['ContextMasters']=[{'MasterName':'Dahui Zonggao','Roles':['utterer']}];s['Occurrences'][5]['AttributionNote']=f'Source text ({zc.title(s["Occurrences"][5]["RelPath"])}). Dahui Zonggao is the direct formal-discourse speaker.';S('t_f7aa7ea86229',p,z)
p,z=L('t_b23e58454acd');o=z['Senses'][0]['Occurrences'][2];setA(o,A('identified-non-master','Huang Tingjian','utterer'),'Huang Tingjian is the exact named public utterer.');S('t_b23e58454acd',p,z)
p,z=L('t_0c86700b60cb');o=z['Senses'][0]['Occurrences'][0];setA(o,A('narrated','the Platform Record compiler','compiler','editorial narration'),'Editorial narration says that people call the southern and northern schools sudden and gradual.');S('t_0c86700b60cb',p,z)
p,z=L('t_cf07831c1f12');o=z['Senses'][0]['Occurrences'][6];setA(o,A('narrated','the direct-record prose voice','compiler','record prose'),'The headed record prose supplies the descriptive clause after the section ladder was checked.');S('t_cf07831c1f12',p,z)
p,z=L('t_6c1f113fbdcd');o=z['Senses'][0]['Occurrences'][5];setA(o,A('narrated','the donor-document prose voice','compiler','signed donor prose'),'Signed donor prose supplies the headword-bearing description.');S('t_6c1f113fbdcd',p,z)

# C72-81
p,z=L('t_90e46d995978');s=z['Senses'][0]
for idx,name in [(0,'Zhicheng'),(1,'Shuangfeng'),(4,'Huanyuan'),(5,None),(8,None)]:setA(s['Occurrences'][idx],A('narrated','the biographical compiler','compiler','biographical narration'),f'Compiler narration reports the formal-inquiry action{(" of "+name) if name else ""}.',([{'MasterName':name,'Roles':['person-described']}] if name else []))
if s['Occurrences'][3].get('ActorAttribution'):s['Occurrences'][3]['ActorAttribution']['ActorRole']='compiler'
S('t_90e46d995978',p,z)
p,z=L('t_e156057131dc');o=z['Senses'][0]['Occurrences'][0];setA(o,A('identified-non-master','the Yongzheng Emperor','utterer'),'The Yongzheng Emperor authors the line.');S('t_e156057131dc',p,z)
p,z=L('t_d9c587fad710');o=z['Senses'][0]['Occurrences'][4];setA(o,A('reviewed-unnamed','the unnamed questioning monk','questioner','monk'),'An unnamed monk asks the headword question; Juelang responds.',[{'MasterName':'Juelang Daosheng','Roles':['respondent']}]);o=z['Senses'][0]['Occurrences'][5];setA(o,A('reviewed-unnamed','the unnamed quoted-source speaker','later-quoter','quoted source'),'The phrase is introduced as an unnamed quoted saying, not as a question.');S('t_d9c587fad710',p,z)
p,z=L('t_8dc9df82b364');o=z['Senses'][0]['Occurrences'][5];o['MasterName']='Mazu Daoyi';o['ContextMasters']=[{'MasterName':'Mazu Daoyi','Roles':['utterer']}];o['AttributionNote']=f'Source text ({zc.title(o["RelPath"])}). The headword is Mazu Daoyi’s explicitly quoted line; the later raiser is contextual.';S('t_8dc9df82b364',p,z)

# C82-91
p,z=L('t_21f09b3726e7');s=z['Senses'][0];line=s['Occurrences'].pop(4);z['Senses'].append({'SenseKey':'lineage-succession','MasterName':None,'PreferredTarget':'lineage succession','AlternateTargets':['line of succession'],'Status':'drafted','Explanation':'This sense denotes succession through a lineage, distinct from blood-flow imagery for continuity and from bodily circulation or hereditary kinship.','Validation':'The explicit succession passage supplies the separate referent.','Note':'Split under the referent rule.','Occurrences':[line],'SourceTexts':[line['RelPath']],'RelatedMasters':[],'RelatedTerms':[]});s['PreferredTarget']='unbroken continuity';S('t_21f09b3726e7',p,z)
p,z=L('t_dcd5468f5104');o=z['Senses'][0]['Occurrences'][0];setA(o,A('impersonal','the quoted treatise voice','compiler','scripture quotation'),'An explicit quotation from the Awakening of Faith Treatise supplies the headword, not a human questioner.');S('t_dcd5468f5104',p,z)
p,z=L('t_dc5f4386a0ed');o=z['Senses'][0]['Occurrences'][3];setA(o,A('identified-non-master','the Yongzheng Emperor','utterer'),'The Yongzheng Emperor authors the line.');S('t_dc5f4386a0ed',p,z)
p,z=L('t_edd5d300476f');s=z['Senses'][1];setA(s['Occurrences'][1],A('identified-non-master','Li Yong','compiler'),'Li Yong is the named preface author.');setA(s['Occurrences'][3],A('narrated','the book-history compiler','compiler','bibliographic narration'),'Compiler narration reports that Huiweng Wuming compiled the book.',[{'MasterName':'Huiweng Wuming','Roles':['person-described']}]);S('t_edd5d300476f',p,z)
p,z=L('t_0160fc00c70d');o=z['Senses'][0]['Occurrences'][2];setA(o,A('identified-non-master','the lay author Huang Yuangong','compiler'),'The named lay author Huang Yuangong writes the headword-bearing letter.');S('t_0160fc00c70d',p,z)
p,z=L('t_057cc9ea8755');o=z['Senses'][0]['Occurrences'][5];setA(o,A('reviewed-unnamed','the anonymous treatise instruction voice','utterer','treatise voice'),'Traditional work attribution does not establish Bodhidharma as the exact speaker; the instruction voice remains anonymous after the six-rung check.');S('t_057cc9ea8755',p,z)
