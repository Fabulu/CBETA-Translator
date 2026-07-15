import hashlib,json,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT));import zc
IDS=['t_aa56c106ef82','t_acaf1f7f698e','t_b8c3ecb60618','t_c688927b7ea1','t_d2892b1eaae0','t_d3631f4abf25','t_d95b944e0749','t_da72db7aa635','t_dda048ca832d','t_e89833bb5e63']
P={i:ROOT/'fresh-build/entries'/i/'entry.v2.json' for i in IDS};old={i:hashlib.sha256(p.read_bytes()).hexdigest() for i,p in P.items()};D={i:json.loads(p.read_text(encoding='utf8')) for i,p in P.items()};R=['line','expanded-context','section-header','book-title','tei-header','parallel-passage'];changes={i:[] for i in IDS}
def cm(n,*r):return {'MasterName':n,'Roles':list(r)}
def named(o,n,note,ctx=None):o['MasterName']=n;o.pop('ActorAttribution',None);o['ContextMasters']=ctx or [cm(n,'utterer')];o['AttributionNote']=note
def actor(o,status,kind,label,role,ev,note,ctx=None):
 o.pop('MasterName',None);o['ActorAttribution']={'Status':status,'Kind':kind,'ActorLabel':label,'ActorRole':role,'GrammarEvidence':ev,'RungsChecked':R,'ReviewedBy':'Codex personal full-read 096-105','ReviewedUtc':datetime.now(timezone.utc).isoformat()};o['ContextMasters']=ctx or [];o['AttributionNote']=note
def un(o,kind,label,role,ev,note,ctx=None):actor(o,'reviewed-unnamed',kind,label,role,ev,note,ctx)
def non(o,kind,label,role,ev,note,ctx=None):actor(o,'identified-non-master',kind,label,role,ev,note,ctx)

# 立雪: two biographies narrate the act; one dialogue belongs to the questioner; one signed preface remains non-roster.
E=D['t_d2892b1eaae0']['Senses'][0]['Occurrences']
actor(E[0],'narrated','compiler narrative','the unnamed compiler of Wudeng Huiyuan','compiler','The headword is in biography before Bodhidharma asks Huike what he seeks.','五燈會元: the unnamed compiler narrates Huike standing motionless in snow; Bodhidharma then questions him.',[cm('Bodhidharma','person-discussed'),cm('Huike','person-discussed')])
actor(E[1],'narrated','compiler narrative','the unnamed compiler of Jianzhong Jingguo Xudeng Lu','compiler','The headword is in the biographical clause 有神光法師立雪斷臂, not in quoted speech.','建中靖國續燈錄: the unnamed compiler narrates that Shenguang stood in snow and severed his arm before being renamed Huike.',[cm('Huike','person-discussed'),cm('Bodhidharma','person-discussed')])
un(E[4],'unnamed monastic questioner','the unnamed monk asking about the Second Patriarch standing in snow','utterer','問 introduces the headword-bearing question; 師曰 begins the response.','五燈全書: an unnamed monk asks why the Second Patriarch stood waist-deep in snow; Fohui Faquan answers.',[cm('Fohui Faquan','respondent'),cm('Huike','person-discussed')])
un(E[5],'preface author','the unresolved author of the Cizhao chanshi Fengyan ji preface','utterer','The headword occurs in the prose preface headed 慈照禪師鳳巖集并序; the surviving packet does not yield a roster-identifiable name.','古尊宿語錄: the unresolved preface author says Cizhao received the ancestral seal by standing in snow.',[cm('Cizhao','person-discussed')])
changes['t_d2892b1eaae0']+=['Reclassified two biographies, one monk question, and one signed preface by full-case turn reading.']

# 啐啄同時: Yuanwu's commentary was misassigned to the master discussed; a compiled comment explicitly names Baiyun.
E=D['t_b8c3ecb60618']['Senses'][0]['Occurrences']
named(E[3],'Yuanwu Keqin','佛果圜悟禪師碧巖錄: Yuanwu Keqin says a traveller must possess the simultaneous-hatching eye and its use; Jingqing is discussed nearby, not the utterer.')
named(E[6],'Baiyun Shouduan','宗門統要正續集: the marker 續白雲端云 explicitly introduces Baiyun Shouduan’s comment that father and son meet in simultaneous pecking and tapping.',[cm('Baiyun Shouduan','utterer','commentator')])
changes['t_b8c3ecb60618']+=['Corrected Yuanwu commentary and explicitly introduced Baiyun comment.']

