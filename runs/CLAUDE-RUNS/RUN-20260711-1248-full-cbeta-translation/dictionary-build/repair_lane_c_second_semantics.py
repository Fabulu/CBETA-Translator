#!/usr/bin/env python3
import json,hashlib,os
from pathlib import Path
ROOT=Path(__file__).resolve().parent;F=ROOT/'fresh-build';E=F/'entries';W=F/'waves';REV=W/'f001-laneC-postrepair-independent-review.json';OUT=W/'f001-laneC-second-semantic-repairs.json';NOW='2026-07-15T01:15:00Z'
R=json.loads(REV.read_text());ROWS=[x for x in R['entries'] if x['verdict']=='REVISE'];assert len(ROWS)==30
PATH={x['term']:E/x['id']/'entry.v2.json' for x in ROWS}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def get(t):p=PATH[t];return p,json.loads(p.read_text())
def save(p,d):q=p.with_suffix('.tmp');q.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n');os.replace(q,p)
def o(d,si,oi):return d['Senses'][si-1]['Occurrences'][oi-1]
def roles(n,*r):return {'MasterName':n,'Roles':list(r)}
def evidence(x,text):
 a=x.get('ActorAttribution');assert a is not None;a['GrammarEvidence']=text;a['ReviewedBy']='Codex lane-C second independent semantic repair';a['ReviewedUtc']=NOW
def named(x,n,ctx=None,note=None):
 x['MasterName']=n;x.pop('ActorAttribution',None);x['ContextMasters']=ctx or [roles(n,'utterer')]
 if note:x['AttributionNote']=note
def actor(x,status,kind,label,role,proof,ctx=None,note=None):
 x.pop('MasterName',None);x['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':proof,'RungsChecked':['line','expanded-context','section-header','book-title','tei-header','parallel-passage'],'ReviewedBy':'Codex lane-C second independent semantic repair','ReviewedUtc':NOW};x['ContextMasters']=ctx or []
 if note:x['AttributionNote']=note
