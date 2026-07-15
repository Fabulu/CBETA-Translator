import hashlib, json, sys
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT))
import zc

IDS=['t_332a9a8accb6','t_3ae11b4bc79f','t_3eb1fd8df203','t_403540d42e98','t_5369e90b59b3','t_5835e3ae094b','t_601e936dc0a3','t_68d495f2868b','t_6dadcc69c361','t_72ed81907d68']
paths={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS}
old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in paths.items()}
ds={i:json.loads(p.read_text(encoding='utf-8')) for i,p in paths.items()}
changes={i:[] for i in IDS}
R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage']

def cm(name,*roles): return {'MasterName':name,'Roles':list(roles)}
def unnamed(o,kind,label,role,evidence,contexts,note):
    o.pop('MasterName',None)
    o['ActorAttribution']={'Status':'reviewed-unnamed','Kind':kind,'ActorLabel':label,'ActorRole':role,
      'GrammarEvidence':evidence,'RungsChecked':R,'ReviewedBy':'Codex personal full-case read 076-085',
      'ReviewedUtc':datetime.now(timezone.utc).isoformat()}
    o['ContextMasters']=contexts; o['AttributionNote']=note
def narrated(o,label,evidence,contexts,note):
    o.pop('MasterName',None)
    o['ActorAttribution']={'Status':'narrated','Kind':'compiler narrative','ActorLabel':label,'ActorRole':'compiler',
      'GrammarEvidence':evidence,'RungsChecked':R,'ReviewedBy':'Codex personal full-case read 076-085',
      'ReviewedUtc':datetime.now(timezone.utc).isoformat()}
    o['ContextMasters']=contexts; o['AttributionNote']=note
def named(o,name,contexts,note):
    o['MasterName']=name; o.pop('ActorAttribution',None); o['ContextMasters']=contexts; o['AttributionNote']=note

# 主中賓: distinguish handbook prose, monk questions, and the named Dongshan question.
d=ds['t_332a9a8accb6']; os=d['Senses'][0]['Occurrences']
narrated(os[0],'the Eye of Humans and Gods compiler','The handbook compiler defines the four positions; this is editorial exposition, not a marked speech turn.',[cm('Linji Yixuan','school-founder','person-discussed')],'Eye of Humans and Gods (人天眼目): the compiler defines the Linji four guest-host positions; Linji Yixuan is the school founder discussed, not the utterer of this editorial clause.')
unnamed(os[1],'unnamed monastic questioner','the unnamed monk asking about guest within host','questioner','The headword stands in 如何是主中賓 before 穴云 introduces Fengxue Yanzhao’s answer.',[cm('Fengxue Yanzhao','respondent')],'Eye of Humans and Gods (人天眼目): an unnamed monk asks “what is guest within host?”; Fengxue Yanzhao answers “the returning simurgh, the two luminaries renewed.”')
for idx in (3,4):
    unnamed(os[idx],'unnamed monastic questioner','the unnamed monk asking about guest within host','questioner','The headword is inside the question before 師云 introduces the response.',[cm('Eryin Mi','respondent','record-owner')],os[idx].get('AttributionNote',''))
unnamed(os[5],'unnamed monastic questioner','the unnamed monk asking about guest within host','questioner','The question marker 曰 and the following 師曰/師便喝 separate the unnamed monk’s headword-bearing question from Shuangling Hua’s answer.',[cm('Shuangling Hua','respondent','record-owner')],'Complete Collection of the Five Lamps (五燈全書): an unnamed monk asks “what is guest within host?” and Shuangling Hua responds with a shout.')
named(os[6],'Dongshan Liangjie',[cm('Dongshan Liangjie','utterer','questioner'),cm('Yinshan Heshang','respondent')],'Records of the Transmission of the Lamp (傳燈錄): Dongshan Liangjie asks Yinshan “what is guest within host?”; Yinshan answers “green mountains cover white clouds.”')
changes['t_332a9a8accb6']=['Corrected editorial exposition to compiler narration; corrected three monk questions; restored Dongshan Liangjie as the named questioner.']

# 無常迅速: the complete passage names the collective petitioners.
o=ds['t_3ae11b4bc79f']['Senses'][0]['Occurrences'][5]
o['ActorAttribution'].update(ActorLabel='disciples Wuquan and Deshen with their fellow petitioners',Kind='named collective petitioners',ActorRole='questioners')
o['AttributionNote']='Recorded Sayings of Dufeng Benshan (毒峰善禪師語錄): disciples Wuquan and Deshen, speaking with their fellow petitioners, say that birth-and-death is the great matter and impermanence is swift; Dufeng Benshan answers them.'
changes['t_3ae11b4bc79f']=['Named Wuquan and Deshen in the collective petitioner attribution.']

