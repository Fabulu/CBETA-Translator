import json
from pathlib import Path
BUILD=Path(__file__).resolve().parents[2];reviewed="2026-07-16T06:30:00Z"
p=BUILD/"fresh-build/entries/t_df2096b961c1/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
o=os[3];o["MasterName"]="Tian'an Sheng";o["ContextMasters"]=[{"MasterName":"Tian'an Sheng","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":"Fengxue Yanzhao","Roles":["quoted-source","case-figure"]}];o["AttributionNote"]="Recorded Sayings of Tian'an Sheng, complete hall address: Tian'an raises and comments on Fengxue Yanzhao's iron-ox mechanism case. Tian'an utters the stored wording; Fengxue is the quoted source."
o=os[5];o["MasterName"]="Yuanwu Keqin";o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["utterer","commentator","later-raiser"]},{"MasterName":"Fengxue Yanzhao","Roles":["quoted-source","case-figure"]}];o["AttributionNote"]="Blue Cliff Record, complete case introduction: Yuanwu Keqin raises Fengxue Yanzhao's iron-ox mechanism case. Yuanwu utters the stored wording; Fengxue is the quoted source."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 36')

# 37 向上事 — ten complete cases hand-read.
p=BUILD/"fresh-build/entries/t_e84753568cda/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
for i,src in [(5,"Dongshan Liangjie"),(8,"Xuefeng Yicun")]:
 o=os[i];o["MasterName"]="Yunmen Wenyan";o["ContextMasters"]=[{"MasterName":"Yunmen Wenyan","Roles":["utterer","record-owner","later-raiser"]},{"MasterName":src,"Roles":["quoted-source","case-figure"]}];o["AttributionNote"]=f"Yunmen's chamber material, complete raised case: Yunmen Wenyan recites the {src} exchange containing the headword. Yunmen is the utterer; {src} is the quoted source."
o=os[6];o["MasterName"]=None;o["ContextMasters"]=[];o["ActorAttribution"]={"Status":"named-unrostered","Kind":"master","ActorLabel":"the master of Longjun Mountain (龍峻山禪師)","ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"師曰不識善惡說甚麼向上事 assigns the phrase to the Longjun Mountain master named by the section heading; it is not Zhaozhou.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]="Five Lamps Meeting the Source, complete Longjun Mountain section: the Longjun master says, ‘If one does not distinguish good and bad, what upward matter are you talking about?’ The prior Zhaozhou attribution was a section error."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 37')

# 38 梵王 — six complete cases hand-read.
p=BUILD/"fresh-build/entries/t_088731c7824b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
def actor(o,status,kind,label,role,evidence,note,contexts=None):
 o["MasterName"]=None;o["ContextMasters"]=contexts or [];o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":evidence,"ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]=note