# 誓願: authorial prose, a question, and Huineng's quoted instruction.
E=D['t_e89833bb5e63']['Senses'][0]['Occurrences']
named(E[0],'Yongming Yanshou','宗鏡錄: Yongming Yanshou states in authorial exposition that one becomes a buddha through vows.')
un(E[1],'anonymous verse author','the unnamed lyric author in the Gaofeng Longquan collection','verse-author','The headword occurs in an unsigned lyric; the six attribution rungs do not identify its author.','高峰龍泉院因師集賢語錄: an unnamed lyric author writes of the four great vows.')
un(E[2],'unnamed questioner','the unnamed questioner asking how a person of little power may make vows','utterer','問 directly introduces the headword-bearing question.','無異元來禪師廣錄: an unnamed questioner asks Wuyi Yuanlai how a lowly person with little power can undertake vows.',[cm('Wuyi Yuanlai','respondent')])
named(E[4],'Huineng','南宗頓教最上大乘摩訶般若波羅蜜經六祖惠能大師於韶州大梵寺施法壇經: Huineng leads the assembly in reciting the four great vows.')
changes['t_e89833bb5e63']+=['Recovered Yongming and Huineng; distinguished anonymous lyric and questioner.']

# 陷虎之機: named label 一麟足 is an identified commentator, not an anonymous compiler.
E=D['t_aa56c106ef82']['Senses'][0]['Occurrences'];non(E[0],'named non-roster commentator','Yilinzu (一麟足)','commentator','一麟足云 explicitly introduces the headword-bearing appraisal.','宗鑑法林: Yilinzu (一麟足) says the World-Honored One has a mechanism for netting dragons and trapping tigers but lacks the use that turns iron into gold.',[cm('Buddha','person-discussed'),cm('Ananda','person-discussed')]);changes['t_aa56c106ef82'].append('Named the explicitly marked commentator Yilinzu.')

# 金鎖: the anthology heading and 乃曰 fix the record owner as the utterer.
E=D['t_dda048ca832d']['Senses'][0]['Occurrences'];named(E[8],'Shengyin Zi','五燈全書: Shengyin Zi says in his hall address that one must break through the golden-lock mystery barrier.',[cm('Shengyin Zi','utterer')]);changes['t_dda048ca832d'].append('Recovered Shengyin Zi from the immediately repeated section heading.')

# 透網金鱗: questioner, explicitly named Bao Hua dialogue, and anonymous verse author.
E=D['t_d95b944e0749']['Senses'][0]['Occurrences']
un(E[3],'unnamed monastic questioner','the unnamed monk asking why the golden-scaled fish still lingers in water','utterer','僧問 introduces the headword; 師云 introduces Wuyi Yuanlai’s answer.','無異元來禪師廣錄: an unnamed monk asks why a golden-scaled fish that passed through the net still lingers in water.',[cm('Wuyi Yuanlai','respondent')])
un(E[5],'unnamed monastic questioner','the unnamed monk questioning Baohua Chaozong Tongren','utterer','寶華因僧問 introduces the question; 師曰 begins Baohua’s answer.','宗鑑法林: an unnamed monk asks Baohua Chaozong Tongren why the golden-scaled fish still lingers in water.',[cm('Baohua Chaozong Tongren','respondent')])
un(E[6],'anonymous verse author','the unnamed linked-verse author','verse-author','The headword occurs inside an unsigned linked verse, not a dialogue turn.','禪宗頌古聯珠通集: an unnamed verse author calls it a lively golden-scaled fish that has passed through the net.')
changes['t_d95b944e0749'].append('Separated two monk questions and one anonymous linked verse from nearby masters.')

# 把斷: full dialogues place the selected clauses in monk questions; record-owner discourse remains named.
E=D['t_da72db7aa635']['Senses'][0]['Occurrences']
un(E[0],'unnamed monastic questioner','the unnamed monk asking whether blocking the strategic pass still leaves a way to teach','utterer','進云 introduces the headword-bearing follow-up; 師云 begins Yuanwu’s answer.','圓悟佛果禪師語錄: an unnamed monk asks whether, after blocking the strategic pass, there is still a way to teach.',[cm('Yuanwu Keqin','respondent')])
un(E[4],'unnamed monastic questioner','the unnamed monk saying Yunmen blocks the strategic pass','utterer','進云 introduces the clause; 師云 begins Miyin’s response.','廣福山勝覺寺密印禪師語錄: an unnamed monk says Yunmen blocks the strategic pass yet still cannot leap free.',[cm('Miyin','respondent')])
un(E[6],'unnamed monastic questioner','the unnamed monk saying the strategic pass is blocked tight','utterer','進云 introduces the headword-bearing follow-up; 師云 begins Gulin Qingmao’s answer.','古林清茂禪師語錄: an unnamed monk says the strategic pass is blocked so tightly that not even water passes.',[cm('Gulin Qingmao','respondent')])
changes['t_da72db7aa635'].append('Corrected three headword-bearing monk turns after reading their complete exchanges.')