# 趙州勘婆: the section heading identifies the lay official as Zeng Hui.
o=ds['t_3eb1fd8df203']['Senses'][0]['Occurrences'][0]
o['ActorAttribution'].update(ActorLabel='the lay official Zeng Hui',Kind='named lay official',ActorRole='utterer',GrammarEvidence='The section heading names 修撰曾會居士; 公曰 marks Zeng Hui’s statement to Xuedou Chongxian.')
o['AttributionNote']='Complete Collection of the Five Lamps (五燈全書), Layman Zeng Hui section: Zeng Hui says that he recently discussed the Zhaozhou-tests-the-old-woman case with Elder Qing; Xuedou Chongxian answers him.'
changes['t_3eb1fd8df203']=['Recovered lay speaker Zeng Hui from the section heading.']

# 抱贓叫屈: the anthology verse has no recoverable author name.
o=ds['t_403540d42e98']['Senses'][0]['Occurrences'][0]
unnamed(o,'anonymous capping-verse author','the unnamed capping-verse author','verse-author','The headword belongs to an anthology capping verse; all six rungs leave its author unnamed.',[],'Forest of the Ancestral Mirror (宗鑑法林): an unnamed capping-verse author says that asking further under blue sky and bright sun is “hugging the loot and crying injustice.”')
changes['t_403540d42e98']=['Reclassified the unidentified capping-verse voice as reviewed-unnamed.']

# 百丈野狐: introductions/headings are narration; the Foyan biography question is Wuzu’s;
# the final verse is spoken by the section master Chongjue Fakong.
d=ds['t_5369e90b59b3']; os=d['Senses'][0]['Occurrences']
narrated(os[0],'the record compiler','師室中常舉 narrates Daxin’s chamber usage; it is not a quotation of Daxin saying the headword.',[cm('Daxin','person-described','record-owner')],'Recorded Sayings of Xiaoyin Daxin (笑隱大訢禪師語錄): the compiler narrates that Daxin regularly raised Baizhang’s wild-fox case in the chamber.')
narrated(os[1],'the record compiler','舉百丈野狐話，頌曰 introduces Hongfu Ziwen’s verse; the headword occurs in the compiler’s case introduction.',[cm('Hongfu Ziwen','verse-author','record-owner')],'Orthodox Succession of the Continued Lamp (續燈正統): the compiler says Hongfu Ziwen raised Baizhang’s wild-fox case and then records his verse.')
narrated(os[2],'the case-record compiler','第八則百丈野狐 is the compiler’s case heading before Wansong Xingxiu’s commentary.',[cm('Wansong Xingxiu','commentator')],'Book of Serenity (從容錄): the compiler’s eighth-case heading names Baizhang’s wild fox; Wansong Xingxiu comments afterward.')
named(os[3],'Wuzu Fayan',[cm('Wuzu Fayan','utterer','questioner'),cm('Foyan Qingyuan','respondent')],'Jiatai Universal Lamp Record (嘉泰普燈錄): Wuzu Fayan asks Foyan Qingyuan what he makes of Baizhang’s wild-fox case; Foyan answers, and Wuzu is delighted.')
narrated(os[4],'the record compiler','上堂，舉百丈野狐話，乃曰 narrates Shita Xuanmi Li raising the case before his quoted verse.',[cm('Shita Xuanmi Li','person-described','record-owner')],'Jiatai Universal Lamp Record (嘉泰普燈錄): the compiler narrates Shita Xuanmi Li raising Baizhang’s wild-fox case in the hall and then quotes his verse.')
narrated(os[5],'the record compiler','上堂，舉百丈野狐話，頌曰 narrates Yunju Shuai’an Fancong raising the case before the quoted verse.',[cm("Yunju Shuai'an Fancong",'person-described','record-owner')],'Orthodox Succession of the Continued Lamp (續燈正統): the compiler narrates Yunju Shuai’an Fancong raising Baizhang’s wild-fox case and then quotes his verse.')
named(os[6],'Chongjue Fakong',[cm('Chongjue Fakong','utterer','verse-author')],'Complete Collection of the Five Lamps (五燈全書), Chongjue Fakong section: Chongjue Fakong’s verse says, “Baizhang’s wild fox, running mad without its head.”')
changes['t_5369e90b59b3']=['Separated six narrated case introductions/headings from speech; corrected Wuzu Fayan as questioner; recovered Chongjue Fakong as verse speaker.']

