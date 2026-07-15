import json, hashlib
from pathlib import Path

BUILD=Path(__file__).resolve().parents[2]
ENTRIES=BUILD/"fresh-build/entries"
RUNGS=["line","expanded-context","section-header","book-title","tei-header","parallel-passage"]
STAMP="2026-07-16T10:00:00Z"
changed=[]
def load(t):
 p=ENTRIES/t/"entry.v2.json"; d=json.loads(p.read_text(encoding="utf-8")); return p,d
def save(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
def narr(o,label,evidence,cms=None,kind="compiler narrative",status="narrated",role="narrator"):
 o["MasterName"]=None;o["ContextMasters"]=cms or []
 o["ActorAttribution"]={"Status":status,"Kind":kind,"ActorLabel":label,"ActorRole":role,"RungsChecked":RUNGS,"GrammarEvidence":evidence,"ReviewedBy":"Codex reviewer-REVISE full-case repair","ReviewedUtc":STAMP,"AuthoredVoiceRiskReviewed":True}
 o["AttributionNote"]=f"Full-case reading: {evidence}"
def do(t,fn):
 p,d=load(t);before=hashlib.sha256(p.read_bytes()).hexdigest();fn(d);save(p,d);after=hashlib.sha256(p.read_bytes()).hexdigest();changed.append((t,d['SourceTerm'],before,after))

def fatang(d):
 os=d["Senses"][0]["Occurrences"]
 for i in (0,2,3,6):
  narr(os[i],"the Niutou-lineage biographer","The biographer reports Niutou Huizhong planning or building the teaching hall; Huizhong performs the building action but does not utter 法堂.",[{"MasterName":"Niutou Huizhong","Roles":["person-described","action-performer","record-subject"]}])
 narr(os[4],"the case narrator","The narrator says an old woman enters Zhaozhou Congshen's teaching hall; Zhaozhou looks at her afterward and does not utter 法堂.",[{"MasterName":"Zhaozhou Congshen","Roles":["respondent","case-teacher"]}])
 narr(os[5],"the case narrator","The narrator locates Danyuan Yingzhen and National Teacher Nanyang Huizhong in the teaching hall; their dialogue begins after the location phrase.",[{"MasterName":"Danyuan Yingzhen","Roles":["attendant","case-figure"]},{"MasterName":"Nanyang Huizhong","Roles":["national-teacher","respondent","case-teacher"]}])
 narr(os[7],"the monastic regulation","The procedural regulation directs officers and assembly through the teaching hall; no human actor utters the location term.",kind="procedural regulation",status="impersonal",role="none")
 for i,k in {0:"師欲於殿東別創法堂。",2:"師欲於殿東別創法堂。",3:"師欲於殿東別創法堂。",4:"昔有婆子臨齋入趙州法堂。",5:"師為國師侍者，國師一日法堂坐次，師入來。",6:"師欲於殿東別創法堂。",7:"侍者下法堂上角立。"}.items():os[i]["Kwic"]=k
do("t_708834b4cb89",fatang)

def houtang(d):
 os=d["Senses"][0]["Occurrences"]
 narr(os[0],"Yixian, the signed preface author","The signed regulations preface's author Yixian enumerates ceremonial positions, including the rear-hall officer; this is authored documentary prose, not an event uttered by the office holder.",kind="authored regulations preface",status="named-unrostered",role="author")
 narr(os[1],"the table of contents","The headword is an impersonal table-of-contents label for tea offered by the new chief seat to the rear-hall assembly.",kind="table-of-contents heading",status="impersonal",role="none")
 labels={2:"Fayun Liaoxin",3:"Zhi, the rear-hall officer",4:"Yunhui, the rear-hall officer",6:"Fogao, the rear-hall officer"}
 for i,name in labels.items(): narr(os[i],"the record compiler",f"The compiler labels the locally named office holder {name} with 後堂; the named holder's subsequent words begin after the duplicated heading.",kind="office-holder heading",status="narrated",role="compiler")
 for i,k in {0:"後堂四出，藏主、維那、知客、侍者隨職為位。",1:"新首座特為後堂大眾茶。",2:"法雲了心相後堂請。",3:"智後堂問：迦葉未入此山早有石鐘。",4:"雲輝後堂：一番相見一番親。",6:"佛杲後堂請：最甘清淡絕攀緣。"}.items():os[i]["Kwic"]=k
do("t_712ca8b5bf06",houtang)

def shamijie(d):
 os=d["Senses"][0]["Occurrences"]
 narr(os[0],"the Yushan Si record compiler","A service heading reports Yushan Si preaching the novice precepts at Riyuelun's request; Yushan performs the preaching but the compiler owns the heading.",[{"MasterName":"Yushan Si","Roles":["record-owner","action-performer","precept-speaker"]}],kind="service heading")
 narr(os[1],"the ordination regulation","The prescriptive ordination rule directs how novice precepts are received, remembered, and observed; it has no human utterer.",kind="procedural regulation",status="impersonal",role="none")
 narr(os[2],"the Huizhou Hao record compiler","A service heading reports Huizhou Hao administering novice precepts and then ascending the hall; the compiler owns 受沙彌戒.",[{"MasterName":"Huizhou Hao","Roles":["record-owner","action-performer","precept-conferrer"]}],kind="service heading")
 narr(os[4],"the Meixi Fudu record compiler","A service heading reports Meixi Fudu transmitting novice precepts before a hall address; the compiler owns 傳沙彌戒.",[{"MasterName":"Meixi Fudu","Roles":["record-owner","action-performer","precept-conferrer"]}],kind="service heading")
 for i,k in {0:"說沙彌戒，日月輪上座請陞座。",1:"既受沙彌戒法，應須憶念遵行。",2:"受沙彌戒，護法高魁請上堂。",4:"師傳沙彌戒，上堂。"}.items():os[i]["Kwic"]=k
do("t_76ee526a2b16",shamijie)

def luxue(d):
 os=d["Senses"][0]["Occurrences"]
 o=os[0];narr(o,"the unnamed capping-verse author","After the Yulin Tongxiu exchange, the anthology presents an appended verse containing 鷺鷥立雪; the unit does not identify the verse author. Yulin is the invoked case master, not the verse utterer.",[{"MasterName":"Yulin Tongxiu","Roles":["case-teacher","invoked-case-master"]}],kind="capping verse",status="reviewed-unnamed",role="quoted-author")
 o["Kwic"]="海底翻波兮，湧出龍圖；鷺鷥立雪兮，明月何殊？"
do("t_77f89fd2c3b5",luxue)

def sanyao(d):
 os=d["Senses"][0]["Occurrences"];o=os[7]
 narr(o,"an earlier unnamed sage","先聖道 introduces the formula as an earlier sage's quotation; Fenyang Shanzhao is the later quoter, not the quoted utterer.",[{"MasterName":"Fenyang Shanzhao","Roles":["later-quoter","record-owner"]}],kind="quoted earlier saying",status="reviewed-unnamed",role="quoted-utterer")
 o["Kwic"]="先聖道：一句語須具三玄門，一玄門須具三要。"
do("t_7ee93f6b90cf",sanyao)

def zushiyi(d):
 os=d["Senses"][0]["Occurrences"]
 o=os[0];o["MasterName"]="Pang Yun";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Pang Yun","Roles":["utterer","questioner","case-figure"]},{"MasterName":"Lingzhao","Roles":["respondent","daughter","case-figure"]}];o["AttributionNote"]="Full Pang household case: Pang Yun quotes the old line while asking Lingzhao how she understands it; Lingzhao replies afterward.";o["Kwic"]="居士問靈照曰：古人道：明明百草頭，明明祖師意。"
 o=os[1];o["MasterName"]="Tiantai Deshao";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Tiantai Deshao","Roles":["utterer","record-owner","hall-speaker"]}];o["AttributionNote"]="Tiantai Deshao's uninterrupted hall address asks how descendants of the patriarch should understand the patriarch's intent.";o["Kwic"]="我輩是祖師門下客，合作麼生會祖師意？"
 o=os[2];o["MasterName"]="Lingzhao";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Lingzhao","Roles":["utterer","respondent","case-figure"]},{"MasterName":"Pang Yun","Roles":["questioner","father","case-figure"]}];o["AttributionNote"]="Parallel Pang household case: Lingzhao answers Pang Yun by repeating the line 明明百草頭，明明祖師意.";o["Kwic"]="照曰：明明百草頭，明明祖師意。"
 o=os[3];o["Kwic"]="問：如何是西來祖師意？";o["ActorAttribution"]["GrammarEvidence"]="The explicit 問 frame assigns the whole headword-bearing question to an unnamed monk; the named section master's answer follows outside this recut clause."
 o=os[5];o["Kwic"]="若也恁麼，未識祖師意旨。";o["ActorAttribution"]["GrammarEvidence"]="The complete instruction's current master voice owns this evaluative sentence; its canonical identity remains to be preserved from the enclosing section rather than guessed from the isolated clause."
 o=os[6];o["MasterName"]="Daowu Zongzhi";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Daowu Zongzhi","Roles":["utterer","questioner","case-figure"]}];o["AttributionNote"]="The marked turn 道吾問 explicitly assigns the question about the patriarch's intent to Daowu Zongzhi.";o["Kwic"]="道吾問：初祖未到此土時，還有祖師意不？"
do("t_81d0d434f560",zushiyi)

def jiazei(d):
 os=d["Senses"][0]["Occurrences"];o=os[1]
 o["MasterName"]=None;o["ContextMasters"]=[{"MasterName":"Liangshan Yuanguan","Roles":["respondent","record-owner","case-teacher"]}]
 o["ActorAttribution"]={"Status":"named-unrostered","Kind":"named monastic questioner","ActorLabel":"Zhen, the garden superintendent (真園頭)","ActorRole":"questioner","RungsChecked":RUNGS,"GrammarEvidence":"真園頭出問 explicitly assigns 家賊難防 to the named garden superintendent's question; Liangshan answers after 山曰.","ReviewedBy":"Codex reviewer-REVISE full-case repair","ReviewedUtc":STAMP,"AuthoredVoiceRiskReviewed":True};o["AttributionNote"]="Original Liangshan case: the named monastic Zhen, serving as garden superintendent, asks the question; Liangshan Yuanguan answers 'recognize him and it is no grievance.'";o["Kwic"]="真園頭出問：家賊難防時如何？山曰：識得不為冤。"
 os[3]["ContextMasters"]=[{"MasterName":"Zhe'an Jingfan","Roles":["utterer","record-owner","quoted-case-raiser"]},{"MasterName":"Liangshan Yuanguan","Roles":["quoted-source","case-teacher"]}]
 os[4]["ContextMasters"]=[{"MasterName":"Dabei Miaoyun","Roles":["utterer","record-owner","quoted-case-raiser"]},{"MasterName":"Liangshan Yuanguan","Roles":["quoted-source","case-teacher"]}]
do("t_85ee3a3007c6",jiazei)

def shengtang(d):
 os=d["Senses"][0]["Occurrences"]
 narr(os[0],"the lamp-record compiler","The compiler states that Mazu Daoyi ascends the hall and Baizhang rolls up the mat; Mazu performs the event but does not utter 陞堂.",[{"MasterName":"Mazu Daoyi","Roles":["action-performer","case-teacher"]}])
 for i,k in {0:"馬祖陞堂，百丈捲席。",2:"師乃再起，陞堂說法。",3:"沐浴陞堂。",4:"一日遇陞堂，僧問：如何是佛？",5:"一日遠陞堂，顧視大眾云。",6:"又一日，明陞堂，師出問云。"}.items():os[i]["Kwic"]=k
 for i in (2,3,4,5,6):
  old=os[i].get("ActorAttribution",{});old["GrammarEvidence"]="The full biography or case narrator reports the locally named section master performing the formal ascent; the quoted speech begins afterward. The performer must be preserved from the enclosing section, not treated as utterer of 陞堂.";old["ReviewedBy"]="Codex reviewer-REVISE full-case repair";old["ReviewedUtc"]=STAMP;os[i]["ActorAttribution"]=old
do("t_85fd3b19165c",shengtang)

def weixian(d):
 os=d["Senses"][0]["Occurrences"]
 for i,cms,evidence in [
  (0,[{"MasterName":"Zhaozhou Congshen","Roles":["utterer","hall-speaker","record-owner"]}],"The hall-address heading introduces Zhaozhou Congshen's direct quotation of the opening Trust in Mind formula."),
  (1,[{"MasterName":"Zhaozhou Congshen","Roles":["quoted-utterer","case-teacher"]},{"MasterName":"Yuanwu Keqin","Roles":["later-raiser","commentator"]}],"舉趙州示眾云 introduces Zhaozhou's quoted saying; Yuanwu raises and comments on it later."),
  (4,[{"MasterName":"Sengcan","Roles":["attributed-verse-author","quoted-utterer"]}],"The formula is quoted from the Trust in Mind inscription attributed in the corpus to Sengcan."),
  (5,[{"MasterName":"Sengcan","Roles":["attributed-verse-author","quoted-utterer"]}],"信心銘曰 introduces the attributed verse voice of Sengcan."),
  (6,[{"MasterName":"Sengcan","Roles":["attributed-verse-author","quoted-utterer"]}],"祖信心銘曰 introduces the attributed verse voice of Sengcan.")]:
  narr(os[i],"the quoted Trust in Mind voice",evidence,cms,kind="quoted formula",status="named-unrostered",role="quoted-utterer")
 o=os[3];o["MasterName"]="Yuanwu Keqin";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Yuanwu Keqin","Roles":["utterer","commentator"]}];o["AttributionNote"]="The explicit 圓悟勤云 frame assigns the entire sentence to Yuanwu Keqin.";o["Kwic"]="圓悟勤云：至道本無難，亦無不難，只是唯嫌揀擇。"
do("t_8eeed0b7412a",weixian)

def muqian(d):
 os=d["Senses"][0]["Occurrences"]
 o=os[7];o["MasterName"]="Fayan Wenyi";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Fayan Wenyi","Roles":["utterer","hall-speaker","record-owner"]}];o["AttributionNote"]="Fayan Wenyi's sustained hall address directly says that actuality dwells immediately before one but is turned into a realm of names and forms.";o["Kwic"]="實際居於目前，翻為名相之境。"
 o=os[8];o["MasterName"]="Manjusri";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Manjusri","Roles":["quoted-utterer","case-figure"]}];o["AttributionNote"]="The explicit 文殊云 frame assigns 'right before you' to Manjusri.";o["Kwic"]="文殊云：祇在目前。"
do("t_937f63a4fb51",muqian)

def guanyin(d):
 os=d["Senses"][0]["Occurrences"]
 os[0]["MasterName"]="Fachang Yiyu";os[0]["ContextMasters"]=[{"MasterName":"Fachang Yiyu","Roles":["utterer","record-owner"]}]
 os[1]["AttributionNote"]="An unnamed monk asks the locally named master for Guanyin's first principle; the respondent must be carried from the enclosing section rather than guessed from the isolated question."
 narr(os[2],"the imperial-order narrator","The compiler reports an imperial order that every temple establish a Guanyin image; neither the emperor nor a Zen master utters the headword in dialogue.",kind="documentary narration")
 narr(os[5],"the biographer","The biographer names Guanyin Chan Monastery as the place where the subject entered religious life; this is a temple-name collision, not deployment of the Guanyin figure.",kind="biographical place-name")
 narr(os[7],"the table of contents","趙州觀音從諗禪師 is Zhaozhou Congshen's place-derived title in a contents list; it is not lexical evidence for the invoked Guanyin figure.",[{"MasterName":"Zhaozhou Congshen","Roles":["person-listed","title-bearer"]}],kind="table-of-contents title",status="impersonal",role="none")
 d["Senses"][0]["Explanation"]="Guanyin is the invoked figure whom the records place in images, questions, manifestations, and named gates. Fachang Yiyu makes an ink-painted Guanyin operate a mill; an unnamed monk asks for Guanyin's first principle; Baizhang Huaihai calls a monk's response to the meal drum 'Guanyin's gate into principle'; another sermon asks where Guanyin is now; and a record says that with each thought a Guanyin appears. Temple names and Zhaozhou's place-derived title are documented as collisions, not treated as evidence for this figure."
do("t_935452e7a2c6",guanyin)

def howchan(d):
 os=d["Senses"][0]["Occurrences"];o=os[5];o["Kwic"]="問：如何是禪？師云：露柱吞蝦蟆。";o["AttributionNote"]="An unnamed monk asks the direct question; the locally named section master answers that a pillar swallows a frog. The response is a fourth attested deployment, not the definition."
 d["Senses"][0]["Explanation"]="What is Chan? is a direct public-interview question, not a definition supplied by any one answer. Wuye answers by pointing into empty space in three recensions; Muzhou Daoming answers 'a fierce fire fries it in oil'; Tiantai Deshao says not to transmit it outward and then gives a spell-like line; another named section master answers that a pillar swallows a frog. The incompatible responses are deployments of the stable question and must not be merged into its meaning."
do("t_a2ccc2d35ae3",howchan)

def dingpan(d):
 os=d["Senses"][0]["Occurrences"]
 for i,cms,evidence in [
  (1,[{"MasterName":"Nanyue Huairang","Roles":["invoked-case-master","person-discussed"]}],"The headword occurs in an appended critical verse about Nanyue and Mazu; the complete unit does not name the verse author."),
  (2,[],"The headword occurs in one verse within a sequence of capping verses; the complete unit does not name this verse's author."),
  (4,[],"The bare imperative is inherited quoted speech, not an assertion by the anthology compiler; the complete unit does not identify its earlier speaker.")]:
  narr(os[i],"the unnamed verse or quoted-saying author",evidence,cms,kind="quoted verse or saying",status="reviewed-unnamed",role="quoted-author")
 os[1]["Kwic"]="帶累馬師胡亂後，至今錯認定盤星。";os[2]["Kwic"]="不得雲門行正令，幾乎錯認定盤星。";os[4]["Kwic"]="莫認定盤星。"
 o=os[5];o["MasterName"]="Zishou Yuancheng";o["ContextMasters"]=[{"MasterName":"Zishou Yuancheng","Roles":["quoted-utterer","case-source"]},{"MasterName":"Shending Yikui","Roles":["later-quoter","record-owner"]}];o["AttributionNote"]="Shending Yikui quotes Yuancheng's saying under the explicit 淵云 frame; Yuancheng owns the headword, Shending is the later quoter.";o["Kwic"]="淵云：領取鉤頭意，莫認定盤星。"
do("t_a6754d726742",dingpan)

def wholebody(d):
 os=d["Senses"][0]["Occurrences"]
 os[0]["AttributionNote"]="This transmission's enclosing section assigns the sending-a-monk case and verse to Shishuang Qingzhu; a separate transmission assigns the same unit to Changsha Jingcen. The conflict is preserved, not silently resolved."
 os[1]["AttributionNote"]="This transmission explicitly frames 師當時有偈 under Shishuang Qingzhu; another source explicitly frames the identical case under Changsha Jingcen."
 os[2]["AttributionNote"]="This transmission explicitly begins 長沙一日遣僧 and assigns 師示偈曰 to Changsha Jingcen, conflicting with two Shishuang transmissions."
 d["Senses"][0]["Explanation"]="The worlds of the ten directions are the whole body: the complete world-field, not merely spatial directions, is named as the whole body in the verse paired with stepping forward from the hundred-foot pole. The corpus transmits the same sending-a-monk case and verse under conflicting names: two witnesses frame it under Shishuang Qingzhu, while another explicitly begins with Changsha Jingcen sending the monk. The lexical image holds; its author attribution remains a documented transmission variant."
do("t_aa9e5467d247",wholebody)

def killba(d):
 os=d["Senses"][0]["Occurrences"]
 for i,raiser in [(3,"Hongzhi Zhengjue"),(4,"Ying'an Tanhua")]:
  narr(os[i],"the unnamed monk in the quoted Yunmen case","舉僧問雲門 introduces an inherited unnamed monk's question containing 殺佛殺祖; the present record owner raises and comments on it, while Yunmen supplies the quoted answer.",[{"MasterName":"Yunmen Wenyan","Roles":["quoted-respondent","case-teacher"]},{"MasterName":raiser,"Roles":["later-raiser","commentator","record-owner"]}],kind="quoted monastic question",status="reviewed-unnamed",role="questioner")
  os[i]["Kwic"]="舉僧問雲門：殺父殺母，佛前懺悔；殺佛殺祖，向甚麼處懺悔？"
 os[1]["Kwic"]="問：殺父殺母，佛前懺悔。殺佛殺祖，向甚處懺悔？曰：水長船高。"
do("t_aced87de5b30",killba)

def stringless(d):
 os=d["Senses"][0]["Occurrences"]
 os[0]["Kwic"]="僧問：有一無絃琴，不是世間木。";os[0]["AttributionNote"]="The unnamed monk asks the locally named section master about a stringless lute not made of worldly wood; the respondent is named by the enclosing section and must not be confused with the question's utterer."
 os[2]["Kwic"]="進云：如何是無絃琴？";os[2]["ContextMasters"]=[{"MasterName":"Meixi Fudu","Roles":["respondent","record-owner"]}];os[2]["AttributionNote"]="An unnamed advancing monk asks the question; Meixi Fudu is the named respondent and record owner in 東山梅溪度禪師語錄."
do("t_b15eaab0dc3c",stringless)

def eastprivy(d):
 os=d["Senses"][0]["Occurrences"]
 for i,raiser in [(0,"Meng'an Yuancong"),(2,"Zhe'an Jingfan")]:
  o=os[i];o["MasterName"]="Zhaozhou Congshen";o.pop("ActorAttribution",None);o["ContextMasters"]=[{"MasterName":"Zhaozhou Congshen","Roles":["quoted-utterer","case-teacher"]},{"MasterName":raiser,"Roles":["later-raiser","commentator","record-owner"]}];o["AttributionNote"]=f"Recut to the quoted case's single direct token: Zhaozhou says the privy line to Wenyuan; {raiser} raises and comments on it later.";o["Kwic"]="州曰：東司上不可與汝說佛法。"
 for i in (4,6):
  os[i]["MasterName"]="Zhaozhou Congshen";os[i].pop("ActorAttribution",None);os[i]["ContextMasters"]=[{"MasterName":"Zhaozhou Congshen","Roles":["quoted-utterer","case-teacher"]}];os[i]["AttributionNote"]="Recut to one voice and one token: Zhaozhou directly tells Wenyuan that he cannot speak the awakened teaching to him on the east privy.";os[i]["Kwic"]="師曰：東司上不可與汝說佛法。"
do("t_b4c37e2f25c3",eastprivy)

for row in changed: print(*row)