def aliases(s,vals):s['SearchAliases']=vals[:5];assert 3<=len(s['SearchAliases'])<=5
def repair(pos,t,d):
 s=d['Senses'][0]
 if t=='迷悟':aliases(s,['confusion and awakening','lost and awakened','confusion versus awakening','neither confusion nor awakening'])
 elif t=='明暗':
  old=s['Explanation'];prefix='The records use light and dark as a coordinated pair in visual conditions, questions, verses, and instructions; their attested relations include distinction, intermingling, reciprocity, and pairing.'
  if old.startswith('Literally'):old=old[old.find('.',0)+1:].lstrip()
  s['Explanation']=prefix+' The graphs themselves mean “light or bright” and “dark.” '+old
  aliases(s,['light and dark','bright and dark','light-dark pair','light and darkness'])
 elif t=='經行':aliases(s,['walk about','walk back and forth','walking circuit','pace back and forth','go walking'])
 elif t=='入定':
  aliases(s,['enter absorption','enter a settled state','emerge from absorption','remain absorbed','enter samadhi'])
  x=o(d,1,8);actor(x,'narrated','case narration','the case compiler','compiler','The verbs are third-person case narration: the compiler says a woman enters the settled state, while Manjusri later fails to bring her out.',[roles('Shakyamuni Buddha','case-figure'),roles('Manjusri','case-figure')],"Recorded Sayings of Huiyue Xuming (晦嶽旭禪師語錄): the case compiler narrates the woman's entry; Shakyamuni and Manjusri act in the surrounding case but do not utter the headword.")
 elif t=='衣鉢':
  evidence(o(d,1,1),'The compiler narrates Huineng throwing the robe and bowl onto a rock and Huiming trying to lift them; 衣鉢 is the object of the narrated actions, not quoted speech.')
  evidence(o(d,1,2),'The Baizhang code gives an impersonal disciplinary procedure: 集眾燒衣鉢道具 directs the assembly to burn the expelled person’s robes, bowl, and implements.')
  evidence(o(d,1,3),'The monastic rule gives a procedural sequence: after printing, the robe and bowl attendant receives the articles; no dialogue turn governs the headword.')
  evidence(o(d,2,4),'The prose narrator reports that Feiyin Tongrong’s robe and bowl were kept by disciples and transmitted to Nanming for more than ten years; the named people are possessions’ holders, not utterers.')
 elif t=='覷破':
  x=o(d,1,7);named(x,'Jifei Ruyi',note="Complete Record of Chan Master Jifei (即非禪師全錄), Jifei Ruyi's birthday hall address: Jifei directly describes patron Rufeng Zhuyuan as recently having seen through the higher move.")
 elif t=='淨瓶':evidence(o(d,1,5),'The biography’s subject is Baozhou Juean, but the headword occurs in the compiler’s third-person event clause: water suddenly surges from the clean bottle and pours onto him.')
 elif t=='觀心':
  evidence(o(d,1,5),'The Yongzheng Emperor’s authored discourse directly warns that making silent observation of mind the ultimate is “escaping a pit and falling into a ditch.”')
  evidence(o(d,1,6),'The source identifies Layman Xuri as author of the verse; he contrasts closing the eyes and mistakenly seeking access by observing mind.')
  evidence(o(d,1,7),'Layman Xuri’s authored verse directly coordinates “holding mind” and “observing mind” as producing subject and object.')
  evidence(o(d,1,9),'Layman Xuri’s authored verse directly says that if observing mind is clearly understood, it is uniformly self-lucid.')
 elif t=='芥子':
  x=o(d,1,1);x['ContextMasters']=[roles('Guizong Zhichang','respondent')];evidence(x,'The line is Li Bo’s direct question: he accepts Sumeru containing a mustard seed and asks whether a mustard seed containing Sumeru is false; Guizong Zhichang answers immediately afterward.')
 elif t=='傳衣':
  x=o(d,1,2);actor(x,'narrated','biographical narration','the lamp-record compiler','compiler','傳衣時至 is a narrator clause meaning that the time for transmitting the robe had arrived; Hongren’s quoted instruction begins only after 乃謂曰.',[roles('Hongren','person-described')],"Jianzhong Jingguo Continuation of the Lamp Record (建中靖國續燈錄): the compiler announces that the time for transmitting the robe arrived, then records Hongren's separate speech.")
 elif t=='杜撰':
  x=o(d,1,3)
  for c in x.get('ContextMasters',[]):
   if c.get('MasterName')=='Tianan Sheng':c['MasterName']="Tian'an Sheng"
  x['AttributionNote']=x.get('AttributionNote','').replace('Tianan Sheng',"Tian'an Sheng")
 elif t=='不與萬法為侶':
  x=o(d,1,5);x['ActorAttribution']['ActorRole']='utterer';evidence(x,'The passage is Zhang Jun’s signed preface (張浚序); in his own first-person assessment he says that every raising of Pang’s case already drags through mud and water.')
 elif t=='沒蹤跡':
  x=o(d,1,6);named(x,'Wuyi Yuanlai',note="Extensive Record of Chan Master Wuyi Yuanlai (無異元來禪師廣錄), Wuyi Yuanlai's hall address: Wuyi directly recites ‘leave no trace; do not hide the body’ and continues to address the assembly.")
 elif t=='打坐':evidence(o(d,1,1),'The compiler narrates an unnamed monk who, instead of reading in the scripture hall, sat every day; only afterward does the storekeeper question him.')
 elif t=='落空':
  x=o(d,1,3);x['ActorAttribution']['ActorRole']='utterer';x['ContextMasters']=[roles('Dazhu Huihai','respondent')];evidence(x,'Faming’s clause 謂師曰 is declarative direct speech: the named Vinaya lecturer tells Dazhu Huihai that Chan teachers mostly fall into blankness; Dazhu replies in the next turn.')
  x['AttributionNote']='Transmission of the Lamp (景德傳燈錄): the named Vinaya lecturer Faming directly states that Chan teachers mostly fall into blankness; Dazhu Huihai is the respondent.'
 elif t=='淨裸裸':
  x=o(d,1,6);named(x,'Poshan Haiming',note="Recorded Sayings of Chan Master Poshan (破山禪師語錄), Poshan Haiming's hall address for patron Jin: Poshan directly tells the assembly that release from the burden of birth and death is clean and bare, with nothing to undertake.")
 elif t=='老婆心切':evidence(o(d,1,3),'The grammar explicitly marks Huang Tingjian’s direct turn with 公…曰; he tells Huitang Zuxin that the master’s old-woman concern is exceedingly pressing, and Huitang replies.')
 elif t=='頓漸':evidence(o(d,1,1),'The Platform Record compiler narrates the public naming: people called the southern and northern lines sudden and gradual, while students did not know their purport.')
 elif t=='一口吸盡西江水':
  x=o(d,1,6);x['ContextMasters']=[roles('Mazu Daoyi','utterer'),roles('Wuchu Daguan','commentator')];x['AttributionNote']='Old Recorded Sayings of Venerable Masters (古尊宿語錄), Wuchu Daguan’s record: Mazu Daoyi is the explicitly quoted headword speaker; Wuchu Daguan, identified in the current comment as Yuwang, raises and comments on the case.'
 elif t=='參請':
  evidence(o(d,1,1),'The Five Lamps compiler narrates Zhicheng travelling to Shaoyang, joining the assembly in formal inquiry, and withholding where he came from.')
  evidence(o(d,1,2),'The Five Lamps compiler narrates Shuangfeng’s later arrival at Shishuang and says he merely followed the assembly without making formal inquiry.')
  evidence(o(d,1,5),'The biography of Huanyuan Fuyu narrates his bending the knee to inquire of Lingyin Tai; Tai’s quoted correction begins only after 泰甞謂師曰.')
  evidence(o(d,1,6),'In Huqiu Shaolong’s biography, the compiler says the community made inquiry morning and evening and Huqiu answered tirelessly; the headword names the community’s narrated activity.')
  x=o(d,1,9);x['ContextMasters']=[roles('Ruibai Mingxue','person-described')];evidence(x,'The record’s section heading “inquiry encounters” and following compiler line introduce Ruibai Mingxue’s seven visits to great teachers; the headword is metadata/narration, not a quoted turn.')
 elif t=='無事人':
  x=o(d,1,6);x.update({'RelPath':'X/X80/X80n1565.xml','FromLb':'0239a17','ToLb':'0239a18','Kwic':'上堂。良久曰。無為無事人。猶是金鎻難。喝一喝。下座。','Curated':True});named(x,'Ciming Chuyuan',note="Five Lamps Meeting the Source (五燈會元), Ciming Chuyuan's Nanyuan hall address: after a long pause Ciming directly says that even an inactive person with nothing going on remains a difficult golden lock.")
  s['SourceTexts']=list(dict.fromkeys(z['RelPath'] for z in s['Occurrences']))
 elif t=='老婆禪':
  x=o(d,1,3);x['ActorAttribution']['ActorRole']='utterer';evidence(x,'The source identifies Huang Yuangong as the letter’s lay author; in his own sentence he contrasts Miyun Yuanwu’s pungent handling with teachers elsewhere who speak old-woman Chan.')
 elif t=='血脈':
  aliases(d['Senses'][0],['transmission line','lineage current','continuous thread','unbroken connection','teaching bloodline']);aliases(d['Senses'][1],['hereditary bloodline','family bloodline','line of descent','blood kinship','father-son bloodline']);aliases(d['Senses'][2],['blood circulation','bodily circulation','blood vessels','blood-vessel network','circulating blood'])
 elif t=='直心':evidence(o(d,1,1),'The headword and definition occur inside an explicit quotation introduced by 起信論云 (“the Awakening of Faith Treatise says”); the quoted treatise voice, not the current record owner, defines straight mind as having no bends or twists.')
 elif t=='掛搭':aliases(s,['register at a monastery','take up monastic residence','be admitted to a monastery',"receive a place in the monks' hall",'request monastery lodging'])
 elif t=='攝心':
  x=o(d,1,6);actor(x,'impersonal','quoted treatise instruction','the Treatise on Breaking Appearances voice','compiler','The headword is in continuous instructional prose under the explicit section title “Second Gate: Treatise on Breaking Appearances”; neither the line, section header, nor source title identifies a personal speaker. The prior assignment to Guifeng Zongmi had no source support.',[],"Six Gates of Shaoshi (少室六門), Second Gate: Treatise on Breaking Appearances (第二門破相論): the treatise voice instructs the reader to gather the mind and illuminate within; the source does not name a personal speaker for this clause.")
 elif t=='明心見性':evidence(o(d,1,4),'The line belongs to the Yongzheng Emperor’s authored verse: he parses “illuminate” as illuminating mind and “see” as seeing its substance, then reverses the compound in parallel.')
 elif t=='聯燈':
  aliases(d['Senses'][0],['linked lamps','link the lamps','continue the lamp','lamp succession','lineage lamps']);aliases(d['Senses'][1],['Linked Lamp Essentials','Linked-Lamp Essentials','Essentials of the Linked Lamps','Linked Lamp Record','lamp record book'])
  evidence(o(d,2,2),'The title clause occurs in Li Yong’s named preface; Li Yong states that the work was titled Linked-Lamp Essentials and carved for circulation.')
  # Named author rows already have exact speech proof; tighten their notes rather than adding null actor records.
  o(d,2,1)['AttributionNote']="Linked-Lamp Essentials (聯燈會要), Huiweng Wuming's signed self-preface: Huiweng inventories the selected encounter material, says it was divided into thirty fascicles, and directly gives the work its title."
  o(d,2,3)['AttributionNote']="Extensive Record of Chan Master Yongjue Yuanxian (永覺元賢禪師廣錄), Yongjue's own preface: Yongjue directly reports that Huiweng Wuming made Linked-Lamp Essentials during the Chunxi era."
 elif t=='客塵':aliases(d['Senses'][1],['Baizhang guest and dust','guest-dust function','illuminating guest and dust','Baizhang host-guest classification'])
 elif t=='就路還家':aliases(d['Senses'][1],['physical journey home','set out for home','return home by road','homeward journey'])
 else:raise AssertionError(t)