# 髑髏: make the unnamed verse author explicit; other full-read attributions hold.
o=ds['t_5835e3ae094b']['Senses'][0]['Occurrences'][1]
o['ActorAttribution'].update(Status='reviewed-unnamed',Kind='anonymous verse author',ActorLabel='the unnamed verse author in the Shakyamuni section',ActorRole='verse-author',GrammarEvidence='The headword occurs in an unattributed verse on Shakyamuni’s awakening; all six rungs leave the verse author unnamed.')
changes['t_5835e3ae094b']=['Made the anonymous verse-author status explicit.']

# 慧命: named imperial prose is not a master; make the other unnamed preface explicit.
d=ds['t_601e936dc0a3']; os=d['Senses'][0]['Occurrences']
os[5]['ActorAttribution'].update(ActorLabel='the unnamed preface author of Chanyu Neiji',Kind='anonymous preface author')
o=os[6]; o.pop('MasterName',None); o['ActorAttribution']={'Status':'identified-non-master','Kind':'named emperor','ActorLabel':'the Kangxi Emperor','ActorRole':'authorial speaker','GrammarEvidence':'朕 marks the Kangxi Emperor’s own first-person preface voice.','RungsChecked':R,'ReviewedBy':'Codex personal full-case read 076-085','ReviewedUtc':datetime.now(timezone.utc).isoformat()}; o['ContextMasters']=[]; o['AttributionNote']='Imperial Selection of Recorded Sayings (御選語錄): the Kangxi Emperor, writing in the first person as 朕, says he is concerned for the wisdom-life of humans and devas and the separate transmission of the buddhas and patriarchs.'
changes['t_601e936dc0a3']=['Made the unnamed preface author explicit; moved the Kangxi Emperor from MasterName to named non-master attribution.']

# 料揀: two monk questions are not masters; handbook definition is editorial prose.
d=ds['t_6dadcc69c361']; os=d['Senses'][0]['Occurrences']
unnamed(os[0],'unnamed monastic questioner','the unnamed monk requesting a discrimination','questioner','The headword is in the monk’s request before 師曰 introduces Nanyue Jiqi Hongchu’s answer.',[cm('Nanyue Jiqi Hongchu','respondent','record-owner')],'Recorded Sayings of Nanyue Jiqi (南嶽繼起和尚語錄): an unnamed monk asks Nanyue Jiqi Hongchu to discriminate between two episodes; Nanyue answers.')
narrated(os[3],'the Eye of Humans and Gods compiler','The handbook compiler defines the Four Selections in editorial exposition, not a marked master speech turn.',[cm('Linji Yixuan','school-founder','person-discussed')],'Eye of Humans and Gods (人天眼目): the compiler introduces and defines the Linji Four Selections; Linji Yixuan is the school founder discussed.')
unnamed(os[5],'unnamed monastic questioner','the unnamed monk requesting Pin Jixiang’s appraisal','questioner','The monk says 學人今日上來，請師料揀 before 師曰 introduces Pin Jixiang’s response.',[cm('Pin Jixiang','respondent','record-owner')],'Recorded Sayings of Pin Jixiang (頻吉祥禪師語錄): an unnamed monk requests Pin Jixiang’s appraisal; Pin answers immediately afterward.')
changes['t_6dadcc69c361']=['Removed two placeholder names from MasterName; reclassified the handbook definition as compiler exposition.']

# 施為: the authored interrogative is Yongming’s; the named record-owner address is Zhengfa Ximing’s.
d=ds['t_72ed81907d68']; os=d['Senses'][0]['Occurrences']
named(os[0],'Yongming Yanshou',[cm('Yongming Yanshou','utterer','author')],'Source Mirror Record (宗鏡錄): Yongming Yanshou asks whether, amid the four deportments, dressing, eating, responding, working, and acting, each matter can be discerned as real.')
named(os[1],'Zhengfa Ximing',[cm('Zhengfa Ximing','utterer','record-owner')],'Complete Collection of the Five Lamps (五燈全書), Zhengfa Ximing section: Zhengfa Ximing says that when the source is thoroughly clear, movement and stillness, conduct, walking, sitting, and lying each accord with the Way.')
changes['t_72ed81907d68']=['Recovered Yongming Yanshou as authorial speaker and Zhengfa Ximing as the named record-owner speaker.']

rows=[]
for i,p in paths.items():
    p.write_text(json.dumps(ds[i],ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    rows.append({'id':i,'term':ds[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':changes[i] or ['Full-case read confirmed; no data change.']})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-076-085-full-read-repair-ledger.json'
out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(out)