actor(os[0],"reviewed-unnamed","merchant questioner","one of the two unnamed merchants","questioner","\u554f\u66f0：\u70ba\u662f\u68b5\u738b\u8036 assigns the headword-bearing question to one of the two merchants who encounter the seated World-Honored One.","Complete Buddha-case unit in Five Lamps Complete Book: one of two merchants asks whether the seated World-Honored One is a Brahma king. The merchant, not the Buddha or compiler, utters the headword.")
actor(os[1],"named-nonmaster","Brahma king","Brahma King Equal Conduct (等行)","questioner","\u6709\u4e00\u68b5\u738b\u540d\u66f0\u7b49行白佛言 explicitly names Equal Conduct and introduces his question to the Buddha.","Record of the Mirror of the Teaching, complete cited scripture-and-comment unit: Brahma King Equal Conduct asks which manifested tathagata is real. Equal Conduct is a named non-master actor; Yongming's following commentary is a separate voice.")
o=os[2];o["MasterName"]=None;o["ContextMasters"]=[];o["ActorAttribution"]={"Status":"named-unrostered","Kind":"master","ActorLabel":"Baizhang Zhiying Baoyue (百丈智映寶月禪師)","ActorRole":"utterer","RungsChecked":["line","expanded-context","section-header","book-title","tei-header","parallel-passage"],"GrammarEvidence":"The nearest section heading is 洪州百丈山智映寶月禪師 and the sole line is marked 師云.","ReviewedBy":"Codex cohorts 1-3 v6 full-case hand read","ReviewedUtc":reviewed};o["AttributionNote"]="Records of the Lamp, complete one-line section occurrence: Baizhang Zhiying Baoyue says, 'A Brahma king leads in front; Indra follows behind.' He is explicitly identified by the section heading but has no safely normalized roster name here."
actor(os[3],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","\u50e7問 introduces the headword-bearing comparison before the Guangsheng master answers.","Complete Guangsheng Shihu section exchange in Five Lamps Strict Lineage: an unnamed monk contrasts the Brahma king's former invitation with the ruler's present attendance. The monk utters the headword; Guangsheng Shihu answers afterward.")
actor(os[4],"reviewed-unnamed","master","the unnamed Chengtian record-owner","utterer","\u5e2b云過去梵王引… places the phrase in the master's answer, after the preceding interlocutor's remark; the supplied structural unit does not expose a personal name.","Ancient Worthies' Recorded Sayings, complete exchange unit from a Chengtian record: the record-owner master says, 'In the past the Brahma king led; in the present Shakyamuni is revered.' The passage names the institutional setting but not a safely normalized personal name.")
actor(os[5],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","\u6642有僧問 introduces the headword-bearing opening-day question; the Jinsha master answers with 從古至今.","Complete Jinsha master opening-day case: an unnamed monk asks about the Brahma king inviting the Buddha and Kashyapa striking the block. The monk utters the headword; the Jinsha master is respondent.")
d["Senses"][0]["Explanation"]="The Brahma king is a pre-Chan figure whom Chan records place before the Buddha as requester or attendant. The stored witnesses invoke him in ceremonial comparisons and in questions about who initiated or witnessed an assembly; none of these headword cases supports calling him the presenter of the flower."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 38')

# 39 草賊 — six complete cases hand-read.
p=BUILD/"fresh-build/entries/t_09909bd0c29e/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
actor(os[0],"reviewed-unnamed","monastic questioner","the unnamed monk","answerer","After Linji asks whether his shout was good, 僧云：草賊大敗 assigns the verdict to the monk; Linji's next turn asks where the fault lies.","Linji's complete hall exchange: an unnamed monk answers Linji's question with 'The petty bandit is badly defeated.' Linji is the respondent in the next turn, not the utterer of this occurrence.",[{"MasterName":"Linji Yixuan","Roles":["respondent","record-owner"]}])
actor(os[1],"named-unrostered","master","Chongfan Yu (崇梵餘禪師)","utterer","The section opens 建州崇梵餘禪師 and the answer is marked 師曰：草賊大敗.","Five Lamps Complete Book, complete Chongfan Yu exchange: Chongfan Yu answers the monk's gesture with 'The petty bandit is badly defeated.' The inherited Tiantong Danjiao attribution came from the adjacent preceding section and was false.")
for i in (2,3,4,5):
 o=os[i];o["MasterName"]="Linji Yixuan" if i==2 else "Zhaozhou Congshen"
# Replace the stale name in the reader-facing gloss while preserving its evidence-based claim.
d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace("Tiantong Danjiao","Chongfan Yu")
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 39')

# 40 白椎 — seven complete structural units hand-read.
p=BUILD/"fresh-build/entries/t_0ecac939496b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
actor(os[0],"named-nonmaster","lay author","Su Shi (蘇軾)","quoted-author","The narrative identifies 坡 and says he produced the written appeal, whose closing sentence is 一時稽首，重聽白椎.","Five Lamps Complete Book, complete Shita Jie episode: Su Shi authors the appeal asking the assembly to bow and hear the mallet proclamation again; Chao Buzhi reads it aloud. Shita Jie is the person being petitioned, not the utterer of the headword.")
actor(os[1],"impersonal","table-of-contents label","the table-of-contents compiler","editor","The token appears only in the volume heading 白椎後垂語 inside a contents list, not in dialogue or attributed prose.","Patriarchs' Guidelines, complete contents-list unit: the headword is part of the editorial heading 'instructions after the mallet proclamation.' No master utters it.")
actor(os[2],"reviewed-unnamed","hall officer","the unnamed hall coordinator (weina)","ritual-proclaimer","\u7dad那白椎云 directly assigns the proclamation formula to the hall coordinator before the master raises his staff.","Recorded Sayings of Konggu Daocheng, complete opening ceremony: the unnamed hall coordinator makes the mallet proclamation, 'The dragons and elephants at the Dharma assembly should contemplate the first principle.' Konggu Daocheng speaks only afterward.")
actor(os[3],"impersonal","compiler narrative","the record compiler","compiler","\u767d椎竟 is a narrative completion marker between taking the seat and the master's next action; it does not quote an utterance containing the headword.","Recorded Sayings of Yulin Tongxiu, complete enthronement unit: the compiler marks that the mallet proclamation had concluded before Yulin lifted his staff. The headword is narration, not Yulin's speech.")
actor(os[4],"reviewed-unnamed","preface author","the unnamed first-person preface author","author","\u4e88…焚香披閱一二，見其白椎說法處 is explicit first-person evaluative prose by the preface author; the supplied full unit does not safely expose his name.","Preface to Juelang Daosheng's recorded sayings, complete autobiographical preface unit: the first-person author says that, on reading the collection, he saw places where Juelang made the mallet proclamation and preached. Juelang is the described performer, not the utterer of the stored sentence.",[{"MasterName":"Juelang Daosheng","Roles":["subject","described-performer"]}])
actor(os[5],"impersonal","compiler narrative","the record compiler","compiler","\u7d2b谷大師白椎云 is record narration naming Zigu as the ritual performer and then quoting the formula; the compiler, not Zigu, supplies the headword token.","Recorded Sayings from Qinglong Longfu Monastery, complete opening ceremony: the compiler reports that Master Zigu made the mallet proclamation and quotes its formula. Zigu performs the rite; the headword itself occurs in the compiler's frame.")
actor(os[6],"impersonal","compiler narrative","the record compiler","compiler","\u6582衣就座白椎竟 narrates completion of the proclamation before Xueqiao raises an earlier case.","Recorded Sayings of Xueqiao Yuanxin, complete opening unit: the compiler marks the mallet proclamation as completed. Xueqiao's raised case follows; he does not utter this headword occurrence.",[{"MasterName":"Xueqiao Yuanxin","Roles":["record-owner","subsequent-speaker"]}])
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 40')

# 41 一炷香 — six complete cases hand-read.
p=BUILD/"fresh-build/entries/t_0f4c2ed08d86/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
actor(os[0],"reviewed-unnamed","master","the unnamed later Linji-line record-owner","utterer","The first-person speaker says 供養我臨濟先師, explicitly making Linji his deceased predecessor; he therefore cannot be Linji Yixuan.","Ancient Worthies' Recorded Sayings, complete incense declaration: a later Linji-line master recounts learning from Sansheng and Dajue and dedicates one stick of incense to 'my late teacher Linji.' The inherited Linji attribution contradicted the speech itself.",[{"MasterName":"Linji Yixuan","Roles":["dedication-recipient","predecessor"]}])
# O2–O5 are direct first-person/ceremonial speech by the inherited record owners.
o=os[5];o["MasterName"]="Faxi Yin";o["ContextMasters"]=[{"MasterName":"Faxi Yin","Roles":["utterer","record-owner","memorial-speaker"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Recorded Sayings of Faxi Yin, complete stupa memorial: Faxi Yin says that he offers a cup of tea and burns one stick of incense before his late teacher's stupa. The headword is inside his direct quotation, not compiler narration."
d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace("Linji dedicates one stick to his teacher", "a later Linji-line master dedicates one stick to his teacher Linji")
d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace(". a later Linji-line", ". A later Linji-line")
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 41')

# 42 棒喝 — ten complete cases hand-read.
p=BUILD/"fresh-build/entries/t_0f97bfab265c/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
actor(os[0],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","\u554f德山入門便棒…不動棒喝還有為人處也無 assigns the compound to the monk's question; Yulin answers only with 有.","Yulin Tongxiu's complete evening interview: an unnamed monk asks whether Deshan and Linji have a place for people without moving stick or shout. Yulin is respondent, not utterer.",[{"MasterName":"Yulin Tongxiu","Roles":["respondent","record-owner"]}])
actor(os[1],"impersonal","compiler narrative","the record compiler","compiler","\u81e8濟出世後，唯以棒喝示徒 is third-person biographical narration about Linji, not Baichi's marked direct speech.","Baichi Yuanshuo's collection, complete one-line raised narrative: the compiler or reciter describes Linji as using stick-blows and shouts. Baichi is not explicitly the utterer of this standalone wording.",[{"MasterName":"Linji Yixuan","Roles":["subject","case-figure"]}])
# O3 is Huangbo Wunian Shenyou's letter; O5–O7 and O9 remain direct record-owner speech.
actor(os[3],"reviewed-unnamed","monastic questioner","the unnamed monk","questioner","\u554f：德山棒、臨濟喝，除此二途… places the compound in the monk's question; Fushi answers and strikes afterward.","Fushi Tongxian's complete opening interview: an unnamed monk asks what indication exists beyond Deshan's stick and Linji's shout. Fushi is respondent, not utterer.",[{"MasterName":"Fushi Tongxian","Roles":["respondent","record-owner"]}])
actor(os[7],"named-nonmaster","lay questioner","Yu Jisheng (余集生)","questioner","\u4f59集生冏卿問 introduces the entire question, including 纔著袈裟便行棒喝者.","Chan Gate Forge, complete letter-question: Yu Jisheng asks whether robe-wearers who immediately wield stick and shout truly represent the Linji school. Wuyi Yuanlai is the addressee, not the utterer.",[{"MasterName":"Wuyi Yuanlai","Roles":["addressee","respondent"]}])
o=os[9];o["MasterName"]="Tian'an Sheng";o["ContextMasters"]=[{"MasterName":"Tian'an Sheng","Roles":["utterer","record-owner","later-raiser"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Recorded Sayings of Tian'an Sheng, complete evening address: Tian'an constructs and voices the comic scene in which Indra hears 'stick-blows and shouts crossing.' Tian'an is the local utterer; Indra is the quoted figure within his address."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 42')

# 43 東山水上行 — eight complete cases hand-read.
p=BUILD/"fresh-build/entries/t_114bbd284d1c/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
o=os[1];o["MasterName"]="Yuanwu Keqin";o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["utterer","later-raiser","commentator"]},{"MasterName":"Yunmen Wenyan","Roles":["quoted-source","case-figure"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Complete Dahui biography recounting Yuanwu Keqin's Tianning address: Yuanwu raises Yunmen's 'East Mountain walks on water' exchange and supplies his own contrasting answer. Yuanwu is the local utterer; Yunmen is the quoted source."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 43')

# 44 臨濟喝 — seven complete cases hand-read; inherited actor rulings survive.
p=BUILD/"fresh-build/entries/t_1403ddf1e83b/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"))
d["Senses"][0]["Explanation"]=d["Senses"][0]["Explanation"].replace("The named shout associated by the Chan record with Linji Yixuan. ","")
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('gated hand-read entry 44')

# 45 分別 — nine complete cases hand-read.
p=BUILD/"fresh-build/entries/t_15026800437e/entry.v2.json";d=json.loads(p.read_text(encoding="utf-8"));os=d["Senses"][0]["Occurrences"]
o=os[4];o["MasterName"]="Huangbo Xiyun";o["ContextMasters"]=[{"MasterName":"Huangbo Xiyun","Roles":["utterer","respondent","record-owner"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Essentials of Mind Transmission, complete question-and-answer: Huangbo Xiyun answers that delusion lacks a root and exists only because of discrimination. The headword occurs in Huangbo's marked reply."
actor(os[5],"impersonal","quoted scripture narrative","the scripture narrator quoted by the lamp compiler","narrator","The headword occurs inside a sustained scriptural simile about the bodhisattva at the unmoving ground, before third-person narration says that Shan awakened; no Chan master utters it locally.","Five Lamps Complete Book, complete scripture-and-awakening unit: the quoted scripture narrator says that mental motion, recollection, imagination, and discrimination cease. The later master is the subject who awakens after the quotation, not its utterer.")
actor(os[6],"reviewed-unnamed","master","the unnamed record-owner giving the opening-fire address","utterer","\u958b爐示眾 introduces a continuous first-person teaching-seat address; 蓋能所分別作障礙 is inside that address, but the supplied unit exposes no safely normalized personal name.","Complete opening-fire address: the record-owner says that discrimination into knower and known makes an obstruction. The full speech is clear, but this structural packet does not expose a safely normalized roster name.")
actor(os[7],"named-unrostered","master","Faming of Xingzhou Kaiyuan (邢州開元法明上座)","utterer","The nearest section heading names 邢州開元法明上座 and 師乃曰 introduces his death verse containing the headword.","Five Lamps Complete Book, complete Faming biography: Faming says in his death verse that although his life reeled in drunkenness, within drunkenness there was discrimination. He is named by the section heading but retained as unrostered here.")
o=os[8];o["MasterName"]="Yongjia Xuanjue";o["ContextMasters"]=[{"MasterName":"Yongjia Xuanjue","Roles":["utterer","interlocutor"]},{"MasterName":"Huineng","Roles":["respondent","case-figure"]}];o.pop("ActorAttribution",None);o["AttributionNote"]="Imperially Selected Record of Yongjia Xuanjue, complete Huineng exchange: Yongjia Xuanjue tells Huineng, 'You yourself give rise to discrimination.' Huineng replies in the next turn; Yongjia is the exact utterer."
for s in d["Senses"]:s["SourceTexts"]=list(dict.fromkeys(o["RelPath"] for o in s["Occurrences"]))
p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print('repaired hand-read entry 45')