def main():
 led={'schemaVersion':1,'wave':'f001','lane':'C','reviewLedger':'fresh-build/waves/f001-laneC-postrepair-independent-review.json','startedUtc':NOW,'completed':0,'entries':[]}
 for row in ROWS:
  p,d=get(row['term']);cur=sha(p);assert cur==row['entrySha256'],(row['term'],cur,row['entrySha256']);repair(row['position'],row['term'],d);save(p,d);h=sha(p)
  with (p.parent/'WORK.md').open('a',encoding='utf-8') as f:f.write(f"\n## Second independent semantic repair — {NOW}\nApplied the post-repair independent finding with full-case grammatical proof. New entry SHA-256: `{h}`.\n")
  led['entries'].append({'position':row['position'],'id':row['id'],'term':row['term'],'priorSha256':cur,'entrySha256':h,'finding':row['findings'][0],'completedUtc':NOW});led['completed']=len(led['entries'])
  q=OUT.with_suffix('.tmp');q.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n');os.replace(q,OUT)
 led['completedUtc']=NOW;q=OUT.with_suffix('.tmp');q.write_text(json.dumps(led,ensure_ascii=False,indent=2)+'\n');os.replace(q,OUT);print(json.dumps({'completed':30},ensure_ascii=False))
if __name__=='__main__':main()