# 青州布衫: the quotation is explicitly attributed to Yunju Shun, not the enclosing record owner.
E=D['t_c688927b7ea1']['Senses'][0]['Occurrences'];named(E[5],'Yunju Shun','千巖和尚語錄: the address explicitly quotes Yunju Shun (舜老夫云) saying “Zhenzhou’s radish is big; Qingzhou’s cloth shirt is heavy.”',[cm('Yunju Shun','utterer')]);changes['t_c688927b7ea1'].append('Recovered quoted speaker Yunju Shun from 舜老夫云.')

# 紅爐片雪: unsigned verses, biographical narration, and a signed preface must not inherit nearby masters.
E=D['t_d3631f4abf25']['Senses'][0]['Occurrences']
un(E[0],'anonymous case-verse author','the unnamed case-verse author','verse-author','頌曰 introduces an unsigned verse containing the headword.','禪宗頌古聯珠通集: an unnamed case-verse author likens the old foreigner’s blow to a snowflake flying in a great smelting furnace.')
un(E[1],'anonymous case-verse author','the unnamed case-verse author','verse-author','The headword occurs in an unsigned verse reproduced after named comments.','宗鑑法林: an unnamed case-verse author likens the old foreigner’s blow to a snowflake flying in a great smelting furnace.')
actor(E[2],'narrated','compiler narrative','the unnamed compiler of Wudeng Quanshu','compiler','The headword occurs in biography: 聞舉紅爐片雪話問, before Xue’an Congjin’s response.','五燈全書: the unnamed compiler narrates that Xue’an Congjin heard the “snowflake in a red furnace” saying raised in the room and was questioned on it.',[cm('Xuean Congjin','person-discussed')])
non(E[4],'signed preface author','Xilin Xuansu Tidaoren','utterer','The prose closes 席林玄素體道人書, explicitly signing the headword-bearing preface.','攖寧靜禪師語錄: Xilin Xuansu Tidaoren says the two houses’ exchanges are like a snowflake in a red furnace.')
changes['t_d3631f4abf25'].append('Distinguished two unsigned verses, compiler biography, and signed preface author.')

# 法臘: the first two selected clauses are questions; the final passage is first-person record-owner autobiography.
E=D['t_acaf1f7f698e']['Senses'][0]['Occurrences']
non(E[1],'named lay questioner','Shifan Yue','utterer','石帆岳司馬問 explicitly names the lay questioner who asks 法臘多少.','破山禪師語錄: Shifan Yue asks Poshan Haiming how many monastic years he has.',[cm('Poshan Haiming','respondent')])
un(E[2],'unnamed monastic questioner','the unnamed monk asking about monastic years','utterer','僧問 introduces the headword-bearing question; 師云 begins Daxiu Zhu’s answer.','大休珠禪師語錄: an unnamed monk asks that, although the matter is beyond years, monastic years still apply in the present gate.',[cm('Daxiu Zhu','respondent')])
named(E[3],'Yinyuan Longqi','隱元禪師語錄: Yinyuan Longqi says in first-person autobiographical prose that he has passed sixty worldly years and thirty monastic years.')
changes['t_acaf1f7f698e'].append('Identified Shifan Yue, an unnamed monk question, and confirmed Yinyuan first-person autobiography.')

for d in D.values():
 for s in d['Senses']:
  for o in s.get('Occurrences',[]):
   title=zc.title(o['RelPath']);note=o.get('AttributionNote','')
   if title and title not in note:o['AttributionNote']=f'{title}: {note}'
rows=[]
for i,p in P.items():
 p.write_text(json.dumps(D[i],ensure_ascii=False,indent=2)+'\n',encoding='utf8');rows.append({'id':i,'term':D[i]['SourceTerm'],'oldSha256':old[i],'newSha256':hashlib.sha256(p.read_bytes()).hexdigest(),'changes':changes[i] or ['Full-case reading confirmed every stored actor decision.']})
out=ROOT/'maintenance/attribution-read-adjudication/cohorts-7-9-096-105-full-read-repair-ledger.json';out.write_text(json.dumps({'generatedUtc':datetime.now(timezone.utc).isoformat(),'readCount':70,'tierAWitnessesRead':2,'rows':rows},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(out)
